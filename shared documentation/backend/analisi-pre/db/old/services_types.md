# Tabella `services_types`

> Fonti: schema e dati reali (88 righe), model
> [`back-pre/app/models/servicetypes.model.js`](../../back-pre/app/models/servicetypes.model.js)
> (`sequelize.define("services_types", ...)`), controller
> [`servicetypes.controller.js`](../../back-pre/app/controllers/servicetypes.controller.js) (letto per
> intero, 132 righe), frontend `front-pre/src/components/servicetype/` (form di gestione bar/normali/a
> ore) e consumer in fase di prenotazione (`reserveArticle/`, `serviceReservation/`).
>
> **Nota di contesto**: come `sectors_rows`/`seasons_periods`/`discounts`, è **una riga per `propertyId`**
> (pattern find-one-poi-crea-o-aggiorna confermato in `addservicetypes`). 88 righe su un totale di
> stabilimenti più ampio: non tutte le proprietà hanno configurato questa sezione.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: `1, 2, 3, ..` — verifica più recente sul DB di sviluppo: colonna fisica `bigint`
  (non `int` come nel model Sequelize — discrepanza minore fra tipo dichiarato e tipo reale, senza impatto
  pratico dato il range), `NOT NULL`, autoincrementale

### barServices
- **Tipo**: dichiarato `Sequelize.JSON`
- **Descrizione**: array dei servizi da bar acquistabili, struttura verificata sui dati reali —
  `{ ServiceName, Servicemage, ServicePrice, PurchaseStatus, currentId }` (il nome del campo icona,
  `Servicemage`, è scritto così anche nel codice sorgente — refuso storico, non un errore di trascrizione
  di questa scheda). **currentId**: stessa convenzione client-side `Math.random()`-based già documentata in
  [`sectors_rows.md`](sectors_rows.md#sectors) — non un identificativo di business. **Candidato a
  enumerazione**: `PurchaseStatus` è un intero 0/1 usato come booleano ("acquistabile o no") — nel nuovo
  schema andrebbe tipizzato come colonna `BOOLEAN`/`BIT` reale su una tabella relazionale dedicata, non come
  intero dentro un blob JSON; non è un vero ENUM multi-valore, solo un booleano mal tipizzato.
- **Nullabile**: No (colonna fisica `longtext NOT NULL`, verificato su `information_schema`)
- **Esempio di valori**: `[{"ServiceName":"Birra","Servicemage":"https://summerbooking.s3.us-east-2.amazonaws.com/images/service/birra.png","ServicePrice":"3.50","PurchaseStatus":1,"currentId":473829.15}]`

### normalServices
- **Tipo**: `Sequelize.JSON`
- **Descrizione**: stessa struttura di `barServices` — servizi "normali" (non a bar, non a ore) acquistabili
  a corredo della prenotazione (es. noleggio attrezzatura a giornata). **Candidato a enumerazione**: stessa
  osservazione su `PurchaseStatus` fatta per `barServices` sopra.
- **Nullabile**: No
- **Esempio di valori**: `[{"ServiceName":"Noleggio SUP","Servicemage":"https://.../sup.png","ServicePrice":"15","PurchaseStatus":1,"currentId":812043.9}]`

### hourlyServices
- **Tipo**: `Sequelize.JSON`
- **Descrizione**: struttura più ricca rispetto agli altri due array — oltre ai campi comuni
  (`ServiceName`, `Servicemage`, `ServicePrice`, `PurchaseStatus`, `currentId`), include:
  - **HourFrom / HourTo**: fascia oraria in cui il servizio è disponibile.
  - **day_constant**: array di 7 elementi `{ id, name, status, it }` — uno per giorno della settimana,
    permette di attivare/disattivare il servizio per singolo giorno.
  - **timeslot_constants**: array `{ id, name, status, price }` — fasce orarie interne con eventuale
    sovrapprezzo/prezzo dedicato per fascia (es. tariffa diversa per lo slot 9-11 rispetto a 15-17).

  **Candidato a enumerazione**: `day_constant[].status` e `timeslot_constants[].status` sono anch'essi
  interi/booleani a due valori (attivo/non attivo per giorno o fascia) — stessa raccomandazione di
  `PurchaseStatus` sopra (tipo booleano reale, non ENUM). `day_constant[].name` invece è un vocabolario
  davvero chiuso e stabile (i 7 giorni della settimana, mai destinato a crescere) — è il candidato più
  solido di questa tabella per un `ENUM` DB vero e proprio se il campo venisse normalizzato fuori dal JSON,
  oppure per un riferimento a una piccola tabella di lookup `days_of_week` se serve la traduzione già
  presente nel sotto-campo `it`.
- **Nullabile**: No
- **Esempio di valori**: `[{"ServiceName":"Campo da beach volley","ServicePrice":"10","PurchaseStatus":1,"HourFrom":"09:00","HourTo":"20:00","currentId":220157.3,"day_constant":[{"id":1,"name":"Monday","it":"Lunedì","status":1}, "...(altri 6 giorni)"],"timeslot_constants":[{"id":1,"name":"9-11","status":1,"price":"10"},{"id":2,"name":"15-17","status":1,"price":"12"}]}]`

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio.
- **Nullabile**: No (verifica più recente su `information_schema`: colonna fisica `bigint NOT NULL`, non
  `int` come nel model — stessa discrepanza minore già segnalata per `id`)
- **Esempio di valori**: `createdBy`: `90`; `propertyId`: `82`

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: No
- **Esempio di valori**: `2024-03-12 09:15:00`, `2025-11-02 17:40:33`

---

## Osservazioni verificate nel codice (criticità)

- **A differenza delle altre tabelle di questo dominio, qui l'`update` è correttamente atteso**:
  `addservicetypes` (righe 46-58) chiama `await Servicetypes.update(...)` — **verificato che l'`await` è
  presente**, quindi **non** è affetto dal bug del "successo falso" già documentato per `updatesectors`
  ([`sectors_rows.md`](sectors_rows.md)) e `updateseasonsperiods`
  ([`seasons_periods.md`](seasons_periods.md)). Segnalato esplicitamente per evitare di assumere per
  analogia che il bug sia sistemico su tutte le tabelle di questo dominio — non lo è.
- **IDOR verificato**: sia `addservicetypes` sia `viewservicetypes` operano su `req.body.propertyId` senza
  alcuna verifica contro l'utente autenticato — stesso pattern sistemico del dominio C2.
- **Cache Redis con chiave per singolo `propertyId`, non per tipo di servizio**: `viewservicetypes` mette
  in cache l'intero oggetto (`barServices`+`normalServices`+`hourlyServices` insieme) sotto
  `servicetypes:${propertyId}` — qualunque modifica anche a una sola delle tre liste invalida (tramite
  `clearGroupCache`) l'intera cache combinata, non solo la sezione modificata; comportamento coerente ma da
  tenere presente se nella riscrittura si separano le tre liste in risorse indipendenti.
- **Nessuna relazione tipizzata con `warehouses`/`services`**: sia `barServices`/`normalServices` sia gli
  articoli di `warehouses.items` rappresentano "articoli acquistabili a corredo", ma sono due strutture
  JSON completamente separate e non collegate fra loro nel codice esaminato — la distinzione di business
  esatta fra un "servizio" di questa tabella e un "articolo" di `warehouses` non è stata determinata con
  certezza in questa scheda (nessun controller li mette in relazione); da chiarire con il team prodotto
  prima della riscrittura, dato che nella UI entrambi appaiono come voci acquistabili nella medesima
  schermata di prenotazione.
