# Stagioni e periodi - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Stagioni e periodi** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare o cambiare, con
payload, risposta, ordine dei controlli, errori attesi e modifiche al database.

La pagina è `/settings/seasons`: in alto le date delle stagioni, sotto due tab, "Stagione Corrente"
(`/settings/seasons/1`) e "Prossima Stagione" (`/settings/seasons/2`), ognuna con i periodi tariffari della sua stagione
e i loro intervalli di date. In una tab si possono spuntare intervalli di periodi diversi e cancellarli tutti insieme
con "Cancella selezionati"; il cestino di un intervallo ne cancella uno solo.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/catalog/tariff-periods/bulk-delete-ranges` | **Nuova**: cancella intervalli di uno o più periodi della stagione corrente, tutti o nessuno |
| 2 | POST | `/api/catalog/next-season/tariff-periods/bulk-delete-ranges` | **Nuova**: lo stesso per la stagione successiva |
| 3 | DELETE, POST | `/api/catalog/tariff-periods/{key}`, `/api/catalog/[next-season/]tariff-periods` | **Modifica**: prima di eliminare un periodo controlla anche i servizi che lo usano; la `key` di un periodo eliminato non si riusa |
| 4 | POST, PUT | `/api/catalog/[next-season/]tariff-periods[/{key}]` | **Modifica**: nessun giorno in comune tra periodi diversi della stessa stagione |
| 5 | POST, PUT, DELETE | `/api/catalog/[next-season/]tariff-periods[/{key}]` e le API 1 e 2 | **Modifica**: una scrittura alla volta sui periodi di una stagione; una sovrapposizione rifiutata dal database risponde 409, mai 500 |

`[next-season/]` vuol dire: tutte e due le rotte, quella della stagione corrente e quella con `/next-season`.

Le API 1 e 2 sono **nuove**: oggi un intervallo si cancella solo con `PUT /tariff-periods/{key}`, un periodo alla volta.
Il frontend ha bisogno di cancellare con **una sola chiamata** intervalli di periodi diversi, e di non cancellarne
nessuno se uno non si può cancellare. La 3, la 4 e la 5 sono **modifiche** ad API che esistono già.

Tutto sta nel modulo Catalog (`Module.Catalog`): `Endpoints/SeasonEndpoints.cs`,
`Application/Services/SeasonService.cs`, `Application/Dtos/SeasonDtos.cs`, `Application/Validators/SeasonValidators.cs`,
`Domain/CatalogErrors.cs`, `Domain/Entities/PricingSeason.cs`,
`Infrastructure/Configurations/PricingSeasonConfiguration.cs`, `Infrastructure/Repositories/SeasonSectorRepositories.cs`
(`SeasonRepository`), `Application/Abstractions/ICatalogRepositories.cs` (`ISeasonRepository`) e una migration nuova
del modulo.

## Codici d'errore nuovi

Vanno aggiunti a `CatalogErrors`, accanto agli altri `pricing.period_*`. Il frontend li conosce già, con i testi nelle
7 lingue.

| Costante | Codice | Status | `args` | Sezioni |
|---|---|---|---|---|
| `PeriodRangeNotFound` | `pricing.period_range_not_found` | 404 | nessuno | 1, 2 |
| `PeriodRangesRequired` | `pricing.period_ranges_required` | 422 | nessuno | 1, 2 |
| `PeriodInUseByServices` | `pricing.period_in_use_by_services` | 409 | `services`: array di stringhe, mai vuoto | 3 |
| `PeriodOverlap` | `pricing.period_overlap` | 409 | `periodName`: stringa | 4, 5 |

## Regole comuni

Valgono per le API 1 e 2.

- **Rotte**: `MapPost("/bulk-delete-ranges", …)` nei gruppi `/tariff-periods` e `/next-season/tariff-periods` di
  `SeasonEndpoints.Map`, con la logica in `SeasonService`.
- **Le API esistenti non cambiano**: `/api/catalog/season`, `/api/catalog/tariff-periods` e
  `/api/catalog/next-season/tariff-periods`, con le loro sotto-rotte (`GET`, `POST`, `PUT` e `DELETE /{key}`), restano
  come sono; le due nuove si aggiungono. Le uniche modifiche sono quelle delle sezioni 3, 4 e 5.
- **Stabilimento dal token**: come per le altre API dei periodi (`CurrentProperty`); nessuna richiesta lo contiene.
  Token senza stabilimento: 403 `catalog.property_required`; stabilimento del token non trovato: 404
  `catalog.property_not_found`.
- **Permessi**: `RequireAuthorization` con la policy di `CatalogSettings`, come `PUT /tariff-periods/{key}`. Senza
  permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Le date sono giorni `yyyy-MM-dd`, come in `DateRangeDto`.
- **Tutti o nessuno**: diversamente dalle altre `bulk-delete` del Catalog, che rispondono 200 con `succeeded` e
  `failed` e cancellano quello che possono, queste due fanno tutti i controlli prima di cancellare e salvano in una sola
  transazione. Se un controllo fallisce non si cancella niente.
- **Errori** nel formato Problem Details del backend (`ProblemDetailsWriter`), con `code`.
- **Prima del codice dell'endpoint**, come per tutte le API: senza token valido 401 `auth.unauthenticated`; senza
  permesso 403 `auth.forbidden`; poi la lettura del corpo. Un corpo che non si legge (assente, non JSON, la data
  `2026-02-30`, una `key` che non è un numero intero o non sta in `short`) non arriva all'endpoint e risponde 400 senza
  `errors[]`, come per le altre API, senza codice da scrivere (in Development il binding lancia
  `BadHttpRequestException` e `GlobalExceptionHandler` risponde `validation.failed`).

## 1. POST /api/catalog/tariff-periods/bulk-delete-ranges

Tab "Stagione Corrente": bottone "Cancella selezionati" (con almeno due intervalli spuntati) e cestino di un intervallo
(un solo elemento), dopo la conferma nel popup. Il frontend non manda la richiesta se un periodo resterebbe senza
intervalli (mostra un avviso): il 422 `pricing.period_ranges_required` arriva solo se nel frattempo qualcun altro ha
cancellato degli intervalli.

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
| `ranges` | array di oggetti | Obbligatorio, da 1 a 300 elementi (`BulkIdsRequest.MaxIds` di `Shared/Dtos/BulkResult.cs`, come le altre `bulk-delete` del Catalog). Nessun elemento `null`. |
| `ranges[].key` | number (intero, `short`) | Obbligatorio. La `key` del periodo, come in `GET /tariff-periods`. |
| `ranges[].startsOn` | string (`yyyy-MM-dd`) | Obbligatorio. Primo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |
| `ranges[].endsOn` | string (`yyyy-MM-dd`) | Obbligatorio. Ultimo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |

- DTO in `Application/Dtos/SeasonDtos.cs`, con tutti i campi nullable:
  `DeleteTariffPeriodRangesRequest(IReadOnlyList<TariffPeriodRangeRequest?>? Ranges)` e
  `TariffPeriodRangeRequest(short? Key, DateOnly? StartsOn, DateOnly? EndsOn)`. Così un campo assente o `null` arriva
  al validatore come `null` e diventa `validation.required`; con `short` e `DateOnly` diventerebbe `0` o `0001-01-01`
  e la richiesta passerebbe ai controlli successivi.
- Validatore `DeleteTariffPeriodRangesRequestValidator` in `Application/Validators/SeasonValidators.cs` (il modulo
  registra da solo i validatori), sul modello di `BulkIdsRequestValidator`
  (`Application/Validators/InventoryValidators.cs`):
  - `ranges`: `NotEmpty()` (assente, `null` o `[]` → `validation.required`), poi al massimo 300 elementi (`Must` con
    `WithErrorCode(ValidationErrorCodes.Invalid)` → `validation.invalid`);
  - ogni elemento, solo quando `ranges` ha da 1 a 300 elementi: `RuleForEach(x => x.Ranges)` con
    `.Cascade(CascadeMode.Continue)`, `NotNull()` (→ `ranges[i]`) e `ChildRules` con `NotNull()` su `key`, `startsOn`
    ed `endsOn` (→ `ranges[i].key`, `ranges[i].startsOn`, `ranges[i].endsOn`), tutti con `validation.required` e `i`
    da 0. `Continue` fa controllare ogni elemento anche dopo un elemento sbagliato, qualunque sia la cascata globale
    (`CascadeMode.Stop` in `Boot/Program.cs`); `ChildRules` non gira su un elemento `null`.

  Ogni campo ha al massimo un errore; `EnsureValid` li riporta tutti insieme in `errors[]`. Con più di 300 elementi c'è
  solo l'errore su `ranges`.
- Un intervallo si riconosce da `key`, `startsOn` ed `endsOn` insieme: dentro un periodo gli intervalli non si
  sovrappongono, quindi non ce ne sono due con le stesse date. Lo stesso intervallo ripetuto nella lista conta una
  volta; il limite di 300 vale sulla lista come arriva.

### Cosa fa

Tocca solo le righe elencate, senza riscrivere i periodi: niente `ReplacePeriodAsync` e niente controlli di nome, di
giorni della stagione o di sovrapposizioni, che servono solo a chi scrive date nuove. Dopo i controlli di "Regole
comuni", in quest'ordine; ogni passo vale per tutta la lista prima del successivo e il primo che non passa decide la
risposta:

1. Validatore. Se non va, 400 `validation.failed` con i suoi errori in `errors[]`, e nient'altro.
2. Stabilimento dal token (`CurrentProperty.IdAsync`): 403 `catalog.property_required` / 404
   `catalog.property_not_found`.
3. Apre una transazione e blocca la riga della stagione (sezione 5), **prima** di leggere la stagione e i periodi.
4. Legge la stagione con i suoi periodi (`RequireSeasonAsync`): senza stagione 422 `pricing.season_required`.
5. Toglie dalla lista i doppioni.
6. Ogni `key` della lista deve essere un periodo della stagione (almeno una riga non eliminata di
   `catalog.pricing_tariff_periods` con quel `period_group_key`): altrimenti 404 `pricing.period_not_found`.
7. Ogni elemento deve corrispondere a una riga non eliminata di quel periodo con le stesse `starts_on` ed `ends_on`:
   altrimenti 404 `pricing.period_range_not_found`.
8. Ogni periodo della lista deve tenere almeno una riga non eliminata che non è nella lista: altrimenti 422
   `pricing.period_ranges_required`.
9. Elimina solo le righe elencate, con il soft delete di sempre (`Remove`, che `SoftDeleteInterceptor` trasforma in
   `deleted_at`). Le altre righe non cambiano: stesso id, stesso nome, stessa `key`. I periodi che non compaiono nella
   lista restano come sono.
10. Salva (`SaveChangesAsync`) e chiude la transazione (commit). Con un errore in un passo qualsiasi la transazione si
    annulla e non cambia niente.
11. Dopo il commit, come le altre API dei periodi, scrive nel registro una voce `catalog.tariff_period_updated` per ogni
    periodo toccato (`RecordPeriodAsync` con la `key`, il nome del periodo e `season` = `current`).

### Risposta

204 senza corpo. Dopo il 204 la tab ricarica i periodi con `GET /tariff-periods`; dopo un errore li ricarica solo se lo
status è 404. Il frontend mostra ogni errore di questa API nell'avviso "Operazione non riuscita", con il testo del suo
`code` (per un 400 il testo generico di `validation.failed`, senza i singoli campi).

### Errori

Nell'ordine in cui si controllano:

| Status | `code` | Quando |
|---|---|---|
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 400 | `validation.failed` | Corpo che non si legge (vedi "Regole comuni"): senza `errors[]`. |
| 400 | `validation.failed` | `ranges` assente, `null` o vuota: `errors[]` con `field` `ranges` e `code` `validation.required`. |
| 400 | `validation.failed` | Più di 300 elementi: `errors[]` con solo `field` `ranges` e `code` `validation.invalid`. |
| 400 | `validation.failed` | Un elemento `null`: `field` `ranges[i]` e `code` `validation.required`. |
| 400 | `validation.failed` | Un elemento senza `key`, `startsOn` o `endsOn` (assente o `null`): un errore per campo mancante, con `field` `ranges[i].key`, `ranges[i].startsOn` o `ranges[i].endsOn` e `code` `validation.required`. |
| 403 | `catalog.property_required` | Il token non ha lo stabilimento. |
| 404 | `catalog.property_not_found` | Lo stabilimento del token non esiste. |
| 422 | `pricing.season_required` | Lo stabilimento non ha la stagione. |
| 404 | `pricing.period_not_found` | Una `key` non è un periodo della stagione. |
| 404 | `pricing.period_range_not_found` (nuovo) | Un intervallo non c'è nel suo periodo: `startsOn` ed `endsOn` non sono le date di un intervallo di quella `key`. |
| 422 | `pricing.period_ranges_required` (nuovo) | Un periodo resterebbe senza intervalli. Il periodo intero si cancella con `DELETE /tariff-periods/{key}`. |

Esempio di risposta a una richiesta il cui secondo elemento non ha `endsOn`:

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

## 2. POST /api/catalog/next-season/tariff-periods/bulk-delete-ranges

Tab "Prossima Stagione": come la sezione 1, sulle righe della stagione successiva
(`catalog.pricing_next_season_periods`). Cambia solo questo:

- le `key` sono quelle di `GET /next-season/tariff-periods`;
- al passo 4, dopo il blocco, la stagione si controlla in due passi, come `RequireNextSeasonAsync`: se lo stabilimento
  non ha nessuna stagione, 422 `pricing.season_required`; se ha la stagione ma non la successiva
  (`next_season_starts_on` nullo), 422 `pricing.next_season_required`;
- le righe si eliminano togliendole da `season.NextSeasonPeriods`, come fa oggi `ReplaceNextPeriodAsync`
  (`SoftDeleteInterceptor` le trasforma in `deleted_at`);
- le voci del registro hanno `season` = `next`, come le altre API della stagione successiva;
- dopo il 204, e dopo un 404, la tab ricarica i periodi con `GET /next-season/tariff-periods`.

Richiesta, risposta, ordine delle operazioni, blocco della stagione e gli altri errori sono gli stessi della sezione 1.

## 3. DELETE /api/catalog/tariff-periods/{key}: controllare anche i servizi

API che esiste già (cestino di un periodo nella tab "Stagione Corrente"): rotta, risposta ed errori di oggi restano
uguali; si aggiunge un controllo prima di eliminare. In più la `key` di un periodo eliminato non va data a un periodo
nuovo (vedi "La `key` di un periodo eliminato non si riusa").

### Oggi

`SeasonService.DeletePeriodAsync` chiede a `SeasonRepository.ListPeriodUsagesAsync` dove il periodo è usato e, se lo è,
risponde 409 `pricing.period_in_use` con i nomi in `args.priceLists`. Il controllo guarda solo i prezzi dei listini:

- `catalog.pricing_price_list_period_prices` (listini dello stabilimento);
- `catalog.agreement_price_list_period_prices` (listini delle convenzioni).

Altre tabelle del database si legano a un periodo con la sua `key` (`tariff_period_group` / `period_group_key`), ma non
vengono guardate:

| Tabella | Cosa contiene |
|---|---|
| `catalog.service_sale_item_period_prices` | I prezzi per periodo dei servizi (bar, servizi generici). |
| `catalog.service_hourly_options` | Le opzioni dei servizi orari legate a un periodo (`period_group_key` non nullo). |
| `catalog.pricing_suggested_prices` | I prezzi suggeriti, per giorno e fila di un pacchetto, con il periodo del giorno. |

Un periodo usato solo da queste righe si elimina, e le righe restano legate a una `key` che non indica più nessun
periodo della stagione.

### Cosa cambiare

Ordine dei controlli di `DeletePeriodAsync`; dal blocco in poi tutto sta nella transazione della sezione 5:

1. stabilimento (403 / 404 `catalog.property_*`), blocco della stagione (sezione 5), poi stagione (422
   `pricing.season_required`) e periodo (`RequirePeriod`: 404 `pricing.period_not_found`), come oggi;
2. listini (`ListPeriodUsagesAsync`), come oggi: se il periodo ha prezzi nei listini, 409 `pricing.period_in_use` con
   `args.priceLists`, e il controllo dei servizi non si fa;
3. **nuovo**, servizi: il backend cerca le righe che usano la `key` del periodo:
   - `catalog.service_sale_item_period_prices` con `tariff_period_group` uguale alla `key`;
   - `catalog.service_hourly_options` con `period_group_key` uguale alla `key`.

   Contano solo le righe non eliminate (`deleted_at` nullo) di servizi non eliminati dello stabilimento del token con
   i prezzi per periodo attivi: legate con `sale_item_id` a `catalog.service_sale_items`, con `deleted_at` nullo,
   `property_id` dello stabilimento e `has_period_prices` = `true`. Le righe di servizi eliminati, di altri
   stabilimenti o con i prezzi per periodo spenti (`has_period_prices` = `false`) non bloccano l'eliminazione e restano
   come sono: l'utente non le vede nella pagina Servizi e non potrebbe toglierle. Se c'è almeno una riga, risponde
   409 `pricing.period_in_use_by_services` con `args.services` = i nomi dei servizi di quelle righe
   (`service_sale_items.name`), ognuno una volta;
4. elimina il periodo (`ReplacePeriodAsync` con una lista vuota), chiude la transazione e scrive nel registro, come
   oggi.

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

- Il frontend mostra questo 409 nell'avviso "Operazione non riuscita", come gli altri errori dell'eliminazione, con i
  nomi di `args.services` uniti da una virgola (al massimo 20, poi "…"), come fa con `args.priceLists`.
- Le righe di `catalog.pricing_suggested_prices` con quella `key` non bloccano l'eliminazione: il backend non le guarda
  e non le modifica, come oggi.
- La prossima stagione (`DELETE /api/catalog/next-season/tariff-periods/{key}`) oggi non fa nessun controllo d'uso: i
  prezzi di listini e servizi si legano solo ai periodi della stagione corrente, quindi lì non cambia niente (si
  aggiunge solo il blocco della sezione 5).

### La `key` di un periodo eliminato non si riusa

Oggi `SeasonService.CreatePeriodAsync` dà al periodo nuovo la `key` più alta tra i periodi che esistono, più 1
(`CreateNextPeriodAsync` fa lo stesso per la stagione successiva). Le righe eliminate non contano: il filtro globale del
soft delete (`ModuleDbContextConventions.ApplySoftDeleteQueryFilter`) le esclude da `SeasonRepository.FindAsync`, e
l'eliminazione di un periodo elimina le sue righe (`ReplacePeriodAsync` con una lista vuota; `ReplaceNextPeriodAsync`
senza richiesta per la stagione successiva). Eliminato il periodo con la `key` più alta, il periodo creato dopo riceve
la stessa `key`. Le righe di altre tabelle ancora legate a quella `key` passano così al periodo nuovo: per esempio
quelle di `pricing_suggested_prices`, oppure le righe con una `key` che non è un periodo della stagione corrente, che la
sezione 7 di "Listino - Frontend requests" (`update-package`) chiede di tenere così come arrivano.

Esempio: periodi 0 "Bassa", 1 "Media", 2 "Alta". Si elimina "Alta" e si crea "Ferragosto": oggi "Ferragosto" riceve
`key` 2 e si ritrova i prezzi rimasti di "Alta".

Inoltre due creazioni nello stesso momento calcolano la stessa `key` e i due periodi diventano uno solo (la lettura
raggruppa le righe per `key`); nella stagione corrente, se hanno giorni in comune, il vincolo
`ex_catalog_pricing_tariff_periods_no_overlap` rifiuta il secondo e la risposta è 500.

Cosa cambiare:

- Una `key` già data a un periodo della stagione non va più data a un altro periodo della stessa stagione, anche dopo
  che quello è stato eliminato. Nell'esempio "Ferragosto" riceve `key` 3.
- Due colonne nuove in `catalog.pricing_seasons`, mappate in `PricingSeason` (proprietà `short`) e
  `PricingSeasonConfiguration`, con un commento come le altre colonne:

  | Colonna | Tipo | Contiene |
  |---|---|---|
  | `next_period_group_key` | `smallint NOT NULL DEFAULT 0` | La `key` del prossimo periodo creato nella stagione corrente. |
  | `next_season_next_period_group_key` | `smallint NOT NULL DEFAULT 0` | La `key` del prossimo periodo creato nella stagione successiva. |

  `smallint` come le colonne che tengono la `key` (`period_group_key`, `tariff_period_group`). Una stagione creata dopo
  la migration (`PUT /season` senza stagione) parte da 0.
- `CreatePeriodAsync` e `CreateNextPeriodAsync`, dentro la transazione con la stagione bloccata (sezione 5), dopo tutti
  i controlli e subito prima di scrivere, prendono la `key` dal contatore e lo fanno salire di 1 con una sola
  istruzione:

  ```sql
  UPDATE catalog.pricing_seasons
     SET next_period_group_key = next_period_group_key + 1
   WHERE id = @seasonId
  RETURNING next_period_group_key - 1;
  ```

  Per la stagione successiva la stessa istruzione su `next_season_next_period_group_key`. Dopo l'inserimento della
  stagione le due colonne cambiano solo con questa istruzione, mai attraverso l'entità tracciata. Se la creazione non
  va a buon fine, l'annullamento della transazione riporta indietro anche il contatore. Eliminazioni di periodi, le API
  1 e 2 e `PUT /season` non toccano il contatore.
- Valore di partenza, nella stessa migration che crea le colonne: per ogni stagione, 1 più la `key` più alta mai usata,
  contando tutte le righe, eliminate o no; 0 se non ce n'è nessuna. Per la stagione corrente contano i periodi della
  stagione e le tabelle dei prezzi dello stabilimento; per la successiva solo i suoi periodi (i prezzi non li usano):

  ```sql
  UPDATE catalog.pricing_seasons s
     SET next_period_group_key = 1 + GREATEST(
           COALESCE((SELECT MAX(tp.period_group_key) FROM catalog.pricing_tariff_periods tp
                      WHERE tp.season_id = s.id), -1),
           COALESCE((SELECT MAX(pp.tariff_period_group) FROM catalog.pricing_price_list_period_prices pp
                       JOIN catalog.pricing_price_lists pl ON pl.id = pp.price_list_id
                      WHERE pl.property_id = s.property_id), -1),
           COALESCE((SELECT MAX(ap.tariff_period_group) FROM catalog.agreement_price_list_period_prices ap
                       JOIN catalog.agreement_price_lists al ON al.id = ap.price_list_id
                       JOIN catalog.agreement_partners pa ON pa.id = al.partner_id
                      WHERE pa.property_id = s.property_id), -1),
           COALESCE((SELECT MAX(sp.tariff_period_group) FROM catalog.service_sale_item_period_prices sp
                       JOIN catalog.service_sale_items si ON si.id = sp.sale_item_id
                      WHERE si.property_id = s.property_id), -1),
           COALESCE((SELECT MAX(ho.period_group_key) FROM catalog.service_hourly_options ho
                       JOIN catalog.service_sale_items si ON si.id = ho.sale_item_id
                      WHERE si.property_id = s.property_id), -1),
           COALESCE((SELECT MAX(sg.tariff_period_group) FROM catalog.pricing_suggested_prices sg
                      WHERE sg.property_id = s.property_id), -1)),
         next_season_next_period_group_key = 1 + COALESCE(
           (SELECT MAX(np.period_group_key) FROM catalog.pricing_next_season_periods np
             WHERE np.season_id = s.id), -1);
  ```

  Il `Down` della migration toglie le due colonne.
- Le API non cambiano: `POST /api/catalog/[next-season/]tariff-periods` risponde come oggi (201 con il periodo), con
  la `key` nuova.

## 4. Nessun giorno in comune tra periodi diversi della stessa stagione

Riguarda le API che salvano gli intervalli di un periodo, per tutte e due le stagioni:

- `POST /api/catalog/tariff-periods` e `POST /api/catalog/next-season/tariff-periods` (nuovo periodo);
- `PUT /api/catalog/tariff-periods/{key}` e `PUT /api/catalog/next-season/tariff-periods/{key}` (modifica del periodo).

Rotte, corpi, risposte e gli altri errori restano quelli di oggi: si aggiunge un controllo.

### Oggi

- `SaveTariffPeriodRequestValidator` vieta i giorni in comune solo tra gli intervalli dello **stesso periodo** (400
  `validation.invalid` su `ranges`).
- Il database fa lo stesso solo per la stagione corrente: il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` di
  `catalog.pricing_tariff_periods` è su `season_id`, `period_group_key` e l'intervallo di date (righe con `deleted_at`
  nullo). La tabella della stagione successiva, `catalog.pricing_next_season_periods`, non ha nessun vincolo di questo
  tipo.
