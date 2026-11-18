# Gestione utenti - Frontend requests

Le modifiche che il backend deve fare perché la pagina **Gestione utenti** del gestionale funzioni come la vuole il
frontend. Il documento è pensato per chi le implementa (anche un agente): ogni sezione dice cosa creare, con payload,
risposta, regole ed errori attesi.

La pagina è `/settings/user-management`: una tabella con gli utenti dello stabilimento (Email, Cellulare, Tipo di
account) e i popup per aggiungere, modificare ed eliminare un utente. Dalla pagina si apre anche la schermata del
registro, `/settings/user-management/logs`.

## Riepilogo

| # | Metodo | Rotta | Cosa fa |
|---|---|---|---|
| 1 | POST | `/api/identity/users-list` | Elenco degli utenti per la tabella, a pagine |
| 2 | POST | `/api/identity/create-user` | Crea un utente con i campi del popup "Aggiungi nuovo utente" |
| 3 | GET | `/api/identity/get-selected-user` | Legge l'utente da modificare |
| 4 | POST | `/api/identity/update-user` | Salva i campi del popup "Modifica utente" |
| 5 | POST | `/api/audit/entries-list` | Voci del registro per la tabella, a pagine |
| 6 | POST | `/api/identity/user-combobox` | La tendina degli utenti (qui: filtro del registro) |
| 7 | POST | `/api/audit/print-log` | Documento da stampare con le voci del registro filtrate |
| 8 | POST | `/api/audit/export-log` | File Excel con le voci del registro filtrate |

Oltre alle API, la sezione 9 chiede i codici di azione del registro per i moduli che oggi non hanno API (prenotazioni,
pagamenti, cassa, ordini, database, prenotazioni dei tavoli).

## Regole comuni

- **Moduli**: le API `/api/identity/*` stanno in Identity (`Module.Identity`), accanto a `/api/identity/property-users`
  (`Endpoints/PropertyEndpoints.cs`, `StaffService`), con i dati di `StaffUserDto`; le API `/api/audit/*` stanno in Audit
  (`Module.Audit`), accanto a `/api/audit/entries` (`Endpoints/PropertyAuditEndpoints.cs`, `AuditEntryService`), con le
  voci di `AuditEntryDto`.
- **Le API esistenti non cambiano**: `/api/identity/property-users` con le sue sotto-rotte, `/api/audit/entries`,
  `/api/audit/entries/export` e `/api/audit/actors` restano come sono.
- **Le liste sono le tabelle del frontend**: ogni elenco mostrato in una tabella usa la richiesta e la risposta standard
  delle tabelle del frontend (sotto), con pagine, ricerca e ordinamento fatti dal backend.
- **Stabilimento**: tutte le API lo prendono dal token, comprese le tabelle (l'elenco degli utenti, il registro, la sua
  stampa e la sua esportazione) e le tendine (`*-combobox`), che sono POST senza corpo: nessuna richiesta lo contiene.
- **Permessi**: le API degli utenti come `/api/identity/property-users` (`StaffManage`), quelle del registro come
  `GET /api/audit/entries` (`AuditRead`). `user-combobox` chiede `AuditRead`, come il registro che la usa (sezione 6).
  Senza permesso: 403 `auth.forbidden`.
- **Token senza stabilimento**: 403 `account.property_required` per le API `/api/identity/*`
  (`CurrentAccount.RequirePropertyId`), 403 `audit.property_required` per le API `/api/audit/*`
  (`AuditEntryService.RequirePropertyId`), come oggi.
- **JSON** in camelCase, con i nomi esatti di questo documento. I ruoli viaggiano con i codici di `RoleCodes`
  (es. `cashoperator`). Il prefisso del cellulare viaggia senza `+` (es. `39`) e vale `null` quando l'utente non ha il
  cellulare, anche nelle risposte: oggi `phone_country_code` di `identity.access_users` ha il valore predefinito `+39`
  (`AccountConfiguration`, `HasDefaultValue`), quindi un account salvato senza prefisso lo riceve con il `+`.
- **Errori** nel formato Problem Details del backend, con `code`. Senza token valido: 401 `auth.unauthenticated`.

## 1. POST /api/identity/users-list

Tabella della pagina: il frontend la chiama all'apertura e ogni volta che cambiano pagina, numero di righe, ricerca o
ordinamento. Sostituisce, per il frontend, `GET /api/identity/property-users`, che restituisce tutti gli utenti insieme,
senza pagine, ricerca né ordinamento, e con 13 campi per utente.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [],
  "sortField": "email",
  "sortDirection": "Ascending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number | Pagina richiesta, da 1. |
