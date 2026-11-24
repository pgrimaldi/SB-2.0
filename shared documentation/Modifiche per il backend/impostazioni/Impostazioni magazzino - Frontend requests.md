# Impostazioni magazzino - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Impostazioni magazzino** del gestionale funzioni come la vuole
il frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/warehouse-setting`: una tabella con gli articoli del magazzino dello stabilimento (nome, quantità
totale e soglia), con ricerca, ordinamento e pagine, e i comandi per aggiungere, modificare, duplicare ed eliminare gli
articoli.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/inventory-items-list` | Elenco degli articoli per la tabella, a pagine |
| 2 | POST | `/api/catalog/article-combobox` | Articoli per le tendine, tutti insieme |
| 3 | POST | `/api/catalog/add-warehouse-item` | Aggiunge pezzi a un articolo che esiste già |
| 4 | PUT | `/api/catalog/edit-warehouse-item/{id}` | Nuova rotta di `PUT /api/catalog/inventory-items/{id}`, con una regola in più sull'avviso |
| 5 | POST | `/api/catalog/delete-warehouse-item` | Elimina uno o più articoli, tutto o niente |
| 6 | POST | `/api/catalog/duplicate-warehouse-item` | Duplica uno o più articoli, tutto o niente |
| 7 | GET + PUT | `/api/catalog/inventory-items/{id}` e `/api/catalog/edit-warehouse-item/{id}` | Controllo di versione: `version` nella lettura, `If-Match` ed `ETag` nel salvataggio, 409 `resource.version_conflict` |

## Regole comuni

- **Modulo**: le API stanno in Catalog (`Module.Catalog`), accanto a `/api/catalog/inventory-items`
  (`Endpoints/InventoryEndpoints.cs`, `Application/Services/InventoryService.cs`, `InventoryItemRepository` in
  `Infrastructure/Repositories/CatalogRepositories.cs`), con i dati di `InventoryItem` (`catalog.inventory_items`).
- **Le API esistenti non cambiano**: `GET` e `POST /api/catalog/inventory-items`, `GET` e
  `DELETE /api/catalog/inventory-items/{id}` (il frontend usa la `GET` per il popup "Modifica articolo", non usa la
  `DELETE`: per eliminare usa solo la sezione 5), `POST /api/catalog/inventory-items/bulk-delete` e
  `GET /api/catalog/inventory-articles` restano come sono: stesse rotte, richieste, risposte e comportamento. Anche
  `GET /api/catalog/inventory-items/lookup` resta com'è, salvo che lo sviluppatore scelga l'alternativa della sezione 2.
  `PUT /api/catalog/inventory-items/{id}` cambia rotta e ha una regola in più sull'avviso, che vale anche per
  `POST /api/catalog/inventory-items` perché usano lo stesso validatore (sezione 4). La lettura
  `GET /api/catalog/inventory-items/{id}` e il salvataggio della sezione 4 hanno in più il controllo di versione
  (sezione 7).
- **Permesso**: per l'API 1 `InventoryRead`, come `GET /api/catalog/inventory-items`; per l'API 2 `CatalogRead`
  (sezione 2); per le API 3, 4, 5 e 6 `InventoryManage`, come `POST /api/catalog/inventory-items`, `PUT` e
  `DELETE /api/catalog/inventory-items/{id}`. Senza permesso 403 `auth.forbidden`; senza token valido 401
  `auth.unauthenticated`.
- **Stabilimento**: lo prende dal token (`CurrentProperty.IdAsync`), come le altre API del Catalog: nessuna richiesta lo
  contiene. Token senza stabilimento: 403 `catalog.property_required`; stabilimento che non esiste: 404
  `catalog.property_not_found`.
- **Righe eliminate**: gli articoli con `deleted_at` valorizzato non compaiono mai. Lo fa già il filtro globale delle
  entità `ISoftDeletable` (`ModuleDbContextConventions.ApplySoftDeleteQueryFilter`, richiamato in
  `Infrastructure/DbContext.cs`), che vale anche per `InventoryItem`.
- **JSON** in camelCase, con i nomi esatti di questo documento.
- **Errori** nel formato Problem Details del backend (`Boot/Web/ProblemDetailsWriter.cs`), con `code`, `traceId` ed
  `errors[]` quando ci sono. Gli errori di validazione sono 400 `validation.failed` con un elemento di `errors[]` per
  ogni campo che non rispetta le sue regole (`ValidatorExtensions.EnsureValid`; le regole di un campo si fermano al
  primo errore, `CascadeMode.Stop` in `Boot/Program.cs`), con `field` uguale al nome del campo nel corpo.
- **`args` degli errori di campo**: `ValidatorExtensions.ToFieldError` copia negli `args` i segnaposto `MaxLength`,
  `MinLength`, `ComparisonValue`, `From` e `To` della regola (come `max`, `min`, `value`, `from` e `to`): per esempio
  `validation.max_length` ha `args.max` e anche `args.min` = 0. Il frontend mostra sotto i campi dei popup (sezioni 3
  e 4) il testo del `code` con questi `args`; per la tabella (sezione 1) e la tendina (sezione 2) mostra un testo
  senza i singoli errori.
- **Valori che non si leggono**: un id che non è un UUID o un numero che non entra in un intero a 32 bit non arrivano
  ai validatori. Oggi il backend risponde 400 `validation.failed` senza `errors[]` (`BadHttpRequestException` in
  `Boot/Web/ProblemDetailsWriter.cs`); l'agente verifica con lo sviluppatore che fuori da Development la risposta sia
  la stessa (`RouteHandlerOptions.ThrowOnBadRequest` non è impostato in `Boot/Program.cs`). Il frontend non manda
  questi valori.
