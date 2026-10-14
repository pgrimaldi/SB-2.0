# Tabella `sectors_rows`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **175 righe**, query
> completa; `propertyId` `NULL` su 1 riga; ~175 stabilimenti hanno configurato i settori — numeri
> allineati a [`configure_maps.md`](configure_maps.md) 175). Model
> [`back-pre/app/models/sectors.model.js`](../../../../../../back-pre/app/models/sectors.model.js), controller
> [`sectors.controller.js`](../../../../../../back-pre/app/controllers/sectors.controller.js) (letto per intero, 140
> righe), frontend `front-pre/src/components/sectors/` (`AddRows.js`, `RowsList.js`, `EditRowDetails.js`,
> `SectorsRows.js`, `EditRows.js`, `SectorTableList.js`), confrontato con la copia in `front-customer-pre`.
>
> **Nota di contesto**: nonostante il nome faccia pensare a più righe per stabilimento, la tabella contiene
> **una sola riga per `propertyId`** (verificato: sia `addsectors` sia `updatesectors` operano sempre con
> `findOne({ where: { propertyId } })` seguito da create-o-update sulla singola riga trovata — mai una
> `INSERT` per ogni settore). "Sectors" (i singoli settori/file della spiaggia, es. "Fila 1", "Fila 2") sono
> gli **elementi dell'array JSON** dentro l'unico record, non righe distinte della tabella.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Relazioni**: nessuna FK reale; nessun'altra tabella referenzia esplicitamente questo `id` (i settori
  sono referenziati altrove **per nome**, non per id — vedi `onlineBookingOptions.locationOptionObj.bookingLimit[].sectorName`
  in [`properties.md`](properties.md) e i riferimenti a `"Fila N"` visti nelle altre tabelle di questo
  dominio)
- **Descrizione**: identificativo tecnico della riga di configurazione (una per stabilimento).
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### sectors
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(sectors)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Descrizione**: array dei settori/file in cui è divisa la spiaggia dello stabilimento. Struttura
  verificata sui dati reali — array di oggetti `{ name, currentId }`:
  - **name**: Tipo `string` — nome del settore mostrato in tutta l'app (es. `"Fila 1"`, `"Fila 6-6"`),
    usato come chiave di riferimento (non l'`id` della riga, né un id del singolo settore) in tutte le
    altre tabelle di questo dominio che citano un settore per nome (`onlineBookingOptions.locationOptionObj.bookingLimit`
    su `properties`, `selectedRowsData`/`sectorRowsPkg` su `price_lists`, righe della mappa spiaggia su
    `configurations.beachMapData`).
  - **currentId**: Tipo `number` (osservato come numero decimale, es. `94.16997666370636`) — **non un
    identificativo persistente con significato di business**: verificato in
    `front-pre/src/components/sectors/AddRows.js:26-29`, è generato lato client come
    `Math.random() * 99 + 1` (un numero casuale fra 1 e 100) al solo scopo di avere una chiave univoca
    temporanea per individuare la riga nella UI (modifica/cancellazione nella lista prima del salvataggio)
    — **non deve essere interpretato come un ordine, una priorità o un riferimento stabile**. Lo stesso
    pattern (`currentId` generato con `Math.random()` lato client, mai un contatore server-side) è stato
    riscontrato identico anche in `discounts.items`, `services_types.{bar,normal,hourly}Services` e
    `warehouses.items` — è una convenzione trasversale di questi form "lista dinamica" del gestionale, non
    specifica di questa tabella. **Conferma pratica dai dati reali del DB dev**: nel campo `sectors` di righe
    diverse coesistono entrambi i formati — alcune righe hanno `currentId` come intero "pulito" (es.
    `{"name":"Fila 1","currentId":1}`, quasi certamente dato seed o importato a mano) mentre altre hanno il
    tipico float casuale prodotto da `Math.random()` (es. `{"name":"row1","currentId":17.2645877682304}`).
    Questa coesistenza, nello stesso campo su righe diverse dello stesso ambiente, conferma sul campo che
    `currentId` non è mai stato un identificativo di business coerente: è solo una chiave UI temporanea, e i
    valori interi "puliti" osservati sono quasi certamente artefatti di inserimento manuale/seed, non un
    formato alternativo intenzionale.
- **Nullabile**: Sì
- **Esempio di valori**: `[{"name":"Fila 1","currentId":1},{"name":"Fila 2","currentId":2}]`; sotto-campo
  **name**: "Fila 1", "row1", "Fila 6-6"; sotto-campo **currentId**: 1, 17.2645877682304, 94.16997666370636

### createdBy
- **Tipo**: `int`
- **Vincoli**: nessuno (nessuna FK)
- **Relazioni**: applicativa verso `users.id`, non vincolata
- **Descrizione**: operatore che ha creato/modificato la configurazione (`req.body.createdBy`, dichiarato
  dal client come già visto per gli altri `createdBy` di questo dominio).
- **Nullabile**: Sì
- **Esempio di valori**: "164"

### propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK; indice dedicato (`sectors_rows_property_id`)
- **Relazioni**: applicativa verso `properties.id`
- **Descrizione**: stabilimento a cui appartiene la configurazione — chiave di lookup usata da tutte e tre
  le funzioni del controller.
- **Nullabile**: Sì
- **Esempio di valori**: "111"

### createdAt / updatedAt
- **Tipo**: `datetime`, nessun default DB
- **Descrizione**: standard.
- **Nullabile**: Sì
- **Esempio di valori**: "2024-02-19 11:03:22", "2024-02-19 11:03:22"

---

## Osservazioni verificate nel codice (criticità)

- **`updatesectors` non attende l'`update`** (`sectors.controller.js:119`, `Sectors.update(...)` senza
  `await`): il client riceve conferma di successo (`sectorsUpdated`) anche se la scrittura fallisce —
  criticità già segnalata a livello generale in
  [01-criticita-e-indicazioni-backend.md §5.1](../../progettazione/01-criticita-e-indicazioni-backend.md), qui
  riconfermata leggendo il file per intero.
- **IDOR verificato**: sia `addsectors` sia `updatesectors` operano su `req.body.propertyId` senza mai
  verificarlo contro l'utente autenticato — stesso pattern sistemico già documentato per l'intero dominio
  C2.
- **`viewsectors` e `updatesectors` sono ridondanti fra loro**: `updatesectors` (riga 105) fa lo stesso
  identico lavoro della seconda metà di `addsectors` (righe 36-60, il ramo "else" quando la riga esiste già)
  — tre funzioni per due operazioni concettuali (crea-o-aggiorna, leggi), con logica di update duplicata in
  due punti del file che potrebbero divergere in futuro come già accaduto altrove nel dominio (vedi
  `updateseasonsperiods` con lo stesso bug dell'`await` mancante, documentato in `seasons_periods.md`).