| `pageSize` | number | Righe per pagina: 10, 20, 50 o 100. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Si cerca in email e cellulare (il numero, senza prefisso), senza distinguere maiuscole e minuscole. |
| `filters` | array | Sempre `[]`: la tabella non ha filtri. |
| `sortField` | string | `email`, `phone` o `role`; `""` = ordine del backend (come oggi `property-users`: ruolo, poi email). |
| `sortDirection` | string | `Ascending` o `Descending`. |

Ordinamento per `role`: secondo l'ordine dei ruoli del backend (`Role.SortOrder`), non in ordine alfabetico del codice.

### Risposta

200 nello stile delle tabelle: `total` conta tutti gli utenti dello stabilimento che rispettano la ricerca, `rows` è la
pagina richiesta (al massimo `pageSize` righe).

```json
{
  "total": 2,
  "rows": [
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01",
      "email": "giulia.ferri@example.com",
      "phoneCountryCode": "39",
      "phone": "3401112201",
      "role": "superadmin"
    },
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02",
      "email": "marco.ferri@example.com",
      "phoneCountryCode": "39",
      "phone": "3401112202",
      "role": "cashoperator"
    }
  ]
}
```

Solo questi cinque campi per riga:

| Campo | In `StaffUserDto` | Note |
|---|---|---|
| `idUser` | `id` | UUID pubblico dell'account. |
| `email` | `email` | |
| `phoneCountryCode` | `phoneCountryCode` | Prefisso senza `+` (es. `39`), come lo salva il frontend; `null` se non c'è il cellulare. |
| `phone` | `phone` | Numero senza prefisso; `null` se non c'è. |
| `role` | `role` | Il codice di `RoleCodes`, come oggi: `superadmin`, `admin`, `lifeguard`, `manager`, `cashoperator`, `dailycashoperator`, `websiteadmin`, `user1`, `waiter`, `affiliate`, `advancedlifeguard`, `marketing`. |

Le righe sono tutti gli account dello stabilimento, titolare compreso (`superadmin`): il frontend lo riconosce dal ruolo
e non lo fa modificare né eliminare.

### Errori

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 2. POST /api/identity/create-user

Bottone "Salva" del popup "Aggiungi nuovo utente". È un'**API nuova** con solo i campi del popup:
`POST /api/identity/property-users` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`, che la pagina non ha). Può riusare la stessa logica
(`StaffService.CreateAsync`) con quei campi vuoti: nessun username né nome, la lingua di chi crea l'utente, nessuna
restrizione.

### Richiesta

Lo stabilimento lo prende dal token.

```json
{
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "password": "Sabbia-2026",
  "phoneCountryCode": "39",
  "phone": "340 1112299",
  "phoneIsContact": false,
  "role": "waiter"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `email` | string | Obbligatoria, email valida, al massimo 255 caratteri, non già usata da un altro account. |
| `emailIsContact` | boolean | Casella "Abilita" accanto all'email: come `emailIsContact` di `property-users`, ma salvata (sotto, "Caselle Abilita"). |
| `password` | string | Obbligatoria, come per `property-users`: da 8 a 128 caratteri, non troppo comune (`IdentityRules.NewPassword`). Maiuscola, minuscola e numero li controlla solo il frontend. |
| `phoneCountryCode` | string \| null | Prefisso senza `+` (es. `39`), da 1 a 6 cifre, al massimo 8 caratteri; `null` quando non c'è il cellulare. |
| `phone` | string \| null | Numero senza prefisso, solo cifre e spazi, al massimo 32 caratteri; `null` se non c'è. |
| `phoneIsContact` | boolean | Casella "Abilita" accanto al cellulare: come `phoneIsContact` di `property-users`, ma salvata (sotto, "Caselle Abilita"). |
| `role` | string | Obbligatorio: un codice di `RoleCodes` assegnabile dal pannello (`admin`, `lifeguard`, `manager`, `cashoperator`, `dailycashoperator`, `user1`, `waiter`, `affiliate`, `advancedlifeguard`, `marketing`). |

### Risposta

201 Created con l'utente creato, solo con questi campi (gli stessi che il frontend legge quando apre la modifica di un
utente; mai la password):

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d15",
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "340 1112299",
  "phoneIsContact": false,
  "role": "waiter"
}
```

### Errori

Come `POST /api/identity/property-users`:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `errors[]` sui nomi del corpo: `validation.required` (`email`, `password`, `role`), `validation.email` (`email`), `validation.max_length` con `args.max` (`email` oltre 255, `phone` oltre 32, `phoneCountryCode` oltre 8 caratteri), `validation.password_too_short` / `password_too_long` / `password_too_common` (`password`), `validation.phone` (`phone`), `validation.phone_prefix` (`phoneCountryCode`). Il frontend li mostra sotto i campi. |
| 422 | `account.role_not_assignable` | Ruolo che non si assegna dal pannello (es. `superadmin`). |
| 409 | `account.email_taken` | Esiste già un account con questa email, di qualunque stabilimento. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

Lunghezze e formati sono quelli di `CommonRules` (`BuildingBlocks.Application/Validation/CommonRules.cs`: email 255,
cellulare 32, prefisso 8) e di `CreateStaffUserRequestValidator`
(`Module.Identity/Application/Validators/AccountValidators.cs`).

L'unicità dell'email oggi la controlla solo il servizio (`IAccountRepository.EmailExistsAsync`, su tutti gli account non
eliminati, senza distinguere maiuscole e minuscole): `identity.access_users` non ha un indice unico sull'email.

### Caselle "Abilita"

Oggi il backend non salva `emailIsContact` e `phoneIsContact`: `StaffService.CreateAsync` e `StaffService.UpdateAsync`
(`Module.Identity/Application/Services/StaffService.cs`) le mandano solo nell'evento `StaffContactChannelsChanged`
(`PublishContactChannelsAsync`), che nessun modulo gestisce, e `identity.access_users` non ha colonne per loro. Senza
salvarle, `get-selected-user` non ha valori da restituire e il popup "Modifica utente" non può mostrarle.

Il backend deve:

- salvarle per l'account a ogni `create-user` e `update-user`, così come arrivano (es. due colonne `email_is_contact` e
  `phone_is_contact` di `identity.access_users`, `boolean NOT NULL DEFAULT false`, o dove preferisce il backend);
- restituirle nelle risposte di `create-user`, `get-selected-user` e `update-user`;
- restituire `false` per gli account che non le hanno mai ricevute.

## 3. GET /api/identity/get-selected-user

Apertura del popup "Modifica utente" (matita di una riga): il frontend chiede l'utente e riempie il form. È un'**API
nuova**: oggi il backend non ha una lettura di un solo utente.

### Richiesta

È una GET senza corpo: l'utente va nella query string, lo stabilimento lo prende dal token.

```
GET /api/identity/get-selected-user?idUser=3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03
```

| Parametro | Tipo | Regola |
|---|---|---|
| `idUser` (query string) | string (UUID) | L'utente da modificare (`idUser` di `users-list`). |

### Risposta

200 con l'utente, con gli stessi campi della risposta di `create-user` (mai la password):

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "340 1112203",
  "phoneIsContact": true,
  "role": "manager"
}
```

