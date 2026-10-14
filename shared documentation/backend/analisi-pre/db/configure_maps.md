# Tabella `configure_maps`

> Fonti: schema completo (`SHOW CREATE TABLE`) + **dati reali di produzione** (dump
> `sb_prod_dump_2026_09_08`, **175 righe**, query completa; `id` 4…182 → poche cancellazioni). ~175
> stabilimenti hanno una mappa spiaggia configurata (numeri allineati a
> [`configurations.md`](configurations.md) 173, [`sectors_rows.md`](sectors_rows.md) 175). Model
> [`back-pre/app/models/confimaps.model.js`](../../../../../../back-pre/app/models/confimaps.model.js)
> (`sequelize.define("configure_maps", ...)`, il nome del modello JS è invece `Confimaps`/`confimaps`),
> controller [`confimaps.controller.js`](../../../../../../back-pre/app/controllers/confimaps.controller.js) (letto per
> intero, 439 righe — già citato in
> [01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md) per la logica
> di ridimensionamento griglia), frontend `front-pre/src/components/configuremap/EditTemplate.js`,
> `EditGrid.js`.
>
> **Nota di contesto**: tabella **companion 1:1** di [`configurations.md`](configurations.md) sullo stesso
> `propertyId` — contiene il **layout/template grafico** della mappa spiaggia (dimensioni griglia, template
> immagine, icone scelte per ciascun tipo di elemento), mentre `configurations.beachMapData` contiene lo
> stato cella-per-cella effettivo. Le due tabelle sono aggiornate insieme dalla stessa funzione
> `addconfimaps`, ma **non hanno alcuna FK fra loro** — sono collegate solo tramite lo stesso `propertyId`.

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `4` … `182` per 175 righe.

### templateImg
- **Tipo**: `varchar(255)`
- **Descrizione**: URL dell'immagine di sfondo della griglia. Se `isCustomTemplate = true`, il gestore
  carica un'immagine personalizzata (`UploadBase64OnS3`, poi riscritta con URL CloudFront in
  `addconfimages`-style upload — vedi anche [`configure_images.md`](configure_images.md) per lo stesso
  pattern S3→CloudFront); altrimenti si sceglie fra i template predefiniti presenti in
  `configure_images` (`category = "template"`).
- **Nullabile**: Sì
- **Esempio di valori**: URL S3/CloudFront di un template, es.
  "https://d26kxhigi9o4u2.cloudfront.net/confimages/template1.png"

### colorCode
- **Tipo**: `varchar(255)`
- **Descrizione**: colore associato alla mappa (probabile colore di sfondo/tema della griglia).
  **Verificato lato frontend**: `EditTemplate.js` usa un `<input type="color">` HTML nativo — un
  color-picker a selezione libera, **non** una palette fissa predefinita. Di conseguenza, nonostante la
  bassa cardinalità apparente nel campione osservato, questo campo **non** è un buon candidato a
  ENUM/FK verso una tabella colori: resta correttamente un hex libero, validabile solo nel formato
  (`#RRGGBB`).
- **Nullabile**: Sì. **In produzione è valorizzato più spesso di quanto suggerisse il campione dev**:
  `NULL` solo su **42 righe su 175** (24%). I valori sono hex `#RRGGBB`, quasi tutti tonalità
  sabbia/beige (`#e2ceb4` 40 righe, `#efd7cb` 27, `#f0d8cc` 23, `#f1d9cd` 12) con qualche verde
  (`#8bbc6a`, `#729e5e`).
- **Esempio di valori** (prod): "#e2ceb4", "#efd7cb", "#f0d8cc", "#8bbc6a".

### isCustomTemplate
- **Tipo**: `tinyint(1)`, default `0`
- **Descrizione**: `true` se il template è un'immagine caricata dal gestore invece di uno predefinito.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `1` (91 righe), `0` (84) → la **maggioranza** degli stabilimenti usa
  un template personalizzato (il campione dev, 17/68, lo sottostimava).

### rows
- **Tipo**: `int`
- **Descrizione**: numero di righe della griglia spiaggia.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `4`, `5`, `6`, `22` — range `0` … `40`.

### items
- **Tipo**: `int` — **campo numerico, non un array/JSON** (verificato sia nel model sia con
  `SHOW CREATE TABLE`, nonostante il nome suggerisca altrimenti)
