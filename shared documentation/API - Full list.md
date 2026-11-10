# Accesso

## POST `/api/identity/auth/login`

**Payload JSON**

```json
{
  "username": "string",
  "password": "string",
  "remember": "boolean",
  "app": "string"
}
```

**Response JSON**

```json
{
  "accessToken": "string",
  "expiresIn": "number (intero)",
  "user": {
    "id": "string (UUID)",
    "email": "string",
    "role": "string",
    "propertyId": "string (UUID)"
  }
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `username` | string |
| `password` | string |
| `remember` | boolean |
| `app` | string |

**Response tabella**

| Campo | Tipo |
|---|---|
| `accessToken` | string |
| `expiresIn` | number (intero) |
| `user` | AuthUser (oggetto) |
| `user.id` | string (UUID) |
| `user.email` | string |
| `user.role` | string |
| `user.propertyId` | string (UUID) |

## POST `/api/identity/auth/refresh`

**Payload JSON**

Nessun corpo: il browser manda il cookie `sb_rt`.

**Response JSON**

```json
{
  "accessToken": "string",
  "expiresIn": "number (intero)",
  "user": {
    "id": "string (UUID)",
    "email": "string",
    "role": "string",
    "propertyId": "string (UUID)"
  }
}
```

**Payload tabella**

Nessun corpo: il browser manda il cookie `sb_rt`.

**Response tabella**

| Campo | Tipo |
|---|---|
| `accessToken` | string |
| `expiresIn` | number (intero) |
| `user` | AuthUser (oggetto) |
| `user.id` | string (UUID) |
| `user.email` | string |
| `user.role` | string |
| `user.propertyId` | string (UUID) |

## POST `/api/identity/auth/logout`

**Payload JSON**

Nessun corpo: il browser manda il cookie `sb_rt`.

**Response JSON**

Nessun corpo (204).

**Payload tabella**

Nessun corpo: il browser manda il cookie `sb_rt`.

**Response tabella**

Nessun corpo (204).

# Magazzino

## POST `/api/warehouse/list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idArticle": "string (UUID)",
      "name": "string",
      "totalQuantity": "number (intero)",
      "availableQuantity": "number (intero)",
      "thresholdQuantity": "number (intero) | null",
      "isThresholdWarningActive": "boolean"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | WarehouseItem[] (array di oggetti) |
| `rows[].idArticle` | string (UUID) |
| `rows[].name` | string |
| `rows[].totalQuantity` | number (intero) |
| `rows[].availableQuantity` | number (intero) |
| `rows[].thresholdQuantity` | number (intero) \| null |
| `rows[].isThresholdWarningActive` | boolean |

# Impostazioni

## Informazioni stabilimento

### GET `/api/catalog/bathhouse-configuration-general?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "bathhouseName": "string | null",
  "address": "string | null",
  "postalCode": "string | null",
  "city": "string | null",
  "province": "string | null",
  "region": "string | null",
  "country": "string (ISO 3166-1 alpha-2) | null",
  "phone": "string | null",
  "whatsapp": "string | null",
  "email": "string | null",
  "website": "string | null",
  "facebook": "string | null",
  "instagram": "string | null",
  "description": "string (HTML) | null",
  "openingTime": "string (HH:mm) | null",
  "closingTime": "string (HH:mm) | null",
  "cancellationPolicy": "string (enum CancellationPolicy) | null",
  "petPolicy": "string (enum PetPolicy) | null",
  "regulation": "string (HTML) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `bathhouseName` | string \| null |
| `address` | string \| null |
| `postalCode` | string \| null |
| `city` | string \| null |
| `province` | string \| null |
| `region` | string \| null |
| `country` | string (ISO 3166-1 alpha-2) \| null |
| `phone` | string \| null |
| `whatsapp` | string \| null |
| `email` | string \| null |
| `website` | string \| null |
| `facebook` | string \| null |
| `instagram` | string \| null |
| `description` | string (HTML) \| null |
| `openingTime` | string (HH:mm) \| null |
| `closingTime` | string (HH:mm) \| null |
| `cancellationPolicy` | string (enum CancellationPolicy) \| null |
| `petPolicy` | string (enum PetPolicy) \| null |
| `regulation` | string (HTML) \| null |

### GET `/api/catalog/countries-combobox`

**Payload JSON**

Nessun payload.

**Response JSON**

```json
[
  {
    "id": "string (ISO 3166-1 alpha-2)",
    "value": "string"
  }
]
```

**Payload tabella**

Nessun payload.

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (ISO 3166-1 alpha-2) |
| `[].value` | string |

### POST `/api/catalog/save-bathhouse-configuration-general`

**Payload JSON**

```json
{
  "bathhouseName": "string | null",
  "address": "string | null",
  "postalCode": "string | null",
  "city": "string | null",
  "province": "string | null",
  "region": "string | null",
  "country": "string (ISO 3166-1 alpha-2) | null",
  "phone": "string | null",
  "whatsapp": "string | null",
  "email": "string | null",
  "website": "string | null",
  "facebook": "string | null",
  "instagram": "string | null",
  "description": "string (HTML) | null",
  "openingTime": "string (HH:mm) | null",
  "closingTime": "string (HH:mm) | null",
  "cancellationPolicy": "string (enum CancellationPolicy) | null",
  "petPolicy": "string (enum PetPolicy) | null",
  "regulation": "string (HTML) | null",
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `bathhouseName` | string \| null |
| `address` | string \| null |
| `postalCode` | string \| null |
| `city` | string \| null |
| `province` | string \| null |
| `region` | string \| null |
| `country` | string (ISO 3166-1 alpha-2) \| null |
| `phone` | string \| null |
| `whatsapp` | string \| null |
| `email` | string \| null |
| `website` | string \| null |
| `facebook` | string \| null |
| `instagram` | string \| null |
| `description` | string (HTML) \| null |
| `openingTime` | string (HH:mm) \| null |
| `closingTime` | string (HH:mm) \| null |
| `cancellationPolicy` | string (enum CancellationPolicy) \| null |
| `petPolicy` | string (enum PetPolicy) \| null |
| `regulation` | string (HTML) \| null |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/bathhouse-configuration-services?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "idService": "number (intero)",
    "name": "string",
    "categoryCode": "string",
    "iconUrl": "string | null",
    "isSelected": "boolean"
  }
]
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].idService` | number (intero) |
| `[].name` | string |
| `[].categoryCode` | string |
| `[].iconUrl` | string \| null |
| `[].isSelected` | boolean |

### POST `/api/catalog/save-bathhouse-configuration-services`

**Payload JSON**

```json
{
  "services": [
    {
      "idService": "number (intero)",
      "name": "string",
      "categoryCode": "string",
      "iconUrl": "string | null",
      "isSelected": "boolean"
    }
  ],
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `services[]` | BathhouseServiceItem[] (array di oggetti) |
| `services[].idService` | number (intero) |
| `services[].name` | string |
| `services[].categoryCode` | string |
| `services[].iconUrl` | string \| null |
| `services[].isSelected` | boolean |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/bathhouse-configuration-media?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "photos": [
    {
      "idPhoto": "number (intero)",
      "url": "string",
      "isMain": "boolean"
    }
  ],
  "virtualTourUrl": "string | null",
  "priceListPdfUrl": "string | null",
  "menuPdfUrl": "string | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `photos[]` | BathhousePhoto[] (array di oggetti) |
| `photos[].idPhoto` | number (intero) |
| `photos[].url` | string |
| `photos[].isMain` | boolean |
| `virtualTourUrl` | string \| null |
| `priceListPdfUrl` | string \| null |
| `menuPdfUrl` | string \| null |

## Gestione utenti

### POST `/api/identity/users-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idUser": "string (UUID)",
      "email": "string | null",
      "phoneCountryCode": "string | null",
      "phone": "string | null",
      "role": "string (enum UserRole) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | UserItem[] (array di oggetti) |
| `rows[].idUser` | string (UUID) |
| `rows[].email` | string \| null |
| `rows[].phoneCountryCode` | string \| null |
| `rows[].phone` | string \| null |
| `rows[].role` | string (enum UserRole) \| null |

### POST `/api/identity/create-user`

**Payload JSON**

```json
{
  "email": "string",
  "isEmailActive": "boolean",
  "password": "string",
  "phone": "string | null",
  "isPhoneActive": "boolean",
  "role": "string (enum UserRole) | null",
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `email` | string |
| `isEmailActive` | boolean |
| `password` | string |
| `phone` | string \| null |
| `isPhoneActive` | boolean |
| `role` | string (enum UserRole) \| null |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### GET `/api/identity/get-selected-user?idUser={idUser}`

**Payload JSON** (query string)

```json
{
  "idUser": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idUser": "string (UUID)",
  "email": "string",
  "isEmailActive": "boolean",
  "phone": "string | null",
  "isPhoneActive": "boolean",
  "role": "string (enum UserRole) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idUser` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idUser` | string (UUID) |
| `email` | string |
| `isEmailActive` | boolean |
| `phone` | string \| null |
| `isPhoneActive` | boolean |
| `role` | string (enum UserRole) \| null |

### POST `/api/identity/update-user`

**Payload JSON**

```json
{
  "idUser": "string (UUID)",
  "email": "string",
  "isEmailActive": "boolean",
  "password": "string | null",
  "phone": "string | null",
  "isPhoneActive": "boolean",
  "role": "string (enum UserRole) | null",
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idUser` | string (UUID) |
| `email` | string |
| `isEmailActive` | boolean |
| `password` | string \| null |
| `phone` | string \| null |
| `isPhoneActive` | boolean |
| `role` | string (enum UserRole) \| null |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/identity/delete-user`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idUser": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idUser` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/audit/entries-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idAuditEntry": "string (UUID)",
      "bookingNumber": "string | null",
      "userNumber": "string | null",
      "actorName": "string | null",
      "spotLabel": "string | null",
      "actionCode": "string | null",
      "occurredAt": "string (data e ora ISO 8601 UTC) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | AuditEntryItem[] (array di oggetti) |
| `rows[].idAuditEntry` | string (UUID) |
| `rows[].bookingNumber` | string \| null |
| `rows[].userNumber` | string \| null |
| `rows[].actorName` | string \| null |
| `rows[].spotLabel` | string \| null |
| `rows[].actionCode` | string \| null |
| `rows[].occurredAt` | string (data e ora ISO 8601 UTC) \| null |

### POST `/api/audit/user-combobox`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "id": "string (UUID)",
    "value": "string"
  }
]
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (UUID) |
| `[].value` | string |