- **Prenotazioni (modulo Booking)**: Booking usa gli articoli tramite il loro `public_id`, senza chiavi esterne
  (`resource_id` in `ResourceConsumptionConfiguration.cs`), e `booking.resource_availability_daily.total_qty` copia la
  giacenza del magazzino. Le API di questo documento cambiano la quantità totale (sezioni 3, 4 e 6) o eliminano
  articoli (sezione 5) senza controllare né aggiornare Booking, come fanno oggi `PUT` e `DELETE` di
  `/api/catalog/inventory-items/{id}` (`InventoryService.UpdateAsync` e `DeleteAsync`). L'agente chiede allo
  sviluppatore se servono controlli o aggiornamenti su Booking; se ne nasce un errore nuovo, va scritto qui con status e
  `code`, perché il frontend lo mostri.

## 1. POST /api/catalog/inventory-items-list

Tabella della pagina: il frontend la chiama all'apertura e ogni volta che cambiano pagina, numero di righe, ricerca o
ordinamento. È un'**API nuova**: per il frontend sostituisce `GET /api/catalog/inventory-items`, che ha la stessa
funzione ma con un'altra richiesta (parametri nell'indirizzo `search`, `sortBy`, `sortDir`, `page`, `pageSize`) e
un'altra risposta (`{ total, page, pageSize, items }` con `InventoryItemDto`).

### Richiesta

Il corpo è la richiesta standard delle tabelle del frontend, uguale per tutte le tabelle del gestionale:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "lett",
  "filters": [],
  "sortField": "name",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number (intero) | Pagina richiesta, da 1. |
| `pageSize` | number (intero) | Righe per pagina: 10, 20, 50 o 100. Non valgono gli altri valori di `CatalogQueryRules.PageSizes` (25, 200, 300). |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Il frontend lo manda senza spazi ai lati, da 3 caratteri in su e al massimo 100 (il campo non ne accetta di più). Trova gli articoli il cui nome contiene il testo, senza distinguere maiuscole e minuscole; `%` e `_` contano come caratteri normali, non come caratteri jolly (come già fa `GET /api/catalog/inventory-items` con `LikePattern.Contains`). Al massimo 100 caratteri (`CatalogQueryRules.SearchMaxLength`). |
| `filters` | array | Sempre `[]`: la tabella non ha filtri. |
| `sortField` | string | `name`, `totalQuantity`, `lowStockThreshold`, oppure `""`: senza un ordinamento scelto dall'utente la tabella mostra gli articoli per nome, come con `name` e `Ascending`. |
| `sortDirection` | string | `Ascending` o `Descending`. Con `sortField` `""` il frontend manda `Ascending`. |

Ordinamento:

- **`name`** (e `""`): **ordine naturale** del nome, cioè i numeri dentro il nome si confrontano come numeri, non cifra
  per cifra. Maiuscole e minuscole non contano e le lettere accentate stanno accanto a quelle senza accento.

  | `sortDirection` | Ordine atteso |
  |---|---|
  | `Ascending` | Gazebo 1, Gazebo 2, Gazebo 9, Gazebo 10, Gazebo 11, Lettino, Ombrellone |
  | `Descending` | Ombrellone, Lettino, Gazebo 11, Gazebo 10, Gazebo 9, Gazebo 2, Gazebo 1 |

  Il semplice `ORDER BY name` di oggi (`InventoryItemRepository.ListAsync`) non basta: un ordinamento testuale mette
  "Gazebo 10" e "Gazebo 11" prima di "Gazebo 2". Come ottenere l'ordine naturale (per esempio una collation ICU con
  l'ordine numerico, `und-u-kn-true`, che PostgreSQL supporta già ed è deterministica, o un'altra soluzione) lo decide
  lo sviluppatore backend: l'agente glielo chiede prima di scrivere codice; il risultato deve essere quello della
  tabella sopra, con paginazione e `total` fatti dal database su quell'ordine. Se la soluzione cambia la collation del
  nome, la ricerca con `ILIKE` (`InventoryItemRepository.ListAsync`) e l'indice unico su `lower(name)`
  (`uq_catalog_inventory_items_property_name`) devono continuare a funzionare.
- **`totalQuantity`**: per quantità totale (`total_quantity`).
- **`lowStockThreshold`**: per soglia (`low_stock_threshold`). Gli articoli **senza soglia** (`null`) stanno **sempre in
  fondo**, sia con `Ascending` sia con `Descending` (`NULLS LAST`). Oggi `ORDER BY low_stock_threshold DESC` li mette in
  cima: con `Descending` serve `NULLS LAST` esplicito.
- A parità di valore (stessa quantità, stessa soglia, nomi che differiscono solo per maiuscole o accenti) l'ordine è
  quello del nome, naturale e crescente, poi l'id interno (`id`, come oggi `ThenBy(i => i.Id)` in
  `InventoryItemRepository.ListAsync`).

Valori non validi: 400 `validation.failed` con un errore `validation.invalid` sul campo: `page` (meno di 1),
`pageSize` (non 10, 20, 50 o 100), `sortField` (non `name`, `totalQuantity`, `lowStockThreshold` né `""`),
`sortDirection` (non `Ascending` o `Descending`), `filters` (un elemento qualsiasi). `search` oltre 100 caratteri:
`validation.max_length` su `search`, con `args.max` 100 (e `args.min` 0). Il frontend non manda questi valori.

### Risposta

200 nello stile delle tabelle: `total` conta tutti gli articoli dello stabilimento che rispettano la ricerca, `rows` è la
pagina richiesta (al massimo `pageSize` righe).

```json
{
  "total": 3,
  "rows": [
    {
      "idArticle": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e01",
      "name": "Gazebo 1",
      "totalQuantity": 4,
      "lowStockThreshold": 1
    },
    {
      "idArticle": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e02",
      "name": "Lettino",
      "totalQuantity": 120,
      "lowStockThreshold": 5
    },
    {
      "idArticle": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e03",
      "name": "Ombrellone",
      "totalQuantity": 60,
      "lowStockThreshold": null
    }
  ]
}
```

Solo questi campi per riga:

