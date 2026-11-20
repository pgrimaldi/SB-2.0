# Settori e file - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Settori e file** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/rows-and-sectors`: una tabella con i settori (le file) dello stabilimento, con ricerca,
ordinamento per nome e pagine, e i comandi per aggiungere, rinominare, duplicare ed eliminare i settori.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/sectors-list` | Elenco dei settori per la tabella, a pagine |
| 2 | POST | `/api/catalog/sectors/bulk-delete` | Elimina uno o più settori, tutto o niente |
| 3 | POST | `/api/catalog/sectors/bulk-duplicate` | Duplica uno o più settori, tutto o niente |

Aggiungi e Modifica di una riga usano le API che ci sono già, così come sono: `POST /api/catalog/sectors` e
`PUT /api/catalog/sectors/{id}`. Per eliminare, anche una riga sola, il frontend usa solo l'API 2.

## Regole comuni

- **Modulo**: le API stanno in Catalog (`Module.Catalog`), accanto a `/api/catalog/sectors` (`Endpoints/SeasonEndpoints.cs`,
  `Application/Services/SectorService.cs`), con i dati di `MapSector` (`catalog.map_sectors`).
- **Le API esistenti non cambiano**: `GET`, `POST`, `PUT` e `DELETE /api/catalog/sectors` restano come sono (il
  frontend non usa la `DELETE`: per eliminare usa solo la sezione 2): stesse rotte, richieste, risposte e
  comportamento, salvo la risposta ai salvataggi contemporanei dello stesso nome (vedi "Due salvataggi dello stesso
  nome nello stesso momento").
- **Permesso**: per l'API 1 `CatalogRead`, come `GET /api/catalog/sectors`; per le API 2 e 3 `CatalogSettings`, come
  `POST`, `PUT` e `DELETE /api/catalog/sectors`. Senza permesso 403 `auth.forbidden`; senza token valido 401
  `auth.unauthenticated`.
- **Stabilimento**: lo prende dal token (`CurrentProperty.IdAsync`), come le altre API del Catalog: nessuna richiesta lo
  contiene. Token senza stabilimento: 403 `catalog.property_required`; stabilimento che non esiste: 404
  `catalog.property_not_found`.
- **Righe eliminate**: i settori con `deleted_at` valorizzato non compaiono mai. Lo fa già il filtro globale delle entità
  `ISoftDeletable` (`ModuleDbContextConventions.ApplySoftDeleteQueryFilter`, richiamato in
  `Infrastructure/DbContext.cs`), che vale anche per `MapSector`.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono. Gli errori di validazione sono 400 `validation.failed` con un elemento di `errors[]` per
  ogni regola non rispettata (`ValidatorExtensions.EnsureValid`), con `field` uguale al nome del campo nel corpo.
- **`args` degli errori di campo**: `ValidatorExtensions.ToFieldError` copia sempre tutti i segnaposto che
  FluentValidation dà alla regola, quindi `validation.max_length` ha `args.max` e anche `args.min` = 0 (provato con
  FluentValidation 12.1.1, la versione del backend). Il frontend non mostra il testo dei singoli errori di validazione
  di queste API.

## 1. POST /api/catalog/sectors-list

Tabella della pagina: il frontend la chiama all'apertura e ogni volta che cambiano pagina, numero di righe, ricerca o
ordinamento. È un'**API nuova**: per il frontend sostituisce `GET /api/catalog/sectors`, che restituisce tutti i
settori insieme, senza pagine, ricerca né ordinamento per nome, e con i campi `id`, `name` e `sortOrder`
(`SectorDto`).

### Richiesta

Il corpo è la richiesta standard delle tabelle del frontend, uguale per tutte le tabelle del gestionale:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "fila",
  "filters": [],
  "sortField": "sectorName",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number (intero) | Pagina richiesta, da 1. |
| `pageSize` | number (intero) | Righe per pagina: 10, 20, 50 o 100. Non valgono gli altri valori di `CatalogQueryRules.PageSizes` (25, 200, 300). |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Il frontend lo manda senza spazi ai lati, da 3 caratteri in su e al massimo 100 (il campo non ne accetta di più). Trova i settori il cui nome contiene il testo, senza distinguere maiuscole e minuscole; `%` e `_` contano come caratteri normali, non come caratteri jolly. Al massimo 100 caratteri (`CatalogQueryRules.SearchMaxLength`, come le altre liste del Catalog). |
| `filters` | array | Sempre `[]`: la tabella non ha filtri. |
| `sortField` | string | `sectorName`, oppure `""` per l'ordine del backend: `sort_order`, poi `id`, come oggi `GET /api/catalog/sectors` (`SectorRepository.ListAsync`). |
| `sortDirection` | string | `Ascending` o `Descending`. Conta solo con `sortField` non vuoto: con `sortField` `""` il frontend manda `Ascending` e il backend usa il suo ordine. |

Ordinamento per `sectorName`: **ordine naturale** del nome, cioè i numeri dentro il nome si confrontano come numeri,
non cifra per cifra. Maiuscole e minuscole non contano e le lettere accentate stanno accanto a quelle senza accento.

| `sortDirection` | Ordine atteso |
|---|---|
| `Ascending` | Fila 1, Fila 2, Fila 9, Fila 10, Fila 11, Gazebo, Settore Est |
| `Descending` | Settore Est, Gazebo, Fila 11, Fila 10, Fila 9, Fila 2, Fila 1 |

Il semplice `ORDER BY lower(name)` non basta: con l'ordinamento del database (`en_US.utf8`) mette "Fila 10" e
"Fila 11" prima di "Fila 2". Come ottenere l'ordine naturale (per esempio una collation ICU con l'ordine numerico,
`und-u-kn-true`, che PostgreSQL supporta già, o un'altra soluzione) lo decide lo sviluppatore backend: l'agente glielo
chiede prima di scrivere codice; il risultato deve essere quello della tabella sopra, con paginazione e `total` fatti
dal database su quell'ordine. A parità di ordine (nomi che differiscono solo per maiuscole o accenti) vale l'ordine del
backend: `sort_order`, poi `id`.

Valori non validi: 400 `validation.failed` con un errore `validation.invalid` sul campo: `page` (meno di 1),
`pageSize` (non 10, 20, 50 o 100), `sortField` (non `sectorName` né `""`), `sortDirection` (non `Ascending` o
`Descending`), `filters` (un elemento qualsiasi). `search` oltre 100 caratteri: `validation.max_length` su `search`,
con `args.max` 100 (e `args.min` 0). Il frontend non manda questi valori.

### Risposta

200 nello stile delle tabelle: `total` conta tutti i settori dello stabilimento che rispettano la ricerca, `rows` è la
pagina richiesta (al massimo `pageSize` righe).

```json
{
  "total": 3,
  "rows": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "sectorName": "Fila 1" },
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02", "sectorName": "Fila 2" },
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03", "sectorName": "Fila 3" }
  ]
}
```

Solo questi due campi per riga:

| Campo | In `SectorDto` | Tipo | Note |
|---|---|---|---|
| `idSector` | `id` | string (UUID) | Il `public_id` del settore. Non si mostra: serve alle azioni della riga. |
| `sectorName` | `name` | string | Colonna "Nome". |

Una `page` oltre l'ultima risponde 200 con `rows` vuoto e il `total` vero: il frontend passa da solo all'ultima pagina
che c'è.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogRead`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Valori non validi"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra ogni errore di questa API nel popup di errore della tabella (testo fisso, con "Riprova"), senza il
testo del codice.

