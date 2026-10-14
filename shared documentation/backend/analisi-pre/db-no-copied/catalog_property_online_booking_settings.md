# Righe non migrate — `catalog.property_online_booking_settings`

> Modulo **catalog** · sorgente legacy **`properties`** · **8 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/catalog_property_online_booking_settings.jsonl`](dati/catalog_property_online_booking_settings.jsonl).

## Diagnosi

### Esclusa da una regola — 8 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
not onlineBookingOptions and not onlineReservation
```

**Cosa significa** — Stabilimenti che **non hanno mai attivato** la prenotazione online.

**Causa a monte** — Stessa normalizzazione di `notification_user_channel_preferences`: la riga di impostazioni si crea solo se il canale online e' stato toccato almeno una volta (un flag, o il JSON di configurazione). Uno stabilimento che non vende online non ha impostazioni di vendita online da conservare.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. L'assenza di riga *e'* "canale online non configurato", esattamente come nel legacy lo erano due campi vuoti.

## Righe

| id legacy | motivo | `id` | `propertyName` | `city` | `fullName` | `mobile` | `email` |
|---|---|---|---|---|---|---|---|
| `37` | Esclusa da una regola | 37 | Lido San Domenico | Soverato | Paolo Vitale | 3922837123 | amministrazionelidosandomenico@gmail.com |
| `87` | Esclusa da una regola | 87 | solace testing 123 | nashik pune mumbai | testing testing india | 1234567890 | kushshiva@yopmail.com |
| `88` | Esclusa da una regola | 88 | Bagno Skiuma | Piombino | Massimo Esposito | 3917151869 | info@bagnoskiuma.com |
| `90` | Esclusa da una regola | 90 | Bagno Curaçao | Piombino | Massimo Esposito | 3394252019 | matteo.bagnoskiuma@outlook.it |
| `114` | Esclusa da una regola | 114 | Lido Stella Maris | Villapiana | Marco Mignogna | 3923285881 | fmultiservizi@gmail.com |
| `131` | Esclusa da una regola | 131 | Bagni Piccolo Jolly | Taggia | Michele Nigro | 3927431372 | pjbar2021@gmail.com |
| `146` | Esclusa da una regola | 146 | Lido Solaria | Brancaleone | Martina Schimizzi | 3517691928 | martischim@libero.it |
| `157` | Esclusa da una regola | 157 | Posidonia Beach | Diamante | Valentina Salemme | 3319750009 | valentinadiamante@hotmail.it |
