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
  "page": 1,
  "pageSize": 10,
  "search": "example.com",
  "filters": [],
  "sortField": "email",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `example.com` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `email` |
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
      "phoneCountryCode": "39",
      "phone": "3401112201",
      "role": "superadmin"
    },
    {
      "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02",
      "email": "marco.bianchi@example.com",
      "phoneCountryCode": "39",
      "phone": "3401112202",
      "role": "admin"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | UserItem[] (array di oggetti) | `[{"idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01", "email": "giulia.ferri@example.com", "phoneCountryCode": "39", "phone": "3401112201", "role": "superadmin"}, {"idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d02", "email": "marco.bianchi@example.com", "phoneCountryCode": "39", "phone": "3401112202", "role": "admin"}]` |
| `rows[].idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01` |
| `rows[].email` | string | `giulia.ferri@example.com` |
| `rows[].phoneCountryCode` | string \| null | `39` |
| `rows[].phone` | string \| null | `3401112201` |
| `rows[].role` | string (enum UserRole) | `superadmin` |

### POST `/api/identity/create-user`

Gestione utenti: nuovo utente.

#### Payload

```json
{
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "password": "Sabbia-2026",
  "phoneCountryCode": "39",
  "phone": "3401112299",
  "phoneIsContact": false,
  "role": "waiter"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `email` | string | `nuovo.utente@example.com` |
| `emailIsContact` | boolean | `true` |
| `password` | string | `Sabbia-2026` |
| `phoneCountryCode` | string \| null | `39` |
| `phone` | string \| null | `3401112299` |
| `phoneIsContact` | boolean | `false` |
| `role` | string (enum UserRole) | `waiter` |

#### Response

Stato: 201 Created (l'utente creato).

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d15",
  "email": "nuovo.utente@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "3401112299",
  "phoneIsContact": false,
  "role": "waiter",
  "version": "a36e06e9"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d15` |
| `email` | string | `nuovo.utente@example.com` |
| `emailIsContact` | boolean | `true` |
| `phoneCountryCode` | string \| null | `39` |
| `phone` | string \| null | `3401112299` |
| `phoneIsContact` | boolean | `false` |
| `role` | string (enum UserRole) | `waiter` |
| `version` | string | `a36e06e9` |

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
  "emailIsContact": true,
  "phoneCountryCode": "39",
  "phone": "3401112203",
  "phoneIsContact": true,
  "role": "manager",
  "version": "4a685fa7"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |
| `email` | string | `sara.conti@example.com` |
| `emailIsContact` | boolean | `true` |
| `phoneCountryCode` | string \| null | `39` |
| `phone` | string \| null | `3401112203` |
| `phoneIsContact` | boolean | `true` |
| `role` | string (enum UserRole) | `manager` |
| `version` | string | `4a685fa7` |

### POST `/api/identity/update-user`

Gestione utenti: modifica utente.

#### Payload

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "password": "Spiaggia-2027",
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |
| `email` | string | `sara.conti@example.com` |
| `emailIsContact` | boolean | `true` |
| `password` | string \| null | `Spiaggia-2027` |
| `phoneCountryCode` | string \| null | `41` |
| `phone` | string \| null | `79 123 45 67` |
| `phoneIsContact` | boolean | `false` |
| `role` | string (enum UserRole) | `manager` |

#### Response

Stato: 200 OK (l'utente aggiornato).

```json
{
  "idUser": "3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03",
  "email": "sara.conti@example.com",
  "emailIsContact": true,
  "phoneCountryCode": "41",
  "phone": "79 123 45 67",
  "phoneIsContact": false,
  "role": "manager",
  "version": "d433d493"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idUser` | string (UUID) | `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d03` |
| `email` | string | `sara.conti@example.com` |
| `emailIsContact` | boolean | `true` |
| `phoneCountryCode` | string \| null | `41` |
| `phone` | string \| null | `79 123 45 67` |
| `phoneIsContact` | boolean | `false` |
| `role` | string (enum UserRole) | `manager` |
| `version` | string | `d433d493` |

### DELETE `/api/identity/property-users/{id}`

Gestione utenti: elimina utente.

#### Payload

Nessun corpo: l'id dell'utente va nell'indirizzo e lo stabilimento viene dal token.

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/identity/user-combobox`

Gestione utenti: utenti del filtro dei log.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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

## /audit

### POST `/api/audit/entries-list`

Gestione utenti: log.

#### Payload

```json
{
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
        "catalog.map_spots_created",
        "catalog.season_updated"
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
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `ferri` |
| `filters[]` | PageFilter[] (array di oggetti) | `[{"field": "datetimeFrom", "value": ["2026-10-17T00:00:00.000Z"]}, {"field": "datetimeTo", "value": ["2026-10-23T23:59:59.999Z"]}, {"field": "actionCode", "value": ["catalog.map_spots_created", "catalog.season_updated"]}, {"field": "idUser", "value": ["3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01"]}]` |
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
      "idAuditEntry": "1287",
      "bookingNumber": "1234",
      "actorName": "Giulia Ferri",
      "spotLabel": "A1",
      "actionCode": "catalog.map_spots_created",
      "occurredAt": "2026-10-23T11:11:00.000Z"
    },
    {
      "idAuditEntry": "1265",
      "bookingNumber": "2741",
      "actorName": "Giulia Ferri",
      "spotLabel": "D2",
      "actionCode": "catalog.season_updated",
      "occurredAt": "2026-10-17T14:05:00.000Z"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | AuditEntryItem[] (array di oggetti) | `[{"idAuditEntry": "1287", "bookingNumber": "1234", "actorName": "Giulia Ferri", "spotLabel": "A1", "actionCode": "catalog.map_spots_created", "occurredAt": "2026-10-23T11:11:00.000Z"}, {"idAuditEntry": "1265", "bookingNumber": "2741", "actorName": "Giulia Ferri", "spotLabel": "D2", "actionCode": "catalog.season_updated", "occurredAt": "2026-10-17T14:05:00.000Z"}]` |
| `rows[].idAuditEntry` | string | `1287` |
| `rows[].bookingNumber` | string \| null | `1234` |
| `rows[].actorName` | string \| null | `Giulia Ferri` |
| `rows[].spotLabel` | string \| null | `A1` |
| `rows[].actionCode` | string | `catalog.map_spots_created` |
| `rows[].occurredAt` | string (data e ora ISO 8601 UTC) | `2026-10-23T11:11:00.000Z` |

### POST `/api/audit/export-log`

Gestione utenti: esporta log.

#### Payload

```json
{
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
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.xlsx"` |

### POST `/api/audit/print-log`

Gestione utenti: stampa log.

#### Payload

```json
{
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
| `Content-Type` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| `Content-Disposition` | `attachment; filename="log-2026-10-26.xlsx"` |

## /warehouse

### POST `/api/warehouse/list`

Magazzino: elenco articoli.

#### Payload

```json
{
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

## /database

### POST `/api/database/existing-booking-list`

Database: elenco prenotazioni.

#### Payload

```json
{
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

### POST `/api/database/affiliated-hotel-combobox`

Database: hotel convenzionati per la tendina.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "idBookings": [
    "3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70",
    "7a8b9c0d-1e2f-4a3b-9c4d-5e6f7a8b9c01"
  ],
  "password": "password-digitata"
}
```

| Property | Tipo | Esempio |
|---|---|---|
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

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/database/restore-booking-item`

Database: ripristina dal cestino.

#### Payload

```json
{
  "idBookings": [
    "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBookings` | string[] (UUID, almeno uno) | `["9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/database/delete-selected-item`

Database: elimina definitivamente dal cestino.

#### Payload

```json
{
  "idBookings": [
    "9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
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
  "idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60",
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
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
  "idPrinter": "5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPrinter` | string (UUID) | `5f0c2a1e-8b3d-4c6f-9a2e-1d7b3c4e5f60` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/download-guide-electronic-receipt`

Fiscalizzazione: guida dello scontrino elettronico.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "isElectronicReceiptAccepted": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `isElectronicReceiptAccepted` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/taxation/save-electronic-receipt-account`

Fiscalizzazione: account dello scontrino elettronico.

#### Payload

```json
{
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

### GET `/api/taxation/electronic-receipt`

Fiscalizzazione: scontrino elettronico.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

Il corpo è solo il testo dei termini e condizioni in HTML, come stringa JSON (tra virgolette, Content-Type application/json), nella lingua dell'header Accept-Language.

```json
"<h4>Generale</h4><p>Testo dei termini e condizioni…</p>"
```

## /sharing

### GET `/api/sharing/general-information`

Sharing: impostazioni generali.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "prepaidCardCreditValue": 10,
  "version": "106cb2d0"
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
| `version` | string | `106cb2d0` |

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

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /vouchers

### POST `/api/catalog/discounts/voucher-list`

Sconti e voucher: elenco.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "g",
  "filters": [],
  "sortField": "validFrom",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `g` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
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
      "discountValue": 12.5
    },
    {
      "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f04",
      "voucherName": "Clienti abituali",
      "validFrom": null,
      "validTo": null,
      "discountType": "Fixed",
      "discountValue": 15
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | VoucherItem[] (array di oggetti) | `[{"idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01", "voucherName": "Giugno", "validFrom": "2026-06-01T00:00:00.000Z", "validTo": "2026-06-30T00:00:00.000Z", "discountType": "Percentage", "discountValue": 12.5}, {"idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f04", "voucherName": "Clienti abituali", "validFrom": null, "validTo": null, "discountType": "Fixed", "discountValue": 15}]` |
| `rows[].idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01` |
| `rows[].voucherName` | string | `Giugno` |
| `rows[].validFrom` | string (data ISO 8601 UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `rows[].validTo` | string (data ISO 8601 UTC) \| null | `2026-06-30T00:00:00.000Z` |
| `rows[].discountType` | string (enum DiscountType) \| null | `Percentage` |
| `rows[].discountValue` | number (decimale) \| null | `12.5` |

### POST `/api/catalog/discounts/delete-voucher-item`

Sconti e voucher: eliminazione.

#### Payload

```json
{
  "idVouchers": [
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01",
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVouchers` | string[] (UUID, almeno uno) | `["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01", "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/discounts/duplicate-voucher-item`

Sconti e voucher: duplicazione.

#### Payload

```json
{
  "idVouchers": [
    "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVouchers` | string[] (UUID, almeno uno) | `["6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/discounts/add-voucher-item`

Sconti e voucher: aggiunta.

#### Payload

```json
{
  "voucherName": "Estate",
  "voucherCode": "ESTATE15",
  "isCodeRequired": true,
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
      "timeSlots": []
    }
  ],
  "discountType": "Percentage",
  "discountValue": 12.5,
  "discountScope": "Custom",
  "serviceCategories": [
    "Bar",
    "Normal"
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "maxUseQuantity": 1,
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `voucherName` | string | `Estate` |
| `voucherCode` | string | `ESTATE15` |
| `isCodeRequired` | boolean | `true` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, al massimo 50) | `[{"validFrom": "2026-08-01T00:00:00.000Z", "validTo": "2026-08-31T00:00:00.000Z", "daysOfWeek": [1, 2, 3], "timeSlots": [{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]}, {"validFrom": "2026-09-01T00:00:00.000Z", "validTo": "2026-09-15T00:00:00.000Z", "daysOfWeek": [6, 7], "timeSlots": []}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-01T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-31T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, al massimo 20, anche vuoto) | `[{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `12:00` |
| `discountType` | string (enum DiscountType) | `Percentage` |
| `discountValue` | number (decimale) | `12.5` |
| `discountScope` | string (enum DiscountScope) | `Custom` |
| `serviceCategories` | string[] (enum DiscountServiceCategory, anche vuoto) | `["Bar", "Normal"]` |
| `idSectors` | string[] (UUID, anche vuoto) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `maxUseQuantity` | number (intero ≥ 1) \| null | `1` |
| `isEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/discounts/update-voucher-item`

Sconti e voucher: modifica.

#### Payload

```json
{
  "idVoucher": "6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02",
  "voucherName": "Estate 2026",
  "voucherCode": "ESTATE20",
  "isCodeRequired": true,
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
      "timeSlots": []
    }
  ],
  "discountType": "Percentage",
  "discountValue": 20,
  "discountScope": "All",
  "serviceCategories": [],
  "idSectors": [],
  "maxUseQuantity": null,
  "isEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02` |
| `voucherName` | string | `Estate 2026` |
| `voucherCode` | string | `ESTATE20` |
| `isCodeRequired` | boolean | `true` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, al massimo 50) | `[{"validFrom": "2026-08-01T00:00:00.000Z", "validTo": "2026-08-31T00:00:00.000Z", "daysOfWeek": [1, 2, 3], "timeSlots": [{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]}, {"validFrom": "2026-09-01T00:00:00.000Z", "validTo": "2026-09-15T00:00:00.000Z", "daysOfWeek": [6, 7], "timeSlots": []}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-01T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-31T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, al massimo 20, anche vuoto) | `[{"from": "09:00", "to": "12:00"}, {"from": "15:00", "to": "17:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `12:00` |
| `discountType` | string (enum DiscountType) | `Percentage` |
| `discountValue` | number (decimale) | `20` |
| `discountScope` | string (enum DiscountScope) | `All` |
| `serviceCategories` | string[] (enum DiscountServiceCategory, anche vuoto) | `[]` |
| `idSectors` | string[] (UUID, anche vuoto) | `[]` |
| `maxUseQuantity` | number (intero ≥ 1) \| null | `null` |
| `isEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/discounts/get-selected-item?idVoucher={idVoucher}`

Sconti e voucher: dati per la modifica.

#### Payload

Nessun corpo: è una GET, l'id dello sconto/voucher va nella query string.

```text
/api/catalog/discounts/get-selected-item?idVoucher=6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02
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
  "voucherCode": "FERRAGOSTO5",
  "isCodeRequired": true,
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
  "discountScope": "WithoutCustom",
  "serviceCategories": [
    "Additional"
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "maxUseQuantity": null,
  "isEnabled": false,
  "version": "bf1aac8c"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idVoucher` | string (UUID) | `6d1e2f3a-4b5c-4d6e-8f7a-9b0c1d2e3f02` |
| `voucherName` | string \| null | `Ferragosto` |
| `voucherCode` | string | `FERRAGOSTO5` |
| `isCodeRequired` | boolean | `true` |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, anche vuoto) | `[{"validFrom": "2026-08-12T00:00:00.000Z", "validTo": "2026-08-16T00:00:00.000Z", "daysOfWeek": [1, 2, 3, 4, 5], "timeSlots": [{"from": "09:00", "to": "13:00"}, {"from": "15:00", "to": "19:00"}]}]` |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) | `2026-08-12T00:00:00.000Z` |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) | `2026-08-16T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) | `[1, 2, 3, 4, 5]` |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, al massimo 20, anche vuoto) | `[{"from": "09:00", "to": "13:00"}, {"from": "15:00", "to": "19:00"}]` |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) | `09:00` |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) | `13:00` |
| `discountType` | string (enum DiscountType) | `Fixed` |
| `discountValue` | number (decimale) | `5` |
| `discountScope` | string (enum DiscountScope) | `WithoutCustom` |
| `serviceCategories` | string[] (enum DiscountServiceCategory, anche vuoto) | `["Additional"]` |
| `idSectors` | string[] (UUID, anche vuoto) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `maxUseQuantity` | number (intero) \| null | `null` |
| `isEnabled` | boolean | `false` |
| `version` | string | `bf1aac8c` |

## /catalog

### GET `/api/catalog/season`

Stagioni e periodi: date di stagione.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK (204 senza corpo se la stagione non è impostata).

```json
{
  "startsOn": "2026-05-01",
  "endsOn": "2026-09-30",
  "nextStartsOn": "2027-05-01",
  "nextEndsOn": "2027-09-30",
  "version": "a085cb5d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `endsOn` | string (data, AAAA-MM-GG) | `2026-09-30` |
| `nextStartsOn` | string (data, AAAA-MM-GG) \| null | `2027-05-01` |
| `nextEndsOn` | string (data, AAAA-MM-GG) \| null | `2027-09-30` |
| `version` | string | `a085cb5d` |

### PUT `/api/catalog/season`

Stagioni e periodi: salva date di stagione.

#### Payload

```json
{
  "startsOn": "2026-05-01",
  "endsOn": "2026-09-25",
  "nextStartsOn": "2027-05-01",
  "nextEndsOn": "2027-09-30"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `endsOn` | string (data, AAAA-MM-GG) | `2026-09-25` |
| `nextStartsOn` | string (data, AAAA-MM-GG) | `2027-05-01` |
| `nextEndsOn` | string (data, AAAA-MM-GG) | `2027-09-30` |

#### Response

Stato: 200 OK (le date salvate).

```json
{
  "startsOn": "2026-05-01",
  "endsOn": "2026-09-25",
  "nextStartsOn": "2027-05-01",
  "nextEndsOn": "2027-09-30",
  "version": "688c1fbf"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `endsOn` | string (data, AAAA-MM-GG) | `2026-09-25` |
| `nextStartsOn` | string (data, AAAA-MM-GG) \| null | `2027-05-01` |
| `nextEndsOn` | string (data, AAAA-MM-GG) \| null | `2027-09-30` |
| `version` | string | `688c1fbf` |

### GET `/api/catalog/tariff-periods`

Stagioni e periodi: periodi della stagione corrente.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "key": 0,
    "name": "Bassa stagione",
    "ranges": [
      {
        "startsOn": "2026-05-01",
        "endsOn": "2026-06-15"
      },
      {
        "startsOn": "2026-09-01",
        "endsOn": "2026-09-30"
      }
    ],
    "version": "2fe8da16"
  },
  {
    "key": 1,
    "name": "Media stagione",
    "ranges": [
      {
        "startsOn": "2026-06-16",
        "endsOn": "2026-07-15"
      },
      {
        "startsOn": "2026-08-21",
        "endsOn": "2026-08-31"
      }
    ],
    "version": "ceafe904"
  },
  {
    "key": 2,
    "name": "Alta stagione",
    "ranges": [
      {
        "startsOn": "2026-07-16",
        "endsOn": "2026-08-10"
      }
    ],
    "version": "6e7e623f"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `0` |
| `name` | string | `Bassa stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2026-05-01", "endsOn": "2026-06-15"}, {"startsOn": "2026-09-01", "endsOn": "2026-09-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-06-15` |
| `version` | string | `2fe8da16` |

### GET `/api/catalog/next-season/tariff-periods`

Stagioni e periodi: periodi della prossima stagione.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "key": 0,
    "name": "Bassa stagione",
    "ranges": [
      {
        "startsOn": "2027-05-01",
        "endsOn": "2027-06-15"
      },
      {
        "startsOn": "2027-09-01",
        "endsOn": "2027-09-30"
      }
    ],
    "version": "b6d31f03"
  },
  {
    "key": 1,
    "name": "Alta stagione",
    "ranges": [
      {
        "startsOn": "2027-07-01",
        "endsOn": "2027-08-31"
      }
    ],
    "version": "e8e5bd86"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `0` |
| `name` | string | `Bassa stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2027-05-01", "endsOn": "2027-06-15"}, {"startsOn": "2027-09-01", "endsOn": "2027-09-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-05-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-06-15` |
| `version` | string | `b6d31f03` |

### POST `/api/catalog/tariff-periods`

Stagioni e periodi: nuovo periodo della stagione corrente.

#### Payload

```json
{
  "name": "Ferragosto",
  "ranges": [
    {
      "startsOn": "2026-08-11",
      "endsOn": "2026-08-20"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Ferragosto` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2026-08-11", "endsOn": "2026-08-20"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-08-11` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-08-20` |

#### Response

Stato: 201 Created (il periodo creato).

```json
{
  "key": 3,
  "name": "Ferragosto",
  "ranges": [
    {
      "startsOn": "2026-08-11",
      "endsOn": "2026-08-20"
    }
  ],
  "version": "173b451d"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `3` |
| `name` | string | `Ferragosto` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2026-08-11", "endsOn": "2026-08-20"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-08-11` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-08-20` |
| `version` | string | `173b451d` |

### POST `/api/catalog/next-season/tariff-periods`

Stagioni e periodi: nuovo periodo della prossima stagione.

#### Payload

```json
{
  "name": "Media stagione",
  "ranges": [
    {
      "startsOn": "2027-06-16",
      "endsOn": "2027-06-30"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Media stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2027-06-16", "endsOn": "2027-06-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-06-16` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-06-30` |

#### Response

Stato: 201 Created (il periodo creato).

```json
{
  "key": 2,
  "name": "Media stagione",
  "ranges": [
    {
      "startsOn": "2027-06-16",
      "endsOn": "2027-06-30"
    }
  ],
  "version": "32c918db"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `2` |
| `name` | string | `Media stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2027-06-16", "endsOn": "2027-06-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-06-16` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-06-30` |
| `version` | string | `32c918db` |

### DELETE `/api/catalog/tariff-periods/{key}`

Stagioni e periodi: elimina periodo della stagione corrente.

#### Payload

Nessun corpo: la key del periodo va nell'indirizzo e lo stabilimento viene dal token.

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### DELETE `/api/catalog/next-season/tariff-periods/{key}`

Stagioni e periodi: elimina periodo della prossima stagione.

#### Payload

Nessun corpo: la key del periodo va nell'indirizzo e lo stabilimento viene dal token.

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### PUT `/api/catalog/tariff-periods/{key}`

Stagioni e periodi: modifica periodo della stagione corrente.

#### Payload

```json
{
  "name": "Bassa stagione",
  "ranges": [
    {
      "startsOn": "2026-05-01",
      "endsOn": "2026-06-10"
    },
    {
      "startsOn": "2026-09-01",
      "endsOn": "2026-09-30"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Bassa stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2026-05-01", "endsOn": "2026-06-10"}, {"startsOn": "2026-09-01", "endsOn": "2026-09-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-06-10` |

#### Response

Stato: 200 OK (il periodo salvato).

```json
{
  "key": 0,
  "name": "Bassa stagione",
  "ranges": [
    {
      "startsOn": "2026-05-01",
      "endsOn": "2026-06-10"
    },
    {
      "startsOn": "2026-09-01",
      "endsOn": "2026-09-30"
    }
  ],
  "version": "894f2dc4"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `0` |
| `name` | string | `Bassa stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2026-05-01", "endsOn": "2026-06-10"}, {"startsOn": "2026-09-01", "endsOn": "2026-09-30"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-05-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-06-10` |
| `version` | string | `894f2dc4` |

### PUT `/api/catalog/next-season/tariff-periods/{key}`

Stagioni e periodi: modifica periodo della prossima stagione.

#### Payload

```json
{
  "name": "Altissima stagione",
  "ranges": [
    {
      "startsOn": "2027-07-01",
      "endsOn": "2027-08-31"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Altissima stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2027-07-01", "endsOn": "2027-08-31"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-07-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-08-31` |

#### Response

Stato: 200 OK (il periodo salvato).

```json
{
  "key": 1,
  "name": "Altissima stagione",
  "ranges": [
    {
      "startsOn": "2027-07-01",
      "endsOn": "2027-08-31"
    }
  ],
  "version": "b869e94f"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `key` | number (intero) | `1` |
| `name` | string | `Altissima stagione` |
| `ranges[]` | SeasonPeriodInterval[] (array di oggetti) | `[{"startsOn": "2027-07-01", "endsOn": "2027-08-31"}]` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-07-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-08-31` |
| `version` | string | `b869e94f` |

### POST `/api/catalog/tariff-periods/bulk-delete-ranges`

Stagioni e periodi: elimina intervalli della stagione corrente.

#### Payload

```json
{
  "ranges": [
    {
      "key": 0,
      "startsOn": "2026-09-01",
      "endsOn": "2026-09-30"
    },
    {
      "key": 1,
      "startsOn": "2026-08-21",
      "endsOn": "2026-08-31"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `ranges[]` | SeasonPeriodIntervalItem[] (array di oggetti, da 1 a 300) | `[{"key": 0, "startsOn": "2026-09-01", "endsOn": "2026-09-30"}, {"key": 1, "startsOn": "2026-08-21", "endsOn": "2026-08-31"}]` |
| `ranges[].key` | number (intero) | `0` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2026-09-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2026-09-30` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/next-season/tariff-periods/bulk-delete-ranges`

Stagioni e periodi: elimina intervalli della prossima stagione.

#### Payload

```json
{
  "ranges": [
    {
      "key": 0,
      "startsOn": "2027-09-01",
      "endsOn": "2027-09-30"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `ranges[]` | SeasonPeriodIntervalItem[] (array di oggetti, da 1 a 300) | `[{"key": 0, "startsOn": "2027-09-01", "endsOn": "2027-09-30"}]` |
| `ranges[].key` | number (intero) | `0` |
| `ranges[].startsOn` | string (data, AAAA-MM-GG) | `2027-09-01` |
| `ranges[].endsOn` | string (data, AAAA-MM-GG) | `2027-09-30` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/inventory-items-list`

Impostazioni magazzino: elenco articoli.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "lett",
  "filters": [],
  "sortField": "name",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `lett` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
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
      "lowStockThreshold": 5
    },
    {
      "idArticle": "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b",
      "name": "Lettino XL",
      "totalQuantity": 16,
      "lowStockThreshold": null
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | WarehouseSettingItem[] (array di oggetti) | `[{"idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c", "name": "Lettino", "totalQuantity": 120, "lowStockThreshold": 5}, {"idArticle": "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b", "name": "Lettino XL", "totalQuantity": 16, "lowStockThreshold": null}]` |
| `rows[].idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `rows[].name` | string | `Lettino` |
| `rows[].totalQuantity` | number (intero) | `120` |
| `rows[].lowStockThreshold` | number (intero) \| null | `5` |

### POST `/api/catalog/article-combobox`

Impostazioni magazzino: articoli per la tendina.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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

### POST `/api/catalog/add-warehouse-item`

Impostazioni magazzino: aggiunta di pezzi a un articolo.

#### Payload

```json
{
  "idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "totalQuantity": 10,
  "lowStockThreshold": null,
  "lowStockAlertEnabled": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `totalQuantity` | number (intero > 0) | `10` |
| `lowStockThreshold` | number (intero ≥ 0) \| null | `null` |
| `lowStockAlertEnabled` | boolean \| null | `null` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/inventory-items/{id}`

Impostazioni magazzino: articolo da modificare.

#### Payload

Nessun corpo: l'id dell'articolo va nell'indirizzo e lo stabilimento viene dal token (header Authorization: Bearer <accessToken>, come ogni chiamata).

#### Response

Stato: 200 OK.

```json
{
  "id": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "articleCode": "sunbed",
  "name": "Lettino",
  "totalQuantity": 120,
  "lowStockThreshold": 5,
  "lowStockAlertEnabled": true,
  "mapPlaceable": true,
  "version": "4f1c9a27"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `articleCode` | string \| null | `sunbed` |
| `name` | string | `Lettino` |
| `totalQuantity` | number (intero) | `120` |
| `lowStockThreshold` | number (intero) \| null | `5` |
| `lowStockAlertEnabled` | boolean | `true` |
| `mapPlaceable` | boolean | `true` |
| `version` | string | `4f1c9a27` |

### PUT `/api/catalog/edit-warehouse-item/{id}`

Impostazioni magazzino: modifica di un articolo.

#### Payload

```json
{
  "articleCode": "sunbed",
  "name": "Lettino",
  "totalQuantity": 110,
  "lowStockThreshold": 8,
  "lowStockAlertEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `articleCode` | string \| null | `sunbed` |
| `name` | string | `Lettino` |
| `totalQuantity` | number (intero ≥ 0) | `110` |
| `lowStockThreshold` | number (intero ≥ 0) \| null | `8` |
| `lowStockAlertEnabled` | boolean | `true` |

#### Response

Stato: 200 OK (l'articolo salvato).

```json
{
  "id": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "articleCode": "sunbed",
  "name": "Lettino",
  "totalQuantity": 110,
  "lowStockThreshold": 8,
  "lowStockAlertEnabled": true,
  "mapPlaceable": true,
  "version": "9b03e6d1"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `articleCode` | string \| null | `sunbed` |
| `name` | string | `Lettino` |
| `totalQuantity` | number (intero) | `110` |
| `lowStockThreshold` | number (intero) \| null | `8` |
| `lowStockAlertEnabled` | boolean | `true` |
| `mapPlaceable` | boolean | `true` |
| `version` | string | `9b03e6d1` |

### POST `/api/catalog/delete-warehouse-item`

Impostazioni magazzino: eliminazione di articoli.

#### Payload

```json
{
  "idItems": [
    "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
    "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idItems` | string[] (UUID, almeno uno) | `["8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c", "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/duplicate-warehouse-item`

Impostazioni magazzino: duplicazione di articoli.

#### Payload

```json
{
  "idItems": [
    "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
    "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idItems` | string[] (UUID, almeno uno) | `["8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c", "2f7d9b3e-1a4c-4e6f-8b2d-5c6a7e8f9a0b"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/lookups/countries`

Catalogo: nazioni.

#### Payload

Nessun corpo e nessun parametro: è una GET. La lingua dei nomi è quella dell'header Accept-Language.

#### Response

Stato: 200 OK.

```json
[
  {
    "code": "IT",
    "name": "Italia"
  },
  {
    "code": "FR",
    "name": "Francia"
  },
  {
    "code": "DE",
    "name": "Germania"
  },
  {
    "code": "CN",
    "name": "Cina"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `code` | string (ISO 3166-1 alpha-2) | `IT` |
| `name` | string | `Italia` |

### GET `/api/catalog/property-info/bathhouse-configuration-general`

Informazioni stabilimento: informazioni generali.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "phone": "06 1234567",
  "whatsapp": "333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "isFreeCancellation": true,
  "cancellationNoticeHours": 48,
  "petPolicy": "Allowed",
  "regulation": "<p>Vietato <strong>fumare</strong> in spiaggia.</p>",
  "version": "64c2b8d4"
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
| `phone` | string \| null | `06 1234567` |
| `whatsapp` | string \| null | `333 1234567` |
| `email` | string \| null | `info@lidodemo.example` |
| `website` | string \| null | `https://www.lidodemo.example` |
| `facebook` | string \| null | `https://www.facebook.com/lidodemo.example` |
| `instagram` | string \| null | `https://www.instagram.com/lidodemo.example` |
| `description` | string (HTML) \| null | `<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>` |
| `openingTime` | string (HH:mm) \| null | `08:30` |
| `closingTime` | string (HH:mm) \| null | `19:00` |
| `isFreeCancellation` | boolean | `true` |
| `cancellationNoticeHours` | number (intero) \| null | `48` |
| `petPolicy` | string (enum PetPolicy) | `Allowed` |
| `regulation` | string (HTML) \| null | `<p>Vietato <strong>fumare</strong> in spiaggia.</p>` |
| `version` | string | `64c2b8d4` |

### POST `/api/catalog/property-info/save-bathhouse-configuration-general`

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
  "phone": "06 1234567",
  "whatsapp": "333 1234567",
  "email": "info@lidodemo.example",
  "website": "https://www.lidodemo.example",
  "facebook": "https://www.facebook.com/lidodemo.example",
  "instagram": "https://www.instagram.com/lidodemo.example",
  "description": "<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>",
  "openingTime": "08:30",
  "closingTime": "19:00",
  "isFreeCancellation": true,
  "cancellationNoticeHours": 48,
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
| `phone` | string \| null | `06 1234567` |
| `whatsapp` | string \| null | `333 1234567` |
| `email` | string \| null | `info@lidodemo.example` |
| `website` | string \| null | `https://www.lidodemo.example` |
| `facebook` | string \| null | `https://www.facebook.com/lidodemo.example` |
| `instagram` | string \| null | `https://www.instagram.com/lidodemo.example` |
| `description` | string (HTML) \| null | `<p>Il <strong>Lido Demo</strong> offre ombrelloni e lettini in prima fila.</p><ul><li><p>Bar sulla spiaggia</p></li><li><p>Parcheggio</p></li></ul>` |
| `openingTime` | string (HH:mm) \| null | `08:30` |
| `closingTime` | string (HH:mm) \| null | `19:00` |
| `isFreeCancellation` | boolean | `true` |
| `cancellationNoticeHours` | number (intero) \| null | `48` |
| `petPolicy` | string (enum PetPolicy) | `Allowed` |
| `regulation` | string (HTML) \| null | `<p>Vietato <strong>fumare</strong> in spiaggia.</p>` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/property-info/bathhouse-configuration-services`

Informazioni stabilimento: servizi.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "services": [
    {
      "idService": 14,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/accesso-disabili.svg",
      "isSelected": true
    },
    {
      "idService": 31,
      "name": "Doccia",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/doccia.svg",
      "isSelected": true
    },
    {
      "idService": 38,
      "name": "Wifi",
      "categoryCode": "accessibility_comfort",
      "iconUrl": null,
      "isSelected": false
    }
  ],
  "version": "d17e7ba6"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `services[]` | BathhouseServiceItem[] (array di oggetti) | `[{"idService": 14, "name": "Accesso disabili", "categoryCode": "accessibility_comfort", "iconUrl": "https://media.example.com/icons/accesso-disabili.svg", "isSelected": true}, {"idService": 31, "name": "Doccia", "categoryCode": "accessibility_comfort", "iconUrl": "https://media.example.com/icons/doccia.svg", "isSelected": true}, {"idService": 38, "name": "Wifi", "categoryCode": "accessibility_comfort", "iconUrl": null, "isSelected": false}]` |
| `services[].idService` | number (intero) | `14` |
| `services[].name` | string | `Accesso disabili` |
| `services[].categoryCode` | string (enum BathhouseServiceGroup) | `accessibility_comfort` |
| `services[].iconUrl` | string \| null | `https://media.example.com/icons/accesso-disabili.svg` |
| `services[].isSelected` | boolean | `true` |
| `version` | string | `d17e7ba6` |

### POST `/api/catalog/property-info/save-bathhouse-configuration-services`

Informazioni stabilimento: salvataggio dei servizi.

#### Payload

```json
{
  "services": [
    {
      "idService": 14,
      "name": "Accesso disabili",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/accesso-disabili.svg",
      "isSelected": true
    },
    {
      "idService": 31,
      "name": "Doccia",
      "categoryCode": "accessibility_comfort",
      "iconUrl": "https://media.example.com/icons/doccia.svg",
      "isSelected": false
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `services[]` | BathhouseServiceItem[] (array di oggetti) | `[{"idService": 14, "name": "Accesso disabili", "categoryCode": "accessibility_comfort", "iconUrl": "https://media.example.com/icons/accesso-disabili.svg", "isSelected": true}, {"idService": 31, "name": "Doccia", "categoryCode": "accessibility_comfort", "iconUrl": "https://media.example.com/icons/doccia.svg", "isSelected": false}]` |
| `services[].idService` | number (intero) | `14` |
| `services[].name` | string | `Accesso disabili` |
| `services[].categoryCode` | string (enum BathhouseServiceGroup) | `accessibility_comfort` |
| `services[].iconUrl` | string \| null | `https://media.example.com/icons/accesso-disabili.svg` |
| `services[].isSelected` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/property-info/bathhouse-configuration-media`

Informazioni stabilimento: media.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp"
    },
    {
      "idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp"
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdfUrl": "https://media.example.com/lido-demo/listino-prezzi.pdf",
  "priceListPdfFileName": "listino-prezzi.pdf",
  "menuPdfUrl": "https://media.example.com/lido-demo/menu.pdf",
  "menuPdfFileName": "menu.pdf",
  "version": "0dae65a2"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | BathhousePhoto[] (array di oggetti) | `[{"idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp"}, {"idPhoto": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp"}]` |
| `photos[].idPhoto` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string | `https://media.example.com/lido-demo/bar.webp` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number (decimale, 6 cifre) \| null | `44.063812` |
| `longitude` | number (decimale, 6 cifre) \| null | `12.580143` |
| `priceListPdfUrl` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `priceListPdfFileName` | string \| null | `listino-prezzi.pdf` |
| `menuPdfUrl` | string \| null | `https://media.example.com/lido-demo/menu.pdf` |
| `menuPdfFileName` | string \| null | `menu.pdf` |
| `version` | string | `0dae65a2` |

### POST `/api/catalog/property-info/save-bathhouse-configuration-media`

Informazioni stabilimento: salvataggio del link del tour virtuale e della posizione.

#### Payload

```json
{
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number (decimale, 6 cifre) \| null | `44.063812` |
| `longitude` | number (decimale, 6 cifre) \| null | `12.580143` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/bar-service-list`

Servizi: servizi bar.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "acqua",
  "filters": [],
  "sortField": "price",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `acqua` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `price` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c08",
      "serviceName": "Acqua naturale 1L",
      "category": "Drinks",
      "price": 1.2
    },
    {
      "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c09",
      "serviceName": "Acqua frizzante 1L",
      "category": "Drinks",
      "price": 1.2
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | BarServiceItem[] (array di oggetti) | `[{"idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c08", "serviceName": "Acqua naturale 1L", "category": "Drinks", "price": 1.2}, {"idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c09", "serviceName": "Acqua frizzante 1L", "category": "Drinks", "price": 1.2}]` |
| `rows[].idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c08` |
| `rows[].serviceName` | string \| null | `Acqua naturale 1L` |
| `rows[].category` | string (enum BarServiceCategory) \| null | `Drinks` |
| `rows[].price` | number (decimale, €) \| null | `1.2` |

### POST `/api/catalog/services/create-bar-service`

Servizi: creazione servizio bar.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "serviceName": "Caffè ginseng",
      "description": "Caffè al ginseng in tazza piccola"
    },
    {
      "language": "en",
      "serviceName": "Ginseng coffee",
      "description": "Ginseng coffee in a small cup"
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "isHighlighted": true,
  "price": 1.5,
  "vatRate": 10,
  "variants": [
    {
      "variantName": "Grande",
      "price": 0.5
    }
  ],
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 1.5
    },
    {
      "idPeriod": 1,
      "price": 1.8
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": "09:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "12:00",
      "maxBookingPerHourQuantity": 2,
      "maxBookingPerDayQuantity": 5,
      "maxPerSlot": null,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Caffè ginseng", "description": "Caffè al ginseng in tazza piccola"}, {"language": "en", "serviceName": "Ginseng coffee", "description": "Ginseng coffee in a small cup"}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Caffè ginseng` |
| `translations[].description` | string \| null | `Caffè al ginseng in tazza piccola` |
| `category` | string (enum BarServiceCategory) \| null | `Bar` |
| `icon` | string (enum BarServiceIcon) \| null | `Coffee` |
| `isHighlighted` | boolean | `true` |
| `price` | number (decimale, €) | `1.5` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `variants[]` | ServiceVariant[] (array di oggetti) | `[{"variantName": "Grande", "price": 0.5}]` |
| `variants[].variantName` | string | `Grande` |
| `variants[].price` | number (decimale, €) | `0.5` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 1.5}, {"idPeriod": 1, "price": 1.8}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `1.5` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-07-01T00:00:00.000Z", "dateTo": "2026-07-31T00:00:00.000Z", "weekdays": [], "managementStartTime": "09:00", "managementEndTime": "12:00", "onlineStartTime": "09:00", "onlineEndTime": "12:00", "maxBookingPerHourQuantity": 2, "maxBookingPerDayQuantity": 5, "maxPerSlot": null, "intervalMinutes": 30, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 2}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-31T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `[]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `12:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `12:00` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `2` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `5` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `30` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `2` |

#### Response

Stato: 201 Created.

Il corpo è solo l'id del servizio bar creato, come stringa JSON (tra virgolette, Content-Type application/json).

```json
"8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c13"
```

### POST `/api/catalog/services/{id}/image`

Servizi: caricamento dell'immagine di un servizio.

#### Payload

```json
{
  "file": "caffe.webp (image/webp, 0,4 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `caffe.webp (image/webp, 0,4 MB)` |

#### Response

Stato: 200 OK (il servizio, che il frontend non legge).

Nessun corpo.

### DELETE `/api/catalog/services/{id}/image`

Servizi: eliminazione dell'immagine di un servizio.

#### Payload

Nessun corpo: l'id del servizio è nella rotta.

#### Response

Stato: 200 OK (il servizio, che il frontend non legge).

Nessun corpo.

### GET `/api/catalog/services/get-bar-service-general-info?idBarService={idBarService}`

Servizi: servizio bar, tab Info Generali.

#### Payload

Nessun corpo: è una GET, l'id del servizio bar va nella query string.

```text
/api/catalog/services/get-bar-service-general-info?idBarService=8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |

#### Response

Stato: 200 OK.

```json
{
  "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Caffè",
      "description": "Espresso in tazzina."
    },
    {
      "language": "en",
      "serviceName": "Coffee",
      "description": "Espresso in a small cup."
    }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "imageUrl": null,
  "isHighlighted": true,
  "version": "feac3ca6"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |
| `translations[]` | ServiceTranslation[] (array di oggetti) | `[{"language": "it", "serviceName": "Caffè", "description": "Espresso in tazzina."}, {"language": "en", "serviceName": "Coffee", "description": "Espresso in a small cup."}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Caffè` |
| `translations[].description` | string \| null | `Espresso in tazzina.` |
| `category` | string (enum BarServiceCategory) \| null | `Bar` |
| `icon` | string \| null | `Coffee` |
| `imageUrl` | string (URL) \| null | `null` |
| `isHighlighted` | boolean | `true` |
| `version` | string | `feac3ca6` |

### GET `/api/catalog/services/get-bar-service-prices?idBarService={idBarService}`

Servizi: servizio bar, tab Prezzi.

#### Payload

Nessun corpo: è una GET, l'id del servizio bar va nella query string.

```text
/api/catalog/services/get-bar-service-prices?idBarService=8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |

#### Response

Stato: 200 OK.

```json
{
  "price": 1,
  "vatRate": 10,
  "variants": [
    {
      "variantName": "Doppio",
      "price": 0.8
    }
  ],
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 1.2
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "version": "37b6a44c"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `price` | number (decimale, €) | `1` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `variants[]` | ServiceVariant[] (array di oggetti) | `[{"variantName": "Doppio", "price": 0.8}]` |
| `variants[].variantName` | string | `Doppio` |
| `variants[].price` | number (decimale, €) | `0.8` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 1.2}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `1.2` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `version` | string | `37b6a44c` |

### GET `/api/catalog/services/get-bar-service-booking-options?idBarService={idBarService}`

Servizi: servizio bar, tab Prenotazioni.

#### Payload

Nessun corpo: è una GET, l'id del servizio bar va nella query string.

```text
/api/catalog/services/get-bar-service-booking-options?idBarService=8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |

#### Response

Stato: 200 OK.

```json
{
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "maxPerSlot": 5,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 4
    }
  ],
  "version": "96f5de5b"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-07-01T00:00:00.000Z", "dateTo": "2026-07-31T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": "08:00", "managementEndTime": "12:00", "onlineStartTime": "08:30", "onlineEndTime": "11:30", "maxBookingPerHourQuantity": 20, "maxBookingPerDayQuantity": 80, "maxPerSlot": 5, "intervalMinutes": 30, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 4}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-31T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `08:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `12:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `08:30` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `11:30` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `20` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `80` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `5` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `30` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `4` |
| `version` | string | `96f5de5b` |

### POST `/api/catalog/services/update-bar-service-general`

Servizi: servizio bar, salvataggio Info Generali.

#### Payload

```json
{
  "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Caffè espresso",
      "description": "Espresso in tazzina."
    },
    {
      "language": "en",
      "serviceName": "Coffee",
      "description": "Espresso in a small cup."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "category": "Bar",
  "icon": "Coffee",
  "isHighlighted": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Caffè espresso", "description": "Espresso in tazzina."}, {"language": "en", "serviceName": "Coffee", "description": "Espresso in a small cup."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Caffè espresso` |
| `translations[].description` | string \| null | `Espresso in tazzina.` |
| `category` | string (enum BarServiceCategory) \| null | `Bar` |
| `icon` | string \| null | `Coffee` |
| `isHighlighted` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-bar-service-prices`

Servizi: servizio bar, salvataggio Prezzi.

#### Payload

```json
{
  "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
  "price": 1.3,
  "vatRate": 10,
  "variants": [
    {
      "variantName": "Doppio",
      "price": 0.8
    }
  ],
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 1.2
    },
    {
      "idPeriod": 1,
      "price": 1.5
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |
| `price` | number (decimale, €) | `1.3` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `variants[]` | ServiceVariant[] (array di oggetti) | `[{"variantName": "Doppio", "price": 0.8}]` |
| `variants[].variantName` | string | `Doppio` |
| `variants[].price` | number (decimale, €) | `0.8` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 1.2}, {"idPeriod": 1, "price": 1.5}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `1.2` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-bar-service-booking`

Servizi: servizio bar, salvataggio Prenotazioni.

#### Payload

```json
{
  "idBarService": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
  "bookingOptions": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": "08:00",
      "managementEndTime": "12:00",
      "onlineStartTime": "08:30",
      "onlineEndTime": "11:30",
      "maxBookingPerHourQuantity": 20,
      "maxBookingPerDayQuantity": 80,
      "maxPerSlot": 5,
      "intervalMinutes": 30,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 4
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarService` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-07-01T00:00:00.000Z", "dateTo": "2026-07-31T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": "08:00", "managementEndTime": "12:00", "onlineStartTime": "08:30", "onlineEndTime": "11:30", "maxBookingPerHourQuantity": 20, "maxBookingPerDayQuantity": 80, "maxPerSlot": 5, "intervalMinutes": 30, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 4}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-07-31T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `08:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `12:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `08:30` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `11:30` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `20` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `80` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `5` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `30` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `4` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/delete-bar-service-item`

Servizi: eliminazione servizi bar.

#### Payload

```json
{
  "idBarServices": [
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c08",
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c09"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarServices` | string[] (UUID, almeno uno) | `["8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c08", "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c09"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/duplicate-bar-service-item`

Servizi: duplicazione servizi bar.

#### Payload

```json
{
  "idBarServices": [
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idBarServices` | string[] (UUID, almeno uno) | `["8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/standard-service-list`

Servizi: servizi.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "doccia",
  "filters": [],
  "sortField": "serviceName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `doccia` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `serviceName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 1,
  "rows": [
    {
      "idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
      "serviceName": "Doccia calda",
      "price": 3
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | StandardServiceItem[] (array di oggetti) | `[{"idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01", "serviceName": "Doccia calda", "price": 3}]` |
| `rows[].idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `rows[].serviceName` | string \| null | `Doccia calda` |
| `rows[].price` | number (decimale, €) \| null | `3` |

### POST `/api/catalog/services/create-standard-service`

Servizi: creazione servizio.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "serviceName": "Doccia calda",
      "description": "Acqua calda nelle docce dello stabilimento."
    },
    {
      "language": "en",
      "serviceName": "Hot shower",
      "description": "Hot water in the showers of the beach club."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Shower",
  "isHighlighted": true,
  "price": 3,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 2.5
    },
    {
      "idPeriod": 1,
      "price": 3.5
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-06-15T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": "09:00",
      "managementEndTime": "19:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "18:00",
      "maxBookingPerHourQuantity": 6,
      "maxBookingPerDayQuantity": 50,
      "maxPerSlot": null,
      "intervalMinutes": 15,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Doccia calda", "description": "Acqua calda nelle docce dello stabilimento."}, {"language": "en", "serviceName": "Hot shower", "description": "Hot water in the showers of the beach club."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Doccia calda` |
| `translations[].description` | string \| null | `Acqua calda nelle docce dello stabilimento.` |
| `icon` | string (enum StandardServiceIcon) \| null | `Shower` |
| `isHighlighted` | boolean | `true` |
| `price` | number (decimale, €) | `3` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 2.5}, {"idPeriod": 1, "price": 3.5}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `2.5` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-15T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": [], "managementStartTime": "09:00", "managementEndTime": "19:00", "onlineStartTime": "09:00", "onlineEndTime": "18:00", "maxBookingPerHourQuantity": 6, "maxBookingPerDayQuantity": 50, "maxPerSlot": null, "intervalMinutes": 15, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 2}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-06-15T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `[]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `18:00` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `6` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `50` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `15` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `2` |

#### Response

Stato: 201 Created.

Il corpo è solo l'id del servizio creato, come stringa JSON (tra virgolette, Content-Type application/json).

```json
"9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d04"
```

### GET `/api/catalog/services/get-standard-service-general-info?idStandardService={idStandardService}`

Servizi: servizio, tab Info Generali.

#### Payload

Nessun corpo: è una GET, l'id del servizio va nella query string.

```text
/api/catalog/services/get-standard-service-general-info?idStandardService=9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |

#### Response

Stato: 200 OK.

```json
{
  "idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Doccia calda",
      "description": "Acqua calda nelle docce dello stabilimento."
    },
    {
      "language": "en",
      "serviceName": "Hot shower",
      "description": "Hot water in the showers of the beach club."
    }
  ],
  "icon": "Shower",
  "imageUrl": null,
  "isHighlighted": true,
  "version": "339ffdae"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `translations[]` | ServiceTranslation[] (array di oggetti) | `[{"language": "it", "serviceName": "Doccia calda", "description": "Acqua calda nelle docce dello stabilimento."}, {"language": "en", "serviceName": "Hot shower", "description": "Hot water in the showers of the beach club."}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Doccia calda` |
| `translations[].description` | string \| null | `Acqua calda nelle docce dello stabilimento.` |
| `icon` | string \| null | `Shower` |
| `imageUrl` | string (URL) \| null | `null` |
| `isHighlighted` | boolean | `true` |
| `version` | string | `339ffdae` |

### GET `/api/catalog/services/get-standard-service-prices?idStandardService={idStandardService}`

Servizi: servizio, tab Prezzi.

#### Payload

Nessun corpo: è una GET, l'id del servizio va nella query string.

```text
/api/catalog/services/get-standard-service-prices?idStandardService=9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |

#### Response

Stato: 200 OK.

```json
{
  "price": 3,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 2.5
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "version": "52f0d596"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `price` | number (decimale, €) | `3` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 2.5}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `2.5` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `version` | string | `52f0d596` |

### GET `/api/catalog/services/get-standard-service-booking-options?idStandardService={idStandardService}`

Servizi: servizio, tab Prenotazioni.

#### Payload

Nessun corpo: è una GET, l'id del servizio va nella query string.

```text
/api/catalog/services/get-standard-service-booking-options?idStandardService=9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |

#### Response

Stato: 200 OK.

```json
{
  "bookingOptions": [
    {
      "dateFrom": "2026-06-15T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": "09:00",
      "managementEndTime": "19:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "18:00",
      "maxBookingPerHourQuantity": 6,
      "maxBookingPerDayQuantity": 50,
      "maxPerSlot": null,
      "intervalMinutes": 15,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ],
  "version": "f71a86f3"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-15T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": "09:00", "managementEndTime": "19:00", "onlineStartTime": "09:00", "onlineEndTime": "18:00", "maxBookingPerHourQuantity": 6, "maxBookingPerDayQuantity": 50, "maxPerSlot": null, "intervalMinutes": 15, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 2}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-06-15T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `18:00` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `6` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `50` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `15` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `2` |
| `version` | string | `f71a86f3` |

### POST `/api/catalog/services/update-standard-service-general`

Servizi: servizio, salvataggio Info Generali.

#### Payload

```json
{
  "idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Doccia calda",
      "description": "Acqua calda nelle docce dello stabilimento."
    },
    {
      "language": "en",
      "serviceName": "Hot shower",
      "description": "Hot water in the showers of the beach club."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Shower",
  "isHighlighted": false
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Doccia calda", "description": "Acqua calda nelle docce dello stabilimento."}, {"language": "en", "serviceName": "Hot shower", "description": "Hot water in the showers of the beach club."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Doccia calda` |
| `translations[].description` | string \| null | `Acqua calda nelle docce dello stabilimento.` |
| `icon` | string \| null | `Shower` |
| `isHighlighted` | boolean | `false` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-standard-service-prices`

Servizi: servizio, salvataggio Prezzi.

#### Payload

```json
{
  "idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
  "price": 4,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "periodPrices": [
    {
      "idPeriod": 0,
      "price": 2.5
    },
    {
      "idPeriod": 1,
      "price": 3.5
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `price` | number (decimale, €) | `4` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) | `[{"idPeriod": 0, "price": 2.5}, {"idPeriod": 1, "price": 3.5}]` |
| `periodPrices[].idPeriod` | number (intero) | `0` |
| `periodPrices[].price` | number (decimale, €) \| null | `2.5` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-standard-service-booking`

Servizi: servizio, salvataggio Prenotazioni.

#### Payload

```json
{
  "idStandardService": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
  "bookingOptions": [
    {
      "dateFrom": "2026-06-15T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": "09:00",
      "managementEndTime": "19:00",
      "onlineStartTime": "09:00",
      "onlineEndTime": "18:00",
      "maxBookingPerHourQuantity": 6,
      "maxBookingPerDayQuantity": 50,
      "maxPerSlot": null,
      "intervalMinutes": 15,
      "minItemPerBookingQuantity": 1,
      "maxItemPerBookingQuantity": 2
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardService` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-15T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": "09:00", "managementEndTime": "19:00", "onlineStartTime": "09:00", "onlineEndTime": "18:00", "maxBookingPerHourQuantity": 6, "maxBookingPerDayQuantity": 50, "maxPerSlot": null, "intervalMinutes": 15, "minItemPerBookingQuantity": 1, "maxItemPerBookingQuantity": 2}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null | `2026-06-15T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `18:00` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `6` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `50` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `15` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `1` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `2` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/delete-standard-service-item`

Servizi: eliminazione servizi.

#### Payload

```json
{
  "idStandardServices": [
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardServices` | string[] (UUID, almeno uno) | `["9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01", "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/duplicate-standard-service-item`

Servizi: duplicazione servizi.

#### Payload

```json
{
  "idStandardServices": [
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idStandardServices` | string[] (UUID, almeno uno) | `["9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/hourly-service-list`

Servizi: servizi orari.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "pisc",
  "filters": [],
  "sortField": "serviceName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `pisc` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `serviceName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 1,
  "rows": [
    {
      "idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a03",
      "serviceName": "Piscina"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | HourlyServiceItem[] (array di oggetti) | `[{"idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a03", "serviceName": "Piscina"}]` |
| `rows[].idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a03` |
| `rows[].serviceName` | string \| null | `Piscina` |

### POST `/api/catalog/services/delete-hourly-service-item`

Servizi: eliminazione servizi orari.

#### Payload

```json
{
  "idHourlyServices": [
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyServices` | string[] (UUID, almeno uno) | `["4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01", "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/duplicate-hourly-service-item`

Servizi: duplicazione servizi orari.

#### Payload

```json
{
  "idHourlyServices": [
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyServices` | string[] (UUID, almeno uno) | `["4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/create-hourly-service`

Servizi Orari: creazione servizio orario.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "serviceName": "Sup",
      "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."
    },
    {
      "language": "en",
      "serviceName": "Sup",
      "description": "Stand up paddle board with paddle, rented by the hour."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Surfing",
  "isHighlighted": false,
  "price": 15,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "availabilityOptions": [],
  "periodAvailabilities": [
    {
      "idPeriod": 0,
      "availabilityOptions": [
        {
          "startTime": "09:00",
          "endTime": "19:00",
          "daysOfWeek": [
            1,
            2,
            3,
            4,
            5,
            6,
            7
          ],
          "timeSlotPrices": [
            {
              "timeSlot": "TenMinutes",
              "isEnabled": true,
              "price": 3
            },
            {
              "timeSlot": "FifteenMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "ThirtyMinutes",
              "isEnabled": true,
              "price": 8
            },
            {
              "timeSlot": "FortyFiveMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "OneHour",
              "isEnabled": true,
              "price": 15
            },
            {
              "timeSlot": "OneHourThirtyMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "TwoHours",
              "isEnabled": true,
              "price": 25
            },
            {
              "timeSlot": "ThreeHours",
              "isEnabled": false,
              "price": null
            }
          ],
          "isCustomTimeSlotEnabled": true,
          "customTimeSlotMinuteQuantity": 20,
          "customTimeSlotPrice": 6,
          "vatRate": 22,
          "maxBookingPerTimeSlotQuantity": 4,
          "hasFixedTimeSlots": false
        }
      ]
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "bookingOptions": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [],
      "managementStartTime": null,
      "managementEndTime": null,
      "onlineStartTime": null,
      "onlineEndTime": null,
      "maxBookingPerHourQuantity": null,
      "maxBookingPerDayQuantity": null,
      "maxPerSlot": null,
      "intervalMinutes": null,
      "minItemPerBookingQuantity": null,
      "maxItemPerBookingQuantity": null,
      "availabilityOption": {
        "startTime": "09:00",
        "endTime": "19:00",
        "daysOfWeek": [
          1,
          2,
          3,
          4,
          5,
          6,
          7
        ],
        "timeSlotPrices": [
          {
            "timeSlot": "TenMinutes",
            "isEnabled": true,
            "price": 3
          },
          {
            "timeSlot": "FifteenMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "ThirtyMinutes",
            "isEnabled": true,
            "price": 8
          },
          {
            "timeSlot": "FortyFiveMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "OneHour",
            "isEnabled": true,
            "price": 15
          },
          {
            "timeSlot": "OneHourThirtyMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "TwoHours",
            "isEnabled": true,
            "price": 25
          },
          {
            "timeSlot": "ThreeHours",
            "isEnabled": false,
            "price": null
          }
        ],
        "isCustomTimeSlotEnabled": true,
        "customTimeSlotMinuteQuantity": 20,
        "customTimeSlotPrice": 6,
        "vatRate": 22,
        "maxBookingPerTimeSlotQuantity": 4,
        "hasFixedTimeSlots": false
      }
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Sup", "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."}, {"language": "en", "serviceName": "Sup", "description": "Stand up paddle board with paddle, rented by the hour."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Sup` |
| `translations[].description` | string \| null | `Tavola da stand up paddle con pagaia, a noleggio a ore.` |
| `icon` | string (enum StandardServiceIcon) \| null | `Surfing` |
| `isHighlighted` | boolean | `false` |
| `price` | number (decimale, €) | `15` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[]` |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) | `[{"idPeriod": 0, "availabilityOptions": [{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]}]` |
| `periodAvailabilities[].idPeriod` | number (intero) | `0` |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]` |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null | `09:00` |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null | `19:00` |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null | `22` |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean | `false` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `bookingOptions[]` | HourlyServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": [], "managementStartTime": null, "managementEndTime": null, "onlineStartTime": null, "onlineEndTime": null, "maxBookingPerHourQuantity": null, "maxBookingPerDayQuantity": null, "maxPerSlot": null, "intervalMinutes": null, "minItemPerBookingQuantity": null, "maxItemPerBookingQuantity": null, "availabilityOption": {"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `[]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `null` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].availabilityOption` | ServiceAvailabilityOption (oggetto) \| null | `{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}` |
| `bookingOptions[].availabilityOption.startTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].availabilityOption.endTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].isEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `bookingOptions[].availabilityOption.isCustomTimeSlotEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `bookingOptions[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `bookingOptions[].availabilityOption.vatRate` | number (decimale, %) \| null | `22` |
| `bookingOptions[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `bookingOptions[].availabilityOption.hasFixedTimeSlots` | boolean | `false` |

#### Response

Stato: 201 Created.

Il corpo è solo l'id del servizio orario creato, come stringa JSON (tra virgolette, Content-Type application/json).

```json
"4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a04"
```

### GET `/api/catalog/services/get-hourly-service-general-info?idHourlyService={idHourlyService}`

Servizi Orari: servizio orario, tab Info Generali.

#### Payload

Nessun corpo: è una GET, l'id del servizio orario va nella query string.

```text
/api/catalog/services/get-hourly-service-general-info?idHourlyService=4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |

#### Response

Stato: 200 OK.

```json
{
  "idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Sup",
      "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."
    },
    {
      "language": "en",
      "serviceName": "Sup",
      "description": "Stand up paddle board with paddle, rented by the hour."
    }
  ],
  "icon": "Surfing",
  "imageUrl": null,
  "isHighlighted": false,
  "version": "c293ede7"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |
| `translations[]` | ServiceTranslation[] (array di oggetti) | `[{"language": "it", "serviceName": "Sup", "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."}, {"language": "en", "serviceName": "Sup", "description": "Stand up paddle board with paddle, rented by the hour."}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Sup` |
| `translations[].description` | string \| null | `Tavola da stand up paddle con pagaia, a noleggio a ore.` |
| `icon` | string \| null | `Surfing` |
| `imageUrl` | string (URL) \| null | `null` |
| `isHighlighted` | boolean | `false` |
| `version` | string | `c293ede7` |

### GET `/api/catalog/services/get-hourly-service-prices?idHourlyService={idHourlyService}`

Servizi Orari: servizio orario, tab Prezzi.

#### Payload

Nessun corpo: è una GET, l'id del servizio orario va nella query string.

```text
/api/catalog/services/get-hourly-service-prices?idHourlyService=4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |

#### Response

Stato: 200 OK.

```json
{
  "price": 15,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "availabilityOptions": [],
  "periodAvailabilities": [
    {
      "idPeriod": 0,
      "availabilityOptions": [
        {
          "startTime": "09:00",
          "endTime": "19:00",
          "daysOfWeek": [
            1,
            2,
            3,
            4,
            5,
            6,
            7
          ],
          "timeSlotPrices": [
            {
              "timeSlot": "TenMinutes",
              "isEnabled": true,
              "price": 3
            },
            {
              "timeSlot": "FifteenMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "ThirtyMinutes",
              "isEnabled": true,
              "price": 8
            },
            {
              "timeSlot": "FortyFiveMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "OneHour",
              "isEnabled": true,
              "price": 15
            },
            {
              "timeSlot": "OneHourThirtyMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "TwoHours",
              "isEnabled": true,
              "price": 25
            },
            {
              "timeSlot": "ThreeHours",
              "isEnabled": false,
              "price": null
            }
          ],
          "isCustomTimeSlotEnabled": true,
          "customTimeSlotMinuteQuantity": 20,
          "customTimeSlotPrice": 6,
          "vatRate": 22,
          "maxBookingPerTimeSlotQuantity": 4,
          "hasFixedTimeSlots": false
        }
      ]
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true,
  "version": "afabd0e9"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `price` | number (decimale, €) | `15` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[]` |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) | `[{"idPeriod": 0, "availabilityOptions": [{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]}]` |
| `periodAvailabilities[].idPeriod` | number (intero) | `0` |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]` |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null | `09:00` |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null | `19:00` |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null | `22` |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean | `false` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `version` | string | `afabd0e9` |

### GET `/api/catalog/services/get-hourly-service-booking-options?idHourlyService={idHourlyService}`

Servizi Orari: servizio orario, tab Prenotazioni.

#### Payload

Nessun corpo: è una GET, l'id del servizio orario va nella query string.

```text
/api/catalog/services/get-hourly-service-booking-options?idHourlyService=4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |

#### Response

Stato: 200 OK.

```json
{
  "bookingOptions": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": null,
      "managementEndTime": null,
      "onlineStartTime": null,
      "onlineEndTime": null,
      "maxBookingPerHourQuantity": null,
      "maxBookingPerDayQuantity": null,
      "maxPerSlot": null,
      "intervalMinutes": null,
      "minItemPerBookingQuantity": null,
      "maxItemPerBookingQuantity": null,
      "availabilityOption": {
        "startTime": "09:00",
        "endTime": "19:00",
        "daysOfWeek": [
          1,
          2,
          3,
          4,
          5,
          6,
          7
        ],
        "timeSlotPrices": [
          {
            "timeSlot": "TenMinutes",
            "isEnabled": true,
            "price": 3
          },
          {
            "timeSlot": "FifteenMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "ThirtyMinutes",
            "isEnabled": true,
            "price": 8
          },
          {
            "timeSlot": "FortyFiveMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "OneHour",
            "isEnabled": true,
            "price": 15
          },
          {
            "timeSlot": "OneHourThirtyMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "TwoHours",
            "isEnabled": true,
            "price": 25
          },
          {
            "timeSlot": "ThreeHours",
            "isEnabled": false,
            "price": null
          }
        ],
        "isCustomTimeSlotEnabled": true,
        "customTimeSlotMinuteQuantity": 20,
        "customTimeSlotPrice": 6,
        "vatRate": 22,
        "maxBookingPerTimeSlotQuantity": 4,
        "hasFixedTimeSlots": false
      }
    }
  ],
  "version": "dec5d66e"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `bookingOptions[]` | HourlyServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": null, "managementEndTime": null, "onlineStartTime": null, "onlineEndTime": null, "maxBookingPerHourQuantity": null, "maxBookingPerDayQuantity": null, "maxPerSlot": null, "intervalMinutes": null, "minItemPerBookingQuantity": null, "maxItemPerBookingQuantity": null, "availabilityOption": {"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `null` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].availabilityOption` | ServiceAvailabilityOption (oggetto) \| null | `{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}` |
| `bookingOptions[].availabilityOption.startTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].availabilityOption.endTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].isEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `bookingOptions[].availabilityOption.isCustomTimeSlotEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `bookingOptions[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `bookingOptions[].availabilityOption.vatRate` | number (decimale, %) \| null | `22` |
| `bookingOptions[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `bookingOptions[].availabilityOption.hasFixedTimeSlots` | boolean | `false` |
| `version` | string | `dec5d66e` |

### POST `/api/catalog/services/update-hourly-service-general`

Servizi Orari: servizio orario, salvataggio Info Generali.

#### Payload

```json
{
  "idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Sup",
      "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."
    },
    {
      "language": "en",
      "serviceName": "Sup",
      "description": "Stand up paddle board with paddle, rented by the hour."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Surfing",
  "isHighlighted": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Sup", "description": "Tavola da stand up paddle con pagaia, a noleggio a ore."}, {"language": "en", "serviceName": "Sup", "description": "Stand up paddle board with paddle, rented by the hour."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Sup` |
| `translations[].description` | string \| null | `Tavola da stand up paddle con pagaia, a noleggio a ore.` |
| `icon` | string \| null | `Surfing` |
| `isHighlighted` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-hourly-service-prices`

Servizi Orari: servizio orario, salvataggio Prezzi.

#### Payload

```json
{
  "idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
  "price": 15,
  "vatRate": 22,
  "isPeriodPricingEnabled": true,
  "availabilityOptions": [],
  "periodAvailabilities": [
    {
      "idPeriod": 0,
      "availabilityOptions": [
        {
          "startTime": "09:00",
          "endTime": "19:00",
          "daysOfWeek": [
            1,
            2,
            3,
            4,
            5,
            6,
            7
          ],
          "timeSlotPrices": [
            {
              "timeSlot": "TenMinutes",
              "isEnabled": true,
              "price": 3
            },
            {
              "timeSlot": "FifteenMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "ThirtyMinutes",
              "isEnabled": true,
              "price": 8
            },
            {
              "timeSlot": "FortyFiveMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "OneHour",
              "isEnabled": true,
              "price": 15
            },
            {
              "timeSlot": "OneHourThirtyMinutes",
              "isEnabled": false,
              "price": null
            },
            {
              "timeSlot": "TwoHours",
              "isEnabled": true,
              "price": 25
            },
            {
              "timeSlot": "ThreeHours",
              "isEnabled": false,
              "price": null
            }
          ],
          "isCustomTimeSlotEnabled": true,
          "customTimeSlotMinuteQuantity": 20,
          "customTimeSlotPrice": 6,
          "vatRate": 22,
          "maxBookingPerTimeSlotQuantity": 4,
          "hasFixedTimeSlots": false
        }
      ]
    }
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isOnlinePurchaseEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |
| `price` | number (decimale, €) | `15` |
| `vatRate` | number (decimale, %) \| null | `22` |
| `isPeriodPricingEnabled` | boolean | `true` |
| `availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[]` |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) | `[{"idPeriod": 0, "availabilityOptions": [{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]}]` |
| `periodAvailabilities[].idPeriod` | number (intero) | `0` |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) | `[{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}]` |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null | `09:00` |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null | `19:00` |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean | `true` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null | `22` |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean | `false` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isOnlinePurchaseEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/services/update-hourly-service-booking`

Servizi Orari: servizio orario, salvataggio Prenotazioni.

#### Payload

```json
{
  "idHourlyService": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
  "bookingOptions": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-15T00:00:00.000Z",
      "weekdays": [
        "Saturday",
        "Sunday"
      ],
      "managementStartTime": null,
      "managementEndTime": null,
      "onlineStartTime": null,
      "onlineEndTime": null,
      "maxBookingPerHourQuantity": null,
      "maxBookingPerDayQuantity": null,
      "maxPerSlot": null,
      "intervalMinutes": null,
      "minItemPerBookingQuantity": null,
      "maxItemPerBookingQuantity": null,
      "availabilityOption": {
        "startTime": "09:00",
        "endTime": "19:00",
        "daysOfWeek": [
          1,
          2,
          3,
          4,
          5,
          6,
          7
        ],
        "timeSlotPrices": [
          {
            "timeSlot": "TenMinutes",
            "isEnabled": true,
            "price": 3
          },
          {
            "timeSlot": "FifteenMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "ThirtyMinutes",
            "isEnabled": true,
            "price": 8
          },
          {
            "timeSlot": "FortyFiveMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "OneHour",
            "isEnabled": true,
            "price": 15
          },
          {
            "timeSlot": "OneHourThirtyMinutes",
            "isEnabled": false,
            "price": null
          },
          {
            "timeSlot": "TwoHours",
            "isEnabled": true,
            "price": 25
          },
          {
            "timeSlot": "ThreeHours",
            "isEnabled": false,
            "price": null
          }
        ],
        "isCustomTimeSlotEnabled": true,
        "customTimeSlotMinuteQuantity": 20,
        "customTimeSlotPrice": 6,
        "vatRate": 22,
        "maxBookingPerTimeSlotQuantity": 4,
        "hasFixedTimeSlots": false
      }
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idHourlyService` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |
| `bookingOptions[]` | HourlyServiceBookingOption[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-15T00:00:00.000Z", "weekdays": ["Saturday", "Sunday"], "managementStartTime": null, "managementEndTime": null, "onlineStartTime": null, "onlineEndTime": null, "maxBookingPerHourQuantity": null, "maxBookingPerDayQuantity": null, "maxPerSlot": null, "intervalMinutes": null, "minItemPerBookingQuantity": null, "maxItemPerBookingQuantity": null, "availabilityOption": {"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}}]` |
| `bookingOptions[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `bookingOptions[].dateTo` | string (ISO 8601, UTC) \| null | `2026-09-15T00:00:00.000Z` |
| `bookingOptions[].weekdays` | string[] (enum Weekday) | `["Saturday", "Sunday"]` |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null | `null` |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxPerSlot` | number (intero) \| null | `null` |
| `bookingOptions[].intervalMinutes` | number (intero) \| null | `null` |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingOptions[].availabilityOption` | ServiceAvailabilityOption (oggetto) \| null | `{"startTime": "09:00", "endTime": "19:00", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlotPrices": [{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}], "isCustomTimeSlotEnabled": true, "customTimeSlotMinuteQuantity": 20, "customTimeSlotPrice": 6, "vatRate": 22, "maxBookingPerTimeSlotQuantity": 4, "hasFixedTimeSlots": false}` |
| `bookingOptions[].availabilityOption.startTime` | string (HH:mm) \| null | `09:00` |
| `bookingOptions[].availabilityOption.endTime` | string (HH:mm) \| null | `19:00` |
| `bookingOptions[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) | `[{"timeSlot": "TenMinutes", "isEnabled": true, "price": 3}, {"timeSlot": "FifteenMinutes", "isEnabled": false, "price": null}, {"timeSlot": "ThirtyMinutes", "isEnabled": true, "price": 8}, {"timeSlot": "FortyFiveMinutes", "isEnabled": false, "price": null}, {"timeSlot": "OneHour", "isEnabled": true, "price": 15}, {"timeSlot": "OneHourThirtyMinutes", "isEnabled": false, "price": null}, {"timeSlot": "TwoHours", "isEnabled": true, "price": 25}, {"timeSlot": "ThreeHours", "isEnabled": false, "price": null}]` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) | `TenMinutes` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].isEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null | `3` |
| `bookingOptions[].availabilityOption.isCustomTimeSlotEnabled` | boolean | `true` |
| `bookingOptions[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null | `20` |
| `bookingOptions[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null | `6` |
| `bookingOptions[].availabilityOption.vatRate` | number (decimale, %) \| null | `22` |
| `bookingOptions[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null | `4` |
| `bookingOptions[].availabilityOption.hasFixedTimeSlots` | boolean | `false` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/automatic-prices/automatic-price-list`

Servizi: prezzi automatici.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "cop",
  "filters": [],
  "sortField": "price",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `cop` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `price` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 1,
  "rows": [
    {
      "idAutomaticPrice": "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01",
      "serviceName": "Coperto",
      "price": 2
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | AutomaticPriceItem[] (array di oggetti) | `[{"idAutomaticPrice": "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01", "serviceName": "Coperto", "price": 2}]` |
| `rows[].idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |
| `rows[].serviceName` | string \| null | `Coperto` |
| `rows[].price` | number (decimale, €) \| null | `2` |

### GET `/api/catalog/automatic-prices/get-automatic-price-services`

Prezzi Automatici: servizi a cui si applicano.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "barServices": [
    {
      "id": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
      "value": "Caffè"
    },
    {
      "id": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02",
      "value": "Croissant"
    }
  ],
  "standardServices": [
    {
      "id": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01",
      "value": "Doccia calda"
    }
  ],
  "hourlyServices": [
    {
      "id": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01",
      "value": "Sup"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `barServices[]` | ComboboxItem[] (array di oggetti) | `[{"id": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01", "value": "Caffè"}, {"id": "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02", "value": "Croissant"}]` |
| `barServices[].id` | string (UUID) | `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01` |
| `barServices[].value` | string | `Caffè` |
| `standardServices[]` | ComboboxItem[] (array di oggetti) | `[{"id": "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01", "value": "Doccia calda"}]` |
| `standardServices[].id` | string (UUID) | `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01` |
| `standardServices[].value` | string | `Doccia calda` |
| `hourlyServices[]` | ComboboxItem[] (array di oggetti) | `[{"id": "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01", "value": "Sup"}]` |
| `hourlyServices[].id` | string (UUID) | `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01` |
| `hourlyServices[].value` | string | `Sup` |

### POST `/api/catalog/automatic-prices/create-automatic-price`

Prezzi Automatici: creazione prezzo automatico.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "serviceName": "Menu pranzo",
      "description": "Prezzo fisso per il pranzo al bar."
    },
    {
      "language": "en",
      "serviceName": "Lunch menu",
      "description": "Fixed price for lunch at the bar."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Cutlery",
  "price": 12,
  "vatRate": 10,
  "isOnlinePurchaseEnabled": true,
  "validityPeriods": [
    {
      "dateFrom": "2026-08-01T00:00:00.000Z",
      "dateTo": "2026-08-31T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3
      ],
      "timeSlots": [
        {
          "startTime": "09:00",
          "endTime": "12:00"
        },
        {
          "startTime": "15:00",
          "endTime": "17:00"
        }
      ]
    }
  ],
  "isAllServicesEnabled": false,
  "idBarServices": [
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"
  ],
  "idStandardServices": [
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"
  ],
  "idHourlyServices": [
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"
  ],
  "isPricePerServiceEnabled": false,
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Menu pranzo", "description": "Prezzo fisso per il pranzo al bar."}, {"language": "en", "serviceName": "Lunch menu", "description": "Fixed price for lunch at the bar."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Menu pranzo` |
| `translations[].description` | string \| null | `Prezzo fisso per il pranzo al bar.` |
| `icon` | string (enum BarServiceIcon o StandardServiceIcon) \| null | `Cutlery` |
| `price` | number (decimale, €) | `12` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) | `[{"dateFrom": "2026-08-01T00:00:00.000Z", "dateTo": "2026-08-31T00:00:00.000Z", "daysOfWeek": [1, 2, 3], "timeSlots": [{"startTime": "09:00", "endTime": "12:00"}, {"startTime": "15:00", "endTime": "17:00"}]}]` |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-08-01T00:00:00.000Z` |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null | `2026-08-31T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3]` |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) | `[{"startTime": "09:00", "endTime": "12:00"}, {"startTime": "15:00", "endTime": "17:00"}]` |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) | `09:00` |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) | `12:00` |
| `isAllServicesEnabled` | boolean | `false` |
| `idBarServices` | string[] (UUID) | `["8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01", "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"]` |
| `idStandardServices` | string[] (UUID) | `["9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"]` |
| `idHourlyServices` | string[] (UUID) | `["4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"]` |
| `isPricePerServiceEnabled` | boolean | `false` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |

#### Response

Stato: 201 Created.

Il corpo è solo l'id del prezzo automatico creato, come stringa JSON (tra virgolette, Content-Type application/json).

```json
"7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d03"
```

### GET `/api/catalog/automatic-prices/get-automatic-price-general-info?idAutomaticPrice={idAutomaticPrice}`

Prezzi Automatici: prezzo automatico, tab Info Generali.

#### Payload

Nessun corpo: è una GET, l'id del prezzo automatico va nella query string.

```text
/api/catalog/automatic-prices/get-automatic-price-general-info?idAutomaticPrice=7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |

#### Response

Stato: 200 OK.

```json
{
  "idAutomaticPrice": "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Coperto",
      "description": "Coperto a persona per i servizi del bar."
    },
    {
      "language": "en",
      "serviceName": "Cover charge",
      "description": "Cover charge per person for the bar services."
    }
  ],
  "icon": "Cutlery",
  "imageUrl": null,
  "version": "390a8384"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |
| `translations[]` | ServiceTranslation[] (array di oggetti) | `[{"language": "it", "serviceName": "Coperto", "description": "Coperto a persona per i servizi del bar."}, {"language": "en", "serviceName": "Cover charge", "description": "Cover charge per person for the bar services."}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Coperto` |
| `translations[].description` | string \| null | `Coperto a persona per i servizi del bar.` |
| `icon` | string \| null | `Cutlery` |
| `imageUrl` | string (URL) \| null | `null` |
| `version` | string | `390a8384` |

### GET `/api/catalog/automatic-prices/get-automatic-price-prices-validity?idAutomaticPrice={idAutomaticPrice}`

Prezzi Automatici: prezzo automatico, tab Prezzi e Validità.

#### Payload

Nessun corpo: è una GET, l'id del prezzo automatico va nella query string.

```text
/api/catalog/automatic-prices/get-automatic-price-prices-validity?idAutomaticPrice=7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |

#### Response

Stato: 200 OK.

```json
{
  "price": 2,
  "vatRate": 10,
  "isOnlinePurchaseEnabled": true,
  "validityPeriods": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-30T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3,
        4,
        5,
        6,
        7
      ],
      "timeSlots": [
        {
          "startTime": "12:00",
          "endTime": "15:00"
        },
        {
          "startTime": "19:00",
          "endTime": "23:00"
        }
      ]
    }
  ],
  "isAllServicesEnabled": false,
  "idBarServices": [
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"
  ],
  "idStandardServices": [
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"
  ],
  "idHourlyServices": [
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"
  ],
  "isPricePerServiceEnabled": true,
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "version": "5221df56"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `price` | number (decimale, €) | `2` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-30T00:00:00.000Z", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlots": [{"startTime": "12:00", "endTime": "15:00"}, {"startTime": "19:00", "endTime": "23:00"}]}]` |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null | `2026-09-30T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) | `[{"startTime": "12:00", "endTime": "15:00"}, {"startTime": "19:00", "endTime": "23:00"}]` |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) | `12:00` |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) | `15:00` |
| `isAllServicesEnabled` | boolean | `false` |
| `idBarServices` | string[] (UUID) | `["8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01", "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"]` |
| `idStandardServices` | string[] (UUID) | `["9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"]` |
| `idHourlyServices` | string[] (UUID) | `["4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"]` |
| `isPricePerServiceEnabled` | boolean | `true` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `version` | string | `5221df56` |

### POST `/api/catalog/automatic-prices/update-automatic-price-general`

Prezzi Automatici: prezzo automatico, salvataggio Info Generali.

#### Payload

```json
{
  "idAutomaticPrice": "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01",
  "translations": [
    {
      "language": "it",
      "serviceName": "Coperto",
      "description": "Coperto a persona per i servizi del bar."
    },
    {
      "language": "en",
      "serviceName": "Cover charge",
      "description": "Cover charge per person for the bar services."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "icon": "Cutlery"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Coperto", "description": "Coperto a persona per i servizi del bar."}, {"language": "en", "serviceName": "Cover charge", "description": "Cover charge per person for the bar services."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Coperto` |
| `translations[].description` | string \| null | `Coperto a persona per i servizi del bar.` |
| `icon` | string \| null | `Cutlery` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/automatic-prices/update-automatic-price-prices-validity`

Prezzi Automatici: prezzo automatico, salvataggio Prezzi e Validità.

#### Payload

```json
{
  "idAutomaticPrice": "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01",
  "price": 2.5,
  "vatRate": 10,
  "isOnlinePurchaseEnabled": true,
  "validityPeriods": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-30T00:00:00.000Z",
      "daysOfWeek": [
        1,
        2,
        3,
        4,
        5,
        6,
        7
      ],
      "timeSlots": [
        {
          "startTime": "12:00",
          "endTime": "15:00"
        },
        {
          "startTime": "19:00",
          "endTime": "23:00"
        }
      ]
    }
  ],
  "isAllServicesEnabled": false,
  "idBarServices": [
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01",
    "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"
  ],
  "idStandardServices": [
    "9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"
  ],
  "idHourlyServices": [
    "4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"
  ],
  "isPricePerServiceEnabled": true,
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrice` | string (UUID) | `7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01` |
| `price` | number (decimale, €) | `2.5` |
| `vatRate` | number (decimale, %) \| null | `10` |
| `isOnlinePurchaseEnabled` | boolean | `true` |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-30T00:00:00.000Z", "daysOfWeek": [1, 2, 3, 4, 5, 6, 7], "timeSlots": [{"startTime": "12:00", "endTime": "15:00"}, {"startTime": "19:00", "endTime": "23:00"}]}]` |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-01T00:00:00.000Z` |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null | `2026-09-30T00:00:00.000Z` |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) | `[1, 2, 3, 4, 5, 6, 7]` |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) | `[{"startTime": "12:00", "endTime": "15:00"}, {"startTime": "19:00", "endTime": "23:00"}]` |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) | `12:00` |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) | `15:00` |
| `isAllServicesEnabled` | boolean | `false` |
| `idBarServices` | string[] (UUID) | `["8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01", "8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02"]` |
| `idStandardServices` | string[] (UUID) | `["9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01"]` |
| `idHourlyServices` | string[] (UUID) | `["4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01"]` |
| `isPricePerServiceEnabled` | boolean | `true` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/automatic-prices/{id}/image`

Prezzi Automatici: immagine di un prezzo automatico.

#### Payload

```json
{
  "file": "coperto.webp (image/webp, 0,4 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `coperto.webp (image/webp, 0,4 MB)` |

#### Response

Stato: 200 OK (il prezzo automatico, che il frontend non legge).

Nessun corpo.

### DELETE `/api/catalog/automatic-prices/{id}/image`

Prezzi Automatici: eliminazione dell'immagine di un prezzo automatico.

#### Payload

Nessun corpo: l'id del prezzo automatico è nella rotta.

#### Response

Stato: 200 OK (il prezzo automatico, che il frontend non legge).

Nessun corpo.

### POST `/api/catalog/automatic-prices/delete-automatic-price-item`

Servizi: eliminazione prezzi automatici.

#### Payload

```json
{
  "idAutomaticPrices": [
    "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01",
    "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrices` | string[] (UUID, almeno uno) | `["7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01", "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/automatic-prices/duplicate-automatic-price-item`

Servizi: duplicazione prezzi automatici.

#### Payload

```json
{
  "idAutomaticPrices": [
    "7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAutomaticPrices` | string[] (UUID, almeno uno) | `["7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/table-services/table-service-list`

Servizi: prenotazione servizio tavoli.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "cen",
  "filters": [],
  "sortField": "serviceName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `cen` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `serviceName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 1,
  "rows": [
    {
      "idTableService": "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a702",
      "serviceName": "Cena"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | TableServiceItem[] (array di oggetti) | `[{"idTableService": "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a702", "serviceName": "Cena"}]` |
| `rows[].idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a702` |
| `rows[].serviceName` | string \| null | `Cena` |

### POST `/api/catalog/table-services/delete-table-service-item`

Servizi: eliminazione servizi tavoli.

#### Payload

```json
{
  "idTableServices": [
    "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701",
    "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a702"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableServices` | string[] (UUID, almeno uno) | `["2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701", "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a702"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/table-services/duplicate-table-service-item`

Servizi: duplicazione servizi tavoli.

#### Payload

```json
{
  "idTableServices": [
    "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableServices` | string[] (UUID, almeno uno) | `["2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/table-services/create-table-service`

Prenotazione tavoli: creazione servizio tavoli.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "serviceName": "Aperitivo",
      "description": "Tavoli per l'aperitivo al tramonto."
    },
    {
      "language": "en",
      "serviceName": "Aperitif",
      "description": "Tables for the sunset aperitif."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "isHighlighted": true,
  "availabilities": [
    {
      "dateFrom": "2026-07-01T00:00:00.000Z",
      "dateTo": "2026-07-31T00:00:00.000Z",
      "days": [
        {
          "dayOfWeek": 1,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 2,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 3,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 4,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 5,
          "isEnabled": true,
          "startTime": "18:00",
          "endTime": "21:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 6,
          "isEnabled": true,
          "startTime": "18:00",
          "endTime": "21:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 7,
          "isEnabled": true,
          "startTime": "18:00",
          "endTime": "21:00",
          "otherTimeSlots": []
        }
      ]
    }
  ],
  "availabilityMode": "Tables",
  "tableGroups": [
    {
      "tableQuantity": 10,
      "seatPerTableQuantity": 2
    },
    {
      "tableQuantity": 6,
      "seatPerTableQuantity": 4
    }
  ],
  "totalSeatQuantity": null,
  "averageMealMinutes": 90,
  "maxBookingPerTimeSlotQuantity": 8,
  "minSeatPerBookingQuantity": null,
  "maxSeatPerBookingQuantity": null,
  "bookingPayment": "Deposit",
  "depositMode": "PerPerson",
  "depositPercentage": 20,
  "depositAmount": null,
  "isOnlineBookingEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Aperitivo", "description": "Tavoli per l'aperitivo al tramonto."}, {"language": "en", "serviceName": "Aperitif", "description": "Tables for the sunset aperitif."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Aperitivo` |
| `translations[].description` | string \| null | `Tavoli per l'aperitivo al tramonto.` |
| `isHighlighted` | boolean | `true` |
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) | `[{"dateFrom": "2026-07-01T00:00:00.000Z", "dateTo": "2026-07-31T00:00:00.000Z", "days": [{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}]}]` |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) | `2026-07-01T00:00:00.000Z` |
| `availabilities[].dateTo` | string (ISO 8601, UTC) | `2026-07-31T00:00:00.000Z` |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) | `[{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "18:00", "endTime": "21:00", "otherTimeSlots": []}]` |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) | `1` |
| `availabilities[].days[].isEnabled` | boolean | `false` |
| `availabilities[].days[].startTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].endTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].otherTimeSlots[]` | TableServiceTimeSlot[] (array di oggetti) | `[]` |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) | `Tables` |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) | `[{"tableQuantity": 10, "seatPerTableQuantity": 2}, {"tableQuantity": 6, "seatPerTableQuantity": 4}]` |
| `tableGroups[].tableQuantity` | number (intero) | `10` |
| `tableGroups[].seatPerTableQuantity` | number (intero) | `2` |
| `totalSeatQuantity` | number (intero) \| null | `null` |
| `averageMealMinutes` | number (intero, minuti) \| null | `90` |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null | `8` |
| `minSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `maxSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) | `Deposit` |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) | `PerPerson` |
| `depositPercentage` | number (decimale, %) \| null | `20` |
| `depositAmount` | number (decimale, €) \| null | `null` |
| `isOnlineBookingEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/table-services/get-table-service-general-info?idTableService={idTableService}`

Prenotazione tavoli: servizio tavoli, tab Info Generali.

#### Payload

Nessun corpo: è una GET, l'id del servizio tavoli va nella query string.

```text
/api/catalog/table-services/get-table-service-general-info?idTableService=2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701` |

#### Response

Stato: 200 OK.

```json
{
  "idTableService": "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701",
  "translations": [
    {
      "language": "it",
      "serviceName": "Pranzo",
      "description": "Tavoli del ristorante per il pranzo."
    },
    {
      "language": "en",
      "serviceName": "Lunch",
      "description": "Restaurant tables for lunch."
    }
  ],
  "isHighlighted": true,
  "version": "05be1153"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701` |
| `translations[]` | ServiceTranslation[] (array di oggetti) | `[{"language": "it", "serviceName": "Pranzo", "description": "Tavoli del ristorante per il pranzo."}, {"language": "en", "serviceName": "Lunch", "description": "Restaurant tables for lunch."}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Pranzo` |
| `translations[].description` | string \| null | `Tavoli del ristorante per il pranzo.` |
| `isHighlighted` | boolean | `true` |
| `version` | string | `05be1153` |

### GET `/api/catalog/table-services/get-table-service-availability-bookings?idTableService={idTableService}`

Prenotazione tavoli: servizio tavoli, tab Disponibilità e Prenotazioni.

#### Payload

Nessun corpo: è una GET, l'id del servizio tavoli va nella query string.

```text
/api/catalog/table-services/get-table-service-availability-bookings?idTableService=2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701` |

#### Response

Stato: 200 OK.

```json
{
  "availabilities": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-30T00:00:00.000Z",
      "days": [
        {
          "dayOfWeek": 1,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 2,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 3,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 4,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 5,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 6,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 7,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        }
      ]
    }
  ],
  "availabilityMode": "Tables",
  "tableGroups": [
    {
      "tableQuantity": 10,
      "seatPerTableQuantity": 2
    },
    {
      "tableQuantity": 8,
      "seatPerTableQuantity": 4
    },
    {
      "tableQuantity": 6,
      "seatPerTableQuantity": 5
    }
  ],
  "totalSeatQuantity": null,
  "averageMealMinutes": 90,
  "maxBookingPerTimeSlotQuantity": 8,
  "minSeatPerBookingQuantity": null,
  "maxSeatPerBookingQuantity": null,
  "bookingPayment": "Deposit",
  "depositMode": "Total",
  "depositPercentage": 20,
  "depositAmount": null,
  "isOnlineBookingEnabled": true,
  "version": "75208201"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-30T00:00:00.000Z", "days": [{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}]}]` |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) | `2026-06-01T00:00:00.000Z` |
| `availabilities[].dateTo` | string (ISO 8601, UTC) | `2026-09-30T00:00:00.000Z` |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) | `[{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}]` |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) | `1` |
| `availabilities[].days[].isEnabled` | boolean | `false` |
| `availabilities[].days[].startTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].endTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].otherTimeSlots[]` | TableServiceTimeSlot[] (array di oggetti) | `[]` |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) | `Tables` |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) | `[{"tableQuantity": 10, "seatPerTableQuantity": 2}, {"tableQuantity": 8, "seatPerTableQuantity": 4}, {"tableQuantity": 6, "seatPerTableQuantity": 5}]` |
| `tableGroups[].tableQuantity` | number (intero) | `10` |
| `tableGroups[].seatPerTableQuantity` | number (intero) | `2` |
| `totalSeatQuantity` | number (intero) \| null | `null` |
| `averageMealMinutes` | number (intero, minuti) \| null | `90` |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null | `8` |
| `minSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `maxSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) | `Deposit` |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) | `Total` |
| `depositPercentage` | number (decimale, %) \| null | `20` |
| `depositAmount` | number (decimale, €) \| null | `null` |
| `isOnlineBookingEnabled` | boolean | `true` |
| `version` | string | `75208201` |

### POST `/api/catalog/table-services/update-table-service-general`

Prenotazione tavoli: servizio tavoli, salvataggio Info Generali.

#### Payload

```json
{
  "idTableService": "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701",
  "translations": [
    {
      "language": "it",
      "serviceName": "Pranzo",
      "description": "Tavoli del ristorante per il pranzo."
    },
    {
      "language": "en",
      "serviceName": "Lunch",
      "description": "Restaurant tables for lunch."
    },
    {
      "language": "fr",
      "serviceName": null,
      "description": null
    },
    {
      "language": "de",
      "serviceName": null,
      "description": null
    },
    {
      "language": "es",
      "serviceName": null,
      "description": null
    },
    {
      "language": "ar",
      "serviceName": null,
      "description": null
    },
    {
      "language": "zh",
      "serviceName": null,
      "description": null
    }
  ],
  "isHighlighted": false
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701` |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "serviceName": "Pranzo", "description": "Tavoli del ristorante per il pranzo."}, {"language": "en", "serviceName": "Lunch", "description": "Restaurant tables for lunch."}, {"language": "fr", "serviceName": null, "description": null}, {"language": "de", "serviceName": null, "description": null}, {"language": "es", "serviceName": null, "description": null}, {"language": "ar", "serviceName": null, "description": null}, {"language": "zh", "serviceName": null, "description": null}]` |
| `translations[].language` | string | `it` |
| `translations[].serviceName` | string \| null | `Pranzo` |
| `translations[].description` | string \| null | `Tavoli del ristorante per il pranzo.` |
| `isHighlighted` | boolean | `false` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/table-services/update-table-service-availability-bookings`

Prenotazione tavoli: servizio tavoli, salvataggio Disponibilità e Prenotazioni.

#### Payload

```json
{
  "idTableService": "2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701",
  "availabilities": [
    {
      "dateFrom": "2026-06-01T00:00:00.000Z",
      "dateTo": "2026-09-30T00:00:00.000Z",
      "days": [
        {
          "dayOfWeek": 1,
          "isEnabled": false,
          "startTime": null,
          "endTime": null,
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 2,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 3,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 4,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 5,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 6,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        },
        {
          "dayOfWeek": 7,
          "isEnabled": true,
          "startTime": "12:00",
          "endTime": "15:00",
          "otherTimeSlots": []
        }
      ]
    }
  ],
  "availabilityMode": "Tables",
  "tableGroups": [
    {
      "tableQuantity": 10,
      "seatPerTableQuantity": 2
    },
    {
      "tableQuantity": 8,
      "seatPerTableQuantity": 4
    },
    {
      "tableQuantity": 6,
      "seatPerTableQuantity": 5
    }
  ],
  "totalSeatQuantity": null,
  "averageMealMinutes": 90,
  "maxBookingPerTimeSlotQuantity": 8,
  "minSeatPerBookingQuantity": null,
  "maxSeatPerBookingQuantity": null,
  "bookingPayment": "Deposit",
  "depositMode": "Total",
  "depositPercentage": 20,
  "depositAmount": null,
  "isOnlineBookingEnabled": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idTableService` | string (UUID) | `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701` |
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) | `[{"dateFrom": "2026-06-01T00:00:00.000Z", "dateTo": "2026-09-30T00:00:00.000Z", "days": [{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}]}]` |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) | `2026-06-01T00:00:00.000Z` |
| `availabilities[].dateTo` | string (ISO 8601, UTC) | `2026-09-30T00:00:00.000Z` |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) | `[{"dayOfWeek": 1, "isEnabled": false, "startTime": null, "endTime": null, "otherTimeSlots": []}, {"dayOfWeek": 2, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 3, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 4, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 5, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 6, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}, {"dayOfWeek": 7, "isEnabled": true, "startTime": "12:00", "endTime": "15:00", "otherTimeSlots": []}]` |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) | `1` |
| `availabilities[].days[].isEnabled` | boolean | `false` |
| `availabilities[].days[].startTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].endTime` | string (HH:mm) \| null | `null` |
| `availabilities[].days[].otherTimeSlots[]` | TableServiceTimeSlot[] (array di oggetti) | `[]` |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) | `Tables` |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) | `[{"tableQuantity": 10, "seatPerTableQuantity": 2}, {"tableQuantity": 8, "seatPerTableQuantity": 4}, {"tableQuantity": 6, "seatPerTableQuantity": 5}]` |
| `tableGroups[].tableQuantity` | number (intero) | `10` |
| `tableGroups[].seatPerTableQuantity` | number (intero) | `2` |
| `totalSeatQuantity` | number (intero) \| null | `null` |
| `averageMealMinutes` | number (intero, minuti) \| null | `90` |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null | `8` |
| `minSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `maxSeatPerBookingQuantity` | number (intero) \| null | `null` |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) | `Deposit` |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) | `Total` |
| `depositPercentage` | number (decimale, %) \| null | `20` |
| `depositAmount` | number (decimale, €) \| null | `null` |
| `isOnlineBookingEnabled` | boolean | `true` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/map/settings`

Configura mappa: impostazioni.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "rows": 6,
  "columns": 16,
  "tiltDegrees": 8,
  "backgroundColor": "#F0D8CC",
  "background": {
    "iconId": null,
    "name": "BackgroundBeach",
    "url": null,
    "isCustom": false
  },
  "icons": {
    "umbrella": {
      "free": {
        "iconId": null,
        "name": "Umbrella3",
        "url": null,
        "isDefault": false
      },
      "occupied": {
        "iconId": null,
        "name": "MixedSpot4",
        "url": null,
        "isDefault": false
      }
    },
    "customArticles": [
      {
        "variant": 1,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot1",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": "MixedSpot13",
            "url": null,
            "isDefault": false
          }
        }
      },
      {
        "variant": 2,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot8",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": null,
            "url": null,
            "isDefault": true
          }
        }
      }
    ]
  },
  "resetDeletesBookings": false,
  "backgroundVersion": "953d1375",
  "configurationVersion": "74bc5be3"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `rows` | number (intero) | `6` |
| `columns` | number (intero) | `16` |
| `tiltDegrees` | number (intero) | `8` |
| `backgroundColor` | string (#RRGGBB) \| null | `#F0D8CC` |
| `background` | MapBackground \| null | `{"iconId": null, "name": "BackgroundBeach", "url": null, "isCustom": false}` |
| `background.iconId` | string (UUID) \| null | `null` |
| `background.name` | string \| null | `BackgroundBeach` |
| `background.url` | string (URL) \| null | `null` |
| `background.isCustom` | boolean | `false` |
| `icons` | MapIcons | `{"umbrella": {"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}, "customArticles": [{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]}` |
| `icons.umbrella` | MapIconPair | `{"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}` |
| `icons.umbrella.free` | MapIcon | `{"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}` |
| `icons.umbrella.free.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.free.name` | string \| null | `Umbrella3` |
| `icons.umbrella.free.url` | string (URL) \| null | `null` |
| `icons.umbrella.free.isDefault` | boolean | `false` |
| `icons.umbrella.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}` |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.occupied.name` | string \| null | `MixedSpot4` |
| `icons.umbrella.occupied.url` | string (URL) \| null | `null` |
| `icons.umbrella.occupied.isDefault` | boolean | `false` |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) | `[{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]` |
| `icons.customArticles[].variant` | number (intero) | `1` |
| `icons.customArticles[].icons` | MapIconPair | `{"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}` |
| `icons.customArticles[].icons.free` | MapIcon | `{"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.free.name` | string \| null | `MixedSpot1` |
| `icons.customArticles[].icons.free.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.free.isDefault` | boolean | `false` |
| `icons.customArticles[].icons.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.occupied.name` | string \| null | `MixedSpot13` |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.occupied.isDefault` | boolean | `false` |
| `resetDeletesBookings` | boolean | `false` |
| `backgroundVersion` | string | `953d1375` |
| `configurationVersion` | string | `74bc5be3` |

### PUT `/api/catalog/map/update-background`

Configura mappa: salvataggio dello sfondo.

#### Payload

```json
{
  "backgroundName": "BackgroundBoardwalk"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `backgroundName` | string | `BackgroundBoardwalk` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### PUT `/api/catalog/map/update-configuration`

Configura mappa: salvataggio del reticolo.

#### Payload

```json
{
  "rowQuantity": 6,
  "maxRowItemQuantity": 16,
  "tiltDegrees": 8,
  "umbrellaIcons": {
    "freeIcon": "Umbrella3",
    "occupiedIcon": "MixedSpot4"
  },
  "customArticleIcons": [
    {
      "variant": 1,
      "freeIcon": "MixedSpot1",
      "occupiedIcon": "MixedSpot13"
    },
    {
      "variant": 2,
      "freeIcon": "MixedSpot8",
      "occupiedIcon": null
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `rowQuantity` | number (intero) | `6` |
| `maxRowItemQuantity` | number (intero) | `16` |
| `tiltDegrees` | number (intero) | `8` |
| `umbrellaIcons` | MapElementIcons | `{"freeIcon": "Umbrella3", "occupiedIcon": "MixedSpot4"}` |
| `umbrellaIcons.freeIcon` | string \| null | `Umbrella3` |
| `umbrellaIcons.occupiedIcon` | string \| null | `MixedSpot4` |
| `customArticleIcons[]` | MapArticleIcons[] (array di oggetti) | `[{"variant": 1, "freeIcon": "MixedSpot1", "occupiedIcon": "MixedSpot13"}, {"variant": 2, "freeIcon": "MixedSpot8", "occupiedIcon": null}]` |
| `customArticleIcons[].variant` | number (intero) | `1` |
| `customArticleIcons[].freeIcon` | string \| null | `MixedSpot1` |
| `customArticleIcons[].occupiedIcon` | string \| null | `MixedSpot13` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/map/background`

Configura mappa: caricamento dello sfondo personalizzato.

#### Payload

```json
{
  "file": "sfondo-lido-azzurro.png (image/png, 1,8 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `sfondo-lido-azzurro.png (image/png, 1,8 MB)` |

#### Response

Stato: 200 OK.

```json
{
  "rows": 6,
  "columns": 16,
  "tiltDegrees": 8,
  "backgroundColor": "#F0D8CC",
  "background": {
    "iconId": null,
    "name": null,
    "url": "https://cdn.summerbooking.it/backgrounds/sfondo-lido-azzurro.png",
    "isCustom": true
  },
  "icons": {
    "umbrella": {
      "free": {
        "iconId": null,
        "name": "Umbrella3",
        "url": null,
        "isDefault": false
      },
      "occupied": {
        "iconId": null,
        "name": "MixedSpot4",
        "url": null,
        "isDefault": false
      }
    },
    "customArticles": [
      {
        "variant": 1,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot1",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": "MixedSpot13",
            "url": null,
            "isDefault": false
          }
        }
      },
      {
        "variant": 2,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot8",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": null,
            "url": null,
            "isDefault": true
          }
        }
      }
    ]
  },
  "resetDeletesBookings": false,
  "backgroundVersion": "b0a7f6ba",
  "configurationVersion": "74bc5be3"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `rows` | number (intero) | `6` |
| `columns` | number (intero) | `16` |
| `tiltDegrees` | number (intero) | `8` |
| `backgroundColor` | string (#RRGGBB) \| null | `#F0D8CC` |
| `background` | MapBackground \| null | `{"iconId": null, "name": null, "url": "https://cdn.summerbooking.it/backgrounds/sfondo-lido-azzurro.png", "isCustom": true}` |
| `background.iconId` | string (UUID) \| null | `null` |
| `background.name` | string \| null | `null` |
| `background.url` | string (URL) \| null | `https://cdn.summerbooking.it/backgrounds/sfondo-lido-azzurro.png` |
| `background.isCustom` | boolean | `true` |
| `icons` | MapIcons | `{"umbrella": {"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}, "customArticles": [{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]}` |
| `icons.umbrella` | MapIconPair | `{"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}` |
| `icons.umbrella.free` | MapIcon | `{"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}` |
| `icons.umbrella.free.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.free.name` | string \| null | `Umbrella3` |
| `icons.umbrella.free.url` | string (URL) \| null | `null` |
| `icons.umbrella.free.isDefault` | boolean | `false` |
| `icons.umbrella.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}` |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.occupied.name` | string \| null | `MixedSpot4` |
| `icons.umbrella.occupied.url` | string (URL) \| null | `null` |
| `icons.umbrella.occupied.isDefault` | boolean | `false` |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) | `[{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]` |
| `icons.customArticles[].variant` | number (intero) | `1` |
| `icons.customArticles[].icons` | MapIconPair | `{"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}` |
| `icons.customArticles[].icons.free` | MapIcon | `{"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.free.name` | string \| null | `MixedSpot1` |
| `icons.customArticles[].icons.free.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.free.isDefault` | boolean | `false` |
| `icons.customArticles[].icons.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.occupied.name` | string \| null | `MixedSpot13` |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.occupied.isDefault` | boolean | `false` |
| `resetDeletesBookings` | boolean | `false` |
| `backgroundVersion` | string | `b0a7f6ba` |
| `configurationVersion` | string | `74bc5be3` |

### DELETE `/api/catalog/map/background`

Configura mappa: eliminazione dello sfondo personalizzato.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "rows": 6,
  "columns": 16,
  "tiltDegrees": 8,
  "backgroundColor": "#F0D8CC",
  "background": null,
  "icons": {
    "umbrella": {
      "free": {
        "iconId": null,
        "name": "Umbrella3",
        "url": null,
        "isDefault": false
      },
      "occupied": {
        "iconId": null,
        "name": "MixedSpot4",
        "url": null,
        "isDefault": false
      }
    },
    "customArticles": [
      {
        "variant": 1,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot1",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": "MixedSpot13",
            "url": null,
            "isDefault": false
          }
        }
      },
      {
        "variant": 2,
        "icons": {
          "free": {
            "iconId": null,
            "name": "MixedSpot8",
            "url": null,
            "isDefault": false
          },
          "occupied": {
            "iconId": null,
            "name": null,
            "url": null,
            "isDefault": true
          }
        }
      }
    ]
  },
  "resetDeletesBookings": false,
  "backgroundVersion": "3828b8cd",
  "configurationVersion": "74bc5be3"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `rows` | number (intero) | `6` |
| `columns` | number (intero) | `16` |
| `tiltDegrees` | number (intero) | `8` |
| `backgroundColor` | string (#RRGGBB) \| null | `#F0D8CC` |
| `background` | MapBackground \| null | `null` |
| `icons` | MapIcons | `{"umbrella": {"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}, "customArticles": [{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]}` |
| `icons.umbrella` | MapIconPair | `{"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}` |
| `icons.umbrella.free` | MapIcon | `{"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}` |
| `icons.umbrella.free.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.free.name` | string \| null | `Umbrella3` |
| `icons.umbrella.free.url` | string (URL) \| null | `null` |
| `icons.umbrella.free.isDefault` | boolean | `false` |
| `icons.umbrella.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}` |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.umbrella.occupied.name` | string \| null | `MixedSpot4` |
| `icons.umbrella.occupied.url` | string (URL) \| null | `null` |
| `icons.umbrella.occupied.isDefault` | boolean | `false` |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) | `[{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]` |
| `icons.customArticles[].variant` | number (intero) | `1` |
| `icons.customArticles[].icons` | MapIconPair | `{"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}` |
| `icons.customArticles[].icons.free` | MapIcon | `{"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.free.name` | string \| null | `MixedSpot1` |
| `icons.customArticles[].icons.free.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.free.isDefault` | boolean | `false` |
| `icons.customArticles[].icons.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}` |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null | `null` |
| `icons.customArticles[].icons.occupied.name` | string \| null | `MixedSpot13` |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null | `null` |
| `icons.customArticles[].icons.occupied.isDefault` | boolean | `false` |
| `resetDeletesBookings` | boolean | `false` |
| `backgroundVersion` | string | `3828b8cd` |
| `configurationVersion` | string | `74bc5be3` |

### POST `/api/catalog/map/reset`

Configura mappa: reimpostazione della mappa.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "settings": {
    "rows": 1,
    "columns": 1,
    "tiltDegrees": 0,
    "backgroundColor": "#F0D8CC",
    "background": {
      "iconId": null,
      "name": "BackgroundBeach",
      "url": null,
      "isCustom": false
    },
    "icons": {
      "umbrella": {
        "free": {
          "iconId": null,
          "name": "Umbrella3",
          "url": null,
          "isDefault": false
        },
        "occupied": {
          "iconId": null,
          "name": "MixedSpot4",
          "url": null,
          "isDefault": false
        }
      },
      "customArticles": [
        {
          "variant": 1,
          "icons": {
            "free": {
              "iconId": null,
              "name": "MixedSpot1",
              "url": null,
              "isDefault": false
            },
            "occupied": {
              "iconId": null,
              "name": "MixedSpot13",
              "url": null,
              "isDefault": false
            }
          }
        },
        {
          "variant": 2,
          "icons": {
            "free": {
              "iconId": null,
              "name": "MixedSpot8",
              "url": null,
              "isDefault": false
            },
            "occupied": {
              "iconId": null,
              "name": null,
              "url": null,
              "isDefault": true
            }
          }
        }
      ]
    },
    "resetDeletesBookings": false,
    "backgroundVersion": "953d1375",
    "configurationVersion": "76b811ec"
  },
  "spots": [],
  "sectors": [
    {
      "id": "3f6b2a1c-8d4e-4c7a-9b5f-2e1d0c9a8b71",
      "name": "Fila A",
      "sortOrder": 1
    }
  ],
  "beachElements": []
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `settings` | MapSettings | `{"rows": 1, "columns": 1, "tiltDegrees": 0, "backgroundColor": "#F0D8CC", "background": {"iconId": null, "name": "BackgroundBeach", "url": null, "isCustom": false}, "icons": {"umbrella": {"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}, "customArticles": [{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]}, "resetDeletesBookings": false, "backgroundVersion": "953d1375", "configurationVersion": "76b811ec"}` |
| `settings.rows` | number (intero) | `1` |
| `settings.columns` | number (intero) | `1` |
| `settings.tiltDegrees` | number (intero) | `0` |
| `settings.backgroundColor` | string (#RRGGBB) \| null | `#F0D8CC` |
| `settings.background` | MapBackground \| null | `{"iconId": null, "name": "BackgroundBeach", "url": null, "isCustom": false}` |
| `settings.background.iconId` | string (UUID) \| null | `null` |
| `settings.background.name` | string \| null | `BackgroundBeach` |
| `settings.background.url` | string (URL) \| null | `null` |
| `settings.background.isCustom` | boolean | `false` |
| `settings.icons` | MapIcons | `{"umbrella": {"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}, "customArticles": [{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]}` |
| `settings.icons.umbrella` | MapIconPair | `{"free": {"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}}` |
| `settings.icons.umbrella.free` | MapIcon | `{"iconId": null, "name": "Umbrella3", "url": null, "isDefault": false}` |
| `settings.icons.umbrella.free.iconId` | string (UUID) \| null | `null` |
| `settings.icons.umbrella.free.name` | string \| null | `Umbrella3` |
| `settings.icons.umbrella.free.url` | string (URL) \| null | `null` |
| `settings.icons.umbrella.free.isDefault` | boolean | `false` |
| `settings.icons.umbrella.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot4", "url": null, "isDefault": false}` |
| `settings.icons.umbrella.occupied.iconId` | string (UUID) \| null | `null` |
| `settings.icons.umbrella.occupied.name` | string \| null | `MixedSpot4` |
| `settings.icons.umbrella.occupied.url` | string (URL) \| null | `null` |
| `settings.icons.umbrella.occupied.isDefault` | boolean | `false` |
| `settings.icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) | `[{"variant": 1, "icons": {"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}}, {"variant": 2, "icons": {"free": {"iconId": null, "name": "MixedSpot8", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": null, "url": null, "isDefault": true}}}]` |
| `settings.icons.customArticles[].variant` | number (intero) | `1` |
| `settings.icons.customArticles[].icons` | MapIconPair | `{"free": {"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}, "occupied": {"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}}` |
| `settings.icons.customArticles[].icons.free` | MapIcon | `{"iconId": null, "name": "MixedSpot1", "url": null, "isDefault": false}` |
| `settings.icons.customArticles[].icons.free.iconId` | string (UUID) \| null | `null` |
| `settings.icons.customArticles[].icons.free.name` | string \| null | `MixedSpot1` |
| `settings.icons.customArticles[].icons.free.url` | string (URL) \| null | `null` |
| `settings.icons.customArticles[].icons.free.isDefault` | boolean | `false` |
| `settings.icons.customArticles[].icons.occupied` | MapIcon | `{"iconId": null, "name": "MixedSpot13", "url": null, "isDefault": false}` |
| `settings.icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null | `null` |
| `settings.icons.customArticles[].icons.occupied.name` | string \| null | `MixedSpot13` |
| `settings.icons.customArticles[].icons.occupied.url` | string (URL) \| null | `null` |
| `settings.icons.customArticles[].icons.occupied.isDefault` | boolean | `false` |
| `settings.resetDeletesBookings` | boolean | `false` |
| `settings.backgroundVersion` | string | `953d1375` |
| `settings.configurationVersion` | string | `76b811ec` |
| `spots` | array di oggetti | `[]` |
| `sectors` | array di oggetti | `[{"id": "3f6b2a1c-8d4e-4c7a-9b5f-2e1d0c9a8b71", "name": "Fila A", "sortOrder": 1}]` |
| `beachElements` | array di oggetti | `[]` |

### POST `/api/catalog/sectors-list`

Settori e file: elenco.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "fila",
  "filters": [],
  "sortField": "sectorName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `fila` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `sectorName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 3,
  "rows": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "sectorName": "Fila 1",
      "version": "1ababee5"
    },
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02",
      "sectorName": "Fila 2",
      "version": "ba89f270"
    },
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03",
      "sectorName": "Fila 3",
      "version": "785a83d1"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `3` |
| `rows[]` | SectorItem[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "sectorName": "Fila 1", "version": "1ababee5"}, {"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02", "sectorName": "Fila 2", "version": "ba89f270"}, {"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03", "sectorName": "Fila 3", "version": "785a83d1"}]` |
| `rows[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `rows[].sectorName` | string \| null | `Fila 1` |
| `rows[].version` | string | `1ababee5` |

### POST `/api/catalog/sectors`

Settori e file: aggiunta.

#### Payload

```json
{
  "name": "Fila 9"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Fila 9` |

#### Response

Stato: 201 Created (il settore creato).

```json
{
  "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a13",
  "name": "Fila 9",
  "sortOrder": 12
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a13` |
| `name` | string | `Fila 9` |
| `sortOrder` | number (intero) | `12` |

### PUT `/api/catalog/sectors/{id}`

Settori e file: modifica.

#### Payload

```json
{
  "name": "Fila 2 lato mare"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `name` | string | `Fila 2 lato mare` |

#### Response

Stato: 200 OK (il settore salvato).

```json
{
  "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02",
  "name": "Fila 2 lato mare",
  "sortOrder": 1
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02` |
| `name` | string | `Fila 2 lato mare` |
| `sortOrder` | number (intero) | `1` |

### POST `/api/catalog/sectors/bulk-delete`

Settori e file: eliminazione di settori.

#### Payload

```json
{
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02",
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idSectors` | string[] (UUID, almeno uno) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02", "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a03"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/sectors/bulk-duplicate`

Settori e file: duplicazione.

#### Payload

```json
{
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idSectors` | string[] (UUID, almeno uno) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/sectors/sector-combobox`

Settori e file: settori per la tendina.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
    "value": "Fila 1"
  },
  {
    "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02",
    "value": "Fila 2"
  },
  {
    "id": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a09",
    "value": "Settore Est"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `value` | string | `Fila 1` |

### POST `/api/catalog/price-lists/packages-list`

Listino: elenco dei pacchetti.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "ombrellone",
  "filters": [],
  "sortField": "packageName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `ombrellone` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `packageName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
      "packageName": "Ombrellone e 2 lettini"
    },
    {
      "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02",
      "packageName": "Ombrellone, sdraio e lettino"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | PackageItem[] (array di oggetti) | `[{"idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "packageName": "Ombrellone e 2 lettini"}, {"idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02", "packageName": "Ombrellone, sdraio e lettino"}]` |
| `rows[].idPackage` | string (UUID) | `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01` |
| `rows[].packageName` | string \| null | `Ombrellone e 2 lettini` |

### POST `/api/catalog/price-lists/delete-package-item`

Listino: eliminazione dei pacchetti.

#### Payload

```json
{
  "idPackages": [
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPackages` | string[] (UUID, almeno uno) | `["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/price-lists/duplicate-package-item`

Listino: duplicazione dei pacchetti.

#### Payload

```json
{
  "idPackages": [
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPackages` | string[] (UUID, almeno uno) | `["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/sectors/sector-area-combobox`

Listino: aree della mappa per la tendina.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a03",
    "value": "Area cabine"
  },
  {
    "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01",
    "value": "Area lettini"
  },
  {
    "id": "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a02",
    "value": "Area ombrelloni"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a03` |
| `value` | string | `Area cabine` |

### POST `/api/catalog/price-lists/create-package`

Listino: creazione pacchetto.

#### Payload

```json
{
  "translations": [
    {
      "language": "it",
      "packageName": "Ombrellone e 2 lettini"
    },
    {
      "language": "en",
      "packageName": "Umbrella and 2 sunbeds"
    },
    {
      "language": "fr",
      "packageName": null
    },
    {
      "language": "de",
      "packageName": null
    },
    {
      "language": "es",
      "packageName": null
    },
    {
      "language": "ar",
      "packageName": null
    },
    {
      "language": "zh",
      "packageName": null
    }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "items": [
    {
      "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9",
      "quantity": 1
    },
    {
      "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb",
      "quantity": 2
    }
  ],
  "includedServices": "Cassetta di sicurezza e doccia calda",
  "idSectorAreas": [
    "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isDefault": false,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        {
          "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
          "seasonalHalfDayPrice": 450,
          "seasonalPrice": 800
        }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": [
        {
          "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
          "seasonalHalfDayPrice": 500,
          "seasonalPrice": 900
        }
      ]
    }
  ],
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 600,
      "seasonalPrice": 1100
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 15,
      "dailyPrice": 25,
      "weekendPrice": 30,
      "halfDayWeekendPrice": 18,
      "halfDaySevenDaysPrice": 90,
      "sevenDaysPrice": 150,
      "halfDayFourteenDaysPrice": 170,
      "fourteenDaysPrice": 280,
      "halfDayMonthlyPrice": 320,
      "monthlyPrice": 520
    }
  ],
  "vatRate": 22,
  "calculationMethod": "DailyWithWeekend",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-06-02T00:00:00.000Z",
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `translations[]` | PackageTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "packageName": "Ombrellone e 2 lettini"}, {"language": "en", "packageName": "Umbrella and 2 sunbeds"}, {"language": "fr", "packageName": null}, {"language": "de", "packageName": null}, {"language": "es", "packageName": null}, {"language": "ar", "packageName": null}, {"language": "zh", "packageName": null}]` |
| `translations[].language` | string | `it` |
| `translations[].packageName` | string \| null | `Ombrellone e 2 lettini` |
| `maxAdditionalItemQuantity` | number (intero) \| null | `2` |
| `maxPeopleQuantity` | number (intero) \| null | `4` |
| `items[]` | PackageItemQuantity[] (array di oggetti) | `[{"idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "quantity": 1}, {"idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "quantity": 2}]` |
| `items[].idInventoryItem` | string (UUID) | `276a67d2-d0eb-47f5-8223-4804b9a9dcf9` |
| `items[].quantity` | number (intero) | `1` |
| `includedServices` | string \| null | `Cassetta di sicurezza e doccia calda` |
| `idSectorAreas` | string[] (UUID) | `["9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"]` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isDefault` | boolean | `false` |
| `isDefaultOnline` | boolean | `false` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `false` |
| `reducedSeasonalPrices[]` | PackageReducedSeasonalPrice[] (array di oggetti) | `[{"startsOn": "2026-06-01T00:00:00.000Z", "endsOn": "2026-06-30T00:00:00.000Z", "reducedSeasonalPriceName": "Giugno", "sectorPrices": [{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]}, {"startsOn": "2026-09-01T00:00:00.000Z", "endsOn": "2026-09-15T00:00:00.000Z", "reducedSeasonalPriceName": "Inizio settembre", "sectorPrices": [{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 500, "seasonalPrice": 900}]}]` |
| `reducedSeasonalPrices[].startsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-01T00:00:00.000Z` |
| `reducedSeasonalPrices[].endsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-30T00:00:00.000Z` |
| `reducedSeasonalPrices[].reducedSeasonalPriceName` | string | `Giugno` |
| `reducedSeasonalPrices[].sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]` |
| `reducedSeasonalPrices[].sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `450` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `800` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `600` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `1100` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 15, "dailyPrice": 25, "weekendPrice": 30, "halfDayWeekendPrice": 18, "halfDaySevenDaysPrice": 90, "sevenDaysPrice": 150, "halfDayFourteenDaysPrice": 170, "fourteenDaysPrice": 280, "halfDayMonthlyPrice": 320, "monthlyPrice": 520}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `15` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `30` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `18` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `150` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `170` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `280` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `320` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `520` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `DailyWithWeekend` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-06-02T00:00:00.000Z", "2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/price-lists/get-package-articles`

Listino: articoli dei pacchetti.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "article": "Umbrella",
    "items": [
      {
        "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9",
        "itemName": "Ombrellone",
        "totalQuantity": 60
      }
    ]
  },
  {
    "article": "Sunbed",
    "items": [
      {
        "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb",
        "itemName": "Lettino",
        "totalQuantity": 120
      }
    ]
  },
  {
    "article": "SunbedMaxi",
    "items": []
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `article` | string (enum InventoryArticle) | `Umbrella` |
| `items[]` | PackageArticleItem[] (array di oggetti) | `[{"idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "itemName": "Ombrellone", "totalQuantity": 60}]` |
| `items[].idInventoryItem` | string (UUID) | `276a67d2-d0eb-47f5-8223-4804b9a9dcf9` |
| `items[].itemName` | string | `Ombrellone` |
| `items[].totalQuantity` | number (intero) | `60` |

### GET `/api/catalog/price-lists/get-package?idPackage={idPackage}`

Listino: pacchetto da modificare.

#### Payload

Nessun corpo: è una GET, l'id del pacchetto va nella query string.

```text
/api/catalog/price-lists/get-package?idPackage=8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPackage` | string (UUID) | `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01` |

#### Response

Stato: 200 OK.

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    {
      "language": "it",
      "packageName": "Ombrellone e 2 lettini"
    },
    {
      "language": "en",
      "packageName": "Umbrella and 2 sunbeds"
    }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "items": [
    {
      "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9",
      "quantity": 1
    },
    {
      "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb",
      "quantity": 2
    }
  ],
  "includedServices": "Cassetta di sicurezza",
  "idSectorAreas": [
    "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isDefault": true,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        {
          "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
          "seasonalHalfDayPrice": 450,
          "seasonalPrice": 800
        }
      ]
    },
    {
      "startsOn": "2026-09-01T00:00:00.000Z",
      "endsOn": "2026-09-15T00:00:00.000Z",
      "reducedSeasonalPriceName": "Inizio settembre",
      "sectorPrices": [
        {
          "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
          "seasonalHalfDayPrice": 500,
          "seasonalPrice": 900
        }
      ]
    }
  ],
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 600,
      "seasonalPrice": 1100
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 15,
      "dailyPrice": 25,
      "weekendPrice": 30,
      "halfDayWeekendPrice": 18,
      "halfDaySevenDaysPrice": 90,
      "sevenDaysPrice": 150,
      "halfDayFourteenDaysPrice": 170,
      "fourteenDaysPrice": 280,
      "halfDayMonthlyPrice": 320,
      "monthlyPrice": 520
    }
  ],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ],
  "version": "57e34fdc"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPackage` | string (UUID) | `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01` |
| `translations[]` | PackageTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "packageName": "Ombrellone e 2 lettini"}, {"language": "en", "packageName": "Umbrella and 2 sunbeds"}]` |
| `translations[].language` | string | `it` |
| `translations[].packageName` | string \| null | `Ombrellone e 2 lettini` |
| `maxAdditionalItemQuantity` | number (intero) \| null | `2` |
| `maxPeopleQuantity` | number (intero) \| null | `4` |
| `items[]` | PackageItemQuantity[] (array di oggetti) | `[{"idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "quantity": 1}, {"idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "quantity": 2}]` |
| `items[].idInventoryItem` | string (UUID) | `276a67d2-d0eb-47f5-8223-4804b9a9dcf9` |
| `items[].quantity` | number (intero) | `1` |
| `includedServices` | string \| null | `Cassetta di sicurezza` |
| `idSectorAreas` | string[] (UUID) | `["9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"]` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isDefault` | boolean | `true` |
| `isDefaultOnline` | boolean | `false` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `false` |
| `reducedSeasonalPrices[]` | PackageReducedSeasonalPrice[] (array di oggetti) | `[{"startsOn": "2026-06-01T00:00:00.000Z", "endsOn": "2026-06-30T00:00:00.000Z", "reducedSeasonalPriceName": "Giugno", "sectorPrices": [{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]}, {"startsOn": "2026-09-01T00:00:00.000Z", "endsOn": "2026-09-15T00:00:00.000Z", "reducedSeasonalPriceName": "Inizio settembre", "sectorPrices": [{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 500, "seasonalPrice": 900}]}]` |
| `reducedSeasonalPrices[].startsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-01T00:00:00.000Z` |
| `reducedSeasonalPrices[].endsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-30T00:00:00.000Z` |
| `reducedSeasonalPrices[].reducedSeasonalPriceName` | string | `Giugno` |
| `reducedSeasonalPrices[].sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]` |
| `reducedSeasonalPrices[].sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `450` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `800` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1100}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `600` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `1100` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 15, "dailyPrice": 25, "weekendPrice": 30, "halfDayWeekendPrice": 18, "halfDaySevenDaysPrice": 90, "sevenDaysPrice": 150, "halfDayFourteenDaysPrice": 170, "fourteenDaysPrice": 280, "halfDayMonthlyPrice": 320, "monthlyPrice": 520}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `15` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `30` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `18` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `150` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `170` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `280` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `320` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `520` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `Daily` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |
| `version` | string | `57e34fdc` |

### POST `/api/catalog/price-lists/update-package`

Listino: modifica pacchetto.

#### Payload

```json
{
  "idPackage": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
  "translations": [
    {
      "language": "it",
      "packageName": "Ombrellone e 2 lettini premium"
    },
    {
      "language": "en",
      "packageName": "Umbrella and 2 sunbeds"
    },
    {
      "language": "fr",
      "packageName": null
    },
    {
      "language": "de",
      "packageName": null
    },
    {
      "language": "es",
      "packageName": null
    },
    {
      "language": "ar",
      "packageName": null
    },
    {
      "language": "zh",
      "packageName": null
    }
  ],
  "maxAdditionalItemQuantity": 2,
  "maxPeopleQuantity": 4,
  "items": [
    {
      "idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9",
      "quantity": 1
    },
    {
      "idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb",
      "quantity": 2
    }
  ],
  "includedServices": "Cassetta di sicurezza",
  "idSectorAreas": [
    "9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"
  ],
  "idSectors": [
    "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"
  ],
  "isDefault": true,
  "isDefaultOnline": false,
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "reducedSeasonalPrices": [
    {
      "startsOn": "2026-06-01T00:00:00.000Z",
      "endsOn": "2026-06-30T00:00:00.000Z",
      "reducedSeasonalPriceName": "Giugno",
      "sectorPrices": [
        {
          "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
          "seasonalHalfDayPrice": 450,
          "seasonalPrice": 800
        }
      ]
    }
  ],
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 600,
      "seasonalPrice": 1200
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 15,
      "dailyPrice": 25,
      "weekendPrice": 30,
      "halfDayWeekendPrice": 18,
      "halfDaySevenDaysPrice": 90,
      "sevenDaysPrice": 150,
      "halfDayFourteenDaysPrice": 170,
      "fourteenDaysPrice": 280,
      "halfDayMonthlyPrice": 320,
      "monthlyPrice": 520
    }
  ],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idPackage` | string (UUID) | `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01` |
| `translations[]` | PackageTranslation[] (array di oggetti, una per lingua) | `[{"language": "it", "packageName": "Ombrellone e 2 lettini premium"}, {"language": "en", "packageName": "Umbrella and 2 sunbeds"}, {"language": "fr", "packageName": null}, {"language": "de", "packageName": null}, {"language": "es", "packageName": null}, {"language": "ar", "packageName": null}, {"language": "zh", "packageName": null}]` |
| `translations[].language` | string | `it` |
| `translations[].packageName` | string \| null | `Ombrellone e 2 lettini premium` |
| `maxAdditionalItemQuantity` | number (intero) \| null | `2` |
| `maxPeopleQuantity` | number (intero) \| null | `4` |
| `items[]` | PackageItemQuantity[] (array di oggetti) | `[{"idInventoryItem": "276a67d2-d0eb-47f5-8223-4804b9a9dcf9", "quantity": 1}, {"idInventoryItem": "c87770a7-74b6-4dc4-a5ad-e193e136bacb", "quantity": 2}]` |
| `items[].idInventoryItem` | string (UUID) | `276a67d2-d0eb-47f5-8223-4804b9a9dcf9` |
| `items[].quantity` | number (intero) | `1` |
| `includedServices` | string \| null | `Cassetta di sicurezza` |
| `idSectorAreas` | string[] (UUID) | `["9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01"]` |
| `idSectors` | string[] (UUID) | `["5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01"]` |
| `isDefault` | boolean | `true` |
| `isDefaultOnline` | boolean | `false` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `false` |
| `reducedSeasonalPrices[]` | PackageReducedSeasonalPrice[] (array di oggetti) | `[{"startsOn": "2026-06-01T00:00:00.000Z", "endsOn": "2026-06-30T00:00:00.000Z", "reducedSeasonalPriceName": "Giugno", "sectorPrices": [{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]}]` |
| `reducedSeasonalPrices[].startsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-01T00:00:00.000Z` |
| `reducedSeasonalPrices[].endsOn` | string (ISO 8601, mezzanotte UTC) | `2026-06-30T00:00:00.000Z` |
| `reducedSeasonalPrices[].reducedSeasonalPriceName` | string | `Giugno` |
| `reducedSeasonalPrices[].sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 450, "seasonalPrice": 800}]` |
| `reducedSeasonalPrices[].sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `450` |
| `reducedSeasonalPrices[].sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `800` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 600, "seasonalPrice": 1200}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `600` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `1200` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 15, "dailyPrice": 25, "weekendPrice": 30, "halfDayWeekendPrice": 18, "halfDaySevenDaysPrice": 90, "sevenDaysPrice": 150, "halfDayFourteenDaysPrice": 170, "fourteenDaysPrice": 280, "halfDayMonthlyPrice": 320, "monthlyPrice": 520}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `15` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `30` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `18` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `150` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `170` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `280` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `320` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `520` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `Daily` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/price-lists/additional-services-articles-list`

Listino: elenco degli articoli/servizi aggiuntivi.

#### Payload

```json
{
  "page": 1,
  "pageSize": 10,
  "search": "lettino",
  "filters": [],
  "sortField": "additionalItemName",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `lettino` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `sortField` | string | `additionalItemName` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

Stato: 200 OK.

```json
{
  "total": 2,
  "rows": [
    {
      "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01",
      "additionalItemName": "Lettino"
    },
    {
      "idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a11",
      "additionalItemName": "Lettino extra famiglia"
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | AdditionalItem[] (array di oggetti) | `[{"idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "additionalItemName": "Lettino"}, {"idAdditionalItem": "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a11", "additionalItemName": "Lettino extra famiglia"}]` |
| `rows[].idAdditionalItem` | string (UUID) | `4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01` |
| `rows[].additionalItemName` | string \| null | `Lettino` |

### POST `/api/catalog/price-lists/delete-additional-services-article-item`

Listino: eliminazione degli articoli/servizi aggiuntivi.

#### Payload

```json
{
  "idAdditionalItems": [
    "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01",
    "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAdditionalItems` | string[] (UUID, almeno uno) | `["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01", "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a02"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/price-lists/duplicate-additional-services-article-item`

Listino: duplicazione degli articoli/servizi aggiuntivi.

#### Payload

```json
{
  "idAdditionalItems": [
    "4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01"
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAdditionalItems` | string[] (UUID, almeno uno) | `["4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a01"]` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/price-lists/package-combobox`

Listino: pacchetti per la tendina.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
[
  {
    "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
    "value": "Ombrellone e 2 lettini"
  },
  {
    "id": "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a02",
    "value": "Ombrellone, sdraio e lettino"
  }
]
```

| Property | Tipo | Esempio |
|---|---|---|
| `id` | string (UUID) | `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01` |
| `value` | string | `Ombrellone e 2 lettini` |

### POST `/api/catalog/price-lists/create-additional-services-article-item`

Listino: creazione articolo/servizio aggiuntivo.

#### Payload

```json
{
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": [
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"
  ],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 60,
      "seasonalPrice": 120
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 4,
      "dailyPrice": 7,
      "weekendPrice": 9,
      "halfDayWeekendPrice": 5,
      "halfDaySevenDaysPrice": 25,
      "sevenDaysPrice": 42,
      "halfDayFourteenDaysPrice": 46,
      "fourteenDaysPrice": 80,
      "halfDayMonthlyPrice": 90,
      "monthlyPrice": 150
    }
  ],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idArticle` | string (UUID) | `afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62` |
| `idPackages` | string[] (UUID) | `["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"]` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `false` |
| `isMapAssignable` | boolean | `true` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `60` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `120` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 4, "dailyPrice": 7, "weekendPrice": 9, "halfDayWeekendPrice": 5, "halfDaySevenDaysPrice": 25, "sevenDaysPrice": 42, "halfDayFourteenDaysPrice": 46, "fourteenDaysPrice": 80, "halfDayMonthlyPrice": 90, "monthlyPrice": 150}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `4` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `7` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `9` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `5` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `42` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `46` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `80` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `150` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `Daily` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/catalog/price-lists/get-additional-services-article-item?idAdditionalItem={idAdditionalItem}`

Listino: articolo/servizio aggiuntivo da modificare.

#### Payload

Nessun corpo: è una GET, l'id dell'articolo aggiuntivo va nella query string e lo stabilimento viene dal token.

```text
/api/catalog/price-lists/get-additional-services-article-item?idAdditionalItem=2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAdditionalItem` | string (UUID) | `2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02` |

#### Response

Stato: 200 OK.

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": [
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"
  ],
  "isOnlineEnabled": true,
  "isOnlineOnly": false,
  "isMapAssignable": true,
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 60,
      "seasonalPrice": 120
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 4,
      "dailyPrice": 7,
      "weekendPrice": 9,
      "halfDayWeekendPrice": 5,
      "halfDaySevenDaysPrice": 25,
      "sevenDaysPrice": 42,
      "halfDayFourteenDaysPrice": 46,
      "fourteenDaysPrice": 80,
      "halfDayMonthlyPrice": 90,
      "monthlyPrice": 150
    }
  ],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ],
  "version": "9864e3d5"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAdditionalItem` | string (UUID) | `2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02` |
| `idArticle` | string (UUID) \| null | `afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62` |
| `idPackages` | string[] (UUID) | `["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01"]` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `false` |
| `isMapAssignable` | boolean | `true` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 120}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `60` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `120` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 4, "dailyPrice": 7, "weekendPrice": 9, "halfDayWeekendPrice": 5, "halfDaySevenDaysPrice": 25, "sevenDaysPrice": 42, "halfDayFourteenDaysPrice": 46, "fourteenDaysPrice": 80, "halfDayMonthlyPrice": 90, "monthlyPrice": 150}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `4` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `7` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `9` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `5` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `42` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `46` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `80` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `150` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `Daily` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |
| `version` | string | `9864e3d5` |

### POST `/api/catalog/price-lists/update-additional-services-article-item`

Listino: modifica articolo/servizio aggiuntivo.

#### Payload

```json
{
  "idAdditionalItem": "2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02",
  "idArticle": "afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62",
  "idPackages": [
    "8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01",
    "4e2a9c71-3b5d-4f6e-8a1b-2c3d4e5f6a02"
  ],
  "isOnlineEnabled": true,
  "isOnlineOnly": true,
  "isMapAssignable": true,
  "sectorPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "seasonalHalfDayPrice": 60,
      "seasonalPrice": 130
    }
  ],
  "periodPrices": [
    {
      "idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01",
      "idPeriod": 0,
      "halfDayPrice": 4,
      "dailyPrice": 7,
      "weekendPrice": 9,
      "halfDayWeekendPrice": 5,
      "halfDaySevenDaysPrice": 25,
      "sevenDaysPrice": 42,
      "halfDayFourteenDaysPrice": 46,
      "fourteenDaysPrice": 80,
      "halfDayMonthlyPrice": 90,
      "monthlyPrice": 150
    }
  ],
  "vatRate": 22,
  "calculationMethod": "Daily",
  "isCumulative": false,
  "monthlySetting": "EffectiveMonthDuration",
  "weekendSetting": "SaturdaySunday",
  "isPublicHolidaysIncluded": true,
  "publicHolidayDates": [
    "2026-08-15T00:00:00.000Z"
  ],
  "isIntermediatePricesIncluded": true,
  "intermediatePrices": [
    {
      "idPeriod": 0,
      "isFirstRow": true,
      "dayQuantity": 2,
      "price": 35
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idAdditionalItem` | string (UUID) | `2b7e4c90-1d3a-4f5b-8c6d-7e8f9a0b1c02` |
| `idArticle` | string (UUID) | `afb531ab-f98b-4ba6-bc0b-fc7d86fa0d62` |
| `idPackages` | string[] (UUID) | `["8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a01", "4e2a9c71-3b5d-4f6e-8a1b-2c3d4e5f6a02"]` |
| `isOnlineEnabled` | boolean | `true` |
| `isOnlineOnly` | boolean | `true` |
| `isMapAssignable` | boolean | `true` |
| `sectorPrices[]` | PackageSectorPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "seasonalHalfDayPrice": 60, "seasonalPrice": 130}]` |
| `sectorPrices[].idSector` | string (UUID) | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `sectorPrices[].seasonalHalfDayPrice` | number (decimale, €) \| null | `60` |
| `sectorPrices[].seasonalPrice` | number (decimale, €) \| null | `130` |
| `periodPrices[]` | PackagePeriodPrice[] (array di oggetti) | `[{"idSector": "5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01", "idPeriod": 0, "halfDayPrice": 4, "dailyPrice": 7, "weekendPrice": 9, "halfDayWeekendPrice": 5, "halfDaySevenDaysPrice": 25, "sevenDaysPrice": 42, "halfDayFourteenDaysPrice": 46, "fourteenDaysPrice": 80, "halfDayMonthlyPrice": 90, "monthlyPrice": 150}]` |
| `periodPrices[].idSector` | string (UUID) \| null | `5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01` |
| `periodPrices[].idPeriod` | number (intero) \| null | `0` |
| `periodPrices[].halfDayPrice` | number (decimale, €) \| null | `4` |
| `periodPrices[].dailyPrice` | number (decimale, €) \| null | `7` |
| `periodPrices[].weekendPrice` | number (decimale, €) \| null | `9` |
| `periodPrices[].halfDayWeekendPrice` | number (decimale, €) \| null | `5` |
| `periodPrices[].halfDaySevenDaysPrice` | number (decimale, €) \| null | `25` |
| `periodPrices[].sevenDaysPrice` | number (decimale, €) \| null | `42` |
| `periodPrices[].halfDayFourteenDaysPrice` | number (decimale, €) \| null | `46` |
| `periodPrices[].fourteenDaysPrice` | number (decimale, €) \| null | `80` |
| `periodPrices[].halfDayMonthlyPrice` | number (decimale, €) \| null | `90` |
| `periodPrices[].monthlyPrice` | number (decimale, €) \| null | `150` |
| `vatRate` | number \| null | `22` |
| `calculationMethod` | string (enum PriceCalculationMethod) \| null | `Daily` |
| `isCumulative` | boolean | `false` |
| `monthlySetting` | string (enum PriceMonthlySetting) \| null | `EffectiveMonthDuration` |
| `weekendSetting` | string (enum PriceWeekendSetting) \| null | `SaturdaySunday` |
| `isPublicHolidaysIncluded` | boolean | `true` |
| `publicHolidayDates` | string[] (date UTC ISO 8601) | `["2026-08-15T00:00:00.000Z"]` |
| `isIntermediatePricesIncluded` | boolean | `true` |
| `intermediatePrices[]` | PackageIntermediatePrice[] (array di oggetti) | `[{"idPeriod": 0, "isFirstRow": true, "dayQuantity": 2, "price": 35}]` |
| `intermediatePrices[].idPeriod` | number (intero) | `0` |
| `intermediatePrices[].isFirstRow` | boolean | `true` |
| `intermediatePrices[].dayQuantity` | number (intero) | `2` |
| `intermediatePrices[].price` | number | `35` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### POST `/api/catalog/property-info/main-photo`

Informazioni stabilimento: caricamento dell'immagine principale.

#### Payload

```json
{
  "file": "spiaggia.webp (image/webp, 1,2 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `spiaggia.webp (image/webp, 1,2 MB)` |

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp",
      "sortOrder": 1
    },
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": {
    "fileName": "listino-prezzi.pdf",
    "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"
  },
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp", "sortOrder": 1}, {"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/bar.webp` |
| `photos[].sortOrder` | number (intero) | `1` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `{"fileName": "listino-prezzi.pdf", "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"}` |
| `priceListPdf.fileName` | string | `listino-prezzi.pdf` |
| `priceListPdf.url` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

### DELETE `/api/catalog/property-info/main-photo`

Informazioni stabilimento: eliminazione dell'immagine principale.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": null,
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp",
      "sortOrder": 1
    },
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": {
    "fileName": "listino-prezzi.pdf",
    "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"
  },
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `null` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp", "sortOrder": 1}, {"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/bar.webp` |
| `photos[].sortOrder` | number (intero) | `1` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `{"fileName": "listino-prezzi.pdf", "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"}` |
| `priceListPdf.fileName` | string | `listino-prezzi.pdf` |
| `priceListPdf.url` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

### POST `/api/catalog/property-info/photos`

Informazioni stabilimento: aggiunta di una foto alla galleria.

#### Payload

```json
{
  "file": "bar.webp (image/webp, 0,9 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `bar.webp (image/webp, 0,9 MB)` |

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp",
      "sortOrder": 1
    },
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": {
    "fileName": "listino-prezzi.pdf",
    "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"
  },
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp", "sortOrder": 1}, {"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/bar.webp` |
| `photos[].sortOrder` | number (intero) | `1` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `{"fileName": "listino-prezzi.pdf", "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"}` |
| `priceListPdf.fileName` | string | `listino-prezzi.pdf` |
| `priceListPdf.url` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

### DELETE `/api/catalog/property-info/photos/{id}`

Informazioni stabilimento: eliminazione di una foto della galleria.

#### Payload

Nessun corpo: l'id della foto va nell'indirizzo e lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": {
    "fileName": "listino-prezzi.pdf",
    "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"
  },
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/ombrelloni.webp` |
| `photos[].sortOrder` | number (intero) | `2` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `{"fileName": "listino-prezzi.pdf", "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"}` |
| `priceListPdf.fileName` | string | `listino-prezzi.pdf` |
| `priceListPdf.url` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

### POST `/api/catalog/property-info/documents/{document}`

Informazioni stabilimento: caricamento del PDF del listino o del menù.

#### Payload

```json
{
  "file": "listino-prezzi.pdf (application/pdf, 0,4 MB)"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `file` | file (multipart/form-data) | `listino-prezzi.pdf (application/pdf, 0,4 MB)` |

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp",
      "sortOrder": 1
    },
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": {
    "fileName": "listino-prezzi.pdf",
    "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"
  },
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp", "sortOrder": 1}, {"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/bar.webp` |
| `photos[].sortOrder` | number (intero) | `1` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `{"fileName": "listino-prezzi.pdf", "url": "https://media.example.com/lido-demo/listino-prezzi.pdf"}` |
| `priceListPdf.fileName` | string | `listino-prezzi.pdf` |
| `priceListPdf.url` | string \| null | `https://media.example.com/lido-demo/listino-prezzi.pdf` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

### DELETE `/api/catalog/property-info/documents/{document}`

Informazioni stabilimento: eliminazione del PDF del listino o del menù.

#### Payload

Nessun corpo: il documento va nell'indirizzo e lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

```json
{
  "mainPhotoUrl": "https://media.example.com/lido-demo/spiaggia.webp",
  "photos": [
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01",
      "url": "https://media.example.com/lido-demo/bar.webp",
      "sortOrder": 1
    },
    {
      "id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02",
      "url": "https://media.example.com/lido-demo/ombrelloni.webp",
      "sortOrder": 2
    }
  ],
  "virtualTourUrl": "https://tour.example.com/lido-demo",
  "latitude": 44.063812,
  "longitude": 12.580143,
  "priceListPdf": null,
  "menuPdf": null
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `mainPhotoUrl` | string \| null | `https://media.example.com/lido-demo/spiaggia.webp` |
| `photos[]` | PropertyInfoPhoto[] (array di oggetti) | `[{"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01", "url": "https://media.example.com/lido-demo/bar.webp", "sortOrder": 1}, {"id": "0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02", "url": "https://media.example.com/lido-demo/ombrelloni.webp", "sortOrder": 2}]` |
| `photos[].id` | string (UUID) | `0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01` |
| `photos[].url` | string \| null | `https://media.example.com/lido-demo/bar.webp` |
| `photos[].sortOrder` | number (intero) | `1` |
| `virtualTourUrl` | string \| null | `https://tour.example.com/lido-demo` |
| `latitude` | number \| null | `44.063812` |
| `longitude` | number \| null | `12.580143` |
| `priceListPdf` | PropertyInfoDocument \| null | `null` |
| `menuPdf` | PropertyInfoDocument \| null | `null` |

## /booking

### GET `/api/booking/get-online-services-options`

Prenotazioni online: opzioni dei servizi.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "usefulInformation": "<p>Il <strong>parcheggio</strong> è gratuito per i clienti.</p>",
  "version": "0fb5c88e"
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
| `version` | string | `0fb5c88e` |

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

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

## /system

### GET `/api/system/email-configuration`

Configurazione email: lettura.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "smtpSecurity": "Tls",
  "version": "9e40274f"
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
| `version` | string | `9e40274f` |

### POST `/api/system/send-test-email`

Configurazione email: invio di prova.

#### Payload

```json
{
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

Nessun corpo né parametri: lo stabilimento viene dal token.

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
  "firstName": "Mario",
  "lastName": "Rossi",
  "email": "mario.rossi@example.com",
  "mobilePhone": "+39 333 123 4567",
  "message": "Buongiorno, vorrei sapere come esportare il listino."
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `firstName` | string | `Mario` |
| `lastName` | string | `Rossi` |
| `email` | string | `mario.rossi@example.com` |
| `mobilePhone` | string \| null | `+39 333 123 4567` |
| `message` | string | `Buongiorno, vorrei sapere come esportare il listino.` |

#### Response

Stato: 204 No Content (nessun corpo).

Nessun corpo.

### GET `/api/system/get-stripe-information`

Prenotazioni online: conto Stripe collegato.

#### Payload

Nessun corpo né parametri: lo stabilimento viene dal token.

#### Response

Stato: 200 OK.

Il corpo è solo l'id del conto Stripe collegato, come stringa JSON (tra virgolette, Content-Type application/json): acct_…. null se lo stabilimento non ha ancora collegato Stripe.

```json
"acct_1QmDemo4Fz8Lw2Xc"
```