| Campo | In `InventoryItemDto` | Tipo | Note |
|---|---|---|---|
| `idArticle` | `id` | string (UUID) | Il `public_id` dell'articolo. Non si mostra: serve alle azioni della riga. |
| `name` | `name` | string | Colonna "Articoli". |
| `totalQuantity` | `totalQuantity` | number (intero) | Colonna "Totale". |
| `lowStockThreshold` | `lowStockThreshold` | number (intero) o `null` | Colonna "Soglia"; `null` = nessuna soglia, la cella resta vuota. |

Una `page` oltre l'ultima risponde 200 con `rows` vuoto e il `total` vero: il frontend passa da solo all'ultima pagina
che c'è.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `InventoryRead`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Valori non validi"), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra ogni errore di questa API nel popup di errore della tabella (testo fisso, con "Riprova"), senza il
testo del codice.

## 2. POST /api/catalog/article-combobox

Tendina degli articoli del magazzino: il frontend la chiama a ogni apertura del popup "Aggiungi articolo" di questa
pagina e dei pannelli "Aggiungi nuovo articolo/servizio aggiuntivo" e "Modifica articolo/servizio aggiuntivo" del
Listino (tendina "Seleziona articolo"). È **una sola API per le due pagine**: la sezione 12 di
`Listino - Frontend requests.md` rimanda a questa. Segue la regola di tutte le tendine del frontend:
`POST /api/<area>/<cosa>-combobox`, senza corpo né pagine, risposta `[{ id, value }]`.

### Cosa ci serve

