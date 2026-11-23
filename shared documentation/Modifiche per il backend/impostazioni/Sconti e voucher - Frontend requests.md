# Sconti e voucher - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Sconti e voucher** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/vouchers`: una tabella con gli sconti e i voucher dello stabilimento (cestino, duplica e matita per
riga, cancella e duplica selezionati), con ordinamento e pagine, e due popup, uno per aggiungere e uno per modificare.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/discounts/voucher-list` | Tabella degli sconti |
| 2 | POST | `/api/catalog/discounts/delete-voucher-item` | Elimina uno o più sconti, tutti o nessuno |
| 3 | POST | `/api/catalog/discounts/duplicate-voucher-item` | Duplica uno o più sconti, tutti o nessuno |
| 4 | POST | `/api/catalog/discounts/add-voucher-item` | Crea uno sconto |
| 5 | GET | `/api/catalog/discounts/get-selected-item` | Legge uno sconto per il popup di modifica |
| 6 | POST | `/api/catalog/discounts/update-voucher-item` | Salva uno sconto modificato |
| 7 | GET + POST | Le API 5 e 6 | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

Sono tutte **nuove**, nel gruppo `/discounts` che il backend ha già (`Module.Catalog/Endpoints/DiscountEndpoints.cs:18`). I nomi
delle rotte non si confondono con `/{id:guid}`, perché non sono UUID. Per il frontend sostituiscono le API di
`/api/catalog/discounts`, che lavorano con `SaveDiscountRequest` e `DiscountDto`.

## Regole comuni

- **Le API esistenti non cambiano**: `GET`, `POST`, `PUT` e `DELETE /api/catalog/discounts` e `bulk-delete` tengono rotte, richieste e
  risposte di oggi; le nuove si aggiungono e possono riusarne la logica (`DiscountsService`). Le sole differenze sono i giorni e le
  fasce orarie di ogni periodo ("Regole comuni dei popup", "API di oggi") e le lunghezze di codice e nome ("Lunghezze").
- **Stabilimento dal token** (`CurrentProperty.IdAsync`): nessuna richiesta lo contiene.
- **Permessi**: come le API degli sconti di oggi (`DiscountEndpoints.cs:16-17`): `CatalogRead` per la tabella e la lettura,
  `CatalogPricing` per il resto. Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Date: giorni in UTC ISO 8601 (`"2026-08-12T00:00:00.000Z"`). Ore:
  `"HH:mm"`. Enum: i nomi .NET (`Percentage`, `Fixed`).
- **Errori** nel formato Problem Details del backend, con `code`; gli errori di un campo in `errors[]` con i nomi del corpo di questo
  documento (es. `validityPeriods[0].validTo`). Senza token valido: 401 `auth.unauthenticated`.
- **Sconto che non c'è**: 404 `pricing.discount_not_found` (`CatalogErrors.DiscountNotFound`), come oggi.

## 1. POST /api/catalog/discounts/voucher-list

Tabella della pagina. Per il frontend sostituisce `GET /api/catalog/discounts`, che ha la stessa funzione con un'altra richiesta
(parametri nell'indirizzo), un'altra risposta (`{ total, page, pageSize, items }` con `DiscountDto`) e due soli ordinamenti (`code`
e `value`).

- **Righe**: gli sconti dello stabilimento (`catalog.pricing_discounts`), attivi e spenti; quelli eliminati non compaiono e non contano
  in `total` (filtro globale delle entità `ISoftDeletable`).
- **Permesso**: `CatalogRead`.

### Richiesta

La richiesta standard delle tabelle del frontend, con le stesse regole e gli stessi errori di
`POST /api/catalog/price-lists/packages-list` (`Listino - Frontend requests.md`, sezione 1):

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "",
  "filters": [],
  "sortField": "validFrom",
  "sortDirection": "Ascending"
}
```

- `sortField`: `voucherName`, `validFrom`, `validTo`, `discountType` o `discountValue`, oppure `""`: senza un ordinamento scelto
  dall'utente le righe sono per nome, come con `voucherName` e `Ascending`.
- `search`: la pagina non ha la barra di ricerca e manda sempre `""`. Con un testo, trova gli sconti il cui nome **o** codice lo
  contiene, come `GET /api/catalog/discounts` (`DiscountRepository.ListAsync`).
