# Gestione dati

> Documento di indirizzo su come modellare specifici tipi di dato nel nuovo backend, a partire da casi
> concreti individuati durante l'analisi di `back-pre`. Complemento a
> [01-criticita-e-indicazioni-backend.md](01-criticita-e-indicazioni-backend.md) (criticità individuate) e
> [pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md) (regole di modulo/schema
> a cui le soluzioni qui descritte si allineano).
>
> **Documento vivo**: verranno aggiunte altre sezioni via via che emergono altri casi da normalizzare.

---

## 1. Link/URL salvati a DB (immagini, PDF, documenti)

### 1.1 Problema riscontrato

Nello schema attuale i campi che puntano a un file (es. `bathouses.photos`, `bathouses.mainPhoto`,
`bathouses.priceListPDF`, `bathouses.menuPDF`, `configure_images.image`, ...) salvano **l'URL completo**,
ad esempio:

```
["https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/3br3w805slif0a1m8.jpeg",
 "https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/3br3w805slif0a1mp.jpeg", ...]
```

Criticità concrete individuate:

1. **Ripetizione**: la parte iniziale dell'URL (dominio/bucket) è identica su ogni riga e viene duplicata
   migliaia di volte.
2. **Il prefisso non è stabile nel tempo, nemmeno per lo stesso campo**: immagini caricate in periodi diversi
   possono puntare a bucket/region diversi (es. alcune su `s3.us-east-2`, altre storicamente su
   `s3.us-east-1`) — non è un singolo valore "attuale", è un dato che varia riga per riga.
3. **Segmenti di percorso intermedi specifici** (es. `report/`, `img/logo/`) fanno parte del percorso, non
   del prefisso.
4. **Non è un solo campo**: lo stesso pattern si ripete su più campi e più tabelle.
5. **Non sempre è una risorsa nostra**: alcuni campi possono contenere URL completamente esterni (link a
   risorse di terzi), non componibili con un prefisso interno.
6. **Prefisso già hardcoded nel codice esistente**: `confimages.controller.js` (righe 91-92) sostituisce a
   mano l'URL S3 con quello CloudFront (`s3Url` / `cloudFrontUrl` cablati nel controller) — un cambio di
   bucket/distribuzione richiederebbe una modifica di codice. Già segnalato in
   [`analisi-pre/db/configure_images.md`](../analisi-pre/db/configure_images.md).

### 1.2 Soluzione proposta

**Non salvare mai l'URL completo.** Ogni riferimento a un file si scompone in:

- **`origin`** — codice breve che identifica *da dove viene* il file (es. `S3_US_EAST_2`, `S3_US_EAST_1`,
  `CLOUDFRONT`, `EXTERNAL`). Vive **per riga**, perché può differire da riga a riga (punto 2 sopra).
- **`path`** — il resto del percorso (incluse eventuali sottocartelle come `report/`, `img/logo/`, punto 3
  sopra). Se `origin = EXTERNAL`, `path` contiene l'URL completo così com'è, invariato.

La **mappa `origin` → URL base** vive in configurazione (`appsettings` / SSM Parameter Store), non a
database, e contiene **tutti** i prefissi conosciuti (attuali e storici), non solo quello corrente:

```
Storage:Origins:S3_US_EAST_2 = https://summerbooking.s3.us-east-2.amazonaws.com
Storage:Origins:S3_US_EAST_1 = https://summerbooking.s3.us-east-1.amazonaws.com
Storage:Origins:CLOUDFRONT   = https://d26kxhigi9o4u2.cloudfront.net
Storage:Origins:EXTERNAL     =
```

```csharp
string BuildUrl(string origin, string path) =>
    origin == "EXTERNAL" ? path : $"{origins[origin]}/{path}";
```

Un cambio di bucket/CDN diventa quindi una modifica di configurazione, mai una migrazione dati.

### 1.3 Dove vivono i dati: una tabella di supporto per modulo, non per campo

Per evitare di ripetere le due colonne (`origin`, `path`) su ogni singolo campo-file di ogni tabella (punto
4 sopra), ogni **modulo** che possiede file introduce **una propria** tabella di supporto nel proprio
schema (coerente con la regola "un modulo possiede i suoi dati", [§5 di
linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)). Nome e forma canonica
di questa tabella (`<dominio>_media_assets`) sono fissati in **§3.1quater.A**.

```
catalog_media_assets            -- una per modulo che ne ha bisogno (qui: Catalog/Configuration)
- id            PK
- origin        es. 'S3_US_EAST_2' | 'EXTERNAL' | ...
- path          percorso relativo, o URL intero se origin = 'EXTERNAL'
- kind          'photo' | 'pdf' | 'icon' (per C2; ogni modulo definisce i propri)
- created_at
```

I campi che oggi sono varchar/JSON con l'URL diventano una FK verso questa tabella, **all'interno dello
stesso schema di modulo** (nessun JOIN cross-modulo):

```
bathouses
- main_photo_id       FK -> catalog_media_assets.id
- price_list_pdf_id   FK -> catalog_media_assets.id
- menu_pdf_id         FK -> catalog_media_assets.id

bathouse_photos                 -- sostituisce l'array JSON "photos"
- id
- bathouse_id     FK -> bathouses.id
- media_asset_id  FK -> catalog_media_assets.id
- sort_order

configure_images
- image_id        FK -> catalog_media_assets.id   -- al posto della varchar "image"
```

La tabella di giunzione `bathouse_photos` (con `sort_order`) sostituisce l'array JSON `photos` e risolve
anche il bug già documentato in [`bathouses.md`](../analisi-pre/db/bathouses.md) sulla gestione
dell'array (`if (photos.length && photos.length === 0)`, condizione sempre falsa): aggiungere/rimuovere una
foto diventa un `INSERT`/`DELETE` normale invece di riscrivere l'intero blob.

### 1.4 Perché non una tabella unica condivisa da tutti i moduli

Una singola tabella `media_assets` globale referenziata via FK da tabelle di schemi diversi (`catalog`,
`fiskaly`, `notification`, ...) violerebbe la regola "zero FK/JOIN cross-modulo" e ricreerebbe
l'accoppiamento che la riscrittura vuole eliminare. La tabella di supporto resta quindi **locale al modulo
proprietario dei dati** che referenzia — non "una tabella in più per ogni dominio" nel senso di
complessità aggiunta, ma la stessa piccola tabella di riferimento che qualunque modulo con file avrebbe
comunque bisogno di avere.

### 1.5 Migrazione dei dati esistenti

Script una tantum in fase di migrazione MySQL → PostgreSQL: per ogni URL esistente, riconoscere l'host
contro gli host noti (`s3.us-east-2...`, `s3.us-east-1...`, dominio CloudFront) e scomporlo in
`origin` + `path`; se l'host non corrisponde a nessuna origine nota, salvare con `origin = EXTERNAL` e
l'URL intero in `path`.

---

## 2. Enumerazioni e vocabolari chiusi

### 2.1 Problema riscontrato

In `back-pre` quasi ogni campo che rappresenta uno stato/tipo/categoria è un `varchar` libero, senza
`ENUM`/`CHECK` a livello DB e senza validazione server-side contro un insieme chiuso di valori. L'analisi
tabella per tabella (`analisi-pre/db/*.md`) ha trovato lo stesso problema decine di volte, con conseguenze
concrete già osservate sui dati reali:

1. **Drift di valori equivalenti scritti in modi diversi**, perché nulla impedisce una variante nuova ad ogni
   punto di scrittura:
   - `properties.onlineBookingOptions.locationOptionObj.surchargeType` / `serviceBookingOptions.surchargeType`:
     `"Disable"` e `"disable"` coesistono nello stesso campo.
   - `price_lists.priceCalculation`: 7 valori osservati per quello che concettualmente sono 3-4 concetti
     (`"proportional"` / `"proportional price"`, `"Daily price"` / `"Daily price with weekend"`,
     `"daily"`, `"logarithmic"`).
   - `price_lists.monthlySetting` / `weekendSetting`: descritti nell'analisi iniziale come "costanti" sulla
     base di un campione locale ridotto, poi smentiti da un campione più ampio (5 e 4 valori reali
     rispettivamente) — la variazione esiste, semplicemente non era stata prevista né vincolata a schema.
2. **Refusi che diventano permanenti**: `service_type_filters.serviceTypes` contiene `"Restuarant"` invece
   di "Restaurant" — corretto in DB dovrebbe restare sincronizzato con ogni punto del frontend che lo
   confronta per stringa esatta, altrimenti l'intera sezione sparisce silenziosamente dalla UI.
3. **Righe orfane senza che nessun vincolo lo impedisca**: `services.serviceFilterType = 0` non corrisponde
   a nessun `service_type_filters.id` (che parte da 1) — un `INNER JOIN` applicativo la esclude
   silenziosamente, senza errore.
4. **Stesso vocabolario concettuale reimplementato più volte, in modo indipendente**: lo stato
   "attivo/non attivo" torna, con tipi e convenzioni diverse, su `services.status` (`varchar(10)`,
   `"active"`), `configure_images.status` (`tinyint(1)`, commento DB `0-inactive|1-active`),
   `configure_categories.status` (`tinyint(1)`, mai nemmeno filtrato dal codice), `users.userStatus`
   (`varchar`, `"active"`/`"inactive"`) — quattro implementazioni scollegate dello stesso concetto, nessuna
   fonte di verità comune.
5. **Alcune tabelle sono già, di fatto, un vocabolario chiuso mai collegato con una vera FK**: `licenses`
   (`type`: `basic`/`advanced`) esiste già come lookup, ma `users.license` la duplica come stringa libera
   invece di referenziarla — stesso problema per `service_type_filters` rispetto a
   `services.serviceFilterType`.

### 2.2 Soluzione proposta: due strumenti, non uno

Non esiste un'unica soluzione per "un campo con pochi valori possibili": la scelta corretta dipende da **chi
deve poter far crescere il vocabolario** e da **quante proprietà porta con sé oltre al valore stesso**.

#### A. `enum` C# nel modulo proprietario (default)

Per un vocabolario **chiuso per definizione di business/dominio**, dove aggiungere un valore richiede
comunque una modifica di codice (nuovo ramo di logica, nuovo comportamento, nuova integrazione) — non ha
senso renderlo gestibile da UI/DB, perché senza codice nuovo il valore non farebbe comunque nulla.

```csharp
// Modules/Catalog/Domain/Enums/DiscountType.cs
public enum DiscountType { Percentage, Fixed }

// Modules/Identity/Domain/Enums/LoginMethod.cs
public enum LoginMethod { Normal, Google, Apple, Facebook }
```

- Vive in `Domain/Enums/` del modulo che possiede il concetto (§2 di
  [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)) — mai in `SharedKernel`
  salvo il rarissimo caso di un vocabolario davvero trasversale a tutti i moduli.
- A DB: colonna `smallint`/`varchar` con conversione EF Core (`HasConversion<string>()` preferito a
  `<int>` per leggibilità in query dirette e stabilità se l'ordine dichiarato cambia), oppure `ENUM` nativo
  Postgres se il modulo lo adotta in modo coerente. **Mai come `CHECK` costruito a mano parallelo a un
  `varchar` libero**: quello è esattamente il pattern che ha già prodotto il drift descritto in §2.1.
- Se serve un'etichetta tradotta (mostrata all'utente), viene dalle risorse di localizzazione del modulo
  (`Resources/*.resx`/`.json`, §2 di linee-guida-implementative-dotnet.md), **non** da una colonna a DB —
  l'esigenza di traduzione da sola non giustifica una tabella.

#### B. Tabella di lookup nello schema del modulo (quando il vocabolario deve poter crescere senza deploy)

Per un vocabolario che un operatore/admin deve poter estendere **senza toccare codice** (es. aggiungere una
categoria filtro, un nuovo piano di abbonamento, una nuova categoria di icone), o che porta con sé metadati
propri oltre al codice (icona, ordinamento, flag attivo, più traduzioni) — sul modello di quanto già esiste
correttamente nel legacy per `licenses`, `configure_categories`, `service_type_filters`.

```
catalog_service_amenity_categories        -- schema del modulo Catalog
- id            PK
- code          es. 'popular', 'accessibility'   -- stabile, usato in codice/API
- sort_order
- is_active
- created_at / updated_at / deleted_at

catalog_service_amenity_category_translations   -- figlia: le etichette per lingua (NON label_it/label_en a colonne)
- id            PK
- category_id   FK -> catalog_service_amenity_categories.id
- culture       es. 'it-IT'
- label
```

Ogni colonna che oggi referenzia il vocabolario per stringa/numero libero diventa una **FK reale** verso
`id` (mai un confronto per stringa come l'attuale `configure_images.category` ↔ `configure_categories`).
Le **etichette tradotte** vivono in una tabella figlia `<padre>_translations` (**mai** `label_it`/
`label_en`/… a colonne del padre) — forma canonica in **§3.1quater.C**.

### 2.3 Criterio di scelta

| Domanda | Sì → | No → |
|---|---|---|
| Aggiungere un valore richiede comunque nuovo codice (nuova logica/integrazione)? | `enum` C# | tabella lookup |
| Un operatore deve poterlo estendere da pannello, senza deploy? | tabella lookup | `enum` C# |
| Serve più di un'etichetta tradotta (icona, ordine, flag attivo, ecc.)? | tabella lookup | `enum` C# |
| Il vocabolario è già usato/gestito da più di un modulo? | tabella lookup nel modulo proprietario, referenziata anche dagli altri solo per **id copiato in evento/proiezione**, mai per FK cross-modulo (regola §5 di linee-guida-implementative-dotnet.md) | `enum` locale al modulo |

Nel dubbio, si parte da `enum` (più semplice, a costo zero se il vocabolario resta davvero fisso) e si
promuove a tabella di lookup solo quando emerge un bisogno reale di gestione runtime — non il contrario.

### 2.4 Mappatura dei casi concreti individuati nell'analisi

| Campo legacy | Valori reali osservati | Enum o lookup? |
|---|---|---|
| `bathouses.pets` | `Allowed` / `Not Allowed` | `enum` (2 valori, logica di business fissa) |
| `bathouses.cancellation` | 5 varianti tipo "Free within N hours" / "Not Allowed" | non enum di stringhe: campo strutturato (`int? noticeHours`, `null` = non consentito) |
| `discounts.items.type` | `percentage` / `fixed` | `enum` |
| `discounts.items.voucherType` | `all` / `custom` / `withoutCustom` | `enum` |
| `price_lists.type` | `package` / `additional` | `enum` |
| `price_lists.priceCalculation` | 7 varianti da consolidare (vedi §2.1) | `enum`, dopo normalizzazione dati in migrazione |
| `configure_images.type` | `occupied` / `free` | `enum` |
| `users.loginMethod` | `normal` / `google` / `apple` / `facebook` | `enum` (ogni provider richiede comunque codice dedicato) |
| `users.userStatus` | `active` / `inactive` | `enum` |
| `users.subscriptionStatus` | `enable` / `disable` | `enum` |
| `users.license` | `basic` / `advanced` (duplica `licenses.type`) | **FK** verso la tabella lookup `licenses` esistente, non un enum parallelo |
| `licenses.type` | `basic` / `advanced` | resta tabella lookup: un piano nuovo è un dato gestibile da SuperAdmin, non necessariamente una nuova release |
| `services.status` | sempre `active` nei dati osservati | `enum` (`Active`/`Inactive`) |
| `services.serviceFilterType` | 7 categorie, in crescita nel tempo (una riga di test già aggiunta) | **FK** verso tabella lookup `service_type_filters` (con traduzione IT aggiunta, oggi assente) |
| `servicetypes_icons.type` | `normal` / `hourly` / `bar` | `enum` (identico a `services_types`, che è già strutturale) |
| `configure_categories` / `configure_images.category` | 5 categorie icone mappa spiaggia | tabella lookup (già lo è nel legacy; va solo collegata con una vera FK) |

### 2.4bis Campi del dominio Prenotazioni (C1)

Individuati durante l'analisi tabella per tabella delle prenotazioni (`analisi-pre/db/bookings.md`,
`service_reservation.md`, `booking_payments.md`, `booking_absence_details.md`, ecc.). Molti di questi
sono oggi `varchar`/`tinyint`/`int` liberi, spesso con **codici numerici memorizzati come stringa**
(`"0"`/`"1"`) e con **drift già osservato** su campioni di pochi record.