`emailIsContact` e `phoneIsContact` sono i valori salvati (sezione 2, "Caselle Abilita"): quelli ricevuti l'ultima
volta da `create-user` o `update-user`, `false` se non sono mai arrivati.

### Errori

Gli stessi di `update-user` per l'utente da leggere:

| Status | `code` | Quando |
|---|---|---|
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente ha un ruolo che non si gestisce dal pannello (es. il titolare). |
| 422 | `account.cannot_modify_self` | L'utente collegato chiede se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 4. POST /api/identity/update-user

Bottone "Salva" del popup "Modifica utente". È un'**API nuova** con solo i campi del popup:
`PATCH /api/identity/property-users/{userId}` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`). Può riusare la stessa logica (`StaffService.UpdateAsync`).

### Richiesta

Tutti i campi del popup, cambiati o no; l'utente da modificare è `idUser`. Lo stabilimento lo prende dal token.

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "password": null,
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idUser` | string (UUID) | L'utente da modificare (`idUser` di `users-list`). |
| `email` | string | Obbligatoria, email valida, al massimo 255 caratteri, non già usata da un altro account. |
| `emailIsContact` | boolean | Come in `create-user`: salvata e restituita. |
| `password` | string \| null | `null` = la password resta quella salvata; altrimenti la nuova password, con le regole di `create-user`. |
| `phoneCountryCode` | string \| null | Prefisso senza `+` (es. `41`), da 1 a 6 cifre, al massimo 8 caratteri; `null` quando non c'è il cellulare. |
| `phone` | string \| null | Numero senza prefisso, solo cifre e spazi, al massimo 32 caratteri; `null` = nessun cellulare. |
| `phoneIsContact` | boolean | Come in `create-user`: salvata e restituita. |
| `role` | string | Obbligatorio: un codice di `RoleCodes` assegnabile dal pannello, come in `create-user`. |

Diversamente dal `PATCH`, qui `null` non vuol dire "invariato" (tranne la password): il corpo è sempre tutto il popup,
quindi ogni campo prende il valore ricevuto.

### Sessioni dell'utente modificato

