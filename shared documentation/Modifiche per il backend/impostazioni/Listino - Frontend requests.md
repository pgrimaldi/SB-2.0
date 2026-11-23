# Listino - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Listino** del gestionale funzioni come la vuole il frontend.
Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload, risposta,
regole ed errori attesi.

La pagina è `/settings/catalog`, con due tab: "Pacchetti" (`/settings/catalog/1`) e "Articoli/Servizi aggiuntivi"
(`/settings/catalog/2`). Ognuna ha una tabella (cestino, duplica e matita per riga, cancella e duplica selezionati) e
un pannello laterale per aggiungere e uno per modificare. I pannelli dei pacchetti hanno le tab "Info Generali" e
"Listino"; quelli degli articoli aggiuntivi hanno i loro campi e la stessa tab "Listino" (prezzi stagionali, prezzi per
periodo, festivi, prezzi intermedi).

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/price-lists/packages-list` | Tabella Pacchetti, con il nome nella lingua dell'utente |
| 2 | POST | `/api/catalog/price-lists/delete-package-item` | Elimina uno o più pacchetti, tutti o nessuno |
| 3 | POST | `/api/catalog/price-lists/duplicate-package-item` | Duplica uno o più pacchetti, tutti o nessuno |
| 4 | POST | `/api/catalog/sectors/sector-area-combobox` | Tendina "Aree abilitate" dei pannelli dei pacchetti |
| 5 | POST | `/api/catalog/price-lists/create-package` | Crea un pacchetto con le due tab del pannello |
| 6 | GET | `/api/catalog/price-lists/get-package` | Legge un pacchetto per il pannello di modifica |
| 7 | POST | `/api/catalog/price-lists/update-package` | Salva un pacchetto modificato |
| 8 | POST | `/api/catalog/additional-services-articles-list` | Tabella Articoli/Servizi aggiuntivi |
| 9 | POST | `/api/catalog/delete-additional-services-article-item` | Cancella uno o più articoli aggiuntivi, tutti o nessuno |
| 10 | POST | `/api/catalog/duplicate-additional-services-article-item` | Duplica uno o più articoli aggiuntivi, tutti o nessuno |
| 11 | POST | `/api/catalog/package-combobox` | Tendina "Pacchetti" dei pannelli degli articoli aggiuntivi |
| 12 | POST | `/api/catalog/article-combobox` | Tendina "Seleziona articolo" (anche Impostazioni magazzino) |
| 13 | POST | `/api/catalog/create-additional-services-article-item` | Crea un articolo aggiuntivo |
| 14 | GET | `/api/catalog/get-additional-services-article-item` | Legge un articolo aggiuntivo per il pannello di modifica |
| 15 | POST | `/api/catalog/update-additional-services-article-item` | Salva un articolo aggiuntivo modificato |
| 16 | GET + POST | Le API 6, 7, 14 e 15 | Controllo di versione: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |
| 17 | GET | `/api/catalog/price-lists/get-package-articles` | Gli articoli dei pannelli dei pacchetti e quali ci sono in magazzino |

Sono tutte **nuove**. Per il frontend sostituiscono le API di `/api/catalog/price-lists`, che lavorano con
`SavePriceListRequest` e `PriceListDto`: ogni sezione dice campo per campo cosa cambia rispetto a quelle.

## Regole comuni

- **Le API esistenti non cambiano**: `/api/catalog/price-lists` (con le sue sotto-rotte) e
  `GET /api/catalog/inventory-items/lookup` restano come sono; le nuove si aggiungono e possono riusarne la logica
  (`PriceListsService`).
- **Stabilimento dal token**: nessuna richiesta lo contiene.
- **Permessi**: come le API dei listini di oggi, salvo dove la sezione dice altro. Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Date: giorni in UTC ISO 8601
  (`"2026-06-01T00:00:00.000Z"`). Enum: i nomi .NET (es. `Daily`, `SaturdaySunday`).
- **Lingua**: il frontend manda sempre `Accept-Language` con la lingua dell'utente (`it`, `en`, `fr`, `es`, `de`,
  `zh`, `ar`).
- **Errori** nel formato Problem Details del backend, con `code`; gli errori di un campo in `errors[]` con i nomi del
  corpo di questo documento (es. `translations[0].packageName`). Senza token valido: 401 `auth.unauthenticated`.
- **Controllo di versione**: le letture dei pannelli di modifica (sezioni 6 e 14) restituiscono `version` e i loro
  salvataggi (sezioni 7 e 15) la ricevono in `If-Match` e rispondono con `ETag` (sezione 16).

## 1. POST /api/catalog/price-lists/packages-list

Tabella della tab "Pacchetti" (`/settings/catalog/1`). Per il frontend sostituisce
`GET /api/catalog/price-lists?kind=package`, che ha la stessa funzione con un'altra richiesta (parametri nell'indirizzo),
un'altra risposta (`{ total, page, pageSize, items }` con `PriceListListItemDto`) e il solo nome italiano.

- **API nuova, accanto a quelle di oggi**: gruppo `/price-lists` (`Endpoints/PriceListEndpoints.cs:19`), servizio
  `PriceListsService`, repository `PriceListRepository`. Il nome della rotta non si confonde con `/{id:guid}`, perché non è
  un UUID. `GET /api/catalog/price-lists` resta com'è.
- **Righe**: i listini con `kind` `package` dello stabilimento (`catalog.pricing_price_lists`); quelli eliminati non
  compaiono e non contano in `total` (filtro globale delle entità `ISoftDeletable`).
- **Permesso**: `CatalogRead`, come la `GET` di oggi (`PriceListEndpoints.cs:16`). **Stabilimento** dal token
  (`CurrentProperty.IdAsync`).

### Richiesta

Il corpo è la richiesta standard delle tabelle del frontend, uguale per tutte le tabelle del gestionale (per esempio
`POST /api/catalog/services/bar-service-list` di `Servizi - Frontend requests.md`, sezione 2):

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ombrellone",
  "filters": [],
  "sortField": "packageName",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number (intero) | Pagina richiesta, da 1. |
| `pageSize` | number (intero) | Righe per pagina: 10, 20, 50 o 100. Non valgono gli altri valori di `CatalogQueryRules.PageSizes` (25, 200, 300). |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Il frontend lo manda senza spazi ai lati, da 3 caratteri in su. Trova i pacchetti il cui nome mostrato (`packageName`) contiene il testo, senza distinguere maiuscole e minuscole; `%` e `_` contano come caratteri normali (`LikePattern.Contains`, come `PriceListRepository.ListAsync`). Al massimo 100 caratteri (`CatalogQueryRules.SearchMaxLength`). |
| `filters` | array | Sempre `[]`: la tabella non ha filtri. |
| `sortField` | string | `packageName` oppure `""`: senza un ordinamento scelto dall'utente la tabella mostra le righe per nome, come con `packageName` e `Ascending`. |
| `sortDirection` | string | `Ascending` o `Descending`. Con `sortField` `""` il frontend manda `Ascending`. |

Valori non validi: 400 `validation.failed` con un errore `validation.invalid` sul campo: `page` (meno di 1), `pageSize` (non
10, 20, 50 o 100), `sortField`, `sortDirection` (non `Ascending` o `Descending`), `filters` (un elemento qualsiasi).
`search` oltre 100 caratteri: `validation.max_length` su `search`, con `args.max` 100 (e `args.min` 0). Il frontend non
manda questi valori.

### Nome e ordine

- **`packageName` è il nome nella lingua dell'header `Accept-Language`**, che il frontend manda sempre con la lingua
  dell'utente (`it`, `en`, `fr`, `es`, `de`, `zh`, `ar`): per `it` il nome italiano, per le altre lingue il `name` della riga
  di `catalog.pricing_price_list_translations` in quella lingua; se in quella lingua il nome non c'è, la colonna `name`
  (vedi `create-package`, sezione 5). Ricerca e ordinamento lavorano su questo nome. Esempio: pacchetto con solo "Umbrella"
  (en) → un utente italiano vede "Umbrella"; dopo che qualcuno scrive anche "Ombrellone" (it), vede "Ombrellone".
- **Ordine naturale** del nome mostrato: i numeri dentro il nome si confrontano come numeri, non cifra per cifra.
  Maiuscole e minuscole non contano e le lettere accentate stanno accanto a quelle senza accento. A parità di nome, l'id
  interno (come oggi `ThenBy(p => p.Id)`).

  | `sortDirection` | Ordine atteso |
  |---|---|
  | `Ascending` | Cabina 1, Cabina 2, Cabina 10, Gazebo e 2 lettini, Ombrellone, Ombrellone e 2 lettini |
  | `Descending` | Ombrellone e 2 lettini, Ombrellone, Gazebo e 2 lettini, Cabina 10, Cabina 2, Cabina 1 |

  Il semplice `ORDER BY name` di oggi (`PriceListRepository.ListAsync`) non basta: mette "Cabina 10" prima di "Cabina 2".
  Come ottenerlo (per esempio una collation ICU con l'ordine numerico, `und-u-kn-true`) lo decide lo sviluppatore backend,
  come per le tabelle di Impostazioni magazzino e di Servizi: l'agente glielo chiede prima di scrivere codice. Paginazione
  e `total` li fa il database su quell'ordine, anche quando il nome viene da una traduzione.

### Risposta

200: `total` conta tutti i pacchetti dello stabilimento che rispettano la ricerca, `rows` è la pagina richiesta (al massimo
`pageSize` righe), con solo questi campi. Una `page` oltre l'ultima risponde 200 con `rows` vuoto e il `total` vero: il
frontend passa da solo all'ultima pagina che c'è.

```json
{
  "total": 2,
  "rows": [
    { "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "packageName": "Ombrellone e 2 lettini" },
    { "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "packageName": "Ombrellone, sdraio e lettino" }
  ]
}
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `idPackage` | `public_id` | string (UUID) | Non si mostra: serve alle azioni della riga. |
| `packageName` | `name` o traduzione | string | Colonna "Nome". |

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogRead`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Valori non validi"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra ogni errore nel popup di errore della tabella (testo fisso, con "Riprova"), senza il testo del codice.

## 2. POST /api/catalog/price-lists/delete-package-item

**L'unica API con cui il frontend elimina i pacchetti**, dopo la conferma nel popup: il cestino di una riga la chiama con un
id, "Elimina selezione" della tabella (compare con almeno due righe scelte) con tutti gli id scelti. Elimina **uno o più
pacchetti in una sola chiamata, tutto o niente**, come `POST /api/catalog/services/delete-bar-service-item`
(`Servizi - Frontend requests.md`, sezione 7).

### Cosa ci serve

- **Tutto o niente**: se anche un solo pacchetto della lista non si può eliminare, non si elimina nessuno e la risposta è
  l'errore di quel pacchetto. Il popup chiede all'utente di eliminarli insieme: non deve ritrovarsi con una parte eliminata
  e una no.
- **API nuova**: le API di oggi restano come sono e il frontend non le usa. `DELETE /api/catalog/price-lists/{id}` elimina un
  listino solo; `POST /api/catalog/price-lists/bulk-delete` (`DeleteManyAsync`, `BulkResult`) elimina quelli possibili e
  restituisce gli altri come falliti, con un 200: per questa pagina non va bene.
- **Solo pacchetti**: l'id di un articolo aggiuntivo (`kind` `additional`) risponde 404 come un id che non esiste e non si
  elimina niente. La `DELETE` e il `bulk-delete` di oggi non guardano il tipo.
- **Stessi effetti della `DELETE` di oggi** per ogni pacchetto (`RemoveAsync`, `PriceListsService.cs:171`): soft delete, il
  pacchetto esce dagli articoli aggiuntivi a cui è legato, una voce `catalog.price_list_deleted` nel registro per
  pacchetto.
- **L'ultimo pacchetto di un articolo aggiuntivo non si elimina**: oggi `RemoveAsync` toglie il legame anche quando è l'unico,
  e un articolo aggiuntivo senza legami vale per **tutti** i pacchetti (`QuoteService.cs:156-157`): da quel momento si
  venderebbe con qualunque pacchetto, senza che nessuno l'abbia scelto. Con questa API, se un articolo aggiuntivo dello
  stabilimento (non eliminato) è legato **solo** a pacchetti della lista, 409 `pricing.price_list_in_use` (codice nuovo) e
  non si elimina niente. Vale anche con più pacchetti insieme: un articolo legato a due pacchetti si blocca se sono tutti e
  due nella lista, non se ce n'è uno solo. Un articolo senza legami non blocca niente: vale già per tutti.
- **Nessun altro controllo**, come la `DELETE` di oggi: le prenotazioni tengono i loro dati (`BookingLine.PriceListId` non ha
  vincoli e il pacchetto resta in tabella con il soft delete) e anche il pacchetto predefinito si può eliminare; finché
  l'utente non ne segna un altro non c'è un predefinito.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodo nuovo, transazione...), purché il risultato sia questo.
- **Permesso**: `CatalogPricing`, come la `DELETE` di oggi (`PriceListEndpoints.cs:17`). **Stabilimento** dal token.

### Richiesta e risposta

```json
{
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02"]
}
```

- `idPackages`: gli `idPackage` delle righe di `packages-list` (sezione 1), almeno uno. Lo stesso id ripetuto conta una volta.
  La selezione può comprendere righe di più pagine, quindi anche tutti i pacchetti.
- Il frontend non ha un tetto al numero di id. Se ne serve uno (come i 300 di `BulkIdsRequest.MaxIds`), lo decide lo
  sviluppatore backend: l'agente glielo chiede prima di scrivere codice e lo scrive qui, così il frontend lo rispetta.
- Risposta: 204 senza corpo. Dopo il 204 gli id escono dalla selezione e la tabella si ricarica.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogPricing`. |
| 400 | `validation.failed` | `idPackages` assente o vuoto: `validation.required` su `idPackages` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `pricing.price_list_not_found` (`CatalogErrors.PriceListNotFound`) | Un id non è un pacchetto dello stabilimento (anche perché già eliminato, o perché è un articolo aggiuntivo). Non si elimina niente. |
| 409 | `pricing.price_list_in_use` (nuovo) | Un articolo aggiuntivo resterebbe senza pacchetti (sopra). Non si elimina niente. `args`: `priceListName`, il nome del primo pacchetto della lista (nell'ordine degli id) che lo blocca, nella lingua di `Accept-Language`, e `additionalItems`, i nomi (come in tabella, sezione 8) degli articoli aggiuntivi legati a quel pacchetto che resterebbero senza. |

Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con
`pricing.price_list_not_found` toglie gli id dalla selezione e ricarica la tabella. Con `pricing.price_list_in_use` il testo
è "{{priceListName}} non si può eliminare: gli articoli/servizi aggiuntivi {{additionalItems}} resterebbero senza pacchetti
e varrebbero per tutti. Collegali prima ad altri pacchetti, o eliminali.".

```json
{
  "type": "https://errors.summerbooking/pricing/price_list_in_use",
  "title": "pricing.price_list_in_use",
  "status": 409,
  "code": "pricing.price_list_in_use",
  "args": { "priceListName": "Ombrellone famiglia e 4 lettini", "additionalItems": ["Lettino extra famiglia"] },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

```json
{
  "type": "https://errors.summerbooking/pricing/price_list_not_found",
  "title": "pricing.price_list_not_found",
  "status": 404,
  "code": "pricing.price_list_not_found",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

## 3. POST /api/catalog/price-lists/duplicate-package-item

**L'unica API con cui il frontend duplica i pacchetti**, dopo la conferma nel popup: il bottone "Duplica" di una riga la
chiama con un id, "Duplica selezione" della tabella con tutti gli id scelti. Duplica **uno o più pacchetti in una sola
chiamata, tutto o niente**, come `POST /api/catalog/services/duplicate-bar-service-item` (`Servizi - Frontend requests.md`,
sezione 12).

### Cosa ci serve

- **Tutto o niente**, come l'eliminazione: se un pacchetto non si può duplicare non si duplica nessuno.
- **API nuova**: le API di oggi restano come sono e il frontend non le usa. `POST /api/catalog/price-lists/{id}/duplicate`
  duplica un listino solo e risponde 201 con la copia; `POST /api/catalog/price-lists/bulk-duplicate`
  (`DuplicateManyAsync`) duplica quelli possibili e restituisce gli altri come falliti, con un 200: per questa pagina non va
  bene.
- **Solo pacchetti**, come l'eliminazione.
- **Ogni copia è quella che il backend fa già** (`CopyAsync`, `PriceListsService.cs:145`), con queste regole:
  - un pacchetto nuovo dello stesso stabilimento, con un `public_id` nuovo, `created_by_user_id` dell'utente del token e
    **tutti** i dati del pacchetto di partenza, compresi quelli che nascono con la sezione 5 (aree e settori abilitati,
    servizi inclusi, prezzi stagionali ridotti, prezzi intermedi...); **mai predefinito**, né nel gestionale né online;
  - il **nome lo sceglie il backend** con `CopyNames.Next` (`Domain/CopyNames.cs`), fra i nomi dei pacchetti:
    "Ombrellone copia", poi "Ombrellone copia 2"; la copia di una copia riparte dal nome di base; senza distinguere
    maiuscole e minuscole, al massimo 255 caratteri (`PriceList.NameMaxLength`);
  - **anche i nomi tradotti prendono il suffisso della copia**, ognuno nella sua lingua: `en` " copy", `fr` " copie", `es`
    " copia", `de` " Kopie", `zh` " 副本", `ar` " نسخة", con " 2", " 3"... come in italiano. Oggi la copia tiene i nomi
    tradotti dell'originale (`ToRequest` copia `Translations` così come sono), quindi chi usa un'altra lingua vede in
    tabella due righe con lo stesso nome. Per ogni lingua vale la regola di `CopyNames.Next` sui nomi dei pacchetti in quella
    lingua; il numero è il primo libero in quella lingua e può essere diverso da quello italiano;
  - gli articoli aggiuntivi legati al pacchetto di partenza non si legano alla copia, come oggi;
  - nel registro una voce `catalog.price_list_duplicated` per copia.
- **Nomi in contemporanea**: i nomi non hanno un indice unico, quindi due duplicazioni dello stesso pacchetto nello stesso
  momento possono dare due copie con lo stesso nome. Va bene così: l'utente può rinominarle.
- Se lo stesso id compare più volte, si fa una copia sola.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.
- **Permesso**: `CatalogPricing`, come la duplicazione di oggi (`PriceListEndpoints.cs:17`). **Stabilimento** dal token.

### Richiesta, risposta ed errori

```json
{
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"]
}
```

- `idPackages`: come nell'eliminazione (sezione 2), con la stessa nota sul tetto al numero di id.
- Risposta: 204 senza corpo. Dopo il 204 la selezione si svuota (solo per "Duplica selezione") e la tabella si ricarica.
- Errori: gli stessi dell'eliminazione, nello stesso ordine. Con 404 `pricing.price_list_not_found` non si duplica niente.
- **Un pacchetto che non si può copiare** risponde con un errore suo, che dice quale pacchetto e perché, e non si duplica
  niente. Oggi la copia rifà i controlli del salvataggio (`CopyAsync` chiama `ApplyAsync`) e può rispondere 422
  `catalog.vat_rate_not_allowed`, 422 `pricing.period_not_found` o 404 `catalog.sector_not_found`, senza dire di quale
  pacchetto: con più righe scelte l'utente non può saperlo. Codici nuovi (`CatalogErrors`), dopo il 404, sul primo pacchetto che non va nell'ordine degli
  id:

  | Status | `code` | Quando | `args` |
  |---|---|---|---|
  | 422 | `pricing.duplicate_vat_rate_not_allowed` | L'IVA del pacchetto non è più tra le aliquote ammesse (`VatPolicy.ResolveAsync`). | `priceListName`, `rates` |
  | 422 | `pricing.duplicate_period_not_found` | Il pacchetto ha prezzi per un periodo eliminato. | `priceListName` |
  | 422 | `pricing.duplicate_area_not_found` | Il pacchetto ha fra le aree abilitate un'area della mappa eliminata. | `priceListName` |
  | 422 | `pricing.duplicate_sector_not_found` | Il pacchetto ha dati di un settore eliminato: un settore abilitato, un prezzo stagionale, per periodo o stagionale ridotto. | `priceListName` |

  `priceListName` è il nome nella lingua di `Accept-Language`, come in tabella.
- **Un settore eliminato fa sempre fallire la copia**, in qualunque dato del pacchetto. Oggi succede solo con i prezzi per
  periodo: `ToDtoAsync` dà quel settore come UUID vuoto (`PriceListsService.cs:286`) e `ApplyAsync` non lo trova (`:203`);
  i settori abilitati e le righe di `sectorPrices` e `reducedPrices` di un settore eliminato invece si perdono in silenzio
  (`ToDtoAsync` li esclude), e la copia è diversa dall'originale. L'utente risolve modificando e salvando il pacchetto: le
  righe dei settori eliminati si scartano e un settore abilitato eliminato lo toglie lui (sezione 7).
- **Un periodo eliminato fa fallire la copia**, come un settore: eliminato = tutte le sue righe di
  `catalog.pricing_tariff_periods` sono eliminate. I prezzi di un periodo di un'altra stagione, non eliminato, invece **si
  copiano come sono**, come li tiene il salvataggio (sezione 7): oggi `ApplyAsync` li rifiuta con 422
  `pricing.period_not_found`. L'utente risolve salvando il pacchetto, che scarta le righe dei periodi eliminati (sezione 7).
- Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con
  `pricing.price_list_not_found` toglie gli id dalla selezione e ricarica la tabella.

## 4. POST /api/catalog/sectors/sector-area-combobox

Tendina a scelta multipla "Aree abilitate" della tab Info Generali dei pannelli "Aggiungi nuovo pacchetto" e "Modifica
pacchetto", riletta a ogni apertura della tab. Segue la regola di tutte le tendine del frontend: `POST`, senza corpo né
pagine, risposta `[{ id, value }]`, come `POST /api/catalog/sectors/sector-combobox` (`Servizi - Frontend requests.md`,
sezione 25).

### Cosa ci serve

- **Un'API nuova nel gruppo `/api/catalog/sectors`** (`Endpoints/SeasonEndpoints.cs:83`), accanto a `sector-combobox`. Il nome
  non si confonde con `/{id:guid}`, perché non è un UUID. Oggi il backend non ha un elenco delle aree; le API di oggi
  (`/api/catalog/sectors`, `/api/catalog/map`) restano come sono.
- **Le aree sono quelle della mappa dello stabilimento**: le postazioni di `catalog.map_spots` con `is_area` vero
  (`MapSpot.IsArea`: `umbrella_area`, `cabin_area`, `sunbed_area`, `parking_area`), senza le eliminate (filtro globale delle
  entità `ISoftDeletable`), anche quelle senza settore.
- **Ordine naturale** di `value` (i numeri dentro il nome si confrontano come numeri; maiuscole e accenti non contano), come
  `packageName` nella sezione 1.
- **Permesso**: `CatalogRead`, come `GET /api/catalog/sectors`. **Stabilimento** dal token (`CurrentProperty.IdAsync`).
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Richiesta e risposta

`POST` senza corpo. Risposta 200 con un array di `{ id, value }`, vuoto se la mappa non ha aree. Nessun altro campo:

```json
[
  { "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01", "value": "Area lettini" },
  { "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a02", "value": "Area ombrelloni" }
]
```

| Campo | In `catalog.map_spots` | Tipo | Note |
|---|---|---|---|
| `id` | `public_id` | string (UUID) | Quello che i pannelli mandano e leggono in `idSectorAreas`. |
| `value` | `label` | string | L'etichetta dell'area, unica nello stabilimento (`uq_catalog_map_spots_property_label`): non ha traduzioni. |

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogRead`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra l'errore sotto la tendina, con il testo del suo `code`, e rilegge l'elenco quando l'utente prova ad
aprirla.

## Regole comuni dei pannelli dei pacchetti (sezioni 5-7)

I pannelli "Aggiungi nuovo pacchetto" e "Modifica pacchetto" hanno le tab Info Generali e Listino, che si leggono e si salvano
insieme: una sola chiamata per creare, una per leggere, una per salvare.

### Cosa ci serve

- **API nuove nel gruppo `/api/catalog/price-lists`** (`Endpoints/PriceListEndpoints.cs:19`). I nomi non si confondono con
  `/{id:guid}`, perché non sono UUID. `POST /price-lists`, `GET` e `PUT /price-lists/{id}` restano come sono e il frontend non
  le usa: lavorano con `SavePriceListRequest` e `PriceListDto`, che hanno altri nomi e altri formati (nome italiano a parte,
  enum in snake_case, giorni senza ora), valgono anche per gli articoli aggiuntivi, non hanno `version` e non possono
  contenere una parte dei dati del pannello (sezione 5, campo per campo).
- **Solo pacchetti**: lettura e salvataggio accettano solo listini con `kind` `package` dello stabilimento; ogni altro id,
  anche di un articolo aggiuntivo, risponde 404 `pricing.price_list_not_found`. La creazione li fa di questo tipo.
- **Permesso**: `CatalogRead` per la lettura, `CatalogPricing` per creazione e salvataggio (`PriceListEndpoints.cs:16-17`).
  **Stabilimento** dal token (`CurrentProperty.IdAsync`).
- **Registro**: `catalog.price_list_created` e `catalog.price_list_updated`, come oggi.
- **Un solo predefinito**: `isDefault` e `isDefaultOnline` valgono come oggi (`ResetOtherDefaultsAsync`): segnandone uno, gli
  altri pacchetti si spengono.
- **Controllo di versione** su lettura e salvataggio (sezione 16).
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Errori

Nell'ordine in cui si controllano. Le righe 404 `pricing.price_list_not_found` e 409 valgono per lettura e salvataggio.

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso (`CatalogRead` o `CatalogPricing`). |
| 400 | `validation.failed` | Corpo non valido, con un elemento di `errors[]` per campo (tabella qui sotto). |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `pricing.price_list_not_found` | `idPackage` non è un pacchetto dello stabilimento (anche perché già eliminato). Niente cambia. |
| 422 | `catalog.vat_rate_not_allowed` | `vatRate` non è un'aliquota del Paese dello stabilimento, con `args` `{ "rates": [4, 5, 10, 22] }` come oggi (`VatPolicy.ResolveAsync`). Niente cambia. |
| 422 | `pricing.calculation_method_not_found` | Il metodo di `calculationMethod` non è nel catalogo (`CatalogErrors.CalculationMethodNotFound`). Niente cambia. |
| 404 | `catalog.sector_not_found` | Un id di `idSectors` non è un settore dello stabilimento, anche perché eliminato. Niente cambia. |
| 404 | `map.spot_not_found` | Un id di `idSectorAreas` non è un'area della mappa dello stabilimento, anche perché eliminata (`CatalogErrors.MapSpotNotFound`). Niente cambia. |
| 404 | `inventory.item_not_found` | Un `idInventoryItem` di `items` non è un articolo del magazzino dello stabilimento, anche perché eliminato (`CatalogErrors.InventoryItemNotFound`). Niente cambia. |
| 409 | `resource.version_conflict` | `If-Match` manca o non è più la versione del pacchetto (sezione 16). Niente cambia. |

Le righe di prezzo di settori o periodi eliminati non danno errore: si scartano (sezione 7).

Errori di campo del 400, con `field` uguale al nome nel corpo:

| Campo | `code` | Quando |
|---|---|---|
| `translations[i].packageName` | `validation.required` | Vuoto nella lingua di `Accept-Language` (`i` è la sua posizione nell'elenco). |
| `translations[i].packageName` | `validation.max_length` | Oltre 255 caratteri (`PriceList.NameMaxLength`), in qualunque lingua. |
| `maxPeopleQuantity` | `validation.out_of_range` | Fuori da 1-100, con `args.from` e `args.to` (`InclusiveBetween`). |
| `maxAdditionalItemQuantity` | `validation.out_of_range` | Fuori da 0-100, con `args.from` e `args.to`. |
| `items` | `validation.invalid` | Più di 50, una `quantity` fuori da 1-100 o lo stesso `idInventoryItem` due volte, come oggi. |
| `includedServices` | `validation.max_length` | Oltre 1.000 caratteri. |
| `vatRate` | `validation.invalid` | Non maggiore di 0 e minore di 100 (`ContentRules.Vat`). |
| `calculationMethod`, `monthlySetting`, `weekendSetting` | `validation.invalid` | Valore fuori dal suo enum. |
| `sectorPrices` | `validation.invalid` | Un prezzo fuori da 0-1.000.000 (`SavePriceListRequestValidator.MaxPrice`), o lo stesso `idSector` due volte. |
| `periodPrices` | `validation.invalid` | Un prezzo fuori da 0-1.000.000, la stessa coppia `idSector` e `idPeriod` due volte, o più di 500 righe con almeno un prezzo. |
| `reducedSeasonalPrices[i].reducedSeasonalPriceName` | `validation.required` o `validation.max_length` | Vuoto, o oltre 120 caratteri (`ReducedSeason.NameMaxLength`). |
| `reducedSeasonalPrices[i].startsOn` | `validation.required` o `validation.invalid` | Una delle due date assente; oppure giorni in comune con un altro prezzo ridotto del pacchetto. |
| `reducedSeasonalPrices[i].endsOn` | `validation.end_before_start` | Prima di `startsOn`. |
| `reducedSeasonalPrices` | `validation.invalid` | Un prezzo fuori da 0-1.000.000, o lo stesso `idSector` due volte nello stesso prezzo ridotto. |
| `publicHolidayDates` | `validation.invalid` | Un giorno non valido o ripetuto, o più di 100. |
| `intermediatePrices` | `validation.invalid` | Un prezzo fuori da 0-1.000.000, `dayQuantity` fuori da 1-90, un `idPeriod` assente, o la stessa `idPeriod` + `isFirstRow` + `dayQuantity` due volte. |

Il frontend controlla già nome, lunghezze, prezzi massimi, date dei prezzi ridotti e quantità. Mostra sotto il campo gli errori
dei campi di primo livello, `catalog.vat_rate_not_allowed` (sotto IVA, con le aliquote di `rates`) e
`pricing.calculation_method_not_found` (sotto "Calcolo del prezzo"); sopra i bottoni tutti gli altri, con il testo del loro
`code`. Con `pricing.price_list_not_found` ricarica la tabella; con `catalog.sector_not_found` e `map.spot_not_found` rilegge
settori e aree e mostra quelli eliminati come non più disponibili; con `inventory.item_not_found` rilegge gli articoli (sezione
17) e toglie quelli che non ci sono più; con `resource.version_conflict` avvisa e rilegge il pacchetto.

## 5. POST /api/catalog/price-lists/create-package

Bottone "Crea pacchetto" del pannello "Aggiungi nuovo pacchetto" (pagina Listino): il frontend manda in una sola
chiamata tutto il modulo delle due tab (Info Generali e Listino). Per il frontend sostituisce
`POST /api/catalog/price-lists`, che accetta solo `SavePriceListRequest`. Lo stabilimento è quello del token. Il corpo è
quello qui sotto.

```json
{
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" },
    { "language": "fr", "packageName": null },
    { "language": "de", "packageName": null },
    { "language": "es", "packageName": null },
    { "language": "ar", "packageName": null },
    { "language": "zh", "packageName": null }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "items": [
    { "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "quantity": 1 },
    { "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "quantity": 2 }
  ],
  "includedServices": "Cassetta di sicurezza e doccia calda",
  "idSectorAreas": ["9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isDefault": false,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800 }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": []
    }
  ],
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100 }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 15,
      "dailyPrice": 25,
      "weekendPrice": 30,
      "halfDayWeekendPrice": 18,
      "halfDaySevenDaysPrice": 90,
      "sevenDaysPrice": 150,
      "halfDayFourteenDaysPrice": 170,
      "fourteenDaysPrice": 280,
      "halfDayMonthlyPrice": 320,
      "monthlyPrice": 520
    }
  ],
  "vatRate": 22,
  "calculationMethod": "DailyWithWeekend",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": ["2026-06-02T00:00:00.000Z", "2026-08-15T00:00:00.000Z"],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    { "idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35 },
    { "idPeriod": 0, "isFirstRow": false, "dayQuantity": 2, "price": 30 }
  ]
}
```

Modifiche, campo per campo, rispetto a `SavePriceListRequest`:

- `kind`: il frontend non lo manda: questa API crea sempre un pacchetto.
- `translations`: al posto di `name` + `translations`. Arrivano sempre tutte e 7 le lingue con `packageName`, null se
  non scritto. È obbligatorio il nome nella lingua di `Accept-Language`, quella dell'utente, che nel pannello è la
  principale (non sempre l'italiano).
  Oggi il backend vuole obbligatorio il nome italiano (`name`) e non accetta l'italiano tra le `translations`: con
  questa API salva i 7 nomi come arrivano e controlla come obbligatorio solo quello nella lingua di `Accept-Language`.
  Nella colonna `name` (non vuota, usata per ricerca, ordinamento e copie) mette il nome italiano se c'è, altrimenti
  quello nella lingua di `Accept-Language`. Esempio: utente inglese che scrive solo "Umbrella" → si salva, con
  `name` = "Umbrella".
- `maxAdditionalItemQuantity` e `maxPeopleQuantity`: oggi `maxAdditionalItems` e `maxPeople`. Il pannello fa scegliere da 1 a
  15 oppure null; un valore più alto già salvato si legge, si mostra e torna uguale finché l'utente non ne sceglie un altro.
- `items`: gli articoli di magazzino che compongono il pacchetto, come oggi (`items` con `inventoryItemId` + `quantity`), con
  `idInventoryItem` al posto di `inventoryItemId`; `[]` se nessuno. Il pannello ha una riga per ognuno dei 24 articoli
  dell'elenco fisso (sezione 17) e fa scegliere da 1 a 10; un valore più alto già salvato torna uguale finché l'utente non ne
  sceglie un altro. Gli articoli che il pannello non mostra ("Altro", senza codice) si leggono e tornano uguali. Restano com'erano
  anche `isAreaPackage` (falso in creazione) e il tipo con cui gli articoli sono salvati (`PackageItemKind.InventoryItem` o
  `AreaItem`).
- `includedServices`: testo libero (campo "Servizi inclusi (opzionale)"), al massimo 1.000 caratteri, `null` se vuoto. **Va
  aggiunto** al listino: oggi non esiste (colonna nuova di testo, che può essere nulla). I servizi inclusi di oggi
  (`includedServiceIds`, righe `PackageItemKind.IncludedService`) il pannello non li ha: restano com'erano, `[]` in creazione.
- `idSectorAreas`: id delle aree scelte in "Aree abilitate", gli stessi di `POST /api/catalog/sectors/sector-area-combobox`;
  `[]` = nessuna scelta. **Va aggiunto il legame fra pacchetto e aree della mappa** (tabella nuova): oggi esiste solo
  `isAreaPackage` (sì/no), che è un'altra cosa. La lettura dà **tutte le aree salvate, anche quelle eliminate**, che il
  pannello mostra come "Area non più disponibile" perché l'utente le tolga.
- `idSectors`: al posto di `isSectorRestricted` + `sectorIds`. `[]` = tutti i settori e file.
  **Vanno salvati a parte**, non ricavati dalle righe dei prezzi. Oggi `PriceListsService.ApplyAsync` non salva
  `sectorIds` e in lettura i settori abilitati sono quelli con una riga in `sectorPrices`. Esempio: `idSectors` = [Fila 1,
  Fila 2] e prezzi per 10 settori → `GET /api/catalog/price-lists/get-package` risponde `idSectors` = [Fila 1, Fila 2].
- `isOnlineEnabled` e `isOnlineOnly`: al posto di `salesChannel`. Entrambi false = `manager_only`; `isOnlineEnabled`
  true = `manager_and_online`; `isOnlineOnly` true = `online_only`.
- `reducedSeasonalPrices`: i prezzi stagionali ridotti del pacchetto, quanti se ne vogliono (`[]` se nessuno), ognuno
  con nome, periodo e i suoi prezzi per settore (`sectorPrices`: gli stessi campi dei `sectorPrices` del pacchetto,
  "Prezzo stagionale mezza giornata" e "Prezzo stagionale", una riga per settore). I periodi di due prezzi ridotti dello
  stesso pacchetto non possono avere giorni in comune. Oggi il prezzo stagionale ridotto è
  una voce dello stabilimento (`/api/catalog/reduced-seasons`) e nel pacchetto ci sono solo i suoi prezzi
  (`reducedPrices`). Per ogni elemento il backend crea, o riusa, la voce con questo nome e questo periodo, la lega al
  pacchetto e ci salva i prezzi di `sectorPrices`; cambiare nome o date in un pacchetto non cambia la voce degli altri
  pacchetti che la usano. Il frontend li manda in ordine di `startsOn`. Le date sono giorni in UTC ISO 8601
  (`"2026-06-01T00:00:00.000Z"`), come in tutte le API del frontend, al posto di `DateOnly`. **Da aggiungere**:
  - **il prezzo di mezza giornata**: oggi `PriceListReducedPrice` ha un prezzo solo, obbligatorio; servono
    `seasonalHalfDayPrice` e `seasonalPrice`, tutti e due facoltativi, almeno uno per riga;
  - **il preventivo** (`QuoteService`, `PricingSource`): con la mezza giornata usa il prezzo ridotto di mezza giornata, e se
    quello che serve è `null` calcola come senza prezzo ridotto. Deve cercare **fra i prezzi ridotti di quel pacchetto** con
    quelle date: oggi prende la prima voce dello stabilimento con quelle date (`reducedSeasons.FirstOrDefault`), quindi con
    due voci di pari date e nome diverso il prezzo del pacchetto può non trovarsi.
- `sectorPrices`: `idSector` al posto di `sectorId`, il resto uguale.
- **Le righe senza prezzi non si salvano**: il frontend manda una riga di `sectorPrices` per ogni settore e una di
  `periodPrices` per ogni settore e periodo, anche vuote (tutti i prezzi `null`). Oggi una riga vuota si salva
  (`SetSectorPrices`) e conta come uso del settore (`CountUsagesAsync`): nessun settore si potrebbe più eliminare. Vale anche
  per i `sectorPrices` dei prezzi ridotti. Il limite di 500 righe di `periodPrices` (`SavePriceListRequestValidator`) conta
  solo le righe con almeno un prezzo.
- `periodPrices`: `idSector` al posto di `sectorId`. `idPeriod` (la `key` del periodo di `catalog/tariff-periods`) al
  posto di `periodKey`. Prezzi rinominati: `halfDayPrice` (`halfDay`), `dailyPrice` (`daily`), `weekendPrice` (`weekend`),
  `halfDayWeekendPrice` (`halfDayWeekend`), `halfDaySevenDaysPrice` (`halfDay7`), `sevenDaysPrice` (`days7`),
  `halfDayFourteenDaysPrice` (`halfDay14`), `fourteenDaysPrice` (`days14`), `halfDayMonthlyPrice` (`halfDayMonthly`),
  `monthlyPrice` (`monthly`). Non c'è `stayOverrides` (vedi `intermediatePrices`).
- `sectorPrices` e `periodPrices` arrivano vuoti se l'utente non ha aperto la tab Listino: il pacchetto si crea senza
  prezzi.
- `calculationMethod`: tendina "Calcolo del prezzo", null se non scelta. I valori sono i nomi .NET, come in tutti gli
  enum del frontend: `Proportional`, `Daily`, `Logarithmic`, `DailyWithWeekend` (oggi `proportional`, `daily`,
  `logarithmic`, `daily_with_weekend`).
- `vatRate`: il campo "IVA (%)" della tab Listino, number o `null`, come nei servizi (`Servizi - Frontend requests.md`, sezione
  17): in percentuale; `null` = senza aliquota, si salva la natura predefinita (`VatPolicy.ResolveAsync`,
  `VatRules.DefaultExemptNature`), come vuole il vincolo `ck_catalog_pricing_price_lists_vat`. La lettura di un pacchetto
  esente dà `null`; un pacchetto che ha già una natura la mantiene se `vatRate` è ancora `null`.
- `monthlySetting`: tendina "Impostazione del mensile", null se non scelta: `EffectiveMonthDuration` (31 giorni),
  `ActualMonthDuration` (i giorni veri del mese) o `Fixed30Days` (30 giorni), come `PackagePricer.MonthDays`; oggi
  `effective_month_duration`, `actual_month_duration` e `fixed30days`.
- `weekendSetting`: tendina "Impostazione del weekend", null se non scelta: `SaturdaySunday` (sabato e domenica) o
  `SundayOnly` (solo domenica); oggi `saturday_sunday` e `sunday_only`.
- `isPublicHolidaysIncluded`: checkbox nell'intestazione della sezione "Festivi", al posto di `allowsPublicHolidays`.
- `publicHolidayDates`: i giorni della sezione "Festivi" (scelti nel calendario di "Aggiungi festivi", tolti con il
  cestino), al posto di `holidayDates`. Giorni in UTC ISO 8601, in ordine, ognuno una volta sola, `[]` se nessuno.
  Spegnere il checkbox, dopo una conferma, svuota la lista.
- `isIntermediatePricesIncluded`: checkbox nell'intestazione della sezione "Modifica prezzi intermedi". **Va aggiunto** al
  listino (oggi non esiste): vero o falso, falso per i pacchetti di oggi senza prezzi intermedi e vero per gli altri. Spegnerlo,
  dopo una conferma, svuota `intermediatePrices`.
- `intermediatePrices`: i prezzi per numero di giorni scritti nella sezione, solo quelli con un prezzo (`[]` se nessuno), in
  ordine (prima `isFirstRow` true, poi per `idPeriod`, numerico, e `dayQuantity`). Ognuno ha `idPeriod` (la `key` di un periodo
  della stagione corrente), `isFirstRow` (true = "Fila 1", false = "Altre file"), `dayQuantity` (giorni di soggiorno, da 1 a
  90) e `price`. Una sola riga per `idPeriod` + `isFirstRow` + `dayQuantity`. **Sono gli `stayOverrides` di oggi**
  (`catalog.pricing_price_list_stay_prices`, uno per settore, periodo e numero di giorni, fino a 366):
  - "Fila 1" è il primo settore di `catalog/sectors/sector-combobox`, "Altre file" tutti gli altri;
  - **lettura**: "Altre file" ha il prezzo di un numero di giorni solo se è lo stesso in tutti gli altri settori; altrimenti
    non si restituisce. Non si restituiscono nemmeno i giorni oltre 90;
  - **salvataggio**: scrive il prezzo nel primo settore o in tutti gli altri, e toglie quelli che la lettura restituirebbe e
    che non ci sono più. **Quelli che la lettura non restituisce restano com'erano**, a meno che per quel periodo e quei giorni
    non arrivi un prezzo di "Altre file";
  - i prezzi di 7, 14 e 30 giorni il frontend li scrive anche in `sevenDaysPrice`, `fourteenDaysPrice` e `monthlyPrice` delle
    righe di `periodPrices` di quel periodo (della prima fila o delle altre), così i due valori arrivano uguali.
- `isMapAssignable` non arriva: vale solo per gli articoli aggiuntivi (`PriceList.Update`).
- Risposta: 204 senza corpo.
- Errori: quelli delle regole comuni.

## 6. GET /api/catalog/price-lists/get-package?idPackage=…

Apertura del pannello "Modifica pacchetto" (matita della tabella Pacchetti, pagina Listino): il frontend legge tutto il
pacchetto in una sola chiamata e ci riempie il modulo delle due tab. Per il frontend sostituisce
`GET /api/catalog/price-lists/{id}`, che risponde `PriceListDto`. L'id va nella query string (`idPackage`). Lo stabilimento è
quello del token. La risposta ha `idPackage` e gli stessi campi, con gli stessi nomi e valori, che il frontend manda a
`POST /api/catalog/price-lists/create-package` (vedi sopra).

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "items": [
    { "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "quantity": 1 },
    { "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "quantity": 2 }
  ],
  "includedServices": "Cassetta di sicurezza",
  "idSectorAreas": [],
  "idSectors": ["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"],
  "isDefault": true,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800 }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": []
    }
  ],
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100 }
  ],
  "periodPrices": [],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": [],
  "version": "3f9c1a7e"
}
```

