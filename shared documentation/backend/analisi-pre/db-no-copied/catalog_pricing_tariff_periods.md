# Righe non migrate — `catalog.pricing_tariff_periods`

> Modulo **catalog** · sorgente legacy **`seasons_periods`** · **3 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_pricing_tariff_periods.jsonl`](dati/catalog_pricing_tariff_periods.jsonl).

## Diagnosi

### Esclusa da una regola — 3 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
periodo contenuto in un altro dello stesso gruppo e fascia: prezzo invariato (il prezzo e' agganciato al gruppo, non alla riga)
```

**Cosa significa** — Periodi tariffari contenuti in un altro dello stesso gruppo: prezzo invariato.

**Causa a monte** — Il prezzo e' agganciato al **gruppo** di periodi (`pricing_price_list_period_prices.tariff_period_group`), non alla singola riga: due periodi della stessa stagione e dello stesso gruppo hanno per costruzione lo stesso prezzo, quindi quale dei due copra un dato giorno non cambia un centesimo. Si tiene quello che contiene e si scarta quello contenuto, cosi' il vincolo di non sovrapposizione (aggiunto il 2026-09-30) puo' restare attivo.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Da non confondere con le sovrapposizioni fra **gruppi diversi**, che sono la funzione e non un difetto: un giorno di festa prezzato a parte dentro un periodo ordinario. Quelle il vincolo le permette, e le risolve il motore di prezzo con la regola "vince il periodo piu' breve" (analisi-post/bl/03).

## Righe

| id legacy | motivo | `id` | `periods` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|
| `14` | Esclusa da una regola | 14 | [{"name":"Low Season","selectedRanges":[{"startDate":"2021-… | 2021-05-19T07:12:17 | 2021-05-19T07:12:17 |
| `18` | Esclusa da una regola | 18 | [{"name":"Stagione unica","selectedRanges":[{"startDate":"2… | 2021-06-11T15:03:35 | 2021-06-15T06:35:39 |
| `38` | Esclusa da una regola | 38 | [{"name":"Stagione unica","selectedRanges":[{"startDate":"2… | 2022-05-03T15:32:03 | 2022-05-03T15:32:03 |