- **Un'API nuova**, oppure, in alternativa, **la modifica di `GET /api/catalog/inventory-items/lookup`** perché diventi
  questa: rotta, metodo, risposta e ordine di questa sezione (oggi la lookup è una GET e risponde
  `[{ id, name, articleCode }]` per nome con l'ordine del database). Quale delle due strade seguire lo decide lo
  sviluppatore backend: l'agente glielo chiede prima di scrivere codice. Al frontend serve solo che esista l'API
  descritta qui.
- Gli articoli sono quelli di `GET /api/catalog/inventory-items/lookup` (`InventoryService.LookupAsync`,
  `InventoryItemRepository.ListAllAsync`): tutti quelli dello stabilimento, senza gli eliminati. Se lo sviluppatore
  sceglie di modificare la lookup, `InventoryService.LookupAsync` deve continuare a servire
  `ConfigSnapshotBuilder` (`Application/Services/ConfigSnapshotBuilder.cs`), che la usa per contare gli articoli.
- **Permesso**: `CatalogRead`, come le altre tendine del Listino. Ogni ruolo con `InventoryRead` ha anche
  `CatalogRead` (`Shared/Security/RolePermissions.cs`), quindi la tendina la vede chi apre il Listino o le Impostazioni
  magazzino.

### Richiesta

`POST` senza corpo: lo stabilimento lo prende il backend dal token.

### Risposta

200 con un array di `{ id, value }`, vuoto se lo stabilimento non ha articoli:

```json
[
  { "id": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e01", "value": "Gazebo 1" },
  { "id": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e05", "value": "Gazebo 2" },
  { "id": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e06", "value": "Gazebo 10" },
  { "id": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e02", "value": "Lettino" }
]
```

| Campo | In `InventoryItemLookupDto` | Tipo | Note |
|---|---|---|---|
| `id` | `id` | string (UUID) | Il `public_id` dell'articolo: lo stesso `idArticle` delle righe di `inventory-items-list` e quello che il Listino manda come `idArticle` in `create-additional-services-article-item` e `update-additional-services-article-item` e legge in `get-additional-services-article-item`. È il valore che la tendina salva. |
| `value` | `name` | string | Il nome dell'articolo, il testo mostrato nella tendina. |

Nessun altro campo. Ordine: **ordine naturale del nome**, come `sortField` `name` della sezione 1 ("Gazebo 2" prima di
"Gazebo 10", maiuscole, minuscole e accenti non contano), poi l'id interno (`id`).

### Errori

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza `CatalogRead`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |

Il frontend mostra ogni errore di questa API sotto il campo della tendina, con il testo del suo `code`, e la richiede
alla prossima apertura.

## 3. POST /api/catalog/add-warehouse-item

Bottone "Aggiungi" del popup "Aggiungi articolo" di questa pagina: l'utente sceglie un articolo **che esiste già** nella
tendina (sezione 2) e scrive quanti pezzi aggiungere. È un'**API nuova**: **non crea un articolo**, aumenta la quantità
totale di quello scelto. Oggi il backend può solo sostituire tutto l'articolo (`PUT /api/catalog/inventory-items/{id}`)
o crearne uno nuovo (`POST /api/catalog/inventory-items`).

### Cosa ci serve

- La **quantità totale** dell'articolo diventa quella di prima **più** `totalQuantity`.
- **Soglia e avviso cambiano solo se l'utente scrive una soglia** nel popup, che non mostra quelli attuali: senza
  soglia partono tutti e due `null`, che vuol dire "lascia com'è", così un'aggiunta di pezzi non cancella mai la soglia
  o l'avviso che l'articolo aveva. Con una soglia scritta (anche 0) l'avviso parte `true` o `false`, come lo mostra il
  popup: con soglia 0 sempre `false`.
- **Due aggiunte nello stesso momento sullo stesso articolo devono contare tutte e due**: nessuna somma persa (per
  esempio con un aggiornamento `total_quantity = total_quantity + @quantità` fatto dal database invece di leggere,
  sommare e salvare). Come ottenerlo lo decide lo sviluppatore backend: l'agente glielo chiede prima di scrivere
  codice.
- Nome, codice dell'articolo e tutto il resto non cambiano.
- Nel registro una voce come quella di `PUT /api/catalog/inventory-items/{id}`
  (`catalog.inventory_item_updated`, `AuditActionCodes.Catalog.InventoryItemUpdated`, con il nome dell'articolo).
- Per lo sviluppatore: una somma fatta dal database (per esempio con `ExecuteUpdate`) non passa da `AuditInterceptor`
  (che aggiorna `updated_at`) né dagli snapshot di `UnitOfWork`; l'agente glielo fa presente prima di scrivere codice.

### Richiesta

```json
{
  "idArticle": "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e02",
  "totalQuantity": 10,
  "lowStockThreshold": null,
  "lowStockAlertEnabled": null
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idArticle` | string (UUID) | L'`id` scelto nella tendina (sezione 2): il `public_id` di un articolo dello stabilimento, non eliminato. Sempre presente. |
| `totalQuantity` | number (intero) | Pezzi da aggiungere, maggiore di 0. Sempre presente. |
| `lowStockThreshold` | number (intero) o `null` | Nuova soglia, da 0 alla nuova quantità totale (quella di prima più `totalQuantity`), come la regola di `InventoryItem.Update`. `null` = l'utente non l'ha scritta: la soglia resta quella di prima. |
| `lowStockAlertEnabled` | boolean o `null` | Nuovo stato dell'avviso di soglia (`low_stock_alert_enabled`). `true` solo insieme a una `lowStockThreshold` maggiore di 0 nella stessa richiesta (il popup accende l'avviso solo così). `null` solo quando anche `lowStockThreshold` è `null`: l'avviso resta com'era. |

Valori non validi: 400 `validation.failed` con un elemento di `errors[]` per campo, con `field` uguale al nome del
campo:

- `totalQuantity` assente: `validation.required`; 0 o meno, oppure una nuova quantità totale oltre il massimo di
  `total_quantity` (intero a 32 bit): `validation.out_of_range`;
- `lowStockThreshold` minore di 0 o maggiore della nuova quantità totale: `validation.out_of_range`;
- `lowStockAlertEnabled` `true` senza una `lowStockThreshold` maggiore di 0: `validation.invalid`;
- `idArticle` assente: `validation.required`; un valore che non è un UUID: vedi "Valori che non si leggono" nelle
  regole comuni (il frontend non li manda).

Il frontend mostra sotto il loro campo gli errori di `totalQuantity`, `lowStockThreshold` e
`lowStockAlertEnabled`; quelli su `idArticle` sopra i bottoni.

### Risposta

204 senza corpo. Dopo il 204 il popup si chiude e la tabella si ricarica con `inventory-items-list` (sezione 1).

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `InventoryManage`. |
| 400 | `validation.failed` | Corpo non valido (sopra, "Valori non validi"), con `errors[]`. Le regole che dipendono dalla quantità di prima (soglia fino alla nuova quantità, nuova quantità entro l'intero a 32 bit) valgono sulla quantità che il database somma, non su una letta prima: un'aggiunta fatta nello stesso momento non deve lasciarle violate né portare a un 500. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `inventory.item_not_found` | `idArticle` non è un articolo dello stabilimento (anche perché eliminato). Niente cambia. |

Con ogni errore l'articolo resta com'era. Il frontend mostra gli errori senza campo sopra i bottoni del popup, che
resta aperto con i dati scritti.

## 4. PUT /api/catalog/edit-warehouse-item/{id}

Bottone Salva del popup "Modifica articolo". È **la stessa API di `PUT /api/catalog/inventory-items/{id}`
con un'altra rotta e una regola in più sull'avviso di soglia**: si rinomina la rotta, nello stesso dominio delle altre
API di questa pagina (`add-warehouse-item` della sezione 3). Il frontend chiama solo la rotta nuova.

### Cosa ci serve

- In `Endpoints/InventoryEndpoints.cs` la `MapPut("/{id:guid}", ...)` del gruppo `/inventory-items` (scrittura,
  permesso `InventoryManage`) diventa `PUT /api/catalog/edit-warehouse-item/{id}`, con `{id}` sempre un UUID. La rotta
  vecchia `PUT /api/catalog/inventory-items/{id}` non resta. Con il vincolo `{id:guid}` un `{id}` che non è un UUID
  risponde 404 senza corpo, prima di `inventory.item_not_found`: il frontend manda sempre l'`id` letto.
- **Regola nuova sull'avviso di soglia**, la stessa dell'aggiunta (sezione 3): `lowStockAlertEnabled` può essere
  `true` solo con una `lowStockThreshold` maggiore di 0. Con `true` e una soglia `null` o 0 la risposta è 400
  `validation.failed` con `validation.invalid` su `lowStockAlertEnabled` e l'articolo non cambia. Oggi il backend
  accetta l'avviso acceso anche senza soglia (`SaveInventoryItemRequestValidator` in
  `Application/Validators/InventoryValidators.cs` non ha regole su `LowStockAlertEnabled`). La regola va in
  `SaveInventoryItemRequestValidator`, quindi vale anche per `POST /api/catalog/inventory-items`, che usa lo stesso
  validatore (il frontend non la chiama).
- Articoli già salvati con l'avviso acceso e la soglia vuota o 0: lo sviluppatore decide se correggerli (per esempio
  con una migration che spegne l'avviso); l'agente glielo chiede. Il frontend li mostra con l'avviso spento e al Salva
  manda `false`.
- Tutto il resto resta com'è: `InventoryService.UpdateAsync`, corpo `SaveInventoryItemRequest`, risposta 200 con
  `InventoryItemDto`, le altre regole (`SaveInventoryItemRequestValidator`, nome unico), voce nel registro
  `catalog.inventory_item_updated` ed errori. Il totale può essere anche 0 (`GreaterThanOrEqualTo(0)`): il frontend
  lo manda.
- Il documento `89-api-catalog.md` del collega descrive la rotta vecchia: aggiornarlo tocca allo sviluppatore, non al
  frontend.

### Richiesta

`{id}` è l'`id` dell'articolo letto con `GET /api/catalog/inventory-items/{id}` (il `public_id`, l'`idArticle` delle
righe). Il corpo è tutto l'articolo: il frontend rimanda `articleCode` e `name` letti, invariati, con totale, soglia e
avviso del popup.

```json
{
  "articleCode": "sunbed",
  "name": "Lettino",
  "totalQuantity": 110,
  "lowStockThreshold": 8,
  "lowStockAlertEnabled": true
}
```

### Risposta

200 con `InventoryItemDto`, come oggi: il frontend non la legge, chiude il popup e ricarica la tabella.

### Errori

Gli stessi di oggi, più la regola sull'avviso:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `InventoryManage`. |
| 400 | `validation.failed` | Corpo non valido (`SaveInventoryItemRequestValidator`, con la regola sull'avviso), con `errors[]`. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `inventory.item_not_found` | `{id}` non è un articolo dello stabilimento (anche perché eliminato). |
| 409 | `resource.version_conflict` | La versione in `If-Match` non è più quella dell'articolo (sezione 7). Niente cambia. |
| 409 | `inventory.name_taken` | Un altro articolo dello stabilimento ha già questo nome. |

Il frontend mostra sotto il loro campo gli errori su `totalQuantity`, `lowStockThreshold` e `lowStockAlertEnabled`;
gli altri (anche su `name` e `articleCode`, 404 e 409) sopra i bottoni del popup.

## 5. POST /api/catalog/delete-warehouse-item

**L'unica API con cui il frontend elimina gli articoli**, dopo la conferma nel popup: il cestino di una riga la chiama
con un id, "Elimina selezione" della tabella (compare con almeno due righe scelte) con tutti gli id scelti. Elimina
**uno o più articoli in una sola chiamata, tutto o niente**. È un'**API nuova**: il frontend non usa
`DELETE /api/catalog/inventory-items/{id}`, che resta com'è.

### Cosa ci serve

- **Tutto o niente**: se anche un solo articolo della lista non si può eliminare, non si elimina nessuno e la risposta
  è l'errore di quell'articolo. Il popup chiede all'utente di eliminare quegli articoli insieme: non deve ritrovarsi
  con una parte eliminata e una no.
- `POST /api/catalog/inventory-items/bulk-delete`, che il backend ha già (`InventoryService.DeleteManyAsync`, con
  `BulkIdsRequest` e `BulkResult` di `Shared/Dtos/BulkResult.cs`), fa un'altra cosa: elimina quelli possibili e
  restituisce gli altri come falliti, con un 200. Per questa pagina non va bene: resta com'è, il frontend non la usa.
- Per ogni articolo valgono gli stessi controlli e lo stesso effetto di `DELETE /api/catalog/inventory-items/{id}`
  (`InventoryService.DeleteAsync`): l'articolo deve essere dello stabilimento e non usato nei listini né nei listini
  delle convenzioni (`InUseErrorAsync`, `ListUsagesAsync`); si elimina con il soft delete di sempre e nel registro va
  una voce `catalog.inventory_item_deleted` per ogni articolo.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodo nuovo di `InventoryService`, controlli per più articoli
  insieme, transazione...), purché il risultato sia questo. Il controllo "articolo non usato" e l'eliminazione non sono
  atomici rispetto a un listino salvato nello stesso momento, come già con la `DELETE` di oggi: l'agente chiede allo
  sviluppatore se va gestito.

### Richiesta

```json
{
  "idItems": ["0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e02", "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e03"]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idItems` | array di string (UUID) | Gli `idArticle` delle righe della tabella (il `public_id` degli articoli), almeno uno: uno solo per il cestino di una riga, tutti quelli scelti per "Elimina selezione". Lo stesso id ripetuto conta una volta. La selezione della tabella può comprendere righe di più pagine, quindi anche tutti gli articoli dello stabilimento. |

Il frontend non ha un tetto al numero di id. Se ne serve uno (come i 300 di `BulkIdsRequest.MaxIds`), lo decide lo
sviluppatore backend: l'agente glielo chiede prima di scrivere codice e lo scrive qui, così il frontend lo rispetta.

### Risposta

204 senza corpo. Dopo il 204 la tabella si ricarica.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `InventoryManage`. |
| 400 | `validation.failed` | `idItems` assente o vuoto: `validation.required` su `idItems` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `inventory.item_not_found` | Un id non è un articolo dello stabilimento (anche perché già eliminato). Non si elimina nessun articolo. |
| 409 | `inventory.item_in_use` | Un articolo è ancora usato. Non si elimina nessun articolo. `args` come in `DELETE /api/catalog/inventory-items/{id}` (`priceLists` e `agreementPriceLists`, gli elenchi dei nomi) e **in più `articleName`**, il nome di quell'articolo, anche con un id solo: il messaggio dice sempre quale. |

Prima si controlla che tutti gli id siano articoli dello stabilimento (404 se uno non lo è), poi che nessuno sia usato:
con più articoli in uso, `articleName` e gli elenchi sono quelli del primo in uso nell'ordine di `idItems`.

```json
{
  "type": "https://errors.summerbooking/inventory/item_in_use",
  "title": "inventory.item_in_use",
  "status": 409,
  "code": "inventory.item_in_use",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "args": {
    "articleName": "Lettino",
    "priceLists": ["Listino estate"],
    "agreementPriceLists": ["Hotel Mare – Listino convenzione"]
  }
}
```

Il frontend mostra ogni errore di questa API nell'avviso "Operazione non riuscita", con il testo del suo `code`: per
`inventory.item_in_use` "L'articolo {{articleName}} è usato nei listini {{usedIn}} e non si può eliminare: toglilo
prima da questi listini.", dove `usedIn` sono i nomi di `priceLists` e `agreementPriceLists` uniti dal frontend.

## 6. POST /api/catalog/duplicate-warehouse-item

**L'unica API con cui il frontend duplica gli articoli**, dopo la conferma nel popup: il bottone "Duplica" di una riga
la chiama con un id, "Duplica selezione" della tabella con tutti gli id scelti. Duplica **uno o più articoli in una
sola chiamata, tutto o niente**. È un'**API nuova**: oggi il backend non ha un modo di duplicare un articolo
(`Endpoints/InventoryEndpoints.cs`).

### Cosa ci serve

- **Tutto o niente**, come l'eliminazione (sezione 5): se un id non va bene non si duplica nessun articolo.
- Per ogni articolo della lista un articolo nuovo dello stesso stabilimento, con un `public_id` nuovo e:
  - **gli stessi dati dell'articolo di partenza**: `article_code`, `total_quantity` (la copia parte con la stessa
    quantità), `low_stock_threshold` e `low_stock_alert_enabled`;
  - il **nome lo sceglie il backend**, come le altre copie del Catalog con `CopyNames.Next`
    (`Domain/CopyNames.cs`): "Lettino copia", poi "Lettino copia 2", "Lettino copia 3"; la copia di una copia riparte
    dal nome di base (se "Lettino copia" e "Lettino copia 2" esistono già, la copia di "Lettino copia" è
    "Lettino copia 3", non "Lettino copia copia"). Deve rispettare le regole di
    `POST /api/catalog/inventory-items`: unico fra gli articoli non eliminati dello stabilimento senza distinguere
    maiuscole e minuscole (indice `uq_catalog_inventory_items_property_name`) e al massimo 120 caratteri
    (`InventoryItem.NameMaxLength`), accorciando il nome di partenza se serve;
  - `created_by_user_id` dell'utente del token, come `InventoryService.CreateAsync`;
  - è solo l'articolo: i listini e i listini delle convenzioni che usano l'articolo di partenza restano suoi, la copia
    non è usata da nessuno e non ha consumi delle prenotazioni;
  - nel registro una voce per ogni copia;
  - mai un 500 per il nome di una copia, anche se nello stesso momento un altro utente salva quel nome o duplica lo
    stesso articolo: oggi il controllo del nome (`EnsureNameFreeAsync` di `InventoryService`) e l'indice unico non
    bastano, perché la violazione dell'indice diventa un 500 `server.unexpected`.