- **Descrizione**: numero di **colonne** per riga della griglia — confermato in
  `confimaps.controller.js:addconfimaps` (`const column = newItems`, righe 191-236) e in `resetBeachMap`
  (righe 403-438, che azzera `{ rows: 0, items: 0 }` insieme a svuotare `configurations.beachMapData`).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `4`, `7`, `10`, `12` — range `0` … `77` colonne.

### umbrellaImg / umbrellaImage_free
- **Tipo**: `varchar(255)`
- **Descrizione**: icone scelte per l'elemento "ombrellone" nei due stati — occupato (`umbrellaImg`) e
  libero (`umbrellaImage_free`) — coerente con la distinzione `type: occupied|free` di
  [`configure_images.md`](configure_images.md), da cui queste icone vengono scelte.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: URL S3/CloudFront quando presenti (es.
  "https://d26kxhigi9o4u2.cloudfront.net/confimages/umbrella_occupied.png"), altrimenti `NULL`

### specialUmbrella1..5 / specialUmbrella1_free..5_free
- **Tipo**: `varchar(255)`, default `NULL`
- **Descrizione**: stessa logica di `umbrellaImg`/`umbrellaImage_free` ma per le 5 varianti "speciali" di
  ombrellone (corrispondenti a `SpecialUmbrella1`..`SpecialUmbrella5` negli `items.name` di
  `configurations.beachMapData`, vedi [`configurations.md`](configurations.md)).
- **Nullabile**: Sì per tutti e 10 i campi. **In produzione**: `specialUmbrella1`/`2` valorizzati su
  ~85% delle righe (`NULL`/`''` su ~29), `specialUmbrella3`/`4`/`5` su ~65% (`NULL` su ~60); il valore
  ricorrente è il placeholder `.../configure/Umbrella/umbrella-empty.png` per le varianti `_free` non
  personalizzate.
- **Esempio di valori** (prod): URL CloudFront `.../images/configure/<hash>.png`, oppure `''` (stringa
  vuota), oppure il placeholder `umbrella-empty.png`.

### specialCabin
- **Tipo**: `varchar(255)`
- **Descrizione**: icona per l'elemento "cabina speciale" (`SpecialCabin`). A differenza degli ombrelloni,
  **non ha una variante `_free` separata** — asimmetria nello schema non spiegata da alcun commento nel
  codice, da chiarire se intenzionale prima della riscrittura.
- **Nullabile**: Sì
- **Produzione**: **`NULL` su tutte le 175 righe** (`sb_prod_dump_2026_09_08`) — l'elemento "cabina
  speciale" non è mai stato configurato da nessuno stabilimento. Candidato alla rimozione.

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio. `propertyId` `NULL` su 1 riga.
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): `createdBy` `2`, `85`, `90` (range `2` … `6100`); `propertyId` `1`,
  `32`, `35`, `85` (range `1` … `10856`).

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): `createdAt` `2021-03-15 04:30:10` … `2026-08-01 09:05:23`; `updatedAt`
  fino a `2026-08-22 07:16:24`.

---

## Osservazioni verificate nel codice (criticità)

- **Bug di ridimensionamento condiviso con `configurations.beachMapData`**: già documentato in
  [`configurations.md`](configurations.md#osservazioni-verificate-nel-codice-criticità) — ridurre `rows`/
  `items` da questa tabella tronca silenziosamente la griglia companion, con perdita dei dati delle celle
  rimosse.
- **Asimmetria `specialCabin` senza variante `_free`** rispetto a tutti gli ombrelloni speciali (che hanno
  sia la variante occupata sia quella libera) — segnalata sopra, verosimilmente un'omissione storica.
  **In produzione `specialCabin` è `NULL` su tutte le 175 righe** → colonna morta, da rimuovere.
- **Nessuna relazione tipizzata con `configure_images`**: le icone (`templateImg`, `umbrellaImg`,
  `specialUmbrella*`) sono salvate come URL stringa copiato dalla riga scelta in `configure_images`, non
  come riferimento (id) a quella riga — se l'immagine master venisse aggiornata/sostituita in
  `configure_images`, le mappe già configurate **non** rifletterebbero il cambiamento (continuano a puntare
  al vecchio URL), un comportamento che può essere voluto (snapshot al momento della scelta) ma che non è
  stato possibile confermare come intenzionale nel codice esaminato.
- **IDOR verificato**: `addconfimaps`/`viewconfimaps`/`resetBeachMap` operano su `req.body.propertyId`
  senza alcuna verifica contro l'utente autenticato — stesso pattern sistemico del dominio C2.
