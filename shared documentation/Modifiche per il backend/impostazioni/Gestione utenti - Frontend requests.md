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

## Regole comuni

- **Moduli**: le API `/api/identity/*` stanno in Identity (`Module.Identity`), accanto a `/api/identity/property-users`
  (`Endpoints/PropertyEndpoints.cs`, `StaffService`), con i dati di `StaffUserDto`; le API `/api/audit/*` stanno in Audit
  (`Module.Audit`), accanto a `/api/audit/entries` (`Endpoints/PropertyAuditEndpoints.cs`, `AuditEntryService`), con le
  voci di `AuditEntryDto`.
- **Le API esistenti non cambiano**: `/api/identity/property-users` con le sue sotto-rotte, `/api/audit/entries`,
  `/api/audit/entries/export` e `/api/audit/actors` restano come sono.
- **Le liste sono le tabelle del frontend**: ogni elenco mostrato in una tabella usa la richiesta e la risposta standard
  delle tabelle del frontend (sotto), con pagine, ricerca e ordinamento fatti dal backend.
- **Stabilimento**: le richieste con la ricerca e i filtri di una tabella (l'elenco degli utenti, il registro, la sua
  stampa e la sua esportazione) e quelle delle tendine (`*-combobox`) hanno `idProperty`, che deve essere quello del
  token; tutte le altre API lo prendono dal token e non lo ricevono.
- **Permessi**: le API degli utenti come `/api/identity/property-users` (`StaffManage`), quelle del registro come
  `GET /api/audit/entries` (`AuditRead`). Senza permesso: 403 `auth.forbidden`.
- **JSON** in camelCase, con i nomi esatti di questo documento. I ruoli viaggiano con i codici di `RoleCodes`
  (es. `cashoperator`), il prefisso del cellulare senza `+` (es. `39`).
- **Errori** nel formato Problem Details del backend, con `code`. Senza token valido: 401 `auth.unauthenticated`.

## 1. POST /api/identity/users-list

Tabella della pagina: il frontend la chiama all'apertura e ogni volta che cambiano pagina, numero di righe, ricerca o
ordinamento. Sostituisce, per il frontend, `GET /api/identity/property-users`, che restituisce tutti gli utenti insieme,
senza pagine, ricerca né ordinamento, e con 13 campi per utente.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "idProperty": "7ddc54a7-233b-4f4b-a365-68be583e661f",
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
| `idProperty` | string (UUID) | Lo stabilimento della tabella: deve essere quello dell'utente del token, altrimenti 403 `auth.forbidden`. |
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
| 403 | `auth.forbidden` | `idProperty` assente, sconosciuto o non dell'utente, oppure utente senza il permesso `StaffManage`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 2. POST /api/identity/create-user

