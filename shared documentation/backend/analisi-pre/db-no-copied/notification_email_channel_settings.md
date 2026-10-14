# Righe non migrate — `notification.email_channel_settings`

> Modulo **notification** · sorgente legacy **`bathhouse_email_configrations`** · **2 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/notification_email_channel_settings.jsonl`](dati/notification_email_channel_settings.jsonl).

## Diagnosi

### Esclusa da una regola — 2 righe

**Regola applicata** (`exclude_row_if` nella spec di mapping):

```
fromEmail is None or smtpServer is None
```

**Cosa significa** — Configurazione SMTP incompleta (manca il mittente o il server).

**Causa a monte** — Una configurazione di invio priva di mittente o di server non e' utilizzabile: migrarla produrrebbe un canale che fallisce al primo invio.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. Lo stabilimento ricadra' sul mittente predefinito di piattaforma.

## Righe

| id legacy | motivo | `id` | `propertyId` | `status` | `createdAt` | `updatedAt` |
|---|---|---|---|---|---|---|
| `4` | Esclusa da una regola | 4 | 10566 | 1 | 2025-05-04T19:39:38 | 2025-05-04T19:39:38 |
| `5` | Esclusa da una regola | 5 | 115 | 0 | 2025-05-07T22:14:13 | 2026-05-11T10:14:47 |