- `pageSize`: 10, 20, 50 o 100.

### Ordine

| `sortField` | Ordine |
|---|---|
| `voucherName` | **Ordine naturale** del nome mostrato: i numeri si confrontano come numeri ("Sconto 2" prima di "Sconto 10"), maiuscole e accenti non contano. Lo stesso dei nomi dei pacchetti (`Listino`, sezione 1). |
| `validFrom`, `validTo` | Per giorno. Gli sconti senza periodi in fondo, con tutti e due i versi. |
| `discountType` | `Percentage`, poi `Fixed` (al contrario con `Descending`). |
| `discountValue` | Per numero, senza guardare se è % o €. |

A parità di valore: per nome (ordine naturale, sempre crescente), poi per id interno.

### Risposta

200: `total` conta tutti gli sconti trovati, `rows` è la pagina richiesta. Una `page` oltre l'ultima risponde 200 con `rows` vuoto e
il `total` vero.

```json
{
  "total": 2,
  "rows": [
    {
      "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f04",
      "voucherName": "Clienti abituali",
      "validFrom": null,
      "validTo": null,
      "discountType": "Fixed",
      "discountValue": 15
    },
    {
      "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
      "voucherName": "Ferragosto",
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "discountType": "Percentage",
      "discountValue": 12.5
    }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idVoucher` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `voucherName` | `name`, o `code` se il nome manca | string | Colonna "Sconto". |
| `validFrom` | il più piccolo `starts_on` dei periodi | string (giorno UTC) o `null` | Colonna "Valido dal". `null` senza periodi. |
| `validTo` | il più grande `ends_on` dei periodi | string (giorno UTC) o `null` | Colonna "al". `null` senza periodi. |
| `discountType` | `type` | `Percentage` o `Fixed` | Colonna "Tipo di sconto". |
| `discountValue` | `value` | number | Colonna "Valore", con % o €. |

### Errori

Nell'ordine in cui si controllano: 401 `auth.unauthenticated`, 403 `auth.forbidden`, 400 `validation.failed` con `errors[]`,
403 `catalog.property_required`, 404 `catalog.property_not_found`. Il frontend mostra ogni errore nel popup di errore della
tabella (testo fisso, con "Riprova").

## 2. POST /api/catalog/discounts/delete-voucher-item

**L'unica API con cui il frontend elimina gli sconti**, dopo la conferma nel popup: il cestino di una riga la chiama con un id,
"Elimina selezione" con tutti gli id scelti. Elimina **uno o più sconti in una sola chiamata, tutto o niente**, come
`POST /api/catalog/price-lists/delete-package-item` (`Listino - Frontend requests.md`, sezione 2).

- **Tutto o niente**: se anche un solo sconto della lista non c'è, non si elimina nessuno. `DELETE /api/catalog/discounts/{id}`
  elimina uno sconto solo; `bulk-delete` elimina quelli possibili e restituisce gli altri come falliti con un 200: restano come
  sono e il frontend non le usa.
- **Stessi effetti della `DELETE` di oggi** per ogni sconto (`DiscountsService.DeleteAsync`): soft delete dello sconto **e delle sue
  righe figlie** (periodi, fasce orarie, settori) e una voce `catalog.discount_deleted` nel registro per sconto. Attenzione:
  `bulk-delete` carica gli sconti senza le figlie (`DiscountRepository.FindManyAsync`), che quindi restano. Le righe di
  `catalog.pricing_discount_sectors` rimaste contano ancora nel 409 `catalog.sector_in_use` dei settori
  (`SectorRepository.CountUsagesAsync`): un settore risulterebbe usato da uno sconto che non c'è più. Se è così, va corretto anche in
  `bulk-delete`.
- **Nessun altro controllo**, come oggi: anche uno sconto già usato si elimina (le prenotazioni tengono la copia dei suoi dati,
  `booking_discount`).
- **Permesso**: `CatalogPricing`.

### Richiesta e risposta

```json
{
  "idVouchers": ["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01", "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"]
}
```

- `idVouchers`: gli `idVoucher` delle righe della tabella, almeno uno. Lo stesso id ripetuto conta una volta. La selezione può
  comprendere righe di più pagine.