- Due periodi diversi della stessa stagione possono quindi coprire lo stesso giorno; per quel giorno preventivi e
  ricerca usano il periodo con l'intervallo più breve (`TariffCalendar.Resolve`). Il frontend lo impedisce nei suoi
  calendari (i giorni già presi da altri periodi sono spenti), ma un'altra applicazione, o due utenti che salvano nello
  stesso momento, possono farlo passare.

### Cosa cambiare

- Ordine dei controlli di creazione e modifica, dopo i controlli di "Regole comuni": validatore (400), stabilimento
  (403 / 404 `catalog.property_*`), blocco della stagione (sezione 5), stagione (422 `pricing.season_required`; per la
  stagione successiva anche 422 `pricing.next_season_required`), solo nella modifica il periodo (404
  `pricing.period_not_found`), nome (409 `pricing.period_name_taken`), giorni della stagione (422
  `pricing.period_outside_season`), poi il controllo **nuovo**, in `EnsurePeriodAllowed` / `EnsureNextPeriodAllowed`.
- Il controllo nuovo: nessun intervallo della richiesta deve avere giorni in comune con gli intervalli degli **altri**
  periodi della stessa stagione (righe non eliminate; nella modifica senza contare quelli del periodo stesso, che
  vengono sostituiti). Il primo e l'ultimo giorno contano: un intervallo che finisce il 15 giugno e uno che comincia il
  15 giugno si sovrappongono.
