# API Summer Booking

Elenco di tutte le API usate dal frontend, divise per sezione: rotta, payload e risposta, in JSON e in tabella con un esempio per ogni property. I dati degli esempi sono inventati. Errori, header e regole comuni sono in `API_CONTRACTS.docx`.

Ogni chiamata, tranne `identity/auth/login`, `identity/auth/refresh` e `identity/auth/logout`, porta l'header `Authorization: Bearer <accessToken>`.

## /auth

### POST `/api/identity/auth/login`

Accesso.

#### Payload

```json
{
  "username": "mario.rossi@example.com",
  "password": "password-digitata",
  "remember": true,
  "app": "hub"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `username` | string | `mario.rossi@example.com` |
| `password` | string | `password-digitata` |
| `remember` | boolean | `true` |
| `app` | string | `hub` |

#### Response

Stato: 200 OK.

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.firma-di-esempio",
  "expiresIn": 604800,
  "user": {
    "id": "0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c",
    "email": "mario.rossi@example.com",
    "role": "manager",
    "propertyId": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
  }
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `accessToken` | string | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.firma-di-esempio` |
| `expiresIn` | number (intero) | `604800` |
| `user` | AuthUser (oggetto) | `{"id": "0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c", "email": "mario.rossi@example.com", "role": "manager", "propertyId": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"}` |
| `user.id` | string (UUID) | `0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c` |
| `user.email` | string | `mario.rossi@example.com` |
| `user.role` | string | `manager` |
| `user.propertyId` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `Set-Cookie (header)` | cookie HttpOnly del refresh token | `sb_rt=<refresh-token>; HttpOnly; Secure; SameSite=Strict; Path=/api/identity; Max-Age=2592000` |

### POST `/api/identity/auth/refresh`

Rinnovo del token.

#### Payload

Nessun corpo: la richiesta porta solo il cookie del refresh token, inviato dal browser in automatico.

#### Response

Stato: 200 OK.

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.firma-di-esempio",
  "expiresIn": 604800,
  "user": {
    "id": "0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c",
    "email": "mario.rossi@example.com",
    "role": "manager",
    "propertyId": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
  }
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `accessToken` | string | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0In0.firma-di-esempio` |
| `expiresIn` | number (intero) | `604800` |
| `user` | AuthUser (oggetto) | `{"id": "0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c", "email": "mario.rossi@example.com", "role": "manager", "propertyId": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"}` |
| `user.id` | string (UUID) | `0f3c2a1b-7d6e-4f5a-9b8c-1d2e3f4a5b6c` |
| `user.email` | string | `mario.rossi@example.com` |
| `user.role` | string | `manager` |
| `user.propertyId` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `Set-Cookie (header)` | cookie HttpOnly del refresh token | `sb_rt=<refresh-token>; HttpOnly; Secure; SameSite=Strict; Path=/api/identity; Max-Age=2592000` |

### POST `/api/identity/auth/logout`

Uscita.

#### Payload

Nessun corpo: la richiesta porta solo il cookie del refresh token, inviato dal browser in automatico.

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

| Property | Tipo | Esempio |
|---|---|---|
| `Set-Cookie (header)` | cancella il cookie del refresh token | `sb_rt=; HttpOnly; Secure; SameSite=Strict; Path=/api/identity; Max-Age=0` |

## /identity

### POST `/api/identity/users-list`

Gestione utenti: elenco.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "rossi",
  "filters": [
    {
      "field": "role",
      "value": [
        "Admin",
        "Manager"
      ]
    }
  ],
  "sortField": "lastName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `rossi` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "role", "value": ["Admin", "Manager"]}]` |
| `filters[].field` | string | `role` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["Admin", "Manager"]` |
| `sortField` | string | `lastName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01",
      "email": "giulia.ferri@example.com",
      "phoneCountryCode": "+39",
      "phone": "3401112201",
      "role": "SuperAdmin"
    },
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02",
      "email": "marco.bianchi@example.com",
      "phoneCountryCode": "+39",
      "phone": "3401112202",
      "role": "Admin"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | UserItem[] (array di oggetti) | `[{"idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "email": "giulia.ferri@example.com", "phoneCountryCode": "+39", "phone": "3401112201", "role": "SuperAdmin"}, {"idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02", "email": "marco.bianchi@example.com", "phoneCountryCode": "+39", "phone": "3401112202", "role": "Admin"}]` |
| `rows[].idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01` |
| `rows[].email` | string \| null | `giulia.ferri@example.com` |
| `rows[].phoneCountryCode` | string \| null | `+39` |
| `rows[].phone` | string \| null | `3401112201` |
| `rows[].role` | string (enum UserRole) \| null | `SuperAdmin` |

### POST `/api/identity/create-user`

Gestione utenti: nuovo utente.

#### Payload

```json
{
  "email": "nuovo.utente@example.com",
  "isEmailActive": true,
  "password": "Sabbia-2026",
  "phone": "3401112299",
  "isPhoneActive": false,
  "role": "Waiter",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `email` | string | `nuovo.utente@example.com` |
| `isEmailActive` | boolean | `true` |
| `password` | string | `Sabbia-2026` |
| `phone` | string \| null | `3401112299` |
| `isPhoneActive` | boolean | `false` |
| `role` | string (enum UserRole) \| null | `Waiter` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/identity/get-selected-user?idUser={idUser}`

Gestione utenti: dati di un utente.

#### Payload

Nessun corpo: è una GET, l'utente va nella query string.

```text
/api/identity/get-selected-user?idUser=3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |

#### Response

Stato: 200 OK.

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "isEmailActive": true,
  "phone": "3401112203",
  "isPhoneActive": true,
  "role": "Manager"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |
| `email` | string | `sara.conti@example.com` |
| `isEmailActive` | boolean | `true` |
| `phone` | string \| null | `3401112203` |
| `isPhoneActive` | boolean | `true` |
| `role` | string (enum UserRole) \| null | `Manager` |

### POST `/api/identity/update-user`

Gestione utenti: modifica utente.

#### Payload

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "isEmailActive": true,
  "password": "Spiaggia-2027",
  "phone": "3401112203",
  "isPhoneActive": false,
  "role": "Manager",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |
| `email` | string | `sara.conti@example.com` |
| `isEmailActive` | boolean | `true` |
| `password` | string \| null | `Spiaggia-2027` |
| `phone` | string \| null | `3401112203` |
| `isPhoneActive` | boolean | `false` |
| `role` | string (enum UserRole) \| null | `Manager` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/identity/delete-user`

Gestione utenti: elimina utente.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /audit

### POST `/api/audit/entries-list`

Gestione utenti: log.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "ferri",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-10-17T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-10-23T23:59:59.999Z"
      ]
    },
    {
      "field": "actionCode",
      "value": [
        "booking.spot_added",
        "booking.dates_changed"
      ]
    },
    {
      "field": "idUser",
      "value": [
        "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01"
      ]
    }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `ferri` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-10-17T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-10-23T23:59:59.999Z"]}, {"field": "actionCode", "value": ["booking.spot_added", "booking.dates_changed"]}, {"field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-10-17T00:00:00.000Z"]` |