- Il frontend non ha un tetto al numero di id. Se ne serve uno (come i 300 di `BulkIdsRequest.MaxIds`), lo decide lo sviluppatore
  backend: l'agente glielo chiede prima di scrivere codice e lo scrive qui, così il frontend lo rispetta.
- Risposta: 204 senza corpo. Dopo il 204 gli id escono dalla selezione e la tabella si ricarica.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogPricing`. |
| 400 | `validation.failed` | `idVouchers` assente o vuoto: `validation.required` su `idVouchers` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `pricing.discount_not_found` | Un id non è uno sconto dello stabilimento (anche perché già eliminato). Non si elimina niente. |

Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con `pricing.discount_not_found`
toglie gli id dalla selezione e ricarica la tabella.

## 3. POST /api/catalog/discounts/duplicate-voucher-item

**L'unica API con cui il frontend duplica gli sconti**, dopo la conferma nel popup: il bottone "Duplica" di una riga la chiama con un
id, "Duplica selezione" con tutti gli id scelti. **Tutto o niente**, come l'eliminazione. Oggi il backend non ha la duplicazione
degli sconti.

- **La copia** è uno sconto nuovo dello stesso stabilimento, con un `public_id` nuovo, `created_by_user_id` dell'utente del token e
  **tutti** i dati dello sconto di partenza (tipo, valore, ambito con categorie e settori, canale, `requiresCode`, numero massimo di
  utilizzi, periodi con giorni e fasce orarie), tranne:
  - **codice** (`code`): `CopyNames.Next` (`Domain/CopyNames.cs`) fra i codici dello stabilimento, al massimo **60** caratteri:
    "ESTATE" → "ESTATE copia", poi "ESTATE copia 2"; la copia di una copia riparte dal codice di base; senza distinguere maiuscole e
    minuscole;
  - **nome** (`name`): la stessa regola fra i nomi dello stabilimento, al massimo **120** caratteri; se manca resta nullo;
  - **spenta** (`is_active` falso): il codice della copia non deve valere prima che l'utente l'abbia controllato;
  - **mai usata** (`used_count` 0).
- **Codici in contemporanea**: il codice ha un indice unico (`uq_catalog_pricing_discounts_property_code`). Se fra la scelta del codice
  e il salvataggio un'altra richiesta lo prende, si prende il successivo libero: la duplicazione non risponde mai con un errore
  (né un 500) per il codice. Più copie dello stesso sconto, o di sconti con lo stesso codice di base, nella stessa chiamata prendono
  codici diversi.
- Se lo stesso id compare più volte, si fa una copia sola.
- Nel registro una voce per copia: `catalog.discount_created`, oppure un codice nuovo `catalog.discount_duplicated` come
  `catalog.price_list_duplicated`. Lo sceglie lo sviluppatore; se è nuovo lo scrive qui (il filtro del Log della pagina Gestione
  utenti ha l'elenco dei codici).
- **Permesso**: `CatalogPricing`.

### Richiesta, risposta ed errori

```json
{
  "idVouchers": ["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"]
}
```

- `idVouchers` ed errori: come nell'eliminazione (sezione 2). Con 404 `pricing.discount_not_found` non si duplica niente.
- Risposta: 204 senza corpo. Dopo il 204 la selezione si svuota (solo per "Duplica selezione") e la tabella si ricarica.

## Regole comuni dei popup (sezioni 4-6)

I popup "Aggiungi nuovo sconto/voucher" e di modifica hanno gli stessi campi. Salva manda **sempre tutto lo sconto**: quello letto, con
le modifiche dell'utente.

| Campo | Oggi | Tipo | Regola |
|---|---|---|---|
| `voucherName` | `name` | string | Obbligatorio, al massimo 120 caratteri. Il backend toglie gli spazi ai lati. |
| `voucherCode` | `code` | string | Obbligatorio, al massimo 60 caratteri, unico nello stabilimento senza distinguere maiuscole e minuscole. Il backend toglie gli spazi ai lati. |
| `isCodeRequired` | `requiresCode` | boolean | Il popup non lo mostra: `true` per uno sconto nuovo; in modifica torna com'è stato letto. |
| `validityPeriods` | `periods`, `weekdays`, `timeRanges` | array | Al massimo 50. Almeno uno se `isCodeRequired` è falso; con il codice può essere `[]` (sempre valido). |
| `validityPeriods[].validFrom`, `validTo` | `startsOn`, `endsOn` | string (giorno UTC) | Obbligatori; `validTo` non prima di `validFrom`. Compresi tutti e due. |
| `validityPeriods[].daysOfWeek` | `weekdays` (dello sconto) | number[] | Giorni ISO, da 1 (lunedì) a 7 (domenica), almeno uno, senza ripetizioni. Tutti e sette = ogni giorno. |
| `validityPeriods[].timeSlots` | `timeRanges` (dello sconto) | array di `{ from, to }` | Al massimo 20, `"HH:mm"`, `to` dopo `from`. `[]` = tutto il giorno. |
| `discountType` | `type` | string | `Percentage` o `Fixed`. |
| `discountValue` | `value` | number | Più di 0, con al massimo 2 decimali. Fino a 100 per `Percentage`, fino a 100.000 per `Fixed`. |
| `discountScope` | `scope` | string | `All`, `Custom` o `WithoutCustom` ("A cosa si applica"). |
| `serviceCategories` | `serviceCategories` | string[] | `Bar`, `Normal`, `Hourly`, `Additional`; `[]` con `All`. |
| `idSectors` | `sectorIds` | string[] (UUID) | `public_id` dei settori; `[]` con `All`. |
| `maxUseQuantity` | `maxUses` | number (intero) o `null` | Almeno 1; `null` = illimitato, come oggi. Non meno degli utilizzi già fatti. |
| `isEnabled` | `isActive` | boolean | Obbligatorio. |

- **Canale** (`channel`): il frontend non lo manda. Uno sconto nuovo è `manager_and_online`; la modifica lascia quello salvato.
- **Utilizzi già fatti** (`used_count`): il frontend non li manda né li legge; la modifica non li cambia.

### Giorni e fasce orarie di ogni periodo

Oggi i giorni della settimana e le fasce orarie sono **dello sconto** e valgono per tutti i suoi periodi (`Discount.Weekdays`,
`Discount.TimeRanges`): uno sconto valido dal lunedì al venerdì a giugno e tutti i giorni ad agosto non si può salvare. Il popup li fa
scegliere **in ogni periodo**.

- **Da aggiungere**: i giorni e le fasce orarie di ogni periodo (`DiscountDateRange`, `catalog.pricing_discount_date_ranges`). Come
  salvarli (colonna dei giorni e tabella delle fasce legate al periodo, o altro) lo sceglie lo sviluppatore del backend: l'agente
  glielo chiede prima di scrivere codice.
- **Quando lo sconto vale** (`DiscountRules.IsValid`, `Domain/Pricing/QuoteRules.cs:44`): deve esserci **almeno un periodo** che
  contiene le date della prenotazione, con tutti i giorni della prenotazione fra i suoi giorni e, se ha fasce orarie, con l'ora attuale
  dentro una di esse. Le tre condizioni si guardano **sullo stesso periodo**, non una per periodo. Per tutto il resto (attivo, canale,
  utilizzi, ora attuale e non quella della prenotazione, fascia da `from` compreso a `to` escluso) vale quello che c'è oggi.
  `PublicCatalogRepository.Discounts()` deve caricare anche i dati nuovi.
- **Senza periodi**, come oggi: uno sconto con il codice vale sempre, uno automatico mai.
- **Dati già salvati** (migrazione): ogni periodo prende i giorni e le fasce orarie del suo sconto, così nessuno sconto cambia. Uno
  sconto **senza** periodi con giorni o fasce li tiene come oggi (valgono finché non gli si salva un periodo); il popup non li mostra
  e un salvataggio che lascia `validityPeriods` vuoto li lascia com'erano.
- **In lettura** un periodo senza giorni (vuoto = tutti) dà `daysOfWeek` `[1, 2, 3, 4, 5, 6, 7]`; tutti e sette e vuoto sono la stessa
  cosa.
- **API di oggi**: `POST` e `PUT /api/catalog/discounts` mettono `weekdays` e `timeRanges` in tutti i periodi della richiesta. Se tutti i
  periodi hanno gli stessi giorni e le stesse fasce, le letture di oggi (`DiscountDto`) li danno come ora. Se sono diversi, `DiscountDto`
  non li può dire: **lo sviluppatore decide come** (per esempio un campo in più nei `periods`), purché
  `PUT /api/catalog/discounts/{id}` non tolga senza volerlo quelli salvati dal popup.
- **Snapshot offline** `price_catalog` (`ConfigSnapshotBuilder.cs:153`): contiene `DiscountDto`, quindi deve dire i giorni e le fasce di
  ogni periodo. Si rigenera già quando cambiano le entità che cominciano con `Discount` (`SnapshotTriggers.cs:28`); se la tabella nuova ha
  un altro prefisso, va aggiunta.

### Lunghezze: una correzione

`SaveDiscountRequestValidator` accetta un codice fino a **64** caratteri (`Discount.CodeMaxLength`) e un nome fino a **255**, ma le colonne
ne tengono **60** (`code`) e **120** (`name`) (`DiscountConfiguration.cs:42,55`): un codice di 62 caratteri passa il controllo e il
salvataggio fallisce nel database con un 500. Con le API nuove e con quelle di oggi: 400 `validation.max_length` oltre **60** e **120**,
con `args.max`. Il frontend non fa scrivere di più.

### Codice già usato

`CodeExistsAsync` controlla prima di salvare, ma due salvataggi nello stesso momento con lo stesso codice passano tutti e due il controllo
e il secondo viola l'indice unico: deve rispondere **409 `pricing.discount_code_taken`** con `args.code`, non un 500. Per la duplicazione
vale la sezione 3.

### A cosa si applica

Il popup ha gli stessi tre ambiti del backend, con gli stessi effetti (`DiscountRules.Amount`, `Domain/Pricing/QuoteRules.cs:63`), che non
cambiano:

| `discountScope` | `scope` | Nel popup | Su cosa vale lo sconto |
|---|---|---|---|
| `All` | `all` | Tutto | Tutte le righe della prenotazione |
| `Custom` | `custom` | Solo alcune scelte | Le righe il cui settore è fra gli `idSectors` **oppure** la cui categoria è fra le `serviceCategories` |
| `WithoutCustom` | `without_custom` | Tutto tranne alcune scelte | Tutte le altre righe |

- `serviceCategories`: `Bar` ("Servizi Bar"), `Normal` ("Servizi"), `Hourly` ("Servizi Orari"), `Additional` ("Articoli/Servizi aggiuntivi"): i
  nomi di `DiscountServiceCategory`.
- `idSectors`: gli `id` di `POST /api/catalog/sectors/sector-combobox` (`Servizi - Frontend requests.md`, sezione 25), cioè i `public_id`
  dei settori.
- **`Custom` e `WithoutCustom` vogliono almeno una categoria o un settore**, come oggi: 400 `validation.required` su `serviceCategories`.
- **Con `All`** il frontend manda tutti e due gli elenchi vuoti; se arrivano pieni si ignorano e si salvano vuoti, come oggi
  (`Discount.Update`, `SetSectors([])`).
- **Un settore che non c'è** (eliminato da un altro utente mentre il popup è aperto, o di un altro stabilimento): 404
  `catalog.sector_not_found`, come oggi (`ContentMapping.SectorIdsAsync`). Un settore già salvato in uno sconto non si può eliminare
  (409 `catalog.sector_in_use`, `args.discounts`), quindi la lettura non ha settori che non esistono più.

### Errori

Nell'ordine in cui si controllano, per le API 4 e 6 (le righe con "6" solo per la modifica):

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogPricing`. |
| 400 | `validation.failed` | Un campo non valido (tabella sotto), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 (6) | `pricing.discount_not_found` | `idVoucher` non è uno sconto dello stabilimento. |
| 409 (6) | `resource.version_conflict` | Sezione 7. Si controlla subito dopo il 404 dello sconto. |
| 409 | `pricing.discount_code_taken` | Il codice è di un altro sconto dello stabilimento. `args`: `code`, come oggi. |
| 422 (6) | `pricing.discount_max_uses_below_used` | `maxUseQuantity` è meno degli utilizzi già fatti. `args`: `usedCount`, come oggi. |
| 404 | `catalog.sector_not_found` (`CatalogErrors.SectorNotFound`) | Un `idSectors` non è un settore dello stabilimento. |