- `version`: la versione del pacchetto (stringa opaca, sezione 16), che il frontend rimanda con `update-package`.
- `translations`: solo le lingue con un nome. Il frontend aggiunge le altre vuote.
- `items`: anche gli articoli che il pannello non mostra; `idSectorAreas`: anche le aree eliminate.
- `idSectors`: quelli salvati con `create-package` o `update-package`, non i settori che hanno una riga di prezzo; **anche
  quelli eliminati**, che il pannello mostra come "Settore/fila non più disponibile" perché l'utente li tolga. Così un
  limite non diventa mai "tutti i settori" (`[]`) senza che l'utente lo scelga.
- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sectors/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe di settori o periodi che non mostra (es. di un'altra stagione) le tiene da parte e le
  rimanda invariate con `update-package`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- `reducedSeasonalPrices`: in qualsiasi ordine (il frontend li ordina per `startsOn`). Anche i loro `sectorPrices`
  possono avere solo le righe salvate: il frontend fa una riga per ogni settore e le righe di settori che non conosce le
  tiene senza mostrarle e le rimanda invariate con `update-package`.
- Errori: quelli delle regole comuni, senza i 400.

## 7. POST /api/catalog/price-lists/update-package

Bottone "Salva" del pannello "Modifica pacchetto": il frontend manda tutto il pacchetto, con lo stesso corpo di
`POST /api/catalog/price-lists/create-package` (vedi sopra) più `idPackage`. Il pacchetto prende tutti i valori mandati:
`sectorPrices` e `periodPrices` hanno tutte le righe del pacchetto, anche quelle di settori o periodi che il pannello
non mostra (es. di un'altra stagione), così nessun prezzo salvato si perde; lo stesso per i `sectorPrices` di ogni
prezzo stagionale ridotto. `reducedSeasonalPrices` ha tutti i prezzi ridotti rimasti, in ordine di `startsOn`: quelli
tolti con il cestino non ci sono più e il backend li toglie dal pacchetto. Lo stesso per `publicHolidayDates`: tutti
i giorni festivi rimasti, in ordine; e per `intermediatePrices`: tutti i prezzi intermedi scritti, in ordine (un prezzo
cancellato non c'è più).
Per il frontend sostituisce `PUT /api/catalog/price-lists/{id}`, che accetta solo `SavePriceListRequest`.

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    { "language": "it", "packageName": "Ombrellone e 2 lettini" },
    { "language": "en", "packageName": "Umbrella and 2 sunbeds" }
  ],
  "maxPeopleQuantity": 6
}
```

(nell'esempio solo alcuni campi: arrivano sempre tutti, come nella creazione)

- Header `If-Match: "<version>"` con la `version` letta con `GET /api/catalog/price-lists/get-package` (sezione 16).
- Risposta: 204 senza corpo, con la nuova versione nell'header `ETag` (sezione 16).
- Righe di settori o periodi che non esistono più: non devono far fallire il salvataggio, e nemmeno la creazione. Oggi
  `ApplyAsync` risponde 404 `catalog.sector_not_found` per un settore eliminato e 422 `pricing.period_not_found` per una `key`
  che non è un periodo della stagione corrente. Il backend tiene come arrivano le righe dei periodi di un'altra stagione e
  scarta in silenzio quelle dei settori e dei periodi eliminati (anche dentro i `sectorPrices` dei prezzi stagionali ridotti). Esempio: Fila 9 eliminata dopo l'apertura del pannello → "Salva"
  risponde 204 e la riga di Fila 9 non si salva. Le righe tenute restano legate al loro periodo: se la loro `key` viene
  riusata da un periodo nuovo, non devono finire su quello.
- Un settore eliminato in `idSectors` invece risponde 404 `catalog.sector_not_found`, e un'area eliminata in `idSectorAreas` 404
  `map.spot_not_found`: non si salva niente, l'utente li toglie dalla tendina e salva di nuovo.
- Errori: quelli delle regole comuni. Con 409 `resource.version_conflict` (senza `errors[]`) niente cambia: il frontend non lo
  mostra sopra i bottoni, avvisa e rilegge il pacchetto.

## 8. POST /api/catalog/additional-services-articles-list

Tabella della tab "Articoli/Servizi aggiuntivi" della pagina Listino (`/settings/catalog/2`), come `packages-list`.
Per il frontend sostituisce `GET /price-lists?kind=additional`.

- Corpo: la richiesta standard delle tabelle, con le stesse regole di `packages-list` (`sortField`
  `additionalItemName`).

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "",
  "filters": [],
  "sortField": "",
  "sortDirection": "Ascending"
}
```

- Risposta 200 nello stile delle altre tabelle: `total` e `rows`, con `idAdditionalItem` (oggi `id`) e
  `additionalItemName` (oggi `name`).

```json
{
  "total": 2,
  "rows": [
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "additionalItemName": "Lettino" },
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02", "additionalItemName": "Sdraio" }
  ]
}
```

- Errori come `packages-list`.

## 9. POST /api/catalog/delete-additional-services-article-item

Cestino di una riga e "elimina selezionati" della tabella della tab "Articoli/Servizi aggiuntivi" (pagina Listino),
come `delete-package-item`. Per il frontend sostituisce `DELETE /price-lists/{id}` e `POST /price-lists/bulk-delete`
per gli articoli aggiuntivi: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idAdditionalItems`, gli `idAdditionalItem` delle righe di
  `additional-services-articles-list`; uno solo con il cestino della riga, più con "elimina selezionati".

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02"]
}
```

- **Tutto o niente**: se un id non va bene non si elimina nessuno.
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` per un id che non è un articolo aggiuntivo dello stabilimento (anche un pacchetto)
  o per `idAdditionalItems` vuoto; 403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre tabelle.

```json
{
  "type": "https://errors.summerbooking/resource/not-found",
  "status": 404,
  "code": "resource.not_found",
  "title": "Resource not found"
}
```

## 10. POST /api/catalog/duplicate-additional-services-article-item

Bottone duplica di una riga e "duplica selezionati" della tabella della tab "Articoli/Servizi aggiuntivi" (pagina
Listino), come `duplicate-package-item`. Per il frontend sostituisce `POST /price-lists/{id}/duplicate` e
`POST /price-lists/bulk-duplicate` per gli articoli aggiuntivi: qui arrivano **uno o più id insieme**, tutto o niente.

- Corpo: `idAdditionalItems`, come `delete-additional-services-article-item`; uno solo con il bottone
  della riga, più con "duplica selezionati".

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01"]
}
```

- **Tutto o niente**: se un id non va bene non si duplica nessuno.
- Per ogni articolo una copia con gli stessi dati e un nome scelto dal backend che non si ripete fra gli articoli
  aggiuntivi dello stabilimento, senza distinguere maiuscole e minuscole: per esempio "Lettino (copia)", poi
  "Lettino (copia 2)".
- Risposta: 204 senza corpo.
- Errori: come `delete-additional-services-article-item`.

## 11. POST /api/catalog/package-combobox

Tendina a scelta multipla "Pacchetti" dei pannelli di aggiunta e di modifica degli articoli/servizi aggiuntivi
(pagina Listino), riletta a ogni apertura del pannello. Combobox standard come le altre del frontend (`catalog/sectors/sector-combobox`,
`catalog/article-combobox`): tutti i pacchetti dello stabilimento, senza pagine, nel loro ordine.

- POST senza corpo: lo stabilimento lo prende il backend dal token.
- Risposta 200: un array di `{ id, value }`, con `id` = `idPackage` e `value` = nome del pacchetto, scelto
  come `packageName` di `packages-list` (lingua di `Accept-Language`, altrimenti `name`); array vuoto se non ce ne sono.

```json
[
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "value": "Ombrellone e 2 lettini" },
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "value": "Ombrellone, sdraio e lettino" }
]
```

- Errori: 403 `auth.forbidden` per un utente senza il permesso per questa operazione; 401 `auth.unauthenticated`.

## 12. POST /api/catalog/article-combobox

Tendina obbligatoria "Seleziona articolo" dei pannelli "Aggiungi nuovo articolo/servizio aggiuntivo" e "Modifica
articolo/servizio aggiuntivo" (pagina Listino), riletta a ogni apertura del pannello. È la stessa API della tendina
"Articolo" del popup "Aggiungi articolo" delle Impostazioni magazzino: **si fa una volta sola**, come la descrive la
sezione 2 di `Impostazioni magazzino - Frontend requests.md` (rotta, risposta `[{ id, value }]`, ordine, permesso
`CatalogRead`, errori e verifica).

Per il Listino conta in più: l'`id` scelto è l'`idArticle` che il frontend manda in
`create-additional-services-article-item` e `update-additional-services-article-item` e che legge in
`get-additional-services-article-item`: deve essere lo stesso id.

## 13. POST /api/catalog/create-additional-services-article-item

Bottone "Crea articolo/servizio" del pannello "Aggiungi nuovo articolo/servizio aggiuntivo" (pagina Listino): il
frontend manda tutto il modulo in una sola chiamata. Per il frontend sostituisce `POST /api/catalog/price-lists` con
`kind: "additional"`, che accetta solo `SavePriceListRequest`. Lo stabilimento lo prende il backend dal token.

```json
{
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120 }
  ],
  "periodPrices": [],
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": []
}
```

Modifiche, campo per campo, rispetto a `SavePriceListRequest`:

- `idArticle`: l'articolo di magazzino scelto in "Seleziona articolo" (id di `catalog/article-combobox`),
  obbligatorio, al posto di `items` (che per un articolo aggiuntivo ha un solo elemento). Il nome dell'articolo
  aggiuntivo è quello dell'articolo: il frontend non manda `name` né `translations`.
- `idPackages`: pacchetti scelti in "Pacchetti" (id di `catalog/package-combobox`), `[]` = tutti i pacchetti, al
  posto di `packageLinks` (il frontend non manda `maxCount`).
- `isOnlineEnabled` e `isOnlineOnly` ("Prenotazioni online" e "Solo online"): al posto di `salesChannel`, come in
  `create-package`.
- `isMapAssignable`: uguale.
- `sectorPrices`, `periodPrices`, `calculationMethod`, `isCumulative`, `monthlySetting`, `weekendSetting`,
  `isPublicHolidaysIncluded`, `publicHolidayDates`, `isIntermediatePricesIncluded`, `intermediatePrices`: gli stessi
  campi e valori di `create-package` (vedi sopra, con le stesse differenze).
  `sectorPrices` e `periodPrices` hanno una riga per ogni settore e per ogni settore in ogni periodo della stagione
  corrente.
- Risposta: 204 senza corpo.
- Errori: 404 `resource.not_found` per un `idArticle` che non è un articolo attivo dello stabilimento o un `idPackage`
  che non è dei suoi; 400 `validation.failed` con `errors[]` per i valori fuori dagli enum (es. `calculationMethod`);
  403 `auth.forbidden` e 401 `auth.unauthenticated` come le altre.

## 14. GET /api/catalog/get-additional-services-article-item

Apertura del pannello "Modifica articolo/servizio aggiuntivo" (matita della tabella Articoli/Servizi aggiuntivi,
pagina Listino): il frontend legge tutto l'articolo aggiuntivo in una sola chiamata e ci riempie il modulo. Per il
frontend sostituisce `GET /api/catalog/price-lists/{id}`, che risponde `PriceListDto`. L'id va nella query
(`?idAdditionalItem=...`); lo stabilimento è quello del token. La risposta ha `idAdditionalItem` e gli stessi campi,
con gli stessi nomi e valori, che il frontend manda a `POST /api/catalog/create-additional-services-article-item`
(vedi sopra).

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": ["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    { "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120 }
  ],
  "periodPrices": [],
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": false,
  "publicHolidayDates": [],
  "isIntermediatePricesIncluded": false,
  "intermediatePrices": [],
  "version": "a41e09d2"
}
```

- `version`: la versione dell'articolo aggiuntivo (stringa opaca, sezione 16), che il frontend rimanda con
  `update-additional-services-article-item`.
- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sectors/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe di settori o periodi che non mostra (es. di un'altra stagione) le tiene da parte e le
  rimanda invariate con `update-additional-services-article-item`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- Errori: 404 `resource.not_found` per un articolo aggiuntivo che non è dello stabilimento; 401 `auth.unauthenticated`.

## 15. POST /api/catalog/update-additional-services-article-item

Bottone "Salva" del pannello "Modifica articolo/servizio aggiuntivo": il frontend manda tutto il modulo, con lo stesso
corpo di `POST /api/catalog/create-additional-services-article-item` (vedi sopra) più `idAdditionalItem`. L'articolo
aggiuntivo prende tutti i valori mandati: `sectorPrices` e `periodPrices` hanno tutte le sue righe, anche quelle di
settori o periodi che il pannello non mostra (es. di un'altra stagione), così nessun prezzo salvato si perde.
`publicHolidayDates` ha tutti i giorni festivi rimasti, in ordine: quelli tolti con il cestino non ci sono più;
`intermediatePrices` ha tutti i prezzi intermedi scritti, in ordine.
Per il frontend sostituisce `PUT /api/catalog/price-lists/{id}`, che accetta solo `SavePriceListRequest`.

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "isOnlineOnly": true
}
```

