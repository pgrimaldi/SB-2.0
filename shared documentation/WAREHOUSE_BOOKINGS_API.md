# API Warehouse e Bookings

## Warehouse

### POST `/api/warehouse/list`

**Usata da:** Pagine Magazzino (`views/warehouse`) e Impostazioni magazzino (`views/settings/warehouse-setting`): tabella

**Rotta:** `POST /api/warehouse/list`

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "lettino",
  "filters": [],
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
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string | `2026-07-01T00:00:00.000Z` |
| `sortField` | string | `name` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

`200 OK`

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
      "thresholdQuantity": null,
      "isThresholdWarningActive": false
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | WarehouseItem[] (array di oggetti) | — |
| `rows[].idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `rows[].name` | string | `Lettino` |
| `rows[].totalQuantity` | number (intero) | `120` |
| `rows[].availableQuantity` | number (intero) | `84` |
| `rows[].thresholdQuantity` | number (intero) \| null | `10` |
| `rows[].isThresholdWarningActive` | boolean | `true` |

### POST `/api/warehouse/combobox-list`

**Usata da:** Popup "Aggiungi articolo" (`FormWarehouseAddItemPopup`, Impostazioni magazzino): tendina degli articoli

**Rotta:** `POST /api/warehouse/combobox-list`

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

`200 OK`

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

**Usata da:** Popup "Aggiungi articolo" (`FormWarehouseAddItemPopup`, Impostazioni magazzino): bottone Aggiungi

**Rotta:** `POST /api/warehouse/add-warehouse-item`

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "idArticle": "8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c",
  "name": null,
  "totalQuantity": 10,
  "availableQuantity": null,
  "thresholdQuantity": 5,
  "isThresholdWarningActive": true
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `idArticle` | string (UUID) | `8c2e4a1b-6f3d-4b7e-a9c5-1d2e3f4a5b6c` |
| `name` | string \| null | `null` |
| `totalQuantity` | number (intero > 0) | `10` |
| `availableQuantity` | number (intero) \| null | `null` |
| `thresholdQuantity` | number (intero ≥ 0) \| null | `5` |
| `isThresholdWarningActive` | boolean | `true` |

#### Response

`204 No Content (nessun corpo)`

### POST `/api/warehouse/edit-warehouse-item`

**Usata da:** Popup "Modifica articolo" (`FormWarehouseEditItemPopup`, Impostazioni magazzino): bottone Salva

**Rotta:** `POST /api/warehouse/edit-warehouse-item`

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

`204 No Content (nessun corpo)`

### POST `/api/warehouse/delete-warehouse-item`

**Usata da:** Popup di conferma `DeleteWarehouseItemsPopup` (Impostazioni magazzino): elimina riga / elimina selezionati

**Rotta:** `POST /api/warehouse/delete-warehouse-item`

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

`204 No Content (nessun corpo)`

### POST `/api/warehouse/duplicate-warehouse-item`

**Usata da:** Popup di conferma `DuplicateWarehouseItemsPopup` (Impostazioni magazzino): duplica riga / duplica selezionati

**Rotta:** `POST /api/warehouse/duplicate-warehouse-item`

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

`204 No Content (nessun corpo)`

## Bookings

### POST `/api/database/existing-booking-list`

**Usata da:** Pagina Database (`views/settings/database`, `/settings/database`): tabella

**Rotta:** `POST /api/database/existing-booking-list`

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
      "value": "2026-07-01T00:00:00.000Z"
    },
    {
      "field": "datetimeTo",
      "value": "2026-08-31T23:59:59.999Z"
    },
    {
      "field": "paymentStatus",
      "value": "FullyPaid"
    },
    {
      "field": "paymentStatus",
      "value": "PartiallyPaid"
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
| `filters[]` | PageFilter[] (array di oggetti) | — |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string | `2026-07-01T00:00:00.000Z` |
| `sortField` | string | `dateFrom` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

`200 OK`

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
      "agreementName": null,
      "reservationStatus": "Pending",
      "isTrashed": false
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `2` |
| `rows[]` | ExistingBookingItem[] (array di oggetti) | — |
| `rows[].idBooking` | string (UUID) | `3d1e5f7a-9b2c-4d6e-8f1a-2b3c4d5e6f70` |
| `rows[].customerName` | string \| null | `Mario Rossi` |
| `rows[].reservationKind` | string (enum ReservationKind) \| null | `Spot` |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-07-05T00:00:00.000Z` |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null | `2026-07-12T00:00:00.000Z` |
| `rows[].agreementName` | string \| null | `Hotel Esempio` |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null | `Confirmed` |
| `rows[].isTrashed` | boolean | `false` |