Le sessioni dell'utente modificato si chiudono solo se cambia la password (`password` diversa da `null`) o se `role` è
diverso da quello salvato. Oggi `StaffService.UpdateAsync` le chiude tutte (`SessionFactory.RevokeAllAsync`) appena
`Role` non è `null`, anche se è lo stesso ruolo: siccome `update-user` manda sempre il ruolo, ogni Salva chiuderebbe le
sessioni dell'utente anche quando cambiano solo il cellulare o una casella.

### Risposta

200 con l'utente aggiornato, con gli stessi campi della risposta di `create-user` (mai la password):

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager"
}
```

### Errori

Come `PATCH /api/identity/property-users/{userId}`:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | `errors[]` sui nomi del corpo, come in `create-user` (con `validation.required` su `email` e `role`; le regole della password solo se `password` non è `null`). |
| 422 | `account.role_not_assignable` | Il nuovo ruolo non si assegna dal pannello. |
| 409 | `account.email_taken` | L'email è già di un altro account. |
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente da modificare ha un ruolo che non si gestisce dal pannello (es. il titolare). |
| 422 | `account.cannot_modify_self` | L'utente collegato prova a modificare se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 5. POST /api/audit/entries-list

Tabella della schermata del registro (`/settings/user-management/logs`): il frontend la chiama all'apertura e ogni volta
che cambiano pagina, numero di righe, ricerca, periodo, filtri o ordinamento. È un'**API nuova** per la tabella:
`GET /api/audit/entries` resta com'è. Rispetto a quella, la tabella ha bisogno di filtrare più utenti insieme, di
ordinare ogni colonna, della colonna "ID utente", del periodo letto come giorni nel fuso dell'utente e di righe con solo
i campi mostrati.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-17T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-23T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.map_spots_created", "catalog.season_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `page` | number | Pagina richiesta, da 1. |
| `pageSize` | number | Righe per pagina: 10, 20, 50 o 100. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Si cerca in nome dell'operatore, numero della prenotazione e postazione, senza distinguere maiuscole e minuscole. |
| `filters` | array | Un elemento per campo, `value` sempre un array di stringhe; `[]` = nessun filtro. Campi qui sotto. |
| `sortField` | string | Una delle sei colonne: `bookingNumber`, `userNumber`, `actorName`, `spotLabel`, `actionCode`, `occurredAt`; `""` = le più recenti prima. |
| `sortDirection` | string | `Ascending` o `Descending`. |

I filtri (un campo può mancare; più valori dello stesso campo = uno qualsiasi, campi diversi = tutti insieme):

| `field` | Valori | Significato |
|---|---|---|
| `datetimeFrom` | un valore: il primo giorno del periodo a mezzanotte UTC (es. `2026-10-17T00:00:00.000Z`) | Solo le voci dal primo giorno, compreso (il periodo scelto nella tendina delle date; sotto, "Periodo"). |
| `datetimeTo` | un valore: l'ultimo giorno del periodo alle 23:59:59.999 UTC (es. `2026-10-23T23:59:59.999Z`) | Solo le voci fino all'ultimo giorno, compreso. |
| `actionCode` | uno o più codici di azione di `AuditActionCodes` (es. `catalog.map_spot_updated`), fino a tutti quelli del filtro | Le azioni scelte nel popup Filtri (sotto, "Limiti"). |
| `idUser` | uno o più id di utenti (UUID) | Gli utenti scelti nel popup Filtri, o l'utente del bottone "Log" di una riga di Gestione utenti. Sono gli `actorUserId` delle voci. |

### Periodo

`datetimeFrom` e `datetimeTo` sono **giorni**, non istanti: il frontend manda il giorno scelto con un'ora fissa, senza
conversione di fuso (`dateRangeFilters` in `behaviours/dates/date-periods.ts`). Il backend:

- prende solo la data (`2026-10-17`, `2026-10-23`) e la legge nel fuso dell'header `X-Time-Zone` (nome IANA, es.
  `Europe/Rome`; `Europe/Rome` se l'header manca o non è valido);
- restituisce le voci con `occurredAt` dalla mezzanotte locale del primo giorno (compresa) alla mezzanotte locale del
  giorno dopo l'ultimo (esclusa), quindi con tutto l'ultimo giorno;
- accetta lo stesso giorno in `datetimeFrom` e `datetimeTo` (es. "Oggi").

Oggi `GET /api/audit/entries` tratta `from` e `to` come istanti, con la fine esclusa (`AuditEntryRepository.Filter`:
`OccurredAt >= from`, `OccurredAt < to`): con questi valori, a Roma con l'ora legale, perderebbe le voci tra mezzanotte
e le 2 del primo giorno e mostrerebbe quelle tra mezzanotte e le 2 del giorno dopo l'ultimo.

### Limiti

- `actionCode`: il popup Filtri permette di scegliere tutte le azioni del catalogo (oggi 102, quelle di
  `GET /api/audit/action-codes`), quindi questa API accetta almeno tutti i codici del catalogo. Il tetto di 50 codici di
  `GET /api/audit/entries` (`AuditQueryRules.MaxActionCodes` in `Module.Audit/Application/Validators/AuditValidators.cs`,
  400 oltre 50) qui non vale.
- `search`: al massimo 100 caratteri (`AuditQueryRules.SearchMaxLength`, come `GET /api/audit/entries`); oltre, 400
  `validation.failed` con `errors[]` `{ "field": "search", "code": "validation.max_length", "args": { "max": 100 } }`.
  Il frontend non limita la lunghezza della ricerca: la manda senza spazi ai lati, da 3 caratteri in su.

### Risposta

200 nello stile delle tabelle: `total` conta tutte le voci che rispettano ricerca e filtri, `rows` è la pagina
richiesta (al massimo `pageSize` righe).

```json
{
  "total": 2,
  "rows": [
    {
      "idAuditEntry": "1287",
      "bookingNumber": "1234",
      "userNumber": "123",
      "actorName": "Giulia Ferri",
      "spotLabel": "A1",
      "actionCode": "catalog.map_spots_created",
      "occurredAt": "2026-10-23T11:11:00.000Z"
    },
    {
      "idAuditEntry": "1265",
      "bookingNumber": "2741",
      "userNumber": "123",
      "actorName": "Giulia Ferri",
      "spotLabel": "D2",
      "actionCode": "catalog.season_updated",
      "occurredAt": "2026-10-17T14:05:00.000Z"
    }
  ]
}
```

Le voci sono le stesse di `GET /api/audit/entries`, con solo questi sette campi per riga:

| Campo | In `AuditEntryDto` | Note |
|---|---|---|
| `idAuditEntry` | `id` | Identificativo della voce (il numero `bigint` di `audit.trail_entries`), come testo. Non si mostra. |
| `bookingNumber` | `bookingNumber` | Colonna "ID prenotazione"; `null` se la voce non riguarda una prenotazione. |
| `userNumber` | — (nuovo) | Colonna "ID utente": codice breve e leggibile dell'utente che ha fatto l'azione; `null` se non c'è. Origine, formato e ordinamento sono ancora da decidere: oggi né `identity.access_users` né `audit.trail_entries` hanno un campo così. |
| `actorName` | `actorName` | Colonna "Nome completo". |
| `spotLabel` | `spotLabel` | Colonna "Postazione"; `null` se la voce non riguarda una postazione. |
| `actionCode` | `actionCode` | Colonna "Azione": il codice (`<modulo>.<azione>`), che il frontend traduce. |
| `occurredAt` | `occurredAt` | Colonna "Data e ora", ISO 8601 UTC. |

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`: `search` oltre 100 caratteri (`validation.max_length`); giorno di `datetimeTo` prima di quello di `datetimeFrom` (`validation.end_before_start`). |
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 6. POST /api/identity/user-combobox