| Campo legacy | Valori reali osservati | Enum o lookup? |
|---|---|---|
| `bookings.booking_period` · `booking_locations[].booking_period` · `booking_absence_details.book_period_allowed` (nullable) · `sharing_details.book_period_allowed` · `service*.selectedData` | `FullDay` / `Morning` / `Afternoon` | `enum` — **esiste già** la costante `globalConstants.BookingPeriod`; portarla a `enum` C# `BookingPeriod` nel modulo Booking |
| `bookings.bookingByAdmin` · `service_reservation.bookingByAdmin` · `services_booking_counts.booking_type` | `"0"` (cliente/online) / `"1"` (gestionale) — `varchar` | `enum` `BookingChannel { Customer, Manager }` — un solo vocabolario per i 3 campi; oggi `booking_type` ha nome fuorviante |
| `bookings.status` | `0` (cancellata, con `isDeleted=1`) / `1` (attiva) / `2` (osservato) / `3` (guardia anti-doppia prenotazione, da `payment_status "2"`) | `enum` `BookingStatus` — **normalizzare** i significati con il team prodotto prima di mappare; separare lo stato dal soft-delete |
| `bookings.bookingType` | `0` listino standard / `1` listino hotel senza voucher / `2` voucher hotel (commento nel model) | `enum` `BookingListingType { Standard, HotelPriceList, HotelVoucher }` |
| `booking_items_counts_details.status` · `additional_items_counts_details.status` | `0` / `1` / `3` — copia del `payment_status` della prenotazione | **stesso** vocabolario di `bookings.status`/`payment_status`: consolidare, non un enum a parte |
| `booking_payments.type` · `service_reservation_payments.type` | `1` = Booking, `2` = Service (commento DB); ma `creditDetails` restituisce `1`/`0` | `enum` `PaymentSubjectType { Booking, ServiceReservation }` — unificare anche il valore esposto in API |
| `booking_payments.payment_mode` · `service_reservation_payments.payment_mode` · `bookings.booking_payments.paymentMode` (JSON) | `Cash` / `Credit card` / `Other` / `null` / `""` (drift) | `enum` `PaymentMode { Cash, CreditCard, Other }` — normalizzare `""`→`null` in migrazione |
| `service_reservation.status` | `1` (confermata) / `2` (in attesa pagamento) | `enum` `ServiceReservationStatus { Confirmed, PendingPayment, Cancelled }` — allineare a `BookingStatus` |
| `service_reservation.payNow` | `0` / `1` | booleano (`PayNow`) — non enum |
| `services_booking_counts.service_type` | `Bar Service` / `Normal Service` (gli orari non sono conteggiati qui) | `enum` `ServiceCategory { Bar, Normal, Hourly }` — **stesso** concetto di `services_types.type` / `servicetypes_icons.type` (`normal`/`hourly`/`bar`): un solo enum, attenzione al drift etichette (`"Bar Service"` vs `bar`) |
| `booking_absence_details.status` | `2` = giorno disponibile alla rivendita / `1` = giorno ripreso | `enum` `AbsenceSlotState { Available, Reassigned }` — numerazione oggi controintuitiva (2 = disponibile); ridondante con `isBooked` (0/1) da eliminare |
| `bookings_send_payment_summaries.payment_status` | `unpaid` / `paid` (dati reali) — il commento storico del model diceva `0`/`1`/`2` | `enum` `PaymentLinkStatus { Unpaid, Paid, Cancelled }` — drift documentale stringhe/numeri |
| `bookings_send_payment_summaries.method_flag` | `0` default / `1` email / `2` whatsapp (commento DB) | `enum` `SummaryDeliveryChannel { None, Email, WhatsApp }` |
| `online_transaction_details.payment_type` | `pay_now` / `advance_balance` / (`pay_at_property` nel codice) | `enum` `OnlinePaymentType { PayNow, AdvanceBalance, PayAtProperty }` |
| `online_transaction_details.status` | `succeeded` / `pending` / `Uncaptured` (casing incoerente) — passthrough Stripe | `enum` `OnlineTransactionStatus` interno, derivato dallo stato Stripe (che va conservato grezzo in un campo dedicato) |
| `online_transaction_details.booking_type` | `"1"` (o `null`) — semantica non documentata | da chiarire: probabile `enum` `{ Booking, ServiceReservation }` (allineare a `PaymentSubjectType`) |
| `card_transactions.transaction_type` | commento `1` credito / `2` debito, ma **nei dati sempre `1`** con importi negativi per i debiti | `enum` `CardMovementType { Credit, Debit }` — **richiede normalizzazione dati**: i debiti attuali sono `type=1` + `amount<0` |
| `transactions_payments.type` | `deposit` / `payment` — **già `ENUM` nativo MySQL** | `enum` `TransactionEntryType { Deposit, Payment }` — mappatura diretta |
| `parent_bookings.status` | `0` default / `1` linksent / `2` cancel / `3` success (commento) — **tabella dead** | irrilevante finché la feature non viene ripristinata |
| `booking_groups.status` · `services_booking_counts.status` · `hourly_service_booking_management.status` | `1` = attivo (soft-delete implicito) | `enum` `ActiveState { Active, Inactive }` — ennesima reimplementazione del concetto "attivo/non attivo" (vedi §2.1 punto 4) |
| `sharing_details.isBooked` · `isApplied` · `email_sent` · `is_sharing_coming_from_absence` | `0` / `1` (`int`/`tinyint`) | **non enum**: booleani (`is_booked`, `is_applied`, `is_coming_from_absence`); `email_sent` è un flag di canale/esito invio → **C6** (log notifiche), non un campo dell'aggregato |
| `sharing_details.credit_value` / `number_of_credit` | interi (`credit_value` parsato da stringa con `€` rimosso) | **non enum**: `number_of_credit` intero, `credit_value` → `numeric(12,2)` (§3.7) |
| `bookings.booking_element` (in `booking_items_counts_details` / `additional_items_counts_details`) · `services_booking_counts.service_name` · `hourly_service_booking_management.hourly_service_name` | nomi articolo/servizio come **stringa libera**, con nucleo fisso (`Umbrella`/`Sunbed`/`Cabin`/…) ma anche voci personalizzate e refusi (`"Lettino Standard\t"`, `"Restuarant"`) | **non enum**: FK verso l'anagrafica articoli/servizi del modulo Catalogo; il confronto attuale per stringa esatta con `warehouses` è la causa dei conteggi errati |
| `bookings.booking_discount.type` · `bookings.additionalAutoVoucher[].type` · `service_reservation.discountData.type` | `percentage` / `fixed` | `enum` `DiscountType` — **stesso** di `discounts.items.type` già mappato sopra |
| `hotel_payments.priceCalculation` | `proportional` / `daily` (snapshot) | **stesso** vocabolario di `price_lists.priceCalculation` / `hotel_price_lists.priceCalculation` (7 varianti da consolidare) — non un enum separato |
| `hotel_payments.paymentStatus` | `pending` / `completed` / `refunded` / `failed` (default `pending`; nei dati solo `completed`) | `enum` `HotelPaymentStatus { Pending, Completed, Refunded, Failed }` |
| `hotel_payments.paymentMethod` | `cash` / `card` / `bank transfer` / … (mai popolato nel campione) | `enum` `PaymentMode` — allineare a `booking_payments.payment_mode` |
| `hotel_payments.usedAsDiscount` | `0` / `1` | booleano — non enum |
| `hotel_reports.searchType` | `CHECK_IN` / `CHECK_OUT` — **già `ENUM` MySQL** | `enum` `HotelReportSearchType { CheckIn, CheckOut }` |
| `hotel_reports.status` | `pending` / `created` / `cancelled` — **già `ENUM` MySQL** (il create forza `created`) | `enum` `HotelReportStatus { Pending, Created, Cancelled }` — unificare con `isDeleted` (doppio soft-delete) |

Campi che **non** sono enumerazioni (flag booleani `tinyint` da modellare come `bool`, elencati per
completezza): `bookings.checkIn`, `checkOut`, `seasonalPass`, `isSplit`, `isRepeat`, `isDeleted`,
`isOrderFilled`, `isRecalculate`, `isBookingDoneBySuggestedPrice`, `isMarketingCampaign`,
`is_booking_confirmed`; `booking_absence_details.isBooked`; `bookings_send_payment_summaries`
(`cancel_payment_flag`, `booking_cancel_flag`, `email_sent`, `isCallingFromBookingDetail`);
`online_transaction_details.isIframe`; `card_transactions.IsDeleted`; `online_reservations`
(`isDisable`, `isEnableMap`, `isEnablePeriod`, `isEnableLocation`); `service_reservation.isOrderFilled`.
`bookings.booking_consumer.is_guest` / `customerData[].is_guest` = `0`/`1` booleano.

### 2.4ter Campi dei domini Pagamenti (C3), Fiscale (C4) e Stampa RT (C4bis)

