# Tabella `seasons_periods`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/seasons.model.js`](../../back-pre/app/models/seasons.model.js), controller
> [`seasons.controller.js`](../../back-pre/app/controllers/seasons.controller.js) (letto per intero, 182
> righe), frontend `front-pre/src/components/seasonperiod/SeasonsPeriods.js` e i consumer del dato in fase
> di prenotazione (`reserveArticle/ReserveArticle.js`, `ReservationModal.js`). Il model Sequelize la
> definisce come `"seasons"` con `sequelize.define`, ma **la tabella fisica reale è `seasons_periods`**
> (verificato: nessuna tabella chiamata `seasons` esiste nel DB) — ulteriore conferma della pluralizzazione
> automatica di Sequelize già osservata su altre tabelle di questo dominio.
>
> **Nota di contesto**: come `sectors_rows`, è **una riga per `propertyId`** (stesso pattern
> find-one-poi-crea-o-aggiorna verificato in tutte e tre le funzioni del controller). La "stagione" vera e
> propria (alta/bassa/media, con le relative date) vive **dentro il JSON `periods`**, non come righe
> distinte.

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Relazioni**: nessuna FK reale. Referenziata applicativamente da `getLowestPrice`
  (`property.controller.js`, già documentato in [`properties.md`](properties.md)), che fa
  `seasons.findOne({ where: { propertyId } })` per calcolare il prezzo minimo mostrato in ricerca.
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### seasonFrom / seasonTo
- **Tipo**: `date`, `NOT NULL` (senza default)
- **Vincoli**: `NOT NULL` a livello DB — a differenza della quasi totalità degli altri campi di questo
  dominio, qui la colonna è dichiarata obbligatoria nello schema fisico