## 2. POST /api/catalog/sectors/bulk-delete

**L'unica API con cui il frontend elimina i settori**, dopo la conferma nel popup: il cestino di una riga la chiama con
un id, "Elimina selezione" della tabella (compare con almeno due righe scelte) con tutti gli id scelti. Elimina **uno o
più settori in una sola chiamata, tutto o niente**. È un'**API nuova**: il frontend non usa
`DELETE /api/catalog/sectors/{id}`, che resta com'è.

### Cosa ci serve

- **Tutto o niente**: se anche un solo settore della lista non si può eliminare, non si elimina nessuno e la risposta
  è l'errore di quel settore. Il popup chiede all'utente di eliminare quei settori insieme: non deve ritrovarsi con una
  parte eliminata e una no.
- Le `bulk-delete` che il Catalog ha già (per esempio `Endpoints/PriceListEndpoints.cs:76`, con `BulkIdsRequest` e
  `BulkResult` di `Shared/Dtos/BulkResult.cs`) fanno un'altra cosa: eliminano quelli possibili e restituiscono gli altri
  come falliti. Per questa pagina non vanno bene.
- Per ogni settore valgono gli stessi controlli e lo stesso effetto di `DELETE /api/catalog/sectors/{id}`
  (`SectorService.DeleteAsync`): il settore deve essere dello stabilimento e non usato (`CountUsagesAsync`); si elimina
  con il soft delete di sempre e nel registro va una voce `catalog.sector_deleted` per ogni settore.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodo nuovo di `SectorService`, conteggi per più settori
  insieme, transazione...), purché il risultato sia questo.