> **Separazione C3 / C4 / C4bis** — coerente con
> [`docs/pre/architettura-evolutiva-microservizi.md` §2.2](pre/architettura-evolutiva-microservizi.md)
> e [`docs/pre/linee-guida-implementative-dotnet.md`](pre/linee-guida-implementative-dotnet.md):
> **C3 = modulo `Payment`** (Stripe, incassi/transazioni, rimborsi, borsellino, webhook,
> revenue/reporting, integrazioni POS OfSale/TeamSystem); **C4 = modulo `Fiskaly`** (scontrino
> fiscale **elettronico** IT via API SIGN IT); **C4bis = "Stampa fiscale su hardware RT"** (feature
> a sé — meccanismo legacy alternativo, stampa su registratore di cassa RT fisico via adapter
> Epson/RCH lato client; 3 tabelle: `printer_configs`, `booking_receipts`, `receipt_serial_numbers`;
> **si mantiene come componente separato** — decisione di business del committente, indipendente
> dall'uso attuale). La §4 di questo documento è stata scritta
> con la precedente etichetta unica "C3 Pagamenti & Fiscale": dove vi si legge "C3" per `fiskaly_*` il
> dominio corretto è **C4**, per `booking_receipts` / `receipt_serial_numbers` / `printer_configs` è
> **C4bis**.
> Il modulo di destinazione di ogni riga della tabella è dato dalla nota subito prima della tabella.

Individuati durante l'analisi tabella per tabella della feature "Pagamenti"
(`analisi-pre/db/fiskaly_account.md`, `fiskaly_cash_register.md`, `fiskaly_receipt.md`,
`fiskaly_non_fiscal_receipt.md`, `fiskaly_receipt_log.md`, `fiskaly_receipt_sequence.md`,
`stripe_configrations.md`, `user_stripe_card_details.md`, `keysecret.md`, `receipt_serial_numbers.md`,
`printer_configs.md`, `teamsystem_expenses.md`). Molti stati Fiskaly sono `varchar` liberi che fanno
da **passthrough** ai valori dell'API SIGN IT; alcune costanti esistono già in
[`back-pre/app/models/constants/fiskaly_consts.js`](../../../../../back-pre/app/models/constants/fiskaly_consts.js)
(`FiskalyState`, `FiskalyMode`, `FiskalyRecordState`, `FiskalyVatRate`, `FiskalyVatExemption`) ma
**non sono usate come tipo**: la colonna resta stringa libera. La costante `FiskalyReceiptState` nello
stesso file è **disallineata** dai valori realmente scritti (vedi `fiskaly_receipt.record_state`).

Salvo diversa indicazione in riga, i tipi qui sotto vivono nel **modulo `Fiskaly` (C4)**. Eccezioni:
`stripe_configrations.*`, `user_stripe_card_details.*` e `teamsystem_expenses.*` → **modulo `Payment`
(C3)** (`teamsystem_expenses` è un'integrazione POS il cui record è una spesa/incasso, non un
documento fiscale); `keysecret.appName` → **INFRA/Integrazioni**;
`taxpayer_address_country`/`currency` → **`SharedKernel`** (condivisi C3+C4).
`printer_configs.*` e `receipt_serial_numbers.*` → **feature `C4bis` — "Stampa fiscale su hardware RT"**
(legacy, gestito lato client con adapter Epson/RCH) — **non** è il modulo `Fiskaly`/SIGN IT né C3.

| Campo legacy | Valori reali osservati | Enum o lookup? (modulo) |
|---|---|---|
| `fiskaly_account.taxpayer_state` · `location_state` · `fiskaly_cash_register.system_state` | `ACQUIRED` / `COMMISSIONED` / `DECOMMISSIONED` | `enum` `FiskalyEntityState` nel modulo `Fiskaly` (C4) — costante `FiskalyState` già esistente, portarla a `enum` C# |
| `fiskaly_account.organization_state` · `subject_state` | `ENABLED` / `DECOMMISSIONED` (quest'ultimo forzato solo in DB) | `enum` `FiskalyOrgState { Enabled, Decommissioned }` — vocabolario più ridotto del precedente (Organization/Subject non hanno il ciclo `ACQUIRED`) |
| `fiskaly_account.taxpayer_mode` · `location_mode` · `fiskaly_cash_register.system_mode` | `INACTIVE` / `OPERATIVE` / `SUSPENDED` / `DEGRADED` (nei dati solo `OPERATIVE`) | `enum` `FiskalyMode` — costante `FiskalyMode` già esistente |
| `fiskaly_account.organization_type` | `UNIT` (costante) | `enum` `FiskalyOrganizationType { Unit }` — di fatto costante; ha senso solo per tipizzare la chiamata API |
| `fiskaly_account.taxpayer_address_country` · `fiskaly_receipt`/`fiskaly_non_fiscal_receipt` `currency` | `IT` · `EUR` (costanti nei dati) | non enum di dominio: ISO 3166-1 / ISO 4217 — insieme chiuso condiviso (tabella di riferimento o `enum` in `SharedKernel`), stesso trattamento di `country` (vedi §2.4) |
| `fiskaly_cash_register.system_type` | `FISCAL_DEVICE` (costante) | `enum` `FiskalySystemType { FiscalDevice }` — costante |
| `fiskaly_receipt.record_state` | `INTENTION_CREATED` / `TRANSACTION_CREATED` / `TRANSACTION_FAILED` / `ERECEIPT_FAILED` / `S3_UPLOAD_FAILED` / `COMPLETED` / `CANCELLATION_INTENTION_CREATED` / `CANCELLATION_TRANSACTION_FAILED` / `CANCELLED` (+ passthrough di `content.state` Fiskaly nella stessa colonna) | `enum` `FiskalyReceiptRecordState` nel modulo `Fiskaly` (C4) — **separare** lo stato interno del flusso dal `state` grezzo Fiskaly (che va in un campo dedicato); la costante `FiskalyReceiptState` è da rifare |
| `fiskaly_receipt.record_mode` | `FINISHED` (nei dati) | `enum` `FiskalyRecordMode` — passthrough Fiskaly, tenere anche il grezzo |
| `fiskaly_receipt.source` | `other` / `autoreceipt` | `enum` `ReceiptSource { Manual, AutoReceipt }` |
| `fiskaly_receipt.languageCode` · `fiskaly_non_fiscal_receipt.languageCode` (+ param `language_code` degli endpoint `send`) | `"it"` / `"en"` **e** `1` (en) / `2` (it) — doppia convenzione | `enum` `LanguageCode { It, En }` — unificare stringa e codice numerico in migrazione |
| `fiskaly_receipt.entries[].type` · `fiskaly_non_fiscal_receipt.entries[].type` | `SALE` / `RETURN` (nei dati solo `SALE`) | `enum` `FiskalyLineItemType { Sale, Return }` |
| `…entries[].category` | `SERVICE` / `GOODS` (nei dati solo `SERVICE`) | `enum` `FiskalyLineItemCategory { Service, Goods }` |
| `…entries[].vat.type` | `VAT_RATE` / `VAT_EXEMPTION` | `enum` `FiskalyVatKind { VatRate, VatExemption }` |
| `…entries[].vat.code` | `VAT_RATE`: `STANDARD` / `REDUCED_1` / `REDUCED_2` / `REDUCED_3` (22/10/5/4%). `VAT_EXEMPTION`: `NOT_SUBJECT` / `NOT_TAXABLE` / `CAUSE_1..4` (Natura N1-N6) | **tabella di lookup** `fiskaly_vat_codes` (codice + aliquota + Natura + label IT/EN) — già di fatto un vocabolario con metadati in `fiskaly_consts.js` (`FiskalyVatRate`/`FiskalyVatExemption`), esposto dagli endpoint `fiskaly-helper/vat-rates` / `vat-exemptions`; può crescere (nuove aliquote) senza deploy |
| `fiskaly_receipt.payments[].type` · `fiskaly_non_fiscal_receipt.payments[].type` | `CASH` / `CARD` / `ONLINE` / `OTHER` / `VOUCHER` / `OUTSTANDING` — con **drift** su non-fiscal (`type` assente, `"BANK TRANSFER"` verbatim, `"CASH"`) | `enum` `FiskalyPaymentType` — normalizzare il drift in migrazione (`mapToFiskalyPaymentType` è già la logica di mapping) |
| `fiskaly_receipt.payments[].kind` | `CREDIT` / `DEBIT` (CARD) · `SINGLE` (VOUCHER) | `enum` `CardKind { Credit, Debit }` + `enum` `VoucherKind { Single }` |
| `fiskaly_receipt.payments[].concept` | `SERVICE` (OUTSTANDING) | `enum` `OutstandingConcept { Service }` — costante |
| `fiskaly_receipt.paid_deltas.items[].type` | `package` / `bar_service` / `normal_service` / `hourly_service` / `agreement_voucher_package` / `standard_voucher_fixed` / `standard_voucher_percentage` / `surcharge_fixed` / `surcharge_percentage` / `additional_item` / … | `enum` `LineItemType` — **stesso** `LineItemTypeEnum` di [`controllers/helpers/fiskaly.helper.js`](../../../../../back-pre/app/controllers/helpers/fiskaly.helper.js), condiviso con `bookings.booking_element` e le voci di `booking_number_informations` |
| `fiskaly_receipt_log.state` | come `record_state` + `PDF_GENERATION_FAILED` / `CREATION_FAILED` / `CANCELLATION_INTENTION_FAILED` | `enum` `FiskalyReceiptLogState` — sovrainsieme di `FiskalyReceiptRecordState` con gli step di sola diagnostica |
| `fiskaly_receipt_log.result` | `OK` / `KO` | `enum` `LogResult { Ok, Ko }` (o `bool is_ok`) |
| `fiskaly_receipt_log.fiskaly_record_state` | `ACCEPTED` / `REJECTED` / `COMPLETED` / `FAILED` | `enum` `FiskalyRecordState` — costante `FiskalyRecordState` già esistente |
| `keysecret.appName` | `OfSale` / `Transactions` / `TeamSystem` | `enum` `IntegrationApp` nel modulo INFRA/Integrazioni — vocabolario già centralizzato in `APP_NAMES` (un valore nuovo richiede codice dedicato). Lookup solo se si vuole gestire le integrazioni da pannello |
| `teamsystem_expenses.payload.app` | `"Cassa in Cloud"` (valore fisso richiesto dalla spec) | non enum: costante da validare |
| `teamsystem_expenses.payload.data[].vat` | percentuale IVA come **intero** (`22`) — mentre altrove è `"22.00"` stringa/`22.00` numero | non enum: `numeric` aliquota, unificare la rappresentazione |
| `printer_configs.model` | `FP-81` / `FP-90` / `RCH` / `TM-T88` / `TM-T20` / `ONLINE` — con refusi nei dati (`dddd`, `Asdax`, `fi`, `tets`) | `enum` `PrinterModel { Fp81, Fp90, Rch, TmT88, TmT20, Online }` — ogni modello ha un adapter dedicato lato client (serve codice per aggiungerne uno) |
| `printer_configs.mode` | `fiscal` / `nonfiscal` / `orders` | `enum` `PrinterMode { Fiscal, NonFiscal, Orders }` |
| `stripe_configrations.sellerStripeConnectId` · `user_stripe_card_details.stripeCustomerCardId` / `token` | id esterni Stripe (`acct_…`, `cus_…`, `tok_…`) | **non** enumerazioni: identificativi esterni opachi |

Campi che **non** sono enumerazioni (flag booleani da modellare come `bool`, elencati per
completezza): `fiskaly_account` (`accept_terms` `0`/`1`, `sw_cash_register_default`),
`fiskaly_cash_register` (`is_default`, `is_software`), `fiskaly_receipt` (`emailSent`, `whatsappSent`),
`keysecret.isEnabled`, `printer_configs` (`isActive`, `isdeleted` → `deleted_at`, `SSL` → `use_tls`),
`teamsystem_expenses.fiscalized` (oggi **colonna morta** — mai portata a `1`, vedi scheda),
`fiskaly_receipt.fiskaly_version` e `user_stripe_card_details.token` sono **colonne morte** (mai
scritte/lette) da **non** migrare; `keysecret.expiresAt` è dichiarata ma non usata (nessun controllo
di scadenza sulla chiave).

### 2.4quater Campi del dominio Comunicazione (C6)

Individuati durante l'analisi tabella per tabella del dominio C6 — modulo `Notification` di
[`docs/01-…backend.md` §8](01-criticita-e-indicazioni-backend.md): notifiche in-app
([`notification_details`](../analisi-pre/db/notification_details.md)), canale email
([`email_logs`](../analisi-pre/db/email_logs.md),
[`bathhouse_email_configrations`](../analisi-pre/db/bathhouse_email_configrations.md),
[`custom_templates`](../analisi-pre/db/custom_templates.md)) e marketing
([`contact_list`](../analisi-pre/db/contact_list.md),
[`contact_list_member`](../analisi-pre/db/contact_list_member.md),
[`marketing_event`](../analisi-pre/db/marketing_event.md),
[`marketing_event_notification`](../analisi-pre/db/marketing_event_notification.md),
[`marketing_email_campaign`](../analisi-pre/db/marketing_email_campaign.md),
[`marketing_automation`](../analisi-pre/db/marketing_automation.md)). Alcuni stati marketing sono
**già `ENUM` nativo MySQL**; i tipi "notifica"/"template" sono `int` `1`/`2` liberi.

| Campo legacy | Valori reali osservati | Enum o lookup? (modulo `Notification`, C6) |
|---|---|---|
| `notification_details.type` | `1` = prenotazione ombrellone (`reference_id` → `bookings.id`) / `2` = prenotazione servizi (`reference_id` → `service_reservation.id`) | `enum NotificationSubjectType { Booking, ServiceReservation }` — **stesso** vocabolario di `booking_payments.type` / `service_reservation_payments.type` (`PaymentSubjectType`, §2.4bis): un solo enum, valore copiato nell'evento di dominio, mai FK cross-modulo |
| `notification_details.status` | `1` = notifica visibile / `2` = nascosta finché la prenotazione non è confermata/pagata (resa `1` dai flussi Stripe) | non un enum di dominio: **flag di ciclo di vita/visibilità** — `enum NotificationState { Visible, PendingConfirmation }`, o derivare dallo stato della prenotazione; naming legacy in collisione con `bookings.status` / `service_reservation.status` |
| `email_logs.status` | `0` = in coda (solo newsletter, cron consumatore **spento**) / `1` = inviata/loggata (di fatto **sempre `1`**) | `enum EmailDeliveryStatus { Queued, Sending, Sent, Failed, Bounced }` — l'attuale `0/1` mescola coda e log a posteriori; nessuno stato di fallimento |
| `email_logs.emailType` (parte "categoria") | `Newsletter`, `Campaign Email`, `Automation Email`, `Forgot Password`, `Electronic Receipt`, … (mescolato con l'oggetto reale e nomi di file PDF) | `enum EmailKind { Newsletter, Campaign, Automation, BookingConfirmation, PaymentLink, ForgotPassword, … }` + `subject` separato per l'oggetto |
| `email_logs.cronStatus` | `0` / `1` — **mai letto** | **eliminare** (colonna morta) |
| `bathhouse_email_configrations.smtpSecurity` | `TLS` (unica riga) | `enum SmtpSecurity { None, StartTls, Tls }` — da usare davvero per `secure`/`requireTLS` (oggi i consumatori cablano `secure: true`) |
| `custom_templates.type` | `1` = email / `2` = WhatsApp (commento model) | `enum MessageChannel { Email, WhatsApp }` — **canale** (≠ *soggetto* di `notification_details.type`); base per i futuri canali dell'outbox del modulo Notification |
| `custom_templates.status` | `0` / `1` | `enum ActiveState { Active, Inactive }` — ennesima reimplementazione di "attivo/non attivo" (§2.1) |
| `marketing_event.repeatType` | `never` / `daily` / `weekly` / `monthly` / `yearly` / `custom` — **già `ENUM`** | `enum EventRepeatType { Never, Daily, Weekly, Monthly, Yearly, Custom }` (mappatura diretta) |
| `marketing_event.repeatUnit` | `day` / `week` / `month` / `year` — **già `ENUM`** (sempre `NULL` nei dati) | `enum EventRepeatUnit { Day, Week, Month, Year }` |
| `marketing_event_notification.notificationType` | `1_day_before` / `3_days_before` / `1_week_before` / `custom` — **già `ENUM`** (0 righe) | `enum EventReminderOffset { OneDayBefore, ThreeDaysBefore, OneWeekBefore, Custom }` |
| `marketing_event_notification.status` | `pending` / `sent` / `failed` — **già `ENUM`** (mai diverso da `pending`: nessun mittente) | `enum ReminderStatus { Pending, Sent, Failed }` |
| `marketing_email_campaign.state` | `draft` / `scheduled` / `sent` / `deleted` — **già `ENUM`** | `enum CampaignState { Draft, Scheduled, Sent, Deleted }` — **stesso** vocabolario di `marketing_automation.state` (ordine diverso a schema): unificare; `Deleted` da separare dal soft-delete se si aggiunge `deleted_at` |
| `marketing_automation.state` | `draft` / `sent` / `scheduled` / `deleted` — **già `ENUM`** | `enum AutomationState { Draft, Scheduled, Sent, Deleted }` — unificare con `CampaignState` |
| `marketing_automation.name` (branch del cron) | `welcome` / `goodbye` / altro (confronto `name.toLowerCase()`) | `enum AutomationKind { Welcome, Goodbye, Custom }` — al posto del confronto per stringa |
| `marketing_automation.scheduleType` | `before` / `after` / `custom` — **già `ENUM`** | `enum AutomationScheduleType { Before, After, Custom }` |
| `marketing_automation.scheduleValue[].type` (JSON) | `ofbooking` / `beforebooking` / `afterbooking` / `pendingpayments` | `enum AutomationTrigger { OfBooking, BeforeBooking, AfterBooking, PendingPayments }` |

Campi che **non** sono enumerazioni (booleani `tinyint` da modellare come `bool`, elencati per
completezza): `notification_details.read` (→ stato-lettura **per utente**);
`marketing_event` (`isFullDay`, `showOnEstablishmentPage` — mai usato come filtro,
`notificationsEnabled` — feature incompleta), `bathhouse_email_configrations.status`,
`custom_templates.isDefault`, `contact_list` (`status`, `isDefaultList`), `contact_list_member.status`,
`marketing_email_campaign` (`enablePushNotifications`, `sendCopy` — entrambi attivi nel cron
`sendCampaignEmail`), `marketing_automation.enableAppNotifications` (attivo: push FCM in
`sendAutomationEmail`).
Colonne da **non migrare**: `notification_details.user_id` (contiene un `customers.id`, mai riletto),
`email_logs.cronStatus` (mai letta), `contact_list_member.status` (mai letta), `email_configrations`
(intera — 0 righe, nessun model). `marketing_email_campaign.timezone` / `marketing_automation.timezone`
= identificatori IANA (insieme chiuso "di libreria", non enum di dominio) — come `country` (§2.4).

### 2.4quinquies Campi del dominio Switchboard / Centralino (C7)

Individuati durante l'analisi tabella per tabella del dominio C7 — modulo `Switchboard` di
[`docs/01-…backend.md` §9](01-criticita-e-indicazioni-backend.md): configurazione centralino
([`switchboard_config`](../analisi-pre/db/switchboard_config.md)), IVR ad albero
([`switchboard_menu_item`](../analisi-pre/db/switchboard_menu_item.md),
[`switchboard_menu_action`](../analisi-pre/db/switchboard_menu_action.md)), audio prompt
([`switchboard_audio_file`](../analisi-pre/db/switchboard_audio_file.md)), template WhatsApp
([`switchboard_wa_template`](../analisi-pre/db/switchboard_wa_template.md)) e log
([`switchboard_call_register`](../analisi-pre/db/switchboard_call_register.md),
[`switchboard_call_log`](../analisi-pre/db/switchboard_call_log.md),
[`switchboard_wa_msg_log`](../analisi-pre/db/switchboard_wa_msg_log.md)). **Tutte le 8 tabelle hanno
0 righe** in `local_sb` (`AUTO_INCREMENT = 1`): i valori sotto derivano dal codice
([`switchboardService.js`](../../../../../back-pre/app/Service/switchboardService.js),
[`switchboard.controller.js`](../../../../../back-pre/app/controllers/switchboard.controller.js),
[`telnyx.helper.js`](../../../../../back-pre/app/controllers/helpers/telnyx.helper.js),
[`whatsapp.helper.js`](../../../../../back-pre/app/controllers/helpers/whatsapp.helper.js),
[`sip.helper.js`](../../../../../back-pre/app/controllers/helpers/sip.helper.js)) e dai formati delle API
Telnyx / Meta Cloud, non da dati osservati. Uno dei campi (`switchboard_menu_action.action_type`) è
**già `ENUM` nativo MySQL**; gli altri sono `varchar`/`smallint` liberi.

| Campo legacy | Valori reali osservati | Enum o lookup? (modulo `Switchboard`, C7) |
|---|---|---|
| `switchboard_menu_action.action_type` | `hangup` / `forward_sip` / `send_whatsapp` — **già `ENUM`**; whitelist duplicata anche nel controller | `enum SwitchboardActionType { Hangup, ForwardSip, SendWhatsApp }` — ogni valore ha un ramo di integrazione dedicato (commento del model: "extensible") |
| `switchboard_config.forward_caller_id_mode` | `caller` (default) / `centralino` / `custom` | `enum ForwardCallerIdMode { Caller, Centralino, Custom }` — ramo di logica per ciascuno in `executeMenuAction` |
| `switchboard_config.sip_client_type` | `generic` (default) / (model cita `softphone`/`webrtc`/`mobile`) — **solo descrittivo**, nessun effetto sul flusso chiamata | `enum SipClientType { Generic, Softphone, WebRtc, Mobile }` **solo se** si vuole vincolare; altrimenti stringa libera |
| `switchboard_config.telnyx_phone_number_status` | passthrough Telnyx (`pending` all'ordine, poi `active`/`provisioning`/…) | stato esterno Telnyx: conservare grezzo; eventuale `enum` interno derivato (come `online_transaction_details.status` Stripe, §2.4bis) |
| `switchboard_config.reg_it_customer_type` · `reg_identity_doc_type` | vocabolari chiusi **imposti da Telnyx** per l'ordine numero IT (tipicamente `individual`/`business`; `passport`/`national_id`/`driving_license`) | `enum ItCustomerType` / `enum IdentityDocType` — passthrough dei valori Telnyx |
| `switchboard_wa_template.category` | `UTILITY` (default) / `MARKETING` / `AUTHENTICATION` | `enum WaTemplateCategory { Marketing, Utility, Authentication }` — vocabolario chiuso Meta (impatta pricing e regole di invio) |
| `switchboard_wa_template.meta_status` | `PENDING` (default) / `APPROVED` / `REJECTED` — **già `ENUM`**, ma `refreshWaTemplateStatus` scrive `status.toUpperCase()` grezzo da Meta → possibili `PAUSED` / `DISABLED` / `IN_APPEAL` / `PENDING_DELETION` fuori dall'`ENUM` | `enum WaTemplateStatus` come **passthrough** dello stato Meta (non limitare a 3 valori); tenere eventualmente anche il grezzo |
| `switchboard_wa_msg_log.event_type` | `status` / `message_in` | `enum WaWebhookEventType { Status, MessageIn }` |
| `switchboard_wa_msg_log.status` | `sent` / `delivered` / `read` / `failed` (`null` per gli inbound) | `enum WaMessageStatus { Sent, Delivered, Read, Failed }` |
| `switchboard_wa_msg_log.conversation_origin` · `pricing_category` | `marketing` / `utility` / `service` / `authentication` / `referral_conversion` (origin); `marketing` / `utility` / `authentication` / `service` (pricing) | `enum WaConversationOrigin` / `enum WaPricingCategory` — vocabolari Meta (il pricing evolve con la loro tariffazione: valutare passthrough) |
| `switchboard_wa_msg_log.message_type` | `text` / `button` / `interactive` / `reaction` / `image` / `document` / `audio` / `video` / `location` / `sticker` / `contacts` | `enum WaInboundMessageType { … , Unknown }` — passthrough Meta con fallback |
| `switchboard_call_log.direction` | `incoming` / `outgoing` (grezzo Telnyx) | `enum CallDirection { Incoming, Outgoing }` — solo se si tipizza il log (tabella diagnostica) |

**Non enumerazioni** (per completezza):
- **booleani** `smallint`/`tinyint` → `bool`: `switchboard_config.is_active`,
  `switchboard_menu_item.is_active`, `switchboard_wa_msg_log.pricing_billable`.
- **codici lingua** (insieme "di libreria", non enum di dominio — come `country`/`timezone`, §2.4):
  `switchboard_wa_template.language` (`it` / `en_US` / …), il param `language`/`language_code` degli
  endpoint WhatsApp.
- **caratteri DTMF**: `switchboard_menu_item.dtmf_key` — singolo carattere `[0-9*#]`
  (`valid_digits: "0123456789*#"` in `gatherDTMF`); eventuale `CHECK`, non un `enum`.
- **stringhe diagnostiche non tipizzabili**: `switchboard_call_log.event_type` (eventi Telnyx
  `call.*` **e** passi interni `step.*`).
- **identificatori esterni opachi**: `switchboard_config.telnyx_*` (phone number id, connection id,
  call control app id, number order id, messaging profile id), `switchboard_config.reg_*` id
  (address/document/requirement group Telnyx), `switchboard_config.whatsapp_phone_number_id` /
  `whatsapp_business_account_id`, `switchboard_wa_template.meta_template_id`,
  `switchboard_wa_msg_log.wamid` / `waba_id` / `phone_number_id` / `conversation_id`.

**Colonne da non migrare** (scritte ma mai rilette dal codice, o log grezzi di sola diagnostica):
`switchboard_config.whatsapp_business_account_id` (mai riletto), `switchboard_audio_file.s3_bucket`
(mai riletto — il bucket vive in configurazione, §1), `switchboard_audio_file.mime_type` (costante
`audio/mpeg`); le tabelle [`switchboard_call_log`](../analisi-pre/db/switchboard_call_log.md)
(disabilitata di default via `SB_CALL_LOGGING_ENABLED`, **nessun lettore**) e
[`switchboard_wa_msg_log`](../analisi-pre/db/switchboard_wa_msg_log.md) (**nessun lettore**) sono
log forensici → nella riscrittura vanno sostituiti da tracing/observability o dall'outbox del modulo
`Notification`, non migrati come tabelle di dominio. Da verificare sul DB di produzione il drift di
naming `switchboard_config.deviceFcmToken` / `deviceVoipToken` (camelCase contro `underscored: true`
del model e contro il resto delle colonne `snake_case`).

### 2.5 Anti-pattern da evitare

**Non creare un'unica tabella generica "tutti i valori di tutte le liste"** (pattern EAV/valori
polimorfici, es. `domain_values(id, domain_code, value, label)`). Sembra ridurre la duplicazione ma in
realtà:

- elimina i vincoli di tipo e la possibilità di FK dirette (il riferimento diventa `domain_code + value`,
  di nuovo un confronto per stringa — lo stesso problema che si voleva risolvere);
- rende impossibile indicizzare/filtrare in modo mirato;
- concentra in un'unica tabella dati concettualmente non correlati, aumentando l'accoppiamento invece di
  ridurlo.

Meglio tanti `enum` locali (a costo zero) e poche tabelle di lookup piccole e tipizzate, ciascuna nello
schema del modulo che possiede il concetto — mai una tabella condivisa fra moduli.

### 2.6 Migrazione dei dati esistenti

A differenza della migrazione URL (§1.5), qui il passo preliminare non è solo tecnico ma **richiede una
normalizzazione dei dati**: prima di mappare un campo su un `enum`/lookup, va costruita la tabella di
frequenza dei valori reali (query `GROUP BY` sul campo) e va deciso esplicitamente a quale valore canonico
riportare ogni variante (es. `"Daily price"` e `"Daily price with weekend"` → verificare con il team
prodotto se sono davvero due concetti o un unico valore scritto in due modi). Solo dopo questa
normalizzazione lo script di migrazione può scrivere il valore enum/l'id di lookup corretto — mappare alla
cieca ogni stringa distinta a un proprio valore enum riprodurrebbe il drift invece di risolverlo.

### 2.7 Placeholder dei template (segnaposto `@…` / `{…}`)

Caso particolare di "vocabolario chiuso non vincolato": i **segnaposto** dei testi/template di
comunicazione (dominio **C6**). L'analisi tabella per tabella
(`analisi-pre/db/custom_templates.md`, `hotel_message_templates.md`, `marketing_automation.md`) ha
trovato **tre motori di sostituzione indipendenti, due sintassi diverse e nomi divergenti per lo
stesso concetto**.

**Motori di sostituzione oggi (tutti diversi):**

| Dove | Motore | Sintassi | Sostituzione | Segnaposto sconosciuto |
|---|---|---|---|---|
| `hotel_message_templates` (subject + text) → report convenzioni | [`Service/hotelReportNotificationService.js`](../../../../../back-pre/app/Service/hotelReportNotificationService.js) `applyPlaceholders` (regex, vocabolario chiuso di 8 chiavi) | `@chiave` | **server** | lasciato **letterale** |
| `custom_templates.text` / `.subject` → "riepilogo prenotazione" | `front-pre/src/services/CommonServices.js` `matchValuesToObject` (regex per riga, avvolge il valore in `<strong>`) | `@chiave` | **client** | **scarta l'intera riga** se il valore è vuoto o mancante |
| `marketing_automation.textFields` → welcome/goodbye/custom | [`controllers/helpers/cron.helper.js`](../../../../../back-pre/app/controllers/helpers/cron.helper.js) `replaceTextFields` (regex per chiave, **appende `<br/>`** a ogni valore; **3 mappe diverse** nei 3 call-site) | `@chiave` | **server** | lasciato letterale |
| `marketing_email_campaign.message` | template EJS `mailtemplates/campaignEmail.ejs` (`<%= MESSAGE %>`) | — (inietta l'intero corpo) | server | — |

Inoltre la scheda di `hotel_message_templates` segnala record il cui **subject** usa le graffe
(`{hotelName}`, `{date}`) mentre il **body** usa la chiocciola: le graffe **non sono gestite da alcun
motore** (`applyPlaceholders` riconosce solo `@chiave`) → resterebbero letterali nel messaggio.

**Nomi divergenti per lo stesso concetto** (da normalizzare a un vocabolario unico):

| Concetto | Varianti legacy osservate |
|---|---|
| nome dello stabilimento / struttura | `@propertyname` · `@facilityname` · `@establishmentname` |
| nome del cliente | `@fullname` · `@surnamename` |
| telefono | `@phonenumber` · `@telephonenumber` |
| data / periodo | `@date` · `@dates` · `@reportperiod` · `@reportcreationdate` |
| importo totale | `@total` (ma `custom_templates` lo usa per il totale prenotazione, `hotel_message_templates` per il totale report) |
| altri (solo `custom_templates`) | `@location` · `@row` · `@package` · `@deposit` · `@QRcode` |
| altri (solo `hotel_message_templates`) | `@totalbookings` · `@email` |
| altri (solo `marketing_automation`) | `@email` · `@dates` |

**Indicazione per la riscrittura (modulo `Notification`, C6):**

1. **Una sola sintassi**, esplicita e con delimitatore di chiusura: `{{ nome_variabile }}` (mustache).
   Niente `@` (collide con le email nel testo) né `{…}` singola (collide con JSON/JS nei corpi).
2. **Un solo renderer** server-side, con **auto-escaping** per canale (HTML per email, testo per
   WhatsApp/SMS/push) — mai `String.replace` nel browser.
3. **Vocabolario canonico unico** dei segnaposto, versione `enum`/costante + tabella di mappatura
   `legacy → canonico` usata dallo script di migrazione dei template esistenti. Bozza di canonici:
   `establishment_name`, `customer_full_name`, `customer_first_name`, `customer_phone`,
   `customer_email`, `booking_date_from`, `booking_date_to`, `booking_locations`, `booking_package`,
   `booking_total`, `booking_deposit`, `booking_qr_code`, `report_period`, `report_total`,
   `report_bookings_count`.
4. **Segnaposto sconosciuto o valore mancante → stringa vuota** (o placeholder di debug in ambiente
   non-prod), **mai** il token letterale nel messaggio inviato e **mai** `undefined`.
5. I template restano dati (`custom_templates` + `hotel_message_templates` unificati in un unico
   catalogo `notification` — vedi §3.1bis), ma l'**insieme dei segnaposto ammessi** è chiuso e
   validato alla `create`/`update` del template (rifiuto se contiene un token fuori vocabolario).

---

## 3. Convenzioni di schema PostgreSQL

Regole trasversali per il nuovo schema, indipendenti dal singolo modulo.

### 3.1 Naming

- **Tabelle e colonne in `snake_case`**, tutto minuscolo. Nomi di tabella al **plurale** (`bookings`,
  `booking_payments`, `booking_item_consumptions`). Niente `camelCase` (il legacy mescola
  `booking_start_date` e `bookingNumber` nella stessa tabella — da eliminare).
- **Chiavi**: `id` per la PK; le FK si chiamano `<tabella_singolare>_id` (`booking_id`,
  `property_id`, `hotel_price_list_id`). Nessun prefisso ungherese, nessun `tbl_`.
- **Booleani**: prefisso `is_` / `has_` / `can_` (`is_deleted`, `is_checked_in`, `has_seasonal_pass`).
- **Timestamp**: `created_at`, `updated_at`, `deleted_at` (vedi §3.3 e §3.4). Le date "di business"
  restano esplicite (`booking_start_date`, `absence_date`).
- **Enum**: valore singolare in minuscolo (`pending`, `paid`); il tipo enum PostgreSQL si chiama
  `<contesto>_<concetto>` (`booking_status`, `payment_mode`).
- **Importi/prezzi**: **niente suffisso `_amount`** sui campi di riepilogo di un aggregato — il nome è
  il concetto (`subtotal`, `discount`, `surcharge`, `total`, `deposit`, `paid`, `balance_due`); il
  valore forzato a mano è `price_override`, non `applied_price`. Allineato alla forma del contratto API
  (§3.7). Dove una tabella ha **più** colonne di prezzo, la forma canonica è
  **`<qualificatore>_price`** (`daily_price`, `weekend_price`, `days_7_price`, `days_14_price`,
  `monthly_price`) — non mescolarla con `price_<qualificatore>` nello stesso gruppo. Il numero, se
  serve, sta **dentro** il nome (`days_7_price`), mai in testa: un identificatore non può iniziare con
  una cifra né in PostgreSQL (unquoted) né in C#.
- **Campi snapshot** (valore *congelato* copiato da un'altra entità al momento in cui si "fotografa" il
  fatto, §3.6): il suffisso **`_snapshot`** si mette **solo** quando il campo è un'**etichetta/nome
  descrittivo** di quell'entità tenuto **accanto alla FK viva** (`spot_label_snapshot`,
  `sector_label_snapshot`, `package_name_snapshot`, `customer_full_name_snapshot`) — serve a dire "non
  aspettarti che seguìta la sorgente". I **numeri/enum che sono dato proprio del documento** (prezzo di
  riga, aliquota IVA, metodo di calcolo, valore dello sconto bloccati alla conferma) **non** portano il
  suffisso: la loro natura congelata sta nella descrizione del campo, non nel nome. Vedi §3.6.

### 3.1bis Domini, sottoaree e nome delle tabelle

Un **dominio** (= modulo, = una delle classificazioni C1…C7) può avere **sottoaree**: raggruppamenti
funzionali interni che condividono lo stesso ciclo di vita e lo stesso team ma sono coesi tra loro (es.
in **C6 Comunicazione** la sottoarea **marketing** — contatti, eventi, campagne, automazioni — è
distinta dai canali di invio; in **C3 Pagamenti** **gateway**, **ledger**, **wallet**, **pos**). La
sottoarea **non** è un modulo a sé (nessuna regola "zero FK cross-modulo" al suo interno) e **non** è
un secondo schema PostgreSQL: è solo un prefisso del nome tabella.

**Convenzione di naming**: `<dominio>_<sottoarea se presente>_<nome specifico>`

- `<dominio>`: nome breve del modulo, minuscolo, **singolare** — `booking`, `catalog`, `payment`,
  `fiskaly`, `fiscalprint`, `identity`, `notification`, `switchboard`, `infra`, `audit`.
- `<sottoarea>`: **solo se il dominio ne ha più d'una** e la tabella appartiene chiaramente a una;
  minuscolo, **una parola** — `marketing`, `email`, `pricing`, `gateway`, `pos`, …
- `<nome specifico>`: `snake_case`, **plurale** (regola §3.1) — `contact_lists`, `email_campaigns`.
- **Forma canonica** (schema-per-modulo, preferita): lo **schema PostgreSQL è `<dominio>`** e il nome
  tabella è `<sottoarea>_<nome specifico>` — dentro lo schema del modulo il prefisso di dominio
  sarebbe ridondante. Es. schema `notification`, tabella `marketing_contact_lists` →
  `notification.marketing_contact_lists`.
- **Forma flat** (se si adotta un unico schema): il nome fisico completo è
  `<dominio>_<sottoarea>_<nome specifico>` (`notification_marketing_contact_lists`).

**Domini e sottoaree individuati nell'analisi:**

| Dominio (modulo) | Sottoaree | Note |
|---|---|---|
| `booking` (C1) | `spot` (ombrellone/postazione), `service` (servizi), `hold` (riserva/blocco), `group` | testata `booking.reservations` + righe `booking.lines` senza sottoarea (nucleo del dominio) |
| `catalog` (C2) | `map` (mappa spiaggia), `pricing` (stagioni/listini/sconti), `service` (servizi), `inventory` (magazzino), `agreement` (convenzioni/hotel) | — |
| `payment` (C3) | `gateway` (Stripe Connect), `transaction` (online + rimborsi), `ledger` (movimenti), `wallet` (borsellino prepagato), `pos` (integrazioni OfSale/TeamSystem) | — |
| `fiskaly` (C4) | *(nessuna)* | dominio piccolo: `fiskaly.accounts`, `fiskaly.receipts`, … |
| `fiscalprint` (C4bis) | *(nessuna)* | `fiscalprint.printer_configs`, `fiscalprint.receipts`, `fiscalprint.serial_numbers` |
| `identity` (C5) | `access` (utenti/ruoli), `customer` (anagrafica clienti finali) | — |
| `notification` (C6) | `inapp` (notifiche in-app), `email` (canale email), `whatsapp` (canale WhatsApp), `marketing` (contatti/eventi/campagne/automazioni) | i **template** sono trasversali ai canali → `notification.templates` (senza sottoarea) |
| `switchboard` (C7) | `voice` (chiamate/IVR), `messaging` (WhatsApp/SMS) | — |
| `chatbot` (C8) | *(nessuna)* | assistente AI dell'app gestionale: `chatbot.conversation_turns`, `chatbot.manager_configs`, `chatbot.llm_providers` — dettaglio in [`analisi-post/db/_feature-tabelle.md` § Dominio C8](../analisi-post/db/_feature-tabelle.md) |
| `infra` | `integration` (credenziali partner) | ex `keysecret` → `infra.integration_credentials` |
| `audit` | *(nessuna)* | registro trasversale "chi ha fatto cosa": `audit.trail_entries` — deciso il 2026-10-07, [86-modulo-audit.md](86-modulo-audit.md) |

**Esempio di rinomina — dominio `notification` (C6):**

| Tabella legacy | Sottoarea | Target (`schema.tabella` / forma flat) |
|---|---|---|
| `notification_details` | inapp | `notification.inapp_items` / `notification_inapp_items` |
| `email_logs` | email | `notification.email_outbox` / `notification_email_outbox` |
| `bathhouse_email_configrations` | email | `notification.email_channel_configs` / `notification_email_channel_configs` |
| `custom_templates` + `hotel_message_templates` *(unificate)* | — (trasversale) | `notification.templates` / `notification_templates` |
| `contact_list` | marketing | `notification.marketing_contact_lists` / `notification_marketing_contact_lists` |
| `contact_list_member` | marketing | `notification.marketing_contact_list_members` / `notification_marketing_contact_list_members` |
| `marketing_event` | marketing | `notification.marketing_events` / `notification_marketing_events` |
| `marketing_event_notification` | marketing | `notification.marketing_event_reminders` / `notification_marketing_event_reminders` *(se non dismessa)* |
| `marketing_email_campaign` | marketing | `notification.marketing_email_campaigns` / `notification_marketing_email_campaigns` |
| `marketing_automation` | marketing | `notification.marketing_automations` / `notification_marketing_automations` |
| *(nuova — outbox unico per destinatario)* | — | `notification.deliveries` / `notification_deliveries` |

### 3.1ter Identificatori: chiave primaria interna e id pubblico

> Decisione presa per il dominio **C5 (identity)** e adottata come **default di progetto** per tutti i
> moduli salvo motivo contrario. **Supera** la precedente indicazione "UUIDv7 come PK ovunque" di
> [`pre/identificatori-e-uuid.md`](pre/identificatori-e-uuid.md) (aggiornato di conseguenza). Le ragioni
> di sicurezza di quel documento (le PK non sono segreti; l'autorizzazione per-risorsa è il vero
> controllo; reset/inviti/sessioni usano token random dedicati) **restano valide e invariate**.

- **Chiave primaria**: `id bigint` generato come identità (`GENERATED ALWAYS AS IDENTITY`) su **ogni**
  tabella. Motivi: migrazione **1:1** dagli id interi del legacy (nessuna mappa `legacy_id → id` da
  propagare su tutte le FK; dual-run e riconciliazione col vecchio DB immediati — vedi
  [`pre/refactoring-analisi-A-vs-B.md`](pre/refactoring-analisi-A-vs-B.md)), indici e FK compatti
  (8 byte), località di scrittura senza bloat.
- **Id pubblico**: colonna `public_id uuid NOT NULL DEFAULT <uuidv7> UNIQUE` (UUIDv7, per ordinamento
  temporale e località d'indice — generato in factory con `Guid.CreateVersion7()`, §2 di
  [`pre/identificatori-e-uuid.md`](pre/identificatori-e-uuid.md)) **solo sulle entità referenziate
  fuori dal proprio schema di modulo**: quelle esposte in URL/REST, negli endpoint GDPR
  ([`11-modulo-privacy.md` §5](11-modulo-privacy.md)) e — soprattutto — quelle **il cui id viene
  copiato in eventi/proiezioni da altri moduli**. In C5: `identity_access_users`,
  `identity_customer_customers`. Le tabelle referenziate **solo internamente** allo schema (lookup
  ruoli, piani, documenti/accettazioni contrattuali, preferiti, scope risorse, impostazioni sicurezza)
  restano **solo `bigint`**.
- **Contratto cross-modulo (bidirezionale)**: quando un modulo memorizza un riferimento a un'entità di
  **un altro** modulo, copia il **`public_id` (uuid)** di quell'entità, **mai** il suo `bigint` (che non
  esce dallo schema proprietario). Vale in entrambe le direzioni:
  - *altri moduli → C5*: Booking/Payment/Notification/Fiskaly che puntano a `identity_access_users` /
    `identity_customer_customers` copiano il loro `public_id`;
  - *C5 → altri moduli*: le tabelle C5 che puntano a `property` o `bathhouse` (C2) memorizzano il
    `public_id uuid` di quelle entità — colonne `property_id` / `bathhouse_id` **di tipo `uuid`**, non
    `bigint`.

  Così l'estrazione futura di un modulo in microservizio
  ([`pre/architettura-evolutiva-microservizi.md`](pre/architettura-evolutiva-microservizi.md)) non
  richiede modifiche di schema: il riferimento è già globale e stabile. Coerente con
  [`pre/autenticazione-autorizzazione-dotnet.md` §9](pre/autenticazione-autorizzazione-dotnet.md)
  ("Collegamento **per ID**, non FK cross-schema": `Customer.IdentityAccountId` nullable).
- **Nome vs tipo delle colonne di riferimento**: le colonne FK **interne** allo schema di un modulo
  (`user_id`, `role_id`, `license_plan_id`, …) sono `bigint` e puntano alla PK `id`. Le colonne che
  referenziano **un altro** modulo (`property_id`, `bathhouse_id`, `customer_id` visto da fuori
  identity, …) sono `uuid` e contengono il `public_id`. Il nome resta `<entità>_id` (§3.1); il tipo
  dice se è interno (`bigint`) o cross-modulo (`uuid`).
- **Traduzione al confine**: gli input che arrivano da fuori (path param, payload evento) sono
  `public_id`; si risolvono in `id` interno **una volta**, al bordo del modulo, prima di qualsiasi
  lavoro di persistenza. `public_id` e `id` sono entrambi valorizzati alla `INSERT` e **immutabili**.
- **Discriminatore tenant**: la colonna `property_id` sulle tabelle multi-tenant è di tipo **`uuid`** e
  contiene il `public_id` della `property` (C2). Indicizzata (§3.11), spesso prima colonna di indici
  compositi; RLS e query filter la confrontano come `uuid`. Vedi [`34-multitenant.md`](34-multitenant.md).

### 3.1quater Tabelle a schema/scopo ricorrente: nome condiviso tra moduli

Alcune tabelle **non sono specifiche di un dominio**: risolvono un problema tecnico che **ogni modulo**
può avere (file, read-model, traduzioni). Per non reinventare struttura e nome ogni volta, hanno un
**nome canonico condiviso** — ogni modulo che ne ha bisogno crea la **propria** copia nel proprio schema
(nessuna tabella globale cross-modulo, coerente con §1.4 e §3.1ter). Le tre famiglie individuate:

#### A) `<dominio>_media_assets` — riferimenti a file (§1)

Tabella di supporto **per modulo** che scompone ogni URL in `origin` + `path` (§1.2–§1.3). I campi-file
delle tabelle del modulo diventano FK verso questa; gli array di URL diventano tabelle di giunzione
ordinate (es. `catalog_bathhouse_photos`).

| Elemento | Nome | Note |
|---|---|---|
| Tabella | `<dominio>_media_assets` (canonica: `<dominio>.media_assets`) | una per modulo che possiede file |
| Colonne | `id`, `origin`, `path`, `kind`, `checksum?`, `byte_size?`, `content_type?`, `created_at`, `updated_at`, `deleted_at` | `path` = URL intero se `origin = external` |
| Enum `origin` | `<dominio>.media_origin` | valori = codici sorgente (`s3_us_east_2`, `cloudfront`, `external`, …); la mappa `origin → base URL` vive in configurazione |
| Enum `kind` | `<dominio>.media_kind` | valori **specifici del modulo** (C2: `photo`, `pdf`, `icon`) |

Prima realizzazione: [`analisi-post/db/catalog/catalog_media_assets.md`](../analisi-post/db/catalog/catalog_media_assets.md).
Futuri: `fiskaly_media_assets` (PDF scontrini), `notification_media_assets` (allegati/loghi email),
`switchboard_media_assets` (prompt audio) — stessa forma.

#### B) `<dominio>_config_snapshots` — read-model JSON di configurazione (§7)

Tabella **per modulo** che materializza in `jsonb` la configurazione servita *wholesale* al frontend
(fonte di verità = le tabelle normalizzate; il payload è sempre ricostruibile). Vedi §7 per regola,
aggiornamento, `assetRef`, `ETag`.

| Elemento | Nome | Note |
|---|---|---|
| Tabella | `<dominio>_config_snapshots` (canonica: `<dominio>.config_snapshots`) | 1 riga per `(<scope>_id, snapshot_kind)` |
| Colonne | `id`, `<scope>_id` (di norma `property_id`), `snapshot_kind`, `payload jsonb`, `payload_schema smallint`, `source_hash`, `content_hash`, `stale_since timestamptz NULL`, `built_at`, `created_at`, `updated_at`, `deleted_at` | `UNIQUE (<scope>_id, snapshot_kind)` |
| Enum `snapshot_kind` | `<dominio>.config_snapshot_kind` | valori **specifici del modulo** (C2: `property_management_config`, `beach_map`, `public_showcase`, `price_catalog`, `services_catalog`, `agreement_catalog`) |
| Read-model **interrogato con filtri** | tabella **dedicata** con colonne vere indicizzate (non un `snapshot_kind`) | C2: `catalog_search_bathhouses` (`min_price`, `amenity_ids` GIN, `location` GiST) + un `jsonb` per il solo payload di rendering |

Prima realizzazione: [`analisi-post/db/catalog/catalog_config_snapshots.md`](../analisi-post/db/catalog/catalog_config_snapshots.md),
[`catalog_search_bathhouses.md`](../analisi-post/db/catalog/catalog_search_bathhouses.md).

#### C) `<entità>_translations` — traduzioni di un vocabolario gestito a runtime (§2.2.B)

Tabella **figlia** che porta le etichette multilingua di un vocabolario **gestito a runtime** e con
**testo mostrato all'utente** (piani, paesi, categorie/dotazioni consumer-facing). I vocabolari
**code-bound** (usati in codice: ruoli, stati, metodi di calcolo, giorni della settimana) usano invece
**risorse di localizzazione** `.resx`/`.json` con chiave = `code` (§2.2.A) — **nessuna** tabella
`_translations`.

| Elemento | Nome | Note |
|---|---|---|
| Tabella | `<nome_tabella_padre_singolarizzato>_translations` | es. `catalog_countries` → `catalog_country_translations`; `catalog_service_amenities` → `catalog_service_amenity_translations`; `identity_account_license_plans` → `identity_account_license_plan_translations` |
| Colonne | `id`, `<padre>_id` (FK `ON DELETE CASCADE`), `culture varchar(10)` (BCP-47, es. `it-IT`), il/i campo/i tradotti (`name`, `label`, `description`, …), `created_at`, `updated_at`, `deleted_at` | — |
| Indice | `UNIQUE (<padre>_id, culture) WHERE deleted_at IS NULL` + `(culture)` parziale | una traduzione per (riga, lingua) |
| Regola di modellazione | **mai** `label_it`/`label_en`/… a colonne sul padre; il nome nella lingua di default può restare sul padre come `name`, le altre lingue nella figlia | vedi §2.2.B |

Prima realizzazione: `identity_account_license_plan_translations` (C5),
[`catalog_country_translations`](../analisi-post/db/catalog/catalog_country_translations.md),
[`catalog_service_amenity_translations`](../analisi-post/db/catalog/catalog_service_amenity_translations.md).

> **Nome scelto**: **`_translations`** (plurale). Attaccato allo stem **singolare** della tabella padre
> (`countries` → `country_`, `service_amenities` → `service_amenity_`), coerente con la regola FK
> `<tabella_singolare>_id` di §3.1.

### 3.2 Riferimenti a un utente: il nome della colonna deve dire *quale* utente

Nel legacy lo stesso concetto — "l'operatore/gestore dello stabilimento che ha creato o toccato la
riga" — è scritto con quattro nomi diversi e incoerenti:

| Tabella legacy | Colonna legacy | Chi è, davvero |
|---|---|---|
| `bookings` | `booking_agent` | id utente operatore che ha inserito la prenotazione da gestionale |
| `card_transactions` | `operated_by` | id utente operatore che ha registrato il movimento carta |
| `hourly_service_booking_management` | `user_id` | id utente operatore che ha registrato lo slot (**non** il cliente) |
| `hotel_reports` | `generated_by` | id utente che ha generato il report |
| `location_reservations` | `agent` | id utente operatore che ha marcato la postazione come riservata |
| `service_reservation` | `agent` / `updatedById` | operatore che ha creato / modificato |
| `bookings_requested_details` | `booking_request_by` | id **utente consumer** che ha aperto la prenotazione da link |

**Regola**: la colonna che referenzia `users.id` deve contenere le parole **`user`** e **`id`** e un
qualificatore che ne renda esplicito il ruolo. Schema suggerito: `user_<ruolo>_id`.

| Ruolo | Colonna | Uso |
|---|---|---|
| Operatore che ha **creato** la riga | `user_operator_id` (o `created_by_user_id`) | ex `agent`, `operated_by`, `user_id`, `generated_by` |
| Operatore che ha fatto l'**ultima modifica** | `user_updated_by_id` | ex `updatedById` |
| Manager/titolare dello stabilimento | `user_manager_id` | quando il ruolo è specificamente "manager" |
| Cliente consumer (account) | `user_customer_id` | ex `booking_request_by`; **non** confondere con `customer_id` che punta a `customers.id` (anagrafica), non a `users.id` (account di login) |
| Bagnino / lifeguard | `user_lifeguard_id` | dove serve |

Se la stessa riga ha più attori (creatore + ultimo modificatore), sono **due colonne distinte**, non
un campo riusato. Tutte con FK reale verso `users(id)`.

### 3.3 Coerenza dei timestamp

- **Sempre** `created_at` e `updated_at` su ogni tabella (vedi §3.4 per la valorizzazione automatica).
  Eliminare i doppioni del legacy (`created_at` **e** `createdAt` sulla stessa tabella —
  `booking_payments`, `service_reservation_payments`, `location_reservations`).
- Tipo: **`timestamptz`** (timestamp con time zone), memorizzato in UTC. Mai `datetime`/`timestamp`
  senza time zone: il legacy fa `moment.parseZone(...).format('YYYY-MM-DD HH:mm:ss')` e salva stringhe
  locali, con `+1 giorno` di compensazione fuso sparsi nel codice — sorgente di bug.
- Le date "solo giorno" (`booking_start_date` a mezzanotte, `absence_date`) usano `date` quando l'ora
  non ha significato, altrimenti `timestamptz`.

### 3.4 Valorizzazione automatica di `created_at` / `updated_at`

Non serve scrivere codice applicativo. Due livelli, complementari:

**A livello DB (PostgreSQL)** — difesa anche per scritture fuori da EF:
```sql
created_at  timestamptz NOT NULL DEFAULT now(),
updated_at  timestamptz NOT NULL DEFAULT now()
```
e un trigger per `updated_at`:
```sql
CREATE OR REPLACE FUNCTION set_updated_at() RETURNS trigger AS $$
BEGIN NEW.updated_at = now(); RETURN NEW; END; $$ LANGUAGE plpgsql;

CREATE TRIGGER trg_set_updated_at BEFORE UPDATE ON <tabella>
FOR EACH ROW EXECUTE FUNCTION set_updated_at();
```

**A livello EF Core** — così i valori sono coerenti anche prima del round-trip:
- un `SaveChangesInterceptor` (o override di `SaveChangesAsync`) che, per ogni entità che implementa
  `IAuditable`, imposta `CreatedAt`/`UpdatedAt` su `Added`/`Modified`;
- oppure, per il solo default di inserimento, `Property(x => x.CreatedAt).HasDefaultValueSql("now()")`
  + `.ValueGeneratedOnAdd()`.

Non usare `[DatabaseGenerated]` da solo (non aggiorna su UPDATE) né logica sparsa nei controller
(il pattern legacy, che assegna `createdAt`/`updatedAt` a mano in decine di punti).

### 3.5 Soft-delete uniforme (`deleted_at`) e filtro implicito

Sì: **ogni tabella "di dominio" ha `deleted_at timestamptz NULL`** (`deleted_at IS NULL` ⇔ riga
attiva, `deleted_at IS NOT NULL` ⇔ cancellata *e quando*). Si preferisce `deleted_at` a un booleano
`is_deleted` perché: (a) porta con sé il *quando* senza colonna aggiuntiva, utile per ripristino/audit
e per le retention policy; (b) è un pattern EF/Npgsql idiomatico; (c) permette indici parziali
`WHERE deleted_at IS NULL` altrettanto efficienti. Il legacy ha già `isDeleted`/`IsDeleted`/`status=0`
sparsi e incoerenti (a volte `int`, casing diverso — `card_transactions.IsDeleted`): tutti si mappano
su `deleted_at` in migrazione (`isDeleted=1` → `deleted_at = updated_at` o una data convenzionale).

**Interfaccia comune** (in `SharedKernel` del backend):
```csharp
public interface ISoftDeletable { DateTimeOffset? DeletedAt { get; set; } }
```

**Dove si registra il Global Query Filter.** Va in **`OnModelCreating` del `DbContext` del modulo**
(ogni modulo ha il suo `DbContext`, §1/§5 di
[linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)). Non su ogni entità
a mano: un **loop di convenzione** che lo applica a tutte le entità `ISoftDeletable`, così non lo si
dimentica mai:

```csharp
// Modules/Booking/Infrastructure/BookingDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

        // e => e.DeletedAt == null
        var e = Expression.Parameter(entityType.ClrType, "e");
        var body = Expression.Equal(
            Expression.Property(e, nameof(ISoftDeletable.DeletedAt)),
            Expression.Constant(null, typeof(DateTimeOffset?)));
        modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(Expression.Lambda(body, e));
    }
}
```
(Se si preferisce la forma esplicita per una singola entità è
`modelBuilder.Entity<Booking>().HasQueryFilter(b => b.DeletedAt == null);` — ma la convenzione sopra
è quella da adottare.)

**Come si "cancella" davvero** — un `SaveChangesInterceptor` converte la `DELETE` in `UPDATE`:
```csharp
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries<ISoftDeletable>()
                     .Where(x => x.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
// registrazione: options.AddInterceptors(new SoftDeleteInterceptor());
```

Note operative:
- il filtro si applica automaticamente a ogni `LINQ`, incluse le `Include` (EF lo propaga alle
  navigazioni: una FK verso una riga soft-deleted fa diventare la navigazione `null`);
- per includere i cancellati (report, ripristino, riconciliazione): `.IgnoreQueryFilters()`;
- **non** replicare la logica lato DB con una `WHERE` in ogni vista: il punto di verità del filtro è
  EF; a DB restano solo `deleted_at` + indici parziali `WHERE deleted_at IS NULL`;
- vincoli di unicità che devono valere solo tra le righe vive → indice unico parziale
  (`CREATE UNIQUE INDEX ... WHERE deleted_at IS NULL`).

Tabelle di puro log/append (es. `booking_logs` come sequenza, `transactions_payments` se diventa
append-only) **non** hanno `deleted_at` e **non** implementano `ISoftDeletable`.

**Soft-delete ≠ conservazione a vita: oltre al soft-delete c'è l'hard-delete di retention.** Il
`deleted_at` marca una riga come "logicamente cancellata *e quando*", non "da tenere per sempre".
Sopra vi agiscono i **batch di retention** definiti in [80-retention.md](80-retention.md), che
rimuovono **fisicamente** (o anonimizzano/archiviano) le righe a fine finestra:

- righe con `deleted_at` più vecchio della finestra di ripristino/audit (default **90 giorni**, o il
  termine legale se più lungo) → **hard-delete** (`CompactSoftDeleted`, [80-retention.md §6](80-retention.md));
- righe **ancora attive** ma oltre la loro retention operativa/legale (prenotazioni di stagioni
  passate, contatori d'inventario, log, snapshot JSON pesanti, dati marketing, clienti inattivi) →
  hard-delete, **pseudonimizzazione** o **archiviazione su storage freddo** secondo la categoria di
  dato (tabellone di [80-retention.md §3](80-retention.md));
- i dati con obbligo di conservazione (fiscale/contabile: **10 anni**) non vengono mai hard-deleted
  prima della scadenza — al più archiviati fuori dal DB operativo.

Il `SaveChangesInterceptor` che converte `DELETE`→`UPDATE` (sopra) è quindi solo il **primo stadio**;
il secondo stadio (la cancellazione vera) è un job schedulato, idempotente, con backup verificato a
monte — vedi [80-retention.md §0 e §6](80-retention.md) e [20-jobs.md](20-jobs.md). Per eseguire
l'hard-delete effettivo il job usa `.IgnoreQueryFilters()` + `ExecuteDelete()`.

### 3.6 Campi snapshot: come gestire i dati "fotografati" che nell'originale possono cambiare

Problema ricorrente: `bookings.booking_consumer` è una copia JSON di `customers` al momento della
prenotazione; se il cliente poi cambia email/telefono, la prenotazione resta con i vecchi dati (a
volte è voluto — ricevute già emesse — a volte no). Lo stesso vale per `booking_payments.locationData`,
`hotel_payments.detailPrices`, `transactions_payments.customer*`, ecc.

**Criterio**: decidere per ogni campo se è **riferimento vivo** o **snapshot immutabile**, e non
lasciare che sia un caso.

1. **Riferimento vivo** (default): non copiare nulla. FK verso la riga sorgente (`customer_id` →
   `customers.id`) + JOIN al momento della lettura. Vale per tutto ciò che deve "seguire" l'anagrafica
   (nome cliente mostrato nell'agenda di oggi, contatti per ricontattare).

2. **Snapshot immutabile** quando il valore fa parte di un **documento/fatto storico** che non deve
   cambiare retroattivamente (riga di scontrino/ricevuta, movimento contabile, report inviato,
   prezzo bloccato alla conferma). In questo caso:
   - colonne tipizzate esplicite, **non** un blob JSON: `customer_full_name`, `customer_email`,
     `unit_price_gross`, `vat_rate` — così sono indicizzabili, validabili e leggibili;
   - popolate **una sola volta** alla creazione del fatto (o alla transizione di stato che lo
     "congela", es. `confirmed_at`), poi mai riscritte;
   - mantenere comunque la FK alla sorgente (`customer_id`) accanto allo snapshot, per poter
     risalire e per audit;
   - dove ha senso, un campo `priced_at` / `snapshot_at` che dice *a quando* si riferisce la foto.

3. **Versionamento della sorgente** (solo se serve ricostruire "com'era" un dato che cambia spesso e
   di cui non basta lo snapshot puntuale): tabella di storico `*_history` con `valid_from`/`valid_to`
   (SCD-2), oppure `PERIOD FOR SYSTEM_TIME` / estensione temporale. Da usare con parsimonia: costa
   complessità. Per il dominio prenotazioni, snapshot tipizzato (punto 2) è quasi sempre sufficiente.

4. **Aggregato che possiede i suoi dati**: la prenotazione è un aggregato; alcuni valori (composizione
   pacchetto, prezzo applicato) sono *legittimamente* parte dell'aggregato e non "copie" — vanno
   modellati come righe figlie della prenotazione (`booking_lines`), non come snapshot di un listino.
   Il listino serve solo a *proporre* i valori in fase di creazione.

5. **Documento di un sistema esterno di cui non possediamo lo schema** — es.
   `switchboard_wa_template.components` (definizione template WhatsApp, formato Meta Cloud API),
   `fiskaly_receipt.fiskaly_response` (payload SIGN IT), i vari `raw_payload` di webhook
   Telnyx/Meta/Stripe.
   Qui **non** vale il punto 2 ("spezzare in colonne tipizzate"): la forma è definita e versionata da
   un terzo, evolve senza preavviso, e da noi è **passthrough puro** (creato altrove, salvato e
   rimostrato verbatim, mai interrogato per campo). Va tenuto come **`jsonb` così com'è**, snapshot
   della versione remota. Normalizzarlo in tabelle plain trasforma ogni modifica dell'API di terzi in
   una migrazione di schema e richiede comunque un catch-all JSON per le parti non modellate
   (anti-pattern "normalizzare il DTO di un'API di terzi"). Al massimo: **validazione al write**
   contro un insieme chiuso (per i template WhatsApp: i segnaposto ammessi, §2.7) e **poche colonne
   di riepilogo derivate** read-only per le liste di UI (`header_format`, `has_buttons`,
   `param_count`, …) — mai autorevoli. La fonte di verità resta il sistema esterno. Vale solo per
   documenti che teniamo davvero (template, ricevute fiscali): i `raw_payload` di puro log
   diagnostico non vanno migrati affatto (vedi schede `switchboard_call_log` / `switchboard_wa_msg_log`).

**Regola pratica per la riscrittura**: i campi che oggi sono snapshot JSON in `bookings` /
`service_reservation` vanno spezzati in (a) FK vive verso le sorgenti e (b) poche colonne di snapshot
tipizzate sui soli fatti storici (righe pagamento/ricevuta). Vedi §1 (link/URL) e §4 per la
serializzazione dei prezzi.

**Convenzione di nome del suffisso `_snapshot`** (coerente con §3.1). Non tutte le colonne "congelate"
lo portano — solo quelle in cui il nome deve avvisare che *quel valore non seguirà la sorgente*:

| Tipo di campo congelato | Suffisso `_snapshot`? | Esempi |
|---|---|---|
| **Etichetta / nome descrittivo** di un'altra entità, tenuto **accanto alla FK viva** verso quella entità | **Sì** | `spot_label_snapshot` (+ FK `resource_id`), `sector_label_snapshot` (+ FK `sector_id`), `package_name_snapshot` (+ FK `price_list_id`), `customer_full_name_snapshot` (+ FK `customer_id`) |
| **Numero / enum che è dato proprio del documento** (prezzo di riga, aliquota IVA, metodo di calcolo, valore/tipo dello sconto), bloccato alla conferma perché parte del fatto storico (§3.6 punti 2 e 4) | **No** — la natura "congelata" sta nella descrizione del campo | `line_price`, `unit_price`, `vat_rate`, `slot_price`, `price_calculation`, discount `value`/`type`/`scope` |
| **Blob passthrough di un sistema esterno** (§3.6 punto 5) | No — è `jsonb` grezzo, non un "snapshot di un nostro campo" | `fiskaly_response`, `raw_payload` |

Prima applicazione sistematica: le schede `analisi-post/db/booking/*`.

### 3.7 Valori numerici di prezzo verso il frontend

Il legacy è incoerente: `booking_payments.total_amount` è `float`, `online_transaction_details.*` sono
`varchar`, i JSON contengono numeri e stringhe (`"appliedPrice":"110"` vs `"subTotal":116`), e il
frontend fa `parseInt`/`parseFloat`/`_checkIsNumberFloat` sparsi con rischio di errori di
arrotondamento.

**A DB**: sempre `numeric(12,2)` (o `numeric(12,4)` dove servono più decimali per calcoli intermedi).
Mai `float`/`real`/`double`.

**Nelle API**: due opzioni valide, purché **una sola, coerente su tutta l'API**:

- **Numero JSON** con 2 decimali fissi, unità = euro (`"amount": 12.50`). Semplice; il rischio
  floating-point è trascurabile per importi < ~10^13 con 2 decimali, se il **backend** fa tutti i
  calcoli autoritativi e il frontend non ricalcola nulla di vincolante.
- **Interi in centesimi** (`"amount_cents": 1250`) — convenzione Stripe. Elimina del tutto il
  floating-point sul filo; il frontend divide per 100 solo per la visualizzazione.

**Cos'è "numero JSON a 2 decimali"** — è un **valore numerico** JSON (token `number`, senza
virgolette) che il backend serializza sempre con 2 cifre decimali. Esempio di risposta di un
preventivo prenotazione:

```json
{
  "bookingNumber": "10795-0000012345",
  "currency": "EUR",
  "lines": [
    { "label": "Ombrellone + 2 lettini (A12)", "quantity": 1, "unitPrice": 116.00, "lineTotal": 116.00 },
    { "label": "Lettino aggiuntivo",            "quantity": 2, "unitPrice": 12.00,  "lineTotal": 24.00  },
    { "label": "Caffè",                         "quantity": 3, "unitPrice": 1.20,   "lineTotal": 3.60   }
  ],
  "subtotal": 143.60,
  "discount": -14.36,
  "surcharge": 1.00,
  "taxableAmount": 106.75,
  "vatAmount": 23.49,
  "total": 130.24,
  "paid": 30.00,
  "balanceDue": 100.24
}
```

- `116.00` **non** `"116.00"` (stringa) e **non** `116` "nudo": è un numero, serializzato con 2
  decimali fissi. In .NET: proprietà `decimal` + `JsonSerializerOptions` con un converter che forza
  `F2` (o `[JsonConverter(typeof(MoneyJsonConverter))]` sul tipo `Money`); mai `double`/`float`.
- Il client legge `body.total` come numero e lo formatta per la valuta locale — non fa parsing di
  stringhe.
- La somma nell'esempio (`143.60 = 116.00 + 24.00 + 3.60`) è calcolata **dal backend**: il client non
  deve rifarla per confermare (vedi sotto).

**Scelta consigliata**: interi in centesimi per gli importi che transitano verso i sistemi di
pagamento (già così in Stripe), numero `decimal`/`numeric` a 2 decimali serializzato come numero JSON
per il resto — documentato in un unico punto del contratto API. **Mai stringhe** per i valori
numerici.

**Chi fa i calcoli** (es. "aggiungo un servizio alla prenotazione, quanto è il nuovo totale?"):

- il **totale autoritativo** (quello che si salva, si incassa, si mette in ricevuta) lo calcola
  **sempre il backend**, con un endpoint di "quote/preventivo" (`POST /bookings/quote`) che riceve la
  composizione (postazioni, pacchetti, servizi, sconti) e restituisce il breakdown completo
  (imponibili, IVA, sconti, totale). È lo stesso motore che poi usa la conferma, così non c'è
  divergenza;
- il frontend **può** fare una stima ottimistica locale per feedback immediato (somma
  `prezzo × quantità` mentre l'utente clicca), ma è puramente indicativa e va **riconciliata** con la
  risposta del backend prima di poter confermare/pagare. Non deve reimplementare sconti, supplementi,
  arrotondamenti, IVA: quelli stanno solo nel backend.

Questo evita sia il round-trip ad ogni click sia il rischio (presente nel legacy: `BookingCalcService`
lato client con la sua logica di sconti/commissioni) che frontend e backend calcolino un totale
diverso.

### 3.8 Consumi di magazzino / disponibilità: modello dati

Situazione legacy: **sei** tabelle sovrapposte per "qualcosa è occupato in una certa data" —
`booking_items_counts_details` (articoli pacchetto), `additional_items_counts_details` (articoli
aggiuntivi per date puntuali), `services_booking_counts` (servizi bar/normali),
`hourly_service_booking_management` (slot orari), `location_reservations` (blocco morbido postazione),
`booking_areas` (posti ad area, di fatto morta) — più la legacy `booking_items_counts`. Ognuna con
schema diverso, `status` che riusa `payment_status`, nomi articolo come stringa libera, update come
`destroy`+`recreate` non transazionale.

A queste si aggiungono, **sul lato opposto** (una postazione che *torna* disponibile in certi giorni),
`booking_absence_details` (assenza pianificata) e `sharing_details` (rivendita "sharing" con crediti al
titolare) — vedi [`analisi-pre/db/sharing_details.md`](../analisi-pre/db/sharing_details.md): stesso
concetto "(postazione, giorno) → disponibile/occupato" modellato una terza e quarta volta, con
`sharing_details.is_sharing_coming_from_absence` a fare da ponte fragile tra le due. Nel modello target
diventano righe di `resource_consumptions` con `source_type = manual_reservation` / `state` dedicato
(più un piccolo ledger crediti separato per la parte "sharing"), così la disponibilità si legge da
**una sola** tabella.

**Proposta.**

*Dettaglio (una tabella).* Un'unica tabella di **consumo** append-friendly:

```
resource_consumptions
- id
- property_id           FK
- resource_type         enum: umbrella | sunbed | cabin | deckchair | chair | tent | parking
                              | bar_service | normal_service | hourly_service | area_seat | location
- resource_id           FK verso l'anagrafica articoli/servizi/postazioni del modulo Catalogo
                        (NON una stringa: risolve i confronti fragili e i refusi tipo "Restuarant")
- consumption_date      date            -- il giorno occupato (una riga per giorno, sempre)
- slot_start / slot_end time NULL       -- valorizzati solo per hourly_service
- quantity              int
- source_type           enum: booking | service_reservation | manual_reservation
- source_id             FK (booking_id o service_reservation_id o …)
- booking_number        -- copiato per query dirette (coerente con la scelta "chiave = booking_number")
- state                 enum: held | confirmed | released   -- NON riusa payment_status
- created_at / updated_at
```

Questo **accorpa** `booking_items_counts_details` + `additional_items_counts_details` (stesso concetto,
si differenziavano solo per `booking_date` vs `selected_date`), **e** `services_booking_counts` e
**hourly_service_booking_management** (aggiungendo `slot_*`), **e** la parte "conteggio" di
`booking_areas`. `location_reservations` è **diverso** (blocco morbido senza cliente/pagamento):
può rientrare come `source_type = manual_reservation` + `state = held`, oppure restare separata se si
vuole tenerne distinta la UI — ma la logica di disponibilità legge **una sola** tabella.

*Totali per (resource_type|resource_id, date, property_id).* **Sì, una seconda tabella di aggregati**
`resource_availability_daily` (o una vista materializzata) con `booked_qty` e `total_qty` (da
`warehouses`) e `available_qty` generata. Va tenuta in sincrono **dal DB**, non dall'applicazione:
- vista materializzata con refresh mirato, **oppure**
- trigger `AFTER INSERT/UPDATE/DELETE` su `resource_consumptions` che fa `UPSERT` sull'aggregato con
  `booked_qty = booked_qty ± NEW.quantity` (atomico, niente race).

Manutenibilità: **migliora** (un solo posto dove sta la logica di conteggio, un solo schema, nomi via
FK). Prestazioni: **migliorano** in lettura (la disponibilità è una `SELECT` puntuale sull'aggregato
invece di `GROUP BY` su centinaia di righe di dettaglio) al costo di una scrittura in più per consumo,
assorbita dal trigger. Il rischio è la **coerenza** dettaglio↔aggregato: mitigato tenendolo lato DB e
con un job di riconciliazione periodico che ricalcola e confronta.

*Redis.* Utile **solo come cache di lettura**, non come sorgente di verità: flusso
`INSERT consumo (DB) → trigger aggiorna aggregato (DB) → invalidazione/aggiornamento chiave Redis`
`property:{id}:availability:{date}`. La prenotazione **deve** comunque validare la disponibilità in
transazione contro il DB (`SELECT … FOR UPDATE` sull'aggregato o vincolo di esclusione, vedi §3.9),
altrimenti sotto carico due richieste passano entrambe il check su Redis. Redis serve a mostrare la
mappa spiaggia veloce a molti utenti, non a decidere chi vince la postazione.

### 3.9 Blocco temporaneo della postazione durante il checkout

Stato legacy: **nessun blocco reale**. `hold_locations` ha modello + endpoint CRUD ma **zero
chiamanti** e nessuna enforcement; `common.helper.checkLocationReservation` (che leggerebbe
`location_reservations`) **non è mai invocata**; `isLocationAvailable` fa una `SELECT` e poi il codice
crea la prenotazione **senza lock/transazione** → due richieste concorrenti sulla stessa
postazione/data/periodo passano entrambe. L'unica cosa simile a un lock è `bookings.status = 3`,
impostato quando `payment_status == "2"` per "evitare doppie prenotazioni", ma è la riga di
prenotazione stessa, non un hold con scadenza.

**Proposta**:
- un vero `resource_consumptions` con `state = held` e `held_until timestamptz` creato all'ingresso
  nel checkout/pagamento; un job (o un `WHERE held_until > now()` in fase di verifica) lo fa scadere;
  alla conferma/pagamento passa a `confirmed`, all'abbandono/timeout a `released`;
- la verifica di disponibilità e la creazione della prenotazione in **un'unica transazione** con
  `SELECT … FOR UPDATE` sull'aggregato giornaliero, oppure — meglio — un **constraint di esclusione**
  PostgreSQL sul dettaglio:
  ```sql
  ALTER TABLE resource_consumptions
    ADD CONSTRAINT no_overlap EXCLUDE USING gist (
      property_id WITH =, resource_id WITH =, daterange(consumption_date, consumption_date, '[]') WITH &&
    ) WHERE (state <> 'released');
  ```
  così è il DB a garantire che non esistano due consumi attivi sulla stessa risorsa/giorno, anche
  sotto concorrenza.

### 3.10 Enum: `ENUM` nativo PostgreSQL, mapping C#, migration, come vederli in pgAdmin

Tre modi di persistere un enum di dominio; per il progetto si sceglie **A (ENUM nativo)** per i
vocabolari stabili, **B (`varchar`+`CHECK`)** come fallback, **C (smallint)** da evitare.

#### A. `ENUM` nativo PostgreSQL (consigliato per i vocabolari stabili)

**Che tipo è in C#.** È un **`enum` C# normale**; Npgsql fa il mapping label PG ↔ membro C# **per
nome** (con conversione di naming configurabile, tipicamente `snake_case`). L'entità espone la
proprietà tipizzata, non `int`/`string`:

```csharp
// Modules/Booking/Domain/Enums/BookingStatus.cs
public enum BookingStatus { Pending, Confirmed, CheckedIn, CheckedOut, Cancelled }

public class Booking
{
    public BookingStatus Status { get; set; }   // <-- tipizzata, non int né string
}
```

**Come si dichiara e come finisce nel DB — via migration .NET, non SQL a mano.**

1. Registrazione del tipo nel modello EF (una riga in `OnModelCreating` del `DbContext` del modulo):
   ```csharp
   modelBuilder.HasPostgresEnum<BookingStatus>("booking", "booking_status");
   // schema "booking", tipo "booking_status" con label: pending, confirmed, checked_in, ...
   ```
2. Registrazione lato provider Npgsql (perché il driver sappia serializzare l'enum):
   ```csharp
   // AddDbContext / NpgsqlDataSourceBuilder
   dataSourceBuilder.MapEnum<BookingStatus>("booking.booking_status");
   // (Npgsql 8+; con EF9 spesso basta HasPostgresEnum + EnableDynamicJson/MapEnum sul data source)
   ```
3. `dotnet ef migrations add AddBookingStatusEnum` → la migration generata contiene
   `migrationBuilder.AlterDatabase().Annotation("Npgsql:Enum:booking.booking_status", "pending,confirmed,...")`,
   che a `dotnet ef database update` esegue **`CREATE TYPE booking.booking_status AS ENUM ('pending', 'confirmed', ...)`**.
   Aggiungere un valore in futuro: si aggiunge il membro all'`enum` C#, `migrations add`, ed EF emette
   **`ALTER TYPE booking.booking_status ADD VALUE 'no_show'`**.

   → **gli enum entrano in DB tramite la migration .NET.** Non si scrive `CREATE TYPE` a mano (lo si
   *può* fare con `migrationBuilder.Sql(...)`, ma non è il flusso previsto).

**Limiti da conoscere** (motivo per cui i vocabolari che devono *crescere da pannello* restano tabelle
di lookup, §2.2.B):
- `ALTER TYPE ... ADD VALUE` **non è transazionale** prima di PG 12 e comunque non può girare dentro
  una transazione insieme ad altro DDL/DML → EF marca quella migration come non-transazionale; tenerla
  isolata (solo l'aggiunta del valore).
- **rinominare** un label = `ALTER TYPE ... RENAME VALUE`; **rimuovere** un label non è supportato da
  Postgres (serve ricreare il tipo). Quindi gli `ENUM` nativi vanno bene per vocabolari
  *append-mostly*, non per liste che cambiano spesso.

**Come si vede in pgAdmin.** Il tipo enum **non è sotto "Tables"**: sta in
`Server > Databases > <db> > Schemas > <schema> > Types`. Ogni enum compare come **Type** di categoria
*Enumeration*; tasto destro → *Properties* → tab *Definition* elenca i label; il pannello *SQL* mostra
`CREATE TYPE ... AS ENUM (...)`. Nelle celle di una query/tabella il valore è **già mostrato come
stringa** del label (es. `confirmed`), senza join.
Da `psql`: `\dT+ booking.booking_status`. Da query:
```sql
SELECT enum_range(NULL::booking.booking_status);              -- tutti i label
SELECT n.nspname AS schema, t.typname AS name
FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE t.typtype = 'e';                                        -- elenco di tutti gli enum
```
Il tipo si chiama, in gergo Postgres, **"enumerated type"** (`pg_type.typtype = 'e'`); nell'albero
pgAdmin è la sezione **"Types"**.

**Creazione manuale in pgAdmin** — solo per spike/verifica; sullo schema di progetto il tipo lo crea
la migration EF (sopra):
1. `Databases ▸ <db> ▸ Schemas ▸ <schema>` → tasto destro su **Types** → **Create ▸ Type…**
2. tab **General**: `Name` = `booking_status`, `Schema` = quello del modulo;
3. tab **Definition**: campo **Type** → **Enumeration**; nella griglia **Enum values** premere **+** e
   inserire un label per riga nell'ordine voluto (`pending`, `confirmed`, `checked_in`,
   `checked_out`, `cancelled`);
4. **Save**. Il tab **SQL** del dialog mostra `CREATE TYPE … AS ENUM (…)`.
5. Aggiungere un valore dopo: tasto destro sul tipo → **Properties ▸ Definition**, **+** un label
   (campi *Before*/*After* per la posizione) → **Save** (esegue `ALTER TYPE … ADD VALUE`). Rimuovere o
   riordinare label esistenti non è possibile da GUI (limite Postgres).

**Usare il tipo in una tabella creata a mano in pgAdmin**:
1. `Schemas ▸ <schema> ▸ Tables` → tasto destro → **Create ▸ Table…**; tab **General**: nome tabella.
2. tab **Columns** → **+** per ogni colonna. Per la colonna di stato: nel campo **Data type** digitare
   `booking_status` e sceglierlo dalla lista (pgAdmin elenca i tipi definiti dall'utente nello schema
   in `search_path`; se è in un altro schema qualificarlo `modulo.booking_status`). Impostare
   **Not NULL** = *Yes* e, se serve, **Default** = `'pending'` (con apici).
3. tab **Constraints ▸ Primary Key** → **+** → colonna `id`; **Unique** → `booking_number`; FK/Check
   se servono.
4. **Save**. Il tab **SQL** mostra il `CREATE TABLE` con `status booking_status NOT NULL DEFAULT
   'pending'`.
5. Vincolo di unicità solo tra righe vive: `Table ▸ Indexes ▸ Create ▸ Index`, colonne
   `booking_number`, e nel tab **Definition** il campo **Constraint (WHERE)** = `deleted_at IS NULL`.

Equivalente SQL (Query Tool), più rapido:
```sql
CREATE TYPE booking.booking_status AS ENUM
    ('pending','confirmed','checked_in','checked_out','cancelled');

CREATE TABLE booking.bookings (
    id             bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    booking_number varchar(40)           NOT NULL,
    status         booking.booking_status NOT NULL DEFAULT 'pending',
    created_at     timestamptz           NOT NULL DEFAULT now(),
    updated_at     timestamptz           NOT NULL DEFAULT now(),
    deleted_at     timestamptz           NULL,
    CONSTRAINT uq_bookings_number UNIQUE (booking_number)
);
CREATE UNIQUE INDEX uq_bookings_number_alive
    ON booking.bookings (booking_number) WHERE deleted_at IS NULL;
```

#### B. `varchar` + `CHECK` (fallback, o quando serve massima portabilità)

`HasConversion<string>()` → colonna `text`/`varchar` con `CHECK (status IN ('pending','confirmed',...))`.
In C# resta un `enum`; il valore è leggibile in ogni client; nessun oggetto-tipo da gestire in
migration a parte il `CHECK`. Svantaggio: nessun controllo di dominio a livello di *tipo* (solo il
`CHECK`), niente `enum_range`, `ADD VALUE` diventa "modifica del `CHECK`".

#### C. `smallint` (`HasConversion<int>()`) — **da evitare**

Il client mostra il numero. Se imposto, servono viste di comodo o una funzione
`enum_label(enum_name, value)` che legge una lookup — complessità inutile: preferire A o B.

**In tutti i casi**: **una sola fonte** delle etichette. Con `ENUM` nativo è il tipo stesso (+ le
risorse di localizzazione del modulo per le label mostrate all'utente, §2.2.A); evitare di duplicare
la lista in una tabella *e* in codice.

### 3.11 Indici

Nel legacy gli indici sono **quasi assenti**: le tabelle hanno la sola PK e pochi indici impliciti;
diverse colonne usate come filtro caldo o come FK non sono indicizzate affatto.
[01-criticita-e-indicazioni-backend.md §6](01-criticita-e-indicazioni-backend.md) documenta la mancanza
di indici su `stripe_transaction_id` / `booking_id` / `reservation_id` in `onlineTaransactionDetails`
(letti a ogni webhook/refund) e su `(user_id, property_id, isDeleted)` per il saldo del borsellino
prepagato, con degrado a full scan sotto carico stagionale. Il nuovo schema **definisce gli indici in
modo esplicito e deliberato**, come parte del modello EF Core e delle migration — non come intervento
successivo su una tabella già in produzione.

Questa è la sezione di indirizzo: l'elenco puntuale degli indici per tabella si costruisce insieme allo
schema di ogni modulo e alle query reali, e si rivede con le metriche di produzione ("Cosa NON è un
indice buono a priori", in fondo).

#### Cosa va sempre indicizzato

1. **Ogni colonna FK.** PostgreSQL crea un indice automatico **solo sul lato referenziato** (la PK
   della tabella target), **mai** sul lato referenziante: ogni colonna `<tabella>_id` ha un indice
   esplicito, altrimenti ogni JOIN e ogni verifica di integrità in cancellazione fa sequential scan
   (è la causa diretta dei full scan citati sopra).
2. **`property_id` su ogni tabella multi-tenant.** Quasi ogni query dell'applicazione filtra per
   stabilimento (§3.1bis, [refactoring-analisi-A-vs-B.md §6.5](pre/refactoring-analisi-A-vs-B.md)):
   `property_id` è tipicamente la **colonna di testa** degli indici composti, non un indice a sé.
3. **Chiavi naturali / di business → indice `UNIQUE`.** `bookings.booking_number`, il `code` delle
   tabelle di lookup (§2.2.B), ecc. Dove l'unicità deve valere solo tra le righe vive, **indice unico
   parziale** `WHERE deleted_at IS NULL` (già in §3.5 e nell'esempio di §3.10:
   `uq_bookings_number_alive`).
4. **Colonne dei percorsi di lettura caldi** in `WHERE` / `ORDER BY` / `JOIN`: disponibilità per
   `(property_id, resource_id, consumption_date)` e `(property_id, consumption_date, state)` su
   `resource_consumptions` (§3.8), prenotazioni per `(property_id, booking_start_date)`, transazioni
   per `(booking_id)` e per lo stato/gateway letto a ogni webhook (§4,
   [01-criticita-e-indicazioni-backend.md §6](01-criticita-e-indicazioni-backend.md)).
5. **Colonne di supporto alle retention.** I batch di [80-retention.md](80-retention.md) selezionano
   per `deleted_at` e per data di business oltre la finestra: gli indici parziali/compositi che
   servono a quelle scansioni vanno previsti insieme alla policy, non aggiunti quando il job va in
   timeout.

#### Indici composti

- Ordine delle colonne: **prima le uguaglianze, poi il range** — `(property_id, booking_start_date)`
  serve sia il filtro tenant per uguaglianza sia la scansione per intervallo di date; l'ordine inverso
  no.
- **Non** creare un indice a colonna singola se quella colonna è già **prefisso** di un composto
  (`(property_id, booking_start_date)` copre anche le query sul solo `property_id`).
- `INCLUDE` (covering index) solo per le poche query calde, misurate, in cui evita l'accesso alla
  heap — non di default.

#### Indici parziali

Con il filtro globale di soft-delete (§3.5) **ogni** query sulle tabelle di dominio porta con sé
`deleted_at IS NULL`: gli indici su queste tabelle sono di norma **parziali**
`WHERE deleted_at IS NULL` (più piccoli, più efficienti, non appesantiti dalle righe cancellate).
Stesso criterio per gli stati "quasi sempre esclusi" (es. `WHERE state <> 'released'` su
`resource_consumptions`, §3.9 — dove per la concorrenza si usa comunque il constraint di esclusione
GiST).

#### JSONB

Le poche colonne `jsonb` che sopravvivono sono **passthrough di documenti di terzi** non interrogati
per campo (§3.6 punto 5) e non richiedono indici. Se emergesse la necessità di filtrare **dentro** un
JSON, è un **sintomo di modellazione da correggere** (§2.5), non un caso per un indice GIN: il modello
relazionale nuovo esiste proprio per togliere il tetto "JSON non indicizzabile" del legacy
([refactoring-analisi-A-vs-B.md §6.5](pre/refactoring-analisi-A-vs-B.md)). Un GIN si valuta solo per un
`jsonb` che si è **scelto** di tenere come tale e che va effettivamente cercato.

#### Dove vivono gli indici

Dichiarati nel modello EF Core (`HasIndex(...)` negli `IEntityTypeConfiguration` del modulo, coerente
con [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)) e materializzati
dalle migration. **Mai** aggiunti a mano solo in produzione: un indice che non è nella migration non
esiste negli altri ambienti. Per indici parziali o con espressioni che EF non esprime nativamente,
`migrationBuilder.Sql(...)` dentro una migration versionata.

#### Cosa NON è un indice buono a priori

Ogni indice **rallenta le scritture** e occupa spazio: si indicizza per i percorsi di accesso **noti o
misurati**, non "per ogni evenienza".

- Sulle tabelle ad alta scrittura (`resource_consumptions`, outbox notifiche di C6, tabelle di log)
  tenere il set di indici **minimo**.
- In fase di tuning: `EXPLAIN (ANALYZE, BUFFERS)` sulle query critiche; `pg_stat_user_indexes` per
  individuare e **rimuovere** gli indici mai usati.
- La misura "tempi nelle letture dati" richiesta in [note.md](note.md) (sezione "Stima") e il KPI
  "tempi query sulle viste critiche" di
  [refactoring-analisi-A-vs-B.md §8](pre/refactoring-analisi-A-vs-B.md) si fanno **con gli indici del
  nuovo schema in essere**: un confronto col legacy su uno schema non ancora indicizzato non è
  significativo.

---

## 4. Collocazione delle tabelle di pagamento/incasso tra i domini (C1 vs C3 vs C4 vs C4bis vs C6)

> **Nota sulla nomenclatura**: questa sezione è stata scritta con l'etichetta unica
> "C3 (Pagamenti & Fiscale)". Con la separazione ufficiale (vedi §2.4ter e
> [`docs/pre/architettura-evolutiva-microservizi.md` §2.2](pre/architettura-evolutiva-microservizi.md)):
> **C3 = `Payment`** (Stripe, incassi, transazioni, rimborsi, borsellino, webhook, revenue,
> **integrazioni POS** OfSale/TeamSystem); **C4 = `Fiskaly`** (scontrino fiscale **elettronico** via
> API SIGN IT); **C4bis = "Stampa fiscale su hardware RT"** (meccanismo legacy alternativo, adapter
> Epson/RCH lato client; 3 tabelle), **si mantiene come componente separato** — decisione di business
> del committente, indipendente dall'uso attuale. Nella
> tabella sotto: `fiskaly_*` → **C4**; `booking_receipts` / `receipt_serial_numbers` /
> `printer_configs` → **C4bis**; le voci "righe fiscali" di
> `booking_number_informations` → **C4** (documento) con il *movimento* di incasso in **C3**;
> `teamsystem_expenses` → **C3** (spesa POS, non documento fiscale — la fiscalizzazione è solo
> consultata su C4); `keysecret` → **INFRA** (serve integrazioni C3). Schede in
> [`analisi-pre/db/`](../analisi-pre/db/); tutto il resto ("come si è incassato") → **C3**.

> Contesto: nella riscrittura l'aggregato **Prenotazione** sarà spezzato in una tabella *testata*
> (`bookings`, chiave `booking_number`) e una tabella *righe* (`booking_lines`, chiave `booking_id` →
> testata), per non duplicare le info comuni — esempio completo in
> [`analisi-pre/db/_feature-tabelle.md` → "Struttura target C1"](../analisi-pre/db/_feature-tabelle.md).
> Le tabelle qui sotto vanno collocate di conseguenza.

Principio: **in C1 (Prenotazioni) resta solo ciò che descrive la prenotazione e il suo stato di
incasso "riassuntivo"**; il **come** si è incassato (transazioni, gateway, ricevute, rimborsi,
riconciliazione contabile) è **C3 (Pagamenti & Fiscale)**; i **canali di invio** (email/WhatsApp del
link o del report) sono **C6 (Comunicazione)** — ma solo per la parte "messaggio", non per il dato di
pagamento.

| Tabella legacy | Dominio | Motivazione |
|---|---|---|
| `booking_payments` | **C1** (testata `booking_number`) | è il *riepilogo economico della prenotazione* (totale, pagato, residuo, modalità prevalente) — dato di stato della prenotazione, non un movimento. In C1 come parte della testata. I singoli movimenti che oggi vivono nel JSON `booking_payments.paidAmountData`/`depositList` vanno estratti in **C3** (`payment_transactions`). |
| `service_reservation_payments` | **C1** (testata) | idem, per la prenotazione di soli servizi. Stessa struttura di `booking_payments`: in riscrittura è la **stessa** tabella testata, con `source_type = service_reservation`. |
| `booking_number_informations` | **da spezzare** | contenitore multi-sorgente. `payments_new`/`payments` (riepiloghi) → C1 testata; `details_payments*` (righe fiscali) → **C3**; `ofsale_payments` → **C3** (integrazione OfSale) con eventuale metadato di canale; `teamsystem_payment` → **C3** (integrazione POS TeamSystem); `quick_actions_data` → **C1** (è UI/planner della prenotazione). La tabella così com'è non sopravvive. |
| `transactions_payments` | **C3** | è già un registro contabile normalizzato per riga/movimento con imponibile+IVA: è il prototipo della tabella `payment_transactions` di C3. Collegata a C1 solo via `booking_id`/`service_reservation_id`. |
| `online_transaction_details` | **C3** | dettaglio delle transazioni Stripe (PaymentIntent, hold, application fee, stato gateway). Puro dominio pagamenti. Verso C1 solo la FK. |
| `refund_transaction_details` | **C3** | movimento di rimborso Stripe. C3. |
| `bookings_send_payment_summaries` | **C3 + C6** | il *fatto* "è stato richiesto un pagamento con link Stripe, importo X, scadenza Y, esito Z" è **C3** (`payment_requests`/`payment_links`); il *fatto* "il link è stato inviato via email/WhatsApp" (`method_flag`, `email_sent`) è **C6** (log di invio messaggio). Da spezzare in due. |
| `booking_receipts` | **C4bis** (Stampa fiscale su hardware RT) | log di stampa scontrino/comanda su stampante RT fisica (adapter Epson/RCH lato client), accanto a `printer_configs` e `receipt_serial_numbers`. **Non** è C3 né il modulo `Fiskaly` (C4). Non lega quasi mai a una prenotazione (`bookingId` per lo più `NULL`). Si mantiene come componente separato (decisione di business, indipendente dall'uso attuale). |
| `card_transactions` | **C3** (con eco in C1) | movimenti del borsellino/tessera prepagata del cliente: sono transazioni di pagamento → **C3** (`customer_wallet_transactions`). In C1 resta solo, se serve, il riferimento "questa consumazione della prenotazione è stata pagata con carta X" (già rappresentato da `bookings.summaryServicesData`, da normalizzare). |
| `hotel_payments` | **C1 (righe `booking_id`) + C3** | la riga "questa postazione/prenotazione ha consumato *questa* voce di listino/voucher hotel" è un **dettaglio della prenotazione** → C1 tabella righe (`booking_id`). L'eventuale importo/incasso e lo stato pagamento (`amount`, `payment_status`, `payment_method`) sono **C3**. Lo snapshot listino (`mainPrices`/`detailPrices`/`priceCalculation`) è un valore bloccato dell'aggregato → C1 righe (vedi §3.6 punto 4). |
| `hotel_reports` | **Convenzioni/Hotel (C2) + C6** | il report periodico è un artefatto della gestione convenzioni (C2); l'invio del PDF alla struttura via email/WhatsApp con i template di `hotel_message_templates` è **C6**. Non C1. |
| `online_reservations` | **C2** | vedi §5: è configurazione del canale di prenotazione online per stabilimento, non un dato transazionale. |

Campi che ricadono in **C6 (Comunicazione)** anche se oggi stanno in tabelle "di prenotazione/pagamento":
`bookings_send_payment_summaries.method_flag` / `email_sent` (canale ed esito invio del link);
`hotel_reports.path` + logica di invio (PDF spedito alla struttura); i vari `email_logs` già
referenziati da `card_transactions.sendCardDetails`. In riscrittura questi diventano righe di un log
di **notifiche/messaggi** (C6) con FK all'entità di origine, non colonne sull'entità stessa.

---

## 5. `online_reservations`: collocazione e possibile accorpamento in `properties`

È **configurazione (C2)**, non C1: descrive *se e come* uno stabilimento accetta prenotazioni online
(on/off, solo mappa / solo periodo / solo location, metodo di pagamento). Nessun dato transazionale.

Oggi la stessa informazione è **triplicata**: `online_reservations` (1 riga per property),
`properties.onlineReservation` (0/1, scritto dallo stesso endpoint), `properties.onlineBookingOptions`
(JSON). Più le colonne carta in chiaro (`card_number`, `cvc`, …) da **eliminare** (PCI).

**Consigliato**: **accorpare in `properties`** (o in una `property_online_booking_settings` 1:1 con
`properties`, nello schema del modulo Catalogo/Configuration). Un solo posto per: `online_booking_enabled`,
`online_booking_mode` (enum `map` / `period` / `location` / `free`), `allowed_periods`,
`allowed_locations`, riferimento al metodo di pagamento. La doppia negazione legacy (`isDisable` vs
`onlineReservation`) sparisce. Migrazione: consolidare le tre fonti nella nuova struttura, scartare
`period`/`locations` vuoti e le colonne carta.

---

## 6. Campi che appartengono a un dominio diverso da quello della loro tabella

> **Nota.** L'assegnazione di una tabella a un dominio (C1…C7, C4bis, INFRA — vedi
> [`analisi-pre/db/_feature-tabelle.md`](../analisi-pre/db/_feature-tabelle.md) e §3.1bis) vale per la
> **tabella nel suo complesso**, non per ogni suo singolo campo. Diverse tabelle legacy sono "sacchi"
> multi-responsabilità: la riga appartiene a un dominio, ma **alcune colonne (o sotto-campi JSON)
> sono dati di un altro dominio**, finiti lì solo perché il modello legacy non separava le
> responsabilità. Nella riscrittura questi campi **non vanno riprodotti nella tabella di partenza**:
> vanno collocati nell'aggregato/tabella del dominio proprietario, e la tabella di partenza vi si
> collega — quando serve — solo per **id copiato in evento/proiezione**, mai con FK cross-modulo
> (regola §5 di [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)).
>
> **Pattern ricorrenti** emersi dalla revisione campo per campo:
> - **flag "inviato / canale"** (`email_sent`, `emailSent`, `whatsappSent`, `method_flag`) su tabelle
>   di prenotazione / pagamento / fiscale → il *fatto di invio* è **C6** (riga dell'outbox notifiche);
>   sulla tabella di origine non resta nulla — l'esito si legge dal log C6 tramite l'id dell'entità.
> - **singoli movimenti di incasso** annidati nei JSON di riepilogo (`paidAmountData`,
>   `addPaidAmountData`, `depositList`, `getCustExpenseData`, …) → **C3** (`payment_transactions`);
>   nella testata prenotazione (C1) resta solo il *riepilogo* (totale / pagato / residuo / modalità
>   prevalente).
> - **credenziali / segreti** (password SMTP, password SIP, API key e password Fisconline di Fiskaly)
>   → **INFRA / secret manager**, mai colonna a DB (vedi §1 per il pattern "config, non DB").
> - **copie JSON dell'anagrafica cliente** (`booking_consumer`, `customerData`) → **C5**: FK a
>   `customers` + eventuale snapshot tipizzato sui soli fatti fiscali (§3.6).
> - **stato / integrazione fiscale sullo stabilimento** (`fiskaly*` su `properties`) → **C4**.
>
> **Contro-esempio** (campo che *resta* dov'è pur sembrando di un altro dominio):
> `properties.contractDocumentsId` — le tabelle `contract_documents` / `contract_acceptation` sono
> **C5** (sottoarea `account`), ma il campo che *attiva* la feature per lo stabilimento è uno stato
> della property e **resta in C2**. Il criterio è *chi possiede il dato e ne governa il ciclo di
> vita*, non *chi lo consuma*: `notification_details` (C6) che referenzia `bookings.id`,
> `customers.isBlackList` (C5) letto dal flusso di prenotazione, `customers.vat`/`fiscalCode`
> fotografati nello scontrino (C4) **non** sono campi cross-dominio — sono riferimenti/consumi
> legittimi. Analogamente i campi di abbonamento/licenza di `users`
> (`license`, `startDate`, `endDate`, `subscriptionStatus`, …) restano in **C5**, ma vanno isolati in
> una sottoarea `account` distinta dall'anagrafica dell'utente (guidano `properties.superAdminOnlineFlag`).

La tabella seguente è il risultato della revisione di **tutti i campi di tutte le tabelle**
(`analisi-pre/db/*.md`). Sono elencati **solo** i campi la cui responsabilità è di un dominio diverso
da quello della tabella; ogni campo non elencato resta nel dominio della sua tabella. Molte tabelle
(`customers`, `roles`, `user_roles`, `seasons_periods`, `price_lists`, `discounts`, `warehouses`,
`services`, `services_types`, `country`, `licenses`, i `switchboard_*`, i `marketing_*`, `email_logs`,
`custom_templates`, …) **non compaiono**: tutti i loro campi appartengono al dominio della tabella.

| Tabella | Dominio tabella | Campo | Dominio destinazione campo | Note |
|---|---|---|---|---|
| `bookings` | C1 | `booking_payments` → `paidAmountData[]`, `addPaidAmountData[]`, `depositList[]`, `getCustExpenseData[]` | **C3** | singoli movimenti di incasso / acconto / spesa cliente → `payment_transactions`; in C1 resta solo il riepilogo economico (totale/pagato/residuo/`paymentMode`) |
| `bookings` | C1 | `summaryServicesData` | **C3** | consumazioni bar/servizi pagate col borsellino/tessera prepagata → movimenti wallet; in C1 solo il riferimento "pagato con carta X" |
| `bookings` | C1 | `isMarketingCampaign` | **C6** | attribuzione campagna + iscrizione automatica del cliente alla contact list marketing di default |
| `bookings` | C1 | `booking_consumer`, `guest_data` | **C5** | copia JSON dell'anagrafica cliente/ospiti → FK a `customers`; snapshot tipizzato solo sui fatti fiscali (C4) |
| `bookings` | C1 | `ofsale_payments` | **C3** | colonna morta su `bookings` (duplicato di `booking_number_informations.ofsale_payments`) → non migrare qui; la versione viva è in C3 |
| `service_reservation` | C1 | `payments` → `paidAmountData[]`, `depositList[]` | **C3** | come `bookings.booking_payments`: i movimenti di incasso → `payment_transactions` |
| `service_reservation` | C1 | `customerData` | **C5** | copia JSON dell'anagrafica cliente → FK a `customers` |
| `booking_number_informations` | C1 | `details_payments_new`, `details_payments` | **C4** | righe fiscali (input dello scontrino elettronico) |
| `booking_number_informations` | C1 | `ofsale_payments` | **C3** | addebiti dal POS OfSale |
| `booking_number_informations` | C1 | `teamsystem_payment` | **C3** | spese dal POS TeamSystem / Cassa in Cloud |
| `hotel_payments` | C1 (righe) | `amount`, `assignedPrice` | **C3** | importo / incasso della voce di listino/voucher hotel |
| `hotel_payments` | C1 (righe) | `paymentStatus` | **C3** | stato del pagamento hotel |
| `hotel_payments` | C1 (righe) | `paymentMethod` | **C3** | modalità di pagamento (allineare a `PaymentMode` di `booking_payments`) |
| `sharing_details` | C1 | `email_sent` | **C6** | esito invio dell'email "credito guadagnato" al titolare |
| `sharing_details` | C1 | `number_of_credit`, `credit_value` | **C3** | credito "sharing" maturato = valore spendibile → ledger crediti / borsellino |
| `properties` | C2 | `fiskalyFlag` | **C4** | step di avanzamento nell'attivazione della fiscalità elettronica |
| `properties` | C2 | `fiskalyEnabled` | **C4** | master-switch dell'integrazione Fiskaly per lo stabilimento |
| `properties` | C2 | `fiskalyAutoreceipt` | **C4** | emissione automatica dello scontrino dopo un pagamento Stripe |
| `properties` | C2 | `commissionAmount` (`onlineCommission`, `iframeCommission`) | **C3** | tasso di commissione sugli incassi online (oggi applicato lato client, da portare server-side in C3, con le eccezioni oggi hardcoded nel frontend) |
| `bathouses` | C2 | `isTwoFactorEnabled` | **C5** | interruttore 2FA per **tutto lo staff** dello stabilimento — impostazione di sicurezza dell'account, non della vetrina pubblica |
| `bathouses` | C2 | `likeCount` | **C5** | contatore denormalizzato dei "mi piace" consumer (fonte: `property_like_logs`, sottoarea `customer`) |
| `configurations` | C2 | `sharing_enable`, `sharing_seasonal_workers`, `credit_perday_resold`, `credit_value`, `disable_both_options`, `enable_sharing_on_all_article`, `plan_absence_as_sharing` | **C1** | parametri della feature "sharing / rivendita del posto" (sottoarea rivendita/`hold`) — vivono nella config mappa solo per accidente |
| `online_reservations` | C2 | `cvc`, `cardHodler`, `cardNumber`, `expiry` | **C3** | dati carta di credito — peraltro **da eliminare** (PAN/CVC in chiaro = violazione PCI-DSS): mai a DB |
| `bookings_send_payment_summaries` | C3 | `method_flag` | **C6** | canale di invio del link di pagamento (email / WhatsApp) |
| `bookings_send_payment_summaries` | C3 | `email_sent` | **C6** | esito dell'invio del messaggio con il link |
| `fiskaly_receipt` | C4 | `emailSent`, `whatsappSent` | **C6** | esito dell'invio dello scontrino elettronico al cliente |
| `fiskaly_non_fiscal_receipt` | C4 | `emailSent`, `whatsappSent` | **C6** | esito dell'invio del pre-conto / ricevuta non fiscale |
| `fiskaly_account` | C4 | credenziali API Fiskaly + credenziali Fisconline (AdE) | **INFRA** | segreti → secret manager / Parameter Store, mai colonna a DB |
| `hotel_reports` | C2 (+C6) | `path` | **C6** | URL del PDF del report, valorizzato **all'invio** → artefatto di consegna (il resto della riga — periodo, totali, `searchType`, `status` — è C2) |
| `bathhouse_email_configrations` | C6 | `smtpPassword` | **INFRA** | password SMTP oggi `varchar` in chiaro → secret manager |
| `users` | C5 | `notifyEmail`, `notifyWhatsapp` | **C6** | preferenze di canale per le notifiche allo staff |
| `users` | C5 | `disableNotification` | **C6** | opt-out delle notifiche in-app dell'utente |
| `users` | C5 | `deviceTokens` | **C6** | token dispositivo per le push notification (registrazione del canale push) |
| `users` | C5 | `stripeCustomerId` | **C3** | id cliente Stripe per le carte salvate (oggi contiene per errore anche id `acct_` Connect) |
| `users` | C5 | `total_number_of_credit`, `total_credit_value`, `totalCreditsEarned` | **C3** | saldo dei crediti "sharing" (borsellino / ledger), oggi denormalizzato su `users` da un cron |
| `switchboard_config` | C7 | `sip_password` | **INFRA** | password SIP in chiaro → secret manager (gli id oggetti Telnyx/Meta restano in C7, sono identificatori esterni opachi) |

I campi indicati come "colonna morta" nelle schede (`bookings.mainPhoto`, `bookings.booking_map`,
`bookings.sharing_dates`, `revenue_notifications.discounts`, `notification_details.user_id`,
`bathouses.isTwoFactorAuthentication`, `keysecret.expiresAt`, `user_stripe_card_details.token`,
`fiskaly_receipt.fiskaly_version`, `teamsystem_expenses.fiscalized`, …) **non sono cross-dominio**:
sono semplicemente **da non migrare**.

---

## 7. Read-model JSON di configurazione affiancato alle tabelle normalizzate

### 7.1 Il caso

Il frontend chiede spesso la **configurazione generale** di uno stabilimento in un colpo solo
(bootstrap del gestionale, apertura della scheda pubblica, apertura del flusso di prenotazione).
Ricostruirla ogni volta da ~20 tabelle normalizzate con molte `JOIN` è costoso e ripetitivo, ed è
esattamente ciò che il legacy "risolveva" tenendo tutto in blob JSON (`onlineBookingOptions`,
`beachMapData`, `barServices`, …).

### 7.2 La regola

Per i dati di **configurazione** (che cambiano di rado, per azione esplicita di un gestore/systemadmin, e
che il frontend chiede *wholesale*) si adotta un **doppio livello**:

1. **Tabelle relazionali normalizzate** = **fonte di verità**. Servono per l'editing, l'integrità
   referenziale, le query mirate e i calcoli autoritativi. È lo schema descritto nel resto di questo
   documento.
2. **Un payload `jsonb` derivato** ("snapshot"), **ricostruibile in ogni momento** dalle tabelle
   (`rebuild(scope) = f(tabelle)`, funzione pura), aggiornato **ad ogni modifica** delle tabelle
   sorgente e restituito così com'è quando serve "tutta la configurazione", senza ricalcolo.

È il pattern **materialized read-model / proiezione CQRS** con storage `jsonb`. Lo snapshot **non è mai
l'unica copia** di un dato: in caso di dubbio o divergenza, si rigenera dalle tabelle. Questa è l'**unica
eccezione consentita** all'anti-pattern "niente JSON di dominio" (§2.5): il JSON qui è un *derivato*, non
il modello.

### 7.3 Dove vive

Una **tabella dedicata per modulo** (`<dominio>_config_snapshots`: `scope_id` + `snapshot_kind` enum +
`payload jsonb` + metadati), **non** una colonna `jsonb` sulle tabelle radice (gonfierebbe le tabelle
hot, es. `catalog_properties`, caricate quasi ad ogni richiesta). I read-model **interrogati con
filtri/ordinamenti** (es. la ricerca pubblica) hanno invece una tabella propria con **colonne vere
indicizzate** (`min_price`, `amenity_ids` GIN, `location` GiST, …) + un `jsonb` per il solo payload di
rendering.

Metadati della riga snapshot:

- `payload_schema smallint` — versione della **forma** del JSON (contratto verso il client); cambia solo
  col codice di serializzazione, non coi dati; il client tollera la versione corrente e la precedente; un
  incremento fa scattare un rebuild di massa.
- `source_hash` — hash degli **input**; se invariato, il rebuild salta la riscrittura (niente rebuild
  no-op).
- `content_hash` — hash del **payload**; usato come **`ETag`** HTTP (richieste condizionali
  `If-None-Match` → `304`), precalcolato quindi a costo zero a runtime.
- `stale_since timestamptz NULL` — `NULL` = aggiornato; altrimenti da quando lo snapshot è da
  rigenerare. Rilevante solo per gli snapshot ad aggiornamento asincrono: è una data e non un flag
  perché la soglia utile è **quanto** è vecchia l'invalidazione.

### 7.4 URL nel payload: riferimento scomposto, non URL completo

Gli snapshot **non** contengono URL completi (re-introdurrebbe il prefisso hardcoded che §1 elimina).
Ogni file è un **`assetRef`** `{ "origin": "<codice>", "path": "<percorso relativo>" }` (se
`origin = external`, `path` è l'URL intero). La mappa `origin → base URL` (la stessa `Storage:Origins` di
§1.2) è servita da un endpoint dedicato e cacheabile (`GET /config/media-origins`, con `ETag` proprio); il
client compone. Così un cambio di bucket/CDN è una modifica di configurazione e **non** richiede di
rigenerare gli snapshot, e il `content_hash` (quindi gli `ETag` dei client) resta stabile.

### 7.5 Aggiornamento — modello uniforme

1. **Durante il giorno**: rebuild **sincrono** nella stessa transazione del comando che modifica una
   tabella sorgente (evento di dominio → handler → `rebuild(scope, kind)`). Payload piccoli, pochi
   tenant, poche scritture/giorno → costo di pochi ms, zero finestra di incoerenza, nessuna coda.
2. **Di notte**: job di **ricostruzione totale** (rete di sicurezza per eventi persi, bug di mappatura
   evento→kind, modifiche fatte fuori dai comandi standard — fix diretti a DB, import).
3. **Manuale**: utility CLI + endpoint admin (`snapshots rebuild --scope … [--kind …] | --all`,
   `snapshots verify` = rigenera in memoria e confronta `content_hash` senza scrivere).

**Escape hatch** (solo se una misura lo richiede): un singolo `kind` costoso passa ad **asincrono**
(evento → coda → job idempotente, `stale_since` valorizzata nel frattempo), senza toccare gli altri.

### 7.6 Cosa NON mettere negli snapshot

Dati **transazionali / che variano per data o per prenotazione**: occupazione/disponibilità (postazioni
libere, slot residui, magazzino consumato — vedi §3.8, dominio C1), prezzo suggerito non ancora
"applicato", dati per-utente (preferiti, carrello), segreti/credenziali, il totale autoritativo di una
prenotazione (sempre da un endpoint `quote` server-side, §3.7).

### 7.7 Applicazione a C2 (prima realizzazione)

Nome e forma canonica della tabella (`<dominio>_config_snapshots`) e della tabella dedicata per i
read-model interrogati con filtri sono fissati in **§3.1quater.B**.

Dominio **C2 (`catalog`)**: tabella `catalog.config_snapshots` con 6 `snapshot_kind`
(`property_management_config`, `beach_map`, `public_showcase`, `price_catalog` = listino *proprio*,
`services_catalog`, `agreement_catalog` = listini/voucher delle convenzioni, presente solo per gli
stabilimenti con partner) + tabella dedicata `catalog.search_bathhouses` per la ricerca pubblica. Dettaglio (contenuto di ogni
snapshot, interazioni utente, esempi, schema tabelle) in
[`analisi-post/db/_config-snapshot-proposta.md`](../analisi-post/db/_config-snapshot-proposta.md),
[`analisi-post/db/catalog_config_snapshots.md`](../analisi-post/db/catalog/catalog_config_snapshots.md),
[`analisi-post/db/catalog_search_bathhouses.md`](../analisi-post/db/catalog/catalog_search_bathhouses.md).
Gli altri domini adottano lo stesso schema (`<dominio>_config_snapshots`) dove emerge lo stesso bisogno.

---

## 8. (altre sezioni da aggiungere)