| `sortField` | string | `occurredAt` |
| `sortDirection` | string (enum SortDirection) | `Descending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idAuditEntry": "9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b01",
      "bookingNumber": "1234",
      "userNumber": "123",
      "actorName": "Giulia Ferri",
      "spotLabel": "A1",
      "actionCode": "booking.spot_added",
      "occurredAt": "2026-10-23T11:11:00.000Z"
    },
    {
      "idAuditEntry": "9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b11",
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

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | AuditEntryItem[] (array di oggetti) | `[{"idAuditEntry": "9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b01", "bookingNumber": "1234", "userNumber": "123", "actorName": "Giulia Ferri", "spotLabel": "A1", "actionCode": "booking.spot_added", "occurredAt": "2026-10-23T11:11:00.000Z"}, {"idAuditEntry": "9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b11", "bookingNumber": "2741", "userNumber": "123", "actorName": "Giulia Ferri", "spotLabel": "D2", "actionCode": "booking.dates_changed", "occurredAt": "2026-10-17T14:05:00.000Z"}]` |
| `rows[].idAuditEntry` | string (UUID) | `9a4c2e1f-7b3d-4e5a-8c6b-0d1e2f3a4b01` |
| `rows[].bookingNumber` | string \| null | `1234` |
| `rows[].userNumber` | string \| null | `123` |
| `rows[].actorName` | string \| null | `Giulia Ferri` |
| `rows[].spotLabel` | string \| null | `A1` |
| `rows[].actionCode` | string \| null | `booking.spot_added` |
| `rows[].occurredAt` | string (data e ora ISO 8601 UTC) \| null | `2026-10-23T11:11:00.000Z` |

### POST `/api/audit/get-user-combobox`

Gestione utenti: utenti del filtro dei log.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d09",
    "value": "Anna Caruso"
  },
  {
    "id": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01",
    "value": "Giulia Ferri"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d09` |
| `value` | string | `Anna Caruso` |

### POST `/api/audit/export-log`

Gestione utenti: esporta log.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "search": "ferri",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-10-20T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-10-26T23:59:59.999Z"
      ]
    }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `search` | string | `ferri` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-10-20T00:00:00.000Z"]` |
| `sortField` | string | `occurredAt` |
| `sortDirection` | string (enum SortDirection) | `Descending` |

#### Response

Stato: 200 OK.

Il corpo è il file (binario), non JSON.

| Header | Esempio |
|---|---|
| `Content-Type` | `text/csv` |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.csv"` |

### POST `/api/audit/print-log`

Gestione utenti: stampa log.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "search": "ferri",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-10-20T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-10-26T23:59:59.999Z"
      ]
    }
  ],
  "sortField": "occurredAt",
  "sortDirection": "Descending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `search` | string | `ferri` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-10-20T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-10-26T23:59:59.999Z"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-10-20T00:00:00.000Z"]` |
| `sortField` | string | `occurredAt` |
| `sortDirection` | string (enum SortDirection) | `Descending` |

#### Response

Stato: 200 OK.

Il corpo è il file (binario), non JSON.

| Header | Esempio |
|---|---|
| `Content-Type` | `application/pdf` |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.pdf"` |

## /seasons

### GET `/api/seasons/season-dates?idProperty={idProperty}`

Stagioni e periodi: date di stagione.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/seasons/season-dates?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "currentSeasonDateFrom": "2026-05-01T00:00:00.000Z",
  "currentSeasonDateTo": "2026-09-30T00:00:00.000Z",
  "nextSeasonDateFrom": "2027-05-01T00:00:00.000Z",
  "nextSeasonDateTo": "2027-09-30T00:00:00.000Z"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `currentSeasonDateFrom` | string (ISO 8601, UTC) | `2026-05-01T00:00:00.000Z` |
| `currentSeasonDateTo` | string (ISO 8601, UTC) | `2026-09-30T00:00:00.000Z` |
| `nextSeasonDateFrom` | string (ISO 8601, UTC) \| null | `2027-05-01T00:00:00.000Z` |
| `nextSeasonDateTo` | string (ISO 8601, UTC) \| null | `2027-09-30T00:00:00.000Z` |

### POST `/api/seasons/save-season-dates`

Stagioni e periodi: salva date di stagione.

#### Payload

```json
{
  "currentSeasonDateFrom": "2026-05-01T00:00:00.000Z",
  "currentSeasonDateTo": "2026-09-25T00:00:00.000Z",
  "nextSeasonDateFrom": "2027-05-01T00:00:00.000Z",
  "nextSeasonDateTo": "2027-09-30T00:00:00.000Z",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `currentSeasonDateFrom` | string (ISO 8601, UTC) | `2026-05-01T00:00:00.000Z` |
| `currentSeasonDateTo` | string (ISO 8601, UTC) | `2026-09-25T00:00:00.000Z` |
| `nextSeasonDateFrom` | string (ISO 8601, UTC) \| null | `2027-05-01T00:00:00.000Z` |
| `nextSeasonDateTo` | string (ISO 8601, UTC) \| null | `2027-09-30T00:00:00.000Z` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/seasons/period-list?idProperty={idProperty}&isNextSeason={isNextSeason}`

Stagioni e periodi: periodi.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/seasons/period-list?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d&isNextSeason=false
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `isNextSeason` | boolean | `false` |

#### Response

Stato: 200 OK.

```json
[
  {
    "idPeriod": "3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01",
    "periodName": "Bassa stagione",
    "intervals": [
      {
        "idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01",
        "dateFrom": "2026-05-01T00:00:00.000Z",
        "dateTo": "2026-06-15T00:00:00.000Z"
      },
      {
        "idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e02",
        "dateFrom": "2026-09-01T00:00:00.000Z",
        "dateTo": "2026-09-30T00:00:00.000Z"
      }
    ]
  },
  {
    "idPeriod": "3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e02",
    "periodName": "Media stagione",
    "intervals": [
      {
        "idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e03",
        "dateFrom": "2026-06-16T00:00:00.000Z",
        "dateTo": "2026-07-31T00:00:00.000Z"
      }
    ]
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPeriod` | string (UUID) | `3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01` |
| `periodName` | string | `Bassa stagione` |
| `intervals[]` | SeasonPeriodIntervalItem[] (array di oggetti) | `[{"idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01", "dateFrom": "2026-05-01T00:00:00.000Z", "dateTo": "2026-06-15T00:00:00.000Z"}, {"idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e02", "dateFrom": "2026-09-01T00:00:00.000Z", "dateTo": "2026-09-30T00:00:00.000Z"}]` |
| `intervals[].idInterval` | string (UUID) | `7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01` |
| `intervals[].dateFrom` | string (ISO 8601, UTC) | `2026-05-01T00:00:00.000Z` |
| `intervals[].dateTo` | string (ISO 8601, UTC) | `2026-06-15T00:00:00.000Z` |

### POST `/api/seasons/add-period`

Stagioni e periodi: nuovo periodo.

#### Payload

