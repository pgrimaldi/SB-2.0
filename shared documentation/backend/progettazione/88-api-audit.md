# API del modulo Audit

> Catalogo degli endpoint di `Module.Audit` (scritti il 2026-10-13). Decisione e regole del modulo in
> [86-modulo-audit.md](86-modulo-audit.md); errori secondo [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md).
> Il dettaglio dei campi è nello Swagger del modulo (`/swagger`, documento *Audit API*). Azione di origine:
> [`13-gestionale-impostazioni.md` §3](../analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md) (pagina «Tutti i log»).

## A cosa serve

Registro di **chi ha fatto cosa, quando e su quale oggetto**, valido per tutti i moduli e non modificabile.
Lo alimentano i moduli pubblicando l'evento `AuditEntryRecorded` (tramite `IAuditTrail`). Lo consultano:

- il **titolare** (e l'admin) dal gestionale, per ricostruire le azioni del personale e gli eventi di sicurezza
  degli account dello stabilimento;
- il **systemadmin** dal back-office, per le verifiche di piattaforma e il registro degli amministratori di
  sistema (Provv. Garante 27/11/2008).

## Regole comuni

- **Prefisso**: `/api/audit`. Token utente obbligatorio.
- **Stabilimento**: dal claim `property_id` del token, mai dalla richiesta. Un token senza stabilimento sulle
  API dello stabilimento riceve 403 `audit.property_required`.
- **Date**: `from` (incluso) e `to` (escluso) sono istanti ISO 8601 con fuso (`2026-07-01T00:00:00+02:00` o
  `…Z`). I preset del filtro («oggi», «questa settimana», «Tutte le date») li traduce il frontend nel fuso
  locale. Nelle risposte le date sono UTC.
- **Paginazione**: `page` (da 1) e `pageSize` (10, 20, 25, 50, 100); risposta `{ total, page, pageSize, items }`.
  Ordine per data, `sortDir` = `desc` (default) o `asc`.
- **Filtri ripetibili**: `actionCodes=a&actionCodes=b`, `categories=operator&categories=security`.
- **Ricerca** (`search`, max 100 caratteri): nome dell'operatore, numero di prenotazione, postazione.
- **Enum nelle risposte**: codici snake_case (`operator`, `succeeded`, `user`, …). Il frontend traduce
  `actionCode` e sceglie l'icona; la freccia verso l'oggetto si costruisce da `targetType` + `targetId`.
- **Export**: `format=csv|xlsx` (default `csv`), file scaricato direttamente. Etichette e azioni nella lingua
  dell'interfaccia di chi esporta, date in ora italiana. Oltre 50.000 righe: 422 `audit.export_too_large` con
  `args: { max, total }`. Ogni export viene registrato a sua volta nel registro.

## Stabilimento (permesso `audit.read`: superadmin, admin)

Lo stabilimento vede le categorie `operator` e `security` delle proprie voci; **non** vede `platform`, IP,
user-agent, trace id né l'identità del systemadmin.

| Metodo e percorso | Uso | Parametri | Risposta |
|---|---|---|---|
| `GET /entries` | tabella «Tutti i log»; pulsante «Log» di un utente = stesso endpoint con `actorUserId` | `from`, `to`, `actionCodes`, `actorUserId`, `search`, `sortDir`, `page`, `pageSize` | `PagedResult<AuditEntryDto>` |
| `GET /entries/export` | pulsante «Esporta» | gli stessi filtri + `format` | file CSV/XLSX |
| `GET /action-codes` | valori del filtro «Tipologia di azione» | — | `[{ code, category, sourceModule }]` (solo categorie visibili) |
| `GET /actors?search=` | valori del filtro «Utente», anche operatori non più presenti | `search` | `[{ userId, name, role }]` (ultimo nome registrato) |

`AuditEntryDto` = `{ id, occurredAt, category, actionCode, outcome, actorKind, actorUserId, actorName, actorRole,
impersonated, targetType, targetId, bookingNumber, spotLabel, metadata }`.

Corrispondenza con la pagina: «nome e cognome operatore» → `actorName`; «ID prenotazione» → `bookingNumber`;
«postazione» → `spotLabel`; «azione» e icona → `actionCode`; «data e ora» → `occurredAt`; freccia →
`targetType`/`targetId`. `impersonated: true` = azione fatta dal supporto della piattaforma entrato come
gestore. «Stampa» è del frontend.

## Systemadmin (permesso `platform.manage`)

| Metodo e percorso | Parametri | Risposta |
|---|---|---|
| `GET /admin/entries` | come `/entries` + `propertyId` (facoltativo), `categories` (vuoto = tutte) | `PagedResult<PlatformAuditEntryDto>` |
| `GET /admin/entries/export` | gli stessi filtri + `format` | file CSV/XLSX |
| `GET /admin/action-codes` | — | tutto il catalogo |

`PlatformAuditEntryDto` aggiunge `recordedAt`, `sourceModule`, `propertyId`, `impersonatorUserId`, `clientIp`,
`clientUserAgent`, `traceId`. Nessuna pagina del back-office usa ancora questi endpoint.

## Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `to` non successivo a `from`, `pageSize` non ammesso, `sortDir` o categoria non validi, più di 50 `actionCodes` |
| 401 | `auth.unauthenticated` | token mancante o non valido |
| 403 | `auth.forbidden` | permesso mancante |
| 403 | `audit.property_required` | token senza stabilimento sulle API dello stabilimento |
| 422 | `audit.export_too_large` | export oltre 50.000 righe |

## Scrittura (in-process, nessun endpoint)

`AuditEntryRecordedHandler` salva ogni `AuditEntryRecorded` con `INSERT … ON CONFLICT (source_event_id,
occurred_at) DO NOTHING`: una seconda consegna dello stesso evento non duplica la voce. Un `sourceModule`
sconosciuto viene scartato e scritto nel log degli errori. Codici azione e categorie in
`Shared/Contracts/Audit/AuditActionCodes.cs`; un codice nuovo va tradotto anche nei `.resx` dell'export.