### POST `/api/database/get-affiliated-hotel-combobox`

**Usata da:** Popup Filtri (`ExistingBookingFiltersPopup`, pagina Database): tendina Strutture convenzionate

**Rotta:** `POST /api/database/get-affiliated-hotel-combobox`

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

`200 OK`

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

**Usata da:** Popup password `ConfirmDeleteBookingPopup` (pagina Database): bottone Cancella dati

**Rotta:** `POST /api/database/move-to-trash`

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

`204 No Content (nessun corpo)`

### POST `/api/database/trash-item-list`

**Usata da:** Pagina Cestino (`views/settings/database`, `/settings/database/trash`): tabella

**Rotta:** `POST /api/database/trash-item-list`

#### Payload

```json
{
  "idProperty": "5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d",
  "page": 1,
  "pageSize": 10,
  "search": "",
  "filters": [],
  "sortField": "",
  "sortDirection": "Ascending"
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `idProperty` | string (UUID) | `5b1f3c7e-2d4a-4e8b-9c1d-7a6e5f4b3c2d` |
| `page` | number (intero, da 1) | `1` |
| `pageSize` | number (intero) | `10` |
| `search` | string | `` |
| `filters[]` | PageFilter[] (array di oggetti) | `[]` |
| `filters[].field` | string | `datetimeFrom` |
| `filters[].value` | string | `2026-07-01T00:00:00.000Z` |
| `sortField` | string | `` |
| `sortDirection` | string (enum SortDirection) | `Ascending` |

#### Response

`200 OK`

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
      "agreementName": null,
      "reservationStatus": "Cancelled",
      "isTrashed": true
    }
  ]
}
```

| Property | Tipo | Esempio |
|---|---|---|
| `total` | number (intero) | `1` |
| `rows[]` | ExistingBookingItem[] (array di oggetti) | — |
| `rows[].idBooking` | string (UUID) | `9c8b7a6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d` |
| `rows[].customerName` | string \| null | `Roberto Lilla` |
| `rows[].reservationKind` | string (enum ReservationKind) \| null | `Spot` |
| `rows[].dateFrom` | string (ISO 8601, UTC) \| null | `2026-06-05T00:00:00.000Z` |
| `rows[].dateTo` | string (ISO 8601, UTC) \| null | `2026-06-07T00:00:00.000Z` |
| `rows[].agreementName` | string \| null | `null` |
| `rows[].reservationStatus` | string (enum ReservationStatus) \| null | `Cancelled` |
| `rows[].isTrashed` | boolean | `true` |

### POST `/api/database/empty-trash`

**Usata da:** Pagina Cestino: bottone Svuota cestino (dopo il popup di conferma)

**Rotta:** `POST /api/database/empty-trash`

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

`204 No Content (nessun corpo)`

### POST `/api/database/restore-booking-item`

**Usata da:** Pagina Cestino: bottone Ripristina

**Rotta:** `POST /api/database/restore-booking-item`

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

`204 No Content (nessun corpo)`

### POST `/api/database/delete-selected-item`

**Usata da:** Pagina Cestino: bottone Elimina definitivamente (dopo il popup di conferma)

**Rotta:** `POST /api/database/delete-selected-item`

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

`204 No Content (nessun corpo)`