```json
{
  "periodName": "Pre stagione",
  "intervals": [
    {
      "dateFrom": "2027-04-20T00:00:00.000Z",
      "dateTo": "2027-04-30T00:00:00.000Z"
    }
  ],
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "isNextSeason": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `isNextSeason` | boolean | `true` |
| `periodName` | string | `Pre stagione` |
| `intervals[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"dateFrom": "2027-04-20T00:00:00.000Z", "dateTo": "2027-04-30T00:00:00.000Z"}]` |
| `intervals[].dateFrom` | string (ISO 8601, UTC) | `2027-04-20T00:00:00.000Z` |
| `intervals[].dateTo` | string (ISO 8601, UTC) | `2027-04-30T00:00:00.000Z` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/seasons/delete-period`

Stagioni e periodi: elimina periodo.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idPeriod": "3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idPeriod` | string (UUID) | `3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/seasons/update-period`

Stagioni e periodi: modifica periodo.

#### Payload

```json
{
  "idPeriod": "3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01",
  "periodName": "Bassissima stagione",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idPeriod` | string (UUID) | `3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01` |
| `periodName` | string | `Bassissima stagione` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/seasons/add-interval`

Stagioni e periodi: nuovo intervallo.

#### Payload

```json
{
  "dateFrom": "2026-09-01T00:00:00.000Z",
  "dateTo": "2026-09-05T00:00:00.000Z",
  "idPeriod": "3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e02",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idPeriod` | string (UUID) | `3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e02` |
| `dateFrom` | string (ISO 8601, UTC) | `2026-09-01T00:00:00.000Z` |
| `dateTo` | string (ISO 8601, UTC) | `2026-09-05T00:00:00.000Z` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/seasons/update-interval`

Stagioni e periodi: modifica intervallo.

#### Payload

```json
{
  "idInterval": "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01",
  "dateFrom": "2026-05-01T00:00:00.000Z",
  "dateTo": "2026-06-20T00:00:00.000Z",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idInterval` | string (UUID) | `7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e01` |
| `dateFrom` | string (ISO 8601, UTC) | `2026-05-01T00:00:00.000Z` |
| `dateTo` | string (ISO 8601, UTC) | `2026-06-20T00:00:00.000Z` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/seasons/delete-intervals`

Stagioni e periodi: elimina intervalli.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idIntervals": [
    "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e02",
    "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e03"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idIntervals` | string[] (UUID) | `["7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e02", "7c2e9a41-5b3d-4e8f-a1c6-2d4b6f8a0e03"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /warehouse

### POST `/api/warehouse/list`

Magazzino: elenco articoli.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "lettino",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-07-01T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-07-31T23:59:59.999Z"
      ]
    },
    {
      "field": "bookingDayType",
      "value": [
        "FullDay"
      ]
    }
  ],
  "sortField": "name",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `lettino` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-07-01T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-07-31T23:59:59.999Z"]}, {"field": "bookingDayType", "value": ["FullDay"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-07-01T00:00:00.000Z"]` |
| `sortField` | string | `name` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
      "name": "Lettino",
      "totalQuantity": 120,
      "availableQuantity": 84,
      "thresholdQuantity": 10,
      "isThresholdWarningActive": true
    },
    {
      "idArticle": "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b",
      "name": "Lettino XL",
      "totalQuantity": 16,
      "availableQuantity": 16,
      "thresholdQuantity": 4,
      "isThresholdWarningActive": false
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | WarehouseItem[] (array di oggetti) | `[{"idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c", "name": "Lettino", "totalQuantity": 120, "availableQuantity": 84, "thresholdQuantity": 10, "isThresholdWarningActive": true}, {"idArticle": "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b", "name": "Lettino XL", "totalQuantity": 16, "availableQuantity": 16, "thresholdQuantity": 4, "isThresholdWarningActive": false}]` |
| `rows[].idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `rows[].name` | string | `Lettino` |
| `rows[].totalQuantity` | number (intero) | `120` |
| `rows[].availableQuantity` | number (intero) | `84` |
| `rows[].thresholdQuantity` | number (intero) \| null | `10` |
| `rows[].isThresholdWarningActive` | boolean | `true` |

### POST `/api/warehouse/combobox-list`

Magazzino: articoli per la tendina.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "4a9e2c1f-7b3d-4e8a-9c6b-2d1f5e7a8b9c",
    "value": "Cabina"
  },
  {
    "id": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
    "value": "Lettino"
  },
  {
    "id": "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b",
    "value": "Lettino XL"
  },
  {
    "id": "6d1b8e4f-3c2a-4f9e-b7a5-9e8d7c6b5a4f",
    "value": "Ombrellone"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `4a9e2c1f-7b3d-4e8a-9c6b-2d1f5e7a8b9c` |
| `value` | string | `Cabina` |

### POST `/api/warehouse/add-warehouse-item`

Magazzino: aggiunta di un articolo.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "name": "Lettino",
  "totalQuantity": 10,
  "availableQuantity": 120,
  "thresholdQuantity": 5,
  "isThresholdWarningActive": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `name` | string \| null | `Lettino` |
| `totalQuantity` | number (intero > 0) | `10` |
| `availableQuantity` | number (intero) \| null | `120` |
| `thresholdQuantity` | number (intero ≥ 0) \| null | `5` |
| `isThresholdWarningActive` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/warehouse/edit-warehouse-item`

Magazzino: modifica di un articolo.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "name": "Lettino",
  "totalQuantity": 110,
  "availableQuantity": 84,
  "thresholdQuantity": 8,
  "isThresholdWarningActive": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `name` | string \| null | `Lettino` |
| `totalQuantity` | number (intero > 0) | `110` |
| `availableQuantity` | number (intero) \| null | `84` |
| `thresholdQuantity` | number (intero ≥ 0) \| null | `8` |
| `isThresholdWarningActive` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/warehouse/delete-warehouse-item`

Magazzino: eliminazione di articoli.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idItems": [
    "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
    "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idItems` | string[] (UUID, almeno uno) | `["8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c", "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/warehouse/duplicate-warehouse-item`

Magazzino: duplicazione di articoli.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idItems": [
    "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idItems` | string[] (UUID, almeno uno) | `["8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /database

### POST `/api/database/existing-booking-list`

Database: elenco prenotazioni.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "rossi",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-07-01T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-08-31T23:59:59.999Z"
      ]
    },
    {
      "field": "bookingTypology",
      "value": [
        "Daily",
        "SevenDays"
      ]
    },
    {
      "field": "agreementBooking",
      "value": [
        "Yes",
        "No"
      ]
    },
    {
      "field": "idAffiliatedHotel",
      "value": [
        "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41",
        "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42"
      ]
    },
    {
      "field": "paymentStatus",
      "value": [
        "FullyPaid",
        "PartiallyPaid"
      ]
    }
  ],
  "sortField": "dateFrom",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `rossi` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-07-01T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-08-31T23:59:59.999Z"]}, {"field": "bookingTypology", "value": ["Daily", "SevenDays"]}, {"field": "agreementBooking", "value": ["Yes", "No"]}, {"field": "idAffiliatedHotel", "value": ["5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41", "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42"]}, {"field": "paymentStatus", "value": ["FullyPaid", "PartiallyPaid"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-07-01T00:00:00.000Z"]` |
| `sortField` | string | `dateFrom` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idBooking": "3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70",
      "customerName": "Mario Rossi",
      "reservationKind": "Spot",
      "dateFrom": "2026-07-05T00:00:00.000Z",
      "dateTo": "2026-07-12T00:00:00.000Z",
      "agreementName": "Hotel Esempio",
      "reservationStatus": "Confirmed",
      "isTrashed": false
    },
    {
      "idBooking": "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01",
      "customerName": "Anna Rossi",
      "reservationKind": "Service",
      "dateFrom": "2026-08-02T00:00:00.000Z",
      "dateTo": "2026-08-02T00:00:00.000Z",
      "agreementName": "Hotel Miramare",
      "reservationStatus": "Pending",
      "isTrashed": false
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | ExistingBookingItem[] (array di oggetti) | `[{"idBooking": "3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70", "customerName": "Mario Rossi", "reservationKind": "Spot", "dateFrom": "2026-07-05T00:00:00.000Z", "dateTo": "2026-07-12T00:00:00.000Z", "agreementName": "Hotel Esempio", "reservationStatus": "Confirmed", "isTrashed": false}, {"idBooking": "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01", "customerName": "Anna Rossi", "reservationKind": "Service", "dateFrom": "2026-08-02T00:00:00.000Z", "dateTo": "2026-08-02T00:00:00.000Z", "agreementName": "Hotel Miramare", "reservationStatus": "Pending", "isTrashed": false}]` |
| `rows[].idBooking` | string (UUID) | `3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70` |
| `rows[].customerName` | string \| null | `Mario Rossi` |
| `rows[].reservationKind` | string (enum ReservationKind) \| null | `Spot` |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-07-05T00:00:00.000Z` |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null | `2026-07-12T00:00:00.000Z` |
| `rows[].agreementName` | string \| null | `Hotel Esempio` |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null | `Confirmed` |
| `rows[].isTrashed` | boolean | `false` |

### POST `/api/database/get-affiliated-hotel-combobox`

Database: hotel convenzionati per la tendina.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41",
    "value": "Hotel Miramare"
  },
  {
    "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a42",
    "value": "Hotel 5 stelle"
  },
  {
    "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a43",
    "value": "Hotel Sun"
  },
  {
    "id": "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a44",
    "value": "Beach Hotel"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41` |
| `value` | string | `Hotel Miramare` |

### POST `/api/database/move-to-trash`

Database: conferma della cancellazione con la password.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idBookings": [
    "3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70",
    "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01"
  ],
  "password": "password-digitata"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idBookings` | string[] (UUID, almeno uno) | `["3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70", "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01"]` |
| `password` | string | `password-digitata` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/database/trash-item-list`

