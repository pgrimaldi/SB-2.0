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
| 8 | POST | `/api/catalog/price-lists/additional-services-articles-list` | Tabella Articoli/Servizi aggiuntivi, con il nome nella lingua dell'utente |
| 9 | POST | `/api/catalog/price-lists/delete-additional-services-article-item` | Elimina uno o più articoli aggiuntivi, tutti o nessuno |
| 10 | POST | `/api/catalog/price-lists/duplicate-additional-services-article-item` | Duplica uno o più articoli aggiuntivi, tutti o nessuno |
| 11 | POST | `/api/catalog/price-lists/package-combobox` | Tendina "Pacchetti" dei pannelli degli articoli aggiuntivi |
| 12 | POST | `/api/catalog/article-combobox` e `/api/catalog/sectors/sector-combobox` | Richieste in altri documenti |
| 13 | POST | `/api/catalog/price-lists/create-additional-services-article-item` | Crea un articolo aggiuntivo |
| 14 | GET | `/api/catalog/price-lists/get-additional-services-article-item` | Legge un articolo aggiuntivo per il pannello di modifica |
| 15 | POST | `/api/catalog/price-lists/update-additional-services-article-item` | Salva un articolo aggiuntivo modificato |
| 16 | GET + POST | Le API 6, 7, 14 e 15 | Controllo di versione: `version` nelle letture, `If-Match` ed `ETag` nei salvataggi, 409 `resource.version_conflict` |
| 17 | GET | `/api/catalog/price-lists/get-package-articles` | Gli articoli dei pannelli dei pacchetti e quali ci sono in magazzino |
| 18 | POST | `/api/catalog/quotes` | Esistente: un listino solo online non si vende dal gestionale |

Sono tutte **nuove**, tranne la 18 (il preventivo, che esiste già). Per il frontend sostituiscono le API di `/api/catalog/price-lists`, che lavorano con
`SavePriceListRequest` e `PriceListDto`: ogni sezione dice campo per campo cosa cambia rispetto a quelle.

## Regole comuni

- **Le API esistenti non cambiano**: `/api/catalog/price-lists` (con le sue sotto-rotte) tiene rotte, richieste e risposte di
  oggi; le nuove si aggiungono e possono riusarne la logica (`PriceListsService`). **Ma non devono perdere i dati nuovi** delle
  sezioni 5 e 13:
  - `PUT /price-lists/{id}` lascia com'erano i dati che non conosce (`isOnlineEnabled` di un listino solo online, aree, testo
    dei servizi inclusi, `isIntermediatePricesIncluded`, prezzo ridotto di mezza giornata, legame fra pacchetto e prezzi
    ridotti) e `{id}/duplicate` e `bulk-duplicate` li copiano;
  - `isSectorRestricted` e `sectorIds` leggono e scrivono i settori abilitati salvati a parte (sezione 5);
  - un prezzo ridotto con solo la mezza giornata non si perde e non diventa 0: oggi `ReducedPriceDto.Price` non può essere nullo
    (`PriceListDtos.cs:37`) e `SetReducedPrices` toglie le righe che la `PUT` non manda (`PriceList.cs`);
  - lo snapshot offline `price_catalog` (`ConfigSnapshotBuilder.PriceCatalogAsync`) contiene quello che serve al preventivo
    (settori abilitati, prezzo ridotto di mezza giornata) e si rigenera quando cambia: `SnapshotTriggers` guarda il nome
    dell'entità, che oggi deve cominciare con `PriceList` o `ReducedSeason`.
- **Dati già salvati**: la migrazione della sezione 5 ("Dati già salvati") non cambia i prezzi e le vendite di oggi. Le vendite
  cambiano solo con le correzioni chieste: il prezzo stagionale (sezione 5) e i listini solo online (sezione 18).
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
  - i servizi inclusi (`includedServiceIds`) eliminati non si copiano e non danno errore, come oggi (`ToDtoAsync` li esclude): il
    pannello non li mostra, quindi l'utente non potrebbe toglierli;
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
  pacchetto: con più righe scelte l'utente non può saperlo. Codici nuovi (`CatalogErrors`), dopo il 404, sul primo pacchetto che
  non va nell'ordine degli id; se un pacchetto ha più problemi, il primo nell'ordine della tabella:

  | Status | `code` | Quando | `args` |
  |---|---|---|---|
  | 422 | `pricing.duplicate_vat_rate_not_allowed` | L'IVA del pacchetto non è più tra le aliquote ammesse (`VatPolicy.ResolveAsync`). | `priceListName`, `rates` |
  | 422 | `pricing.duplicate_period_not_found` | Il pacchetto ha prezzi, anche intermedi, per un periodo eliminato. | `priceListName` |
  | 422 | `pricing.duplicate_area_not_found` | Il pacchetto ha fra le aree abilitate un'area della mappa eliminata. | `priceListName` |
  | 422 | `pricing.duplicate_sector_not_found` | Il pacchetto ha dati di un settore eliminato: un settore abilitato, un prezzo stagionale, per periodo, intermedio o stagionale ridotto. | `priceListName` |

  `priceListName` è il nome nella lingua di `Accept-Language`, come in tabella; `rates` c'è solo con il codice dell'IVA.
- **Un settore eliminato fa sempre fallire la copia**, in qualunque dato del pacchetto. Oggi succede solo con i prezzi per
  periodo: `ToDtoAsync` dà quel settore come UUID vuoto (`PriceListsService.cs:286`) e `ApplyAsync` non lo trova (`:203`);
  i settori abilitati e le righe di `sectorPrices` e `reducedPrices` di un settore eliminato invece si perdono in silenzio
  (`ToDtoAsync` li esclude), e la copia è diversa dall'originale. L'utente risolve modificando e salvando il pacchetto: le
  righe dei settori eliminati si scartano e un settore abilitato eliminato lo toglie lui (sezione 7).
- **Un periodo eliminato fa fallire la copia**, come un settore: eliminato come lo dice la sezione 7 ("Periodi"), anche nei prezzi
  intermedi. Le righe senza periodo o con una `key` sconosciuta invece **si copiano come sono**, come le tiene il salvataggio: oggi
  `ApplyAsync` le rifiuta con 422 `pricing.period_not_found`. L'utente risolve salvando il pacchetto, che scarta le righe dei
  periodi eliminati (sezione 7).
- Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con
  `pricing.price_list_not_found` toglie gli id dalla selezione e ricarica la tabella.

## 4. POST /api/catalog/sectors/sector-area-combobox

Tendina a scelta multipla "Aree abilitate" della tab Info Generali dei pannelli "Aggiungi nuovo pacchetto" e "Modifica
pacchetto", riletta a ogni apertura del pannello. Segue la regola di tutte le tendine del frontend: `POST`, senza corpo né
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
  altri pacchetti si spengono, **nella stessa transazione** del salvataggio. Oggi è un secondo `SaveChangesAsync`
  (`PriceListsService.cs:79-80` e `92-93`): un errore in mezzo lascia due predefiniti.
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
| 409 | `resource.version_conflict` | `If-Match` manca o non è più la versione del pacchetto (sezione 16). Niente cambia. |
| 422 | `catalog.vat_rate_not_allowed` | `vatRate` non è un'aliquota del Paese dello stabilimento, con `args` `{ "rates": [4, 5, 10, 22] }` come oggi (`VatPolicy.ResolveAsync`). Niente cambia. |
| 422 | `pricing.calculation_method_not_found` | Il metodo di `calculationMethod` non è nel catalogo (`CatalogErrors.CalculationMethodNotFound`). Niente cambia. |
| 404 | `catalog.sector_not_found` | Un id di `idSectors` non è un settore dello stabilimento, anche perché eliminato. Niente cambia. |
| 404 | `map.spot_not_found` | Un id di `idSectorAreas` non è un'area della mappa dello stabilimento, anche perché eliminata (`CatalogErrors.MapSpotNotFound`). Niente cambia. |
| 404 | `inventory.item_not_found` | Un `idInventoryItem` di `items` non è un articolo del magazzino dello stabilimento, anche perché eliminato (`CatalogErrors.InventoryItemNotFound`). Niente cambia. |

