# Stagioni e periodi - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Stagioni e periodi** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/seasons`: in alto le date delle stagioni, sotto due tab, "Stagione Corrente"
(`/settings/seasons/1`) e "Prossima Stagione" (`/settings/seasons/2`), ognuna con i periodi tariffari della sua stagione
e i loro intervalli di date. In una tab si possono spuntare intervalli di periodi diversi e cancellarli tutti insieme
con "Cancella selezionati"; il cestino di un intervallo ne cancella uno solo.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/tariff-periods/bulk-delete-ranges` | Cancella intervalli di uno o più periodi della stagione corrente, tutti o nessuno |
| 2 | POST | `/api/catalog/next-season/tariff-periods/bulk-delete-ranges` | Lo stesso per la stagione successiva |
| 3 | DELETE, POST | `/api/catalog/tariff-periods/{key}`, `/api/catalog/[next-season/]tariff-periods` | **Modifica**: prima di eliminare controlla anche i servizi che usano il periodo; la `key` di un periodo eliminato non si riusa |
| 4 | POST, PUT | `/api/catalog/[next-season/]tariff-periods[/{key}]` | **Modifica**: nessun giorno in comune tra periodi diversi della stessa stagione |
| 5 | POST, PUT, DELETE | `/api/catalog/[next-season/]tariff-periods[/{key}]` e le API 1 e 2 | **Modifica**: una scrittura alla volta sui periodi di una stagione; una sovrapposizione rifiutata dal database risponde 409, mai 500 |

Le API 1 e 2 sono **nuove**: oggi un intervallo si cancella solo con `PUT /tariff-periods/{key}`, un periodo alla volta.
Il frontend ha bisogno di cancellare con **una sola chiamata** intervalli di periodi diversi, e di non cancellarne
nessuno se uno non si può cancellare. La 3, la 4 e la 5 sono **modifiche** ad API che esistono già.

## Regole comuni

Valgono per le API 1 e 2.

- **Modulo e rotte**: Catalog (`Module.Catalog`), in `Endpoints/SeasonEndpoints.cs` e `SeasonService`, nei gruppi
  `/tariff-periods` e `/next-season/tariff-periods`.
- **Le API esistenti non cambiano**: `/api/catalog/season`, `/api/catalog/tariff-periods` e
  `/api/catalog/next-season/tariff-periods`, con le loro sotto-rotte (`GET`, `POST`, `PUT` e `DELETE /{key}`), restano
  come sono; le due nuove si aggiungono. Le uniche modifiche sono quelle delle sezioni 3, 4 e 5.
- **Stabilimento dal token**: come per le altre API dei periodi (`CurrentProperty`); nessuna richiesta lo contiene.
  Token senza stabilimento: 403 `catalog.property_required`; stabilimento del token non trovato: 404
  `catalog.property_not_found`.
- **Permessi**: come `PUT /tariff-periods/{key}` (`CatalogSettings`). Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Le date sono giorni `yyyy-MM-dd`, come in `DateRangeDto`.
- **Tutti o nessuno**: diversamente dalle altre `bulk-delete` del Catalog, che rispondono 200 con `succeeded` e
  `failed` e cancellano quello che possono, queste due fanno tutti i controlli prima di cancellare e salvano in una sola
  transazione. Se un controllo fallisce non si cancella niente.
- **Errori** nel formato Problem Details del backend (`ProblemDetailsWriter`), con `code`. Senza token valido: 401
  `auth.unauthenticated`.

## 1. POST /api/catalog/tariff-periods/bulk-delete-ranges

Tab "Stagione Corrente": bottone "Cancella selezionati" e cestino di un intervallo, dopo la conferma nel popup.

### Richiesta

Gli intervalli da cancellare, anche di periodi diversi:

```json
{
  "ranges": [
    { "key": 0, "startsOn": "2026-09-01", "endsOn": "2026-09-30" },
    { "key": 1, "startsOn": "2026-08-21", "endsOn": "2026-08-31" }
  ]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `ranges` | array | Obbligatorio, da 1 a 300 elementi (`BulkIdsRequest.MaxIds`, come le altre `bulk-delete` del Catalog). |
| `ranges[].key` | number | Obbligatorio. La `key` del periodo, come in `GET /tariff-periods`. |
| `ranges[].startsOn` | string (`yyyy-MM-dd`) | Obbligatorio. Primo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |
| `ranges[].endsOn` | string (`yyyy-MM-dd`) | Obbligatorio. Ultimo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |

- Il DTO ha i campi nullable (es. `IReadOnlyList<…>? Ranges` e, per ogni elemento, `short? Key`, `DateOnly? StartsOn`,
  `DateOnly? EndsOn`): un campo assente o `null` diventa un errore `validation.required` del validatore. Con `short` e
  `DateOnly` diventerebbe `0` o `0001-01-01` e la richiesta passerebbe al controllo successivo.
- Un intervallo si riconosce da `key`, `startsOn` ed `endsOn` insieme: dentro un periodo gli intervalli non si
  sovrappongono, quindi non ce ne sono due con le stesse date. Lo stesso intervallo ripetuto nella lista conta una
  volta.

### Cosa fa

Tocca solo le righe elencate, senza riscrivere i periodi: niente `ReplacePeriodAsync` e niente controlli di nome, di
giorni della stagione o di sovrapposizioni, che servono solo a chi scrive date nuove. In quest'ordine:

1. Controlla la richiesta con un validatore FluentValidation, come `BulkIdsRequestValidator`
   (`Application/Validators/InventoryValidators.cs`): `ranges` non vuota e al massimo 300 elementi, ogni elemento con
   i tre campi. Se non va, 400 e nient'altro.
2. Prende lo stabilimento dal token (403 / 404 `catalog.property_*`) e la stagione (`RequireSeasonAsync`: 422
   `pricing.season_required`).
3. Apre una transazione e blocca la riga della stagione (sezione 5): un'altra cancellazione, o un altro salvataggio dei
   periodi della stessa stagione, aspetta che questa finisca. Solo dopo il blocco legge le righe non eliminate dei
   periodi della stagione (`catalog.pricing_tariff_periods`), così vede quello che l'altra ha già salvato.
4. Toglie dalla lista i doppioni.
5. Ogni `key` della lista deve essere un periodo della stagione (almeno una riga con quel `period_group_key`):
   altrimenti 404 `pricing.period_not_found`.
6. Ogni elemento deve corrispondere a una riga di quel periodo con le stesse `starts_on` ed `ends_on`: altrimenti 404
   `pricing.period_range_not_found`.
7. Ogni periodo della lista deve tenere almeno una riga che non è nella lista: altrimenti 422
   `pricing.period_ranges_required`.
8. Elimina solo le righe elencate, con il soft delete di sempre (`Remove`, che `SoftDeleteInterceptor` trasforma in
   `deleted_at`). Le altre righe non cambiano: stesso id, stesso nome, stessa `key`. I periodi che non compaiono nella
   lista restano come sono.
9. Salva, scrive nel registro una voce `catalog.tariff_period_updated` per ogni periodo toccato (`key`, `name` e
   `season` = `current`, come `RecordPeriodAsync`) e chiude la transazione.

### Risposta

204 senza corpo. Il frontend poi ricarica i periodi con `GET /tariff-periods`, anche dopo un 404.

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `ranges` assente o vuota: `errors[]` con `field` `ranges` e `code` `validation.required`. |
| 400 | `validation.failed` | Più di 300 elementi: `errors[]` con `field` `ranges` e `code` `validation.invalid`. |
| 400 | `validation.failed` | Un elemento senza `key`, `startsOn` o `endsOn` (assente o `null`): un errore per campo mancante, con `field` `ranges[i].key`, `ranges[i].startsOn` o `ranges[i].endsOn` (`i` da 0) e `code` `validation.required`. |
| 400 | `validation.failed` | JSON che non si legge (es. la data `2026-02-30` o una `key` che non è un numero): senza `errors[]`, come per le altre API. |
| 403 | `catalog.property_required` | Il token non ha lo stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 422 | `pricing.season_required` | Lo stabilimento non ha la stagione. |
| 404 | `pricing.period_not_found` | Una `key` non è un periodo della stagione. |
| 404 | `pricing.period_range_not_found` (nuovo) | Un intervallo non c'è nel suo periodo: `startsOn` ed `endsOn` non sono le date di un intervallo di quella `key`. |
| 422 | `pricing.period_ranges_required` (nuovo) | Un periodo resterebbe senza intervalli, senza `args`. Il periodo intero si cancella con `DELETE /tariff-periods/{key}`. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

Il primo controllo che non passa decide la risposta, nell'ordine di "Cosa fa". Esempio di risposta a un elemento
senza `endsOn`:

```json
{
  "type": "https://errors.summerbooking/validation/failed",
  "title": "validation.failed",
  "status": 400,
  "code": "validation.failed",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "errors": [
    { "field": "ranges[1].endsOn", "code": "validation.required", "args": {} }
  ]
}
```

I due codici nuovi vanno aggiunti a `CatalogErrors`, accanto agli altri `pricing.period_*`.

## 2. POST /api/catalog/next-season/tariff-periods/bulk-delete-ranges

Tab "Prossima Stagione": come la sezione 1, sulle righe della stagione successiva
(`catalog.pricing_next_season_periods`). Cambia solo questo:

- le `key` sono quelle di `GET /next-season/tariff-periods`;
- la stagione si controlla in due passi, in quest'ordine, come `RequireNextSeasonAsync`: se lo stabilimento non ha
  nessuna stagione, 422 `pricing.season_required`; se ha la stagione ma non la successiva (`next_season_starts_on`
  nullo), 422 `pricing.next_season_required`;
- le voci del registro sono quelle delle altre API della stagione successiva (`season` = `next`).

Richiesta, risposta, ordine delle operazioni, blocco della stagione e gli altri errori sono gli stessi della sezione 1.

## 3. DELETE /api/catalog/tariff-periods/{key}: controllare anche i servizi

API che esiste già (cestino di un periodo nella tab "Stagione Corrente"): rotta, risposta ed errori di oggi restano
uguali, cambia il controllo che si fa prima di eliminare. In più la `key` di un periodo eliminato non va data a un
periodo nuovo (vedi "La `key` di un periodo eliminato non si riusa").

### Oggi

`SeasonService.DeletePeriodAsync` chiede a `SeasonRepository.ListPeriodUsagesAsync`
(`Infrastructure/Repositories/SeasonSectorRepositories.cs`) dove il periodo è usato e, se lo è, risponde 409
`pricing.period_in_use` con i nomi in `args.priceLists`. Il controllo guarda solo i prezzi dei listini:

- `catalog.pricing_price_list_period_prices` (listini dello stabilimento);
- `catalog.agreement_price_list_period_prices` (listini delle convenzioni).

Altre tabelle del database si legano a un periodo con la sua chiave (`tariff_period_group` / `period_group_key`), ma
non vengono guardate:

| Tabella | Cosa contiene |
|---|---|
| `catalog.service_sale_item_period_prices` | I prezzi per periodo dei servizi (bar, servizi generici). |
| `catalog.service_hourly_options` | Le opzioni dei servizi orari legate a un periodo (`period_group_key` non nullo). |
| `catalog.pricing_suggested_prices` | I prezzi suggeriti, per listino e periodo. |

Un periodo usato solo da queste righe si elimina, e le righe restano legate a una chiave che non indica più nessun
periodo della stagione.

### Cosa cambiare

- Il controllo dei listini resta il primo. Dopo, il backend cerca le righe che usano la `key` del periodo:
  - `catalog.service_sale_item_period_prices` con `tariff_period_group` uguale alla `key`;
  - `catalog.service_hourly_options` con `period_group_key` uguale alla `key`.

  Contano solo le righe non eliminate (`deleted_at` nullo) di servizi non eliminati dello stabilimento del token:
  legate con `sale_item_id` a `catalog.service_sale_items`, con `deleted_at` nullo e `property_id` dello stabilimento.
  Le righe di servizi eliminati o di altri stabilimenti non bloccano l'eliminazione.
- Se il periodo ha prezzi nei listini, la risposta resta quella di oggi: 409 `pricing.period_in_use` con
  `args.priceLists`.
- Se non ha prezzi nei listini ma è usato da dei servizi, risponde 409 con un **codice nuovo**,
  `pricing.period_in_use_by_services`, e in `args.services` l'elenco dei nomi dei servizi (`service_sale_items.name`,
  quello della tabella della pagina Servizi), ognuno una volta, come `args.priceLists` è l'elenco dei listini:

```json
{
  "type": "https://errors.summerbooking/pricing/period_in_use_by_services",
  "title": "pricing.period_in_use_by_services",
  "status": 409,
  "code": "pricing.period_in_use_by_services",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "args": { "services": ["Caffè", "Campo da beach volley"] }
}
```

- `pricing_suggested_prices`: da valutare dal backend. Se sono suggerimenti che il sistema rifà da solo, si possono
  eliminare insieme al periodo; altrimenti vanno controllati come i listini.
- Il codice nuovo va aggiunto a `CatalogErrors`, accanto a `pricing.period_in_use`. Il frontend lo mostra nell'avviso
  "Operazione non riuscita", come gli altri errori dell'eliminazione.
- La prossima stagione (`DELETE /api/catalog/next-season/tariff-periods/{key}`) oggi non fa nessun controllo: i prezzi
  di listini e servizi si legano ai periodi della stagione corrente, quindi lì non cambia niente.

### La `key` di un periodo eliminato non si riusa

Oggi `SeasonService.CreatePeriodAsync` dà al periodo nuovo la `key` più alta tra i periodi che esistono, più 1
(`CreateNextPeriodAsync` fa lo stesso per la stagione successiva). Le righe eliminate non contano: il filtro globale del
soft delete (`ModuleDbContextConventions`) le esclude da `SeasonRepository.FindAsync`, e `DeletePeriodAsync` elimina le
righe del periodo (`ReplacePeriodAsync` con una lista vuota). Eliminato il periodo con la `key` più alta, il periodo
creato dopo riceve la stessa `key`. Le righe di altre tabelle ancora legate a quella `key` passano così al periodo
nuovo: per esempio `pricing_suggested_prices`, oppure le righe di periodi che non esistono più e che `update-package`
tiene così come arrivano (vedi il documento del Listino).

