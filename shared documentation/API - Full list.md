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

### POST `/api/warehouse/combobox-list`

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

### POST `/api/database/get-affiliated-hotel-combobox`

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