Errori dei campi (400 `validation.failed`, più di uno insieme):

| Campo | `code` | Quando |
|---|---|---|
| `voucherName`, `voucherCode` | `validation.required` | Vuoto o di soli spazi. |
| `voucherName`, `voucherCode` | `validation.max_length` | Oltre 120 o 60 caratteri, con `args.max`. |
| `validityPeriods` | `validation.required` | Vuoto con `isCodeRequired` falso. |
| `validityPeriods` | `validation.invalid` | Più di 50 periodi. |
| `validityPeriods[i].validFrom`, `.validTo` | `validation.required` | Mancante o non una data. |
| `validityPeriods[i].validTo` | `validation.end_before_start` | Prima di `validFrom`. |
| `validityPeriods[i].daysOfWeek` | `validation.required` | Vuoto, o con un numero che non va da 1 a 7. |
| `validityPeriods[i].timeSlots` | `validation.invalid` | Più di 20, un'ora mancante o non `HH:mm`, o `to` non dopo `from`. |
| `discountType`, `discountScope`, `isEnabled` | `validation.invalid` | Un valore che non esiste. |
| `serviceCategories` | `validation.invalid` | Una categoria che non esiste. |
| `serviceCategories` | `validation.required` | Vuoto con `Custom` o `WithoutCustom`, se anche `idSectors` è vuoto. |
| `discountValue` | `validation.out_of_range` | Mancante, 0 o meno, oltre 100 (`Percentage`) o oltre 100.000 (`Fixed`). |
| `maxUseQuantity` | `validation.out_of_range` | Meno di 1 o non intero. |