### Richiesta

```json
{
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02", "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03"]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idSectors` | array di string (UUID) | Gli `idSector` delle righe di `sectors-list` (il `public_id` dei settori), almeno uno: uno solo per il cestino di una riga, tutti quelli scelti per "Elimina selezione". Lo stesso id ripetuto conta una volta. La selezione della tabella può comprendere righe di più pagine, quindi anche tutti i settori dello stabilimento. |

Il frontend non ha un tetto al numero di id. Se ne serve uno (come i 300 di `BulkIdsRequest.MaxIds`), lo decide lo
sviluppatore backend: l'agente glielo chiede prima di scrivere codice e lo scrive qui, così il frontend lo rispetta.

### Risposta

204 senza corpo. Dopo il 204 la tabella si ricarica con `sectors-list`.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | `idSectors` assente o vuoto: `validation.required` su `idSectors` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `catalog.sector_not_found` | Un id non è un settore dello stabilimento (anche perché già eliminato). Non si elimina nessun settore. |
| 409 | `catalog.sector_in_use` | Un settore è ancora usato. Non si elimina nessun settore. `args` come in `DELETE /api/catalog/sectors/{id}` (`mapSpots`, `priceLists`, `stayLimits`, `discounts`, contati come `CountUsagesAsync`) e **in più `sectorName`**, il nome di quel settore, anche con un id solo: il messaggio dice sempre quale. |

Prima si controlla che tutti gli id siano settori dello stabilimento (404 se uno non lo è), poi che nessuno sia usato:
con più settori in uso, `sectorName` e i conteggi sono quelli del primo in uso nell'ordine di `idSectors`.

```json
{
  "type": "https://errors.summerbooking/catalog/sector_in_use",
  "title": "catalog.sector_in_use",
  "status": 409,
  "code": "catalog.sector_in_use",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "args": { "sectorName": "Fila 1", "mapSpots": 14, "priceLists": 1, "stayLimits": 0, "discounts": 0 }
}
```

Il frontend mostra ogni errore di questa API nell'avviso "Operazione non riuscita", con il testo del suo `code`:
per `catalog.sector_in_use` "Il settore/fila {{sectorName}} è in uso e non si può eliminare: postazioni sulla mappa
{{mapSpots}}, listini {{priceLists}}, limiti di soggiorno {{stayLimits}}, sconti {{discounts}}."

## 3. POST /api/catalog/sectors/bulk-duplicate

Bottone "Duplica" di una riga (un id) e "Duplica selezione" della tabella (uno o più id), dopo la conferma nel popup.
È un'**API nuova**: oggi il backend non ha un modo di duplicare un settore.