Esempio: periodi 0 "Bassa", 1 "Media", 2 "Alta". Si elimina "Alta" e si crea "Ferragosto": oggi "Ferragosto" riceve
`key` 2 e si ritrova i prezzi rimasti di "Alta".

Inoltre due creazioni nello stesso momento calcolano la stessa `key`: i due periodi diventano uno solo (la lettura
raggruppa le righe per `key`), oppure, se hanno giorni in comune, il vincolo sulle sovrapposizioni rifiuta il secondo
con un 500.

Cosa cambiare:

- Una `key` già data a un periodo della stagione non va più data a un altro periodo, anche dopo che quello è stato
  eliminato. Nell'esempio "Ferragosto" riceve `key` 3.
- Il prossimo numero libero sta in due colonne nuove di `catalog.pricing_seasons`, `smallint not null` come
  `period_group_key` e `tariff_period_group` in tutte le tabelle che tengono la `key` (es. `next_period_group_key` per
  la stagione corrente e `next_season_next_period_group_key` per la successiva).
- Il contatore si aggiorna in modo atomico: la creazione, dentro la transazione con la riga della stagione bloccata
  (sezione 5), prende il valore del contatore come `key` e lo fa salire di 1 nello stesso aggiornamento (es.
  `UPDATE catalog.pricing_seasons SET next_period_group_key = next_period_group_key + 1 WHERE id = @id RETURNING
  next_period_group_key - 1`). Due creazioni nello stesso momento ricevono così `key` diverse. L'eliminazione di un
  periodo non cambia il contatore.
