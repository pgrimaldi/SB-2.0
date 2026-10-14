# Tabella `country`

> Fonti: schema completo (`SHOW CREATE TABLE`) + **dati reali di produzione** (dump
> `sb_prod_dump_2026_09_08`, **11 righe**, query completa — contenuto **identico** a quello riportato
> sotto; in produzione **non** esiste la riga "test country" presente nel DB dev), model
> [`back-pre/app/models/country.model.js`](../../../../../../back-pre/app/models/country.model.js), controller
> [`country.controller.js`](../../../../../../back-pre/app/controllers/country.controller.js) (letto per intero, 38
> righe), frontend `front-pre/src/components/bathhouse/Page1BathHouse.js` (selezione nazione nel form di
> registrazione stabilimento) e `front-customer-pre` (`SearchDestination.js`), azione
> `getCountryListApi` in `managerActions.js:264-266`.
>
> **Nota di contesto**: tabella **globale/master**, solo 11 righe — elenco (non esaustivo) di nazioni
> selezionabili in alcuni form, non un elenco ISO completo dei paesi del mondo.

---

## Contenuto (query diretta — tutte le 11 righe esistenti)

| id | country_english | country_italian |
|----|------------------|------------------|
| 1 | Montenegro | Montenegro |
| 2 | Italy | Italia |
| 3 | Albania | Albania |
| 5 | Spain | Spagna |
| 6 | Portugal | Portogallo |
| 7 | Croatia | Croazia |
| 8 | Greece | Grecia |
| 9 | Bosnia-Erzegovina | Bosnia-Erzegovina |
| 11 | Turkey | Turchia |
| 13 | France | Francia |
| 14 | Slovenia | Slovenia |

*(tutte con `status = 1`; gli id mancanti — 4, 10, 12 — indicano righe cancellate in passato, la tabella
non ha soft-delete: le cancellazioni, se avvenute, sono state fisiche)*

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. (con buchi, es. mancano 4, 10, 12 — cancellazioni fisiche pregresse)

### country_english / country_italian
- **Tipo**: `varchar(255)`, `allowNull: true`
- **Descrizione**: nome della nazione in inglese/italiano.
- **Nullabile**: Sì (in produzione nessuna riga ha valori `NULL`)
- **Esempio di valori** (prod): `country_english` "Italy", "Spain", "Croatia", "Slovenia";
  `country_italian` "Italia", "Spagna", "Croazia", "Slovenia" — **in produzione tutte e 11 le righe sono
  nazioni reali** (nessun dato di test). Vedi la tabella "Contenuto" sopra per l'elenco completo.

### status
- **Tipo**: `int`, default `1`
- **Valori osservati**: sempre `1` su tutte le 11 righe (confermato in produzione).
- **Descrizione**: filtro di attivazione — `countryList` interroga esplicitamente `where: { status: 1 }`.
  **Candidato a enumerazione**: campo binario attivo/non attivo con vocabolario presumibilmente fisso a due
  soli valori (0/1); essendo di fatto un flag booleano e non un vocabolario destinato a crescere, un
  booleano/ENUM a livello DB è sufficiente, non serve una tabella di lookup separata.
- **Nullabile**: Sì
- **Esempio di valori**: 1 (unico valore osservato su tutte le righe)

### created_at / updated_at
- **Tipo**: `datetime`, `DEFAULT NULL`, popolati da Sequelize (`defaultValue: Sequelize.NOW` nel model,
  `timestamps: false` disattiva la gestione automatica standard e li tratta come colonne applicative
  esplicite) — **questi sono i timestamp realmente scritti e coerenti con la data di creazione di ogni
  riga** (tutte datate 03/06/2021).
- **Nullabile**: Sì
- **Esempio di valori**: "2021-06-03 10:15:00" (valore indicativo, tutte le righe datate 03/06/2021)

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL`, **nessun default a livello DB**
- **Valori osservati**: **`0000-00-00 00:00:00` su tutte le 11 righe**, senza eccezioni — confermato
  anche sul dump di produzione `sb_prod_dump_2026_09_08`.
- **Descrizione**: colonne **non dichiarate nel model** (che ha `timestamps: false` e usa solo
  `created_at`/`updated_at` snake_case) — sono un residuo dei timestamp automatici standard di Sequelize
  (che di norma userebbe proprio `createdAt`/`updatedAt`) da **prima** che il model fosse configurato con
  `freezeTableName: true, timestamps: false` e i campi snake_case espliciti. Il valore
  `0000-00-00 00:00:00` (una data "zero" tipica di MySQL per colonne `NOT NULL` senza default esplicito al
  momento della creazione della tabella) conferma che **non sono mai state scritte da nessun codice
  applicativo** — pure colonne morte, mai lette né scritte da `country.controller.js`. Caso concreto e
  verificato di disallineamento model/schema dovuto a evoluzione storica senza migrazioni versionate,
  coerente con quanto già segnalato a livello generale in
  [01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md).
- **Nullabile**: No (dichiarato `NOT NULL` a livello schema, ma di fatto sempre valorizzato con la data
  "zero" `0000-00-00 00:00:00`, mai una vera data)
- **Esempio di valori**: "0000-00-00 00:00:00" (unico valore osservato, su tutte le 11 righe)
- **Deprecato**: Sì — colonne doppione morte rispetto a `created_at`/`updated_at` (snake_case), mai lette né
  scritte da alcun codice applicativo verificato; da rimuovere in fase di riscrittura mantenendo solo la
  coppia snake_case realmente popolata.

---

## Osservazioni verificate nel codice (criticità)

- **Elenco nazioni parziale e mantenuto manualmente**: solo 11 nazioni (in gran parte affacciate
  sull'Adriatico/Mediterraneo, coerente con il mercato di riferimento dell'app), gestibili solo tramite
  `createCountry` — nessun form di amministrazione individuato in nessuno dei quattro frontend che lo
  richiami; l'endpoint esiste ma è verosimilmente popolato solo manualmente/via script.
- **Doppia coppia di colonne timestamp, una delle quali completamente morta** (`createdAt`/`updatedAt`
  sempre a `0000-00-00 00:00:00`) — segnalato sopra, da rimuovere nella riscrittura mantenendo solo
  `created_at`/`updated_at` (gli unici realmente popolati e coerenti).
- **Nessuna relazione tipizzata con `properties.country`/`customers`**: come già documentato in
  [`properties.md`](properties.md), il campo `properties.country` è una stringa libera scelta dal form,
  senza FK verso questa tabella — un valore selezionato qui non è garantito rimanere coerente se la lista
  master cambia in futuro (stesso pattern di accoppiamento debole per stringa già visto ripetutamente in
  questo dominio).
