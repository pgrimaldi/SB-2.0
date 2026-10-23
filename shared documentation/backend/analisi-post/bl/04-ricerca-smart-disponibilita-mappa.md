# Ricerca smart: postazioni libere in un intervallo di date

> **Stato**: funzionalità **richiesta dal committente** il 2026-10-21, da implementare. Modulo
> `booking`. Non esiste nel legacy: la mappa mostrava lo stato di un giorno (o di un periodo) per
> cella, ma non diceva **in quali giorni** una postazione è libera dentro un periodo più lungo.

## 1. Cosa chiede

Nella pagina della **mappa** il gestore indica due date, *da* e *a*. Il backend risponde, per lo
stabilimento (`property_id`) e per ogni postazione della mappa, se in quell'intervallo è:

- **libera tutti i giorni**;
- **occupata tutti i giorni**;
- **libera solo alcuni giorni** — e in questo caso restituisce **tutti i tratti contigui** di giorni
  liberi.

Esempio: ricerca dal 1 al 25 agosto; l'ombrellone 1 risulta libero **dal 2 al 7** e **dal 15 al 18**.

## 2. Firma proposta

```
GET /api/booking/properties/{propertyId}/map/free-ranges?from=2026-08-01&to=2026-08-25
```

Ruolo: staff dello stabilimento (stessi permessi della lettura mappa). Filtri facoltativi da
valutare col front: `sectorId`, `resourceType` (default: le postazioni della mappa, cioè
`catalog_map_spots`).

Risposta (una voce per postazione, anche quelle tutte libere o tutte occupate, così il front colora
l'intera mappa con una sola chiamata):

```json
{
  "from": "2026-08-01",
  "to": "2026-08-25",
  "spots": [
    {
      "spotId": "…",
      "label": "1",
      "status": "partial",
      "freeRanges": [
        { "from": "2026-08-02", "to": "2026-08-07" },
        { "from": "2026-08-15", "to": "2026-08-18" }
      ]
    },
    { "spotId": "…", "label": "2", "status": "free",     "freeRanges": [ { "from": "2026-08-01", "to": "2026-08-25" } ] },
    { "spotId": "…", "label": "3", "status": "occupied", "freeRanges": [] }
  ]
}
```

`status` ∈ `free` / `partial` / `occupied`; gli estremi dei tratti sono **inclusivi**, come le date
inviate.

## 3. Da dove si legge

Un giorno di una postazione è **occupato** se esiste almeno una riga viva (`deleted_at IS NULL`) in
[`booking_resource_consumptions`](../db/booking/booking_resource_consumptions.md) con
`resource_id` = la postazione, `consumption_date` nel giorno e `state` in (`confirmed`, `held`)
— dove `held` copre anche i blocchi di
[`booking_spot_holds`](../db/booking/booking_spot_holds.md), che producono righe di consumo con
`source_kind = manual_hold`. Le righe `released` (assenze, sharing, annullamenti, check-out
anticipati) **non** occupano: quei giorni risultano liberi, coerentemente con la mappa.

Il calcolo dei tratti si fa in SQL con la tecnica *gaps and islands*, senza ciclare giorno per
giorno in applicazione:

```sql
WITH giorni AS (
  SELECT s.id AS spot_id, d::date AS giorno
    FROM catalog.map_spots s
   CROSS JOIN generate_series(:from::date, :to::date, interval '1 day') d
   WHERE s.property_id = :property AND s.is_bookable AND s.deleted_at IS NULL
),
liberi AS (
  SELECT g.spot_id, g.giorno
    FROM giorni g
   WHERE NOT EXISTS (
     SELECT 1 FROM booking.resource_consumptions c
      WHERE c.property_id = :property
        AND c.resource_id = g.spot_id
        AND c.consumption_date = g.giorno
        AND c.state IN ('confirmed', 'held')
        AND c.deleted_at IS NULL)
)
SELECT spot_id, min(giorno) AS dal, max(giorno) AS al
  FROM (SELECT spot_id, giorno,
               giorno - (row_number() OVER (PARTITION BY spot_id ORDER BY giorno))::int AS isola
          FROM liberi) t
 GROUP BY spot_id, isola
 ORDER BY spot_id, dal;
```

Lo schema di catalog e le colonne esatte della postazione vanno verificati sull'entità reale al
momento dell'implementazione; la forma della query resta questa. La chiave di partizione
(`consumption_date`) è nel filtro, quindi PostgreSQL legge solo le partizioni degli anni coperti
dall'intervallo.

Il filtro sul giorno **non** guarda `booking_period`: una riga `morning` o `afternoon` occupa il
giorno quanto una `full_day` (decisione del §4).

[`booking_resource_availability_daily`](../db/booking/booking_resource_availability_daily.md) sarebbe
la fonte naturale (già per postazione × giorno), ma oggi è **vuota**: finché non è alimentata si legge
il dettaglio dei consumi. Quando lo sarà, la query cambia solo nel `NOT EXISTS`.

## 4. Decisioni del committente (2026-10-21)

1. **Mezza giornata**: un ombrellone prenotato solo di mattina o solo di pomeriggio è **occupato**
   per quel giorno. Non esiste uno stato «mezzo libero».
2. **Intervallo illimitato**: nessuna lunghezza massima fra *da* e *a*. Il costo cresce con
   postazioni × giorni (300 postazioni su un anno ≈ 110.000 giorni-postazione, una query sola):
   accettabile; se in futuro servisse, si pagina per settore, non si limita l'intervallo.
3. **Blocchi**: le postazioni bloccate senza prenotazione
   ([`booking_spot_holds`](../db/booking/booking_spot_holds.md): manutenzione, VIP, postazioni tenute
   dal planner, checkout online in corso) sono **occupate**, senza uno stato distinto.
4. **Postazioni non prenotabili**
   ([`catalog_map_spots.is_bookable`](../db/catalog/catalog_map_spots.md) = `false`): **escluse**
   dalla risposta.

## 5. Cosa resta da fare

- [ ] Endpoint e query nel modulo `booking`, con la firma del §2.
- [ ] Aggiungere l'endpoint al catalogo API del modulo e al contratto per il front.