- Se lo stesso id compare più volte, si fa una copia sola.
- **Come farlo lo sceglie lo sviluppatore del backend** (metodo nuovo di `InventoryService`, transazione, come evitare
  il 500 sul nome...), purché il risultato sia questo. L'agente gli chiede anche **quale voce del registro** usare per
  le copie: `catalog.inventory_item_created`, come la creazione, oppure un codice nuovo
  `catalog.inventory_item_duplicated`, come `catalog.service_duplicated` (`Shared/Contracts/Audit/AuditActionCodes.cs`).
  Il frontend non legge il registro.
- Il database non ha un vincolo "soglia fino alla quantità totale" (c'è solo `ck_catalog_inventory_items_total_quantity`,
  `total_quantity >= 0`): un articolo vecchio con la soglia oltre il totale, copiato con `InventoryItem.Create`, oggi
  darebbe un 500 (`ArgumentException` in `InventoryItem.Update`). Cosa fare in quel caso lo decide lo sviluppatore:
  l'agente glielo chiede; se diventa un errore, va scritto qui con status e `code`.

### Richiesta

```json
{
  "idItems": ["0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e02", "0193a5c2-7b1e-7c4d-9e2f-3a4b5c6d7e03"]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idItems` | array di string (UUID) | Gli `idArticle` delle righe della tabella (il `public_id` degli articoli), almeno uno: uno solo per il bottone "Duplica" di una riga, tutti quelli scelti per "Duplica selezione". Lo stesso id ripetuto conta una volta. La selezione della tabella può comprendere righe di più pagine. |

