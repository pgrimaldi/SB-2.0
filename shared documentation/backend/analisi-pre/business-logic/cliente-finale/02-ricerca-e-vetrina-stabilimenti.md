# cliente finale — Ricerca e vetrina stabilimenti

> **Ambito**: home del sito pubblico, elenco stabilimenti "in vetrina", ricerca per località/data,
> autosuggest, prezzo minimo mostrato in lista, filtri per servizi/regione.
>
> **Metodo**: `front-customer-pre/src/components/home/*`, `homeDetails/*`, `landingPage/*` +
> `property.controller.js` (`propertyListOnHomePage`, `serchProperty`, `getPropertyIdAndBathhouseFromSlug`,
> `locationAutosuggestion`, `getPropertyLikes`, `statistics`) + `searchRegion.controller.js` +
> `confimages`/`services`/`servicetypeicons` (cataloghi). Schede DB: [`bathouses.md`](../../db/bathouses.md),
> [`properties.md`](../../db/properties.md), [`property_search_regions.md`](../../db/property_search_regions.md),
> [`seasons_periods.md`](../../db/seasons_periods.md), [`price_lists.md`](../../db/price_lists.md).

---

## 1. Elenco stabilimenti in vetrina (home)

**Azione** — `POST propertyListOnHomePage`: il visitatore apre la home e vede gli stabilimenti
pubblicati (con foto, nome, città, "mi piace", prezzo minimo).

**Logiche**: seleziona i `bathouses`/`properties` visibili; per la ricerca pubblica il filtro è
`properties.status IN (1, 3)` (approvati). Ordinamento per rilevanza/like/città.

**Calcoli**: numero di "mi piace" (`bathouses.likeCount`, denormalizzato). **Eventi**: nessuno.

---

## 2. Ricerca per località e date

**Azione** — `POST property/search` (`serchProperty`): il visitatore inserisce località e intervallo
di date; ottiene gli stabilimenti corrispondenti con il **prezzo minimo** per quelle date.

**Logiche**:

- Filtro `status IN (1, 3)` + match su località/regione (`property_search_regions`,
  `bathouses.city/region/province`) + eventuale filtro servizi.
- Per ogni stabilimento in risultato calcola `lowestPrice` per l'intervallo richiesto.

**Calcolo del prezzo minimo (`getLowestPrice(propertyId, startDate, endDate)`)**:

1. Legge i **periodi** dello stabilimento (`seasons_periods.periods`); determina il periodo che
   copre le date richieste (`getMatchingPeriodName`); se nessuno → prezzo `0`.
2. Query SQL su `price_lists` (`JSON_TABLE` su `detailPrices`): considera **solo** i pacchetti con
   `type = "package"` **e** `details.enableOnlineReservation = TRUE` **e** voce di `detailPrices` il
   cui `name` contiene (`LIKE '%…%'`) il nome del periodo.
3. Per ogni voce calcola `LEAST(daily_price, half_day_price)` (i vuoti sono ignorati), poi prende il
   **`MIN`** su tutte le voci → prezzo minimo mostrato in lista.

**Eventi**: nessuno.

---

## 3. Autosuggest località / stabilimento

**Azione** — `POST locationAutosuggestion` (+ `getByName` per la ricerca per nome): mentre il
visitatore digita, propone località/stabilimenti corrispondenti.

**Logiche**: match parziale su nome stabilimento / città / regione. **Calcoli/eventi**: nessuno.

---

## 4. Apertura scheda da slug

**Azione** — `POST getPropertyIdAndBathhouseFromSlug` (rotta `/property-details/:slug/:affiliate?`):
risolve lo **slug** pubblico (`bathouses.slug`) in `propertyId` + `bathHouseId` per aprire la scheda
(vedi [03-scheda-stabilimento](03-scheda-stabilimento.md)). Il parametro opzionale `:affiliate`
traccia l'arrivo da un canale affiliato.

**Calcoli/eventi**: nessuno.

---

## 5. Filtri per servizi e regioni

**Azione**: filtri per categoria servizio (`service_type_filters`) e per regione di ricerca
(`property_search_regions`).

**Logiche**: intersezione fra gli stabilimenti che offrono i servizi selezionati e quelli associati
alla regione scelta. **Calcoli/eventi**: nessuno.

---

## 6. "Mi piace" aggregati e statistiche pubbliche

**Azioni** — `property/likes` (`getPropertyLikes`), `property/statistics`.

**Logiche**: mostra il numero di preferiti di uno stabilimento e alcuni KPI pubblici. La gestione del
"mi piace" del singolo utente è in [09-preferiti-e-condivisione](09-preferiti-e-condivisione.md).

**Calcoli**: conteggi. **Eventi**: nessuno.

---

## 7. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: prezzo minimo per data = `MIN( LEAST(daily_price, half_day_price) )` sulle voci di
  `detailPrices` del periodo, solo pacchetti online‑abilitati; conteggio "mi piace".
- **Logiche**: visibilità solo `status IN (1, 3)`; abbinamento periodo↔data; match `LIKE` nome
  periodo; match località/regione/servizi.
- **Eventi**: **nessuno** in tutta l'area (sola lettura pubblica).
