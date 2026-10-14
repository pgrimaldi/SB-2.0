'use strict';
// Mirrors folders of Markdown documents into Nuclino collections, as configured in the
// "mirrors" list of .github/nuclino-docs.json. Every sub-folder becomes a collection and every
// .md file an item titled with its file name; missing ones are created, nothing is ever deleted.
// Items are matched by title inside their parent: an item renamed in Nuclino is created again.
//
// Environment:
//   NUCLINO_API_KEY   API key (not needed with NUCLINO_DRY_RUN=1)
//   SYNC_BASE         commit of the last successful sync: only files changed since then are
//                     rewritten. Empty, unknown or SYNC_FULL=true -> every file is rewritten.
//   SYNC_FULL         "true" to rewrite every file
//   NUCLINO_DRY_RUN   "1" to run against an in-memory fake, printing what would happen
//   GITHUB_REPOSITORY owner/repo, used for links to repository files that are not mirrored
//   SYNC_BRANCH       branch used in those links (default: dev)

const { existsSync, readdirSync, readFileSync, statSync } = require('node:fs');
const { execFileSync } = require('node:child_process');
const { dirname, join, relative, resolve, sep } = require('node:path');

const API = 'https://api.nuclino.com/v0';
const ROOT = resolve('.');
const DRY = process.env.NUCLINO_DRY_RUN === '1';
const REPO = process.env.GITHUB_REPOSITORY || 'OWNER/REPO';
const BRANCH = process.env.SYNC_BRANCH || 'dev';
const CONFIG = '.github/nuclino-docs.json';
// The API allows 150 requests per minute per key: stay a little below it.
const MIN_INTERVAL_MS = 60000 / 140;

const toPosix = (p) => p.split(sep).join('/');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// ---------------------------------------------------------------- Nuclino API

let lastRequest = 0;

async function api(method, path, body) {
  for (let attempt = 1; ; attempt++) {
    const wait = lastRequest + MIN_INTERVAL_MS - Date.now();
    if (wait > 0) await sleep(wait);
    lastRequest = Date.now();

    const response = await fetch(`${API}${path}`, {
      method,
      headers: {
        Authorization: `Bearer ${process.env.NUCLINO_API_KEY}`,
        'Content-Type': 'application/json',
      },
      body: body === undefined ? undefined : JSON.stringify(body),
      redirect: 'error',
      signal: AbortSignal.timeout(30000),
    });
    if ((response.status === 429 || response.status >= 500) && attempt < 5) {
      const retryAfter = Number(response.headers.get('retry-after'));
      await sleep((retryAfter > 0 ? retryAfter : 2 ** attempt) * 1000);
      continue;
    }
    const json = await response.json().catch(() => null);
    if (!response.ok || json?.status !== 'success') {
      throw new Error(`${method} ${path}: HTTP ${response.status} ${json?.message ?? ''}`.trim());
    }
    return json.data;
  }
}

// Same calls, answered from memory: lets the whole flow run locally without a key.
function fakeApi(rootId) {
  const items = new Map([[rootId, { id: rootId, object: 'collection', title: '(root)', childIds: [] }]]);
  let next = 1;
  return async (method, path, body) => {
    const id = path.split('/')[2];
    if (method === 'GET') return items.get(id);
    if (method === 'POST') {
      const created = { id: `00000000-0000-4000-8000-${String(next++).padStart(12, '0')}`, ...body, childIds: [] };
      items.set(created.id, created);
      items.get(body.parentId).childIds.push(created.id);
      return created;
    }
    if (method === 'PUT') return Object.assign(items.get(id), body);
    throw new Error(`fake api: ${method} ${path}`);
  };
}

// ---------------------------------------------------------------- local tree

function localTree(base, excluded) {
  const walk = (dir) => {
    const node = { dir, folders: [], files: [] };
    for (const entry of readdirSync(dir, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
      const full = join(dir, entry.name);
      const rel = toPosix(relative(base, full));
      if (excluded.some((e) => rel === e || rel.startsWith(`${e}/`))) continue;
      if (entry.isDirectory()) {
        const child = walk(full);
        if (child.files.length || child.folders.length) node.folders.push(child);
      } else if (entry.name.toLowerCase().endsWith('.md')) {
        node.files.push(full);
      }
    }
    return node;
  };
  return walk(base);
}

// ---------------------------------------------------------------- content

const LINK = /(!?)\[((?:[^\][]|\[[^\]]*\])*)\]\((<[^>]+>|[^)\s]+)(\s+"[^"]*")?\)/g;
const REF_DEF = /^(\s*\[[^\]]+\]:\s+)(\S+)(.*)$/;

// Rewrites relative links: mirrored documents -> their Nuclino item, other repository files ->
// GitHub, anything outside the repository (local-only folders) -> plain label.
function render(text, file, idByPath) {
  const target = (raw) => {
    if (/^[a-z][a-z0-9+.-]*:/i.test(raw) || raw.startsWith('#')) return raw;
    const inner = raw.startsWith('<') ? raw.slice(1, -1) : raw;
    const [path, anchor] = inner.split('#');
    let full;
    try {
      full = resolve(dirname(file), decodeURIComponent(path));
    } catch {
      return null;
    }
    const id = idByPath.get(full);
    if (id) return `https://app.nuclino.com/t/b/${id}`;
    const rel = toPosix(relative(ROOT, full));
    if (rel.startsWith('..') || !existsSync(full)) return null;
    const kind = statSync(full).isDirectory() ? 'tree' : 'blob';
    return `https://github.com/${REPO}/${kind}/${BRANCH}/${encodeURI(rel)}${anchor ? `#${anchor}` : ''}`;
  };

  let fence = false;
  return text
    .replace(/\r\n/g, '\n')
    .split('\n')
    .map((line) => {
      if (/^\s*(```|~~~)/.test(line)) fence = !fence;
      if (fence || /^\s*(```|~~~)/.test(line)) return line;
      const def = REF_DEF.exec(line);
      if (def) {
        const t = target(def[2]);
        return t ? def[1] + t + def[3] : line;
      }
      return line.replace(LINK, (match, bang, label, raw, title = '') => {
        const t = target(raw);
        if (t === raw) return match;
        if (t) return `${bang}[${label}](${t}${title})`;
        return bang ? '' : label;
      });
    })
    .join('\n');
}

