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
| 3 | DELETE | `/api/catalog/tariff-periods/{key}` | **Modifica**: prima di eliminare controlla anche i servizi che usano il periodo |
| 4 | POST, PUT | `/api/catalog/[next-season/]tariff-periods[/{key}]` | **Modifica**: nessun giorno in comune tra periodi diversi della stessa stagione |

Le API 1 e 2 sono **nuove**: oggi un intervallo si cancella solo con `PUT /tariff-periods/{key}`, un periodo alla volta.
Il frontend ha bisogno di cancellare con **una sola chiamata** intervalli di periodi diversi, e di non cancellarne
nessuno se uno non si può cancellare. La 3 e la 4 sono **modifiche** ad API che esistono già.

## Regole comuni

Valgono per le API 1 e 2.

- **Modulo e rotte**: Catalog (`Module.Catalog`), in `Endpoints/SeasonEndpoints.cs` e `SeasonService`, nei gruppi
  `/tariff-periods` e `/next-season/tariff-periods`.
- **Le API esistenti non cambiano**: `/api/catalog/season`, `/api/catalog/tariff-periods` e
  `/api/catalog/next-season/tariff-periods`, con le loro sotto-rotte (`GET`, `POST`, `PUT` e `DELETE /{key}`), restano
  come sono; le due nuove si aggiungono. Le uniche modifiche sono quelle delle sezioni 3 e 4.
- **Stabilimento dal token**: come per le altre API dei periodi; nessuna richiesta lo contiene.
- **Permessi**: come `PUT /tariff-periods/{key}` (`CatalogSettings`). Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. Le date sono giorni `yyyy-MM-dd`, come in `DateRangeDto`.
- **Tutti o nessuno**: diversamente dalle altre `bulk-delete` del Catalog, che rispondono 200 con `succeeded` e
  `failed` e cancellano quello che possono, queste due fanno tutti i controlli prima di cancellare e salvano in una sola
  transazione. Se un controllo fallisce non si cancella niente.
- **Errori** nel formato Problem Details del backend, con `code`. Senza token valido: 401 `auth.unauthenticated`.

## 1. POST /api/catalog/tariff-periods/bulk-delete-ranges

Tab "Stagione Corrente": bottone "Cancella selezionati" e cestino di un intervallo, dopo la conferma nel popup.

### Richiesta

Gli intervalli da cancellare, anche di periodi diversi:

```json
{
  "ranges": [
    { "key": 0, "startsOn": "2026-06-01", "endsOn": "2026-06-15" },
    { "key": 0, "startsOn": "2026-08-20", "endsOn": "2026-08-31" },
    { "key": 2, "startsOn": "2026-07-01", "endsOn": "2026-07-10" }
  ]
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `ranges` | array | Obbligatorio, da 1 a 300 elementi. |
| `ranges[].key` | number | La `key` del periodo, come in `GET /tariff-periods`. |
| `ranges[].startsOn` | string (`yyyy-MM-dd`) | Primo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |
| `ranges[].endsOn` | string (`yyyy-MM-dd`) | Ultimo giorno dell'intervallo, come in `ranges` di `GET /tariff-periods`. |

Un intervallo si riconosce da `key`, `startsOn` ed `endsOn` insieme: dentro un periodo gli intervalli non si
sovrappongono, quindi non ce ne sono due con le stesse date. Lo stesso intervallo ripetuto nella lista conta una volta.

### Cosa fa

- Per ogni periodo della lista toglie gli intervalli indicati e tiene gli altri, con lo stesso nome e la stessa `key`:
  il risultato è quello di un `PUT /tariff-periods/{key}` con gli intervalli rimasti.
- I periodi che non compaiono nella lista restano come sono.
- Nel registro: una voce `catalog.tariff_period_updated` per ogni periodo toccato, come per il `PUT`.

### Risposta

204 senza corpo. Il frontend poi ricarica i periodi con `GET /tariff-periods`.

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Lista vuota, più di 300 elementi o un elemento senza `key`, `startsOn` o `endsOn`: `errors[]` con `field` `ranges` (es. `validation.required`). |
| 422 | `pricing.season_required` | Lo stabilimento non ha la stagione. |
| 404 | `pricing.period_not_found` | Una `key` non è un periodo della stagione. |
| 404 | `pricing.period_range_not_found` (nuovo) | Un intervallo non c'è nel suo periodo: `startsOn` ed `endsOn` non sono le date di un intervallo di quella `key`. |
| 422 | `pricing.period_ranges_required` (nuovo) | Un periodo resterebbe senza intervalli, senza `args`. Il periodo intero si cancella con `DELETE /tariff-periods/{key}`. |
| 403 | `auth.forbidden` | Utente senza il permesso `CatalogSettings`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

I due codici nuovi vanno aggiunti a `CatalogErrors`, accanto agli altri `pricing.period_*`.

## 2. POST /api/catalog/next-season/tariff-periods/bulk-delete-ranges

Tab "Prossima Stagione": come la sezione 1, sui periodi della stagione successiva. Cambia solo questo:

- le `key` sono quelle di `GET /next-season/tariff-periods`;
- se lo stabilimento non ha la stagione successiva l'errore è 422 `pricing.next_season_required` (al posto di
  `pricing.season_required`);
- le voci del registro sono quelle delle altre API della stagione successiva (`season` = `next`).

Richiesta, risposta e gli altri errori sono gli stessi della sezione 1.

## 3. DELETE /api/catalog/tariff-periods/{key}: controllare anche i servizi

API che esiste già (cestino di un periodo nella tab "Stagione Corrente"): rotta, risposta ed errori di oggi restano
uguali, cambia solo il controllo che si fa prima di eliminare.

### Oggi

`SeasonService.DeletePeriodAsync` chiede a `ListPeriodUsagesAsync` dove il periodo è usato e, se lo è, risponde 409
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

- Prima di eliminare, il backend controlla anche `service_sale_item_period_prices` e `service_hourly_options` (righe non
  eliminate, `deleted_at` nullo) dei servizi dello stabilimento, con la chiave del periodo.
- Se il periodo ha prezzi nei listini, la risposta resta quella di oggi: 409 `pricing.period_in_use` con
  `args.priceLists`.
- Se non ha prezzi nei listini ma è usato da dei servizi, risponde 409 con un **codice nuovo**,
  `pricing.period_in_use_by_services`, e in `args.services` l'elenco dei nomi dei servizi (il nome italiano), come
  `args.priceLists` è l'elenco dei listini:

```json
{
  "status": 409,
  "code": "pricing.period_in_use_by_services",
  "title": "Period in use by services",
  "args": { "services": ["Caffè", "Campo da beach volley"] }
}
```

- `pricing_suggested_prices`: da valutare dal backend. Se sono suggerimenti che il sistema rifà da solo, si possono
  eliminare insieme al periodo; altrimenti vanno controllati come i listini.
- Il codice nuovo va aggiunto a `CatalogErrors`, accanto a `pricing.period_in_use`. Il frontend aggiunge il suo testo
  ("Il periodo è usato dai servizi …: togli prima i loro prezzi per questo periodo, poi eliminalo.") appena il codice
  esiste.
- La prossima stagione (`DELETE /api/catalog/next-season/tariff-periods/{key}`) oggi non fa nessun controllo: i prezzi
  di listini e servizi si legano ai periodi della stagione corrente, quindi lì non cambia niente.

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

- Prima di salvare (creazione e modifica, in `EnsurePeriodAllowed` / `EnsureNextPeriodAllowed`), il backend controlla
  che nessun intervallo della richiesta abbia giorni in comune con gli intervalli degli **altri** periodi della stessa
  stagione (righe non eliminate; nella modifica senza contare quelli del periodo stesso, che vengono sostituiti).
  Il primo e l'ultimo giorno contano: un intervallo che finisce il 15 giugno e uno che comincia il 15 giugno si
  sovrappongono.
- Se succede, risponde 409 con un **codice nuovo**, `pricing.period_overlap`, senza `errors[]`, e in `args.periodName`
  il nome del primo altro periodo che ha giorni in comune:

```json
{
  "status": 409,
  "code": "pricing.period_overlap",
  "title": "Period overlaps another period",
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
- Il codice nuovo va aggiunto a `CatalogErrors`, accanto agli altri `pricing.period_*`. Il frontend aggiunge il suo
  testo ("Le date si sovrappongono al periodo …: scegli giorni liberi.") appena il codice esiste.

## Verifica

- Una sola chiamata cancella intervalli di più periodi; gli altri intervalli e gli altri periodi restano, con la stessa
  `key` e lo stesso nome.
- Tutti o nessuno: con un intervallo che non esiste, o con un periodo che resterebbe vuoto, non si cancella niente.
- Un periodo con tutti i suoi intervalli nella lista dà 422 `pricing.period_ranges_required`.
- La rotta `/next-season/...` tocca solo i periodi della stagione successiva, quella senza `/next-season` solo quelli
  della stagione corrente.
- La risposta è 204 senza corpo, e `GET /tariff-periods` (o `/next-season/tariff-periods`) subito dopo mostra solo gli
  intervalli rimasti.
- `DELETE /tariff-periods/{key}` di un periodo con prezzi solo in un servizio (o in un'opzione di un servizio orario)
  risponde 409 `pricing.period_in_use_by_services` con il nome del servizio in `args.services` e non elimina niente;
  con prezzi anche in un listino risponde come oggi, `pricing.period_in_use`.
- `POST` e `PUT` di un periodo con un giorno già preso da un altro periodo della stessa stagione (corrente o
  successiva) rispondono 409 `pricing.period_overlap` con il nome dell'altro periodo e non salvano niente; un periodo
  che si sovrappone solo con se stesso (modifica delle sue date) si salva.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.