- Valore di partenza, nella migrazione, per ogni stagione: 1 più la `key` più alta mai usata, anche nelle righe
  eliminate (`deleted_at` non nullo); 0 se non ce n'è nessuna.
  - Stagione corrente: `period_group_key` di tutte le righe della stagione in `catalog.pricing_tariff_periods`, e la
    `key` nelle tabelle dei prezzi dello stabilimento: `tariff_period_group` di
    `catalog.pricing_price_list_period_prices` (listini dello stabilimento),
    `catalog.agreement_price_list_period_prices` (listini delle convenzioni), `catalog.service_sale_item_period_prices`
    e `catalog.pricing_suggested_prices`; `period_group_key` di `catalog.service_hourly_options`.
  - Stagione successiva: `period_group_key` di tutte le righe della stagione in `catalog.pricing_next_season_periods`.
- Le API non cambiano: `POST /api/catalog/[next-season/]tariff-periods` risponde come oggi, con la `key` nuova.

## 4. Nessun giorno in comune tra periodi diversi della stessa stagione

Riguarda le API che salvano gli intervalli di un periodo, per tutte e due le stagioni:

- `POST /api/catalog/tariff-periods` e `POST /api/catalog/next-season/tariff-periods` (nuovo periodo);
- `PUT /api/catalog/tariff-periods/{key}` e `PUT /api/catalog/next-season/tariff-periods/{key}` (modifica del periodo).

