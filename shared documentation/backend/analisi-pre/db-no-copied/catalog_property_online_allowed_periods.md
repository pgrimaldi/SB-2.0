# Righe non migrate — `catalog.property_online_allowed_periods`

> Modulo **catalog** · sorgente legacy **`properties`** · **13 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_property_online_allowed_periods.jsonl`](dati/catalog_property_online_allowed_periods.jsonl).

## Diagnosi

### Esclusa da una regola — 13 righe

**Regola applicata** — non dichiarata nella spec YAML ma scritta nel `custom_row_transform`, quindi il registro non poteva conoscerla: e' stata letta nel codice.

```python
starts_on is None or ends_on is None      # date mancanti o non parsabili
ends_on < starts_on                       # intervallo invertito
```

**Cosa significa** — Finestre di apertura online con date mancanti, non interpretabili, oppure con la fine prima dell'inizio.

**Causa a monte** — Il nuovo schema ha `CHECK (ends_on >= starts_on)`; il legacy non validava questo campo lato client. Un intervallo invertito non ha un significato ricostruibile: non si sa quale delle due date sia quella sbagliata.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna correzione automatica sensata. Sono 6 righe: se servissero, si correggono a mano nel legacy prima del cutover.

## Righe

| id legacy | motivo | `id` | `propertyName` | `city` | `fullName` | `mobile` | `email` |
|---|---|---|---|---|---|---|---|
| `109` | Esclusa da una regola | 109 | Goa Family beach | Mumbai | Pradip Kushare | 8055576553 | pradipkushare@gmail.com |
| `116` | Esclusa da una regola | 116 | Lido Tamerici | Cassano All'Ionio | Antonello Guaragna | 3899666188 | antonelloguaragna@yahoo.it |
| `179` | Esclusa da una regola | 179 | Midnight Testing Bathing | Brisbane | Harish Mogal | 9856898989 | harishmogal@yopmail.com |
| `198` | Esclusa da una regola | 198 | Lido Mediterraneo | Gizzeria | Antonio Nirello | 3898378612 | gianfranconirello24@gmail.com |
| `232` | Esclusa da una regola | 232 | Lido San Basilio | Pisticci | Lucia Locaso | 0835 470075 | info@lidosanbasilio.it |
| `281` | Esclusa da una regola | 281 | Holiday Beach | Paola | Roberto Pennestrì | 3486921845 | holidaybeach@libero.it |
| `10566` | Esclusa da una regola | 10566 | Lido Sole Splendid | Maiori | Alessandro Di Martino | 3478006945 | amministrazione@lidosolesplendid.it |
| `10588` | Esclusa da una regola | 10588 | Marine Village | Ostia | Debora Berini | 0691903724 | direzione@marinevillage.it |
| `10588` | Esclusa da una regola | 10588 | Marine Village | Ostia | Debora Berini | 0691903724 | direzione@marinevillage.it |
| `10588` | Esclusa da una regola | 10588 | Marine Village | Ostia | Debora Berini | 0691903724 | direzione@marinevillage.it |
| `10588` | Esclusa da una regola | 10588 | Marine Village | Ostia | Debora Berini | 0691903724 | direzione@marinevillage.it |
| `10588` | Esclusa da una regola | 10588 | Marine Village | Ostia | Debora Berini | 0691903724 | direzione@marinevillage.it |
| `10828` | Esclusa da una regola | 10828 | Lido Enea | Bacoli | Rosario Scotto | 3382546069 | enealido@gmail.com |
