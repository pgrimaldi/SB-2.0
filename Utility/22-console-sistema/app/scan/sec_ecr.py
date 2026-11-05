"""Immagini ECR scaricate in formato OCI image layout, senza Docker.

Per ogni repository una cartella `artifacts/<region>/ecr/<repository>/` con:
- `oci-layout` e `index.json` (una voce per tag, annotazione `org.opencontainers.image.ref.name`);
- `blobs/sha256/<digest>`: manifest, config e layer. I blob condivisi fra immagini dello stesso
  repository si scaricano una volta sola.

Manifest via `batch_get_image` (byte esatti, il digest si verifica); config e layer via
`get_download_url_for_layer` (URL prefirmato S3, chiesto subito prima di scaricare). I manifest
multi-architettura (index / manifest list) si seguono fino alle immagini figlie, attestazioni
comprese.

Per ricaricare un'immagine altrove:
  skopeo copy oci:<cartella>:<tag> docker://<account>.dkr.ecr.<region>.amazonaws.com/<repo>:<tag>
"""
from __future__ import annotations

import hashlib
import json
import logging
import threading
from pathlib import Path

from app.scan.awsutil import OK, Cancelled, safe_name
from app.scan.download import download, human
from app.scan.engine import S_OK, S_RUN, S_WARN, Ctx, call_logged, ensure, parallel
from app.logging_setup import LOGGER_NAME

log = logging.getLogger(LOGGER_NAME)

INDEX_TYPES = {"application/vnd.oci.image.index.v1+json",
               "application/vnd.docker.distribution.manifest.list.v2+json"}
MANIFEST_TYPES = {"application/vnd.oci.image.manifest.v1+json",
                  "application/vnd.docker.distribution.manifest.v2+json"}
ACCEPTED = sorted(INDEX_TYPES | MANIFEST_TYPES)


class _Repo:
    def __init__(self, ctx: Ctx, region: str, name: str, folder: Path):
        self.ctx, self.region, self.name, self.folder = ctx, region, name, folder
        self.blobs = folder / "blobs" / "sha256"
        self.lock = threading.Lock()
        self.pending: set[str] = set()
        self.bytes = 0

    def blob_path(self, digest: str) -> Path:
        return self.blobs / digest.split(":", 1)[1]

    def get_manifest(self, digest: str) -> tuple[str, bytes, str]:
        outcome, res = call_logged(self.ctx, self.region, "ecr", "batch_get_image", {
            "repositoryName": self.name, "imageIds": [{"imageDigest": digest}], "acceptedMediaTypes": ACCEPTED})
        imgs = (res or {}).get("images") or []
        if outcome != OK or not imgs:
            fail = (res or {}).get("failures") or [{}]
            raise RuntimeError(f"manifest {digest}: {outcome} {fail[0].get('failureReason', '')}")
        img = imgs[0]
        raw = img["imageManifest"].encode("utf-8")
        real = "sha256:" + hashlib.sha256(raw).hexdigest()
        if real != digest:
            # il layout OCI deve restare coerente: si salva sotto il digest dei byte avuti
            log.warning("ECR %s: manifest %s ricevuto con digest %s, salvato con quest'ultimo",
                        self.name, digest, real)
        mt = img.get("imageManifestMediaType") or json.loads(raw).get("mediaType", "")
        p = self.blob_path(real)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(raw)
        return mt, raw, real

    def collect(self, digest: str, seen: dict[str, tuple]) -> tuple[str, int, str]:
        """Scarica il manifest `digest` e i figli; ritorna (media type, dimensione, digest dei byte);
        accoda config e layer da scaricare."""
        if digest in seen:
            return seen[digest]
        mt, raw, real = self.get_manifest(digest)
        seen[digest] = (mt, len(raw), real)
        doc = json.loads(raw)
        if mt in INDEX_TYPES:
            for m in doc.get("manifests", []):
                self.collect(m["digest"], seen)
        elif mt in MANIFEST_TYPES:
            for d in [doc["config"]["digest"], *(ly["digest"] for ly in doc.get("layers", []))]:
                if not self.blob_path(d).exists():
                    with self.lock:
                        self.pending.add(d)
        else:
            raise RuntimeError(f"manifest {digest}: tipo non gestito {mt or '(schema v1?)'}")
        return mt, len(raw), real

    def fetch_blob(self, digest: str) -> int:
        outcome, res = call_logged(self.ctx, self.region, "ecr", "get_download_url_for_layer",
                                   {"repositoryName": self.name, "layerDigest": digest})
        if outcome != OK:
            raise RuntimeError(f"blob {digest}: {outcome}")
        n = download(res["downloadUrl"], self.blob_path(digest), sha256_hex=digest.split(":", 1)[1],
                     cancel=self.ctx.cancel)
        with self.lock:
            self.bytes += n
        return n