Rotte, corpi, risposte e gli altri errori restano quelli di oggi: si aggiunge un controllo.

### Oggi

- `SaveTariffPeriodRequestValidator` vieta i giorni in comune solo tra gli intervalli dello **stesso periodo**.
- Il database fa lo stesso solo per la stagione corrente: il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` di
  `catalog.pricing_tariff_periods` è su `season_id`, `period_group_key` e l'intervallo di date (righe con `deleted_at`
  nullo). La tabella della stagione successiva, `catalog.pricing_next_season_periods`, non ha nessun vincolo di questo
  tipo.
- Due periodi diversi della stessa stagione possono quindi coprire lo stesso giorno, e quel giorno avrebbe due prezzi.
  Il frontend lo impedisce nei suoi calendari (i giorni già presi da altri periodi sono spenti), ma un'altra
  applicazione, o due utenti che salvano nello stesso momento, possono farlo passare.

### Cosa cambiare

- Prima di salvare (creazione e modifica, in `EnsurePeriodAllowed` / `EnsureNextPeriodAllowed`), dentro la
  transazione e dopo il blocco della stagione della sezione 5, il backend controlla che nessun intervallo della
  richiesta abbia giorni in comune con gli intervalli degli **altri** periodi della stessa stagione (righe non
  eliminate; nella modifica senza contare quelli del periodo stesso, che vengono sostituiti). Il primo e l'ultimo
  giorno contano: un intervallo che finisce il 15 giugno e uno che comincia il 15 giugno si sovrappongono.
- Se succede, risponde 409 con un **codice nuovo**, `pricing.period_overlap`, senza `errors[]`, e in `args.periodName`
  il nome del primo altro periodo che ha giorni in comune:

```json
{
  "type": "https://errors.summerbooking/pricing/period_overlap",
  "title": "pricing.period_overlap",
  "status": 409,
  "code": "pricing.period_overlap",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
  "args": { "periodName": "Alta stagione" }
}
```

- Lo stesso vincolo anche nel database, così vale per qualunque scrittura:
  - per `catalog.pricing_tariff_periods`, il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` diventa su
    `season_id` e l'intervallo di date, senza `period_group_key` (sempre sulle righe con `deleted_at` nullo);
  - per `catalog.pricing_next_season_periods`, un vincolo uguale su `season_id` e l'intervallo di date.

  `ReplacePeriodAsync` chiude già i vecchi intervalli del periodo prima di inserire i nuovi, quindi la modifica di un
  periodo non va in conflitto con se stessa. Prima della migrazione va controllato se nei dati esistenti ci sono già
  periodi con giorni in comune: in quel caso il vincolo non si può aggiungere finché non vengono sistemati.
