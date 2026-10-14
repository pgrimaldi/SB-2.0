# Tabella `configure_maps`

> Fonti: schema completo (`SHOW CREATE TABLE`) e dati reali (100 righe), model
> [`back-pre/app/models/confimaps.model.js`](../../back-pre/app/models/confimaps.model.js)
> (`sequelize.define("configure_maps", ...)`, il nome del modello JS è invece `Confimaps`/`confimaps`),
> controller [`confimaps.controller.js`](../../back-pre/app/controllers/confimaps.controller.js) (letto per
> intero, 439 righe — già citato in
> [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md) per la logica
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
- **Esempio di valori**: 1, 2, 3, ..

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
- **Nullabile**: Sì (64 righe `NULL` su 68)
- **Esempio di valori**: "#efd7cb", "#ffe791", "#d3a253", "#77daee"

### isCustomTemplate
- **Tipo**: `tinyint(1)`, default `0`
- **Descrizione**: `true` se il template è un'immagine caricata dal gestore invece di uno predefinito.
- **Nullabile**: Sì
- **Esempio di valori**: 0 (51 righe), 1 (17 righe)

### rows
- **Tipo**: `int`
- **Descrizione**: numero di righe della griglia spiaggia.
- **Nullabile**: Sì
- **Esempio di valori**: es. 5, 8, 10

### items
- **Tipo**: `int` — **campo numerico, non un array/JSON** (verificato sia nel model sia con
  `SHOW CREATE TABLE`, nonostante il nome suggerisca altrimenti)
- **Descrizione**: numero di **colonne** per riga della griglia — confermato in
  `confimaps.controller.js:addconfimaps` (`const column = newItems`, righe 191-236) e in `resetBeachMap`
  (righe 403-438, che azzera `{ rows: 0, items: 0 }` insieme a svuotare `configurations.beachMapData`).
- **Nullabile**: Sì
- **Esempio di valori**: es. 4, 6, 8 (numero di colonne, nonostante il nome del campo)

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
- **Nullabile**: Sì per tutti e 10 i campi
- **Esempio di valori**: per lo più `NULL`; URL S3/CloudFront quando presenti (es.
  "https://d26kxhigi9o4u2.cloudfront.net/confimages/special_umbrella1.png")

### specialCabin
- **Tipo**: `varchar(255)`
- **Descrizione**: icona per l'elemento "cabina speciale" (`SpecialCabin`). A differenza degli ombrelloni,
  **non ha una variante `_free` separata** — asimmetria nello schema non spiegata da alcun commento nel
  codice, da chiarire se intenzionale prima della riscrittura.
  **Confermato dai dati reali**: il campo è `NULL` su **tutte** le 68 righe attuali — nessuna
  configurazione utilizza ancora questo elemento, coerente (ma non risolutivo da solo) con l'asimmetria
  segnalata sopra.
- **Nullabile**: Sì
- **Esempio di valori**: nessun valore osservato — sempre `NULL` (68 righe su 68)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: `createdBy`: es. "12"; `propertyId`: es. "2689"

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: "2023-05-12 10:15:00", "2024-11-03 09:42:17"

---

## Osservazioni verificate nel codice (criticità)

- **Bug di ridimensionamento condiviso con `configurations.beachMapData`**: già documentato in
  [`configurations.md`](configurations.md#osservazioni-verificate-nel-codice-criticità) — ridurre `rows`/
  `items` da questa tabella tronca silenziosamente la griglia companion, con perdita dei dati delle celle
  rimosse.
- **Asimmetria `specialCabin` senza variante `_free`** rispetto a tutti gli ombrelloni speciali (che hanno
  sia la variante occupata sia quella libera) — segnalata sopra, verosimilmente un'omissione storica.
- **Nessuna relazione tipizzata con `configure_images`**: le icone (`templateImg`, `umbrellaImg`,
  `specialUmbrella*`) sono salvate come URL stringa copiato dalla riga scelta in `configure_images`, non
  come riferimento (id) a quella riga — se l'immagine master venisse aggiornata/sostituita in
  `configure_images`, le mappe già configurate **non** rifletterebbero il cambiamento (continuano a puntare
  al vecchio URL), un comportamento che può essere voluto (snapshot al momento della scelta) ma che non è
  stato possibile confermare come intenzionale nel codice esaminato.
- **IDOR verificato**: `addconfimaps`/`viewconfimaps`/`resetBeachMap` operano su `req.body.propertyId`
  senza alcuna verifica contro l'utente autenticato — stesso pattern sistemico del dominio C2.