La tendina degli utenti del frontend: **una sola per tutta l'app**, usata ovunque serva scegliere un utente (salvo
diversa indicazione). In questa pagina è la tendina "Utente" del popup Filtri della schermata del registro, chiamata
ogni volta che il popup si apre (tranne quando la schermata è aperta dal bottone "Log" di una riga e quell'utente è
ancora nel filtro: allora la tendina contiene solo lui). La schermata aperta dal bottone "Log" di una riga la chiama
anche una volta all'apertura, per mostrare il nome dell'utente al posto della sua email. È un'**API nuova** nello
standard delle tendine del frontend: `GET /api/audit/actors` resta com'è.

Quali utenti restituire lo decide il backend. Gli `id` devono essere quelli che filtrano il registro: il frontend li
rimanda nel filtro `idUser` di `entries-list`, `print-log` ed `export-log`.

Permesso: `AuditRead`, come la schermata del registro che la usa: chi apre il registro deve poter scegliere i suoi
utenti. Oggi `AuditRead` e `StaffManage` li hanno gli stessi ruoli, `superadmin` e `admin`
(`Shared/Security/RolePermissions.cs`).

### Richiesta

Come tutte le tendine del frontend, POST senza corpo: lo stabilimento lo prende il backend dal token.

### Risposta

200 con un array (senza un oggetto che lo contiene). L'ordine non conta: il frontend mette le voci in ordine alfabetico
di `value` nella lingua dell'app.

```json
[
  { "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "value": "Giulia Ferri" },
  { "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02", "value": "Marco Bianchi" }
]
```

