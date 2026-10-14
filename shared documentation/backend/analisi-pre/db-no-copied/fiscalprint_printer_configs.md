# Righe non migrate — `fiscalprint.printer_configs`

> Modulo **fiscalprint** · sorgente legacy **`printer_configs`** · **15 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/fiscalprint_printer_configs.jsonl`](dati/fiscalprint_printer_configs.jsonl).

## Diagnosi

### Esclusa da una regola — 15 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
model not in ('FP-81','FP-90','RCH','TM-T88','TM-T20','ONLINE') or (port is not None and port.count('.') == 3)
```

**Cosa significa** — Stampanti fiscali con **modello non riconosciuto**, oppure con la porta valorizzata con un indirizzo IP anziche' con una porta.

**Causa a monte** — Il nuovo schema tipizza il modello come enum chiuso (`FP-81`, `FP-90`, `RCH`, `TM-T88`, `TM-T20`, `ONLINE`) e tiene l'indirizzo separato dalla porta. Il legacy accettava testo libero in entrambi i campi.

**Verdetto: ❓ Da decidere** — il meccanismo e' corretto, ma serve una scelta di business sul dato

**Azione** — **Da guardare**: 15 configurazioni su 58, il 26% — una quota troppo alta per essere solo sporcizia. Due possibilita': (a) sono stampanti dismesse, e allora va bene; (b) esistono modelli in uso che l'enum non prevede, e allora va esteso l'enum, non scartata la configurazione. La differenza la sa solo chi conosce il parco stampanti.

## Righe

| id legacy | motivo | `id` | `printername` | `isActive` | `userId` | `propertyId` | `ip` |
|---|---|---|---|---|---|---|---|
| `11` | Esclusa da una regola | 11 | fffff | 0 | 514 | 228 | 1.1.1.1 |
| `12` | Esclusa da una regola | 12 | ghfdghfd | 0 | 514 | 228 | 1.1.1.2 |
| `14` | Esclusa da una regola | 14 | TestPrinter | 1 | 199 | 96 | 11.11.111.11 |
| `15` | Esclusa da una regola | 15 | asdasd | 1 | 199 | 96 | 1.2.34.4 |
| `36` | Esclusa da una regola | 36 | EPSON TM-T20II | 1 | 3313 | 10807 | 192.168.0.188 |
| `37` | Esclusa da una regola | 37 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `38` | Esclusa da una regola | 38 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `39` | Esclusa da una regola | 39 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `40` | Esclusa da una regola | 40 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `41` | Esclusa da una regola | 41 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `42` | Esclusa da una regola | 42 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `43` | Esclusa da una regola | 43 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `44` | Esclusa da una regola | 44 | EPSON TM-T20III | 1 | 3313 | 10807 | 192.168.0.188 |
| `45` | Esclusa da una regola | 45 | EPSON TM-T20II | 1 | 3313 | 10807 | 192.168.0.188 |
| `46` | Esclusa da una regola | 46 | EPSON TM-T20II | 1 | 3313 | 10807 | 192.168.0.188 |