- Anche la stagione successiva deve salvare in due passi. Oggi `SeasonService.ReplaceNextPeriodAsync` toglie i vecchi
  intervalli e aggiunge i nuovi con un solo `SaveChangesAsync`, e funziona solo perché
  `catalog.pricing_next_season_periods` non ha vincoli. Con il vincolo nuovo deve fare come `ReplacePeriodAsync`: in
  una transazione, prima elimina i vecchi intervalli del periodo e salva, poi inserisce i nuovi e salva. Altrimenti,
  se l'inserimento arriva prima dell'eliminazione, la modifica di un periodo va in conflitto con se stessa. Esempio:
  "Alta" della prossima stagione dal 01/07 al 31/07, modificata in 01/07–15/08 → il vincolo rifiuta il salvataggio
  anche se le date sono giuste.
- Il codice nuovo va aggiunto a `CatalogErrors`, accanto agli altri `pricing.period_*`. Il frontend lo mostra sopra i
  bottoni del popup che salva.

## 5. Una scrittura alla volta sui periodi di una stagione

Riguarda tutte le API che scrivono i periodi, per tutte e due le stagioni: `POST`, `PUT` e `DELETE` di
`/api/catalog/[next-season/]tariff-periods[/{key}]` e le API 1 e 2.

### Oggi

- `CreatePeriodAsync`, `UpdatePeriodAsync` e `DeletePeriodAsync` (e le loro versioni per la stagione successiva)
  leggono la stagione con i suoi periodi e fanno i controlli (`RequirePeriod`, `EnsurePeriodAllowed`, la `key` nuova)
  fuori da una transazione; `ReplacePeriodAsync` ne apre una solo per scrivere.