Il frontend controlla gli stessi limiti e mostra gli errori sotto i campi: `pricing.discount_code_taken` sotto il codice,
`pricing.discount_max_uses_below_used` sotto il numero massimo di utilizzi; gli errori dentro un periodo compaiono sopra i bottoni. Con
`catalog.sector_not_found` il messaggio compare sopra i bottoni e la tendina dei settori si rilegge: quello che non c'è più si vede come
"Settore/fila non più disponibile" e l'utente lo toglie.

```json
{
  "type": "https://errors.summerbooking/pricing/discount_code_taken",
  "title": "pricing.discount_code_taken",
  "status": 409,
  "code": "pricing.discount_code_taken",
  "args": { "code": "FERRAGOSTO5" },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## 4. POST /api/catalog/discounts/add-voucher-item

Salva del popup "Aggiungi nuovo sconto/voucher". Per il frontend sostituisce `POST /api/catalog/discounts`. Crea uno sconto mai usato,
con i dati di "Regole comuni dei popup", e nel registro una voce `catalog.discount_created`.

```json
{
  "voucherName": "Ferragosto",
  "voucherCode": "FERRAGOSTO5",
  "isCodeRequired": true,
  "validityPeriods": [
    {
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "daysOfWeek": [1, 2, 3, 4, 5],
      "timeSlots": [
        { "from": "09:00", "to": "13:00" },
        { "from": "15:00", "to": "19:00" }
      ]
    },
    {
      "validFrom": "2026-08-22T00:00:00.000Z",
      "validTo": "2026-08-23T00:00:00.000Z",
      "daysOfWeek": [6, 7],
      "timeSlots": []
    }
  ],
  "discountType": "Percentage",
  "discountValue": 12.5,
  "discountScope": "Custom",
  "serviceCategories": ["Bar", "Normal"],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "maxUseQuantity": 50,
  "isEnabled": true
}
```

Risposta: 204 senza corpo. Il popup si chiude e la tabella si ricarica. Errori: "Regole comuni dei popup".

## 5. GET /api/catalog/discounts/get-selected-item

Lettura del popup di modifica, a ogni apertura: `GET /api/catalog/discounts/get-selected-item?idVoucher=<uuid>`. Per il frontend
sostituisce `GET /api/catalog/discounts/{id}`. **Permesso**: `CatalogRead`.

200 con tutti i campi di "Regole comuni dei popup", più `idVoucher` e `version` (sezione 7). I periodi in ordine di `validFrom`, le fasce
in ordine di `from`, i giorni in ordine:

```json
{
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Ferragosto",
  "voucherCode": "FERRAGOSTO5",
  "isCodeRequired": true,
  "validityPeriods": [
    {
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "daysOfWeek": [1, 2, 3, 4, 5],
      "timeSlots": [
        { "from": "09:00", "to": "13:00" },
        { "from": "15:00", "to": "19:00" }
      ]
    }
  ],
  "discountType": "Percentage",
  "discountValue": 12.5,
  "discountScope": "WithoutCustom",
  "serviceCategories": ["Additional"],
  "idSectors": [],
  "maxUseQuantity": null,
  "isEnabled": false,
  "version": "5be2d0f7"
}
```

- `voucherName` è `null` se lo sconto non ha un nome (oggi può mancare): il popup lo mostra vuoto e chiede di scriverlo prima di
  salvare.
- Uno sconto non si legge con valori che il backend non ha: il frontend non inventa niente, e Salva resta spento finché la lettura non è
  riuscita.

Errori, nell'ordine in cui si controllano: 401 `auth.unauthenticated`, 403 `auth.forbidden` (`CatalogRead`),
403 `catalog.property_required`, 404 `catalog.property_not_found`, 404 `pricing.discount_not_found` (anche con un `idVoucher` mancante o
non UUID). Con un errore il popup si chiude e compare l'avviso con il testo del `code`; con il 404 dello sconto la tabella si ricarica.

## 6. POST /api/catalog/discounts/update-voucher-item

Salva del popup di modifica. Per il frontend sostituisce `PUT /api/catalog/discounts/{id}`. Il corpo è quello della sezione 4 più
`idVoucher` (quello della lettura), con l'header `If-Match` (sezione 7):

```json
{
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Ferragosto",
  "voucherCode": "FERRAGOSTO5",
  "isCodeRequired": true,
  "validityPeriods": [],
  "discountType": "Fixed",
  "discountValue": 5,
  "discountScope": "All",
  "serviceCategories": [],
  "idSectors": [],
  "maxUseQuantity": null,
  "isEnabled": false
}
```

- **Restano com'erano**: canale, utilizzi già fatti, giorni e fasce dello sconto senza periodi.
- Nel registro una voce `catalog.discount_updated`, come oggi.
- Risposta: 204 senza corpo, con la nuova versione nell'header `ETag`. Il popup si chiude e la tabella si ricarica. Errori: "Regole comuni
  dei popup".

## 7. Controllo di versione sul salvataggio di modifica

Se due utenti modificano lo stesso sconto, vince chi salva per ultimo: il popup rimanda tutto lo sconto letto all'apertura e cancella,
senza che nessuno se ne accorga, quello che un altro utente ha cambiato nel frattempo. Il backend non ha un controllo di concorrenza sugli
sconti: nessun token in `DiscountConfiguration`, nessun `If-Match` in `PUT /api/catalog/discounts/{id}`. Il frontend chiede lo schema di
**concorrenza ottimistica** già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore unico per tutte le
API**: `resource.version_conflict`. Lo stesso schema è chiesto in `Listino - Frontend requests.md` (sezione 16) e negli altri documenti: se lì è
già fatto, qui si aggiungono solo le API 5 e 6.

### Cosa ci serve

- **Versione nella lettura** (sezione 5): il campo `version` (stringa opaca, sempre presente). Cambia ogni volta che cambiano i dati che il
  salvataggio scrive, anche quando li cambia un'altra API (per esempio `PUT /api/catalog/discounts/{id}`), e solo allora: non cambia per
  `used_count` e `updated_at`, altrimenti uno sconto usato mentre il popup è aperto farebbe perdere le modifiche.
- **Dati coperti**: nome, codice, `isCodeRequired`, periodi con giorni e fasce orarie, tipo, valore, ambito con categorie e settori, numero
  massimo di utilizzi e stato. Stanno in più tabelle: un token della sola riga di `catalog.pricing_discounts` (come `xmin`) non basta, e cambierebbe anche con
  `used_count`.
- **`If-Match` nel salvataggio**: il frontend manda la versione letta nell'header `If-Match: "<version>"`. Se non è più quella attuale, il
  backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza `If-Match` risponde allo stesso modo: il
  frontend lo manda sempre.
- **`ETag` nella risposta**: il salvataggio riuscito risponde 204 con la nuova versione nell'header `ETag: "<version>"`.
- Il 409 si controlla **subito dopo il 404 dello sconto**, prima del codice già usato e degli utilizzi.
- **Come farlo lo sceglie lo sviluppatore del backend** (un hash dei dati coperti, un contatore aggiornato da ogni API che li cambia...), nei
  limiti dei punti sopra: l'agente glielo chiede prima di scrivere codice. Il frontend tratta la versione come una stringa opaca e non la
  confronta mai.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il popup si chiude, compare l'avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo
ricaricati. Rifai le tue modifiche." e la tabella si ricarica; riaprendo la matita il popup rilegge lo sconto.

## Verifica

Con un token con `CatalogRead` e `CatalogPricing`:

1. `voucher-list` con ogni `sortField` nei due versi: ordine della sezione 1, sconti senza periodi in fondo per `validFrom` e `validTo`,
   "Sconto 2" prima di "Sconto 10"; `""` come `voucherName` crescente; uno sconto senza nome mostra il codice.
2. `voucher-list` con `pageSize` 25 o un `sortField` sconosciuto: 400 `validation.failed`.
3. `delete-voucher-item` con due id validi: 204 e non ci sono più. Con uno valido e uno sconosciuto (o di un altro stabilimento): 404
   `pricing.discount_not_found` e il valido c'è ancora. Con `[]`: 400 `validation.required` su `idVouchers`.
4. Un settore usato solo da uno sconto eliminato con `delete-voucher-item` si può eliminare (niente `discounts` in
   `catalog.sector_in_use`).
5. `duplicate-voucher-item` di uno sconto attivo e usato: 204; la copia ha codice e nome con " copia", è spenta, con zero utilizzi e tutto il
   resto uguale, periodi, giorni e fasce compresi. Di nuovo: " copia 2". Con un codice di 60 caratteri: la copia ne ha al massimo 60.
6. Due `duplicate-voucher-item` dello stesso sconto nello stesso momento: due 204 e due codici diversi.
7. `add-voucher-item` con il corpo della sezione 4: 204; `get-selected-item` lo rilegge uguale, periodo per periodo.
8. `add-voucher-item` con un codice già usato, anche in maiuscole diverse o con spazi ai lati: 409 `pricing.discount_code_taken` con `code`. Due
   richieste uguali nello stesso momento: una 204 e una 409, mai un 500.
9. `add-voucher-item` con un codice di 61 caratteri o un nome di 121: 400 `validation.max_length`. Lo stesso con `POST /api/catalog/discounts`.
10. `add-voucher-item` con `validityPeriods` `[]` e `isCodeRequired` vero: 204; con `isCodeRequired` falso: 400 `validation.required`.
11. Il preventivo con il codice di uno sconto con due periodi (giugno lun-ven, agosto sab-dom): valido per un martedì di giugno, non per un
    sabato di giugno né per un martedì di agosto. Con una fascia oraria solo nel periodo di giugno, ad agosto l'ora non conta.
12. Uno sconto salvato prima della migrazione con giorni e fasce orarie vale negli stessi momenti di prima; `get-selected-item` li dà in ogni
    suo periodo. Senza giorni: `daysOfWeek` con tutti e sette.
13. `PUT /api/catalog/discounts/{id}` e lo snapshot `price_catalog` dopo la modifica: come deciso in "API di oggi" e "Snapshot offline".
14. `update-voucher-item` con `maxUseQuantity` minore degli utilizzi: 422 `pricing.discount_max_uses_below_used` con `usedCount`; con `null`: 204
    e illimitato.
15. `update-voucher-item` di uno sconto `online_only`: il canale non cambia. Uno sconto `without_custom` con una categoria e un settore si
    rilegge uguale; salvato con `All`, categorie e settori sono vuoti. `Custom` senza categorie e settori: 400 `validation.required` su
    `serviceCategories`. Con un settore di un altro stabilimento: 404 `catalog.sector_not_found` e lo sconto non cambia.
16. `get-selected-item` dà `version` (per esempio `v1`); `update-voucher-item` con `If-Match: "v1"`: 204 con un `ETag` diverso, che è la versione
    della lettura dopo. Lo stesso salvataggio di nuovo con `"v1"`, o senza `If-Match`: 409 `resource.version_conflict` e lo sconto non cambia,
    anche con un codice già usato.
17. Cambiare solo una fascia oraria cambia la versione; cambiare `used_count` no.
18. `update-voucher-item` con un `idVoucher` di un altro stabilimento o eliminato: 404 `pricing.discount_not_found`, anche con un `If-Match`
    qualunque.
