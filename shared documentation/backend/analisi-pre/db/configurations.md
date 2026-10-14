# Tabella `configurations`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **173 righe**, query
> completa; `id` 1…240 → ~67 righe cancellate nel tempo), model
> [`back-pre/app/models/configuration.model.js`](../../../../../../back-pre/app/models/configuration.model.js), controller
> [`configuration.controller.js`](../../../../../../back-pre/app/controllers/configuration.controller.js) (letto per
> intero, 391 righe), controller collegato
> [`confimaps.controller.js`](../../../../../../back-pre/app/controllers/confimaps.controller.js) (letto per intero,
> 439 righe — modifica `configurations.beachMapData` da un file diverso, vedi
> [`configure_maps.md`](configure_maps.md)), frontend `front-pre/src/components/configuremap/`
> (`EditTemplate.js`, `EditGrid.js`).
>
> **Nota di contesto**: come le altre tabelle "1 riga per proprietà" di questo dominio, contiene la mappa
> della spiaggia (griglia di celle) dello stabilimento. Il **layout grafico** (template, dimensioni
> griglia, icone scelte) vive in `configure_maps` (tabella companion, 1:1 sullo stesso `propertyId`);
> questa tabella contiene invece lo **stato cella-per-cella** effettivo della griglia.
>
> **Dato di produzione rilevante**: solo **173 righe** — cioè ~173 stabilimenti hanno una mappa
> spiaggia configurata (numeri coerenti con `configure_maps` 175, `sectors_rows` 175, `seasons_periods`
> 171, e con le ~180 vetrine `bathouses` realmente compilate). Il totale di ~10 mila `properties`
> **non** corrisponde al numero di stabilimenti realmente operativi: il perimetro reale della
> migrazione, per il dominio Catalogo/Mappa (C2), è **dell'ordine di 175 stabilimenti**, non 10 mila.

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1`, `49`, `60`, `62` — range `1` … `240` per 173 righe.

### beachMapData
- **Tipo**: dichiarato `Sequelize.JSON`
- **Descrizione**: array delle righe della griglia spiaggia. Struttura verificata sui dati reali e
  incrociata con la logica di ridimensionamento in `confimaps.controller.js:addconfimaps` (righe 159-288):
  - ogni elemento è `{ row, rec: [...] }`, dove `rec` è l'array delle celle della riga;
  - ogni cella (`rec[]`) ha: `cell`/`id` (coordinata testuale `"R{riga}C{colonna}"`), `gray_image`
    (icona placeholder cella vuota), `name`, `isReserved`, `isSelected`, `currentRow`, e — quando la cella
    contiene un elemento posizionato — un oggetto `items` con `{ id, name, it, itemIcon, emptyItemImg,
    reservedItemImg, bookedItemImg }`.
  - **`items.name`** identifica il **tipo di elemento** posizionato nella cella: verificato in
    `checkAvailability` (`configuration.controller.js:239-257`), i valori riconosciuti dal backend sono
    `Umbrella`, `Sunbed`, `Cabin`, `Parking`, `Tent`, `SpecialCabin`, `SpecialUmbrella1`..`SpecialUmbrella5`,
    `XLSunbed`, `UmbrellaArea`, `CabinArea`, `SundbedArea`, `ParkingArea`, `Ticket` — un elenco chiuso
    definito come stringa letterale nel backend (`itmNames`, righe 239-257), non come enum/lookup a DB.
- **Ridimensionamento griglia**: quando il gestore aumenta righe/colonne da `configure_maps`
  (`rows`/`items`), `confimaps.controller.js:addconfimaps` **rigenera** `beachMapData` aggiungendo righe/
  colonne vuote con id ricalcolati; quando le diminuisce, tronca l'array (`slice`) e **ricalcola gli id di
  tutte le celle rimanenti** — un'operazione potenzialmente distruttiva se righe/colonne contenenti
  elementi già posizionati vengono rimosse (i dati della cella troncata vengono persi, senza conferma
  esplicita richiesta all'utente lato backend — la protezione, se esiste, è solo lato UI).
- **Nullabile**: Sì (1 riga `[]` vuota su 173 in produzione)
- **Formato eterogeneo (prod)**: 21 forme JSON distinte fra le 173 righe — chiavi cella diverse a
  seconda dell'epoca (`cell`+`id` vs solo `id`, `empty_item_image` con URL CloudFront vs percorso
  relativo `../../assets/...`, presenza/assenza di `incrementId`, `areaItemCount`, `selectedOrder`).
  `maxlen` osservata **~670 KB per riga**. La migrazione deve gestire tutte le varianti.
- **Esempio di valori**: struttura array, es. `[{"row": 1, "rec": [{"cell": "R1C1", "id": "R1C1",
  "gray_image": "empty.png", "name": "", "isReserved": false, "isSelected": false, "currentRow": 1,
  "items": {"id": 12, "name": "Umbrella", "it": "Ombrellone", "itemIcon": "umbrella.png", "emptyItemImg":
  "umbrella_empty.png", "reservedItemImg": "umbrella_reserved.png", "bookedItemImg": "umbrella_booked.png"}
  }]}]` — il sotto-campo più rilevante è `items.name` (tipo di elemento posizionato, es. "Umbrella",
  "Sunbed", "Cabin")

### sharing_enable / sharing_seasonal_workers / credit_perday_resold / credit_value / disable_both_options /
### enable_sharing_on_all_article / plan_absence_as_sharing
- **Tipo**: `int`/`boolean`/`varchar` a seconda del campo (vedi model)
- **Descrizione**: gruppo di impostazioni della funzionalità di **"sharing"** (rivendita/condivisione di
  posti spiaggia fra clienti, verosimilmente per assenze programmate) — il nome dei campi e i valori
  ammessi sono coerenti con questa interpretazione (`credit_perday_resold`, `credit_value`,
  `plan_absence_as_sharing`), ma il flusso applicativo completo di come un cliente rivende/condivide il
  proprio posto **non è stato tracciato in questa scheda** (richiederebbe l'analisi del modulo booking/
  sharing lato prenotazione, fuori perimetro delle tabelle di catalogo qui documentate) — i campi sono letti
  e scritti integralmente da `addconfiguration`/`viewconfiguration` senza ulteriore logica di validazione
  nel controller stesso.
- **Nullabile**: Sì per tutti e 7 i campi
- **Distribuzione reale di produzione** (173 righe):
  - `sharing_enable`: `0` (160), `1` (13) → ~13 stabilimenti usano la funzione sharing.
  - `sharing_seasonal_workers`: `0` (166), `1` (7).
  - `credit_perday_resold` (`int`): `0` (148), `1` (20), `2` (1), `30` (1), `320` (1); `NULL` su 2.
  - `credit_value` (`varchar`): **campo testo libero molto sporco** — `''` (125), `'0'` (32), poi
    `'5€'` (2), `'5 €'` (1), `'10 €'` (1), `'7 €'` (1), `'4 €'` (1), `'5 Euro'` (1), `'5%'` (1),
    `'30% del giornaliero'` (1), `'100'` (1), `'140'` (1), `'15'` (1), `'9'` (1), `'1'` (2). Da
    scomporre in valore numerico + tipo (importo / percentuale) in migrazione.
  - `disable_both_options`: `0` (170), **`1` (3)** — non "sempre 0" come nel campione dev.
  - `enable_sharing_on_all_article`: `0` (170), `1` (3).
  - `plan_absence_as_sharing`: `0` (171), **`1` (2)** — non "sempre 0" come nel campione dev.

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio. `propertyId` `NULL` su 1 riga.
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): `createdBy` `76`, `2`, `94` (range `2` … `6100`); `propertyId` `15`,
  `1`, `33`, `59` (range `1` … `10856`).

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): `createdAt` `2021-03-26 07:29:23` … `2026-08-01 09:24:10`; `updatedAt`
  fino a `2026-09-02 08:09:43`.

---

## Osservazioni verificate nel codice (criticità)

- **A differenza di altre tabelle di questo dominio, qui l'`update` è correttamente atteso**:
  `addconfiguration` (riga 49 di `configuration.controller.js`) chiama
  `await Configuration.update(...)` — **verificato che l'`await` è presente**, quindi **non** è affetto
  dal bug del "successo falso" già documentato per `updatesectors`
  ([`sectors_rows.md`](sectors_rows.md)) e `updateseasonsperiods`
  ([`seasons_periods.md`](seasons_periods.md)). Segnalato esplicitamente per evitare di assumere per
  analogia che il bug sia sistemico su tutte le tabelle di questo dominio — non lo è (vedi anche
  [`services_types.md`](services_types.md) per lo stesso chiarimento).
- **`confimaps.controller.js:addconfimaps` attende correttamente** anch'esso i propri `Confimaps.update`
  e `Configuration.update` (righe 131, 248, 278) — segnalato per chiarezza, dato che il bug del punto
  precedente non è generalizzabile a tutte le scritture di questo dominio (come già notato per
  `services_types`, vedi [`services_types.md`](services_types.md)).
- **Doppia valutazione di `JSON.parse` nello stesso controllo** (`confimaps.controller.js:172-174`):
  `JSON.parse(configuration.beachMapData) ? JSON.parse(configuration.beachMapData) : ""` — il parsing
  viene eseguito due volte per lo stesso valore invece di essere memorizzato in una variabile; non causa
  corruzione dei dati (a differenza del pattern di doppia-serializzazione visto in
  [`discounts.md`](discounts.md)), ma è un doppio lavoro inutile e un potenziale punto di comportamento
  divergente se il valore cambiasse fra le due valutazioni (non applicabile in pratica in questo contesto
  sincrono, ma un anti-pattern da non riprodurre) — già annotato come nota storica nel codice stesso
  ("added if beachMapData is null on 01-07-2022").
- **Ridimensionamento griglia distruttivo**: come segnalato sopra, ridurre righe/colonne tronca
  silenziosamente `beachMapData` lato server, senza validare se le celle rimosse contenevano elementi
  configurati — un punto da rivedere nella riscrittura (conferma esplicita o migrazione dei dati invece di
  troncamento diretto).
- **IDOR verificato**: `addconfiguration`/`viewconfiguration`/`checkAvailability` operano su
  `req.body.propertyId` senza alcuna verifica contro l'utente autenticato — stesso pattern sistemico del
  dominio C2.
- **Elenco tipi-elemento (`items.name`) hardcoded lato backend come stringhe letterali**, non un
  enum/lookup a DB — rinominare o aggiungere un tipo di elemento richiede una modifica di codice sia lato
  backend (`itmNames`) sia lato frontend, senza alcuna fonte di verità condivisa; da modellare con un vero
  enum/tabella di riferimento nella riscrittura.
- **`credit_value` è testo libero non normalizzato** (prod: `'5€'`, `'5 €'`, `'5 Euro'`, `'5%'`,
  `'30% del giornaliero'` …): mescola importi e percentuali, con o senza simbolo di valuta. Nel target:
  colonna numerica + enum `credit_type { amount, percentage }`.
- **`disable_both_options` e `plan_absence_as_sharing` non sono costanti**: in produzione 2-3 righe
  hanno valore `1` (il campione dev diceva "sempre 0") — le opzioni sono effettivamente usate, seppur
  da pochissimi stabilimenti.
- **Perimetro reale ~173 stabilimenti**: vedi nota di contesto — le stime di migrazione del dominio C2
  vanno fatte su ~175 righe, non sul conteggio di `properties`.