def _select(images: list[dict], cfg) -> list[dict]:
    imgs = [i for i in images if cfg.include_untagged or i.get("imageTags")]
    imgs.sort(key=lambda i: str(i.get("imagePushedAt") or ""), reverse=True)
    if cfg.max_images_per_repo > 0:
        imgs = imgs[:cfg.max_images_per_repo]
    return imgs


def run_ecr(ctx: Ctx) -> None:
    cfg = ctx.settings.ecr
    for region in ctx.regions:
        ctx.check()
        key = f"ecr:{region}"
        label = "Immagini ECR (OCI)"
        ctx.step(key, region, label, S_RUN, "", "")
        repos = [r for _, res in ensure(ctx, region, "ecr", "describe_repositories") for r in res.get("repositories", [])]
        if cfg.repositories:
            repos = [r for r in repos if r["repositoryName"] in cfg.repositories]
        tot_imgs, tot_bytes, problems = 0, 0, []
        for n, repo in enumerate(repos, 1):
            ctx.check()
            name = repo["repositoryName"]
            outcome, res = call_logged(ctx, region, "ecr", "describe_images", {"repositoryName": name})
            images = _select((res or {}).get("imageDetails", []), cfg)
            if not images:
                continue
            r = _Repo(ctx, region, name, ctx.out / "artifacts" / region / "ecr" / safe_name(name))
            entries, seen = [], {}
            for img in images:
                try:
                    mt, size, digest = r.collect(img["imageDigest"], seen)
                except Cancelled:
                    raise
                except Exception as exc:  # noqa: BLE001
                    problems.append(f"{name}@{img['imageDigest'][:19]}: {exc}")
                    log.error("ECR %s %s: %s", region, name, exc)
                    continue
                tags = img.get("imageTags") or [None]
                for t in tags:
                    e = {"mediaType": mt, "digest": digest, "size": size}
                    if t:
                        e["annotations"] = {"org.opencontainers.image.ref.name": t}
                    entries.append(e)
            res_blobs = parallel(ctx, r.fetch_blob, sorted(r.pending), f"ECR {name}", "ecr")
            problems += [f"{name}: {e}" for e in res_blobs if isinstance(e, Exception)]
            if not entries:
                continue
            (r.folder / "oci-layout").write_text('{"imageLayoutVersion": "1.0.0"}', encoding="utf-8")
            (r.folder / "index.json").write_text(json.dumps(
                {"schemaVersion": 2, "mediaType": "application/vnd.oci.image.index.v1+json", "manifests": entries},
                indent=2), encoding="utf-8")
            tot_imgs += len(images)
            tot_bytes += r.bytes
            log.info("ECR %s %s: %d immagini, %d blob nuovi, %s", region, name, len(images), len(r.pending),
                     human(r.bytes))
            ctx.step(key, region, label, S_RUN, f"{n}/{len(repos)} repository", f"{tot_imgs} immagini, {human(tot_bytes)}")
        status = S_WARN if problems else S_OK
        ctx.step(key, region, label, status, f"{len(repos)} repository, {tot_imgs} immagini, {human(tot_bytes)}",
                 f"{len(problems)} problemi (vedi log)" if problems else "")
        ctx.section(key, status, repository=len(repos), immagini=tot_imgs, byte=tot_bytes, problemi=problems[:50])