### POST `/api/audit/export-log`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

File (binario), non JSON.

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

File (binario), non JSON.

### POST `/api/audit/print-log`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

File (binario), non JSON.

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

File (binario), non JSON.

## Stagioni e periodi

### GET `/api/seasons/season-dates?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "currentSeasonDateFrom": "string (ISO 8601, UTC)",
  "currentSeasonDateTo": "string (ISO 8601, UTC)",
  "nextSeasonDateFrom": "string (ISO 8601, UTC) | null",
  "nextSeasonDateTo": "string (ISO 8601, UTC) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `currentSeasonDateFrom` | string (ISO 8601, UTC) |
| `currentSeasonDateTo` | string (ISO 8601, UTC) |
| `nextSeasonDateFrom` | string (ISO 8601, UTC) \| null |
| `nextSeasonDateTo` | string (ISO 8601, UTC) \| null |

### POST `/api/seasons/save-season-dates`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "currentSeasonDateFrom": "string (ISO 8601, UTC)",
  "currentSeasonDateTo": "string (ISO 8601, UTC)",
  "nextSeasonDateFrom": "string (ISO 8601, UTC)",
  "nextSeasonDateTo": "string (ISO 8601, UTC)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `currentSeasonDateFrom` | string (ISO 8601, UTC) |
| `currentSeasonDateTo` | string (ISO 8601, UTC) |
| `nextSeasonDateFrom` | string (ISO 8601, UTC) |
| `nextSeasonDateTo` | string (ISO 8601, UTC) |

**Response tabella**

Nessun corpo (204).

### GET `/api/seasons/period-list?idProperty={idProperty}&isNextSeason={isNextSeason}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)",
  "isNextSeason": "boolean"
}
```

**Response JSON**

```json
[
  {
    "idPeriod": "string (UUID)",
    "periodName": "string",
    "intervals": [
      {
        "idInterval": "string (UUID)",
        "dateFrom": "string (ISO 8601, UTC)",
        "dateTo": "string (ISO 8601, UTC)"
      }
    ]
  }
]
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `isNextSeason` | boolean |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].idPeriod` | string (UUID) |
| `[].periodName` | string |
| `[].intervals[]` | SeasonPeriodIntervalItem[] (array di oggetti) |
| `[].intervals[].idInterval` | string (UUID) |
| `[].intervals[].dateFrom` | string (ISO 8601, UTC) |
| `[].intervals[].dateTo` | string (ISO 8601, UTC) |

### POST `/api/seasons/add-period`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "isNextSeason": "boolean",
  "periodName": "string",
  "intervals": [
    {
      "dateFrom": "string (ISO 8601, UTC)",
      "dateTo": "string (ISO 8601, UTC)"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `isNextSeason` | boolean |
| `periodName` | string |
| `intervals[]` | SeasonPeriodInterval[] (array di oggetti) |
| `intervals[].dateFrom` | string (ISO 8601, UTC) |
| `intervals[].dateTo` | string (ISO 8601, UTC) |

**Response tabella**

Nessun corpo (204).

### POST `/api/seasons/delete-period`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idPeriod": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idPeriod` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/seasons/update-period`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idPeriod": "string (UUID)",
  "periodName": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idPeriod` | string (UUID) |
| `periodName` | string |

**Response tabella**

Nessun corpo (204).

### POST `/api/seasons/add-interval`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idPeriod": "string (UUID)",
  "dateFrom": "string (ISO 8601, UTC)",
  "dateTo": "string (ISO 8601, UTC)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idPeriod` | string (UUID) |
| `dateFrom` | string (ISO 8601, UTC) |
| `dateTo` | string (ISO 8601, UTC) |

**Response tabella**

Nessun corpo (204).

### POST `/api/seasons/update-interval`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idInterval": "string (UUID)",
  "dateFrom": "string (ISO 8601, UTC)",
  "dateTo": "string (ISO 8601, UTC)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idInterval` | string (UUID) |
| `dateFrom` | string (ISO 8601, UTC) |
| `dateTo` | string (ISO 8601, UTC) |

**Response tabella**

Nessun corpo (204).

### POST `/api/seasons/delete-intervals`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idIntervals": "string[] (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idIntervals` | string[] (UUID) |

**Response tabella**

Nessun corpo (204).

## Settori e file

### POST `/api/catalog/sectors-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idSector": "string (UUID)",
      "sectorName": "string | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | SectorItem[] (array di oggetti) |
| `rows[].idSector` | string (UUID) |
| `rows[].sectorName` | string \| null |

### POST `/api/catalog/add-sector-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "sectorName": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `sectorName` | string |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-sector-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idSector": "string (UUID)",
  "sectorName": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idSector` | string (UUID) |
| `sectorName` | string |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/delete-sector-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idSectors": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idSectors` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-sector-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idSectors": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idSectors` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

## Configura mappa

### GET `/api/catalog/map/settings`

**Payload JSON**

Nessun payload.

**Response JSON**

```json
{
  "rows": "number (intero)",
  "columns": "number (intero)",
  "tiltDegrees": "number (intero)",
  "backgroundColor": "string (#RRGGBB) | null",
  "background": {
    "iconId": "string (UUID) | null",
    "name": "string | null",
    "url": "string (URL) | null",
    "isCustom": "boolean"
  },
  "icons": {
    "umbrella": {
      "free": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      },
      "occupied": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      }
    },
    "customArticles": [
      {
        "variant": "number (intero)",
        "icons": {
          "free": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          },
          "occupied": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          }
        }
      }
    ]
  },
  "resetDeletesBookings": "boolean"
}
```

**Payload tabella**

Nessun payload.

**Response tabella**

| Campo | Tipo |
|---|---|
| `rows` | number (intero) |
| `columns` | number (intero) |
| `tiltDegrees` | number (intero) |
| `backgroundColor` | string (#RRGGBB) \| null |
| `background` | MapBackground \| null |
| `background.iconId` | string (UUID) \| null |
| `background.name` | string \| null |
| `background.url` | string (URL) \| null |
| `background.isCustom` | boolean |
| `icons` | MapIcons |
| `icons.umbrella` | MapIconPair |
| `icons.umbrella.free` | MapIcon |
| `icons.umbrella.free.iconId` | string (UUID) \| null |
| `icons.umbrella.free.name` | string \| null |
| `icons.umbrella.free.url` | string (URL) \| null |
| `icons.umbrella.free.isDefault` | boolean |
| `icons.umbrella.occupied` | MapIcon |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null |
| `icons.umbrella.occupied.name` | string \| null |
| `icons.umbrella.occupied.url` | string (URL) \| null |
| `icons.umbrella.occupied.isDefault` | boolean |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) |
| `icons.customArticles[].variant` | number (intero) |
| `icons.customArticles[].icons` | MapIconPair |
| `icons.customArticles[].icons.free` | MapIcon |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.free.name` | string \| null |
| `icons.customArticles[].icons.free.url` | string (URL) \| null |
| `icons.customArticles[].icons.free.isDefault` | boolean |
| `icons.customArticles[].icons.occupied` | MapIcon |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.occupied.name` | string \| null |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null |
| `icons.customArticles[].icons.occupied.isDefault` | boolean |
| `resetDeletesBookings` | boolean |

### PUT `/api/catalog/map/update-background`

**Payload JSON**

```json
{
  "backgroundName": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `backgroundName` | string |

**Response tabella**

Nessun corpo (204).

### PUT `/api/catalog/map/update-configuration`

**Payload JSON**

```json
{
  "rowQuantity": "number (intero)",
  "maxRowItemQuantity": "number (intero)",
  "tiltDegrees": "number (intero)",
  "umbrellaIcons": {
    "freeIcon": "string | null",
    "occupiedIcon": "string | null"
  },
  "customArticleIcons": [
    {
      "variant": "number (intero)",
      "freeIcon": "string | null",
      "occupiedIcon": "string | null"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `rowQuantity` | number (intero) |
| `maxRowItemQuantity` | number (intero) |
| `tiltDegrees` | number (intero) |
| `umbrellaIcons` | MapElementIcons |
| `umbrellaIcons.freeIcon` | string \| null |
| `umbrellaIcons.occupiedIcon` | string \| null |
| `customArticleIcons[]` | MapArticleIcons[] (array di oggetti) |
| `customArticleIcons[].variant` | number (intero) |
| `customArticleIcons[].freeIcon` | string \| null |
| `customArticleIcons[].occupiedIcon` | string \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/map/background`

**Payload JSON**

```json
{
  "file": "file (multipart/form-data)"
}
```

**Response JSON**

```json
{
  "rows": "number (intero)",
  "columns": "number (intero)",
  "tiltDegrees": "number (intero)",
  "backgroundColor": "string (#RRGGBB) | null",
  "background": {
    "iconId": "string (UUID) | null",
    "name": "string | null",
    "url": "string (URL) | null",
    "isCustom": "boolean"
  },
  "icons": {
    "umbrella": {
      "free": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      },
      "occupied": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      }
    },
    "customArticles": [
      {
        "variant": "number (intero)",
        "icons": {
          "free": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          },
          "occupied": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          }
        }
      }
    ]
  },
  "resetDeletesBookings": "boolean"
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `file` | file (multipart/form-data) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `rows` | number (intero) |
| `columns` | number (intero) |
| `tiltDegrees` | number (intero) |
| `backgroundColor` | string (#RRGGBB) \| null |
| `background` | MapBackground \| null |
| `background.iconId` | string (UUID) \| null |
| `background.name` | string \| null |
| `background.url` | string (URL) \| null |
| `background.isCustom` | boolean |
| `icons` | MapIcons |
| `icons.umbrella` | MapIconPair |
| `icons.umbrella.free` | MapIcon |
| `icons.umbrella.free.iconId` | string (UUID) \| null |
| `icons.umbrella.free.name` | string \| null |
| `icons.umbrella.free.url` | string (URL) \| null |
| `icons.umbrella.free.isDefault` | boolean |
| `icons.umbrella.occupied` | MapIcon |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null |
| `icons.umbrella.occupied.name` | string \| null |
| `icons.umbrella.occupied.url` | string (URL) \| null |
| `icons.umbrella.occupied.isDefault` | boolean |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) |
| `icons.customArticles[].variant` | number (intero) |
| `icons.customArticles[].icons` | MapIconPair |
| `icons.customArticles[].icons.free` | MapIcon |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.free.name` | string \| null |
| `icons.customArticles[].icons.free.url` | string (URL) \| null |
| `icons.customArticles[].icons.free.isDefault` | boolean |
| `icons.customArticles[].icons.occupied` | MapIcon |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.occupied.name` | string \| null |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null |
| `icons.customArticles[].icons.occupied.isDefault` | boolean |
| `resetDeletesBookings` | boolean |