- **Relazioni**: —
- **Descrizione**: data di inizio/fine della stagione **corrente** dello stabilimento (l'intervallo entro
  cui ricadono tutti i periodi tariffari definiti in `periods`, vedi sotto). Letta anche da
  `property.controller.js:serchProperty` (mostrata nei risultati di ricerca pubblica come `seasonFrom`/
  `seasonTo` del bathouse, tramite l'`include` su questo model).
  **Nota di discrepanza verificata sul DB dev**: interrogando direttamente lo schema del DB dev, entrambe le
  colonne risultano `NULLABLE=YES`, in contrasto con quanto riportato sopra (`NOT NULL` a livello DB). Non è
  chiaro se si tratti di una migrazione applicata solo in alcuni ambienti o di una discrepanza fra la
  definizione nel model Sequelize e lo schema fisico realmente in uso — da confermare quale sia lo schema di
  riferimento corretto prima della riscrittura.
- **Nullabile**: No a livello di model Sequelize; **Sì** verificato sullo schema fisico del DB dev (vedi nota
  sopra)
- **Esempio di valori**: "2022-05-31", "2022-09-10"

### periods
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(periods)`,
  `NOT NULL`
- **Vincoli**: `CHECK json_valid(...)`, `NOT NULL`
- **Relazioni**: —
- **Descrizione**: array dei periodi tariffari in cui è suddivisa la stagione (es. "Bassa stagione",
  "Media stagione", "Alta stagione"), ciascuno con uno o più intervalli di date. Struttura verificata sui
  dati reali:
  - **name**: Tipo `string` — nome del periodo (es. `"Bassa stagione"`), usato come chiave di riferimento
    in altri punti del sistema — in particolare `property.controller.js:getLowestPrice` cerca, dentro
    `price_lists.detailPrices`, le voci il cui nome **contiene** (`LIKE '%periodName%'`) il nome di questo
    periodo, per calcolare il prezzo minimo del periodo corrente (vedi [`price_lists.md`](price_lists.md)).
  - **selectedRanges**: Tipo `array` di oggetti `{ startDate, endDate, key }` — uno o più intervalli di
    date che compongono il periodo (un periodo può quindi essere **non contiguo**, es. "bassa stagione" a
    inizio **e** fine estate nello stesso periodo, come osservato nel campione reale: due `selectedRanges`
    per "Bassa stagione", uno a fine aprile/inizio giugno e uno a fine agosto/settembre). `key` è un
    artefatto della libreria di selezione intervallo date usata nel frontend (`react-date-range`,
    `"selection1"`/`"selection2"`), non un dato di business.
- **Nullabile**: Sì (a livello di schema fisico, pur con `CHECK json_valid` e dichiarazione `NOT NULL` nel
  model)
- **Esempio di valori**: `[{"name":"Bassa stagione","selectedRanges":[{"startDate":"2022-04-25","endDate":"2022-06-01","key":"selection1"}]}]`; sotto-campo **name**: "Bassa stagione", "Media stagione", "Alta stagione"; sotto-campo
  **selectedRanges**: `[{"startDate":"2022-08-25","endDate":"2022-09-10","key":"selection2"}]`

### createdBy
- **Tipo**: `bigint`, `NOT NULL`
- **Vincoli**: nessuna FK
- **Relazioni**: applicativa verso `users.id`, non vincolata
- **Descrizione**: operatore che ha creato/modificato la configurazione (dichiarato dal client, come gli
  altri `createdBy` di questo dominio).
- **Nullabile**: Sì
- **Esempio di valori**: "340"

### propertyId
- **Tipo**: `bigint`, `NOT NULL`
- **Vincoli**: nessuna FK; indice dedicato
- **Relazioni**: applicativa verso `properties.id`
- **Descrizione**: stabilimento proprietario, chiave di lookup usata da tutte le funzioni del controller.
- **Nullabile**: No
- **Esempio di valori**: "110"

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL`, nessun default a livello DB
- **Descrizione**: standard.
- **Nullabile**: No
- **Esempio di valori**: "2024-03-12 09:41:07", "2024-03-12 09:41:07"

### nextSeasonFrom / nextSeasonTo
- **Tipo**: `date`, opzionali (`DEFAULT NULL`) — a differenza di `seasonFrom`/`seasonTo` non sono
  obbligatori
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: intervallo di date per il **rinnovo stagione successiva**, verificato nel frontend
  (`SeasonsPeriods.js:65,209-210`, dove nello stato React sono chiamati esplicitamente `renewStartDate`/
  `renewEndDate`) — cioè il gestore può pre-impostare i confini della stagione dell'anno successivo prima
  ancora di definirne il dettaglio periodi/prezzi. Consumati anche nel flusso di prenotazione
  (`reserveArticle/ReserveArticle.js:460-463`, `ReservationModal.js`).
  **Logica d'uso verificata nel codice**: in `seasons.controller.js` (righe 23-26, 49-52, 115-118, 156-159)
  questi campi sono trattati come semplici campi CRUD paralleli a `seasonFrom`/`seasonTo`, senza alcuna
  logica di branch nel controller stesso. Il vero utilizzo si trova nel frontend: in
  `front-pre/src/components/reserveArticle/ReserveArticle.js` (righe 444-468) vengono dispatchate le azioni
  Redux `SetSeasonalDates` (per la stagione corrente) e `setNextSeasonalDates` (per la stagione successiva);
  in `ReservationModal.js`, quando l'utente **rinnova** una prenotazione con `seasonalPass === 1` (pass
  stagionale — righe 1064-1065 di `updateRenewBookingDates`), le nuove date di prenotazione vengono
  impostate **direttamente** uguali a `nextSeasonFrom`/`nextSeasonTo` (righe 1069-1070); per il rinnovo di
  una prenotazione **non** stagionale, invece, le date vengono semplicemente shiftate di +1 anno (righe
  1074-1088), senza mai leggere questi due campi. In sintesi: `nextSeasonFrom`/`nextSeasonTo` servono
  esclusivamente al rinnovo automatico di prenotazioni con pass stagionale attivo e non hanno alcun ruolo
  nelle prenotazioni ordinarie.
- **Nullabile**: Sì (58 righe su 60 con valore `NULL`)
- **Esempio di valori**: `nextSeasonFrom`: "2027-05-17", "2025-02-28"; `nextSeasonTo`: "2027-09-29",
  "2025-05-30"

---

## Osservazioni verificate nel codice (criticità)

- **`updateseasonsperiods` non attende l'`update`** (`seasons.controller.js:154`, stesso bug già
  documentato per `updatesectors` in [`sectors_rows.md`](sectors_rows.md) e già segnalato a livello
  generale in
  [01-criticita-e-indicazioni-backend.md §5.1](../../docs/01-criticita-e-indicazioni-backend.md)).
- **IDOR verificato**: `addseasonsperiods`/`updateseasonsperiods` operano su `req.body.propertyId` senza
  alcuna verifica contro l'utente autenticato — stesso pattern sistemico del dominio C2.
- **`getLowestPrice` fa un matching per nome parziale (`LIKE`) fra `periods[].name` e le voci di
  `price_lists`**: un nome periodo cambiato leggermente (es. "Alta Stagione" con maiuscola diversa, o
  rinominato) rischia di rompere silenziosamente il calcolo del prezzo minimo mostrato in ricerca pubblica,
  senza alcun errore visibile — un accoppiamento fragile fra due tabelle collegate solo per stringa libera,
  da sostituire nella riscrittura con un riferimento tipizzato (FK o enum condiviso).