(nell'esempio solo alcuni campi: arrivano sempre tutti, come nella creazione)

- Header `If-Match: "<version>"` con la `version` letta con `get-additional-services-article-item` (sezione 16).
- Risposta: 204 senza corpo, con la nuova versione nell'header `ETag` (sezione 16).
- Righe di settori o periodi che non esistono più: non devono far fallire il salvataggio. Oggi `ApplyAsync` risponde
  404 `catalog.sector_not_found` per un settore eliminato e 422 `pricing.period_not_found` per una `key` che non è un
  periodo della stagione corrente. Il backend tiene come arrivano le righe dei periodi di un'altra stagione e scarta in
  silenzio quelle dei settori e dei periodi eliminati. Esempio: Fila 9 eliminata dopo l'apertura del pannello → "Salva"
  risponde 204 e la riga di Fila 9 non si salva. Le righe tenute restano legate al loro periodo: se la loro `key` viene
  riusata da un periodo nuovo, non devono finire su quello.
- Errori: come `POST /api/catalog/create-additional-services-article-item`, più 404 `resource.not_found` per un
  `idAdditionalItem` che non è dello stabilimento e 409 `resource.version_conflict`, senza `errors[]`, quando
  `If-Match` manca o non è più la versione dell'articolo aggiuntivo (sezione 16): niente cambia. Il frontend non lo
  mostra sopra i bottoni: avvisa e rilegge l'articolo aggiuntivo.

## 16. Controllo di versione sui salvataggi di modifica

Senza un controllo, se due utenti modificano lo stesso pacchetto o lo stesso articolo aggiuntivo vince chi salva per
ultimo: i pannelli di modifica rimandano tutto quello che hanno letto all'apertura (sezioni 7 e 15) e cancellano,
senza che nessuno se ne accorga, quello che un altro utente ha cambiato nel frattempo. Il backend non ha un controllo
di concorrenza sugli aggiornamenti dei listini: nessun token di concorrenza in `PriceListConfiguration`, nessun
`If-Match`. Il frontend chiede lo schema di **concorrenza ottimistica** che il backend ha già progettato per le
prenotazioni offline (`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un
**codice di errore unico per tutte le API**: `resource.version_conflict`. Lo stesso schema e lo stesso codice sono
chiesti in `Impostazioni magazzino - Frontend requests.md` (sezione 7): se lì sono già fatti, qui si aggiungono solo
le API della tabella sotto.

### Cosa ci serve

- **Versione nelle letture**: ogni lettura della tabella sotto restituisce, insieme ai dati, il campo `version`
  (stringa opaca). Cambia ogni volta che cambiano i dati che il salvataggio corrispondente scrive, anche quando li
  cambia un'altra API (per esempio `PUT /api/catalog/price-lists/{id}`), e solo allora.
- **`If-Match` nei salvataggi**: il frontend manda la versione letta nell'header `If-Match: "<version>"`. Se non è più
  quella attuale, il backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza
  `If-Match` risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nelle risposte**: ogni salvataggio riuscito restituisce la nuova versione nell'header `ETag: "<version>"`,
  anche se risponde 204 senza corpo.
- Il 409 `resource.version_conflict` si controlla dopo il 404 dell'elemento (`pricing.price_list_not_found` per i pacchetti,
  `resource.not_found` per gli articoli aggiuntivi) e prima degli altri 409.
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  dei dati coperti dalla versione, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta
  la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/catalog/price-lists/get-package` (sezione 6) | `POST /api/catalog/price-lists/update-package` (sezione 7) | Tutto il pacchetto: tutti i campi della risposta della sezione 6, nomi, prezzi e giorni festivi compresi |
| `GET /api/catalog/get-additional-services-article-item` (sezione 14) | `POST /api/catalog/update-additional-services-article-item` (sezione 15) | Tutto l'articolo aggiuntivo: tutti i campi della risposta della sezione 14, prezzi e giorni festivi compresi |

Le altre API di questo documento non ne hanno bisogno: le creazioni (sezioni 5 e 13) non rimandano dati letti,
cancellazione e duplicazione (sezioni 2, 3, 9 e 10) lavorano solo con gli id.

### Cosa fa il frontend

Con 409 `resource.version_conflict` i pannelli "Modifica pacchetto" e "Modifica articolo/servizio aggiuntivo" mostrano
l'avviso "Nel frattempo un altro utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e
rileggono i dati come all'apertura (sezione 6 o 14, con settori e periodi). Dopo un salvataggio riuscito il pannello
si chiude, quindi la versione dell'`ETag` non la usa.

## 17. GET /api/catalog/price-lists/get-package-articles

Gli articoli delle righe "Articoli che compongono il pacchetto" dei pannelli "Aggiungi nuovo pacchetto" e "Modifica pacchetto",
letti **a ogni apertura del pannello**: dice subito quali articoli ci sono in magazzino e quali no. Le righe degli articoli che
non ci sono si vedono, spente, con la scritta "Non in magazzino".

### Cosa ci serve

- **Un'API nuova nel gruppo `/api/catalog/price-lists`** (`Endpoints/PriceListEndpoints.cs:19`). `GET /inventory-articles` e
  `GET /inventory-items/lookup` restano come sono: vogliono `InventoryRead`, che chi gestisce il listino può non avere, e non
  danno le quantità.
- **Tutti i 24 articoli dell'elenco fisso** (`InventoryArticle`), sempre, nell'ordine dell'enum, con il nome .NET; per ognuno
  gli articoli del magazzino dello stabilimento con quel codice (`catalog.inventory_items`, `article_code`), senza gli
  eliminati, in ordine naturale di nome; `[]` se non ce ne sono. Quelli senza codice ("Altro") non ci sono.
- **Permesso**: `CatalogRead`. **Stabilimento** dal token (`CurrentProperty.IdAsync`).
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Risposta

200 con un array di 24 elementi (nell'esempio solo tre):

```json
[
  {
    "article": "Umbrella",
    "items": [
      { "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "itemName": "Ombrellone", "totalQuantity": 60 }
    ]
  },
  {
    "article": "Sunbed",
    "items": [
      { "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "itemName": "Lettino", "totalQuantity": 120 }
    ]
  },
  { "article": "SunbedMaxi", "items": [] }
]
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `article` | `InventoryArticle` | string (enum) | Il frontend mostra il nome dell'articolo nella lingua dell'utente. |
| `items` | `catalog.inventory_items` | array | Una riga del pannello per ognuno; `[]` = l'articolo non è in magazzino. Con più di uno il pannello aggiunge il nome. |
| `items[].idInventoryItem` | `public_id` | string (UUID) | Quello di `items` nella sezione 5. |
| `items[].itemName` | `name` | string | Il nome in magazzino, così com'è: non ha traduzioni. |
| `items[].totalQuantity` | `total_quantity` | number (intero) | 0 = nessun pezzo: la riga è come quella di un articolo che non c'è. |

### Errori

401 `auth.unauthenticated`, 403 `auth.forbidden` (senza `CatalogRead`), 403 `catalog.property_required`, 404
`catalog.property_not_found`. Se fallisce nel pannello di modifica vale la regola delle sue letture (si chiude, avviso con
"Riprova"); in quello di aggiunta il messaggio compare sopra i bottoni e si riprova alla prossima apertura della tab.

## API esistenti usate così come sono

I pannelli dei pacchetti e degli articoli aggiuntivi leggono i periodi della stagione corrente con
`GET /api/catalog/tariff-periods` (`key`, `name`, `ranges`): la `key` di ogni periodo è l'`idPeriod` dei prezzi per periodo
e dei prezzi intermedi. Per il Listino **non va cambiata**: la `version` chiesta in `Stagioni e periodi - Frontend
requests.md` (sezione 6) serve solo a quella pagina, e il Listino non la usa. Vale anche qui la sezione 3 di quel documento:
la `key` di un periodo eliminato non si riusa, altrimenti i prezzi salvati passerebbero al periodo nuovo.

## Verifica

- `package-combobox` dà il nome nella lingua di `Accept-Language`, altrimenti quello di `name`.
- `create-package` con `Accept-Language: en` e solo il nome inglese risponde 204; `name` = il nome inglese. Senza il
  nome nella lingua di `Accept-Language` risponde 400 `validation.failed` su `translations[i].packageName`.
- `idSectors` salvati con prezzi per tutti i settori tornano uguali in `GET /api/catalog/price-lists/get-package`, anche se uno
  è stato eliminato dopo; rimandarlo con `update-package` risponde 404 `catalog.sector_not_found`.
- `update-package` e `update-additional-services-article-item` con una riga di un settore eliminato, o di un periodo
  di un'altra stagione, rispondono 204: la prima non si salva, la seconda resta com'era.
- I salvataggi rispondono 204 senza corpo; elimina e duplica fanno tutti o nessuno.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.

API 5-7, con un token con `CatalogRead` e `CatalogPricing`:

- `create-package` con il corpo della sezione 5: 204; il pacchetto compare in `packages-list` e `get-package` restituisce gli
  stessi dati. Nel registro una voce `catalog.price_list_created`; dopo `update-package` una `catalog.price_list_updated`.
- `get-package-articles`: 24 elementi nell'ordine di `InventoryArticle`; un articolo senza righe in magazzino ha `items` `[]`, uno
  con due righe le ha tutte e due in ordine di nome, una riga eliminata o senza codice non compare.
- `items` di un pacchetto con un articolo "Altro": la lettura lo dà e rimandato uguale resta; un id sconosciuto: 404
  `inventory.item_not_found`.
- `vatRate` 22: salvato e riletto; `null`: 204 e la lettura dà `null`; 7 in Italia: 422 `catalog.vat_rate_not_allowed` con
  `args.rates`.
- `includedServices` con un testo: salvato e riletto; i servizi inclusi salvati con `PUT /price-lists/{id}` non cambiano.
- `idSectorAreas` con due aree: salvate e rilette; eliminata una dalla mappa, la lettura la dà ancora e rimandarla risponde 404
  `map.spot_not_found`.
- Un prezzo ridotto con i due prezzi: salvati e riletti; il preventivo di mezza giornata nelle sue date usa quello di mezza
  giornata. Due pacchetti con voci di pari date e nome diverso: ognuno ha il suo prezzo.
- Righe di `sectorPrices` e `periodPrices` con tutti i prezzi `null`: 204, la lettura non le dà e il settore si può eliminare.
- Prezzi intermedi: "Altre file" a 5 giorni scritto: salvato in tutti gli altri settori. Un pacchetto con un prezzo a 5 giorni
  diverso fra gli altri settori e uno a 120 giorni: la lettura non li dà e un salvataggio non li cambia.
- `monthlySetting` `ActualMonthDuration`: salvato e riletto. `maxPeopleQuantity` 40 già salvato: riletto e rimandato uguale.
- Con `isDefault` vero su un secondo pacchetto il primo non è più predefinito; lo stesso con `isDefaultOnline`.
- Ogni riga della tabella degli errori di campo: 400 con quel `field` e quel `code`; mai un 500 per un vincolo del database.
- `get-package` e `update-package` con l'id di un articolo aggiuntivo, di un altro stabilimento o di un pacchetto eliminato: 404
  `pricing.price_list_not_found`.
- Lettura senza `CatalogRead`, creazione e salvataggio senza `CatalogPricing`: 403 `auth.forbidden`; senza token 401
  `auth.unauthenticated`; token senza stabilimento 403 `catalog.property_required`.

API 16, con un pacchetto e un articolo aggiuntivo di prova:

1. `GET /api/catalog/price-lists/get-package` risponde con `version` (per esempio `v1`); `update-package` con
   `If-Match: "v1"`: 204 con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `GET` risponde con
   quella versione.
2. Lo stesso `update-package` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; il
   pacchetto non cambia.
3. `GET` (versione `v1`), poi `PUT /api/catalog/price-lists/{id}` sullo stesso pacchetto, poi `update-package` con
   `If-Match: "v1"`: 409 `resource.version_conflict`; resta quello che ha salvato la `PUT`.
4. `update-package` senza `If-Match`: 409 `resource.version_conflict`; con un `idPackage` di un altro stabilimento:
   404 `pricing.price_list_not_found`, anche con un `If-Match` sbagliato.
5. Gli stessi casi con `get-additional-services-article-item` e `update-additional-services-article-item` (404
   `resource.not_found`).

API 1, con uno stabilimento di prova e il token di un utente con `CatalogRead`:

6. `packages-list` con 12 pacchetti e 3 articoli aggiuntivi, `page` 1 e `pageSize` 10: `total` 12 e 10 righe con solo
   `idPackage` e `packageName`; `page` 2 dà le ultime 2; `page` 3 dà `rows` vuoto e `total` 12. Un pacchetto eliminato o di
   un altro stabilimento non compare.
7. Con "Cabina 10", "Cabina 2", "Ècru" e "Gazebo" e `sortField` `packageName`: con `Ascending` Cabina 2, Cabina 10, Ècru,
   Gazebo; con `Descending` l'inverso. Con `sortField` `""` lo stesso ordine di `Ascending`.
8. Un pacchetto "Ombrellone" con la traduzione inglese "Umbrella": con `Accept-Language: en` la riga ha "Umbrella" e
   `search` "umbr" la trova, "ombr" no; con `Accept-Language: fr` ha "Ombrellone".
9. `search` "50%" non trova "Sconto 500"; `search` di 101 caratteri: 400 con `validation.max_length` su `search`.
   `pageSize` 25, `page` 0, `sortField` `name`, `sortDirection` `asc` o `filters` con un elemento: 400 `validation.failed`
   con `validation.invalid` sul campo.
10. Senza token 401 `auth.unauthenticated`; senza `CatalogRead` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`.

API 2 e 3, con un token con `CatalogPricing`:

11. `delete-package-item` di due pacchetti: 204; non compaiono più in `packages-list` e nel registro ci sono due voci
    `catalog.price_list_deleted`. Con un solo id, anche ripetuto: 204 e una voce.
12. Un pacchetto dello stabilimento e uno di un altro stabilimento, già eliminato, o un articolo aggiuntivo: 404
    `pricing.price_list_not_found`; nessuno è eliminato. Lo stesso con `duplicate-package-item`: nessuna copia.
    Un articolo aggiuntivo legato solo a "Ombrellone", che si elimina da solo o con altri: 409 `pricing.price_list_in_use` con
    `priceListName` "Ombrellone" e `additionalItems` con il suo nome; nessun pacchetto è eliminato. Legato a "Ombrellone" e
    "Gazebo": eliminarli insieme dà 409; eliminare solo "Gazebo" dà 204 e resta il legame con "Ombrellone", che da quel
    momento dà 409. Eliminato l'articolo aggiuntivo, o legato a un altro pacchetto: 204. Un articolo senza legami non
    blocca.
13. `duplicate-package-item` di "Ombrellone" (traduzione inglese "Umbrella", predefinito, con prezzi): 204; c'è un
    pacchetto nuovo "Ombrellone copia" / "Umbrella copy" con gli stessi dati e non predefinito; "Ombrellone" resta
    predefinito; nel registro una voce `catalog.price_list_duplicated`. Di nuovo: "Ombrellone copia 2" e "Umbrella copy 2".
    La copia di "Ombrellone copia" è "Ombrellone copia 3".
14. Un pacchetto con un'IVA che non è più tra le aliquote ammesse, da solo o insieme a uno valido: 422
    `pricing.duplicate_vat_rate_not_allowed` con `priceListName` e `rates`, e nessuna copia. Un pacchetto con un settore
    abilitato, un prezzo stagionale, per periodo o stagionale ridotto di un settore eliminato: 422
    `pricing.duplicate_sector_not_found` con `priceListName`, e nessuna copia. Con un'area abilitata eliminata: 422
    `pricing.duplicate_area_not_found`. Con un prezzo per un periodo eliminato: 422
    `pricing.duplicate_period_not_found`. Dopo un `update-package` di quel pacchetto (senza il settore eliminato in
    `idSectors`) la duplicazione riesce. Un pacchetto con un prezzo per un periodo di un'altra stagione, non eliminato: 204, e
    la copia ha quel prezzo.
15. `idPackages` vuoto o assente: 400 `validation.failed` con `validation.required` su `idPackages`. Senza token 401
    `auth.unauthenticated`; senza `CatalogPricing` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`.

API 4, con una mappa con due aree ("Area 10" e "Area 2"), una terza eliminata e altre postazioni:

16. `sector-area-combobox` senza corpo: 200 con `[Area 2, Area 10]`, ognuna con solo `id` (il `public_id` della postazione) e
    `value`. Uno stabilimento senza aree: 200 con `[]`. Senza token 401 `auth.unauthenticated`; senza `CatalogRead` 403
    `auth.forbidden`; token senza stabilimento 403 `catalog.property_required`.