### DELETE `/api/catalog/map/background`

**Payload JSON**

Nessun payload.

**Response JSON**

```json
{
  "rows": "number (intero)",
  "columns": "number (intero)",
  "tiltDegrees": "number (intero)",
  "backgroundColor": "string (#RRGGBB) | null",
  "background": {
    "iconId": "string (UUID) | null",
    "name": "string | null",
    "url": "string (URL) | null",
    "isCustom": "boolean"
  },
  "icons": {
    "umbrella": {
      "free": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      },
      "occupied": {
        "iconId": "string (UUID) | null",
        "name": "string | null",
        "url": "string (URL) | null",
        "isDefault": "boolean"
      }
    },
    "customArticles": [
      {
        "variant": "number (intero)",
        "icons": {
          "free": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          },
          "occupied": {
            "iconId": "string (UUID) | null",
            "name": "string | null",
            "url": "string (URL) | null",
            "isDefault": "boolean"
          }
        }
      }
    ]
  },
  "resetDeletesBookings": "boolean"
}
```

**Payload tabella**

Nessun payload.

**Response tabella**

| Campo | Tipo |
|---|---|
| `rows` | number (intero) |
| `columns` | number (intero) |
| `tiltDegrees` | number (intero) |
| `backgroundColor` | string (#RRGGBB) \| null |
| `background` | MapBackground \| null |
| `background.iconId` | string (UUID) \| null |
| `background.name` | string \| null |
| `background.url` | string (URL) \| null |
| `background.isCustom` | boolean |
| `icons` | MapIcons |
| `icons.umbrella` | MapIconPair |
| `icons.umbrella.free` | MapIcon |
| `icons.umbrella.free.iconId` | string (UUID) \| null |
| `icons.umbrella.free.name` | string \| null |
| `icons.umbrella.free.url` | string (URL) \| null |
| `icons.umbrella.free.isDefault` | boolean |
| `icons.umbrella.occupied` | MapIcon |
| `icons.umbrella.occupied.iconId` | string (UUID) \| null |
| `icons.umbrella.occupied.name` | string \| null |
| `icons.umbrella.occupied.url` | string (URL) \| null |
| `icons.umbrella.occupied.isDefault` | boolean |
| `icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) |
| `icons.customArticles[].variant` | number (intero) |
| `icons.customArticles[].icons` | MapIconPair |
| `icons.customArticles[].icons.free` | MapIcon |
| `icons.customArticles[].icons.free.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.free.name` | string \| null |
| `icons.customArticles[].icons.free.url` | string (URL) \| null |
| `icons.customArticles[].icons.free.isDefault` | boolean |
| `icons.customArticles[].icons.occupied` | MapIcon |
| `icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null |
| `icons.customArticles[].icons.occupied.name` | string \| null |
| `icons.customArticles[].icons.occupied.url` | string (URL) \| null |
| `icons.customArticles[].icons.occupied.isDefault` | boolean |
| `resetDeletesBookings` | boolean |

### POST `/api/catalog/map/reset`

**Payload JSON**

Nessun payload.

**Response JSON**

```json
{
  "settings": {
    "rows": "number (intero)",
    "columns": "number (intero)",
    "tiltDegrees": "number (intero)",
    "backgroundColor": "string (#RRGGBB) | null",
    "background": {
      "iconId": "string (UUID) | null",
      "name": "string | null",
      "url": "string (URL) | null",
      "isCustom": "boolean"
    },
    "icons": {
      "umbrella": {
        "free": {
          "iconId": "string (UUID) | null",
          "name": "string | null",
          "url": "string (URL) | null",
          "isDefault": "boolean"
        },
        "occupied": {
          "iconId": "string (UUID) | null",
          "name": "string | null",
          "url": "string (URL) | null",
          "isDefault": "boolean"
        }
      },
      "customArticles": [
        {
          "variant": "number (intero)",
          "icons": {
            "free": {
              "iconId": "string (UUID) | null",
              "name": "string | null",
              "url": "string (URL) | null",
              "isDefault": "boolean"
            },
            "occupied": {
              "iconId": "string (UUID) | null",
              "name": "string | null",
              "url": "string (URL) | null",
              "isDefault": "boolean"
            }
          }
        }
      ]
    },
    "resetDeletesBookings": "boolean"
  },
  "spots": "array di oggetti",
  "sectors": "array di oggetti",
  "beachElements": "array di oggetti"
}
```

**Payload tabella**

Nessun payload.

**Response tabella**

| Campo | Tipo |
|---|---|
| `settings` | MapSettings |
| `settings.rows` | number (intero) |
| `settings.columns` | number (intero) |
| `settings.tiltDegrees` | number (intero) |
| `settings.backgroundColor` | string (#RRGGBB) \| null |
| `settings.background` | MapBackground \| null |
| `settings.background.iconId` | string (UUID) \| null |
| `settings.background.name` | string \| null |
| `settings.background.url` | string (URL) \| null |
| `settings.background.isCustom` | boolean |
| `settings.icons` | MapIcons |
| `settings.icons.umbrella` | MapIconPair |
| `settings.icons.umbrella.free` | MapIcon |
| `settings.icons.umbrella.free.iconId` | string (UUID) \| null |
| `settings.icons.umbrella.free.name` | string \| null |
| `settings.icons.umbrella.free.url` | string (URL) \| null |
| `settings.icons.umbrella.free.isDefault` | boolean |
| `settings.icons.umbrella.occupied` | MapIcon |
| `settings.icons.umbrella.occupied.iconId` | string (UUID) \| null |
| `settings.icons.umbrella.occupied.name` | string \| null |
| `settings.icons.umbrella.occupied.url` | string (URL) \| null |
| `settings.icons.umbrella.occupied.isDefault` | boolean |
| `settings.icons.customArticles[]` | MapCustomArticleIcons[] (array di oggetti) |
| `settings.icons.customArticles[].variant` | number (intero) |
| `settings.icons.customArticles[].icons` | MapIconPair |
| `settings.icons.customArticles[].icons.free` | MapIcon |
| `settings.icons.customArticles[].icons.free.iconId` | string (UUID) \| null |
| `settings.icons.customArticles[].icons.free.name` | string \| null |
| `settings.icons.customArticles[].icons.free.url` | string (URL) \| null |
| `settings.icons.customArticles[].icons.free.isDefault` | boolean |
| `settings.icons.customArticles[].icons.occupied` | MapIcon |
| `settings.icons.customArticles[].icons.occupied.iconId` | string (UUID) \| null |
| `settings.icons.customArticles[].icons.occupied.name` | string \| null |
| `settings.icons.customArticles[].icons.occupied.url` | string (URL) \| null |
| `settings.icons.customArticles[].icons.occupied.isDefault` | boolean |
| `settings.resetDeletesBookings` | boolean |
| `spots` | array di oggetti |
| `sectors` | array di oggetti |
| `beachElements` | array di oggetti |

## Impostazioni magazzino

### POST `/api/warehouse/list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idArticle": "string (UUID)",
      "name": "string",
      "totalQuantity": "number (intero)",
      "availableQuantity": "number (intero)",
      "thresholdQuantity": "number (intero) | null",
      "isThresholdWarningActive": "boolean"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | WarehouseItem[] (array di oggetti) |
| `rows[].idArticle` | string (UUID) |
| `rows[].name` | string |
| `rows[].totalQuantity` | number (intero) |
| `rows[].availableQuantity` | number (intero) |
| `rows[].thresholdQuantity` | number (intero) \| null |
| `rows[].isThresholdWarningActive` | boolean |

### POST `/api/warehouse/article-combobox`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "id": "string (UUID)",
    "value": "string"
  }
]
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (UUID) |
| `[].value` | string |

### POST `/api/warehouse/add-warehouse-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idArticle": "string (UUID)",
  "name": "string | null",
  "totalQuantity": "number (intero > 0)",
  "availableQuantity": "number (intero) | null",
  "thresholdQuantity": "number (intero ≥ 0) | null",
  "isThresholdWarningActive": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idArticle` | string (UUID) |
| `name` | string \| null |
| `totalQuantity` | number (intero > 0) |
| `availableQuantity` | number (intero) \| null |
| `thresholdQuantity` | number (intero ≥ 0) \| null |
| `isThresholdWarningActive` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/warehouse/edit-warehouse-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idArticle": "string (UUID)",
  "name": "string | null",
  "totalQuantity": "number (intero > 0)",
  "availableQuantity": "number (intero) | null",
  "thresholdQuantity": "number (intero ≥ 0) | null",
  "isThresholdWarningActive": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idArticle` | string (UUID) |
| `name` | string \| null |
| `totalQuantity` | number (intero > 0) |
| `availableQuantity` | number (intero) \| null |
| `thresholdQuantity` | number (intero ≥ 0) \| null |
| `isThresholdWarningActive` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/warehouse/delete-warehouse-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idItems": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idItems` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/warehouse/duplicate-warehouse-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idItems": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idItems` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

## Sconti e voucher

### POST `/api/vouchers/voucher-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idVoucher": "string (UUID)",
      "voucherName": "string | null",
      "validFrom": "string (data ISO 8601 UTC) | null",
      "validTo": "string (data ISO 8601 UTC) | null",
      "discountType": "string (enum DiscountType) | null",
      "discountValue": "number (decimale) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | VoucherItem[] (array di oggetti) |
| `rows[].idVoucher` | string (UUID) |
| `rows[].voucherName` | string \| null |
| `rows[].validFrom` | string (data ISO 8601 UTC) \| null |
| `rows[].validTo` | string (data ISO 8601 UTC) \| null |
| `rows[].discountType` | string (enum DiscountType) \| null |
| `rows[].discountValue` | number (decimale) \| null |