- Due salvataggi nello stesso momento sulla stessa stagione passano quindi tutti e due i controlli su dati vecchi.
  Esempio: due utenti salvano insieme "Alta" della stagione corrente; il secondo inserisce i suoi intervalli quando ci
  sono già quelli del primo, con la stessa `key`, e il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` lo
  rifiuta (SQLSTATE `23P01`). L'eccezione non è gestita e `GlobalExceptionHandler` (`Boot/Web/ProblemDetailsWriter.cs`)
  risponde 500 `server.unexpected`.

### Cosa cambiare

- Ogni API dell'elenco apre una transazione e, prima di leggere i periodi e di fare i controlli, blocca la riga della
  stagione (es. `SELECT id FROM catalog.pricing_seasons WHERE id = @id FOR UPDATE`). Una seconda scrittura sulla
  stessa stagione aspetta che la prima finisca e poi legge i dati già salvati.
- Nella stessa transazione stanno controlli (nome, giorni della stagione, sovrapposizioni della sezione 4, intervalli
  delle API 1 e 2, listini e servizi della sezione 3), contatore della `key` (sezione 3) e scrittura.
- Il vincolo del database resta come protezione: se una scrittura viola un vincolo sulle sovrapposizioni
  (`ex_catalog_pricing_tariff_periods_no_overlap` o quello nuovo di `catalog.pricing_next_season_periods`, sezione 4),
  cioè una `DbUpdateException` con `PostgresException.SqlState` `23P01` (`PostgresErrorCodes.ExclusionViolation`), il
  backend non risponde mai 500: annulla la transazione e risponde 409 `pricing.period_overlap`, come il controllo della
  sezione 4, con `args.periodName` del periodo che occupa quei giorni, riletto dopo l'annullamento.
- Le risposte riuscite non cambiano.

## API esistenti usate così come sono

Queste API del backend le usa già il frontend e, salvo le modifiche delle sezioni 3, 4 e 5, **non vanno cambiate**:
sono qui solo perché chi lavora sulla pagina sappia cosa il frontend si aspetta. Lo stabilimento viene sempre dal token.
Le `GET` chiedono il permesso `CatalogRead`, le altre `CatalogSettings`.

| Metodo | Rotta | Uso nella pagina |
|---|---|---|
| GET | `/api/catalog/season` | Riquadro delle date, all'apertura e al cambio lingua: 200 con `startsOn`, `endsOn`, `nextStartsOn`, `nextEndsOn` (giorni `yyyy-MM-dd`), oppure 204 senza corpo se la stagione non è ancora impostata (quattro date vuote). |
| PUT | `/api/catalog/season` | "Salva" del riquadro delle date: le quattro date; 200 con le date salvate, che la pagina mostra. Il backend non controlla i periodi: prima di chiamarla il frontend legge i periodi delle due stagioni e non salva se un intervallo resterebbe fuori dalle nuove date. |
| GET | `/api/catalog/tariff-periods` | Tab "Stagione Corrente": i periodi con `key`, `name` e `ranges` (`startsOn`, `endsOn`), in ordine di primo giorno; `[]` senza stagione. Li leggono anche le pagine Listino e Servizi. |
| GET | `/api/catalog/next-season/tariff-periods` | Tab "Prossima Stagione": lo stesso per la stagione successiva. |
| POST | `/api/catalog/[next-season/]tariff-periods` | Popup "Imposta periodo": `name` e `ranges`; 201 con il periodo creato. Si aggiungono il controllo della sezione 4, la `key` mai riusata della sezione 3 e il blocco della sezione 5. |
| PUT | `/api/catalog/[next-season/]tariff-periods/{key}` | Popup "Modifica Periodo", "Aggiungi intervallo" e "Modifica Intervallo": il periodo intero (`name` e tutti i `ranges`), senza controllo di versione (vale l'ultimo salvataggio); 200 con il periodo salvato. Si aggiungono il controllo della sezione 4 e il blocco della sezione 5. |
| DELETE | `/api/catalog/tariff-periods/{key}` | Cestino di un periodo della stagione corrente, dopo la conferma; 204 senza corpo. Si aggiungono il controllo dei servizi della sezione 3 e il blocco della sezione 5. |
| DELETE | `/api/catalog/next-season/tariff-periods/{key}` | Lo stesso per la stagione successiva; 204 senza corpo. Si aggiunge il blocco della sezione 5. |

Il frontend si aspetta questi errori e li mostra così:

| Status | `code` | Dove lo mostra |
|---|---|---|
| 400 | `validation.failed` con `errors[]` | Sotto il campo. Date della stagione (`PUT /season`): `validation.end_before_start` su `endsOn` o `nextEndsOn`, `validation.out_of_range` su `nextStartsOn` (la prossima stagione non inizia dopo la fine della corrente, testo "La prossima stagione deve iniziare dopo la fine di quella corrente."), `validation.required` su `nextStartsOn` o `nextEndsOn` quando c'è solo l'altra. Periodi: `name` sotto il nome; `ranges` e `ranges[i]` sotto il calendario di "Imposta periodo" o sopra i bottoni degli altri popup. |
| 400 | `validation.failed` senza `errors[]` | JSON che non si legge (es. una data `null`): sotto le date del riquadro o sopra i bottoni del popup. |
| 409 | `pricing.period_name_taken` | Sotto il nome del periodo. |
| 422 | `pricing.period_outside_season` | Sotto il calendario di "Imposta periodo" o sopra i bottoni degli altri popup. |
| 422 | `pricing.season_required` / `pricing.next_season_required` | Sopra i bottoni del popup; per l'eliminazione di un periodo nell'avviso "Operazione non riuscita". |
| 404 | `pricing.period_not_found` | Nel popup o nell'avviso "Operazione non riuscita"; la tab ricarica i periodi. |
| 409 | `pricing.period_in_use` | Avviso "Operazione non riuscita" con i nomi dei listini (`args.priceLists`). |
| 403 | `auth.forbidden` | Utente senza il permesso: per una lettura sotto le date del riquadro o sotto il titolo "Periodi", per un salvataggio sopra i bottoni, per un'eliminazione nell'avviso "Operazione non riuscita". |
| 403 | `catalog.property_required` | Token senza stabilimento: dove va `auth.forbidden`. |
| 404 | `catalog.property_not_found` | Stabilimento del token non trovato: dove va `auth.forbidden`. |
| 401 | `auth.unauthenticated` | Il frontend rinnova il token e ripete la chiamata una volta; se la sessione è finita l'utente esce. |

## Verifica

- Una sola chiamata cancella intervalli di più periodi; gli altri intervalli e gli altri periodi restano, con lo stesso
  id, la stessa `key` e lo stesso nome.
- Tutti o nessuno: con un intervallo che non esiste, o con un periodo che resterebbe vuoto, non si cancella niente.
- Un periodo con tutti i suoi intervalli nella lista dà 422 `pricing.period_ranges_required`.
- `{ "ranges": [] }` risponde 400 con `ranges` `validation.required`; 301 elementi con `ranges` `validation.invalid`;
  un elemento senza `endsOn` con `ranges[i].endsOn` `validation.required`, mai con un controllo sulla data
  `0001-01-01`.
- `/next-season/.../bulk-delete-ranges` senza nessuna stagione risponde 422 `pricing.season_required`; con la stagione
  ma senza la successiva 422 `pricing.next_season_required`.
- Due cancellazioni nello stesso momento che insieme toglierebbero tutti gli intervalli di un periodo: una risponde
  204, l'altra 422 `pricing.period_ranges_required`; il periodo tiene almeno un intervallo.
- La rotta `/next-season/...` tocca solo i periodi della stagione successiva, quella senza `/next-season` solo quelli
  della stagione corrente.
- La risposta è 204 senza corpo, e `GET /tariff-periods` (o `/next-season/tariff-periods`) subito dopo mostra solo gli
  intervalli rimasti.
- `DELETE /tariff-periods/{key}` di un periodo con prezzi solo in un servizio (o in un'opzione di un servizio orario)
  risponde 409 `pricing.period_in_use_by_services` con il nome del servizio in `args.services` e non elimina niente;
  con prezzi solo in un servizio eliminato o di un altro stabilimento il periodo si elimina; con prezzi anche in un
  listino risponde come oggi, `pricing.period_in_use`.
- Eliminato il periodo con la `key` più alta, il periodo creato subito dopo riceve una `key` mai usata nella stagione
  (es. 3, non 2); lo stesso nella stagione successiva. Due creazioni nello stesso momento ricevono `key` diverse.
  Dopo la migrazione il contatore di ogni stagione supera ogni `key` già usata, anche nelle righe eliminate e nelle
  tabelle dei prezzi.
- `POST` e `PUT` di un periodo con un giorno già preso da un altro periodo della stessa stagione (corrente o
  successiva) rispondono 409 `pricing.period_overlap` con il nome dell'altro periodo e non salvano niente; un periodo
  che si sovrappone solo con se stesso (modifica delle sue date) si salva, nella stagione corrente e nella successiva
  (es. "Alta" dal 01/07–31/07 al 01/07–15/08 risponde 200).
- Due `PUT` nello stesso momento dello stesso periodo rispondono 200 o 409 `pricing.period_overlap`, mai 500.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.