- Se succede, risponde 409 `pricing.period_overlap`, senza `errors[]`, con `args.periodName` = il nome del primo altro
  periodo, in ordine di primo giorno come nella risposta di `GET /tariff-periods`, che ha giorni in comune con la
  richiesta. Non salva niente.

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

- Lo stesso vincolo anche nel database, così vale per qualunque scrittura. Nella migration nuova, prima un controllo
  che si ferma se nei dati ci sono già giorni in comune (come fa la migration `ServicesAndNextSeason` con i suoi dati),
  poi i vincoli (`btree_gist` c'è già):

  ```sql
  DO $$
  BEGIN
      IF EXISTS (SELECT 1 FROM catalog.pricing_tariff_periods a
                   JOIN catalog.pricing_tariff_periods b
                     ON b.season_id = a.season_id AND b.id > a.id AND b.deleted_at IS NULL
                    AND daterange(a.starts_on, a.ends_on, '[]') && daterange(b.starts_on, b.ends_on, '[]')
                  WHERE a.deleted_at IS NULL)
         OR EXISTS (SELECT 1 FROM catalog.pricing_next_season_periods a
                      JOIN catalog.pricing_next_season_periods b
                        ON b.season_id = a.season_id AND b.id > a.id AND b.deleted_at IS NULL
                       AND daterange(a.starts_on, a.ends_on, '[]') && daterange(b.starts_on, b.ends_on, '[]')
                     WHERE a.deleted_at IS NULL) THEN
          RAISE EXCEPTION 'periodi della stessa stagione con giorni in comune: vanno sistemati a mano prima di questa migration';
      END IF;
  END $$;

  ALTER TABLE catalog.pricing_tariff_periods DROP CONSTRAINT ex_catalog_pricing_tariff_periods_no_overlap;
  ALTER TABLE catalog.pricing_tariff_periods
    ADD CONSTRAINT ex_catalog_pricing_tariff_periods_no_overlap
    EXCLUDE USING gist (season_id WITH =, daterange(starts_on, ends_on, '[]') WITH &&)
    WHERE (deleted_at IS NULL);
  ALTER TABLE catalog.pricing_next_season_periods
    ADD CONSTRAINT ex_catalog_pricing_next_season_periods_no_overlap
    EXCLUDE USING gist (season_id WITH =, daterange(starts_on, ends_on, '[]') WITH &&)
    WHERE (deleted_at IS NULL);
  ```

  Il `Down` toglie `ex_catalog_pricing_next_season_periods_no_overlap` e rimette
  `ex_catalog_pricing_tariff_periods_no_overlap` com'era, con `COALESCE(period_group_key, -1) WITH =` tra `season_id`
  e l'intervallo (`Migrations/20261006061418_InitialCreate.cs`).
- `ReplacePeriodAsync` chiude già i vecchi intervalli del periodo e salva prima di inserire i nuovi, quindi la modifica
  di un periodo della stagione corrente non va in conflitto con se stessa.
- Anche la stagione successiva deve salvare in due passi. Oggi `SeasonService.ReplaceNextPeriodAsync` toglie i vecchi
  intervalli e aggiunge i nuovi con un solo `SaveChangesAsync`, e funziona solo perché
  `catalog.pricing_next_season_periods` non ha vincoli. Con il vincolo nuovo il salvataggio passa in
  `SeasonRepository`, come `ReplacePeriodAsync`: nella transazione della richiesta (sezione 5) prima toglie da
  `season.NextSeasonPeriods` i vecchi intervalli del periodo e salva, poi aggiunge i nuovi e salva. Altrimenti, se
  l'inserimento arriva prima dell'eliminazione, la modifica di un periodo va in conflitto con se stessa. Esempio: "Alta"
  della prossima stagione dal 01/07 al 31/07, modificata in 01/07–15/08: il vincolo rifiuterebbe il salvataggio anche
  se le date sono giuste.
- Il frontend mostra il 409 sopra i bottoni del popup che salva ("Imposta periodo", "Modifica Periodo", "Aggiungi
  intervallo", "Modifica Intervallo"), con il nome di `args.periodName`.

## 5. Una scrittura alla volta sui periodi di una stagione

Riguarda queste API, per tutte e due le stagioni: `POST`, `PUT` e `DELETE` di
`/api/catalog/[next-season/]tariff-periods[/{key}]` e le API 1 e 2.

### Oggi

- `CreatePeriodAsync`, `UpdatePeriodAsync` e `DeletePeriodAsync` (e le loro versioni per la stagione successiva)
  leggono la stagione con i suoi periodi e fanno i controlli (`RequirePeriod`, `EnsurePeriodAllowed`, la `key` nuova)
  fuori da una transazione; `ReplacePeriodAsync` ne apre una solo per scrivere, `ReplaceNextPeriodAsync` salva con un
  solo `SaveChangesAsync`.
- Due salvataggi nello stesso momento sulla stessa stagione passano quindi tutti e due i controlli su dati vecchi.
  Esempio: due utenti salvano insieme "Alta" della stagione corrente. Il secondo toglie solo gli intervalli che aveva
  letto e inserisce i suoi accanto a quelli appena salvati dal primo, con la stessa `key`: se le date si sovrappongono
  il vincolo `ex_catalog_pricing_tariff_periods_no_overlap` lo rifiuta (SQLSTATE `23P01`), l'eccezione non è gestita e
  `GlobalExceptionHandler` (`Boot/Web/ProblemDetailsWriter.cs`) risponde 500 `server.unexpected`; se non si
  sovrappongono, il periodo resta con gli intervalli di tutti e due.

### Cosa cambiare

- Ogni API dell'elenco, dopo il validatore (quelle che lo hanno) e lo stabilimento, apre una transazione (isolamento
  di default, `READ COMMITTED`) e blocca la riga della stagione dello stabilimento:

  ```sql
  SELECT id FROM catalog.pricing_seasons WHERE property_id = @propertyId FOR UPDATE;
  ```

  Solo dopo legge la stagione con i periodi (`FindAsync`, attraverso `RequireSeasonAsync` / `RequireNextSeasonAsync`):
  una lettura fatta prima del blocco resterebbe nel change tracker con i dati vecchi. Una seconda scrittura sulla
  stessa stagione aspetta che la prima finisca e poi legge i dati già salvati. Senza la riga della stagione il blocco
  non trova niente e `RequireSeasonAsync` risponde 422 `pricing.season_required`, come oggi.
- Nella stessa transazione stanno i controlli (nome, giorni della stagione, sovrapposizioni della sezione 4, intervalli
  delle API 1 e 2, listini e servizi della sezione 3), il contatore della `key` (sezione 3) e la scrittura; poi il
  commit. La voce del registro si scrive dopo il commit, come oggi.
- `ReplacePeriodAsync` non apre più una sua transazione (oggi `BeginTransactionAsync`): usa quella della richiesta, che
  EF non permette di aprire due volte sulla stessa connessione. I suoi due `SaveChangesAsync` restano; lo stesso vale
  per il salvataggio a due passi della stagione successiva (sezione 4).
- Il vincolo del database resta come protezione per le scritture che non passano da queste API. Se il salvataggio degli
  intervalli nuovi lancia una `DbUpdateException` con dentro una `PostgresException` con `SqlState` `23P01`
  (`PostgresErrorCodes.ExclusionViolation`) e `ConstraintName` `ex_catalog_pricing_tariff_periods_no_overlap` o
  `ex_catalog_pricing_next_season_periods_no_overlap`, il backend non risponde mai 500: annulla la transazione, svuota
  il change tracker (`ChangeTracker.Clear()`), rilegge i periodi non eliminati della stagione e risponde 409
  `pricing.period_overlap`, come il controllo della sezione 4, con `args.periodName` = il nome del primo periodo, in
  ordine di primo giorno, diverso da quello che si salvava, che ha giorni in comune con gli intervalli della richiesta
  (`""` se nel frattempo non ce n'è più nessuno). Nessuna voce nel registro.
- Le risposte riuscite non cambiano.
- Fuori da questa richiesta, a giudizio dello sviluppatore backend: se lo stesso blocco debba valere anche per
  `PUT /api/catalog/season` (che senza date della stagione successiva ne cancella i periodi, `PricingSeason.cs`) e per
  i salvataggi di listini e servizi che aggiungono prezzi a un periodo mentre lo si elimina. L'agente che implementa
  questa sezione glielo chiede prima di scrivere codice; il frontend non ne dipende.

## API esistenti usate così come sono

Queste API del backend le usa già il frontend e, salvo le modifiche delle sezioni 3, 4 e 5, **non vanno cambiate**:
sono qui solo perché chi lavora sulla pagina sappia cosa il frontend si aspetta. Lo stabilimento viene sempre dal token.
Le `GET` chiedono il permesso `CatalogRead`, le altre `CatalogSettings`.

| Metodo | Rotta | Uso nella pagina |
|---|---|---|
| GET | `/api/catalog/season` | Riquadro delle date, all'apertura e al cambio lingua: 200 con `startsOn`, `endsOn`, `nextStartsOn`, `nextEndsOn` (giorni `yyyy-MM-dd`), oppure 204 senza corpo se la stagione non è ancora impostata. Con il 204, o con un primo giorno `null`, la pagina mostra l'1 gennaio dell'anno in corso (stagione corrente) e dell'anno dopo (prossima) e li manda con il primo Salva; i giorni finali restano vuoti. |
| PUT | `/api/catalog/season` | "Salva" del riquadro delle date: le quattro date; 200 con le date salvate, che la pagina mostra. Il backend non controlla i periodi: prima di chiamarla il frontend legge i periodi delle due stagioni e non salva se un intervallo resterebbe fuori dalle nuove date. |
| GET | `/api/catalog/tariff-periods` | Tab "Stagione Corrente": i periodi con `key`, `name` e `ranges` (`startsOn`, `endsOn`), in ordine di primo giorno; `[]` senza stagione. Li leggono anche le pagine Listino e Servizi. |
| GET | `/api/catalog/next-season/tariff-periods` | Tab "Prossima Stagione": lo stesso per la stagione successiva. |
| POST | `/api/catalog/[next-season/]tariff-periods` | Popup "Imposta periodo": `name` e `ranges`; 201 con il periodo creato. Si aggiungono il controllo della sezione 4, la `key` mai riusata della sezione 3 e il blocco della sezione 5. |
| PUT | `/api/catalog/[next-season/]tariff-periods/{key}` | Popup "Modifica Periodo", "Aggiungi intervallo" e "Modifica Intervallo": il periodo intero (`name` e tutti i `ranges`), senza controllo di versione (vale l'ultimo salvataggio); 200 con il periodo salvato. Si aggiungono il controllo della sezione 4 e il blocco della sezione 5. |
| DELETE | `/api/catalog/tariff-periods/{key}` | Cestino di un periodo della stagione corrente, dopo la conferma; 204 senza corpo. Si aggiungono il controllo dei servizi della sezione 3 e il blocco della sezione 5. |
| DELETE | `/api/catalog/next-season/tariff-periods/{key}` | Lo stesso per la stagione successiva; 204 senza corpo. Si aggiunge il blocco della sezione 5. |

## Dove il frontend mostra gli errori

Per tutte le API della pagina, comprese le nuove:

| Status | `code` | Dove lo mostra |
|---|---|---|
| 400 | `validation.failed` con `errors[]` | Date della stagione (`PUT /season`): sotto il campo; `validation.end_before_start` su `endsOn` o `nextEndsOn`, `validation.out_of_range` su `nextStartsOn` (la prossima stagione non inizia dopo la fine della corrente, testo "La prossima stagione deve iniziare dopo la fine di quella corrente."), `validation.required` su `nextStartsOn` o `nextEndsOn` quando c'è solo l'altra. Periodi: `name` sotto il nome; `ranges` e `ranges[i]` sotto il calendario di "Imposta periodo" o sopra i bottoni degli altri popup. API 1 e 2: nell'avviso "Operazione non riuscita", con il testo generico. |
| 400 | `validation.failed` senza `errors[]` | Corpo che non si legge (es. `startsOn` o `endsOn` `null` in `PUT /season`): sotto le date del riquadro o sopra i bottoni del popup; per le API 1 e 2 nell'avviso "Operazione non riuscita". |
| 409 | `pricing.period_name_taken` | Sotto il nome del periodo ("Imposta periodo", "Modifica Periodo"); sopra i bottoni in "Aggiungi intervallo" e "Modifica Intervallo", che non hanno il nome. |
| 422 | `pricing.period_outside_season` | Sotto il calendario di "Imposta periodo" o sopra i bottoni degli altri popup. |
| 409 | `pricing.period_overlap` | Sopra i bottoni del popup che salva, con `args.periodName`. |
| 422 | `pricing.season_required` / `pricing.next_season_required` | Sopra i bottoni del popup; per le eliminazioni (periodo, API 1 e 2) nell'avviso "Operazione non riuscita". |
| 404 | `pricing.period_not_found` | Nel popup o nell'avviso "Operazione non riuscita"; la tab ricarica i periodi. |
| 404 | `pricing.period_range_not_found` | Avviso "Operazione non riuscita" (API 1 e 2); la tab ricarica i periodi. |
| 422 | `pricing.period_ranges_required` | Avviso "Operazione non riuscita" (API 1 e 2). |
| 409 | `pricing.period_in_use` | Avviso "Operazione non riuscita" con i nomi dei listini (`args.priceLists`). |
| 409 | `pricing.period_in_use_by_services` | Avviso "Operazione non riuscita" con i nomi dei servizi (`args.services`). |
| 403 | `auth.forbidden` | Utente senza il permesso: per una lettura sotto le date del riquadro o sotto il titolo "Periodi", per un salvataggio sopra i bottoni, per un'eliminazione nell'avviso "Operazione non riuscita". |
| 403 | `catalog.property_required` | Token senza stabilimento: dove va `auth.forbidden`. |
| 404 | `catalog.property_not_found` | Stabilimento del token non trovato: dove va `auth.forbidden`. |
| 401 | `auth.unauthenticated` | Il frontend rinnova il token e ripete la chiamata una volta; se la sessione è finita l'utente esce. |

## Verifica

API 1 e 2:

- Una sola chiamata cancella intervalli di più periodi; gli altri intervalli e gli altri periodi restano, con lo stesso
  id, la stessa `key` e lo stesso nome. La risposta è 204 senza corpo, e `GET /tariff-periods` (o
  `/next-season/tariff-periods`) subito dopo mostra solo gli intervalli rimasti.
- Tutti o nessuno: con un intervallo che non esiste (404 `pricing.period_range_not_found`), una `key` che non esiste
  (404 `pricing.period_not_found`) o un periodo che resterebbe vuoto (422 `pricing.period_ranges_required`) non si
  cancella niente.
- `{}` e `{ "ranges": [] }` rispondono 400 con `ranges` `validation.required`; 301 elementi con solo `ranges`
  `validation.invalid`; `{ "ranges": [null] }` con `ranges[0]` `validation.required`; un secondo elemento senza
  `endsOn` con `ranges[1].endsOn` `validation.required`, mai con un controllo sulla data `0001-01-01`;
  `{ "ranges": [null, { "key": 1, "startsOn": "2026-08-21" }] }` con due errori, `ranges[0]` e `ranges[1].endsOn`;
  `"key": "abc"` o la data `2026-02-30` con un 400 senza `errors[]`.
- Ordine: una lista con una `key` che non esiste e un intervallo che non esiste in un'altra `key` risponde 404
  `pricing.period_not_found`; un 400 del validatore arriva anche con un token che ha il permesso ma non lo
  stabilimento.
- `/next-season/.../bulk-delete-ranges` senza nessuna stagione risponde 422 `pricing.season_required`; con la stagione
  ma senza la successiva 422 `pricing.next_season_required`.
- Un periodo con gli intervalli A, B e C: una chiamata cancella A e un'altra, nello stesso momento, B e C. Una risponde
  204, l'altra 422 `pricing.period_ranges_required`; il periodo tiene un intervallo.
- La rotta `/next-season/...` tocca solo i periodi della stagione successiva, quella senza `/next-season` solo quelli
  della stagione corrente.
- Il registro ha una voce `catalog.tariff_period_updated` per ogni periodo toccato, con `season` `current` o `next`.

Sezione 3:

- `DELETE /tariff-periods/{key}` di un periodo con prezzi solo in un servizio (o in un'opzione di un servizio orario)
  risponde 409 `pricing.period_in_use_by_services` con il nome del servizio in `args.services`, una volta sola anche
  se il servizio ha più righe, e non elimina niente; con prezzi solo in un servizio eliminato, di un altro
  stabilimento o con `has_period_prices` = `false`, o solo in `catalog.pricing_suggested_prices`, il periodo si elimina e
  quelle righe restano; con prezzi anche in un listino risponde come oggi, `pricing.period_in_use`.
- Eliminato il periodo con la `key` più alta, il periodo creato subito dopo riceve una `key` mai usata nella stagione
  (es. 3, non 2); lo stesso nella stagione successiva. Una creazione che risponde 409 o 422 non consuma la `key`.
- Dopo la migration il contatore di ogni stagione supera ogni `key` già usata, anche nelle righe eliminate e nelle
  tabelle dei prezzi; una stagione senza periodi né prezzi parte da 0.

Sezione 4:

- `POST` e `PUT` di un periodo con un giorno già preso da un altro periodo della stessa stagione (corrente o
  successiva) rispondono 409 `pricing.period_overlap` con il nome dell'altro periodo e non salvano niente; anche quando
  l'unico giorno in comune è il primo o l'ultimo.
- Un periodo che si sovrappone solo con se stesso (modifica delle sue date) si salva, nella stagione corrente e nella
  successiva (es. "Alta" dal 01/07–31/07 al 01/07–15/08 risponde 200).
- Con due periodi della stessa stagione che hanno già giorni in comune, la migration si ferma con il suo messaggio e
  non cambia niente.

Sezione 5:

- Due `PUT` nello stesso momento dello stesso periodo rispondono tutte e due 200; il periodo resta con gli intervalli
  di quella che salva per ultima, mai con quelli di tutte e due, mai 500.
- Due salvataggi nello stesso momento di periodi diversi con giorni in comune (`POST` o `PUT`): uno riesce, l'altro
  risponde 409 `pricing.period_overlap` con il nome del primo.
- Due `POST` nello stesso momento ricevono `key` diverse e restano due periodi.
- Vincolo come protezione: in una connessione a parte, senza bloccare la stagione, inserire con SQL una riga di un altro
  periodo della stessa stagione e non fare commit; chiamare la `PUT` di un periodo con giorni in comune con quella
  riga; fare commit della connessione a parte. La `PUT` risponde 409 `pricing.period_overlap` con il nome di quel periodo, mai 500.

Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.