Per un eventuale tetto al numero di id vale la nota della sezione 5.

### Risposta

204 senza corpo. Dopo il 204 la tabella si ricarica e le copie compaiono al loro posto nell'ordine della tabella.

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `InventoryManage`. |
| 400 | `validation.failed` | `idItems` assente o vuoto: `validation.required` su `idItems` in `errors[]`. Il frontend non lo manda. |
| 403 | `catalog.property_required` | Token senza stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 404 | `inventory.item_not_found` | Un id non è un articolo dello stabilimento (anche perché già eliminato). Non si duplica nessun articolo. |

Il frontend mostra ogni errore di questa API nell'avviso "Operazione non riuscita", con il testo del suo `code`.

## 7. Controllo di versione sui salvataggi di modifica

Oggi, se due utenti modificano lo stesso articolo, vince chi salva per ultimo: il popup "Modifica articolo" rimanda
tutto l'articolo letto all'apertura e cancella, senza che nessuno se ne accorga, quello che un altro utente ha cambiato
nel frattempo (per esempio i pezzi aggiunti con la sezione 3). Il backend non ha un controllo di concorrenza sugli
aggiornamenti: nessun token di concorrenza in `InventoryItemConfiguration`, nessun `If-Match`. Il frontend chiede lo
schema di **concorrenza ottimistica** che il backend ha già progettato per le prenotazioni offline
(`shared documentation/backend/progettazione/19-frontend-offline.md`, sezioni 4.4 e 5.8), con un **codice di errore
unico per tutte le API**: `resource.version_conflict`.

### Cosa ci serve

- **Versione nelle letture**: la lettura della tabella sotto restituisce, insieme ai dati, il campo `version` (stringa
  opaca). Cambia ogni volta che cambiano i dati che il salvataggio corrispondente scrive, anche quando li cambia
  un'altra API (per esempio `add-warehouse-item` della sezione 3 o la `DELETE`), e solo allora.
- **`If-Match` nei salvataggi**: il frontend manda la versione letta nell'header `If-Match: "<version>"`. Se non è più
  quella attuale, il backend non salva niente e risponde **409 `resource.version_conflict`**, senza `errors[]`. Senza
  `If-Match` risponde allo stesso modo: il frontend lo manda sempre.
- **`ETag` nelle risposte**: ogni salvataggio riuscito restituisce la nuova versione nell'header `ETag: "<version>"`,
  anche quando risponde 204 senza corpo.
