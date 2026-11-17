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

Sono **API nuove**: oggi un intervallo si cancella solo con `PUT /tariff-periods/{key}`, un periodo alla volta. Il
frontend ha bisogno di cancellare con **una sola chiamata** intervalli di periodi diversi, e di non cancellarne nessuno
se uno non si può cancellare.

## Regole comuni

Valgono per tutte e due le API.

- **Modulo e rotte**: Catalog (`Module.Catalog`), in `Endpoints/SeasonEndpoints.cs` e `SeasonService`, nei gruppi
  `/tariff-periods` e `/next-season/tariff-periods`.
- **Le API esistenti non cambiano**: `/api/catalog/season`, `/api/catalog/tariff-periods` e
  `/api/catalog/next-season/tariff-periods`, con le loro sotto-rotte (`GET`, `POST`, `PUT` e `DELETE /{key}`), restano
  come sono; le due nuove si aggiungono.
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

## Verifica

- Una sola chiamata cancella intervalli di più periodi; gli altri intervalli e gli altri periodi restano, con la stessa
  `key` e lo stesso nome.
- Tutti o nessuno: con un intervallo che non esiste, o con un periodo che resterebbe vuoto, non si cancella niente.
- Un periodo con tutti i suoi intervalli nella lista dà 422 `pricing.period_ranges_required`.
- La rotta `/next-season/...` tocca solo i periodi della stagione successiva, quella senza `/next-season` solo quelli
  della stagione corrente.
- La risposta è 204 senza corpo, e `GET /tariff-periods` (o `/next-season/tariff-periods`) subito dopo mostra solo gli
  intervalli rimasti.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene.