### POST `/api/vouchers/add-voucher-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "voucherName": "string",
  "validityPeriods": [
    {
      "validFrom": "string (data ISO 8601 UTC)",
      "validTo": "string (data ISO 8601 UTC)",
      "daysOfWeek": "number[] (interi da 1 a 7, almeno uno)",
      "timeSlots": [
        {
          "from": "string (HH:mm, 24 ore)",
          "to": "string (HH:mm, 24 ore)"
        }
      ]
    }
  ],
  "discountType": "string (enum DiscountType)",
  "discountValue": "number (decimale)",
  "discountScope": "string (enum DiscountScope)",
  "maxUseQuantity": "number (intero ≥ 1)",
  "isEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `voucherName` | string |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, almeno uno) |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) |
| `discountType` | string (enum DiscountType) |
| `discountValue` | number (decimale) |
| `discountScope` | string (enum DiscountScope) |
| `maxUseQuantity` | number (intero ≥ 1) |
| `isEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### GET `/api/vouchers/get-selected-item?idVoucher={idVoucher}`

**Payload JSON** (query string)

```json
{
  "idVoucher": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idVoucher": "string (UUID)",
  "voucherName": "string | null",
  "validityPeriods": [
    {
      "validFrom": "string (data ISO 8601 UTC)",
      "validTo": "string (data ISO 8601 UTC)",
      "daysOfWeek": "number[] (interi da 1 a 7, almeno uno)",
      "timeSlots": [
        {
          "from": "string (HH:mm, 24 ore)",
          "to": "string (HH:mm, 24 ore)"
        }
      ]
    }
  ],
  "discountType": "string (enum DiscountType) | null",
  "discountValue": "number (decimale) | null",
  "discountScope": "string (enum DiscountScope) | null",
  "maxUseQuantity": "number (intero) | null",
  "isEnabled": "boolean | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idVoucher` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idVoucher` | string (UUID) |
| `voucherName` | string \| null |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti) |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) |
| `discountType` | string (enum DiscountType) \| null |
| `discountValue` | number (decimale) \| null |
| `discountScope` | string (enum DiscountScope) \| null |
| `maxUseQuantity` | number (intero) \| null |
| `isEnabled` | boolean \| null |

### POST `/api/vouchers/update-voucher-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idVoucher": "string (UUID)",
  "voucherName": "string",
  "validityPeriods": [
    {
      "validFrom": "string (data ISO 8601 UTC)",
      "validTo": "string (data ISO 8601 UTC)",
      "daysOfWeek": "number[] (interi da 1 a 7, almeno uno)",
      "timeSlots": [
        {
          "from": "string (HH:mm, 24 ore)",
          "to": "string (HH:mm, 24 ore)"
        }
      ]
    }
  ],
  "discountType": "string (enum DiscountType)",
  "discountValue": "number (decimale)",
  "discountScope": "string (enum DiscountScope)",
  "maxUseQuantity": "number (intero ≥ 1)",
  "isEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idVoucher` | string (UUID) |
| `voucherName` | string |
| `validityPeriods[]` | VoucherValidityPeriod[] (array di oggetti, almeno uno) |
| `validityPeriods[].validFrom` | string (data ISO 8601 UTC) |
| `validityPeriods[].validTo` | string (data ISO 8601 UTC) |
| `validityPeriods[].daysOfWeek` | number[] (interi da 1 a 7, almeno uno) |
| `validityPeriods[].timeSlots[]` | VoucherTimeSlot[] (array di oggetti, anche vuoto) |
| `validityPeriods[].timeSlots[].from` | string (HH:mm, 24 ore) |
| `validityPeriods[].timeSlots[].to` | string (HH:mm, 24 ore) |
| `discountType` | string (enum DiscountType) |
| `discountValue` | number (decimale) |
| `discountScope` | string (enum DiscountScope) |
| `maxUseQuantity` | number (intero ≥ 1) |
| `isEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/vouchers/delete-voucher-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idVouchers": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idVouchers` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/vouchers/duplicate-voucher-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idVouchers": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idVouchers` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

## Servizi

### POST `/api/catalog/bar-service-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idBarService": "string (UUID)",
      "serviceName": "string | null",
      "category": "string (enum BarServiceCategory) | null",
      "price": "number (decimale, €) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | BarServiceItem[] (array di oggetti) |
| `rows[].idBarService` | string (UUID) |
| `rows[].serviceName` | string \| null |
| `rows[].category` | string (enum BarServiceCategory) \| null |
| `rows[].price` | number (decimale, €) \| null |

### POST `/api/catalog/sector-combobox`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "id": "string (UUID)",
    "value": "string"
  }
]
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (UUID) |
| `[].value` | string |

### POST `/api/catalog/create-bar-service`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "category": "string (enum BarServiceCategory) | null",
  "icon": "string (enum BarServiceIcon) | null",
  "isHighlighted": "boolean",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "variants": [
    {
      "variantName": "string | null",
      "price": "number (decimale, €) | null"
    }
  ],
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
      "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
      "managementStartTime": "string (HH:mm) | null",
      "managementEndTime": "string (HH:mm) | null",
      "onlineStartTime": "string (HH:mm) | null",
      "onlineEndTime": "string (HH:mm) | null",
      "maxBookingPerHourQuantity": "number (intero) | null",
      "maxBookingPerDayQuantity": "number (intero) | null",
      "timeInterval": "string (enum BookingTimeInterval) | null",
      "minItemPerBookingQuantity": "number (intero) | null",
      "maxItemPerBookingQuantity": "number (intero) | null"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `category` | string (enum BarServiceCategory) \| null |
| `icon` | string (enum BarServiceIcon) \| null |
| `isHighlighted` | boolean |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `variants[]` | ServiceVariant[] (array di oggetti) |
| `variants[].variantName` | string \| null |
| `variants[].price` | number (decimale, €) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null |
| `bookingOptions[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/get-bar-service-general-info?idBarService={idBarService}`

**Payload JSON** (query string)

```json
{
  "idBarService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idBarService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "category": "string (enum BarServiceCategory) | null",
  "icon": "string (enum BarServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idBarService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idBarService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `category` | string (enum BarServiceCategory) \| null |
| `icon` | string (enum BarServiceIcon) \| null |
| `isHighlighted` | boolean |

### GET `/api/catalog/get-bar-service-prices?idBarService={idBarService}`

**Payload JSON** (query string)

```json
{
  "idBarService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "variants": [
    {
      "variantName": "string | null",
      "price": "number (decimale, €) | null"
    }
  ],
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idBarService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `variants[]` | ServiceVariant[] (array di oggetti) |
| `variants[].variantName` | string \| null |
| `variants[].price` | number (decimale, €) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

### GET `/api/catalog/get-bar-service-booking-options?idBarService={idBarService}`

**Payload JSON** (query string)