| Campo | Tipo | Note |
|---|---|---|
| `id` | string (UUID) | L'utente (lo stesso id di `idUser` nelle altre API della pagina). |
| `value` | string | Il nome da mostrare nella tendina. |

### Errori

| Status | `code` | Quando |
|---|---|---|
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 7. POST /api/audit/print-log

Bottone "Stampa" della schermata del registro (`/settings/user-management/logs`). È un'**API nuova**: oggi il backend
ha l'esportazione (`GET /api/audit/entries/export`, csv o xlsx) ma non un documento da stampare.

### Richiesta

Gli stessi filtri che la tabella del registro sta mostrando, senza pagina e righe per pagina: il documento contiene
**tutte** le voci che li rispettano, nell'ordine della tabella.

```json
{
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.sector_updated", "catalog.map_spot_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca, al massimo 100 caratteri. Si cerca in nome dell'operatore, numero della prenotazione e postazione. |
| `filters` | array | Un elemento per campo, `value` sempre un array di stringhe, `[]` se non ci sono filtri. `datetimeFrom` / `datetimeTo`: il periodo, giorni come in `entries-list` (sezione 5, "Periodo"). `actionCode`: i codici delle azioni scelte (una qualsiasi), fino a tutti quelli del filtro. `idUser`: gli utenti scelti (uno qualsiasi). |
| `sortField` | string | `bookingNumber`, `userNumber`, `actorName`, `spotLabel`, `actionCode` o `occurredAt`; `""` = le più recenti prima. |
| `sortDirection` | string | `Ascending` o `Descending`. |

### Risposta

200 con il documento da stampare come file (binario, non JSON). Il formato lo decide il backend (es. un PDF).

| Header | Esempio | Note |
|---|---|---|
| `Content-Type` | `application/pdf` | Tipo del file. |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.pdf"` | Nome del file; il frontend lo usa per salvarlo. Per nomi con lettere accentate anche `filename*=UTF-8''…`. |

Se il backend sta su un dominio diverso da quello del frontend, deve esporre l'header con
`Access-Control-Expose-Headers: Content-Disposition`.

### Lingua, fuso, troppe voci e registro

Valgono per la stampa e per l'esportazione (sezione 8):

- **Lingua**: titoli delle colonne e nomi delle azioni nella lingua dell'header `Accept-Language` (la lingua scelta
  nell'app: `it`, `en`, `fr`, `es`, `de`, `zh`, `ar`); se manca una traduzione l'italiano, poi il codice dell'azione,
  come fa `AuditExportWriter`. Oggi `GET /api/audit/entries/export` usa invece la lingua dell'account di chi esporta
  (`AuditExport.LanguageAsync`).
- **Fuso**: "Data e ora" nel fuso dell'header `X-Time-Zone` (`Europe/Rome` se manca o non è valido), come la tabella
  che la mostra nell'ora del dispositivo. Oggi l'esportazione scrive sempre l'ora italiana (`AuditExportWriter.Time`,
  `ItalianTime`). Il periodo dei filtri segue la sezione 5.
- **Troppe voci**: oltre 50.000 voci (`AuditQueryRules.ExportMaxRows`) nessun file e 422 `audit.export_too_large` con
  `args.max` (50000) e `args.total` (le voci che rispettano i filtri), come `AuditExport.LoadAsync`.
- **Registro**: ogni stampa e ogni esportazione riuscita scrive nel registro dello stabilimento
  `audit.entries_exported` (categoria `Operator`, come `AuditEntryService.ExportAsync`), con nei metadati il formato del
  file: il frontend ha già testo ("Registro esportato") e icona di questo codice. Un codice diverso per la stampa va
  concordato prima, perché il frontend deve aggiungerne testi e icona.

### Errori