// ---------------------------------------------------------------- sync

function changedFiles(base) {
  if (process.env.SYNC_FULL === 'true' || !base) return null;
  try {
    const out = execFileSync('git', ['diff', '--name-only', '-z', base, 'HEAD'], { encoding: 'utf8' });
    const changed = out.split('\0').filter(Boolean);
    // A different configuration or renderer can change every document.
    if (changed.some((f) => f.startsWith('.github/'))) return null;
    return new Set(changed.map((f) => resolve(ROOT, f)));
  } catch {
    console.log(`Commit ${base} not found: rewriting every document.`);
    return null;
  }
}

async function mirror(spec, call, changed) {
  const base = resolve(ROOT, spec.path);
  if (!toPosix(relative(ROOT, base)) || relative(ROOT, base).startsWith('..') || !existsSync(base)) {
    throw new Error(`Mirror "${spec.path}": folder missing or outside the repository.`);
  }
  const tree = localTree(base, (spec.exclude ?? []).map((e) => e.replace(/\/+$/, '')));

  const root = await call('GET', `/items/${spec.collectionId}`);
  if (root?.object !== 'collection') throw new Error(`Mirror "${spec.path}": ${spec.collectionId} is not a collection.`);

  const idByPath = new Map();
  const created = new Set();
  let collections = 0;

  // Finds the child with this title, or creates it. Titles are compared ignoring case.
  async function ensure(parent, title, object, path) {
    if (!parent.children) {
      parent.children = new Map();
      for (const childId of parent.childIds ?? []) {
        const child = await call('GET', `/items/${childId}`);
        const key = child.title.toLowerCase();
        if (parent.children.has(key)) console.log(`::warning::Two items titled "${child.title}" in the same collection: using the first.`);
        else parent.children.set(key, child);
      }
    }
    let node = parent.children.get(title.toLowerCase());
    if (node && node.object !== object) throw new Error(`"${path}" exists in Nuclino as ${node.object}, expected ${object}.`);
    if (!node) {
      node = await call('POST', '/items', { parentId: parent.id, object, title });
      parent.children.set(title.toLowerCase(), node);
      created.add(path);
      console.log(`Created ${object} ${toPosix(relative(ROOT, path))}`);
    }
    return node;
  }

  async function build(local, collection) {
    for (const file of local.files) {
      const item = await ensure(collection, file.split(sep).pop().replace(/\.md$/i, ''), 'item', file);
      idByPath.set(file, item.id);
    }
    for (const folder of local.folders) {
      collections++;
      await build(folder, await ensure(collection, folder.dir.split(sep).pop(), 'collection', folder.dir));
    }
  }
  await build(tree, root);

  const failures = [];
  let written = 0;
  for (const [file, id] of idByPath) {
    if (changed && !changed.has(file) && !created.has(file)) continue;
    const rel = toPosix(relative(ROOT, file));
    const source = readFileSync(file, 'utf8');
    if (!source.trim()) {
      console.log(`::warning::Empty document skipped: ${rel}`);
      continue;
    }
    const content =
      `> Copia automatica di [\`${rel}\`](https://github.com/${REPO}/blob/${BRANCH}/${encodeURI(rel)}) ` +
      `(branch \`${BRANCH}\`): le modifiche fatte qui vengono sovrascritte.\n\n${render(source, file, idByPath)}`;
    try {
      await call('PUT', `/items/${id}`, { content });
      written++;
    } catch (error) {
      failures.push(`${rel}: ${error.message}`);
    }
  }
  console.log(
    `Mirror "${spec.path}": ${idByPath.size} documents in ${collections} collections, ` +
      `${created.size} items/collections created, ${written} documents written.`,
  );
  return { failures, idByPath };
}

async function main() {
  const { mirrors = [] } = JSON.parse(readFileSync(CONFIG, 'utf8'));
  if (!Array.isArray(mirrors)) throw new Error(`${CONFIG}: "mirrors" must be an array.`);
  if (!DRY && !process.env.NUCLINO_API_KEY) throw new Error('Missing repository secret NUCLINO_API_KEY.');

  const changed = changedFiles(process.env.SYNC_BASE);
  console.log(changed ? `Incremental sync since ${process.env.SYNC_BASE}.` : 'Full sync.');

  const failures = [];
  for (const spec of mirrors) {
    if (!spec.collectionId) {
      console.log(`::warning::Mirror "${spec.path}" has no collectionId: skipped.`);
      continue;
    }
    if (!/^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(spec.collectionId)) {
      throw new Error(`Mirror "${spec.path}": invalid collectionId.`);
    }
    const call = DRY ? fakeApi(spec.collectionId) : api;
    const result = await mirror(spec, call, changed);
    failures.push(...result.failures);
    if (DRY && process.env.NUCLINO_DRY_RUN_SHOW) {
      const file = resolve(ROOT, process.env.NUCLINO_DRY_RUN_SHOW);
      console.log(render(readFileSync(file, 'utf8'), file, result.idByPath));
    }
  }
  if (failures.length) {
    throw new Error(`${failures.length} documents not written:\n${failures.join('\n')}`);
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