Le righe di prezzo di settori o periodi eliminati non danno errore: si scartano (sezione 7).

Errori di campo del 400, con `field` uguale al nome nel corpo:

| Campo | `code` | Quando |
|---|---|---|
| `translations[i].packageName` | `validation.required` | Vuoto nella lingua di `Accept-Language` (`i` è la sua posizione nell'elenco). |
| `translations[i].packageName` | `validation.max_length` | Oltre 255 caratteri (`PriceList.NameMaxLength`), in qualunque lingua. |
| `translations` | `validation.invalid` | Una lingua sconosciuta o ripetuta (oggi un 500: `ToDictionary` in `ApplyAsync`). |
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

Inoltre: un elenco assente o `null` vale `[]`; lo stesso id ripetuto in `idSectors` o `idSectorAreas` conta una volta; i prezzi si
salvano con due decimali, arrotondati come oggi (`numeric(12,2)`); con `isPublicHolidaysIncluded` falso i giorni festivi non si
salvano, come oggi (`PriceListsService.cs:252`). Nessun valore del corpo deve dare un 500.

Il frontend controlla già nome, lunghezze, prezzi massimi, date dei prezzi ridotti e quantità. Mostra sotto il campo gli errori
dei campi di primo livello che non sono id, `catalog.vat_rate_not_allowed` (sotto IVA, con le aliquote di `rates`) e
`pricing.calculation_method_not_found` (sotto "Calcolo del prezzo"), aprendo la tab del campo; sopra i bottoni tutti gli altri, con
il testo del loro `code`. Con `pricing.price_list_not_found` ricarica la tabella; con `catalog.sector_not_found` e
`map.spot_not_found` rilegge settori e aree e mostra quelli eliminati come non più disponibili; con `inventory.item_not_found` rilegge gli articoli (sezione
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
  `name` = "Umbrella". **Se l'italiano non è stato scritto la lettura non lo dà**: in quell'esempio `get-package` risponde solo
  `en`, altrimenti il salvataggio dopo lo fisserebbe come italiano. Come ricordare che quel `name` non è italiano lo decide lo
  sviluppatore, allo stesso modo per servizi e listini (`Servizi - Frontend requests.md`, "Valori che il modello di oggi non può
  salvare", 1). Per i pacchetti di oggi `name` è il nome italiano; `PUT /price-lists/{id}` scrive l'italiano.
- `maxAdditionalItemQuantity` e `maxPeopleQuantity`: oggi `maxAdditionalItems` e `maxPeople`. Il pannello fa scegliere da 1 a
  15 oppure null; un valore più alto già salvato si legge, si mostra e torna uguale finché l'utente non ne sceglie un altro.
- `items`: gli articoli di magazzino che compongono il pacchetto, come oggi (`items` con `inventoryItemId` + `quantity`), con
  `idInventoryItem` al posto di `inventoryItemId`; `[]` se nessuno. Il pannello ha una riga per ognuno dei 24 articoli
  dell'elenco fisso (sezione 17) e fa scegliere da 1 a 10; un valore più alto già salvato torna uguale finché l'utente non ne
  sceglie un altro. Gli articoli che il pannello non mostra ("Altro", senza codice) si leggono e tornano uguali. Restano com'erano
  anche `isAreaPackage` (falso in creazione) e il tipo con cui gli articoli sono salvati (`PackageItemKind.InventoryItem` o
  `AreaItem`); un articolo nuovo prende `AreaItem` se il pacchetto ha `isAreaPackage`, altrimenti `InventoryItem`, come oggi
  (`PriceListsService.cs:212`).
- `includedServices`: testo libero (campo "Servizi inclusi (opzionale)"), al massimo 1.000 caratteri, `null` se vuoto. **Va
  aggiunto** al listino: oggi non esiste (colonna nuova di testo, che può essere nulla). I servizi inclusi di oggi
  (`includedServiceIds`, righe `PackageItemKind.IncludedService`) il pannello non li ha: restano com'erano, `[]` in creazione.
- `idSectorAreas`: id delle aree scelte in "Aree abilitate", gli stessi di `POST /api/catalog/sectors/sector-area-combobox`;
  `[]` = nessuna scelta. **Va aggiunto il legame fra pacchetto e aree della mappa** (tabella nuova): oggi esiste solo
  `isAreaPackage` (sì/no), che è un'altra cosa. La lettura dà **tutte le aree salvate, anche quelle eliminate**, che il
  pannello mostra come "Area non più disponibile" perché l'utente le tolga. **Si salvano soltanto**: il preventivo e le vendite
  non le guardano, oggi come dopo questa modifica (limitano solo i settori di `idSectors`).
- `idSectors`: al posto di `isSectorRestricted` + `sectorIds`. `[]` = tutti i settori e file.
  **Vanno salvati a parte**, non ricavati dalle righe dei prezzi. Oggi `PriceListsService.ApplyAsync` non salva
  `sectorIds` e in lettura i settori abilitati sono quelli con una riga in `sectorPrices`. Esempio: `idSectors` = [Fila 1,
  Fila 2] e prezzi per 10 settori → `GET /api/catalog/price-lists/get-package` risponde `idSectors` = [Fila 1, Fila 2].
  Il pacchetto è limitato (`is_sector_restricted`) se `idSectors` non è vuoto, anche con soli settori eliminati. **Anche la vendita
  usa i settori salvati a parte**: oggi il preventivo rifiuta con `pricing.package_not_for_sector` i settori senza una riga in
  `sectorPrices` (`QuoteService.cs:120`, `PricingSource._hasSector`), e `sectorIds` di `GET /price-lists/{id}`
  (`PriceListsService.cs:279`) e dello snapshot si ricavano allo stesso modo. Senza questa modifica il pacchetto dell'esempio si
  venderebbe in 10 settori, e uno con solo prezzi per periodo in nessuno.
- `isOnlineEnabled` e `isOnlineOnly`: due scelte **separate e indipendenti**, al posto di `salesChannel`; possono essere tutte e due
  spente o tutte e due accese, e **si rileggono come sono state salvate**. Da loro dipende dove si vende il listino, cioè il
  `sales_channel` di oggi, che resta quello usato da preventivo, vetrina e ricerca:

  | `isOnlineEnabled` | `isOnlineOnly` | Dove si vende | `sales_channel` |
  |---|---|---|---|
  | false | false | Solo gestionale | `manager_only` |
  | true | false | Gestionale e online | `manager_and_online` |
  | false | true | Solo online | `online_only` |
  | true | true | Solo online (`isOnlineOnly` rende superflua l'altra) | `online_only` |

  **Da aggiungere**: oggi si salva solo `sales_channel` (`PriceList.SalesChannel`), che non distingue le ultime due righe. Basta
  salvare anche `isOnlineEnabled` (una colonna sì/no): `isOnlineOnly` è vero quando `sales_channel` è `online_only`. Come farlo lo
  sceglie lo sviluppatore. `PUT /price-lists/{id}` con `salesChannel` `manager_only` o `manager_and_online` scrive anche
  `isOnlineEnabled` (falso o vero); con `online_only` lo lascia com'era.
- `reducedSeasonalPrices`: i prezzi stagionali ridotti del pacchetto, quanti se ne vogliono (`[]` se nessuno), ognuno
  con nome, periodo e i suoi prezzi per settore (`sectorPrices`: gli stessi campi dei `sectorPrices` del pacchetto,
  "Prezzo stagionale mezza giornata" e "Prezzo stagionale", una riga per settore). I periodi di due prezzi ridotti dello
  stesso pacchetto non possono avere giorni in comune. Oggi il prezzo stagionale ridotto è
  una voce dello stabilimento (`/api/catalog/reduced-seasons`) e nel pacchetto ci sono solo i suoi prezzi
  (`reducedPrices`). Per ogni elemento il backend crea, o riusa, la voce con questo nome e questo periodo, la lega al
  pacchetto e ci salva i prezzi di `sectorPrices`; cambiare nome o date in un pacchetto non cambia la voce degli altri
  pacchetti che la usano. Il frontend li manda in ordine di `startsOn`. Le date sono giorni in UTC ISO 8601
  (`"2026-06-01T00:00:00.000Z"`), come in tutte le API del frontend, al posto di `DateOnly`. **Da aggiungere**:
  - **il legame fra pacchetto e voce**, che resta anche senza prezzi: oggi esiste solo attraverso le righe dei prezzi ridotti, che
    vogliono un settore (`PriceListReducedPriceConfiguration`), quindi un prezzo ridotto con `sectorPrices` vuoto ("Inizio
    settembre" nell'esempio) sparirebbe. Si toglie quando il prezzo ridotto non arriva più, quando si elimina il pacchetto e con
    `DELETE /api/catalog/reduced-seasons/{id}`; si copia con il pacchetto e fa parte della sua versione;
  - **il prezzo di mezza giornata**: oggi `PriceListReducedPrice` ha un prezzo solo, obbligatorio; servono
    `seasonalHalfDayPrice` e `seasonalPrice`, tutti e due facoltativi, almeno uno per riga;
  - **il preventivo** (`QuoteService`, `PricingSource`): con la mezza giornata usa il prezzo ridotto di mezza giornata, e se
    quello che serve è `null` calcola come senza prezzo ridotto. Deve cercare **fra i prezzi ridotti di quel pacchetto** con
    quelle date: oggi prende la prima voce dello stabilimento con quelle date (`reducedSeasons.FirstOrDefault`), quindi con
    due voci di pari date e nome diverso il prezzo del pacchetto può non trovarsi.
  La voce da riusare è quella con le stesse date e lo stesso nome, senza spazi ai lati e senza distinguere maiuscole e minuscole;
  con più voci uguali, quella già legata al pacchetto, altrimenti la prima per id. Una voce creata così va nel registro come
  `catalog.reduced_season_created`. Una voce che nessun pacchetto usa più **non si elimina**: resta fra quelle dello stabilimento
  (`/api/catalog/reduced-seasons`), come oggi.
- `sectorPrices`: `idSector` al posto di `sectorId`, il resto uguale. **Da correggere nel backend: il prezzo stagionale non arriva
  al preventivo.** `PriceListSectorPrice` lo scrive in `BasePrice` (`PriceListSectorPrice.cs`, `Create` e `SetPrices`) e
  `ToDtoAsync` lo rilegge da lì (`PriceListsService.cs:284`), ma il preventivo legge `SeasonalPrice` (`PricingSource.cs:61`), che
  nessun codice scrive: un preventivo stagionale dà 0 con l'avviso `seasonal_price_missing`. Serve che il prezzo salvato sia quello
  usato, anche per i listini già salvati; quale colonna tenere lo sceglie lo sviluppatore. Vale anche per gli articoli aggiuntivi.
  Lo stesso errore c'è nei listini delle convenzioni (`PricingSource.cs:90`), fuori da questa pagina: lo si segnala soltanto.
- **Le righe senza prezzi non si salvano**: il frontend manda una riga di `sectorPrices` per ogni settore e una di
  `periodPrices` per ogni settore e periodo, anche vuote (tutti i prezzi `null`). Oggi una riga vuota si salva
  (`SetSectorPrices`) e conta come uso del settore (`CountUsagesAsync`): nessun settore si potrebbe più eliminare. Vale anche
  per i `sectorPrices` dei prezzi ridotti. Il limite di 500 righe di `periodPrices` (`SavePriceListRequestValidator`) conta
  solo le righe con almeno un prezzo. **Eccezione**: una riga di `periodPrices` senza prezzi si salva lo stesso se quel settore e
  periodo ha, o riceve, prezzi intermedi, che nel database dipendono da quella riga (`PriceListStayPrice.PeriodPriceId`, in
  cascata): toglierla li cancellerebbe. La lettura non la dà in `periodPrices`.
- `periodPrices`: `idSector` al posto di `sectorId`. `idPeriod` (la `key` del periodo di `catalog/tariff-periods`) al
  posto di `periodKey`. Prezzi rinominati: `halfDayPrice` (`halfDay`), `dailyPrice` (`daily`), `weekendPrice` (`weekend`),
  `halfDayWeekendPrice` (`halfDayWeekend`), `halfDaySevenDaysPrice` (`halfDay7`), `sevenDaysPrice` (`days7`),
  `halfDayFourteenDaysPrice` (`halfDay14`), `fourteenDaysPrice` (`days14`), `halfDayMonthlyPrice` (`halfDayMonthly`),
  `monthlyPrice` (`monthly`). Non c'è `stayOverrides` (vedi `intermediatePrices`).
- **Righe di `periodPrices` per tutte le file o senza periodo**: `idSector` e `idPeriod` possono essere `null`
  (`PriceListPeriodPrice.SectorId` "NULL = tutte le file", `TariffPeriodGroup` senza valore nelle righe più vecchie), per esempio
  dopo un'importazione (`PriceListTransferService`). La lettura le dà, il pannello non le mostra e le rimanda uguali; il
  salvataggio e la copia le tengono come arrivano: non sono settori o periodi eliminati. Anche i loro prezzi intermedi restano
  com'erano, finché `isIntermediatePricesIncluded` è vero. Se un prezzo intermedio crea la riga di un settore dove per quel periodo c'è solo la riga "tutte le file", la riga
  nuova parte dai prezzi di quella: il preventivo usa la riga del settore quando c'è (`PricingSource`, `_rates`) e gli altri
  soggiorni di quel settore costerebbero 0.
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
  `effective_month_duration`, `actual_month_duration` e `fixed30_days` (`Codes.Of`).
- `weekendSetting`: tendina "Impostazione del weekend", null se non scelta: `SaturdaySunday` (sabato e domenica) o
  `SundayOnly` (solo domenica); oggi `saturday_sunday` e `sunday_only`.
- **`monthlySetting` e `weekendSetting` `null` restano `null`** anche in creazione e nella copia. Oggi le due colonne hanno un valore
  predefinito nel database (`PriceListConfiguration.cs:191` e `203`) che EF usa quando inserisce un `null`: si salverebbero
  `effective_month_duration` e `saturday_sunday`, e il prezzo cambierebbe (senza scelta il mese vale 30 giorni, con
  `EffectiveMonthDuration` 31).
- `isPublicHolidaysIncluded`: checkbox nell'intestazione della sezione "Festivi", al posto di `allowsPublicHolidays`.
- `publicHolidayDates`: i giorni della sezione "Festivi" (scelti nel calendario di "Aggiungi festivi", tolti con il
  cestino), al posto di `holidayDates`. Giorni in UTC ISO 8601, in ordine, ognuno una volta sola, `[]` se nessuno.
  Spegnere il checkbox, dopo una conferma, svuota la lista.
- `isIntermediatePricesIncluded`: checkbox nell'intestazione della sezione "Modifica prezzi intermedi". **Va aggiunto** al
  listino (oggi non esiste): vero o falso, falso per i pacchetti di oggi senza prezzi intermedi e vero per gli altri. Spegnerlo,
  dopo una conferma ("tutti i prezzi intermedi verranno persi"), svuota `intermediatePrices`. **Con `false` il salvataggio toglie
  tutti i prezzi intermedi del listino**, anche quelli che la lettura non restituisce (qui sotto): altrimenti il preventivo
  continuerebbe a usarli (`PackagePricer`, `Overrides`).
- `intermediatePrices`: i prezzi per numero di giorni scritti nella sezione, solo quelli con un prezzo (`[]` se nessuno), in
  ordine (prima `isFirstRow` true, poi per `idPeriod`, numerico, e `dayQuantity`). Ognuno ha `idPeriod` (la `key` di un periodo
  della stagione corrente), `isFirstRow` (true = "Fila 1", false = "Altre file"), `dayQuantity` (giorni di soggiorno, da 1 a
  90) e `price`. Una sola riga per `idPeriod` + `isFirstRow` + `dayQuantity`. **Sono gli `stayOverrides` di oggi**
  (`catalog.pricing_price_list_stay_prices`, uno per riga di `periodPrices` e numero di giorni, fino a 366; il loro `sector_id` non
  si usa, conta il settore della riga, come nel preventivo):
  - "Fila 1" è il primo settore di `catalog/sectors/sector-combobox`, "Altre file" tutti gli altri (con un settore solo il pannello
    non le offre);
  - **lettura**: "Altre file" ha il prezzo di un numero di giorni solo se è lo stesso in tutti gli altri settori, senza contare
    quelli eliminati e quelli che in quel periodo non hanno nessun prezzo (per esempio un settore appena creato); altrimenti non si
    restituisce. Non si restituiscono nemmeno i giorni oltre 90, i periodi eliminati e le righe "tutte le file";
  - **salvataggio**: scrive il prezzo nel primo settore o in tutti gli altri, e toglie quelli che la lettura restituirebbe e
    che non ci sono più. **Quelli che la lettura non restituisce restano com'erano**, a meno che per quel periodo e quei giorni
    non arrivi un prezzo di "Altre file";
  - i prezzi di 7, 14 e 30 giorni il frontend li scrive anche in `sevenDaysPrice`, `fourteenDaysPrice` e `monthlyPrice` delle
    righe di `periodPrices` di quel periodo (della prima fila o delle altre). L'utente può poi cambiare quelle colonne: se i due
    valori arrivano diversi si salvano tutti e due come arrivano, e il preventivo usa il prezzo intermedio, come oggi;
  - i prezzi intermedi di un periodo eliminato mentre il pannello è aperto si scartano senza errore, come le righe di
    `periodPrices` (sezione 7).
- `isMapAssignable` non arriva: vale solo per gli articoli aggiuntivi (`PriceList.Update`).
- Risposta: 204 senza corpo.
- Errori: quelli delle regole comuni.

### Dati già salvati

La migrazione dei dati nuovi non deve cambiare i prezzi e le vendite di oggi, per pacchetti e articoli aggiuntivi:

- **Settori abilitati**: per i listini con `is_sector_restricted` vero sono i settori con una riga non eliminata in
  `catalog.pricing_price_list_sector_prices`, anche se il settore è eliminato. Senza, dopo il rilascio `idSectors` sarebbe `[]` e i
  pacchetti limitati si venderebbero ovunque. Un listino limitato senza nessuna riga oggi non si vende in nessun settore: resta
  così finché non si salva dal pannello.
- **Righe senza prezzi** di `pricing_price_list_sector_prices` e `pricing_price_list_period_prices` (queste senza prezzi intermedi):
  si tolgono, dopo aver ricavato i settori abilitati. Altrimenti continuano a contare come uso del settore e del periodo e ne
  bloccano l'eliminazione.
- **Prezzi ridotti**: il prezzo di mezza giornata delle righe di oggi parte uguale al loro prezzo, perché oggi vale per tutte e due
  (`QuoteService`, `PriceSpot`); il legame fra pacchetto e voce c'è per ogni voce con almeno una riga.
- **Prezzo stagionale**: quello salvato fino a oggi in `base_price` è quello che il preventivo deve usare.
- **Prezzi intermedi**: `isIntermediatePricesIncluded` come detto sopra.
- **Vendita online**: `isOnlineEnabled` parte vero per i listini `manager_and_online`, falso per gli altri; `sales_channel` non
  cambia.

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
- **Nessuna riga di settori o periodi eliminati**, in `sectorPrices`, `periodPrices`, `intermediatePrices` e nei `sectorPrices` dei
  prezzi ridotti. Oggi `ToDtoAsync` dà le righe di un settore eliminato con un UUID vuoto (`PriceListsService.cs:261` e `:286`): con
  due settori eliminati il frontend rimanderebbe due righe uguali e ogni Salva risponderebbe 400.
- `translations`: solo le lingue con un nome. Il frontend aggiunge le altre vuote.
- `items`: anche gli articoli che il pannello non mostra; `idSectorAreas`: anche le aree eliminate.
- `idSectors`: quelli salvati con `create-package` o `update-package`, non i settori che hanno una riga di prezzo; **anche
  quelli eliminati**, che il pannello mostra come "Settore/fila non più disponibile" perché l'utente li tolga. Così un
  limite non diventa mai "tutti i settori" (`[]`) senza che l'utente lo scelga.
- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sectors/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe che non mostra ("tutte le file", senza periodo o con una `key` che `catalog/tariff-periods` non dà)
  le tiene da parte e le rimanda invariate con `update-package`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- `reducedSeasonalPrices`: in qualsiasi ordine (il frontend li ordina per `startsOn`). Anche i loro `sectorPrices`
  possono avere solo le righe salvate: il frontend fa una riga per ogni settore e le righe di settori che non conosce le
  tiene senza mostrarle e le rimanda invariate con `update-package`.
- Errori: quelli delle regole comuni, senza i 400.

## 7. POST /api/catalog/price-lists/update-package

Bottone "Salva" del pannello "Modifica pacchetto": il frontend manda tutto il pacchetto, con lo stesso corpo di
`POST /api/catalog/price-lists/create-package` (vedi sopra) più `idPackage`. Il pacchetto prende tutti i valori mandati:
`sectorPrices` e `periodPrices` hanno tutte le righe del pacchetto, anche quelle che il pannello
non mostra (sezione 6), così nessun prezzo salvato si perde; lo stesso per i `sectorPrices` di ogni
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
  che non è un periodo della stagione corrente. Il backend scarta in silenzio le righe dei settori e dei periodi eliminati (anche
  dentro i `sectorPrices` dei prezzi stagionali ridotti) e tiene come arrivano le altre. Esempio: Fila 9 eliminata dopo l'apertura
  del pannello → "Salva" risponde 204 e la riga di Fila 9 non si salva.
- **Periodi** (vale per letture, salvataggi e copie): lo stabilimento ha una stagione sola (`uq_catalog_pricing_seasons_property`) e i
  periodi della prossima stagione (`catalog.pricing_next_season_periods`) non c'entrano. Un periodo **esiste** se ha almeno una riga
  non eliminata in `catalog.pricing_tariff_periods`; è **eliminato** se ha solo righe eliminate; una `key` senza nessuna riga, o
  `null`, si tiene e si copia com'è. Una `key` eliminata non si riusa (`Stagioni e periodi - Frontend requests.md`, sezione 3).
- **Quello che è già salvato e torna uguale non dà errore**, anche se non rispetta i limiti nuovi: oggi due prezzi ridotti possono
  avere giorni in comune (`SaveReducedSeasonRequestValidator`) e un'importazione può superare 500 righe. Il controllo dei giorni in
  comune vale solo per i prezzi ridotti nuovi o con le date cambiate; i limiti di numero solo se il salvataggio aggiunge elementi.
  Altrimenti quel listino non si potrebbe più salvare.
- Un settore eliminato in `idSectors` invece risponde 404 `catalog.sector_not_found`, e un'area eliminata in `idSectorAreas` 404
  `map.spot_not_found`: non si salva niente, l'utente li toglie dalla tendina e salva di nuovo.
- Errori: quelli delle regole comuni. Con 409 `resource.version_conflict` (senza `errors[]`) niente cambia: il frontend non lo
  mostra sopra i bottoni, avvisa e rilegge il pacchetto.

## 8. POST /api/catalog/price-lists/additional-services-articles-list

Tabella della tab "Articoli/Servizi aggiuntivi" (`/settings/catalog/2`). Per il frontend sostituisce
`GET /api/catalog/price-lists?kind=additional`. **Vale tutta la sezione 1** (API nuova nello stesso gruppo, richiesta standard
delle tabelle, valori non validi, nome e ordine naturale, permesso `CatalogRead`, stabilimento dal token, errori e il loro
ordine), con queste differenze:

- **Righe**: i listini con `kind` `additional` dello stabilimento, senza gli eliminati.
- **`sortField`**: `additionalItemName` oppure `""` (per nome, come `additionalItemName` e `Ascending`).
- **Risposta**: ogni riga ha solo `idAdditionalItem` (`public_id`) e `additionalItemName`, scelto come `packageName`: il nome
  nella lingua di `Accept-Language`, altrimenti la colonna `name`. Ricerca e ordinamento lavorano su questo nome.

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "lettino",
  "filters": [],
  "sortField": "additionalItemName",
  "sortDirection": "Ascending"
}
```

```json
{
  "total": 2,
  "rows": [
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "additionalItemName": "Lettino" },
    { "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a11", "additionalItemName": "Lettino extra famiglia" }
  ]
}
```

## 9. POST /api/catalog/price-lists/delete-additional-services-article-item

**L'unica API con cui il frontend elimina gli articoli/servizi aggiuntivi**, dopo la conferma nel popup: il cestino di una riga
la chiama con un id, "Elimina selezione" della tabella con tutti gli id scelti. Elimina **uno o più articoli aggiuntivi in una
sola chiamata, tutto o niente**, come `delete-package-item` (sezione 2).

### Cosa ci serve

- **Tutto o niente**, come la sezione 2: se anche un solo articolo della lista non si può eliminare, non si elimina nessuno.
- **API nuova**: `DELETE /api/catalog/price-lists/{id}` e `POST /api/catalog/price-lists/bulk-delete` restano come sono e il
  frontend non le usa, per gli stessi motivi della sezione 2.
- **Solo articoli aggiuntivi**: l'id di un pacchetto (`kind` `package`) risponde 404 come un id che non esiste e non si elimina
  niente.
- **Stessi effetti della `DELETE` di oggi** per ogni articolo (`RemoveAsync`, `PriceListsService.cs:171`): soft delete e una voce
  `catalog.price_list_deleted` nel registro per articolo. **Nessun altro controllo**: niente dipende da un articolo aggiuntivo, e
  le prenotazioni tengono i loro dati come per i pacchetti.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.
- **Permesso**: `CatalogPricing` (`PriceListEndpoints.cs:17`). **Stabilimento** dal token.

### Richiesta, risposta ed errori

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02"]
}
```

- `idAdditionalItems`: gli `idAdditionalItem` delle righe della sezione 8, almeno uno, con le stesse regole di `idPackages`
  (sezione 2): lo stesso id ripetuto conta una volta, righe di più pagine, stessa nota sul tetto al numero di id.
- Risposta: 204 senza corpo. Dopo il 204 gli id escono dalla selezione e la tabella si ricarica.

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogPricing`. |
| 400 | `validation.failed` | `idAdditionalItems` assente o vuoto: `validation.required` su `idAdditionalItems` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `pricing.price_list_not_found` (`CatalogErrors.PriceListNotFound`) | Un id non è un articolo aggiuntivo dello stabilimento (anche perché già eliminato, o perché è un pacchetto). Non si elimina niente. |

Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con
`pricing.price_list_not_found` toglie gli id dalla selezione e ricarica la tabella.

## 10. POST /api/catalog/price-lists/duplicate-additional-services-article-item

**L'unica API con cui il frontend duplica gli articoli/servizi aggiuntivi**, dopo la conferma nel popup: il bottone "Duplica" di
una riga la chiama con un id, "Duplica selezione" della tabella con tutti gli id scelti. Duplica **uno o più articoli aggiuntivi
in una sola chiamata, tutto o niente**, come `duplicate-package-item` (sezione 3).

### Cosa ci serve

- **Tutto o niente** e **solo articoli aggiuntivi**, come l'eliminazione.
- **API nuova**: `POST /api/catalog/price-lists/{id}/duplicate` e `POST /api/catalog/price-lists/bulk-duplicate` restano come
  sono e il frontend non le usa, per gli stessi motivi della sezione 3.
- **Ogni copia è quella che il backend fa già** (`CopyAsync`, `PriceListsService.cs:145`), con le regole della sezione 3:
  - un articolo aggiuntivo nuovo dello stesso stabilimento, con un `public_id` nuovo, `created_by_user_id` dell'utente del token e
    **tutti** i dati di quello di partenza: articolo di magazzino con la sua quantità, pacchetti collegati con la loro quantità
    massima, prezzi, prezzi intermedi, festivi e i dati che il pannello non mostra; **mai predefinito**;
  - il **nome lo sceglie il backend** con `CopyNames.Next` fra i nomi degli articoli aggiuntivi: "Lettino copia", poi "Lettino
    copia 2"; anche i nomi tradotti prendono il suffisso della loro lingua, come nella sezione 3;
  - nel registro una voce `catalog.price_list_duplicated` per copia.
- Nomi in contemporanea e id ripetuti come nella sezione 3.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.
- **Permesso**: `CatalogPricing`. **Stabilimento** dal token.

### Richiesta, risposta ed errori

```json
{
  "idAdditionalItems": ["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01"]
}
```

- `idAdditionalItems`: come nell'eliminazione (sezione 9).
- Risposta: 204 senza corpo. Dopo il 204 la selezione si svuota (solo per "Duplica selezione") e la tabella si ricarica.
- Errori: gli stessi dell'eliminazione, nello stesso ordine. Con 404 `pricing.price_list_not_found` non si duplica niente.
- **Un articolo aggiuntivo che non si può copiare** risponde con un errore suo, dopo il 404, sul primo che non va nell'ordine
  degli id, e non si duplica niente. Sono i codici della sezione 3, con gli stessi motivi:

  | Status | `code` | Quando | `args` |
  |---|---|---|---|
  | 422 | `pricing.duplicate_vat_rate_not_allowed` | L'IVA non è più tra le aliquote ammesse (`VatPolicy.ResolveAsync`). | `priceListName`, `rates` |
  | 422 | `pricing.duplicate_period_not_found` | Ha prezzi, anche intermedi, per un periodo eliminato. | `priceListName` |
  | 422 | `pricing.duplicate_sector_not_found` | Ha dati di un settore eliminato: un prezzo stagionale, per periodo, intermedio o stagionale ridotto, o un settore abilitato. | `priceListName` |

  `priceListName` è il nome nella lingua di `Accept-Language`, come in tabella. Ordine dei codici, settori e periodi eliminati e
  righe senza periodo (che si copiano) come nella sezione 3. L'utente risolve salvando l'articolo aggiuntivo, che toglie tutti i
  dati di settori e periodi eliminati, anche quelli che il pannello non mostra (sezione 15).
- Il frontend mostra ogni errore nell'avviso "Operazione non riuscita", con il testo del suo `code`; con
  `pricing.price_list_not_found` toglie gli id dalla selezione e ricarica la tabella.

## 11. POST /api/catalog/price-lists/package-combobox

Tendina a scelta multipla "Pacchetti" dei pannelli "Aggiungi nuovo articolo/servizio aggiuntivo" e "Modifica articolo/servizio
aggiuntivo", riletta a ogni apertura del pannello. Segue la regola di tutte le tendine del frontend: `POST`, senza corpo né
pagine, risposta `[{ id, value }]`.

### Cosa ci serve

- **Un'API nuova nel gruppo `/api/catalog/price-lists`** (`Endpoints/PriceListEndpoints.cs:19`). `GET /price-lists/lookup` resta
  com'è: risponde `[{ id, kind, name }]` con il solo nome di `name` e un altro ordine, e la usa anche `ConfigSnapshotBuilder`.
- **Tutti i pacchetti** (`kind` `package`) dello stabilimento, senza gli eliminati, in **ordine naturale** di `value`, come
  `packages-list` (sezione 1).
- **Permesso**: `CatalogRead`. **Stabilimento** dal token (`CurrentProperty.IdAsync`).
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

### Richiesta e risposta

`POST` senza corpo. Risposta 200 con un array di `{ id, value }`, vuoto se non ci sono pacchetti. Nessun altro campo:

```json
[
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "value": "Ombrellone e 2 lettini" },
  { "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "value": "Ombrellone, sdraio e lettino" }
]
```

| Campo | Da | Tipo | Note |
|---|---|---|---|
| `id` | `public_id` | string (UUID) | L'`idPackage` di `packages-list`: quello che i pannelli mandano e leggono in `idPackages`. |
| `value` | `name` o traduzione | string | Il nome, scelto come `packageName` (sezione 1): lingua di `Accept-Language`, altrimenti `name`. |

### Errori

Gli stessi della sezione 4, nello stesso ordine: 401 `auth.unauthenticated`, 403 `auth.forbidden` (senza `CatalogRead`), 403
`catalog.property_required`, 404 `catalog.property_not_found`. Il frontend mostra l'errore sotto la tendina, con il testo del suo
`code`, e rilegge l'elenco quando l'utente prova ad aprirla.

## 12. API richieste in altri documenti

I pannelli degli articoli/servizi aggiuntivi le usano senza differenze: si fanno una volta sola, come le descrive il loro
documento.

| API | A cosa serve qui | Dove è richiesta |
|---|---|---|
| `POST /api/catalog/article-combobox` | Tendina "Seleziona articolo": l'`id` è l'`idArticle` delle sezioni 13-15 | `Impostazioni magazzino - Frontend requests.md`, sezione 2 |
| `POST /api/catalog/sectors/sector-combobox` | Le righe dei prezzi, una per settore | `Servizi - Frontend requests.md`, sezione 25 |

## Regole comuni dei pannelli degli articoli/servizi aggiuntivi (sezioni 13-15)

I pannelli "Aggiungi nuovo articolo/servizio aggiuntivo" e "Modifica articolo/servizio aggiuntivo" si leggono e si salvano in una
sola chiamata, come quelli dei pacchetti.

### Cosa ci serve

- **API nuove nel gruppo `/api/catalog/price-lists`**, come quelle dei pacchetti. `POST /price-lists`, `GET` e
  `PUT /price-lists/{id}` restano come sono e il frontend non le usa.
- **Solo articoli aggiuntivi**: lettura e salvataggio accettano solo listini con `kind` `additional` dello stabilimento; ogni
  altro id, anche di un pacchetto, risponde 404 `pricing.price_list_not_found`. La creazione li fa di questo tipo.
- **Permesso**: `CatalogRead` per la lettura, `CatalogPricing` per creazione e salvataggio. **Stabilimento** dal token.
- **Registro**: `catalog.price_list_created` e `catalog.price_list_updated`, come oggi.
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
| 404 | `pricing.price_list_not_found` | `idAdditionalItem` non è un articolo aggiuntivo dello stabilimento (anche perché già eliminato). Niente cambia. |
| 409 | `resource.version_conflict` | `If-Match` manca o non è più la versione dell'articolo aggiuntivo (sezione 16). Niente cambia. |
| 422 | `catalog.vat_rate_not_allowed` | `vatRate` non è un'aliquota del Paese dello stabilimento, con `args.rates`, come per i pacchetti. Niente cambia. |
| 422 | `pricing.calculation_method_not_found` | Il metodo di `calculationMethod` non è nel catalogo. Niente cambia. |
| 404 | `inventory.item_not_found` | `idArticle` non è un articolo del magazzino dello stabilimento, anche perché eliminato (`CatalogErrors.InventoryItemNotFound`). Niente cambia. |
| 404 | `pricing.package_not_found` (nuovo) | Un id di `idPackages` non è un pacchetto dello stabilimento, anche perché eliminato. Niente cambia. Oggi `ApplyAsync` (`PriceListsService.cs:229`) risponde `pricing.price_list_not_found`, lo stesso codice dell'articolo aggiuntivo che non c'è: il frontend non saprebbe quale dei due manca. |

Le righe di prezzo di settori o periodi eliminati non danno errore: si scartano (sezione 15).

Errori di campo del 400, con `field` uguale al nome nel corpo:

| Campo | `code` | Quando |
|---|---|---|
| `idArticle` | `validation.required` | Assente o `null`. |
| `idPackages` | `validation.invalid` | Lo stesso id due volte. |
| `vatRate`, `calculationMethod`, `monthlySetting`, `weekendSetting`, `sectorPrices`, `periodPrices`, `publicHolidayDates`, `intermediatePrices` | `validation.invalid` | Come nella tabella dei pacchetti. |

Elenchi assenti, decimali e giorni festivi come per i pacchetti. Nessun valore del corpo deve dare un 500.

Il frontend controlla già l'articolo obbligatorio e i prezzi massimi. Mostra sotto il campo gli errori dei campi di primo livello che
non sono id, `inventory.item_not_found` (sotto "Seleziona articolo"), `pricing.package_not_found` (sotto "Pacchetti"),
`catalog.vat_rate_not_allowed` (sotto IVA) e `pricing.calculation_method_not_found` (sotto "Calcolo del prezzo"); sopra i bottoni
tutti gli altri, con il testo del loro `code`. Con `inventory.item_not_found` e `pricing.package_not_found` rilegge la tendina e
mostra quello scelto come "Articolo non più disponibile" o "Pacchetto non più disponibile", finché l'utente non lo cambia o lo
toglie; con `pricing.price_list_not_found` ricarica la tabella; con `resource.version_conflict` avvisa e rilegge l'articolo
aggiuntivo.

## 13. POST /api/catalog/price-lists/create-additional-services-article-item

Bottone "Crea articolo/servizio" del pannello "Aggiungi nuovo articolo/servizio aggiuntivo" (pagina Listino): il frontend manda
tutto il modulo in una sola chiamata. Per il frontend sostituisce `POST /api/catalog/price-lists` con `kind: "additional"`, che
accetta solo `SavePriceListRequest`.

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
  "vatRate": 22,
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

- `kind`: il frontend non lo manda: questa API crea sempre un articolo aggiuntivo.
- `idArticle`: l'articolo di magazzino scelto in "Seleziona articolo" (sezione 12), obbligatorio, al posto di `items`, che per un
  articolo aggiuntivo ha un solo elemento (`SavePriceListRequestValidator`, `PriceListValidators.cs:42`). Il pannello non ha la
  **quantità**: 1 in creazione, quella salvata in modifica.
- **Nome**: il frontend non manda `name` né `translations`. In creazione `name` è quello dell'articolo di magazzino, senza
  traduzioni. In modifica nome e traduzioni salvati **non cambiano**, così una copia resta "Lettino copia"; se `idArticle` è
  diverso da quello salvato, `name` diventa quello del nuovo articolo e le traduzioni si tolgono.
- `idPackages`: i pacchetti scelti in "Pacchetti" (sezione 11), `[]` = tutti i pacchetti, al posto di `packageLinks`. Il pannello
  non ha la **quantità massima** per pacchetto (`maxCount`, 1-100): per i pacchetti già collegati resta quella salvata, per i nuovi
  è 100. Il limite di articoli aggiuntivi del pacchetto (`maxAdditionalItems`) vale comunque (`QuoteService`).
- `isOnlineEnabled` e `isOnlineOnly` ("Prenotazioni online" e "Solo online"): al posto di `salesChannel`, separati e indipendenti
  come in `create-package` (sezione 5).
- `isMapAssignable`: uguale.
- `vatRate`, `sectorPrices`, `periodPrices`, `calculationMethod`, `isCumulative`, `monthlySetting`, `weekendSetting`,
  `isPublicHolidaysIncluded`, `publicHolidayDates`, `isIntermediatePricesIncluded`, `intermediatePrices`: gli stessi campi e valori
  di `create-package` (sezione 5), con le stesse differenze e le stesse cose da aggiungere: righe senza prezzi che non si salvano,
  natura IVA predefinita con `vatRate` `null`, `isIntermediatePricesIncluded` nuovo, `intermediatePrices` al posto di
  `stayOverrides`. Qui le righe dei prezzi ci sono sempre, perché il pannello non ha tab.
- **Dati che il pannello non mostra** (`maxPeople`, `isDefault`, `isDefaultOnline`, `isSectorRestricted` con i settori abilitati,
  `reducedPrices`): vuoti o falsi in creazione, **com'erano** in modifica, tranne i settori abilitati e i prezzi ridotti di settori
  eliminati, che il salvataggio toglie (per un articolo aggiuntivo il preventivo non guarda i settori abilitati).
- Risposta: 204 senza corpo.
- Errori: quelli delle regole comuni.

## 14. GET /api/catalog/price-lists/get-additional-services-article-item?idAdditionalItem=…

Apertura del pannello "Modifica articolo/servizio aggiuntivo" (matita della tabella Articoli/Servizi aggiuntivi, pagina Listino):
il frontend legge tutto l'articolo aggiuntivo in una sola chiamata e ci riempie il modulo. Per il frontend sostituisce
`GET /api/catalog/price-lists/{id}`, che risponde `PriceListDto`. L'id va nella query string (`idAdditionalItem`). La risposta ha
`idAdditionalItem` e gli stessi campi, con gli stessi nomi e valori, che il frontend manda a
`create-additional-services-article-item` (sezione 13).

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
  "vatRate": 22,
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
- `idArticle`: anche di un articolo di magazzino eliminato, che il pannello mostra come "Articolo non più disponibile" (oggi il
  backend non lascia eliminare un articolo usato da un listino: 409 `inventory.item_in_use`). Un articolo aggiuntivo importato può
  non averne o averne più di uno (`PriceListTransferService`; il limite a uno è solo in `SavePriceListRequestValidator`): senza, si
  legge `null` e il pannello chiede di sceglierlo; con più di uno si legge il primo (per id della riga) e gli altri restano finché
  `idArticle` non cambia.
- **Nessuna riga di settori o periodi eliminati**, come nella sezione 6.
- `vatRate`: `null` per un articolo aggiuntivo esente, come nella sezione 5.
- `sectorPrices` e `periodPrices`: solo le righe con dei prezzi salvati. Il frontend fa una riga per ogni settore di
  `catalog/sectors/sector-combobox`, e per ogni settore in ogni periodo della stagione corrente (`catalog/tariff-periods`), e ci
  mette i prezzi letti. Le righe che non mostra (sezione 6) le tiene da parte e le rimanda invariate con
  `update-additional-services-article-item`.
- `publicHolidayDates` e `intermediatePrices`: in qualsiasi ordine (il frontend li ordina).
- Errori: quelli delle regole comuni, senza i 400.

## 15. POST /api/catalog/price-lists/update-additional-services-article-item

Bottone "Salva" del pannello "Modifica articolo/servizio aggiuntivo": il frontend manda tutto il modulo, con lo stesso corpo di
`create-additional-services-article-item` (sezione 13) più `idAdditionalItem`. L'articolo aggiuntivo prende tutti i valori mandati:
`sectorPrices` e `periodPrices` hanno tutte le sue righe, anche quelle che il pannello non mostra (sezione 6), così nessun prezzo
salvato si perde. `publicHolidayDates` ha tutti i giorni festivi rimasti, in ordine;
`intermediatePrices` ha tutti i prezzi intermedi scritti, in ordine. Per il frontend sostituisce
`PUT /api/catalog/price-lists/{id}`, che accetta solo `SavePriceListRequest`.

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
- **Quello che il frontend non manda resta com'era**: nome e traduzioni (se l'articolo non cambia), quantità, quantità massime dei
  pacchetti già collegati e i dati che il pannello non mostra (sezione 13).
- Righe di settori o periodi che non esistono più, periodi e dati già salvati: come in `update-package` (sezione 7).
- Errori: quelli delle regole comuni. Con 409 `resource.version_conflict` (senza `errors[]`) niente cambia: il frontend non lo
  mostra sopra i bottoni, avvisa e rilegge l'articolo aggiuntivo.

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
- Il 409 `resource.version_conflict` si controlla subito dopo il 404 dell'elemento (`pricing.price_list_not_found`), prima dei
  controlli sui dati collegati (IVA, settori, aree, articoli, pacchetti). Esempio: eliminare un pacchetto lo toglie dagli articoli
  aggiuntivi e ne cambia la versione; con l'ordine opposto l'utente riceverebbe 404 `pricing.package_not_found`, toglierebbe il
  pacchetto, salverebbe e solo allora un 409 che gli fa perdere le modifiche.
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  dei dati coperti dalla versione, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta
  la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/catalog/price-lists/get-package` (sezione 6) | `POST /api/catalog/price-lists/update-package` (sezione 7) | Tutto il pacchetto: tutti i campi della risposta della sezione 6, nomi, prezzi e giorni festivi compresi, e qual è il primo settore (`isFirstRow` dipende da quello) |
| `GET /api/catalog/price-lists/get-additional-services-article-item` (sezione 14) | `POST /api/catalog/price-lists/update-additional-services-article-item` (sezione 15) | Tutto l'articolo aggiuntivo: tutti i campi della risposta della sezione 14, prezzi e giorni festivi compresi, e qual è il primo settore |

Il primo settore conta perché "Fila 1" è il primo di `catalog/sectors/sector-combobox`: se un altro utente elimina la Fila 1 o cambia
l'ordine mentre il pannello è aperto, il salvataggio scriverebbe i prezzi intermedi di "Fila 1" su un'altra fila.

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
  `GET /inventory-items/lookup` restano come sono: vogliono `InventoryRead`, che chi può leggere il listino (`CatalogRead`) può non
  avere, e non danno le quantità.
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

## 18. POST /api/catalog/quotes: un listino solo online non si vende dal gestionale

Un pacchetto o un articolo aggiuntivo "solo online" (`sales_channel` `online_only`, sezione 5) si deve vendere solo dal sito
pubblico.

### Com'è oggi

Il preventivo controlla il canale in un verso solo: dal sito (`QuoteChannel.Online`) rifiuta un listino `manager_only` con 422
`pricing.package_not_online` (`QuoteService.cs:118` per il pacchetto, `:154` per gli articoli aggiuntivi). Dal gestionale
(`QuoteChannel.Manager`) non controlla niente: `PricingSource.IsOnline` è solo "diverso da `manager_only`"
(`PricingSource.cs:53`), quindi `online_only` si vende come `manager_and_online`.

### Cosa ci serve

- **Con `QuoteChannel.Manager` un listino `online_only` si rifiuta**: 422 `pricing.package_online_only` (codice nuovo,
  `CatalogErrors`), nello stesso punto dei due controlli di oggi, dopo quello sul tipo, per il pacchetto della postazione
  (`spots[].priceListId`) e per ogni articolo aggiuntivo (`spots[].additionals[].priceListId`), anche con un prezzo manuale
  (`manualPrice`). `args`: `priceListName`, il nome nella lingua di `Accept-Language`, come in tabella (sezioni 1 e 8). Senza
  `errors[]`.
- **Dal sito non cambia niente**: `online_only` e `manager_and_online` si vendono, `manager_only` no.
- **Convenzioni**: i loro listini non hanno il canale e non cambiano.
- **Prenotazioni che hanno già quel listino**: il blocco vale quando dal gestionale lo si mette in una prenotazione. Una prenotazione
  presa dal sito con un listino solo online (o con un listino diventato solo online dopo) deve potersi modificare e ricalcolare dal
  gestionale tenendolo. Oggi `QuoteRequest` non dice di quale prenotazione è il preventivo e il salvataggio delle prenotazioni non
  c'è ancora (`89-api-catalog.md`, "Non ancora disponibile"): quando ci sarà, come riconoscere i listini che la prenotazione ha già
  lo sceglie lo sviluppatore. Fino ad allora il blocco vale per ogni preventivo del gestionale.
- **Preventivo offline**: vale la stessa regola (`salesChannel` è già in `PriceListDto`, quindi nello snapshot `price_catalog`).
- Elenchi e tendine dei listini non cambiano: un listino solo online si vede e si modifica come gli altri, e può essere il
  predefinito.
- **Come farlo lo sceglie lo sviluppatore del backend**, purché il risultato sia questo.

```json
{
  "type": "https://errors.summerbooking/pricing/package_online_only",
  "title": "pricing.package_online_only",
  "status": 422,
  "code": "pricing.package_online_only",
  "args": { "priceListName": "Ombrellone e 2 lettini" },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

Oggi il frontend non chiama il preventivo: aggiungerà il testo di questo codice, nelle 7 lingue, con le pagine delle prenotazioni.

## API esistenti usate così come sono

I pannelli dei pacchetti e degli articoli aggiuntivi leggono i periodi della stagione corrente con
`GET /api/catalog/tariff-periods` (`key`, `name`, `ranges`): la `key` di ogni periodo è l'`idPeriod` dei prezzi per periodo
e dei prezzi intermedi. Per il Listino **non va cambiata**: la `version` chiesta in `Stagioni e periodi - Frontend
requests.md` (sezione 6) serve solo a quella pagina, e il Listino non la usa. Vale anche qui la sezione 3 di quel documento:
la `key` di un periodo eliminato non si riusa, altrimenti i prezzi salvati passerebbero al periodo nuovo.

## Verifica

- `create-package` con `Accept-Language: en` e solo il nome inglese risponde 204; `name` = il nome inglese. Senza il
  nome nella lingua di `Accept-Language` risponde 400 `validation.failed` su `translations[i].packageName`.
- `idSectors` salvati con prezzi per tutti i settori tornano uguali in `GET /api/catalog/price-lists/get-package`, anche se uno
  è stato eliminato dopo; rimandarlo con `update-package` risponde 404 `catalog.sector_not_found`.
- `update-package` e `update-additional-services-article-item` con una riga di un settore o di un periodo eliminato, "tutte le file"
  (`idSector` `null`) o con una `key` senza periodo, rispondono 204: le prime due non si salvano, le altre restano com'erano.
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
- Righe di `sectorPrices` e `periodPrices` con tutti i prezzi `null`: 204, la lettura non le dà e il settore si può eliminare, se il
  listino non lo ha fra quelli abilitati e non ha altri suoi prezzi. Una riga di periodo senza prezzi ma con un prezzo intermedio a 5
  giorni: il prezzo intermedio si rilegge.
- Preventivo: un pacchetto con `idSectors` [Fila 1], prezzi stagionali per 10 settori e nessun prezzo stagionale di Fila 1: si vende
  solo in Fila 1. Un preventivo stagionale dà il `seasonalPrice` salvato, non 0, anche per un articolo aggiuntivo.
- `idSectorAreas`: il preventivo non cambia con o senza aree.
- `isOnlineEnabled` e `isOnlineOnly`: salvate le quattro combinazioni, ognuna si rilegge uguale, con il `sales_channel` della tabella
  (sezione 5), anche per un articolo aggiuntivo e dopo una duplicazione.
- Un prezzo ridotto con `sectorPrices` `[]`: salvato e riletto.
- `isIntermediatePricesIncluded` `false` su un pacchetto con un prezzo a 120 giorni e uno a 5 giorni diverso fra gli altri settori:
  dopo il salvataggio non ce n'è più nessuno.
- `monthlySetting` e `weekendSetting` `null` in creazione: la lettura dà `null`; la copia ha `null`.
- Creazione con `Accept-Language: en` e solo l'inglese: `get-package` dà solo `en`.
- `PUT /price-lists/{id}` su un pacchetto con aree, testo dei servizi inclusi e prezzo ridotto di mezza giornata: restano.
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
5. Gli stessi casi con `get-additional-services-article-item` e `update-additional-services-article-item`.

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
    `idSectors`) la duplicazione riesce. Un pacchetto con una riga senza periodo o con una `key` sconosciuta: 204, e la copia ha
    quella riga.
15. `idPackages` vuoto o assente: 400 `validation.failed` con `validation.required` su `idPackages`. Senza token 401
    `auth.unauthenticated`; senza `CatalogPricing` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`.

API 4, con una mappa con due aree ("Area 10" e "Area 2"), una terza eliminata e altre postazioni:

16. `sector-area-combobox` senza corpo: 200 con `[Area 2, Area 10]`, ognuna con solo `id` (il `public_id` della postazione) e
    `value`. Uno stabilimento senza aree: 200 con `[]`. Senza token 401 `auth.unauthenticated`; senza `CatalogRead` 403
    `auth.forbidden`; token senza stabilimento 403 `catalog.property_required`.

API 8-10, con 12 articoli aggiuntivi e 3 pacchetti:

17. `additional-services-articles-list`: i casi 6-10 con `idAdditionalItem`, `additionalItemName` e `sortField`
    `additionalItemName`; nessun pacchetto compare.
18. `delete-additional-services-article-item` di due articoli: 204, non compaiono più e nel registro ci sono due voci
    `catalog.price_list_deleted`; con un solo id, anche ripetuto: 204 e una voce. Con l'id di un pacchetto, di un altro
    stabilimento o già eliminato: 404 `pricing.price_list_not_found` e nessuno è eliminato. Lo stesso con
    `duplicate-additional-services-article-item`: nessuna copia.
19. `duplicate-additional-services-article-item` di "Lettino" (traduzione inglese "Sunbed", legato a due pacchetti con quantità
    massima 3 e 5, quantità dell'articolo 2): 204; c'è "Lettino copia" / "Sunbed copy" con gli stessi dati, compresi legami e
    quantità. Di nuovo: "Lettino copia 2". Con un'IVA non più ammessa, un prezzo di un periodo eliminato o di un settore eliminato:
    422 `pricing.duplicate_vat_rate_not_allowed`, `pricing.duplicate_period_not_found` o `pricing.duplicate_sector_not_found` con
    `priceListName`, e nessuna copia; dopo `update-additional-services-article-item` la duplicazione riesce.
20. `idAdditionalItems` vuoto o assente: 400 `validation.failed` con `validation.required` su `idAdditionalItems`. Permessi, token e
    stabilimento come nei casi 10 e 15.

API 11: `package-combobox` senza corpo dà tutti i pacchetti in ordine naturale ("Cabina 2" prima di "Cabina 10"), con il nome nella
lingua di `Accept-Language`, altrimenti quello di `name`; nessun articolo aggiuntivo né pacchetto eliminato. Errori come nel caso 16.

API 13-15, con un token con `CatalogRead` e `CatalogPricing`:

21. `create-additional-services-article-item` con il corpo della sezione 13: 204; compare in tabella con il nome dell'articolo, la
    lettura dà gli stessi dati, l'articolo ha quantità 1 e i pacchetti quantità massima 100. Una voce `catalog.price_list_created`.
22. Un articolo con nome "Lettino copia", traduzione "Sunbed copy", quantità 2, un pacchetto con quantità massima 3, persone massime,
    settori abilitati e prezzi ridotti: letto e salvato senza cambiare niente restano tutti com'erano. Aggiunto un pacchetto: il
    primo resta a 3, il nuovo è a 100. Cambiato `idArticle`: il nome è quello del nuovo articolo, senza traduzioni.
23. `idArticle` assente: 400 con `validation.required` su `idArticle`. Un articolo sconosciuto o eliminato: 404
    `inventory.item_not_found`. Un pacchetto sconosciuto, eliminato o un articolo aggiuntivo in `idPackages`: 404
    `pricing.package_not_found`. `vatRate` 7 in Italia: 422 `catalog.vat_rate_not_allowed` con `args.rates`; `null`: 204 e la lettura
    dà `null`. In tutti i casi d'errore niente cambia.
24. Lettura e salvataggio con l'id di un pacchetto, di un altro stabilimento o già eliminato: 404 `pricing.price_list_not_found`.
    Righe di prezzo e prezzi intermedi come per i pacchetti; permessi, token e stabilimento come per le API 5-7.

API 18, con un pacchetto e un articolo aggiuntivo per ognuno dei tre canali:

25. `POST /api/catalog/quotes` con il pacchetto `online_only`: 422 `pricing.package_online_only` con `priceListName`, anche con un
    prezzo manuale; con il pacchetto `manager_and_online` o `manager_only`: 200. Lo stesso con l'articolo aggiuntivo `online_only`
    su un pacchetto valido.
26. `POST /api/catalog/public/bathhouses/{slug}/quote`: 200 con `online_only` e `manager_and_online`, 422
    `pricing.package_not_online` con `manager_only`, come oggi.
27. Un pacchetto salvato con `isOnlineEnabled` e `isOnlineOnly` veri si rifiuta dal gestionale; tolta `isOnlineOnly`: 200.