- Il 409 `resource.version_conflict` si controlla dopo il 404 dell'elemento e prima degli altri 409 (per esempio
  `inventory.name_taken`).
- **Come farlo lo sceglie lo sviluppatore del backend** (il token `xmin` di Npgsql come nel documento offline, un hash
  dei dati coperti dalla versione, un contatore...): l'agente glielo chiede prima di scrivere codice. Il frontend tratta
  la versione come una stringa opaca e non la confronta mai.

### API coinvolte

| Lettura (restituisce `version`) | Salvataggio (manda `If-Match`, risponde `ETag`) | Dati coperti dalla versione |
|---|---|---|
| `GET /api/catalog/inventory-items/{id}` (`InventoryItemDto` con in più `version`) | `PUT /api/catalog/edit-warehouse-item/{id}` (sezione 4) | Tutto l'articolo |

Le altre API di questo documento non ne hanno bisogno: l'aggiunta di pezzi (sezione 3) somma nel database, eliminazione
e duplicazione (sezioni 5 e 6) non rimandano dati letti.

### Cosa fa il frontend

Con 409 `resource.version_conflict` il popup "Modifica articolo" si chiude, compare l'avviso "Nel frattempo un altro
utente ha modificato questi dati: li abbiamo ricaricati. Rifai le tue modifiche." e la tabella si ricarica con
`inventory-items-list`: riaprendo la matita il popup rilegge l'articolo con la `GET`.

## API esistenti usate così come sono

Il frontend le chiama senza modifiche; qui solo come le usa e dove mostra le risposte.

| Azione | API | Corpo | Risposta | Errori e dove li mostra il frontend |
|---|---|---|---|---|
| Apertura del popup "Modifica articolo" (matita di una riga) | `GET /api/catalog/inventory-items/{id}`, con l'`idArticle` della riga | Nessuno | 200 con `InventoryItemDto` (più `version`, sezione 7): il popup mostra totale, soglia e avviso e tiene da parte `articleCode`, `name` e `version` | Il popup si chiude e compare l'avviso di errore con il testo del `code`, Chiudi e Riprova (per un 404 `inventory.item_not_found` solo Chiudi). |

## Verifica

API 1, con uno stabilimento di prova con 12 articoli e un token con `inventory.read`:

1. Con `page` 1, `pageSize` 10, `search` `""`, `sortField` `""`: `total` 12, 10 righe in ordine naturale di nome, ognuna
   con solo `idArticle`, `name`, `totalQuantity` e `lowStockThreshold` (gli `id`, `name`, `totalQuantity` e
   `lowStockThreshold` di `GET /api/catalog/inventory-items`); `page` 2 dà le ultime 2; `page` 3 dà `rows` vuoto e
   `total` 12.
2. `sortField` `name` con articoli "Gazebo 1", "Gazebo 2", "Gazebo 9", "Gazebo 10", "Gazebo 11", "gazebo 3": righe in
   ordine naturale, Gazebo 1, Gazebo 2, gazebo 3, Gazebo 9, Gazebo 10, Gazebo 11; con `Descending` l'ordine opposto;
   "Gazebo 10" non viene mai prima di "Gazebo 2".
3. `sortField` `lowStockThreshold` con soglie 5, `null`, 2, `null`: con `Ascending` 2, 5, poi i due senza soglia; con
   `Descending` 5, 2, poi i due senza soglia. A parità di soglia, gli articoli per nome.
4. `sortField` `totalQuantity`: righe per quantità totale, a parità di quantità per nome.
5. `search` `lett` trova gli articoli che hanno "lett", "Lett" o "LETT" nel nome, e `total` conta solo quelli; `search`
   `%` trova solo i nomi che contengono il carattere `%`.
6. Un articolo eliminato (`deleted_at` valorizzato) non compare e non conta in `total`.
7. Un articolo di un altro stabilimento non compare mai.
8. Con articoli "Èlite", "Elite 2" e "elite 10": con `name` e `Ascending` "Èlite" sta accanto agli "Elite"/"elite",
   non in fondo, e "elite 10" viene dopo "Elite 2".
9. `pageSize` 25, `page` 0, `sortField` `thresholdQuantity`, `sortDirection` `asc` o `filters` con un elemento: 400
   `validation.failed` con `validation.invalid` sul campo; `search` di 101 caratteri: 400 con `validation.max_length` su
   `search`.
10. Senza token 401 `auth.unauthenticated`; senza `inventory.read` 403 `auth.forbidden`; token senza stabilimento 403
    `catalog.property_required`; stabilimento del token che non esiste 404 `catalog.property_not_found`.

API 2, con lo stesso stabilimento e lo stesso token:

1. Risposta 200 con i 12 articoli, ognuno con solo `id` (il `public_id`) e `value` (il nome) degli articoli non
   eliminati dello stabilimento, in ordine naturale di nome: "Gazebo 2" prima di "Gazebo 10".
2. Un articolo eliminato non compare; un articolo di un altro stabilimento non compare mai.
3. Uno stabilimento senza articoli: 200 con `[]`.
4. Con il token di un ruolo che ha `catalog.read` ma non `inventory.read` (per esempio `waiter` o `marketing`): 200.
   Senza token 401 `auth.unauthenticated`; con un ruolo senza `catalog.read` (per esempio `consumer`) 403
   `auth.forbidden`; token senza stabilimento 403 `catalog.property_required`.

API 3, con un token con `inventory.manage` e un articolo con quantità 100, soglia 5 e avviso acceso:

1. `totalQuantity` 10, `lowStockThreshold` `null`, `lowStockAlertEnabled` `null`: 204; l'articolo ha quantità 110,
   soglia 5 e avviso acceso; nome e codice uguali; nel registro una voce `catalog.inventory_item_updated`.
2. `totalQuantity` 5, `lowStockThreshold` 8, `lowStockAlertEnabled` `false`: 204; quantità 115, soglia 8, avviso
   spento. Poi `totalQuantity` 1, `lowStockThreshold` 0, `lowStockAlertEnabled` `false`: 204; soglia 0, avviso
   spento.