Database: elenco del cestino.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "lilla",
  "filters": [
    {
      "field": "datetimeFrom",
      "value": [
        "2026-06-01T00:00:00.000Z"
      ]
    },
    {
      "field": "datetimeTo",
      "value": [
        "2026-06-30T23:59:59.999Z"
      ]
    },
    {
      "field": "bookingTypology",
      "value": [
        "MultiDay"
      ]
    },
    {
      "field": "agreementBooking",
      "value": [
        "Yes"
      ]
    },
    {
      "field": "idAffiliatedHotel",
      "value": [
        "5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41"
      ]
    },
    {
      "field": "paymentStatus",
      "value": [
        "AwaitingPayment"
      ]
    }
  ],
  "sortField": "customerName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `lilla` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-06-01T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-06-30T23:59:59.999Z"]}, {"field": "bookingTypology", "value": ["MultiDay"]}, {"field": "agreementBooking", "value": ["Yes"]}, {"field": "idAffiliatedHotel", "value": ["5b1d2c3e-4f50-4a61-8b72-9c0d1e2f3a41"]}, {"field": "paymentStatus", "value": ["AwaitingPayment"]}]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["2026-06-01T00:00:00.000Z"]` |
| `sortField` | string | `customerName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 1,
  "rows": [
    {
      "idBooking": "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d",
      "customerName": "Roberto Lilla",
      "reservationKind": "Spot",
      "dateFrom": "2026-06-05T00:00:00.000Z",
      "dateTo": "2026-06-07T00:00:00.000Z",
      "agreementName": "Hotel Miramare",
      "reservationStatus": "Cancelled",
      "isTrashed": true
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | ExistingBookingItem[] (array di oggetti) | `[{"idBooking": "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d", "customerName": "Roberto Lilla", "reservationKind": "Spot", "dateFrom": "2026-06-05T00:00:00.000Z", "dateTo": "2026-06-07T00:00:00.000Z", "agreementName": "Hotel Miramare", "reservationStatus": "Cancelled", "isTrashed": true}]` |
| `rows[].idBooking` | string (UUID) | `9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d` |
| `rows[].customerName` | string \| null | `Roberto Lilla` |
| `rows[].reservationKind` | string (enum ReservationKind) \| null | `Spot` |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-05T00:00:00.000Z` |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null | `2026-06-07T00:00:00.000Z` |
| `rows[].agreementName` | string \| null | `Hotel Miramare` |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null | `Cancelled` |
| `rows[].isTrashed` | boolean | `true` |

### POST `/api/database/empty-trash`

Database: svuota il cestino.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/database/restore-booking-item`

Database: ripristina dal cestino.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idBookings": [
    "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idBookings` | string[] (UUID, almeno uno) | `["9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/database/delete-selected-item`

Database: elimina definitivamente dal cestino.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idBookings": [
    "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idBookings` | string[] (UUID, almeno uno) | `["9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /taxation

### POST `/api/taxation/printer-list`

Fiscalizzazione: stampanti.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 5,
  "search": "epson",
  "filters": [
    {
      "field": "isEnabled",
      "value": [
        "true",
        "false"
      ]
    }
  ],
  "sortField": "printerName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `5` |
| `search` | string | `epson` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "isEnabled", "value": ["true", "false"]}]` |
| `filters[].field` | string | `isEnabled` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["true", "false"]` |
| `sortField` | string | `printerName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60",
      "printerName": "EPSON - 1",
      "printerIP": "192.0.2.10",
      "isEnabled": false
    },
    {
      "idPrinter": "9a4e6b2c-1d3f-4e5a-8b7c-2f6d1e3a4b50",
      "printerName": "EPSON - 2",
      "printerIP": "192.0.2.11",
      "isEnabled": true
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | PrinterItemListResponse[] (array di oggetti) | `[{"idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60", "printerName": "EPSON - 1", "printerIP": "192.0.2.10", "isEnabled": false}, {"idPrinter": "9a4e6b2c-1d3f-4e5a-8b7c-2f6d1e3a4b50", "printerName": "EPSON - 2", "printerIP": "192.0.2.11", "isEnabled": true}]` |
| `rows[].idPrinter` | string (UUID) | `5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60` |
| `rows[].printerName` | string | `EPSON - 1` |
| `rows[].printerIP` | string | `192.0.2.10` |
| `rows[].isEnabled` | boolean | `false` |

### POST `/api/taxation/set-is-active`

Fiscalizzazione: attivazione di una stampante.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60",
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idPrinter` | string (UUID) | `5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60` |
| `isEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/add-printer`

Fiscalizzazione: aggiunta di una stampante.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "printerName": "EPSON - 3",
  "printerIP": "192.0.2.12",
  "printerPort": 9100,
  "printerModel": "FP-81 II",
  "isSsl": false,
  "isFiscalMode": true,
  "isNonFiscalMode": false,
  "isOrderMode": false,
  "isTextMode": false,
  "departmentVatRates": [
    "22",
    "10",
    "4",
    "0",
    "0"
  ],
  "firstHeader": "Bagno Esempio",
  "secondHeader": "Via del Mare 1",
  "footer": "Grazie e arrivederci"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `printerName` | string | `EPSON - 3` |
| `printerIP` | string \| null | `192.0.2.12` |
| `printerPort` | number (intero) \| null | `9100` |
| `printerModel` | string \| null | `FP-81 II` |
| `isSsl` | boolean | `false` |
| `isFiscalMode` | boolean | `true` |
| `isNonFiscalMode` | boolean | `false` |
| `isOrderMode` | boolean | `false` |
| `isTextMode` | boolean | `false` |
| `departmentVatRates` | string[] (5 elementi) | `["22", "10", "4", "0", "0"]` |
| `firstHeader` | string \| null | `Bagno Esempio` |
| `secondHeader` | string \| null | `Via del Mare 1` |
| `footer` | string \| null | `Grazie e arrivederci` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/delete-printer`

Fiscalizzazione: eliminazione di una stampante.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idPrinter` | string (UUID) | `5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/download-guide-electronic-receipt`

Fiscalizzazione: guida dello scontrino elettronico.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

Il corpo è il file della guida (binario), non JSON.

| Header | Esempio |
|---|---|
| `Content-Type` | `application/pdf` |
| `Content-Disposition` | `attachment; filename="guida-scontrino-elettronico.pdf"` |

### POST `/api/taxation/electronic-receipt-activation`

Fiscalizzazione: attivazione dello scontrino elettronico.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "isElectronicReceiptAccepted": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `isElectronicReceiptAccepted` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/save-electronic-receipt-account`

Fiscalizzazione: account dello scontrino elettronico.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "companyName": "Bagno Esempio Srl",
  "companyAddress": "Via del Mare",
  "streetNumber": "1",
  "postalCode": "00100",
  "city": "Roma",
  "country": "Italia",
  "taxCode": "XXXXXXXXXXXXXXXX",
  "revenueAgencyCode": "AE00000000",
  "vatNumber": "00000000000",
  "pin": "00000",
  "password": "password-digitata"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `companyName` | string \| null | `Bagno Esempio Srl` |
| `companyAddress` | string \| null | `Via del Mare` |
| `streetNumber` | string \| null | `1` |
| `postalCode` | string \| null | `00100` |
| `city` | string \| null | `Roma` |
| `country` | string \| null | `Italia` |
| `taxCode` | string \| null | `XXXXXXXXXXXXXXXX` |
| `revenueAgencyCode` | string \| null | `AE00000000` |
| `vatNumber` | string \| null | `00000000000` |
| `pin` | string \| null | `00000` |
| `password` | string \| null | `password-digitata` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/taxation/electronic-receipt?idProperty={idProperty}`

Fiscalizzazione: scontrino elettronico.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/taxation/electronic-receipt?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

Il corpo è solo il testo dei termini e condizioni in HTML, come stringa JSON (tra virgolette, Content-Type application/json), nella lingua dell'header Accept-Language.

```json
"<h4>Generale</h4><p>Testo dei termini e condizioni…</p>"
```

## /sharing

### GET `/api/sharing/general-information?idProperty={idProperty}`

Sharing: impostazioni generali.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/sharing/general-information?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "isSharingEnabled": true,
  "isMinimumBookingDayEnabled": true,
  "minimumBookingDayQuantity": 3,
  "isOnlySeasonalEnabled": false,
  "isEnabledOnAllArticles": true,
  "isScheduledAbsenceSharing": true,
  "isManualCreditAssignmentEnabled": false,
  "resoldDayCreditQuantity": 1,
  "manualCreditValue": 2.5,
  "isAutomaticVoucherEnabled": true,
  "voucherCreditValue": 5.5,
  "isPrepaidCardTopUpEnabled": false,
  "prepaidCardCreditValue": 10
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `isSharingEnabled` | boolean | `true` |
| `isMinimumBookingDayEnabled` | boolean | `true` |
| `minimumBookingDayQuantity` | number (intero) \| null | `3` |
| `isOnlySeasonalEnabled` | boolean | `false` |
| `isEnabledOnAllArticles` | boolean | `true` |
| `isScheduledAbsenceSharing` | boolean | `true` |
| `isManualCreditAssignmentEnabled` | boolean | `false` |
| `resoldDayCreditQuantity` | number (intero) \| null | `1` |
| `manualCreditValue` | number (decimale, €) \| null | `2.5` |
| `isAutomaticVoucherEnabled` | boolean | `true` |
| `voucherCreditValue` | number (decimale, €) \| null | `5.5` |
| `isPrepaidCardTopUpEnabled` | boolean | `false` |
| `prepaidCardCreditValue` | number (decimale, €) \| null | `10` |

### POST `/api/sharing/save-information`

Sharing: salvataggio delle impostazioni.

#### Payload

```json
{
  "isSharingEnabled": true,
  "isMinimumBookingDayEnabled": true,
  "minimumBookingDayQuantity": 3,
  "isOnlySeasonalEnabled": false,
  "isEnabledOnAllArticles": true,
  "isScheduledAbsenceSharing": true,
  "isManualCreditAssignmentEnabled": false,
  "resoldDayCreditQuantity": 1,
  "manualCreditValue": 2.5,
  "isAutomaticVoucherEnabled": true,
  "voucherCreditValue": 5.5,
  "isPrepaidCardTopUpEnabled": false,
  "prepaidCardCreditValue": 10,
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `isSharingEnabled` | boolean | `true` |
| `isMinimumBookingDayEnabled` | boolean | `true` |
| `minimumBookingDayQuantity` | number (intero) \| null | `3` |
| `isOnlySeasonalEnabled` | boolean | `false` |
| `isEnabledOnAllArticles` | boolean | `true` |
| `isScheduledAbsenceSharing` | boolean | `true` |
| `isManualCreditAssignmentEnabled` | boolean | `false` |
| `resoldDayCreditQuantity` | number (intero) \| null | `1` |
| `manualCreditValue` | number (decimale, €) \| null | `2.5` |
| `isAutomaticVoucherEnabled` | boolean | `true` |
| `voucherCreditValue` | number (decimale, €) \| null | `5.5` |
| `isPrepaidCardTopUpEnabled` | boolean | `false` |
| `prepaidCardCreditValue` | number (decimale, €) \| null | `10` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /vouchers

### POST `/api/vouchers/voucher-list`

Sconti e voucher: elenco.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "g",
  "filters": [
    {
      "field": "discountType",
      "value": [
        "Percentage",
        "Fixed"
      ]
    }
  ],
  "sortField": "validFrom",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `g` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "discountType", "value": ["Percentage", "Fixed"]}]` |
| `filters[].field` | string | `discountType` |
| `filters[].value` | string[] (array di stringhe, almeno una) | `["Percentage", "Fixed"]` |
| `sortField` | string | `validFrom` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01",
      "voucherName": "Giugno",
      "validFrom": "2026-06-01T00:00:00.000Z",
      "validTo": "2026-06-30T00:00:00.000Z",
      "discountType": "Percentage",
      "discountValue": 10
    },
    {
      "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
      "voucherName": "Ferragosto",
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "discountType": "Fixed",
      "discountValue": 5
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | VoucherItem[] (array di oggetti) | `[{"idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01", "voucherName": "Giugno", "validFrom": "2026-06-01T00:00:00.000Z", "validTo": "2026-06-30T00:00:00.000Z", "discountType": "Percentage", "discountValue": 10}, {"idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02", "voucherName": "Ferragosto", "validFrom": "2026-08-12T00:00:00.000Z", "validTo": "2026-08-16T00:00:00.000Z", "discountType": "Fixed", "discountValue": 5}]` |
| `rows[].idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01` |
| `rows[].voucherName` | string \| null | `Giugno` |
| `rows[].validFrom` | string (data ISO 8601 UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `rows[].validTo` | string (data ISO 8601 UTC) \| null | `2026-06-30T00:00:00.000Z` |
| `rows[].discountType` | string (enum DiscountType) \| null | `Percentage` |
| `rows[].discountValue` | number (decimale) \| null | `10` |

### POST `/api/vouchers/delete-voucher-item`

Sconti e voucher: eliminazione.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idVouchers": [
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01",
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idVouchers` | string[] (UUID, almeno uno) | `["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01", "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/vouchers/duplicate-voucher-item`

Sconti e voucher: duplicazione.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idVouchers": [
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idVouchers` | string[] (UUID, almeno uno) | `["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/vouchers/add-voucher-item`

Sconti e voucher: aggiunta.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "voucherName": "Estate",
  "validityPeriods": [
    {
      "validFrom": "2026-08-01T00:00:00.000Z",
      "validTo": "2026-08-31T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3
      ],
      "timeSlots": [
        {
          "from": "09:00",
          "to": "12:00"
        },
        {
          "from": "15:00",
          "to": "17:00"
        }
      ]
    },
    {
      "validFrom": "2026-09-01T00:00:00.000Z",
      "validTo": "2026-09-15T00:00:00.000Z",
      "daysOfWeek": [
        6,
        7
      ],
      "timeSlots": [
        {
          "from": "10:00",
          "to": "18:00"
        }
      ]
    }
  ],
  "discountType": "Percentage",
  "discountValue": 15,
  "discountScope": "All",
  "maxUseQuantity": 1,
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `voucherName` | string | `Estate` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, almeno uno) | `[{"validFrom": "2026-08-01T00:00:00.000Z", "validTo": "2026-08-31T00:00:00.000Z", "daysOfWeek": [1, 2, 3], "timeSlots": [{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]}, {"validFrom": "2026-09-01T00:00:00.000Z", "validTo": "2026-09-15T00:00:00.000Z", "daysOfWeek": [6, 7], "timeSlots": [{"from": "10:00", "to": "18:00"}]}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-01T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-31T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) | `[{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `12:00` |
| `discountType` | string (enum DiscountType) | `Percentage` |
| `discountValue` | number (decimale) | `15` |
| `discountScope` | string (enum DiscountScope) | `All` |
| `maxUseQuantity` | number (intero ≥ 1) | `1` |
| `isEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/vouchers/update-voucher-item`

Sconti e voucher: modifica.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Estate 2026",
  "validityPeriods": [
    {
      "validFrom": "2026-08-01T00:00:00.000Z",
      "validTo": "2026-08-31T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3
      ],
      "timeSlots": [
        {
          "from": "09:00",
          "to": "12:00"
        },
        {
          "from": "15:00",
          "to": "17:00"
        }
      ]
    },
    {
      "validFrom": "2026-09-01T00:00:00.000Z",
      "validTo": "2026-09-15T00:00:00.000Z",
      "daysOfWeek": [
        6,
        7
      ],
      "timeSlots": [
        {
          "from": "10:00",
          "to": "18:00"
        }
      ]
    }
  ],
  "discountType": "Percentage",
  "discountValue": 20,
  "discountScope": "All",
  "maxUseQuantity": 1,
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02` |
| `voucherName` | string | `Estate 2026` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, almeno uno) | `[{"validFrom": "2026-08-01T00:00:00.000Z", "validTo": "2026-08-31T00:00:00.000Z", "daysOfWeek": [1, 2, 3], "timeSlots": [{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]}, {"validFrom": "2026-09-01T00:00:00.000Z", "validTo": "2026-09-15T00:00:00.000Z", "daysOfWeek": [6, 7], "timeSlots": [{"from": "10:00", "to": "18:00"}]}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-01T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-31T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) | `[{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `12:00` |
| `discountType` | string (enum DiscountType) | `Percentage` |
| `discountValue` | number (decimale) | `20` |
| `discountScope` | string (enum DiscountScope) | `All` |
| `maxUseQuantity` | number (intero ≥ 1) | `1` |
| `isEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/vouchers/get-selected-item?idVoucher={idVoucher}`

Sconti e voucher: dati per la modifica.

#### Payload

Nessun corpo: è una GET, l'id dello sconto/voucher va nella query string.

```text
/api/vouchers/get-selected-item?idVoucher=6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02` |

#### Response

Stato: 200 OK.

```json
{
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Ferragosto",
  "validityPeriods": [
    {
      "validFrom": "2026-08-12T00:00:00.000Z",
      "validTo": "2026-08-16T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3,
        4,
        5
      ],
      "timeSlots": [
        {
          "from": "09:00",
          "to": "13:00"
        },
        {
          "from": "15:00",
          "to": "19:00"
        }
      ]
    }
  ],
  "discountType": "Fixed",
  "discountValue": 5,
  "discountScope": "Services",
  "maxUseQuantity": 50,
  "isEnabled": false
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02` |
| `voucherName` | string \| null | `Ferragosto` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti) | `[{"validFrom": "2026-08-12T00:00:00.000Z", "validTo": "2026-08-16T00:00:00.000Z", "daysOfWeek": [1, 2, 3, 4, 5], "timeSlots": [{"from": "09:00", "to": "13:00"}, {"from": "15:00", "to": "19:00"}]}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-12T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-16T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3, 4, 5]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) | `[{"from": "09:00", "to": "13:00"}, {"from": "15:00", "to": "19:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `13:00` |
| `discountType` | string (enum DiscountType) \| null | `Fixed` |
| `discountValue` | number (decimale) \| null | `5` |
| `discountScope` | string (enum DiscountScope) \| null | `Services` |
| `maxUseQuantity` | number (intero) \| null | `50` |
| `isEnabled` | boolean \| null | `false` |

## /catalog

### GET `/api/catalog/countries-combobox`

Catalogo: nazioni per le tendine.

#### Payload

Nessun corpo e nessun parametro: è una GET. La lingua dei nomi è quella dell'header Accept-Language.

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "IT",
    "value": "Italia"
  },
  {
    "id": "FR",
    "value": "Francia"
  },
  {
    "id": "DE",
    "value": "Germania"
  },
  {
    "id": "CN",
    "value": "Cina"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (ISO 3166-1 alpha-2) | `IT` |
| `value` | string | `Italia` |

### GET `/api/catalog/bathhouse-configuration-general?idProperty={idProperty}`

Informazioni stabilimento: informazioni generali.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/catalog/bathhouse-configuration-general?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "bathhouseName": "Lido Demo",
  "address": "Lungomare dei Pini 12",
  "postalCode": "00100",
  "city": "Roma",
  "province": "Roma",
  "region": "Lazio",
  "country": "IT",
  "phone": "+39 06 1234567",
  "whatsapp": "+39 333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "cancellationPolicy": "FreeUntil48Hours",
  "petPolicy": "Allowed",
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `bathhouseName` | string \| null | `Lido Demo` |
| `address` | string \| null | `Lungomare dei Pini 12` |
| `postalCode` | string \| null | `00100` |
| `city` | string \| null | `Roma` |
| `province` | string \| null | `Roma` |
| `region` | string \| null | `Lazio` |
| `country` | string (ISO 3166-1 alpha-2) \| null | `IT` |
| `phone` | string \| null | `+39 06 1234567` |
| `whatsapp` | string \| null | `+39 333 1234567` |
| `email` | string \| null | `info@lidodemo.example` |
| `website` | string \| null | `https://www.lidodemo.example` |
| `facebook` | string \| null | `https://www.facebook.com/lidodemo.example` |
| `instagram` | string \| null | `https://www.instagram.com/lidodemo.example` |
| `description` | string (HTML) \| null | `<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>` |
| `openingTime` | string (HH:mm) \| null | `08:30` |
| `closingTime` | string (HH:mm) \| null | `19:00` |
| `cancellationPolicy` | string (enum CancellationPolicy) \| null | `FreeUntil48Hours` |
| `petPolicy` | string (enum PetPolicy) \| null | `Allowed` |
| `regulation` | string (HTML) \| null | `<p>Vietato <strong>fumare</strong> in spiaggia.</p>` |

### POST `/api/catalog/save-bathhouse-configuration-general`

Informazioni stabilimento: salvataggio delle informazioni generali.

#### Payload

```json
{
  "bathhouseName": "Lido Demo",
  "address": "Lungomare dei Pini 12",
  "postalCode": "00100",
  "city": "Roma",
  "province": "Roma",
  "region": "Lazio",
  "country": "IT",
  "phone": "+39 06 1234567",
  "whatsapp": "+39 333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "cancellationPolicy": "FreeUntil48Hours",
  "petPolicy": "Allowed",
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `bathhouseName` | string \| null | `Lido Demo` |
| `address` | string \| null | `Lungomare dei Pini 12` |
| `postalCode` | string \| null | `00100` |
| `city` | string \| null | `Roma` |
| `province` | string \| null | `Roma` |
| `region` | string \| null | `Lazio` |
| `country` | string (ISO 3166-1 alpha-2) \| null | `IT` |
| `phone` | string \| null | `+39 06 1234567` |
| `whatsapp` | string \| null | `+39 333 1234567` |
| `email` | string \| null | `info@lidodemo.example` |
| `website` | string \| null | `https://www.lidodemo.example` |
| `facebook` | string \| null | `https://www.facebook.com/lidodemo.example` |
| `instagram` | string \| null | `https://www.instagram.com/lidodemo.example` |
| `description` | string (HTML) \| null | `<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>` |
| `openingTime` | string (HH:mm) \| null | `08:30` |
| `closingTime` | string (HH:mm) \| null | `19:00` |
| `cancellationPolicy` | string (enum CancellationPolicy) \| null | `FreeUntil48Hours` |
| `petPolicy` | string (enum PetPolicy) \| null | `Allowed` |
| `regulation` | string (HTML) \| null | `<p>Vietato <strong>fumare</strong> in spiaggia.</p>` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/bathhouse-configuration-services?idProperty={idProperty}`

Informazioni stabilimento: servizi.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/catalog/bathhouse-configuration-services?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
[
  {
    "idService": 1,
    "name": "Accesso disabili",
    "categoryCode": "accessibility_comfort",
    "iconUrl": "/media/icons/accesso-disabili.svg",
    "isSelected": true
  },
  {
    "idService": 3,
    "name": "Doccia",
    "categoryCode": "accessibility_comfort",
    "iconUrl": "/media/icons/doccia.svg",
    "isSelected": true
  },
  {
    "idService": 6,
    "name": "Wifi",
    "categoryCode": "accessibility_comfort",
    "iconUrl": null,
    "isSelected": false
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `idService` | number (intero) | `1` |
| `name` | string | `Accesso disabili` |
| `categoryCode` | string | `accessibility_comfort` |
| `iconUrl` | string \| null | `/media/icons/accesso-disabili.svg` |
| `isSelected` | boolean | `true` |

### POST `/api/catalog/save-bathhouse-configuration-services`

Informazioni stabilimento: salvataggio dei servizi.

#### Payload

```json
{
  "services": [
    {
      "idService": 1,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "/media/icons/accesso-disabili.svg",
      "isSelected": true
    },
    {
      "idService": 3,
      "name": "Doccia",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "/media/icons/doccia.svg",
      "isSelected": true
    }
  ],
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `services[]` | BathhouseServiceItem[] (array di oggetti) | `[{"idService": 1, "name": "Accesso disabili", "categoryCode": "accessibility_comfort", "iconUrl": "/media/icons/accesso-disabili.svg", "isSelected": true}, {"idService": 3, "name": "Doccia", "categoryCode": "accessibility_comfort", "iconUrl": "/media/icons/doccia.svg", "isSelected": true}]` |
| `services[].idService` | number (intero) | `1` |
| `services[].name` | string | `Accesso disabili` |
| `services[].categoryCode` | string | `accessibility_comfort` |
| `services[].iconUrl` | string \| null | `/media/icons/accesso-disabili.svg` |
| `services[].isSelected` | boolean | `true` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/bathhouse-configuration-media?idProperty={idProperty}`

Informazioni stabilimento: media.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/catalog/bathhouse-configuration-media?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "photos": [
    {
      "idPhoto": 41,
      "url": "https://media.example.com/lido-demo/spiaggia.webp",
      "isMain": true
    },
    {
      "idPhoto": 42,
      "url": "https://media.example.com/lido-demo/bar.webp",
      "isMain": false
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "priceListPdfUrl": "https://media.example.com/lido-demo/listino-prezzi.pdf",
  "menuPdfUrl": "https://media.example.com/lido-demo/menu.pdf"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `photos[]` | BathhousePhoto[] (array di oggetti) | `[{"idPhoto": 41, "url": "https://media.example.com/lido-demo/spiaggia.webp", "isMain": true}, {"idPhoto": 42, "url": "https://media.example.com/lido-demo/bar.webp", "isMain": false}]` |
| `photos[].idPhoto` | number (intero) | `41` |
| `photos[].url` | string | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[].isMain` | boolean | `true` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `priceListPdfUrl` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdfUrl` | string \| null | `https://media.example.com/lido-demo/menu.pdf` |

## /booking

### GET `/api/booking/get-online-services-options?idProperty={idProperty}`

Prenotazioni online: opzioni dei servizi.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/booking/get-online-services-options?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "isServicesOnlineBookingEnabled": true,
  "supplementType": "Percentage",
  "supplementValue": 10,
  "isBookingConfirmationRequired": false,
  "isPayNowAccepted": true,
  "isPayAtPropertyAccepted": true,
  "isPayAtPropertyWithoutCardAccepted": false,
  "isDepositAccepted": false,
  "isPrepaidCardAccepted": true,
  "bookingPanelTitle": "<p>Prenota i tuoi servizi.</p>",
  "usefulInformation": "<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `isServicesOnlineBookingEnabled` | boolean | `true` |
| `supplementType` | string (enum SupplementType) | `Percentage` |
| `supplementValue` | number (decimale) \| null | `10` |
| `isBookingConfirmationRequired` | boolean | `false` |
| `isPayNowAccepted` | boolean | `true` |
| `isPayAtPropertyAccepted` | boolean | `true` |
| `isPayAtPropertyWithoutCardAccepted` | boolean | `false` |
| `isDepositAccepted` | boolean | `false` |
| `isPrepaidCardAccepted` | boolean | `true` |
| `bookingPanelTitle` | string (HTML) \| null | `<p>Prenota i tuoi servizi.</p>` |
| `usefulInformation` | string (HTML) \| null | `<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>` |

### POST `/api/booking/update-online-services-options`

Prenotazioni online: salvataggio delle opzioni dei servizi.

#### Payload

```json
{
  "isServicesOnlineBookingEnabled": true,
  "supplementType": "Percentage",
  "supplementValue": 10,
  "isBookingConfirmationRequired": false,
  "isPayNowAccepted": true,
  "isPayAtPropertyAccepted": true,
  "isPayAtPropertyWithoutCardAccepted": false,
  "isDepositAccepted": false,
  "isPrepaidCardAccepted": true,
  "bookingPanelTitle": "<p>Prenota i tuoi servizi.</p>",
  "usefulInformation": "<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>",
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `isServicesOnlineBookingEnabled` | boolean | `true` |
| `supplementType` | string (enum SupplementType) | `Percentage` |
| `supplementValue` | number (decimale) \| null | `10` |
| `isBookingConfirmationRequired` | boolean | `false` |
| `isPayNowAccepted` | boolean | `true` |
| `isPayAtPropertyAccepted` | boolean | `true` |
| `isPayAtPropertyWithoutCardAccepted` | boolean | `false` |
| `isDepositAccepted` | boolean | `false` |
| `isPrepaidCardAccepted` | boolean | `true` |
| `bookingPanelTitle` | string (HTML) \| null | `<p>Prenota i tuoi servizi.</p>` |
| `usefulInformation` | string (HTML) \| null | `<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>` |
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /system

### GET `/api/system/email-configuration?idProperty={idProperty}`

Configurazione email: lettura.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/system/email-configuration?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "senderMailAddress": "prenotazioni@lido-demo.example",
  "senderName": "Lido Demo",
  "smtpServerAddress": "smtp.lido-demo.example",
  "smtpPort": 587,
  "smtpUsername": "prenotazioni@lido-demo.example",
  "hasSmtpPassword": true,
  "smtpSecurity": "Tls"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `senderMailAddress` | string | `prenotazioni@lido-demo.example` |
| `senderName` | string | `Lido Demo` |
| `smtpServerAddress` | string | `smtp.lido-demo.example` |
| `smtpPort` | number (intero) | `587` |
| `smtpUsername` | string | `prenotazioni@lido-demo.example` |
| `hasSmtpPassword` | boolean | `true` |
| `smtpSecurity` | string (enum SmtpSecurity) | `Tls` |

### POST `/api/system/send-test-email`

Configurazione email: invio di prova.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "senderMailAddress": "prenotazioni@lido-demo.example",
  "senderName": "Lido Demo",
  "smtpServerAddress": "smtp.lido-demo.example",
  "smtpPort": 587,
  "smtpUsername": "prenotazioni@lido-demo.example",
  "smtpPassword": "password-smtp-digitata",
  "smtpSecurity": "Tls"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `senderMailAddress` | string | `prenotazioni@lido-demo.example` |
| `senderName` | string | `Lido Demo` |
| `smtpServerAddress` | string | `smtp.lido-demo.example` |
| `smtpPort` | number (intero 1–65535) | `587` |
| `smtpUsername` | string | `prenotazioni@lido-demo.example` |
| `smtpPassword` | string \| null | `password-smtp-digitata` |
| `smtpSecurity` | string (enum SmtpSecurity) \| null | `Tls` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/system/save-email-configuration`

Configurazione email: salvataggio.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "senderMailAddress": "prenotazioni@lido-demo.example",
  "senderName": "Lido Demo",
  "smtpServerAddress": "smtp.lido-demo.example",
  "smtpPort": 465,
  "smtpUsername": "prenotazioni@lido-demo.example",
  "smtpPassword": "password-smtp-digitata",
  "smtpSecurity": "Ssl"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `senderMailAddress` | string | `prenotazioni@lido-demo.example` |
| `senderName` | string | `Lido Demo` |
| `smtpServerAddress` | string | `smtp.lido-demo.example` |
| `smtpPort` | number (intero 1–65535) | `465` |
| `smtpUsername` | string | `prenotazioni@lido-demo.example` |
| `smtpPassword` | string \| null | `password-smtp-digitata` |
| `smtpSecurity` | string (enum SmtpSecurity) \| null | `Ssl` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/system/info-support`

Assistenza: contatti e orari.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

```json
{
  "phoneNumber": "050 7916620",
  "mailAddress": "info@summerbooking.it",
  "supportHour": [
    "1 maggio - 30 settembre, dal lunedì alla domenica: dalle 8:30 alle 20:00",
    "1 ottobre - 30 aprile, dal lunedì al venerdì: dalle 9:00 alle 18:00"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `phoneNumber` | string | `050 7916620` |
| `mailAddress` | string | `info@summerbooking.it` |
| `supportHour` | string[] | `["1 maggio - 30 settembre, dal lunedì alla domenica: dalle 8:30 alle 20:00", "1 ottobre - 30 aprile, dal lunedì al venerdì: dalle 9:00 alle 18:00"]` |

### POST `/api/system/contact-support`

Assistenza: messaggio al supporto.

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "firstName": "Mario",
  "lastName": "Rossi",
  "email": "mario.rossi@example.com",
  "mobilePhone": "+39 333 123 4567",
  "message": "Buongiorno, vorrei sapere come esportare il listino."
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `firstName` | string | `Mario` |
| `lastName` | string | `Rossi` |
| `email` | string | `mario.rossi@example.com` |
| `mobilePhone` | string \| null | `+39 333 123 4567` |
| `message` | string | `Buongiorno, vorrei sapere come esportare il listino.` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/system/get-stripe-information?idProperty={idProperty}`

Prenotazioni online: conto Stripe collegato.

#### Payload

Nessun corpo: è una GET, lo stabilimento va nella query string.

```text
/api/system/get-stripe-information?idProperty=5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |

#### Response

Stato: 200 OK.

Il corpo è solo l'id del conto Stripe collegato, come stringa JSON (tra virgolette, Content-Type application/json): acct_…. null se lo stabilimento non ha ancora collegato Stripe.

```json
"acct_1QmDemo4Fz8Lw2Xc"
```