Bottone "Salva" del popup "Aggiungi nuovo utente". È un'**API nuova** con solo i campi del popup:
`POST /api/identity/property-users` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`, che la pagina non ha). Può riusare la stessa logica
(`StaffService.CreateAsync`) con quei campi vuoti: nessun username né nome, la lingua di chi crea l'utente, nessuna
restrizione.

### Richiesta

Lo stabilimento lo prende dal token: il corpo non ha `idProperty`.

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
| `email` | string | Obbligatoria, email valida. |
| `emailIsContact` | boolean | Casella "Abilita" accanto all'email: come `emailIsContact` di `property-users`. |
| `password` | string | Obbligatoria, come per `property-users` (da 8 a 128 caratteri, non troppo comune). |
| `phoneCountryCode` | string \| null | Prefisso senza `+` (es. `39`); `null` quando non c'è il cellulare. |
| `phone` | string \| null | Numero senza prefisso, solo cifre e spazi, al massimo 32 caratteri; `null` se non c'è. |
| `phoneIsContact` | boolean | Casella "Abilita" accanto al cellulare: come `phoneIsContact` di `property-users`. |
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
| 400 | `validation.failed` | `errors[]` sui nomi del corpo: `validation.required` (`email`, `password`, `role`), `validation.email`, `validation.password_too_short` / `password_too_long` / `password_too_common`, `validation.phone` (`phone`), `validation.phone_prefix` (`phoneCountryCode`). Il frontend li mostra sotto i campi. |
| 422 | `account.role_not_assignable` | Ruolo che non si assegna dal pannello (es. `superadmin`). |
| 409 | `account.email_taken` | Esiste già un account con questa email. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

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

`emailIsContact` e `phoneIsContact` sono i valori ricevuti l'ultima volta da `create-user` o `update-user`.

### Errori

Gli stessi di `update-user` per l'utente da leggere:

| Status | `code` | Quando |
|---|---|---|
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente ha un ruolo che non si gestisce dal pannello (es. il titolare). |
| 422 | `account.cannot_modify_self` | L'utente collegato chiede se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 4. POST /api/identity/update-user

Bottone "Salva" del popup "Modifica utente". È un'**API nuova** con solo i campi del popup:
`PATCH /api/identity/property-users/{userId}` resta com'è (ha in più `username`, `firstName`, `lastName`, `language`,
`isResourceScopeRestricted` e `resourceScopes`). Può riusare la stessa logica (`StaffService.UpdateAsync`).

### Richiesta

Tutti i campi del popup, cambiati o no; l'utente da modificare è `idUser`. Lo stabilimento lo prende dal token: il
corpo non ha `idProperty`.

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
| `email` | string | Obbligatoria, email valida. |
| `emailIsContact` | boolean | Come in `create-user`. |
| `password` | string \| null | `null` = la password resta quella salvata; altrimenti la nuova password, con le regole di `create-user`. |
| `phoneCountryCode` | string \| null | Prefisso senza `+` (es. `41`); `null` quando non c'è il cellulare. |
| `phone` | string \| null | Numero senza prefisso, solo cifre e spazi, al massimo 32 caratteri; `null` = nessun cellulare. |
| `phoneIsContact` | boolean | Come in `create-user`. |
| `role` | string | Obbligatorio: un codice di `RoleCodes` assegnabile dal pannello, come in `create-user`. |

Diversamente dal `PATCH`, qui `null` non vuol dire "invariato" (tranne la password): il corpo è sempre tutto il popup,
quindi ogni campo prende il valore ricevuto.

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
| 400 | `validation.failed` | `errors[]` sui nomi del corpo, come in `create-user` (con `validation.required` su `email` e `role`). |
| 422 | `account.role_not_assignable` | Il nuovo ruolo non si assegna dal pannello. |
| 409 | `account.email_taken` | L'email è già di un altro account. |
| 404 | `account.not_found` | `idUser` non è un utente dello stabilimento. |
| 403 | `account.role_not_assignable` | L'utente da modificare ha un ruolo che non si gestisce dal pannello (es. il titolare). |
| 422 | `account.cannot_modify_self` | L'utente collegato prova a modificare se stesso. |
| 403 | `auth.forbidden` | Utente senza il permesso `StaffManage`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 5. POST /api/audit/entries-list

Tabella della schermata del registro (`/settings/user-management/logs`): il frontend la chiama all'apertura e ogni volta
che cambiano pagina, numero di righe, ricerca, periodo, filtri o ordinamento. È un'**API nuova** per la tabella:
`GET /api/audit/entries` resta com'è. Rispetto a quella, la tabella ha bisogno di filtrare più utenti insieme, di
ordinare ogni colonna, della colonna "ID utente" e di righe con solo i campi mostrati.

### Richiesta

Il corpo è la richiesta standard delle tabelle:

```json
{
  "idProperty": "7ddc54a7-233b-4f4b-a365-68be583e661f",
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-17T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-23T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["booking.spot_added", "booking.dates_changed"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idProperty` | string (UUID) | Lo stabilimento della tabella: deve essere quello dell'utente del token, altrimenti 403. |
| `page` | number | Pagina richiesta, da 1. |
| `pageSize` | number | Righe per pagina: 10, 20, 50 o 100. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Si cerca in nome dell'operatore, numero della prenotazione e postazione, senza distinguere maiuscole e minuscole. |
| `filters` | array | Un elemento per campo, `value` sempre un array di stringhe; `[]` = nessun filtro. Campi qui sotto. |
| `sortField` | string | Una delle sei colonne: `bookingNumber`, `userNumber`, `actorName`, `spotLabel`, `actionCode`, `occurredAt`; `""` = le più recenti prima. |
| `sortDirection` | string | `Ascending` o `Descending`. |

I filtri (un campo può mancare; più valori dello stesso campo = uno qualsiasi, campi diversi = tutti insieme):

| `field` | Valori | Significato |
|---|---|---|
| `datetimeFrom` | un valore, data e ora ISO 8601 UTC | Solo le voci da quel momento (il periodo scelto nella tendina delle date). |
| `datetimeTo` | un valore, data e ora ISO 8601 UTC | Solo le voci fino a quel momento. |
| `actionCode` | uno o più codici di azione (es. `booking.moved`) | Le azioni scelte nel popup Filtri. |
| `idUser` | uno o più id di utenti (UUID) | Gli utenti scelti nel popup Filtri, o l'utente del bottone "Log" di una riga di Gestione utenti. Sono gli `actorUserId` delle voci. |

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
      "actionCode": "booking.spot_added",
      "occurredAt": "2026-10-23T11:11:00.000Z"
    },
    {
      "idAuditEntry": "1265",
      "bookingNumber": "2741",
      "userNumber": "123",
      "actorName": "Giulia Ferri",
      "spotLabel": "D2",
      "actionCode": "booking.dates_changed",
      "occurredAt": "2026-10-17T14:05:00.000Z"
    }
  ]
}
```

Le voci sono le stesse di `GET /api/audit/entries`, con solo questi sette campi per riga:

| Campo | In `AuditEntryDto` | Note |
|---|---|---|
| `idAuditEntry` | `id` | Identificativo della voce, come testo. Non si mostra. |
| `bookingNumber` | `bookingNumber` | Colonna "ID prenotazione"; `null` se la voce non riguarda una prenotazione. |
| `userNumber` | — (nuovo) | Colonna "ID utente": codice breve e leggibile dell'utente che ha fatto l'azione; `null` se non c'è. |
| `actorName` | `actorName` | Colonna "Nome completo". |
| `spotLabel` | `spotLabel` | Colonna "Postazione"; `null` se la voce non riguarda una postazione. |
| `actionCode` | `actionCode` | Colonna "Azione": il codice (`<modulo>.<azione>`), che il frontend traduce. |
| `occurredAt` | `occurredAt` | Colonna "Data e ora", ISO 8601 UTC. |

### Errori

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Filtri non validi (es. `datetimeTo` prima di `datetimeFrom`), con `errors[]`. |
| 403 | `auth.forbidden` | `idProperty` non dell'utente, oppure utente senza il permesso `AuditRead`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 6. POST /api/identity/user-combobox

La tendina degli utenti del frontend: **una sola per tutta l'app**, usata ovunque serva scegliere un utente (salvo
diversa indicazione). In questa pagina è la tendina "Utente" del popup Filtri della schermata del registro, chiamata
ogni volta che il popup si apre. È un'**API nuova** nello standard delle tendine del frontend: `GET /api/audit/actors`
resta com'è.

Quali utenti restituire lo decide il backend. Gli `id` devono essere quelli che filtrano il registro: il frontend li
rimanda nel filtro `idUser` di `entries-list` e `print-log`.

### Richiesta

Come tutte le tendine del frontend, il solo stabilimento:

```json
{
  "idProperty": "7ddc54a7-233b-4f4b-a365-68be583e661f"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idProperty` | string (UUID) | Deve essere quello dell'utente del token, altrimenti 403. |

### Risposta

200 con un array (senza un oggetto che lo contiene), nell'ordine in cui mostrarlo:

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
| 403 | `auth.forbidden` | `idProperty` non dell'utente, oppure utente senza il permesso di vedere gli utenti. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## 7. POST /api/audit/print-log

Bottone "Stampa" della schermata del registro (`/settings/user-management/logs`). È un'**API nuova**: oggi il backend
ha l'esportazione (`GET /api/audit/entries/export`, csv o xlsx) ma non un documento da stampare.

### Richiesta

Gli stessi filtri che la tabella del registro sta mostrando, senza pagina e righe per pagina: il documento contiene
**tutte** le voci che li rispettano, nell'ordine della tabella.

```json
{
  "idProperty": "7ddc54a7-233b-4f4b-a365-68be583e661f",
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["booking.checked_in", "booking.moved"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Campo | Tipo | Regola |
|---|---|---|
| `idProperty` | string (UUID) | Lo stabilimento della tabella: deve essere quello dell'utente del token, altrimenti 403. |
| `search` | string | Testo della barra di ricerca, `""` = nessuna ricerca. Si cerca in nome dell'operatore, numero della prenotazione e postazione. |
| `filters` | array | Un elemento per campo, `value` sempre un array di stringhe, `[]` se non ci sono filtri. `datetimeFrom` / `datetimeTo`: il periodo (date e ore ISO 8601 UTC, un valore ciascuno). `actionCode`: i codici delle azioni scelte (una qualsiasi). `idUser`: gli utenti scelti (uno qualsiasi). |
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

### Errori

Errori in JSON (Problem Details), anche se la risposta giusta è un file:

| Status | `code` | Quando |
|---|---|---|
| 400 | `validation.failed` | Filtri non validi (es. `datetimeTo` prima di `datetimeFrom`), con `errors[]`. |
| 403 | `auth.forbidden` | `idProperty` non dell'utente, oppure utente senza il permesso di leggere il registro (`AuditRead`, come `GET /api/audit/entries`). |
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
  "idProperty": "7ddc54a7-233b-4f4b-a365-68be583e661f",
  "search": "ferri",
  "filters": [
    { "field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"] },
    { "field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"] },
    { "field": "actionCode", "value": ["booking.checked_in"] },
    { "field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"] }
  ],
  "sortField": "actorName",
  "sortDirection": "Ascending"
}
```

### Risposta

200 con un file Excel (`xlsx`), binario, non JSON: una riga per voce, con le colonne della tabella (ID prenotazione,
ID utente, Nome completo, Postazione, Azione, Data e ora).

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
| 400 | `validation.failed` | Filtri non validi (es. `datetimeTo` prima di `datetimeFrom`), con `errors[]`. |
| 403 | `auth.forbidden` | `idProperty` non dell'utente, oppure utente senza il permesso `AuditRead`. |
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

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
| 401 | `auth.unauthenticated` | Token assente, scaduto o non valido. |

## Verifica

- `users-list`: `rows` ha al massimo `pageSize` righe e `total` conta tutte quelle che rispettano la ricerca; la ricerca
  trova un utente da una parte della sua email o del suo cellulare, con maiuscole o minuscole; i tre ordinamenti
  funzionano nei due versi e `sortField` vuoto dà l'ordine del backend; ogni riga ha solo i cinque campi, con `role`
  come codice di `RoleCodes`.
- `create-user` crea l'utente nello stabilimento del token, risponde 201 con i sette campi e ha gli stessi controlli ed
  errori di `property-users`; senza `role` dà 400 `validation.required` sul campo `role`.
- `get-selected-user` restituisce l'utente `idUser` dello stabilimento del token con i sette campi, gli stessi che
  `update-user` ha appena salvato.
- `update-user` cambia solo l'utente `idUser` dello stabilimento del token, con tutti i campi ricevuti (`password`
  `null` la lascia com'è), e risponde 200 con i sette campi.
- `entries-list` filtra più utenti e più azioni insieme, ordina tutte e sei le colonne nei due versi e restituisce
  righe con solo i sette campi, `userNumber` compreso.
- `user-combobox` restituisce `{ id, value }` per ogni utente da mostrare; gli `id` filtrano `entries-list`.
- `print-log` restituisce un file con tutte le voci che rispettano ricerca e filtri, nell'ordine chiesto, e il nome in
  `Content-Disposition`.
- `export-log` accetta lo stesso corpo di `print-log` e restituisce un file `xlsx` con tutte le voci filtrate,
  nell'ordine chiesto, e il nome in `Content-Disposition`.
- Ovunque: un `idProperty` diverso da quello del token dà 403; nessuna API senza `idProperty` lo accetta.