3. Dieci richieste da `totalQuantity` 1 mandate insieme sullo stesso articolo: alla fine la quantità è aumentata di 10.
4. `totalQuantity` 0: 400 con `validation.out_of_range` su `totalQuantity`; `lowStockThreshold` maggiore della nuova
   quantità: 400 con `validation.out_of_range` su `lowStockThreshold`; `lowStockAlertEnabled` `true` con
   `lowStockThreshold` `null` o 0: 400 con `validation.invalid` su `lowStockAlertEnabled`. L'articolo non cambia.
5. `idArticle` di un altro stabilimento o di un articolo eliminato: 404 `inventory.item_not_found`, niente cambia.
6. `totalQuantity` 10 e `lowStockThreshold` 105 (oltre la quantità di prima, entro la nuova): 204; quantità 110,
   soglia 105.
7. Senza `inventory.manage` 403 `auth.forbidden`; senza token 401 `auth.unauthenticated`; token senza stabilimento 403
   `catalog.property_required`.

API 4, con un token con `inventory.manage`:

1. `PUT /api/catalog/edit-warehouse-item/{id}` con il corpo di un articolo letto e il totale cambiato: 200 con
   l'articolo salvato, come rispondeva `PUT /api/catalog/inventory-items/{id}`; nel registro una voce
   `catalog.inventory_item_updated`.
2. Soglia oltre il totale: 400 con `validation.out_of_range` su `lowStockThreshold`; `{id}` di un altro stabilimento:
   404 `inventory.item_not_found`; senza `inventory.manage` 403 `auth.forbidden`.
3. `lowStockAlertEnabled` `true` con `lowStockThreshold` `null` o 0: 400 con `validation.invalid` su
   `lowStockAlertEnabled` e l'articolo non cambia; con soglia 3 e totale 10: 200.
4. `totalQuantity` 0 e `lowStockThreshold` `null`, avviso spento: 200, l'articolo ha 0 pezzi.
5. `POST /api/catalog/inventory-items` con l'avviso acceso e la soglia `null`: 400 con `validation.invalid` su
   `lowStockAlertEnabled`.

API 5, con un token con `inventory.manage`:

1. `delete-warehouse-item` di due articoli non usati: 204; non compaiono più in `GET /api/catalog/inventory-items` e
   nel registro ci sono due voci `catalog.inventory_item_deleted`. Con un solo id non usato: 204 e una voce.
2. `delete-warehouse-item` di un articolo non usato e di uno usato in un listino: 409 `inventory.item_in_use` con
   `args` dell'articolo usato, `articleName` compreso; nessuno dei due è eliminato.
3. Un id di un altro stabilimento o già eliminato: 404 `inventory.item_not_found` e nessun articolo cambia.
4. `idItems` vuoto: 400 `validation.failed` con `validation.required` su `idItems`; senza `inventory.manage` 403
   `auth.forbidden`.
5. Un solo id di un articolo usato in un listino: 409 `inventory.item_in_use` con `articleName`, `priceLists` e
   `agreementPriceLists`; l'articolo non è eliminato.
6. Lo stesso id due volte (`[A, A]`) di un articolo non usato: 204 e una sola voce nel registro.
7. Due articoli usati, B prima di C in `idItems`: 409 con gli `args` di B; nessuno è eliminato.
8. Token senza stabilimento: 403 `catalog.property_required`.

API 6, con un token con `inventory.manage` e l'articolo "Lettino" con `article_code` `sunbed`, quantità 120, soglia 5,
avviso acceso e usato in un listino:

1. `duplicate-warehouse-item` con l'id di "Lettino": 204; in `GET /api/catalog/inventory-items` c'è un articolo nuovo
   "Lettino copia", con un id diverso, `sunbed`, quantità 120, soglia 5 e avviso acceso; "Lettino" non cambia e resta
   nel suo listino, la copia non è in nessun listino; nel registro una voce per la copia.
2. Di nuovo con l'id di "Lettino": "Lettino copia 2". Poi con l'id di "Lettino copia": "Lettino copia 3".
3. Con due id diversi: 204 e due copie; con lo stesso id ripetuto due volte: una copia sola.
4. Un articolo con un nome di 120 caratteri: la copia ha al massimo 120 caratteri e finisce con " copia".
5. Un id valido e uno di un altro stabilimento o già eliminato: 404 `inventory.item_not_found` e nessuna copia, nemmeno
   dell'articolo valido.
6. `idItems` vuoto: 400 `validation.failed` con `validation.required` su `idItems`; senza `inventory.manage` 403
   `auth.forbidden`; senza token 401 `auth.unauthenticated`.
7. Due `duplicate-warehouse-item` dello stesso articolo mandate insieme: 204 tutte e due e due copie con nomi diversi,
   mai un 500.
8. Un `duplicate-warehouse-item` di "Lettino" mentre un altro utente salva un articolo di nome "Lettino copia": mai un
   500 e mai due articoli non eliminati con lo stesso nome.

API 7, con un token con `inventory.manage`:

1. `GET /api/catalog/inventory-items/{id}` risponde con `version` (per esempio `v1`); `PUT /api/catalog/edit-warehouse-item/{id}`
   con `If-Match: "v1"`: 200 con l'header `ETag` di una versione nuova, diversa da `v1`; una nuova `GET` risponde con
   quella versione.
2. Lo stesso `PUT` con `If-Match: "v1"` dopo il primo: 409 `resource.version_conflict` senza `errors[]`; l'articolo non
   cambia.
3. `GET` (versione `v1`), poi `add-warehouse-item` di 10 pezzi sullo stesso articolo, poi `PUT` con `If-Match: "v1"`:
   409 `resource.version_conflict`; i 10 pezzi restano.
4. `PUT` senza `If-Match`: 409 `resource.version_conflict`.