```json
{
  "idBarService": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
    "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
    "managementStartTime": "string (HH:mm) | null",
    "managementEndTime": "string (HH:mm) | null",
    "onlineStartTime": "string (HH:mm) | null",
    "onlineEndTime": "string (HH:mm) | null",
    "maxBookingPerHourQuantity": "number (intero) | null",
    "maxBookingPerDayQuantity": "number (intero) | null",
    "timeInterval": "string (enum BookingTimeInterval) | null",
    "minItemPerBookingQuantity": "number (intero) | null",
    "maxItemPerBookingQuantity": "number (intero) | null"
  }
]
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idBarService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `[].managementStartTime` | string (HH:mm) \| null |
| `[].managementEndTime` | string (HH:mm) \| null |
| `[].onlineStartTime` | string (HH:mm) \| null |
| `[].onlineEndTime` | string (HH:mm) \| null |
| `[].maxBookingPerHourQuantity` | number (intero) \| null |
| `[].maxBookingPerDayQuantity` | number (intero) \| null |
| `[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `[].minItemPerBookingQuantity` | number (intero) \| null |
| `[].maxItemPerBookingQuantity` | number (intero) \| null |

### POST `/api/catalog/update-bar-service-general`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBarService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "category": "string (enum BarServiceCategory) | null",
  "icon": "string (enum BarServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBarService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `category` | string (enum BarServiceCategory) \| null |
| `icon` | string (enum BarServiceIcon) \| null |
| `isHighlighted` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-bar-service-prices`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBarService": "string (UUID)",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "variants": [
    {
      "variantName": "string | null",
      "price": "number (decimale, €) | null"
    }
  ],
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBarService` | string (UUID) |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `variants[]` | ServiceVariant[] (array di oggetti) |
| `variants[].variantName` | string \| null |
| `variants[].price` | number (decimale, €) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-bar-service-booking`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBarService": "string (UUID)",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
      "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
      "managementStartTime": "string (HH:mm) | null",
      "managementEndTime": "string (HH:mm) | null",
      "onlineStartTime": "string (HH:mm) | null",
      "onlineEndTime": "string (HH:mm) | null",
      "maxBookingPerHourQuantity": "number (intero) | null",
      "maxBookingPerDayQuantity": "number (intero) | null",
      "timeInterval": "string (enum BookingTimeInterval) | null",
      "minItemPerBookingQuantity": "number (intero) | null",
      "maxItemPerBookingQuantity": "number (intero) | null"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBarService` | string (UUID) |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null |
| `bookingOptions[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/delete-bar-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBarServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBarServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-bar-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBarServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBarServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/standard-service-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idStandardService": "string (UUID)",
      "serviceName": "string | null",
      "price": "number (decimale, €) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | StandardServiceItem[] (array di oggetti) |
| `rows[].idStandardService` | string (UUID) |
| `rows[].serviceName` | string \| null |
| `rows[].price` | number (decimale, €) \| null |

### POST `/api/catalog/create-standard-service`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
      "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
      "managementStartTime": "string (HH:mm) | null",
      "managementEndTime": "string (HH:mm) | null",
      "onlineStartTime": "string (HH:mm) | null",
      "onlineEndTime": "string (HH:mm) | null",
      "maxBookingPerHourQuantity": "number (intero) | null",
      "maxBookingPerDayQuantity": "number (intero) | null",
      "timeInterval": "string (enum BookingTimeInterval) | null",
      "minItemPerBookingQuantity": "number (intero) | null",
      "maxItemPerBookingQuantity": "number (intero) | null"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null |
| `bookingOptions[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/get-standard-service-general-info?idStandardService={idStandardService}`

**Payload JSON** (query string)

```json
{
  "idStandardService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idStandardService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idStandardService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idStandardService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |

### GET `/api/catalog/get-standard-service-prices?idStandardService={idStandardService}`

**Payload JSON** (query string)

```json
{
  "idStandardService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idStandardService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

### GET `/api/catalog/get-standard-service-booking-options?idStandardService={idStandardService}`

**Payload JSON** (query string)

```json
{
  "idStandardService": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
    "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
    "managementStartTime": "string (HH:mm) | null",
    "managementEndTime": "string (HH:mm) | null",
    "onlineStartTime": "string (HH:mm) | null",
    "onlineEndTime": "string (HH:mm) | null",
    "maxBookingPerHourQuantity": "number (intero) | null",
    "maxBookingPerDayQuantity": "number (intero) | null",
    "timeInterval": "string (enum BookingTimeInterval) | null",
    "minItemPerBookingQuantity": "number (intero) | null",
    "maxItemPerBookingQuantity": "number (intero) | null"
  }
]
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idStandardService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `[].managementStartTime` | string (HH:mm) \| null |
| `[].managementEndTime` | string (HH:mm) \| null |
| `[].onlineStartTime` | string (HH:mm) \| null |
| `[].onlineEndTime` | string (HH:mm) \| null |
| `[].maxBookingPerHourQuantity` | number (intero) \| null |
| `[].maxBookingPerDayQuantity` | number (intero) \| null |
| `[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `[].minItemPerBookingQuantity` | number (intero) \| null |
| `[].maxItemPerBookingQuantity` | number (intero) \| null |

### POST `/api/catalog/update-standard-service-general`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idStandardService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idStandardService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-standard-service-prices`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idStandardService": "string (UUID)",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isPeriodPricingEnabled": "boolean",
  "periodPrices": [
    {
      "idPeriod": "string (UUID)",
      "price": "number (decimale, €) | null"
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idStandardService` | string (UUID) |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isPeriodPricingEnabled` | boolean |
| `periodPrices[]` | ServicePeriodPrice[] (array di oggetti) |
| `periodPrices[].idPeriod` | string (UUID) |
| `periodPrices[].price` | number (decimale, €) \| null |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-standard-service-booking`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idStandardService": "string (UUID)",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, mezzanotte UTC) | null",
      "dateTo": "string (ISO 8601, mezzanotte UTC) | null",
      "managementStartTime": "string (HH:mm) | null",
      "managementEndTime": "string (HH:mm) | null",
      "onlineStartTime": "string (HH:mm) | null",
      "onlineEndTime": "string (HH:mm) | null",
      "maxBookingPerHourQuantity": "number (intero) | null",
      "maxBookingPerDayQuantity": "number (intero) | null",
      "timeInterval": "string (enum BookingTimeInterval) | null",
      "minItemPerBookingQuantity": "number (intero) | null",
      "maxItemPerBookingQuantity": "number (intero) | null"
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idStandardService` | string (UUID) |
| `bookingOptions[]` | ServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, mezzanotte UTC) \| null |
| `bookingOptions[].managementStartTime` | string (HH:mm) \| null |
| `bookingOptions[].managementEndTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineStartTime` | string (HH:mm) \| null |
| `bookingOptions[].onlineEndTime` | string (HH:mm) \| null |
| `bookingOptions[].maxBookingPerHourQuantity` | number (intero) \| null |
| `bookingOptions[].maxBookingPerDayQuantity` | number (intero) \| null |
| `bookingOptions[].timeInterval` | string (enum BookingTimeInterval) \| null |
| `bookingOptions[].minItemPerBookingQuantity` | number (intero) \| null |
| `bookingOptions[].maxItemPerBookingQuantity` | number (intero) \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/delete-standard-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idStandardServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idStandardServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-standard-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idStandardServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idStandardServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/hourly-service-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idHourlyService": "string (UUID)",
      "serviceName": "string | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | HourlyServiceItem[] (array di oggetti) |
| `rows[].idHourlyService` | string (UUID) |
| `rows[].serviceName` | string \| null |

### POST `/api/catalog/delete-hourly-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idHourlyServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idHourlyServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-hourly-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idHourlyServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idHourlyServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/create-hourly-service`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean",
  "isPeriodPricingEnabled": "boolean",
  "periodAvailabilities": [
    {
      "idPeriod": "string (UUID)",
      "availabilityOptions": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null",
          "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
          "timeSlotPrices": [
            {
              "timeSlot": "string (enum ServiceTimeSlot)",
              "isEnabled": "boolean",
              "price": "number (decimale, €) | null"
            }
          ],
          "isCustomTimeSlotEnabled": "boolean",
          "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
          "customTimeSlotPrice": "number (decimale, €) | null",
          "vatRate": "number (decimale, %) | null",
          "maxBookingPerTimeSlotQuantity": "number (intero) | null",
          "hasFixedTimeSlots": "boolean"
        }
      ]
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "availabilityOption": {
        "startTime": "string (HH:mm) | null",
        "endTime": "string (HH:mm) | null",
        "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
        "timeSlotPrices": [
          {
            "timeSlot": "string (enum ServiceTimeSlot)",
            "isEnabled": "boolean",
            "price": "number (decimale, €) | null"
          }
        ],
        "isCustomTimeSlotEnabled": "boolean",
        "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
        "customTimeSlotPrice": "number (decimale, €) | null",
        "vatRate": "number (decimale, %) | null",
        "maxBookingPerTimeSlotQuantity": "number (intero) | null",
        "hasFixedTimeSlots": "boolean"
      }
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |
| `isPeriodPricingEnabled` | boolean |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) |
| `periodAvailabilities[].idPeriod` | string (UUID) |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |
| `bookingOptions[]` | HourlyServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, UTC) \| null |
| `bookingOptions[].availabilityOption` | ServiceAvailabilityOption (oggetto) |
| `bookingOptions[].availabilityOption.startTime` | string (HH:mm) \| null |
| `bookingOptions[].availabilityOption.endTime` | string (HH:mm) \| null |
| `bookingOptions[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `bookingOptions[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `bookingOptions[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `bookingOptions[].availabilityOption.timeSlotPrices[].isEnabled` | boolean |
| `bookingOptions[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null |
| `bookingOptions[].availabilityOption.isCustomTimeSlotEnabled` | boolean |
| `bookingOptions[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `bookingOptions[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null |
| `bookingOptions[].availabilityOption.vatRate` | number (decimale, %) \| null |
| `bookingOptions[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `bookingOptions[].availabilityOption.hasFixedTimeSlots` | boolean |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/get-hourly-service-general-info?idHourlyService={idHourlyService}`

**Payload JSON** (query string)

```json
{
  "idHourlyService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idHourlyService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idHourlyService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idHourlyService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |

### GET `/api/catalog/get-hourly-service-prices?idHourlyService={idHourlyService}`

**Payload JSON** (query string)

```json
{
  "idHourlyService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "isPeriodPricingEnabled": "boolean",
  "periodAvailabilities": [
    {
      "idPeriod": "string (UUID)",
      "availabilityOptions": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null",
          "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
          "timeSlotPrices": [
            {
              "timeSlot": "string (enum ServiceTimeSlot)",
              "isEnabled": "boolean",
              "price": "number (decimale, €) | null"
            }
          ],
          "isCustomTimeSlotEnabled": "boolean",
          "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
          "customTimeSlotPrice": "number (decimale, €) | null",
          "vatRate": "number (decimale, %) | null",
          "maxBookingPerTimeSlotQuantity": "number (intero) | null",
          "hasFixedTimeSlots": "boolean"
        }
      ]
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idHourlyService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `isPeriodPricingEnabled` | boolean |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) |
| `periodAvailabilities[].idPeriod` | string (UUID) |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

### GET `/api/catalog/get-hourly-service-booking-options?idHourlyService={idHourlyService}`

**Payload JSON** (query string)

```json
{
  "idHourlyService": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "dateFrom": "string (ISO 8601, UTC) | null",
    "dateTo": "string (ISO 8601, UTC) | null",
    "availabilityOption": {
      "startTime": "string (HH:mm) | null",
      "endTime": "string (HH:mm) | null",
      "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
      "timeSlotPrices": [
        {
          "timeSlot": "string (enum ServiceTimeSlot)",
          "isEnabled": "boolean",
          "price": "number (decimale, €) | null"
        }
      ],
      "isCustomTimeSlotEnabled": "boolean",
      "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
      "customTimeSlotPrice": "number (decimale, €) | null",
      "vatRate": "number (decimale, %) | null",
      "maxBookingPerTimeSlotQuantity": "number (intero) | null",
      "hasFixedTimeSlots": "boolean"
    }
  }
]
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idHourlyService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].dateFrom` | string (ISO 8601, UTC) \| null |
| `[].dateTo` | string (ISO 8601, UTC) \| null |
| `[].availabilityOption` | ServiceAvailabilityOption (oggetto) |
| `[].availabilityOption.startTime` | string (HH:mm) \| null |
| `[].availabilityOption.endTime` | string (HH:mm) \| null |
| `[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `[].availabilityOption.timeSlotPrices[].isEnabled` | boolean |
| `[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null |
| `[].availabilityOption.isCustomTimeSlotEnabled` | boolean |
| `[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null |
| `[].availabilityOption.vatRate` | number (decimale, %) \| null |
| `[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `[].availabilityOption.hasFixedTimeSlots` | boolean |

### POST `/api/catalog/update-hourly-service-general`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idHourlyService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum StandardServiceIcon) | null",
  "isHighlighted": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idHourlyService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum StandardServiceIcon) \| null |
| `isHighlighted` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-hourly-service-prices`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idHourlyService": "string (UUID)",
  "isPeriodPricingEnabled": "boolean",
  "periodAvailabilities": [
    {
      "idPeriod": "string (UUID)",
      "availabilityOptions": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null",
          "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
          "timeSlotPrices": [
            {
              "timeSlot": "string (enum ServiceTimeSlot)",
              "isEnabled": "boolean",
              "price": "number (decimale, €) | null"
            }
          ],
          "isCustomTimeSlotEnabled": "boolean",
          "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
          "customTimeSlotPrice": "number (decimale, €) | null",
          "vatRate": "number (decimale, %) | null",
          "maxBookingPerTimeSlotQuantity": "number (intero) | null",
          "hasFixedTimeSlots": "boolean"
        }
      ]
    }
  ],
  "idSectors": "string[] (UUID)",
  "isOnlinePurchaseEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idHourlyService` | string (UUID) |
| `isPeriodPricingEnabled` | boolean |
| `periodAvailabilities[]` | ServicePeriodAvailability[] (array di oggetti) |
| `periodAvailabilities[].idPeriod` | string (UUID) |
| `periodAvailabilities[].availabilityOptions[]` | ServiceAvailabilityOption[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].startTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].endTime` | string (HH:mm) \| null |
| `periodAvailabilities[].availabilityOptions[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].isEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].timeSlotPrices[].price` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].isCustomTimeSlotEnabled` | boolean |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `periodAvailabilities[].availabilityOptions[].customTimeSlotPrice` | number (decimale, €) \| null |
| `periodAvailabilities[].availabilityOptions[].vatRate` | number (decimale, %) \| null |
| `periodAvailabilities[].availabilityOptions[].maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `periodAvailabilities[].availabilityOptions[].hasFixedTimeSlots` | boolean |
| `idSectors` | string[] (UUID) |
| `isOnlinePurchaseEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-hourly-service-booking`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idHourlyService": "string (UUID)",
  "bookingOptions": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "availabilityOption": {
        "startTime": "string (HH:mm) | null",
        "endTime": "string (HH:mm) | null",
        "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
        "timeSlotPrices": [
          {
            "timeSlot": "string (enum ServiceTimeSlot)",
            "isEnabled": "boolean",
            "price": "number (decimale, €) | null"
          }
        ],
        "isCustomTimeSlotEnabled": "boolean",
        "customTimeSlotMinuteQuantity": "number (intero, minuti) | null",
        "customTimeSlotPrice": "number (decimale, €) | null",
        "vatRate": "number (decimale, %) | null",
        "maxBookingPerTimeSlotQuantity": "number (intero) | null",
        "hasFixedTimeSlots": "boolean"
      }
    }
  ]
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idHourlyService` | string (UUID) |
| `bookingOptions[]` | HourlyServiceBookingOption[] (array di oggetti) |
| `bookingOptions[].dateFrom` | string (ISO 8601, UTC) \| null |
| `bookingOptions[].dateTo` | string (ISO 8601, UTC) \| null |
| `bookingOptions[].availabilityOption` | ServiceAvailabilityOption (oggetto) |
| `bookingOptions[].availabilityOption.startTime` | string (HH:mm) \| null |
| `bookingOptions[].availabilityOption.endTime` | string (HH:mm) \| null |
| `bookingOptions[].availabilityOption.daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `bookingOptions[].availabilityOption.timeSlotPrices[]` | ServiceTimeSlotPrice[] (array di oggetti) |
| `bookingOptions[].availabilityOption.timeSlotPrices[].timeSlot` | string (enum ServiceTimeSlot) |
| `bookingOptions[].availabilityOption.timeSlotPrices[].isEnabled` | boolean |
| `bookingOptions[].availabilityOption.timeSlotPrices[].price` | number (decimale, €) \| null |
| `bookingOptions[].availabilityOption.isCustomTimeSlotEnabled` | boolean |
| `bookingOptions[].availabilityOption.customTimeSlotMinuteQuantity` | number (intero, minuti) \| null |
| `bookingOptions[].availabilityOption.customTimeSlotPrice` | number (decimale, €) \| null |
| `bookingOptions[].availabilityOption.vatRate` | number (decimale, %) \| null |
| `bookingOptions[].availabilityOption.maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `bookingOptions[].availabilityOption.hasFixedTimeSlots` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/automatic-price-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idAutomaticPrice": "string (UUID)",
      "serviceName": "string | null",
      "price": "number (decimale, €) | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | AutomaticPriceItem[] (array di oggetti) |
| `rows[].idAutomaticPrice` | string (UUID) |
| `rows[].serviceName` | string \| null |
| `rows[].price` | number (decimale, €) \| null |

### GET `/api/catalog/get-automatic-price-services?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "barServices": [
    {
      "id": "string (UUID)",
      "value": "string"
    }
  ],
  "standardServices": [
    {
      "id": "string (UUID)",
      "value": "string"
    }
  ],
  "hourlyServices": [
    {
      "id": "string (UUID)",
      "value": "string"
    }
  ]
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `barServices[]` | ComboboxItem[] (array di oggetti) |
| `barServices[].id` | string (UUID) |
| `barServices[].value` | string |
| `standardServices[]` | ComboboxItem[] (array di oggetti) |
| `standardServices[].id` | string (UUID) |
| `standardServices[].value` | string |
| `hourlyServices[]` | ComboboxItem[] (array di oggetti) |
| `hourlyServices[].id` | string (UUID) |
| `hourlyServices[].value` | string |

### POST `/api/catalog/create-automatic-price`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum BarServiceIcon o StandardServiceIcon) | null",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isOnlinePurchaseEnabled": "boolean",
  "validityPeriods": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
      "timeSlots": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "idBarServices": "string[] (UUID)",
  "idStandardServices": "string[] (UUID)",
  "idHourlyServices": "string[] (UUID)",
  "isPricePerServiceEnabled": "boolean",
  "idSectors": "string[] (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum BarServiceIcon o StandardServiceIcon) \| null |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isOnlinePurchaseEnabled` | boolean |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) \| null |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) \| null |
| `idBarServices` | string[] (UUID) |
| `idStandardServices` | string[] (UUID) |
| `idHourlyServices` | string[] (UUID) |
| `isPricePerServiceEnabled` | boolean |
| `idSectors` | string[] (UUID) |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/get-automatic-price-general-info?idAutomaticPrice={idAutomaticPrice}`

**Payload JSON** (query string)

```json
{
  "idAutomaticPrice": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idAutomaticPrice": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum BarServiceIcon o StandardServiceIcon) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idAutomaticPrice` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idAutomaticPrice` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum BarServiceIcon o StandardServiceIcon) \| null |

### GET `/api/catalog/get-automatic-price-prices-validity?idAutomaticPrice={idAutomaticPrice}`

**Payload JSON** (query string)

```json
{
  "idAutomaticPrice": "string (UUID)"
}
```

**Response JSON**

```json
{
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isOnlinePurchaseEnabled": "boolean",
  "validityPeriods": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
      "timeSlots": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "idBarServices": "string[] (UUID)",
  "idStandardServices": "string[] (UUID)",
  "idHourlyServices": "string[] (UUID)",
  "isPricePerServiceEnabled": "boolean",
  "idSectors": "string[] (UUID)"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idAutomaticPrice` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isOnlinePurchaseEnabled` | boolean |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) \| null |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) \| null |
| `idBarServices` | string[] (UUID) |
| `idStandardServices` | string[] (UUID) |
| `idHourlyServices` | string[] (UUID) |
| `isPricePerServiceEnabled` | boolean |
| `idSectors` | string[] (UUID) |

### POST `/api/catalog/update-automatic-price-general`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idAutomaticPrice": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "icon": "string (enum BarServiceIcon o StandardServiceIcon) | null"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idAutomaticPrice` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `icon` | string (enum BarServiceIcon o StandardServiceIcon) \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-automatic-price-prices-validity`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idAutomaticPrice": "string (UUID)",
  "price": "number (decimale, €) | null",
  "vatRate": "number (decimale, %) | null",
  "isOnlinePurchaseEnabled": "boolean",
  "validityPeriods": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "daysOfWeek": "number[] (interi ISO 8601: 1 = lunedì … 7 = domenica)",
      "timeSlots": [
        {
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "idBarServices": "string[] (UUID)",
  "idStandardServices": "string[] (UUID)",
  "idHourlyServices": "string[] (UUID)",
  "isPricePerServiceEnabled": "boolean",
  "idSectors": "string[] (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idAutomaticPrice` | string (UUID) |
| `price` | number (decimale, €) \| null |
| `vatRate` | number (decimale, %) \| null |
| `isOnlinePurchaseEnabled` | boolean |
| `validityPeriods[]` | AutomaticPriceValidityPeriod[] (array di oggetti) |
| `validityPeriods[].dateFrom` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].dateTo` | string (ISO 8601, UTC) \| null |
| `validityPeriods[].daysOfWeek` | number[] (interi ISO 8601: 1 = lunedì … 7 = domenica) |
| `validityPeriods[].timeSlots[]` | AutomaticPriceTimeSlot[] (array di oggetti) |
| `validityPeriods[].timeSlots[].startTime` | string (HH:mm) \| null |
| `validityPeriods[].timeSlots[].endTime` | string (HH:mm) \| null |
| `idBarServices` | string[] (UUID) |
| `idStandardServices` | string[] (UUID) |
| `idHourlyServices` | string[] (UUID) |
| `isPricePerServiceEnabled` | boolean |
| `idSectors` | string[] (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/delete-automatic-price-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idAutomaticPrices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idAutomaticPrices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-automatic-price-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idAutomaticPrices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idAutomaticPrices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/table-service-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idTableService": "string (UUID)",
      "serviceName": "string | null"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | TableServiceItem[] (array di oggetti) |
| `rows[].idTableService` | string (UUID) |
| `rows[].serviceName` | string \| null |

### POST `/api/catalog/delete-table-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idTableServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idTableServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/duplicate-table-service-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idTableServices": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idTableServices` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/create-table-service`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "isHighlighted": "boolean",
  "availabilities": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "isSameTimeForAllDays": "boolean",
      "startTime": "string (HH:mm) | null",
      "endTime": "string (HH:mm) | null",
      "days": [
        {
          "dayOfWeek": "number (intero ISO 8601: 1 = lunedì … 7 = domenica)",
          "isEnabled": "boolean",
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "availabilityMode": "string (enum TableServiceAvailabilityMode: Tables, Seats)",
  "tableGroups": [
    {
      "tableQuantity": "number (intero) | null",
      "seatPerTableQuantity": "number (intero) | null"
    }
  ],
  "totalSeatQuantity": "number (intero) | null",
  "averageMealDuration": "string (enum TableServiceMealDuration) | null",
  "maxBookingPerTimeSlotQuantity": "number (intero) | null",
  "bookingPayment": "string (enum TableServiceBookingPayment: None, Deposit)",
  "depositMode": "string (enum TableServiceDepositMode: Total, PerPerson)",
  "depositPercentage": "number (decimale, %) | null",
  "isOnlineBookingEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `isHighlighted` | boolean |
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) \| null |
| `availabilities[].dateTo` | string (ISO 8601, UTC) \| null |
| `availabilities[].isSameTimeForAllDays` | boolean |
| `availabilities[].startTime` | string (HH:mm) \| null |
| `availabilities[].endTime` | string (HH:mm) \| null |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) |
| `availabilities[].days[].isEnabled` | boolean |
| `availabilities[].days[].startTime` | string (HH:mm) \| null |
| `availabilities[].days[].endTime` | string (HH:mm) \| null |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) |
| `tableGroups[].tableQuantity` | number (intero) \| null |
| `tableGroups[].seatPerTableQuantity` | number (intero) \| null |
| `totalSeatQuantity` | number (intero) \| null |
| `averageMealDuration` | string (enum TableServiceMealDuration) \| null |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) |
| `depositPercentage` | number (decimale, %) \| null |
| `isOnlineBookingEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### GET `/api/catalog/get-table-service-general-info?idTableService={idTableService}`

**Payload JSON** (query string)

```json
{
  "idTableService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "idTableService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "isHighlighted": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idTableService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `idTableService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `isHighlighted` | boolean |

### GET `/api/catalog/get-table-service-availability-bookings?idTableService={idTableService}`

**Payload JSON** (query string)

```json
{
  "idTableService": "string (UUID)"
}
```

**Response JSON**

```json
{
  "availabilities": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "isSameTimeForAllDays": "boolean",
      "startTime": "string (HH:mm) | null",
      "endTime": "string (HH:mm) | null",
      "days": [
        {
          "dayOfWeek": "number (intero ISO 8601: 1 = lunedì … 7 = domenica)",
          "isEnabled": "boolean",
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "availabilityMode": "string (enum TableServiceAvailabilityMode: Tables, Seats)",
  "tableGroups": [
    {
      "tableQuantity": "number (intero) | null",
      "seatPerTableQuantity": "number (intero) | null"
    }
  ],
  "totalSeatQuantity": "number (intero) | null",
  "averageMealDuration": "string (enum TableServiceMealDuration) | null",
  "maxBookingPerTimeSlotQuantity": "number (intero) | null",
  "bookingPayment": "string (enum TableServiceBookingPayment: None, Deposit)",
  "depositMode": "string (enum TableServiceDepositMode: Total, PerPerson)",
  "depositPercentage": "number (decimale, %) | null",
  "isOnlineBookingEnabled": "boolean"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idTableService` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) \| null |
| `availabilities[].dateTo` | string (ISO 8601, UTC) \| null |
| `availabilities[].isSameTimeForAllDays` | boolean |
| `availabilities[].startTime` | string (HH:mm) \| null |
| `availabilities[].endTime` | string (HH:mm) \| null |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) |
| `availabilities[].days[].isEnabled` | boolean |
| `availabilities[].days[].startTime` | string (HH:mm) \| null |
| `availabilities[].days[].endTime` | string (HH:mm) \| null |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) |
| `tableGroups[].tableQuantity` | number (intero) \| null |
| `tableGroups[].seatPerTableQuantity` | number (intero) \| null |
| `totalSeatQuantity` | number (intero) \| null |
| `averageMealDuration` | string (enum TableServiceMealDuration) \| null |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) |
| `depositPercentage` | number (decimale, %) \| null |
| `isOnlineBookingEnabled` | boolean |

### POST `/api/catalog/update-table-service-general`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idTableService": "string (UUID)",
  "translations": [
    {
      "language": "string",
      "serviceName": "string | null",
      "description": "string | null"
    }
  ],
  "isHighlighted": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idTableService` | string (UUID) |
| `translations[]` | ServiceTranslation[] (array di oggetti, una per lingua) |
| `translations[].language` | string |
| `translations[].serviceName` | string \| null |
| `translations[].description` | string \| null |
| `isHighlighted` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/update-table-service-availability-bookings`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idTableService": "string (UUID)",
  "availabilities": [
    {
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "isSameTimeForAllDays": "boolean",
      "startTime": "string (HH:mm) | null",
      "endTime": "string (HH:mm) | null",
      "days": [
        {
          "dayOfWeek": "number (intero ISO 8601: 1 = lunedì … 7 = domenica)",
          "isEnabled": "boolean",
          "startTime": "string (HH:mm) | null",
          "endTime": "string (HH:mm) | null"
        }
      ]
    }
  ],
  "availabilityMode": "string (enum TableServiceAvailabilityMode: Tables, Seats)",
  "tableGroups": [
    {
      "tableQuantity": "number (intero) | null",
      "seatPerTableQuantity": "number (intero) | null"
    }
  ],
  "totalSeatQuantity": "number (intero) | null",
  "averageMealDuration": "string (enum TableServiceMealDuration) | null",
  "maxBookingPerTimeSlotQuantity": "number (intero) | null",
  "bookingPayment": "string (enum TableServiceBookingPayment: None, Deposit)",
  "depositMode": "string (enum TableServiceDepositMode: Total, PerPerson)",
  "depositPercentage": "number (decimale, %) | null",
  "isOnlineBookingEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idTableService` | string (UUID) |
| `availabilities[]` | TableServiceAvailability[] (array di oggetti) |
| `availabilities[].dateFrom` | string (ISO 8601, UTC) \| null |
| `availabilities[].dateTo` | string (ISO 8601, UTC) \| null |
| `availabilities[].isSameTimeForAllDays` | boolean |
| `availabilities[].startTime` | string (HH:mm) \| null |
| `availabilities[].endTime` | string (HH:mm) \| null |
| `availabilities[].days[]` | TableServiceAvailabilityDay[] (array di 7 oggetti) |
| `availabilities[].days[].dayOfWeek` | number (intero ISO 8601: 1 = lunedì … 7 = domenica) |
| `availabilities[].days[].isEnabled` | boolean |
| `availabilities[].days[].startTime` | string (HH:mm) \| null |
| `availabilities[].days[].endTime` | string (HH:mm) \| null |
| `availabilityMode` | string (enum TableServiceAvailabilityMode: Tables, Seats) |
| `tableGroups[]` | TableServiceTableGroup[] (array di oggetti) |
| `tableGroups[].tableQuantity` | number (intero) \| null |
| `tableGroups[].seatPerTableQuantity` | number (intero) \| null |
| `totalSeatQuantity` | number (intero) \| null |
| `averageMealDuration` | string (enum TableServiceMealDuration) \| null |
| `maxBookingPerTimeSlotQuantity` | number (intero) \| null |
| `bookingPayment` | string (enum TableServiceBookingPayment: None, Deposit) |
| `depositMode` | string (enum TableServiceDepositMode: Total, PerPerson) |
| `depositPercentage` | number (decimale, %) \| null |
| `isOnlineBookingEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

## Sharing

### GET `/api/sharing/general-information?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "isSharingEnabled": "boolean",
  "isMinimumBookingDayEnabled": "boolean",
  "minimumBookingDayQuantity": "number (intero) | null",
  "isOnlySeasonalEnabled": "boolean",
  "isEnabledOnAllArticles": "boolean",
  "isScheduledAbsenceSharing": "boolean",
  "isManualCreditAssignmentEnabled": "boolean",
  "resoldDayCreditQuantity": "number (intero) | null",
  "manualCreditValue": "number (decimale, €) | null",
  "isAutomaticVoucherEnabled": "boolean",
  "voucherCreditValue": "number (decimale, €) | null",
  "isPrepaidCardTopUpEnabled": "boolean",
  "prepaidCardCreditValue": "number (decimale, €) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `isSharingEnabled` | boolean |
| `isMinimumBookingDayEnabled` | boolean |
| `minimumBookingDayQuantity` | number (intero) \| null |
| `isOnlySeasonalEnabled` | boolean |
| `isEnabledOnAllArticles` | boolean |
| `isScheduledAbsenceSharing` | boolean |
| `isManualCreditAssignmentEnabled` | boolean |
| `resoldDayCreditQuantity` | number (intero) \| null |
| `manualCreditValue` | number (decimale, €) \| null |
| `isAutomaticVoucherEnabled` | boolean |
| `voucherCreditValue` | number (decimale, €) \| null |
| `isPrepaidCardTopUpEnabled` | boolean |
| `prepaidCardCreditValue` | number (decimale, €) \| null |

### POST `/api/sharing/save-information`

**Payload JSON**

```json
{
  "isSharingEnabled": "boolean",
  "isMinimumBookingDayEnabled": "boolean",
  "minimumBookingDayQuantity": "number (intero) | null",
  "isOnlySeasonalEnabled": "boolean",
  "isEnabledOnAllArticles": "boolean",
  "isScheduledAbsenceSharing": "boolean",
  "isManualCreditAssignmentEnabled": "boolean",
  "resoldDayCreditQuantity": "number (intero) | null",
  "manualCreditValue": "number (decimale, €) | null",
  "isAutomaticVoucherEnabled": "boolean",
  "voucherCreditValue": "number (decimale, €) | null",
  "isPrepaidCardTopUpEnabled": "boolean",
  "prepaidCardCreditValue": "number (decimale, €) | null",
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `isSharingEnabled` | boolean |
| `isMinimumBookingDayEnabled` | boolean |
| `minimumBookingDayQuantity` | number (intero) \| null |
| `isOnlySeasonalEnabled` | boolean |
| `isEnabledOnAllArticles` | boolean |
| `isScheduledAbsenceSharing` | boolean |
| `isManualCreditAssignmentEnabled` | boolean |
| `resoldDayCreditQuantity` | number (intero) \| null |
| `manualCreditValue` | number (decimale, €) \| null |
| `isAutomaticVoucherEnabled` | boolean |
| `voucherCreditValue` | number (decimale, €) \| null |
| `isPrepaidCardTopUpEnabled` | boolean |
| `prepaidCardCreditValue` | number (decimale, €) \| null |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

## Prenotazioni online

### GET `/api/system/get-stripe-information?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
"string | null"
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `(corpo)` | string \| null |

### GET `/api/booking/get-online-services-options?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "isServicesOnlineBookingEnabled": "boolean",
  "supplementType": "string (enum SupplementType)",
  "supplementValue": "number (decimale) | null",
  "isBookingConfirmationRequired": "boolean",
  "isPayNowAccepted": "boolean",
  "isPayAtPropertyAccepted": "boolean",
  "isPayAtPropertyWithoutCardAccepted": "boolean",
  "isDepositAccepted": "boolean",
  "isPrepaidCardAccepted": "boolean",
  "bookingPanelTitle": "string (HTML) | null",
  "usefulInformation": "string (HTML) | null"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `isServicesOnlineBookingEnabled` | boolean |
| `supplementType` | string (enum SupplementType) |
| `supplementValue` | number (decimale) \| null |
| `isBookingConfirmationRequired` | boolean |
| `isPayNowAccepted` | boolean |
| `isPayAtPropertyAccepted` | boolean |
| `isPayAtPropertyWithoutCardAccepted` | boolean |
| `isDepositAccepted` | boolean |
| `isPrepaidCardAccepted` | boolean |
| `bookingPanelTitle` | string (HTML) \| null |
| `usefulInformation` | string (HTML) \| null |

### POST `/api/booking/update-online-services-options`

**Payload JSON**

```json
{
  "isServicesOnlineBookingEnabled": "boolean",
  "supplementType": "string (enum SupplementType)",
  "supplementValue": "number (decimale) | null",
  "isBookingConfirmationRequired": "boolean",
  "isPayNowAccepted": "boolean",
  "isPayAtPropertyAccepted": "boolean",
  "isPayAtPropertyWithoutCardAccepted": "boolean",
  "isDepositAccepted": "boolean",
  "isPrepaidCardAccepted": "boolean",
  "bookingPanelTitle": "string (HTML) | null",
  "usefulInformation": "string (HTML) | null",
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `isServicesOnlineBookingEnabled` | boolean |
| `supplementType` | string (enum SupplementType) |
| `supplementValue` | number (decimale) \| null |
| `isBookingConfirmationRequired` | boolean |
| `isPayNowAccepted` | boolean |
| `isPayAtPropertyAccepted` | boolean |
| `isPayAtPropertyWithoutCardAccepted` | boolean |
| `isDepositAccepted` | boolean |
| `isPrepaidCardAccepted` | boolean |
| `bookingPanelTitle` | string (HTML) \| null |
| `usefulInformation` | string (HTML) \| null |
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/catalog/sector-combobox`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "id": "string (UUID)",
    "value": "string"
  }
]
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (UUID) |
| `[].value` | string |

## Fiscalizzazione

### POST `/api/taxation/printer-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idPrinter": "string (UUID)",
      "printerName": "string",
      "printerIP": "string",
      "isEnabled": "boolean"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | PrinterItemListResponse[] (array di oggetti) |
| `rows[].idPrinter` | string (UUID) |
| `rows[].printerName` | string |
| `rows[].printerIP` | string |
| `rows[].isEnabled` | boolean |

### POST `/api/taxation/set-is-active`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idPrinter": "string (UUID)",
  "isEnabled": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idPrinter` | string (UUID) |
| `isEnabled` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/taxation/add-printer`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "printerName": "string",
  "printerIP": "string | null",
  "printerPort": "number (intero) | null",
  "printerModel": "string | null",
  "isSsl": "boolean",
  "isFiscalMode": "boolean",
  "isNonFiscalMode": "boolean",
  "isOrderMode": "boolean",
  "isTextMode": "boolean",
  "departmentVatRates": "string[] (5 elementi)",
  "firstHeader": "string | null",
  "secondHeader": "string | null",
  "footer": "string | null"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `printerName` | string |
| `printerIP` | string \| null |
| `printerPort` | number (intero) \| null |
| `printerModel` | string \| null |
| `isSsl` | boolean |
| `isFiscalMode` | boolean |
| `isNonFiscalMode` | boolean |
| `isOrderMode` | boolean |
| `isTextMode` | boolean |
| `departmentVatRates` | string[] (5 elementi) |
| `firstHeader` | string \| null |
| `secondHeader` | string \| null |
| `footer` | string \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/taxation/delete-printer`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idPrinter": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idPrinter` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### GET `/api/taxation/electronic-receipt?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
"string (HTML)"
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `(corpo)` | string (HTML) |

### POST `/api/taxation/electronic-receipt-activation`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "isElectronicReceiptAccepted": "boolean"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `isElectronicReceiptAccepted` | boolean |

**Response tabella**

Nessun corpo (204).

### POST `/api/taxation/download-guide-electronic-receipt`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

File (binario), non JSON.

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

File (binario), non JSON.

### POST `/api/taxation/save-electronic-receipt-account`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "companyName": "string | null",
  "companyAddress": "string | null",
  "streetNumber": "string | null",
  "postalCode": "string | null",
  "city": "string | null",
  "country": "string | null",
  "taxCode": "string | null",
  "revenueAgencyCode": "string | null",
  "vatNumber": "string | null",
  "pin": "string | null",
  "password": "string | null"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `companyName` | string \| null |
| `companyAddress` | string \| null |
| `streetNumber` | string \| null |
| `postalCode` | string \| null |
| `city` | string \| null |
| `country` | string \| null |
| `taxCode` | string \| null |
| `revenueAgencyCode` | string \| null |
| `vatNumber` | string \| null |
| `pin` | string \| null |
| `password` | string \| null |

**Response tabella**

Nessun corpo (204).

## Configurazione email

### GET `/api/system/email-configuration?idProperty={idProperty}`

**Payload JSON** (query string)

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "senderMailAddress": "string",
  "senderName": "string",
  "smtpServerAddress": "string",
  "smtpPort": "number (intero)",
  "smtpUsername": "string",
  "hasSmtpPassword": "boolean",
  "smtpSecurity": "string (enum SmtpSecurity)"
}
```

**Payload tabella** (query string)

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `senderMailAddress` | string |
| `senderName` | string |
| `smtpServerAddress` | string |
| `smtpPort` | number (intero) |
| `smtpUsername` | string |
| `hasSmtpPassword` | boolean |
| `smtpSecurity` | string (enum SmtpSecurity) |

### POST `/api/system/send-test-email`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "senderMailAddress": "string",
  "senderName": "string",
  "smtpServerAddress": "string",
  "smtpPort": "number (intero 1–65535)",
  "smtpUsername": "string",
  "smtpPassword": "string | null",
  "smtpSecurity": "string (enum SmtpSecurity) | null"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `senderMailAddress` | string |
| `senderName` | string |
| `smtpServerAddress` | string |
| `smtpPort` | number (intero 1–65535) |
| `smtpUsername` | string |
| `smtpPassword` | string \| null |
| `smtpSecurity` | string (enum SmtpSecurity) \| null |

**Response tabella**

Nessun corpo (204).

### POST `/api/system/save-email-configuration`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "senderMailAddress": "string",
  "senderName": "string",
  "smtpServerAddress": "string",
  "smtpPort": "number (intero 1–65535)",
  "smtpUsername": "string",
  "smtpPassword": "string | null",
  "smtpSecurity": "string (enum SmtpSecurity) | null"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `senderMailAddress` | string |
| `senderName` | string |
| `smtpServerAddress` | string |
| `smtpPort` | number (intero 1–65535) |
| `smtpUsername` | string |
| `smtpPassword` | string \| null |
| `smtpSecurity` | string (enum SmtpSecurity) \| null |

**Response tabella**

Nessun corpo (204).

## Database

### POST `/api/database/existing-booking-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idBooking": "string (UUID)",
      "customerName": "string | null",
      "reservationKind": "string (enum ReservationKind) | null",
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "agreementName": "string | null",
      "reservationStatus": "string (enum ReservationStatus) | null",
      "isTrashed": "boolean"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | ExistingBookingItem[] (array di oggetti) |
| `rows[].idBooking` | string (UUID) |
| `rows[].customerName` | string \| null |
| `rows[].reservationKind` | string (enum ReservationKind) \| null |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null |
| `rows[].agreementName` | string \| null |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null |
| `rows[].isTrashed` | boolean |

### POST `/api/database/affiliated-hotel-combobox`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
[
  {
    "id": "string (UUID)",
    "value": "string"
  }
]
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `[].id` | string (UUID) |
| `[].value` | string |

### POST `/api/database/move-to-trash`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBookings": "string[] (UUID, almeno uno)",
  "password": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBookings` | string[] (UUID, almeno uno) |
| `password` | string |

**Response tabella**

Nessun corpo (204).

### POST `/api/database/trash-item-list`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "page": "number (intero, da 1)",
  "pageSize": "number (intero)",
  "search": "string",
  "filters": [
    {
      "field": "string",
      "value": "string[] (array di stringhe, almeno una)"
    }
  ],
  "sortField": "string",
  "sortDirection": "string (enum SortDirection)"
}
```

**Response JSON**

```json
{
  "total": "number (intero)",
  "rows": [
    {
      "idBooking": "string (UUID)",
      "customerName": "string | null",
      "reservationKind": "string (enum ReservationKind) | null",
      "dateFrom": "string (ISO 8601, UTC) | null",
      "dateTo": "string (ISO 8601, UTC) | null",
      "agreementName": "string | null",
      "reservationStatus": "string (enum ReservationStatus) | null",
      "isTrashed": "boolean"
    }
  ]
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `page` | number (intero, da 1) |
| `pageSize` | number (intero) |
| `search` | string |
| `filters[]` | PageFilter[] (array di oggetti) |
| `filters[].field` | string |
| `filters[].value` | string[] (array di stringhe, almeno una) |
| `sortField` | string |
| `sortDirection` | string (enum SortDirection) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `total` | number (intero) |
| `rows[]` | ExistingBookingItem[] (array di oggetti) |
| `rows[].idBooking` | string (UUID) |
| `rows[].customerName` | string \| null |
| `rows[].reservationKind` | string (enum ReservationKind) \| null |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null |
| `rows[].agreementName` | string \| null |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null |
| `rows[].isTrashed` | boolean |

### POST `/api/database/empty-trash`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

Nessun corpo (204).

### POST `/api/database/restore-booking-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBookings": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBookings` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

### POST `/api/database/delete-selected-item`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "idBookings": "string[] (UUID, almeno uno)"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `idBookings` | string[] (UUID, almeno uno) |

**Response tabella**

Nessun corpo (204).

## Assistenza e guide

### POST `/api/system/info-support`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)"
}
```

**Response JSON**

```json
{
  "phoneNumber": "string",
  "mailAddress": "string",
  "supportHour": "string[]"
}
```

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |

**Response tabella**

| Campo | Tipo |
|---|---|
| `phoneNumber` | string |
| `mailAddress` | string |
| `supportHour` | string[] |

### POST `/api/system/contact-support`

**Payload JSON**

```json
{
  "idProperty": "string (UUID)",
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "mobilePhone": "string | null",
  "message": "string"
}
```

**Response JSON**

Nessun corpo (204).

**Payload tabella**

| Campo | Tipo |
|---|---|
| `idProperty` | string (UUID) |
| `firstName` | string |
| `lastName` | string |
| `email` | string |
| `mobilePhone` | string \| null |
| `message` | string |

**Response tabella**

Nessun corpo (204).