Errori in JSON (Problem Details), anche se la risposta giusta è un file:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`, come in `entries-list`: `search` oltre 100 caratteri, giorno di `datetimeTo` prima di quello di `datetimeFrom`. |
| 422 | `audit.export_too_large` | Più di 50.000 voci: nessun file; `args.max` e `args.total`. |
| 403 | `auth.forbidden` | Utente senza il permesso di leggere il registro (`AuditRead`, come `GET /api/audit/entries`). |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 8. POST /api/audit/export-log

Bottone "Esporta" della schermata del registro. È un'**API nuova**: `GET /api/audit/entries/export` resta com'è, ma
filtra un solo utente, ordina solo per data e riceve i filtri in un altro formato. Questa usa gli stessi filtri della
tabella (`entries-list`) e della stampa (`print-log`).

### Richiesta

Lo stesso corpo di `print-log` (vedi la sezione 7 per le regole di ogni campo): gli stessi filtri che la tabella sta
mostrando, senza pagina e righe per pagina, quindi **tutte** le voci che li rispettano, nell'ordine della tabella.

```json
{
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["catalog.sector_updated"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "actorName",
  "sortDirection": "Ascending"
}
```

### Risposta

200 con un file Excel (`xlsx`), binario, non JSON: una riga per voce, con le colonne della tabella (ID prenotazione,
ID utente, Nome completo, Postazione, Azione, Data e ora). Lingua, fuso, limite di voci e registro come per la stampa
(sezione 7, "Lingua, fuso, troppe voci e registro").

| Header | Esempio | Note |
|---|---|---|
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` | File Excel. |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.xlsx"` | Nome del file; il frontend lo usa per salvarlo. Per nomi con lettere accentate anche `filename*=UTF-8''…`. |

Se il backend sta su un dominio diverso da quello del frontend, deve esporre l'header con
`Access-Control-Expose-Headers: Content-Disposition`.

### Errori

Errori in JSON (Problem Details), come per `print-log`:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Con `errors[]`, come in `entries-list`: `search` oltre 100 caratteri, giorno di `datetimeTo` prima di quello di `datetimeFrom`. |
| 422 | `audit.export_too_large` | Più di 50.000 voci: nessun file; `args.max` e `args.total`. |
| 403 | `auth.forbidden` | Utente senza il permesso `AuditRead`. |
| 403 | `audit.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 9. Codici di azione del registro per i moduli futuri

La schermata del registro (`/settings/user-management/logs`) traduce e filtra le azioni con i codici di
`AuditActionCodes` (`Shared/Contracts/Audit/AuditActionCodes.cs`), che il frontend conosce tutti. Il mockup della
pagina ha anche azioni dei moduli che oggi non hanno API, quindi non scrivono niente nel registro: il frontend le ha già
tradotte nelle 7 lingue, ma le ha tolte dal filtro "Azione" finché il backend non le registra.

Quando quei moduli registreranno le loro azioni, devono usare **esattamente questi codici** (categoria `Operator`, come
le azioni degli operatori di oggi), così il frontend li mostra senza altre modifiche. Se un'azione non serve o il
backend ne vuole un'altra, va deciso insieme: il frontend allinea il suo elenco.

| Codice | Testo del frontend (italiano) | Modulo |
|---|---|---|
| `booking.checked_in` | Check-in | Prenotazioni (`Module.Booking`) |
| `booking.checked_out` | Check-out | Prenotazioni (`Module.Booking`) |
| `booking.arrival_reported` | Ingresso segnalato | Prenotazioni (`Module.Booking`) |
| `booking.guest_added` | Ospite aggiunto | Prenotazioni (`Module.Booking`) |
| `booking.dates_changed` | Date prenotazione modificate | Prenotazioni (`Module.Booking`) |
| `booking.agreement_added` | Convenzione aggiunta | Prenotazioni (`Module.Booking`) |
| `booking.repeated` | Prenotazione ripetuta | Prenotazioni (`Module.Booking`) |
| `booking.spot_added` | Postazione aggiunta | Prenotazioni (`Module.Booking`) |
| `booking.absences_added` | Assenze programmate aggiunte | Prenotazioni (`Module.Booking`) |
| `booking.moved` | Prenotazione spostata | Prenotazioni (`Module.Booking`) |
| `booking.file_attached` | File allegato | Prenotazioni (`Module.Booking`) |
| `booking.extra_added` | Extra aggiunto | Prenotazioni (`Module.Booking`) |
| `booking.renewed` | Prenotazione rinnovata | Prenotazioni (`Module.Booking`) |
| `booking.cancelled` | Prenotazione cancellata | Prenotazioni (`Module.Booking`) |
| `payment.deposit_added` | Acconto aggiunto | Pagamenti (`Module.Payment`) |
| `payment.balance_added` | Saldo aggiunto | Pagamenti (`Module.Payment`) |
| `payment.bill_split` | Conto diviso | Pagamenti (`Module.Payment`) |
| `payment.discount_code_applied` | Codice sconto applicato | Pagamenti (`Module.Payment`) |
| `payment.discount_applied` | Sconto applicato | Pagamenti (`Module.Payment`) |
| `payment.increase_added` | Incremento aggiunto | Pagamenti (`Module.Payment`) |
| `cash.pre_bill_printed` | Preconto stampato | Cassa e stampa fiscale (`Module.FiscalPrint` / `Module.Fiskaly`) |
| `cash.receipt_printed` | Scontrino stampato | Cassa e stampa fiscale (`Module.FiscalPrint` / `Module.Fiskaly`) |
| `order.marked_done` | Ordine segnalato come evaso | Ordini |
| `order.marked_not_done` | Ordine segnalato come non evaso | Ordini |
| `database.bookings_trashed` | Dati database eliminati | Pagina Database (prenotazioni nel cestino) |
| `database.bookings_restored` | Dati database ripristinati | Pagina Database (prenotazioni nel cestino) |
| `database.trash_emptied` | Cestino database svuotato | Pagina Database (prenotazioni nel cestino) |
| `table_booking.created` | Prenotazione servizio tavoli aggiunta | Prenotazioni del servizio tavoli |
| `table_booking.deleted` | Prenotazione servizio tavoli eliminata | Prenotazioni del servizio tavoli |
| `table_booking.updated` | Prenotazione servizio tavoli modificata | Prenotazioni del servizio tavoli |

Per ognuna, `bookingNumber` e `spotLabel` della voce sono valorizzati quando l'azione riguarda una prenotazione o una
postazione, come per le altre voci (sezione 5).

## API esistenti usate così come sono

Questa API del backend la usa già il frontend e **non va cambiata**: è qui solo perché chi lavora sulla pagina sappia
cosa il frontend si aspetta.

| Metodo | Rotta | Uso nella pagina |
|---|---|---|
| DELETE | `/api/identity/property-users/{id}` | Cestino di una riga, dopo la conferma: `id` = `idUser` di `users-list`; nessun corpo, lo stabilimento viene dal token. |

Il frontend si aspetta 204 senza corpo e questi errori, che mostra nel popup "Operazione non riuscita":

| Status | `code` | Quando |
|---|---|---|
| 404 | `account.not_found` | L'utente non è dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente ha un ruolo che non si gestisce dal pannello (es. il titolare). |
| 422 | `account.cannot_modify_self` | L'utente collegato prova a eliminare se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 403 | `account.property_required` | Token senza stabilimento. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## Verifica

- `users-list`: `rows` ha al massimo `pageSize` righe e `total` conta tutte quelle che rispettano la ricerca; la ricerca
  trova un utente da una parte della sua email o del suo cellulare, con maiuscole o minuscole; i tre ordinamenti
  funzionano nei due versi e `sortField` vuoto dà l'ordine del backend; ogni riga ha solo i cinque campi, con `role`
  come codice di `RoleCodes` e `phoneCountryCode` senza `+` (`null` senza cellulare).
- `create-user` crea l'utente nello stabilimento del token, salva le due caselle "Abilita", risponde 201 con i sette
  campi e ha gli stessi controlli ed errori di `property-users`; senza `role` dà 400 `validation.required` sul campo
  `role`; un'email di 256 caratteri dà 400 `validation.max_length` su `email`.
- `get-selected-user` restituisce l'utente `idUser` dello stabilimento del token con i sette campi, gli stessi che
  `update-user` ha appena salvato, caselle "Abilita" comprese.
- `update-user` cambia solo l'utente `idUser` dello stabilimento del token, con tutti i campi ricevuti (`password`
  `null` la lascia com'è), e risponde 200 con i sette campi; con lo stesso ruolo e `password` `null` le sessioni
  dell'utente restano aperte, con un ruolo diverso o una password nuova si chiudono.
- `entries-list` filtra più utenti e più azioni insieme, accetta `actionCode` con tutti i codici del catalogo (più di
  50), ordina tutte e sei le colonne nei due versi e restituisce righe con solo i sette campi, `userNumber` compreso;
  con `X-Time-Zone: Europe/Rome` e un solo giorno restituisce le voci da mezzanotte a mezzanotte ora italiana; `search`
  di 101 caratteri dà 400 `validation.max_length`.
- `user-combobox` restituisce `{ id, value }` per ogni utente da mostrare; gli `id` filtrano `entries-list`; senza
  `AuditRead` risponde 403 `auth.forbidden`.
- `print-log` restituisce un file con tutte le voci che rispettano ricerca e filtri, nell'ordine chiesto, e il nome in
  `Content-Disposition`.
- `export-log` accetta lo stesso corpo di `print-log` e restituisce un file `xlsx` con tutte le voci filtrate,
  nell'ordine chiesto, e il nome in `Content-Disposition`.
- `print-log` ed `export-log`: testi nella lingua di `Accept-Language`, "Data e ora" nel fuso di `X-Time-Zone`, oltre
  50.000 voci 422 `audit.export_too_large` senza file; ogni file scrive `audit.entries_exported` nel registro.
- I codici della sezione 9, quando i loro moduli registrano le azioni, sono quelli della tabella, con la categoria
  `Operator`, e `GET /api/audit/action-codes` li restituisce.
- Ovunque: lo stabilimento è quello del token, nessuna richiesta lo contiene; un token senza stabilimento dà 403
  `account.property_required` (`/api/identity/*`) o `audit.property_required` (`/api/audit/*`).
