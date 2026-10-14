# Righe non migrate — `switchboard.switchboard_voice_softphone_devices`

> Modulo **switchboard** · sorgente legacy **`switchboard_config`** · **1 righe** non arrivate a destinazione.
>
> Dati grezzi completi, mai troncati: [`dati/switchboard_switchboard_voice_softphone_devices.jsonl`](dati/switchboard_switchboard_voice_softphone_devices.jsonl).

## Diagnosi

### Esclusa da una regola — 1 righe

**Regola applicata** — non dichiarata nella spec YAML ma scritta nel `custom_row_transform`, quindi il registro non poteva conoscerla: e' stata letta nel codice.

```python
not token                    # nessun token di registrazione sul campo atteso
not property_public_id       # stabilimento non risolto
```

**Cosa significa** — Dispositivo softphone senza token di registrazione.

**Causa a monte** — Il token e' l'unico dato che identifica il dispositivo: senza, la riga non descrive alcun apparecchio.

**Verdetto: ✅ Voluto** — nessuna correzione: il comportamento e' quello previsto dal disegno

**Azione** — Nessuna. 1 riga.

## Righe

| id legacy | motivo | `id` | `property_id` | `telnyx_phone_number` | `telnyx_phone_number_id` | `telnyx_connection_id` | `sip_endpoint_id` |
|---|---|---|---|---|---|---|---|
| `1` | Esclusa da una regola | 1 | 96 | +390620197004 | 2938439275730961981 | 2934344913128523132 | f148d20a-b717-4069-a1da-dbb8f5025fe4 |