### Cosa ci serve

- **Tutto o niente**, come l'eliminazione: se un id non va bene non si duplica nessun settore.
- Per ogni settore della lista un settore nuovo dello stesso stabilimento:
  - il **nome lo sceglie il backend** (per esempio "Fila 2 (copia)", poi "Fila 2 (copia 2)"): deve rispettare le
    regole di `POST /api/catalog/sectors`, cioè essere unico nello stabilimento senza distinguere maiuscole e
    minuscole (indice `uq_catalog_map_sectors_property_name`) e avere al massimo 100 caratteri
    (`MapSector.NameMaxLength`), accorciando il nome di partenza se serve;
  - va in **fondo all'ordine** (`sort_order`, come `NextSortOrderAsync`), nell'ordine di `idSectors`;
  - è solo il settore: postazioni della mappa, prezzi dei listini, limiti di soggiorno e sconti restano sul settore di
    partenza, la copia non è usata da nessuno;
  - nel registro una voce `catalog.sector_created`, come `SectorService.CreateAsync`;
  - mai un 500 per il nome di una copia, anche se nello stesso momento un altro utente salva quel nome (vedi "Due
    salvataggi dello stesso nome nello stesso momento").
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Richiesta

Come la sezione 2:

```json
{
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02"]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idSectors` | array di string (UUID) | Gli `idSector` delle righe di `sectors-list`, almeno uno. Lo stesso id ripetuto conta una volta (una copia sola). La selezione può comprendere righe di più pagine. |

Per un eventuale tetto al numero di id vale la nota della sezione 2.

### Risposta

204 senza corpo. Dopo il 204 la tabella si ricarica con `sectors-list`, dove compaiono le copie.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | `idSectors` assente o vuoto: `validation.required` su `idSectors` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `catalog.sector_not_found` | Un id non è un settore dello stabilimento (anche perché eliminato). Non si duplica nessun settore. |

Il frontend mostra ogni errore di questa API nell'avviso "Operazione non riuscita", con il testo del suo `code`.

## API esistenti usate così come sono

Il frontend le chiama senza modifiche; qui solo come le usa e dove mostra le risposte.

| Azione | API | Corpo | Risposta | Errori e dove li mostra il frontend |
|---|---|---|---|---|
| Salva del popup "Aggiungi settore/fila" | `POST /api/catalog/sectors` | `{ "name": "Fila 9" }` (`SaveSectorRequest`) | 201 con `SectorDto`: il frontend non la legge e ricarica la tabella | `validation.failed` su `name` e 409 `catalog.sector_name_taken` sotto il campo del nome; gli altri sopra i bottoni del popup. |
| Salva del popup della matita | `PUT /api/catalog/sectors/{id}`, con l'`idSector` della riga | `{ "name": "Fila 2 lato mare" }` | 200 con `SectorDto`: il frontend non la legge e ricarica la tabella | Come l'aggiunta; 404 `catalog.sector_not_found` sopra i bottoni. |

### Due salvataggi dello stesso nome nello stesso momento

Oggi `POST` e `PUT /api/catalog/sectors` controllano il nome già usato prima di salvare (`SectorService.CreateAsync` e
`UpdateAsync`, `Application/Services/SectorService.cs:42` e `:55`, con `EnsureNameFreeAsync` alle righe 84-87). Se due
utenti salvano lo stesso nome nello stesso momento, passano tutti e due il controllo; al salvataggio l'indice unico
`uq_catalog_map_sectors_property_name` (`property_id`, `lower(name)`, righe non eliminate) blocca il secondo, ma
l'errore del database non è gestito e `GlobalExceptionHandler` (`Boot/Web/ProblemDetailsWriter.cs:34-56`) risponde 500
`server.unexpected`. Il frontend mostra allora un messaggio generico sopra i bottoni invece del nome già usato sotto il
campo.

Cosa serve al frontend: anche in questo caso 409 `catalog.sector_name_taken`, senza `errors[]`, come quando il controllo
trova il nome (il frontend lo mostra già sotto il campo del nome); per `bulk-duplicate` (sezione 3) mai un 500 per il
nome di una copia. Come ottenerlo (riconoscere la violazione dell'indice, riprovare, bloccare...) lo decide lo
sviluppatore backend: l'agente glielo chiede prima di scrivere codice. Il resto del contratto non cambia.

## Verifica

API 1, con uno stabilimento di prova con 12 settori e un token con `catalog.read`:

1. Con `page` 1, `pageSize` 10, `search` `""`, `sortField` `""`: `total` 12, 10 righe nell'ordine di
   `GET /api/catalog/sectors`, ognuna con solo `idSector` e `sectorName` (gli `id` e i `name` di quella lista); `page` 2
   dà le ultime 2; `page` 3 dà `rows` vuoto e `total` 12.
2. `sortField` `sectorName` con settori "Fila 1", "Fila 2", "Fila 9", "Fila 10", "Fila 11", "fila 3": righe in
   ordine naturale, Fila 1, Fila 2, fila 3, Fila 9, Fila 10, Fila 11; con `Descending` l'ordine opposto; "Fila 10" non
   viene mai prima di "Fila 2".
3. `search` `fila` trova i settori che hanno "Fila", "fila" o "FILA" nel nome, e `total` conta solo quelli; `search`
   `%` trova solo i nomi che contengono il carattere `%`.
4. Un settore eliminato (`deleted_at` valorizzato) non compare e non conta in `total`.
5. Un settore di un altro stabilimento non compare mai.
6. `pageSize` 25, `page` 0, `sortField` `name`, `sortDirection` `asc` o `filters` con un elemento: 400
   `validation.failed` con `validation.invalid` sul campo; `search` di 101 caratteri: 400 con `validation.max_length` su
   `search`.
7. Senza token 401 `auth.unauthenticated`; senza `catalog.read` 403 `auth.forbidden`; token senza stabilimento 403
   `catalog.property_required`.

API 2 e 3, con un token con `catalog.settings`:

1. `bulk-delete` di due settori non usati: 204; non compaiono più in `sectors-list` né in `GET /api/catalog/sectors`, e
   nel registro ci sono due voci `catalog.sector_deleted`. Con un solo id non usato: 204 e una voce.
2. `bulk-delete` di un settore non usato e di uno usato da una postazione: 409 `catalog.sector_in_use` con `args` del
   settore usato, `sectorName` compreso; nessuno dei due è eliminato.
3. `bulk-delete` o `bulk-duplicate` con un id di un altro stabilimento o già eliminato: 404 `catalog.sector_not_found`
   e nessun settore cambia.
4. `bulk-duplicate` di due settori: 204; in fondo a `GET /api/catalog/sectors` ci sono due settori nuovi, nell'ordine
   di `idSectors`, con nomi diversi da tutti gli altri dello stabilimento e al massimo 100 caratteri; la mappa, i
   listini, i limiti e gli sconti non cambiano; nel registro due voci `catalog.sector_created`.
5. `bulk-duplicate` dello stesso settore due volte di seguito: due copie con nomi diversi.
6. `idSectors` vuoto: 400 `validation.failed` con `validation.required` su `idSectors`; senza `catalog.settings` 403
   `auth.forbidden`.
7. `bulk-delete` con un solo id di un settore usato da una postazione: 409 `catalog.sector_in_use` con `sectorName` e
   i conteggi; il settore non è eliminato.

Salvataggi contemporanei:

1. Due `POST /api/catalog/sectors` con lo stesso nome (o con nomi che differiscono solo per maiuscole e spazi ai lati)
   mandati insieme: uno risponde 201, l'altro 409 `catalog.sector_name_taken`, mai 500; nello stabilimento c'è un solo
   settore con quel nome.
2. Lo stesso con un `PUT /api/catalog/sectors/{id}` che rinomina un settore mentre un `POST` crea lo stesso nome.
