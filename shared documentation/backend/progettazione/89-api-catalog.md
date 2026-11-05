# API del modulo Catalog

> Catalogo degli endpoint di `Module.Catalog`, scritti per incrementi dal 2026-10-25. Regole comuni in
> [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) e
> [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md). Il dettaglio dei campi è nello Swagger
> del modulo (`/swagger`, documento *Catalog API*). Decisioni e analisi: `documentazione-locale/catalog-api-analisi.md`
> (fuori dal repo); domande rimandate: [85-promemoria.md](85-promemoria.md) (PROMEMORIA-85-13..17).

## Regole comuni

- **Prefisso**: `/api/catalog`. Token utente obbligatorio salvo dove indicato.
- **Stabilimento**: sempre dal claim `property_id` del token, mai dalla richiesta. Un token senza stabilimento
  riceve 403 `catalog.property_required`; uno stabilimento inesistente o eliminato 404 `catalog.property_not_found`.
- **Identificativi**: nelle API solo i `public_id` (uuid), mai gli id interni.
- **Liste**: `page` (da 1), `pageSize` (10, 20, 25, 50, 100, 200, 300), `sortBy` (campi in camelCase indicati per
  ogni lista), `sortDir` = `asc`/`desc`, `search`; risposta `{ total, page, pageSize, items }`.
- **Tendine**: `GET …/lookup` → elenco breve non paginato.
- **Operazioni su più elementi**: `POST …/bulk-delete` con `{ ids }` (massimo 300). Si eseguono quelle possibili;
  la risposta 200 è `{ succeeded: [id], failed: [{ id, code, args }] }`, con lo stesso `code` dell'operazione
  singola.
- **Enum**: codici snake_case stabili; il frontend li traduce.
- **Verbi**: `POST` crea (201 con la risorsa), `PUT` sostituisce la risorsa intera, `DELETE` elimina (204,
  cancellazione logica).
- **Audit**: le modifiche del gestionale finiscono nel registro (categoria `operator`, codici `catalog.*`).

## Permessi

| Permesso | Ruoli |
|---|---|
| `catalog.read` | tutti i ruoli dello staff |
| `catalog.settings`, `catalog.pricing`, `inventory.manage` | superadmin, admin, user1 |
| `inventory.read` | superadmin, admin, user1, manager, lifeguard, advancedlifeguard, cashoperator, dailycashoperator |
| `agreements.manage` | superadmin, admin, manager |
| `revenue.manage` | superadmin, admin, manager, user1 |
| `platform.manage` | systemadmin |

Matrice in `Shared/Security/RolePermissions.cs`; il frontend la riceve da `GET /api/identity/me`. I blocchi per
licenza (Basic: solo Mappa, Magazzino, Impostazioni, Configurazione, Account) sono disattivati come gli altri
controlli di licenza (PROMEMORIA-85-1).

## Elenchi di piattaforma

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /lookups/countries` | nessuno (anche anonimo) | `[{ code, name }]`: codice ISO e nome tradotto |
| `GET /lookups/amenity-categories` | nessuno (anche anonimo) | `[{ code, amenities: [{ code, name, iconUrl }] }]`, le 6 categorie con le dotazioni |
| `GET /lookups/service-icons?type=` | `catalog.read` | icone dei servizi `[{ id, type, name, iconUrl }]`, `type` = `bar`, `normal`, `hourly` |
| `GET /lookups/calculation-methods` | `catalog.read` | `proportional`, `daily`, `daily_with_weekend`, `logarithmic` |
| `GET /lookups/vat-options` | `catalog.read` | aliquote del Paese dello stabilimento: `{ countryCode, rates, exemptNatures, defaultExemptNature }` |

- **Lingua**: i nomi tradotti seguono l'header `Accept-Language` (it, en, fr, es, de, zh, ar); senza una lingua
  gestita vale l'italiano. Le categorie si traducono nel frontend dal `code`.
- **IVA**: `rates` in percentuale, la prima è quella proposta (Italia: 22, 10, 4). Scegliendo «Esente» il frontend
  chiede la natura `n1`…`n6`, predefinita `n4` (esenti). Le voci senza aliquota valida vanno al 22%.
- Paesi, dotazioni e aliquote si caricano con migration ed ETL: non c'è un pannello.

## Informazioni stabilimento

Impostazioni › Informazioni stabilimento: la scheda pubblica dello stabilimento del token. Lettura `catalog.read`,
modifiche `catalog.settings`. Ogni risposta è la scheda intera (`PropertyInfoDto`).

| Metodo e percorso | Note |
|---|---|
| `GET /property-info` | scheda; tutti i campi null se lo stabilimento non l'ha ancora salvata |
| `PUT /property-info` | dati generali, posizione, orari, cancellazione, animali, tour virtuale, slug |
| `PUT /property-info/amenities` | `{ codes }`: le dotazioni spuntate (codici dei lookup) |
| `POST /property-info/main-photo` · `DELETE` | immagine principale (multipart, campo `file`) |
| `POST /property-info/photos` | aggiunge una foto alla galleria (massimo 30) |
| `PUT /property-info/photos/order` | `{ ids }`: tutte le foto nell'ordine voluto |
| `DELETE /property-info/photos/{id}` | |
| `POST /property-info/documents/{menu\|price-list}` · `DELETE` | PDF del menu o del listino |

- `PropertyInfoDto`: `{ name, slug, description, regulation, address, city, cap, province, region, countryCode, latitude,
  longitude, phone, email, website, facebook, instagram, whatsapp, openHoursFrom, openHoursTo, isFreeCancellation,
  cancellationNoticeHours, petsPolicy, virtualTourUrl, mainPhotoUrl, photos: [{ id, url, sortOrder }],
  menuPdf: { fileName, url }, priceListPdf, amenityCodes }`. `petsPolicy` = `allowed`, `not_allowed`, `on_request`.
- La scheda nasce al primo salvataggio. Il nome vale anche per lo stabilimento (come nel legacy).
- **Slug**: minuscole, cifre e trattini; `null` lo genera dal nome la prima volta e poi lo lascia com'è; già usato da
  un'altra struttura: 409 `catalog.slug_taken`.
- Preavviso di cancellazione (ore) solo con la cancellazione gratuita; latitudine e longitudine insieme.
- Immagini png, jpeg o webp fino a 5 MB (422 `catalog.invalid_image`); PDF fino a 10 MB (422 `catalog.invalid_pdf`).

## Registrazione di una struttura

`POST /property-registrations` (anonimo, multipart): `name`, `city`, `contactFullName`, `email`, `phoneCountryCode`
(es. `+39`), `phone`, `verificationToken` (soluzione della sfida anti-robot), `website` (campo trappola, vedi sotto),
`photos` (facoltative, fino a 10 immagini). Risposta 201 `{ id }`. Lo stabilimento nasce **in attesa** di approvazione,
senza titolare né paese; le foto restano dello stabilimento finché non apre la scheda.

- **Anti-robot, senza servizi esterni** (prova di lavoro, stesso formato del widget open source ALTCHA):
  1. il form chiede `GET /api/security/human-challenge` → `{ algorithm, challenge, maxNumber, salt, signature }`;
  2. il browser cerca il numero `n` (da 0 a `maxNumber`) per cui SHA-256(`salt` + `n`) = `challenge` (fino a
     1,3 s su un computer provando i numeri a lotti in parallelo; codice di esempio in
     `analisi-post/docs/catalog-per-front.docx`);
  3. manda in `verificationToken` il JSON `{ algorithm, challenge, number, salt, signature }` in base64.

  Il backend controlla firma, scadenza (10 minuti), che siano passati almeno 3 secondi dalla sfida e che la sfida non
  sia già stata usata.
- **Campo trappola** `website`: nel form va nascosto (non visibile né raggiungibile da tastiera) e lasciato vuoto; se
  arriva compilato la richiesta si rifiuta come robot.
- **Limite di invii**: 5 ogni 10 minuti per indirizzo IP (`Security:PublicForms:PermitPer10Minutes`), oltre → 429
  `auth.too_many_attempts`.
- Verifica fallita (sfida, trappola, token mancante o già usato): 422 `catalog.human_verification_failed`.
- **Configurazione** `Security:HumanVerification`: `Enabled`, `Provider` = `proof_of_work` (predefinito) o `turnstile`
  (Cloudflare, con `SecretKey`), `HmacKey` (base64, almeno 32 byte, dal secret manager), `MaxNumber`,
  `ChallengeMinutes`, `MinSeconds`. In sviluppo è spenta; con `Enabled=true` senza la chiave del provider il backend
  non parte.
- **Non ancora disponibile** (Notification, PROMEMORIA-85-17): email di benvenuto al referente e avviso a info@.
  Catalog pubblica l'evento `PropertyRegistered`.

## Prenotazioni online

Impostazioni › Opzioni prenotazioni online. Lettura `catalog.read`, modifiche `catalog.settings`.

| Metodo e percorso | Note |
|---|---|
| `GET /online-settings` · `PUT` | opzioni della prenotazione online delle postazioni |
| `GET /online-settings/services` · `PUT` | opzioni della prenotazione online dei servizi |
| `GET /online-settings/stay-limits` · `POST` · `PUT /{id}` · `DELETE /{id}` | limiti di giorni per fila/settore e periodo |
| `GET /online-settings/legend` · `PUT` | legenda della mappa online, sostituita per intero |

- **Postazioni** (`OnlineSettingsDto`): `isEnabled`, `isSuspendedByPlatform` (sola lettura: sospensione del systemadmin o
  dell'abbonamento), `isMapPick`, `isLocationRestricted` + `allowedSpotIds`, `isPeriodRestricted` + `allowedPeriods`
  (`[{ startsOn, endsOn }]`, non sovrapposti), `surchargeType` (`disable`, `percentage` 0–100, `fixed`) + `surchargeValue`,
  `isBlocked` («Blocca prenotazioni online»), `requiresManualConfirmation`, `allowsSameDayBooking`, `allowsHalfDay` con
  `morningFrom`/`morningTo`/`afternoonFrom`/`afternoonTo`, `afternoonConversionTime` (dopo quest'ora le prenotazioni del
  giorno diventano pomeridiane), `allowsSeasonal`, `allowsSharing`, `maxSpotsPerBooking`, `maxBookingsPerDay`,
  `bookingHorizonDays` (non si prenota se la fine supera oggi + N giorni; legacy `blockReservation`), `scope` (`all`, `contacts_only`), `paymentMethods`
  (`pay_now`, `pay_at_property`, `pay_at_property_no_card`, `deposit_half`, `pay_at_property_prepaid`), `usefulInfo`,
  `goodToKnow`. Il `PUT` riceve gli stessi campi tranne `isSuspendedByPlatform`; postazioni di altri stabilimenti: 422
  `map.spot_not_found` con `args: { ids }`.
- **Servizi**: `{ isEnabled, surchargeType, surchargeValue, requiresManualConfirmation, sectionTitle, usefulInfo, paymentMethods }`.
- **Limiti di soggiorno**: `{ id, sectorId, startsOn, endsOn, minDays, maxDays }`, almeno uno fra minimo e massimo; per
  la stessa fila i periodi non si sovrappongono (409 `catalog.stay_limit_overlap`). Valgono solo online.
- **Legenda**: `{ items: [{ label, iconId }] }`, `iconId` dal catalogo della mappa; risposta `[{ label, iconId, iconUrl }]`.
- **Non ancora disponibile**: applicazione delle regole alle prenotazioni (Booking) e collegamento a Stripe (Payment).
  Il codice iframe del widget si costruisce nel frontend dallo slug.

## Servizi

Impostazioni › Servizi. Lettura `catalog.read`, modifiche `catalog.settings`.

| Metodo e percorso | Note |
|---|---|
| `GET /services` | `kind` (`bar`, `normal`, `hourly`), `search`; `sortBy` = `name` (default), `price` |
| `GET /services/lookup` | `[{ id, kind, name }]` |
| `GET /services/{id}` · `POST /services` · `PUT /services/{id}` · `DELETE /services/{id}` | servizio completo (`ServiceDto`) |
| `POST /services/bulk-delete` | esito parziale |
| `POST /services/{id}/duplicate` · `POST /services/bulk-duplicate` | copia con il primo nome libero fra «Nome copia», «Nome copia 2»… fra i servizi dello stesso tipo; stessa icona o immagine |
| `POST /services/{id}/image` · `DELETE` | immagine caricata (multipart `file`), in alternativa all'icona |
| `GET /automatic-prices` · `GET /{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` · `POST /{id}/image` | prezzi automatici |
| `POST /automatic-prices/bulk-delete` · `POST /{id}/duplicate` · `POST /bulk-duplicate` | come per i servizi |
| `GET /table-services` · `GET /{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` | servizio tavoli |
| `POST /table-services/bulk-delete` · `POST /{id}/duplicate` · `POST /bulk-duplicate` | come per i servizi |

- **Servizio** (`ServiceDto`): `{ id, kind, name, description, category, price, vatRate, vatNature, iconId, imageUrl,
  isPurchasable, isHighlighted, hasPeriodPrices, periodPrices: [{ periodKey, price }], isSectorRestricted, sectorIds,
  allergens, variants: [{ name, price }], translations: [{ language, name, description }], bookingOptions,
  hourlyPrices }`.
  - `kind` si sceglie alla creazione e non cambia (422 `service.kind_immutable`).
  - `category` solo per i servizi bar: `bar`, `pizzeria`, `restaurant`, `fry_shop`, `sandwich_shop`, `appetizers`,
    `first_courses`, `second_courses`, `desserts`, `fruit`, `ice_cream_shop`, `drinks`.
  - **IVA**: `vatRate` fra le aliquote del Paese (`GET /lookups/vat-options`, altrimenti 422 `catalog.vat_rate_not_allowed`)
    **oppure** `vatNature` (`n1`…`n6`) per «Esente»: mai entrambi. I servizi orari possono lasciarli vuoti se ogni
    disponibilità ha la sua `vatRate`: quella del servizio diventa la prima.
  - `iconId` dai lookup (`GET /lookups/service-icons`); l'immagine caricata la sostituisce, `imageUrl` mostra quella in uso.
  - `periodPrices`: prezzo per periodo tariffario (`periodKey` di `GET /tariff-periods`), usati con `hasPeriodPrices`.
  - `isSectorRestricted` + `sectorIds`: acquistabile solo in quelle file/settori.
  - `allergens` (solo bar): `gluten`, `crustaceans`, `eggs`, `fish`, `peanuts`, `soybeans`, `milk`, `nuts`, `celery`,
    `mustard`, `sesame`, `sulphites`, `lupin`, `molluscs`. `variants` solo per i bar.
  - `translations`: lingue diverse dall'italiano (en, fr, es, de, zh, ar), una volta ciascuna.
  - `bookingOptions` (ripetibili): `{ startsOn, endsOn, weekdays, managementFrom, managementTo, onlineFrom, onlineTo,
    maxPerHour, maxPerDay, maxPerSlot, intervalMinutes, minItems, maxItems, hourly }`; `weekdays` vuoto = tutti i giorni.
    `hourly` solo per i servizi orari: la disponibilità valida nelle date dell'opzione (vedi sotto).
  - **Servizi orari** (dal 2026-11-05): i prezzi sono **disponibilità**. `hourlyPrices` = `[{ periodKey, options }]`:
    con `hasPeriodPrices` un gruppo per periodo tariffario (`periodKey` di `GET /tariff-periods`), senza un solo gruppo
    con `periodKey` = null. Ogni disponibilità (`HourlyOptionDto`):
    `{ timeFrom, timeTo, weekdays, slots: [{ durationMinutes, isEnabled, price, id }], customSlot, vatRate, maxPerSlot,
    hasFixedTimeSlots }`.
    - `slots`: durate standard 10, 15, 30, 45, 60, 90, 120, 180, 240, 300, 360 minuti, una volta ciascuna; una durata
      attiva vuole il prezzo. `customSlot` = durata personalizzata (1…1440 minuti).
    - `vatRate` null = quella del servizio; `maxPerSlot` = massimo di prenotazioni per fascia; `hasFixedTimeSlots` =
      partenze a orari fissi, una dopo l'altra.
    - `id` delle durate solo in risposta: è il `timeSlotId` del preventivo. Si rigenera a ogni salvataggio.
    - Al più 20 disponibilità per gruppo. Errori: 400 `validation.invalid` (durata non standard, periodo con
      `hasPeriodPrices` = false o viceversa, `hourly` su un servizio non orario), 422 `pricing.period_not_found`.
    - I servizi migrati hanno una disponibilità senza periodo con orari, giorni, IVA, massimo per fascia e durate del
      vecchio sistema (e la durata personalizzata, quando leggibile).
  - Eliminando un servizio lo si toglie dai prezzi automatici che lo citavano.
- **Prezzo automatico** (`AutomaticPriceDto`): `{ id, name, description, price, vatRate, vatNature, iconId, imageUrl,
  isPurchasable, appliesPerService, appliesToAllServices, serviceIds, periods, isSectorRestricted, sectorIds,
  translations }`. `appliesPerService` = per ogni servizio dell'ordine invece che una volta per ordine; senza
  `appliesToAllServices` serve almeno un servizio.
  - `periods` (dal 2026-11-05): `[{ startsOn, endsOn, weekdays, timeRanges: [{ from, to }] }]`, al più 50. Date null =
    senza limite; `weekdays` vuoto = tutti i giorni; `timeRanges` vuoto = tutto il giorno. Nessun periodo = sempre.
    Il prezzo si applica a un servizio dell'ordine se un periodo copre il suo giorno, il giorno della settimana e l'ora.
- **Servizio tavoli** (`TableServiceDto`): `{ id, name, description, isOnlineBookable, isHighlighted, capacityMode,
  tableTypes: [{ seats, count }], totalSeats, averageMealMinutes, maxBookingsPerSlot, minSeatsPerBooking,
  maxSeatsPerBooking, depositMode, depositType, depositValue, availabilities: [{ startsOn, endsOn, slots: [{ weekday,
  from, to }] }], translations }`. `capacityMode` = `tables` (con `tableTypes`) o `seats` (con `totalSeats`);
  `depositMode` = `none`, `total`, `per_person` con `depositType` `percentage` (fino a 100) o `amount`; una fascia senza
  `weekday` vale per tutti i giorni del periodo.
- **Non ancora disponibile**: applicazione di capienza, intervalli e prezzi automatici agli ordini, assegnazione dei
  tavoli (Booking) e caparra con carta salvata e pre-autorizzazione (Payment, PROMEMORIA-85-15).

## Listino

Impostazioni › Listino. Lettura `catalog.read`, modifiche `catalog.pricing`.

| Metodo e percorso | Note |
|---|---|
| `GET /price-lists` | `kind` (`package`, `additional`), `search`; `sortBy` = `name` |
| `GET /price-lists/lookup?kind=` | `[{ id, kind, name }]` |
| `GET /price-lists/{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` | voce completa (`PriceListDto`) |
| `POST /price-lists/bulk-delete` | esito parziale |
| `POST /price-lists/{id}/duplicate` · `POST /price-lists/bulk-duplicate` | copia mai predefinita, con il primo nome libero fra «Nome copia», «Nome copia 2», «Nome copia 3»… (la copia di una copia riparte dal nome base; un nome lungo si accorcia, il suffisso resta intero) |
| `GET /price-lists/export?format=csv\|xlsx` · `GET /price-lists/import-template?format=` · `POST /price-lists/import` | prezzi in file |
| `GET /reduced-seasons` · `POST` · `PUT /{id}` · `DELETE /{id}` | voci «prezzo stagionale ridotto» |

- **Voce** (`PriceListDto`): `{ id, kind, name, translations: [{ language, name }], maxPeople, maxAdditionalItems, items:
  [{ inventoryItemId, quantity }], isAreaPackage, includedServiceIds, packageLinks: [{ packageId, maxCount }],
  isSectorRestricted, sectorIds, salesChannel, isDefault, isDefaultOnline, isMapAssignable, calculationMethod,
  monthlySetting, weekendSetting, allowsPublicHolidays, holidayDates, isCumulative, vatRate, vatNature, sectorPrices,
  periodPrices, reducedPrices }`.
  - `kind` non cambia (422 `pricing.price_list_kind_immutable`). Un articolo aggiuntivo vende un solo articolo
    (`items` con un elemento) ed è legato ai pacchetti di `packageLinks` (vuoto = tutti), con il massimo per
    prenotazione; `includedServiceIds` (servizi venduti) e `maxAdditionalItems` valgono per i pacchetti.
  - `salesChannel`: `manager_only`, `manager_and_online`, `online_only`. Un solo pacchetto predefinito e uno
    predefinito online per tipo: segnandone uno, gli altri si spengono.
  - `calculationMethod` dai lookup; `monthlySetting` = `effective_month_duration`, `actual_month_duration`,
    `fixed30_days`; `weekendSetting` = `saturday_sunday`, `sunday_only`. `holidayDates` con `allowsPublicHolidays`.
  - IVA come per i servizi (`vatRate` del Paese oppure `vatNature`); pacchetti e aggiuntivi partono al 22%.
  - **Prezzi**: `sectorPrices` `[{ sectorId, seasonalPrice, seasonalHalfDayPrice }]` (con `isSectorRestricted` le file
    con un prezzo sono le «aree abilitate»); `periodPrices` `[{ sectorId, periodKey, halfDay, halfDayWeekend, daily,
    weekend, halfDay7, days7, halfDay14, days14, halfDayMonthly, monthly, stayOverrides: [{ stayDays, price }] }]`, una
    riga per fila e periodo (`sectorId` null = tutte le file); i prezzi a 7, 14 giorni e mensili, anche di mezza
    giornata, sono i riferimenti del calcolo; `stayOverrides` = «Modifica prezzi intermedi» (1–366 giorni);
    `reducedPrices` `[{ reducedSeasonId, sectorId, price }]`.
- **Stagionale ridotto** (`ReducedSeasonDto`): `{ id, name, startsOn, endsOn }`. La voce vale per tutti i pacchetti, ognuno
  ne imposta il prezzo per fila; eliminandola spariscono i suoi prezzi.
- **File**: colonne `id;kind;name;row;sector;period;half_day;half_day_weekend;daily;weekend;half_day_7;days_7;half_day_14;
  days_14;half_day_monthly;monthly;seasonal;seasonal_half_day`. `row` = `period` (tariffe di una fila in un periodo) o
  `seasonal` (prezzo stagionale della fila); fila e periodo per nome; decimali con la virgola o il punto. Con `id` si
  aggiornano i prezzi di quella voce, senza `id` si cerca per tipo e nome o se ne crea una nuova (canale
  gestionale, IVA 22%). Risposta `{ created, updated, errors: [{ row, code, args }] }`; intestazione diversa: 422
  `pricing.import_file_invalid`.
- **Non ancora disponibile**: il calcolo del prezzo (fase 3) e il prezzo suggerito (Revenue).

## Sconti e voucher

Impostazioni › Sconti e voucher. Lettura `catalog.read`, modifiche `catalog.pricing`.

| Metodo e percorso | Note |
|---|---|
| `GET /discounts` | `search` (codice o nome); `sortBy` = `code` (default), `value` |
| `GET /discounts/{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` | |
| `POST /discounts/bulk-delete` | esito parziale |

- `DiscountDto`: `{ id, code, name, requiresCode, type, value, scope, serviceCategories, sectorIds, channel, maxUses,
  usedCount, isActive, periods, weekdays, timeRanges }`.
- **Quattro tipologie**: con codice (`requiresCode`) o automatico nei periodi di validità (senza codice serve almeno un
  periodo), ciascuno con o senza opzioni personalizzate: `scope` = `all`, `custom`, `without_custom`; con le opzioni
  servono file/settori (`sectorIds`) e/o categorie (`serviceCategories`: `bar`, `normal`, `hourly`, `additional`).
- `code` è il «codice sconto» (per uno automatico il suo nome), unico per stabilimento senza distinguere maiuscole e
  minuscole (409 `pricing.discount_code_taken`). `type` = `percentage` (1–100) o `fixed` (sul totale).
- `channel` = `manager_and_online`, `manager_only`, `online_only`; `maxUses` null = illimitato, non sotto gli utilizzi
  già fatti (422 `pricing.discount_max_uses_below_used`).
- `periods` multipli, `weekdays` (vuoto = tutti), `timeRanges` `[{ from, to }]`.
- **Non ancora disponibile** (fase 3 e Booking): applicazione alla prenotazione, un solo sconto per prenotazione (vale
  l'ultimo inserito), conteggio degli utilizzi.

## Convenzioni

Strutture convenzionate (hotel, campeggi…) con i loro listini e voucher. Permesso `agreements.manage`, salvo la tendina
`GET /agreement-partners/lookup` (`catalog.read`, serve anche in cassa per scegliere la struttura in prenotazione).

| Metodo e percorso | Note |
|---|---|
| `GET /agreement-partners` | `search` (nome o città); `sortBy` = `name` (default), `city` |
| `GET /agreement-partners/{id}` · `POST` · `PUT /{id}` · `DELETE /{id}` · `POST /bulk-delete` | |
| `POST /agreement-partners/{id}/duplicate` | copia città, CAP, provincia, listini e voucher (non indirizzo, contatti, note, report); nome come per i listini: «Nome copia», «Nome copia 2»… |
| `GET /agreement-partners/{id}/price-lists?listType=&kind=` · `POST` | listini (`regular`) e voucher (`voucher`) della struttura |
| `GET /agreement-price-lists/{id}` · `PUT` · `DELETE` · `POST /bulk-delete` · `POST /{id}/duplicate` · `POST /bulk-duplicate` | nome della copia come per i listini, fra i listini e voucher della stessa struttura |
| `GET /agreement-reports?partnerId=&status=` · `GET /{id}` · `POST /{id}/cancel` | report |

- `AgreementPartnerDto`: `{ id, name, city, postalCode, province, streetName, streetNumber, phone, email, note,
  hasPriceList, hasVoucher }`. Eliminando una struttura si eliminano i suoi listini.
- `AgreementPriceListDto`: come la voce di listino (`items`, `includedServiceIds`, `packageLinks`, `calculationMethod`,
  `monthlySetting`, `weekendSetting`, `allowsPublicHolidays`, `holidayDates`, `isCumulative`, `vatRate`/`vatNature`,
  `sectorPrices`, `periodPrices` per fila e periodo) più `partnerId`, `listType`, `minBookingDays` e
  `maxVoucherQuantity` (solo voucher). Un aggiuntivo si lega ai pacchetti della stessa struttura e dello stesso tipo di
  listino. `listType` e `kind` non cambiano.
- Report: `{ id, partnerId, partnerName, searchType, periodStart, periodEnd, status, totalReservations, totalRevenue, notes,
  createdAt }`, `status` = `pending`, `created`, `cancelled`.
- **Non ancora disponibile** (Booking, Notification): creazione e invio dei report, elenco delle prenotazioni della
  struttura, conteggio delle prenotazioni.

## Revenue

Prezzo suggerito e banner sconto dello stabilimento (permesso `revenue.manage`); pesi e periodi standard del
systemadmin (`platform.manage`), uguali per tutti gli stabilimenti.

| Metodo e percorso | Note |
|---|---|
| `GET /revenue/settings` · `PUT` | `{ isSuggestedPriceEnabled, isPriceDiscountEnabled, isShownOnPublicPage, dailyBannerCount, dailyBannerShownCount (sola lettura), isDiscountLimitedToSpots, spotIds }` |
| `GET /revenue/suggested-prices?priceListId=&from=&to=` | al più 366 giorni; senza `priceListId` il pacchetto predefinito |
| `POST /revenue/suggested-prices/compute` | `{ priceListId?, from, to }`, al più 92 giorni; restituisce le righe calcolate |
| `POST /revenue/suggested-prices/apply` | `{ priceListId, items: [{ date, sectorId?, mode, discountPercent? }] }`, tutto o niente |
| `GET /admin/revenue/weights` · `PUT` | pesi come moltiplicatori (0,70 = −30%, 1,20 = +20%) |
| `GET /admin/revenue/standard-periods` · `PUT` | `PUT` con `{ periods: [...] }` sostituisce l'elenco |

- `SuggestedPriceDto`: `{ priceListId, date, sectorId, periodKey, listDailyPrice, listWeekendPrice, suggestedPrice,
  discountPercentage, appliedMode, computedAt, appliedAt }`. Una riga per giorno e fila del pacchetto (`sectorId`
  null = prezzo valido per tutte le file). `discountPercentage` è lo sconto rispetto al prezzo di listino del giorno
  (negativo = aumento); con `appliedMode` = `discount` è quella scelta dal gestore.
- `appliedMode`: `none`, `suggested` (si usa il prezzo suggerito), `original` (si usa quello di listino), `discount`
  (si applica `discountPercent`, tra 0 e 100 esclusi).
- **Calcolo**: per ogni giorno si prende il periodo tariffario che vale (il più breve fra quelli che lo coprono) e, per
  ogni fila, il prezzo del weekend il sabato e la domenica, altrimenti il giornaliero. I pesi si moltiplicano:
  pioggia (sconto di bassa o di alta stagione), giorno prima del weekend (aumento) oppure, negli altri giorni di alta
  stagione, aumento di alta stagione; posti liberi dalla soglia in su sconto, sotto aumento. Un giorno senza periodo
  o senza prezzo non ha suggerimento. Ricalcolando, la scelta del gestore resta.
- **Pioggia** da Open-Meteo con le coordinate dello stabilimento: previsione fino a 16 giorni, storico per i giorni
  passati. Più in là, o se il servizio non risponde, la regola della pioggia non si applica.
- **Periodi standard** `{ name, startMonth, startDay, endMonth, endDay, tier }`, `tier` = `low`, `high`: si ripetono ogni
  anno, possono scavalcare il capodanno, non si sovrappongono; i giorni fuori da ogni periodo sono di bassa stagione.
  Valori iniziali: alta stagione 15/07–26/08; pesi 0,70 / 0,80 pioggia, 1,25 prima del weekend, 1,20 alta stagione,
  soglia 30%, 0,70 / 1,10 posti liberi.
- **Non ancora disponibile**: la percentuale di posti liberi (Booking; fino ad allora la regola non si applica), la
  decisione di mostrare il banner sulla pagina pubblica e il contatore delle visualizzazioni (fase 2), l'uso del
  prezzo scelto in prenotazione (motore di prezzo, fase 3).

## Pannello di amministrazione

Tutto sotto `/admin`, permesso `platform.manage` (systemadmin). I pesi e i periodi standard del prezzo suggerito sono
nella sezione Revenue.

| Metodo e percorso | Note |
|---|---|
| `GET /admin/properties?status=&search=&trashed=` | paginato; `search` su nome, città, email; `trashed=true` = il cestino |
| `GET /admin/properties/{id}` | |
| `POST /admin/properties/{id}/status` | `{ status, comment? }`, `status` = `approved`, `rejected`, `pending`, `hidden` |
| `POST /admin/properties/{id}/trash` · `POST /{id}/restore` · `DELETE /{id}` | cestino, ripristino, eliminazione definitiva (solo dal cestino) |
| `PUT /admin/properties/{id}/region` | `{ regionId }`; `null` torna all'assegnazione automatica |
| `PUT /admin/properties/{id}/badges` | `{ badgeIds }`, al più 10 |
| `GET /admin/map-icons?category=` · `POST` (multipart `file`, `category`, `state`, `name`) | catalogo delle icone della mappa |
| `PUT /admin/map-icons/{id}` · `PUT /{id}/image` (multipart) · `DELETE /{id}` | `{ name, isActive, isDefault }` |
| `GET /admin/beach-elements` · `POST` (multipart `name`, `sortOrder`, `isActive`, `file` facoltativo) | elementi spiaggia |
| `PUT /admin/beach-elements/{id}` · `PUT /{id}/image` (multipart) · `DELETE /{id}` | `{ name, sortOrder, isActive }` |
| `GET /admin/badges` · `POST` · `PUT /{id}` · `DELETE /{id}` | `{ name, color, sortOrder, translations }` |
| `GET /admin/regions` · `POST` · `PUT /{id}` · `DELETE /{id}` | `{ countryCode, name, sortOrder, translations }` |
| `GET /admin/showcase/{section}` · `PUT` | `section` = `featured` («In evidenza»), `popular` («Più frequentati»); `PUT { propertyIds }` in ordine, al più 50 |

- `AdminPropertyDto`: `{ id, name, status, adminComment, city, contactFullName, contactEmail, contactPhone, ownerUserId,
  isOnlineSalesSuspended, isSubscriptionActive, bathhouseId, slug, addressRegion, regionId, isRegionManual, badgeIds,
  likeCount, createdAt, deletedAt }`. `addressRegion` è la regione scritta nell'indirizzo, `regionId` quella della
  vetrina.
- **Stati**: `pending` (in attesa: fuori dal sito, niente prenotazione online, il gestore non entra), `approved`,
  `rejected`, `imported` (importate nel 2023, «solo contatto», non si assegna a mano), `hidden` (le strutture nascoste
  a mano nel legacy: fuori dal sito e fuori dalla coda «in attesa»). Il motivo è facoltativo e si mostra al titolare.
  Una struttura nel cestino non cambia stato.
- **Cestino**: la struttura sparisce dal sito e dal gestionale; col ripristino torna com'era. L'eliminazione
  definitiva la toglie anche da qui; da quel momento partono i tempi di conservazione dei dati.
- **Icone**: una sola predefinita per categoria e stato (`occupied`, `free`), sempre attiva; la nuova predefinita
  sostituisce la precedente. Le mappe restano collegate al catalogo: cambiando l'immagine cambia su tutte le mappe che
  la usano; eliminando un'icona (non la predefinita) quelle mappe passano alla predefinita. La prima icona di una
  categoria e stato diventa predefinita.
- **Elementi spiaggia**: un elemento usato su qualche mappa non si elimina (`usageCount`): si disattiva, e sparisce
  dall'elenco del gestore restando sulle mappe.
- **Regioni**: seminate le 20 italiane; la regione di una struttura si assegna da sola dalla regione dell'indirizzo
  (nome o traduzione, contando solo le lettere), anche quando il gestore salva le informazioni dello stabilimento,
  salvo che il systemadmin l'abbia scelta a mano. Eliminando una regione le strutture tornano all'assegnazione
  automatica. Eliminando un badge sparisce dalle strutture.
- **Eventi pubblicati** (Shared/Contracts/Catalog): `PropertyStatusChanged` (`status`, `previousStatus`, `comment`) e
  `PropertyTrashChanged` (`change` = `trashed`, `restored`, `purged`). Oggi nessun gestore (PROMEMORIA-85-18).
- **Eventi di Identity gestiti**: titolare assegnato (`ownerUserId`), prenotazione online abilitata o disabilitata
  dal systemadmin (`isOnlineSalesSuspended`), stato dell'abbonamento (`isSubscriptionActive`: solo `active` lascia le
  vendite online), «mi piace» (`likeCount`).

## Sito pubblico

Senza autenticazione, sotto `/public`; i testi tradotti seguono `Accept-Language`. Si vedono solo le strutture
**pubblicate**: vetrina con data di pubblicazione, stabilimento `approved` o `imported`, fuori dal cestino.

| Metodo e percorso | Note |
|---|---|
| `GET /public/home` | `{ featured, popular, regions }`: le due sezioni scelte dal systemadmin e le regioni con il numero di strutture |
| `GET /public/search` | `text`, `regionId`, `latitude`+`longitude` (località cercata), `from`+`to`, `amenities` (codici separati da virgola, tutti richiesti), `onlineOnly`, `sortBy` = `popularity` (predefinito), `price`, `distance`, `name`, `sortDir`, `page`, `pageSize` (≤ 50) |
| `GET /public/suggestions?text=` | da 2 caratteri: `{ places: [{ city, province, region, latitude, longitude, count }], bathhouses: [card] }` |
| `GET /public/bathhouses/{slug}` | scheda |
| `GET /public/bathhouses/{slug}/map` | `{ map, onlineSpotIds, legend }`: mappa come nel gestionale; `onlineSpotIds` null = tutte prenotabili online; `legend` = `[{ label, iconId, iconUrl }]` |
| `GET /public/bathhouses/{slug}/services` | `{ services, tables }`: servizi acquistabili online e servizi tavolo prenotabili online |
| `GET /public/bathhouses/{slug}/packages?from=&to=` | pacchetti vendibili online con il prezzo «da» |
| `GET /public/bathhouses/{slug}/discount-banner` · `POST /discount-banner/views` | banner sconto: stato, e conteggio di una visualizzazione |
| `POST /admin/search/rebuild` | systemadmin: ricostruzione completa della ricerca |

- **Card** (`PublicCardDto`): `{ id, name, slug, city, province, region, latitude, longitude, distanceKm, fromPrice,
  likeCount, mainPhotoUrl, description (prime 300 lettere), badges: [{ id, name, color }], amenityCodes,
  isOnlineBookable, isContactOnly }`. I nomi e le icone delle dotazioni vengono da `GET /lookups/amenity-categories`.
- **Ricerca**: `{ total, page, pageSize, items, amenityCounts }`, `amenityCounts` = per ogni dotazione quante
  strutture fra i risultati la hanno. `text` cerca in nome, città, provincia e regione dell'indirizzo.
  **Popolarità** = «mi piace», poi anzianità dell'abbonamento al gestionale. **Distanza** dalla località cercata
  (`latitude`/`longitude`, obbligatorie con `sortBy=distance`); le strutture senza coordinate vanno in fondo e senza
  `distanceKm`. **Prezzo**: con le date si ordina sul prezzo di quei giorni, altrimenti su quello della home; le
  strutture senza prezzo in fondo.
- **Prezzo «da»**: il più basso fra i pacchetti vendibili online, nei periodi tariffari dei giorni indicati (per ogni
  giorno vale il periodo più breve), con il prezzo di mezza giornata se c'è, altrimenti della giornata intera. Senza
  date (home, card, scheda): il prezzo di oggi; fuori stagione il più basso della stagione in arrivo o, finita la
  stagione, della successiva; senza listino `null`. Le date coprono al più 92 giorni.
- **Prenotabile online** (`isOnlineBookable`): stabilimento approvato, vendite online non sospese dal systemadmin,
  abbonamento attivo, prenotazione online abilitata dal gestore e almeno un pacchetto vendibile online.
  `isContactOnly` = struttura importata nel 2023: solo contatti.
- **Scheda** (`PublicBathhouseDto`): `{ id, info, region, badges, likeCount, fromPrice, isOnlineBookable,
  isContactOnly, online }`; `info` ha la forma di `GET /property-info` (contatti della struttura, mai del titolare);
  `online` (solo se prenotabile) = `{ allowsHalfDay, morningFrom, morningTo, afternoonFrom, afternoonTo,
  allowsSeasonal, allowsSameDayBooking, bookingHorizonDays, requiresManualConfirmation, isOnlinePaymentBlocked,
  paymentMethods, goodToKnow, usefulInfo }`.
- **Servizi**: `{ id, kind, name, description, category, price, imageUrl, isHighlighted, allergens, variants: [{ name,
  price }] }`; tavoli `{ id, name, description, isHighlighted, depositMode }`.
- **Pacchetti**: `{ id, name, maxPeople, isDefault (predefinito online), fromPrice }`.
- **Banner sconto**: `{ isVisible, discountPercent, spotIds }`. Visibile se il gestore lo ha attivato e mostrato in
  pagina, la struttura è prenotabile online, sono fra le 10 e le 20 (ora italiana) e nella fascia corrente non è
  ancora stato mostrato: la giornata è divisa in tante fasce quante le visualizzazioni scelte dal gestore.
  `discountPercent` è lo sconto di oggi scelto nel prezzo suggerito, se c'è; `spotIds` le postazioni a cui è limitato
  (vuoto = tutte). Il frontend chiama `POST …/views` quando lo mostra; i contatori si azzerano ogni notte.
- **Aggiornamento**: la ricerca si ricalcola da sola dopo ogni modifica che la riguarda (vetrina, foto, dotazioni,
  badge, stato e cestino, listini, stagione, impostazioni online, «mi piace», abbonamento), all'avvio del backend e
  ogni notte alle 00:05.
- **Non ancora disponibile**: disponibilità delle postazioni sulla mappa e prenotazione (Booking), prezzo esatto di
  una prenotazione (fase 3, preventivo).

## Preventivo

Il prezzo di una prenotazione lo calcola il backend (il legacy lo faceva nel browser e salvava gli importi ricevuti).

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `POST /quotes` | `catalog.read` | gestionale, stabilimento del token |
| `POST /public/bathhouses/{slug}/quote` | nessuno (anche anonimo) | sito: valgono le regole della prenotazione online |

- **Richiesta**: `{ from, to, dayPart (full_day | morning | afternoon), seasonal, spots, services, discountCode }`, al più
  366 giorni.
  - `spots[]`: `{ spotId | sectorId, priceListId | agreementPriceListId, voucherPriceListId, absences, manualPrice, additionals }`.
  - `additionals[]`: `{ priceListId, quantity, dates }` (`dates` = solo quei giorni).
  - `services[]`: `{ serviceId, quantity, variant, date, timeSlotId }`.
- **Risposta**: `{ lines, subtotal, discount, total, requiresManualConfirmation, warnings }`. Ogni riga è
  `{ kind, refId, spotId, sectorId, description, quantity, amount, vatRate, vatNature, rule, segments }`:
  - `kind` = `package`, `additional`, `voucher` (negativo), `service`, `automatic_price`, `surcharge`;
  - `segments` = `[{ from, to, periodKey, days, amount, rule }]`, uno per periodo tariffario;
  - `warnings`: `days_without_period`, `period_without_price`, `seasonal_price_missing`.

  Importi IVA compresa; la commissione online la aggiunge Payment.
- **Precedenze per una postazione**:
  1. prezzo dell'operatore (`manualPrice`, solo gestionale);
  2. stagionale (`seasonal`: prezzo stagionale della fila, o di mezza giornata);
  3. stagionale ridotto (date identiche a una voce «prezzo stagionale ridotto»);
  4. tratto per tratto la griglia del pacchetto. La griglia: per ogni giorno vale il periodo più breve; nessun periodo = 0 con avviso.
- **Griglia** (come il legacy, verificata cella per cella sul codice originale): proporzionale, giornaliero (anche «giornaliero
  con weekend») e logaritmico su 1, 7, 14 giorni e mese (31 giorni con la durata effettiva), con le celle corrette a
  mano dal gestore che prevalgono; nessun limite di 90 giorni.
- **Weekend e festivi**: proporzionale, giornaliero e logaritmico applicano il prezzo weekend a sabato e domenica solo
  fino a 3 giorni e ai festivi fino a 6; il «giornaliero con weekend» applica scaglione mensile, di 14 o di 7 giorni e,
  sui giorni che restano, il prezzo weekend a weekend e festivi. Domenica sola se il pacchetto lo dice. I festivi
  valgono se il pacchetto li ammette.
- **Mezza giornata**: weekend e festivi sempre al prezzo di mezza giornata nel weekend, gli altri giorni con lo stesso
  metodo del pacchetto sui prezzi di mezza giornata a 7, 14 giorni e mese, se impostati, altrimenti prezzo × giorni.
- **Assenze**: i giorni di assenza non si pagano (anche nel «giornaliero con weekend», dove il legacy li faceva pagare).
- **Prezzo suggerito**: se attivo, sul pacchetto predefinito con «giornaliero con weekend», in un tratto di un giorno,
  vale la scelta del gestore: suggerito, originale o sconto (lo sconto solo sulle postazioni ammesse).
- **Cumulativo** (pacchetti e aggiuntivi con il cumulativo, giornata intera, almeno due postazioni dello stesso
  pacchetto e fila): prezzo sul totale dei giorni-postazione di ogni periodo, ripartito per giorni; il centesimo che
  avanza va all'ultima postazione.
- **Aggiuntivi**: stesso calcolo del pacchetto, per quantità; se l'aggiuntivo è legato a dei pacchetti vale solo con
  quelli e fino al massimo indicato; il pacchetto limita il totale degli aggiuntivi.
- **Convenzioni**: `agreementPriceListId` usa il listino della struttura convenzionata; `voucherPriceListId` sottrae il
  prezzo del voucher dal pacchetto, fino a 0.
- **Servizi**: prezzo (per periodo, se il servizio lo prevede), più la variante, o il prezzo della fascia per i servizi
  orari, per quantità. Si aggiungono i **prezzi automatici** validi (periodi, giorni, fasce orarie, file), una volta per
  ordine o per ogni servizio.
- **Supplemento online**: percentuale sulle righe o importo fisso, uno per le postazioni (impostazioni online) e uno
  per i servizi (impostazioni dei servizi).
- **Sconto**: uno solo, quello del codice o, senza, l'automatico valido più recente. Valido se attivo, nel canale, con
  utilizzi residui, con le date dentro un periodo di validità, nei giorni ammessi e, se ha fasce orarie, adesso dentro
  una fascia. `custom` vale solo sulle file e categorie scelte, `without_custom` su tutto il resto; percentuale sulle
  righe ammesse, fisso sul loro totale, mai sotto zero. Codice non valido: 422 `pricing.discount_not_valid`.
- **Regole online** (422): `pricing.online_booking_unavailable`, `pricing.past_dates`, `pricing.same_day_not_allowed`,
  `pricing.beyond_booking_horizon` (`args: { days }`), `pricing.half_day_not_allowed`, `pricing.seasonal_not_allowed`,
  `pricing.dates_not_allowed`, `pricing.spot_not_online`, `pricing.stay_limit` (`args: { minDays, maxDays }`),
  `pricing.package_not_online`, `pricing.service_not_online`, `pricing.manual_price_not_allowed`.
- **Non ancora disponibile** (Booking): disponibilità di postazioni e articoli, capienza dei servizi, conteggio degli
  utilizzi degli sconti, salvataggio della prenotazione con gli importi del preventivo.

## Snapshot di configurazione

Il JSON già pronto con tutta la configurazione di uno stabilimento per un uso preciso, letto in una sola chiamata e
riscaricato solo se è cambiato.

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /snapshots/{kind}` | `catalog.read` | stabilimento del token; tutti i tipi |
| `GET /public/bathhouses/{slug}/snapshots/{kind}` | nessuno (anche anonimo) | solo `public_showcase` e `beach_map` |
| `GET /public/media-origins` | nessuno (anche anonimo) | `{ origine: indirizzo base }`, con `external: ""` |
| `POST /admin/snapshots/rebuild` | `platform.manage` | `{ propertyIds?, kinds?, onlyMissing }` → `{ written, unchanged, removed, failed, errors }` |

- **Tipi** (`kind`):
  - `property_management_config`: avvio del gestionale (informazioni, impostazioni online e dei servizi, limiti di
    soggiorno, stagione e periodi, anche della stagione successiva (`nextSeasonTariffPeriods`), file, Revenue, aliquote, vendite sospese, conteggi di pacchetti, aggiuntivi, sconti,
    servizi, magazzino e convenzioni);
  - `beach_map`: `{ map, legend, onlineSpotIds }`, come `GET /map`;
  - `public_showcase`: la scheda pubblica, come `GET /public/bathhouses/{slug}`;
  - `price_catalog`: periodi, file, metodi di calcolo, listino completo, stagionali ridotti, sconti e prezzi suggeriti
    già scelti dal gestore;
  - `services_catalog`: servizi completi, prezzi automatici, servizi tavolo, impostazioni online dei servizi;
  - `agreement_catalog`: strutture convenzionate con i loro listini e voucher.
- **Forma**: `{ schema, kind, data }`; `schema` è la versione della forma del JSON (oggi 1). `data` ha la stessa forma
  delle API corrispondenti, ma i file sono riferimenti `{ origin, path }`: l'URL è `origins[origin] + "/" + path`, con
  le origini di `GET /public/media-origins`. Gli URL esterni restano stringhe.
- **Cache**: la risposta ha `ETag`; rimandandolo in `If-None-Match` si riceve `304` senza corpo se non è cambiato.
- **Esistenza**: gli snapshot del gestionale esistono per gli stabilimenti non importati e fuori dal cestino,
  `public_showcase` solo per le strutture pubblicate, `agreement_catalog` solo se ci sono convenzioni; altrimenti 404
  `catalog.snapshot_not_found`.
- **Aggiornamento**: si rigenerano dopo ogni modifica che li riguarda (solo gli stabilimenti e i tipi toccati), ogni
  notte tutti, all'avvio del backend quelli che mancano, e a mano con `POST /admin/snapshots/rebuild`. Se uno snapshot
  manca alla lettura si costruisce al momento.

## Magazzino

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /inventory-articles` | `inventory.read` | elenco fisso degli articoli: `[{ code, mapPlaceable }]` |
| `GET /inventory-items` | `inventory.read` | `search`; `sortBy` = `name` (default), `totalQuantity`, `lowStockThreshold` |
| `GET /inventory-items/lookup` | `inventory.read` | tendina: `[{ id, name, articleCode }]` |
| `GET /inventory-items/{id}` | `inventory.read` | |
| `POST /inventory-items` | `inventory.manage` | `{ articleCode, name, totalQuantity, lowStockThreshold, lowStockAlertEnabled }` |
| `PUT /inventory-items/{id}` | `inventory.manage` | stesso corpo |
| `DELETE /inventory-items/{id}` | `inventory.manage` | 409 `inventory.item_in_use` se è in un pacchetto o articolo aggiuntivo (anche delle convenzioni), con `args: { priceLists, agreementPriceLists }` |
| `POST /inventory-items/bulk-delete` | `inventory.manage` | esito parziale |

`InventoryItemDto` = `{ id, articleCode, name, totalQuantity, lowStockThreshold, lowStockAlertEnabled, mapPlaceable }`.

- `articleCode` null = articolo inserito con «Altro». Per un articolo dell'elenco il frontend propone come nome
  l'etichetta tradotta; il nome resta modificabile (es. «Gazebo 1», «Gazebo 2").
- Quantità ≥ 0; soglia da 0 alla quantità, facoltativa.
- Nome unico per stabilimento senza distinguere maiuscole e minuscole: 409 `inventory.name_taken`.
- **Non ancora disponibile** (dipende da Booking): la disponibilità per data e fascia (`availableQuantity`) e
  l'invio dell'avviso soglia nella campanella.

## Stagioni e periodi

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /season` | `catalog.read` | `{ startsOn, endsOn, nextStartsOn, nextEndsOn }`; 204 se lo stabilimento non ha ancora una stagione |
| `PUT /season` | `catalog.settings` | crea o aggiorna; la stagione successiva (facoltativa, entrambe le date) inizia dopo la fine della corrente e serve al rinnovo |
| `GET /tariff-periods` | `catalog.read` | `[{ key, name, ranges: [{ startsOn, endsOn }] }]`, in ordine di data |
| `POST /tariff-periods` | `catalog.settings` | `{ name, ranges }`; 201 |
| `PUT /tariff-periods/{key}` | `catalog.settings` | sostituisce nome e intervalli |
| `DELETE /tariff-periods/{key}` | `catalog.settings` | 409 `pricing.period_in_use` se ci sono prezzi del listino (anche delle convenzioni) per il periodo |

| `GET /next-season/tariff-periods` | `catalog.read` | periodi della **stagione successiva**, stessa forma |
| `POST /next-season/tariff-periods` · `PUT /{key}` · `DELETE /{key}` | `catalog.settings` | come sopra, dentro le date della stagione successiva; 422 `pricing.next_season_required` se non è impostata |

- `key` è il numero del periodo nella stagione (i prezzi del listino vi fanno riferimento), non un uuid.
- **Stagione successiva** (dal 2026-11-05): i suoi periodi si preparano in anticipo e stanno a parte (tabella
  `pricing_next_season_periods`): listini, preventivi e ricerca usano solo quelli della stagione corrente, e non c'è il
  controllo dei prezzi collegati. Togliendo la stagione successiva da `PUT /season` si tolgono anche i suoi periodi. Il
  passaggio dei periodi alla stagione corrente quando questa cambia non è ancora fatto (PROMEMORIA-85-19).
- Un periodo può avere più intervalli, non sovrapposti tra loro e dentro la stagione corrente (422
  `pricing.period_outside_season` con le date della stagione). Periodi **diversi** possono sovrapporsi: per un giorno
  coperto da più periodi vale il più breve ([analisi-post/bl/03](../analisi-post/bl/03-precedenza-dei-periodi-tariffari.md)).
- Nome unico nella stagione (409 `pricing.period_name_taken`). Senza stagione: 422 `pricing.season_required`.
- Le «fasce» del gestore sono questi periodi (bassa, alta, altissima…). I pesi del prezzo suggerito usano invece i
  periodi standard del systemadmin (fase 3).

## Settori e file

| Metodo e percorso | Permesso | Note |
|---|---|---|
| `GET /sectors` | `catalog.read` | `[{ id, name, sortOrder }]` |
| `POST /sectors` | `catalog.settings` | `{ name }`; in fondo all'ordine |
| `PUT /sectors/{id}` | `catalog.settings` | rinomina |
| `DELETE /sectors/{id}` | `catalog.settings` | 409 `catalog.sector_in_use` con `args: { mapSpots, priceLists, stayLimits, discounts }` |

Settore e fila sono la stessa cosa. Nome unico per stabilimento senza distinguere maiuscole e minuscole (409
`catalog.sector_name_taken`).

## Mappa

Impostazioni › Configura mappa e schermata Configurazione mappa. Lettura `catalog.read`, modifiche `catalog.settings`.

| Metodo e percorso | Note |
|---|---|
| `GET /map` | `{ settings, spots, sectors, beachElements }`: tutto ciò che serve a disegnare la mappa (file `[{ id, name, sortOrder }]` ed elementi spiaggia usati `[{ id, name, iconUrl }]`) |
| `GET /map/settings` | solo le impostazioni |
| `PUT /map/settings` | `{ rows, columns, tiltDegrees, backgroundColor, backgroundIconId, resetDeletesBookings }`; crea il layout se manca |
| `POST /map/background` | multipart, campo `file`: sfondo caricato (png, jpeg, webp, massimo 5 MB) |
| `DELETE /map/background` | nessuno sfondo |
| `PUT /map/icons/umbrella` | `{ freeIconId, occupiedIconId }`; null = icona predefinita |
| `PUT /map/icons/custom-articles/{variant}` | stesso corpo, `variant` da 1 a 8 |
| `GET /map/icon-catalog?category=` | icone attive del catalogo: `template` (sfondi), `umbrella`, `tent`, `sunbed`, `cabin` |
| `GET /map/beach-elements` | elementi spiaggia attivi `[{ id, name, iconUrl }]` |
| `POST /map/spots` | riempie le caselle selezionate (201 con le postazioni create) |
| `PUT /map/spots/{id}` | `{ label, sectorId, areaSeatCount, isAgreementOnly }` |
| `PUT /map/spots/placements` | `{ items: [{ id, rotationDegrees, offsetX, offsetY }] }`, esito parziale |
| `DELETE /map/spots/{id}` · `POST /map/spots/bulk-delete` | eliminazione |
| `POST /map/rows` · `POST /map/columns` | `{ at }`: nuova riga o colonna in posizione `at` (da 1 a quante sono + 1) |
| `DELETE /map/rows/{index}` · `DELETE /map/columns/{index}` | elimina riga o colonna con il contenuto; le successive scorrono indietro |
| `POST /map/rows/{index}/clear` · `POST /map/columns/{index}/clear` | svuota: la struttura resta |
| `POST /map/reset` | «Reimposta mappa»: postazioni eliminate, reticolo a 0 × 0; sfondo, icone e impostazioni restano |

Le operazioni su righe, colonne e reset rispondono con la mappa intera (`{ settings, spots }`).

**Impostazioni** (`MapSettingsDto`): `rows` (file, massimo 50), `columns` (articoli per fila, massimo 80),
`tiltDegrees` (da −45 a 45), `backgroundColor` (`#RRGGBB`), `background: { iconId, url, isCustom }`,
`icons: { umbrella: { free, occupied }, customArticles: [{ variant, icons: { free, occupied } }] }` con ogni icona
`{ iconId, url, isDefault }`, `resetDeletesBookings`.

- Ridurre `rows`/`columns` sotto l'ultima postazione dà 409 `map.spots_outside_grid` con `args: { rows, columns }`
  (dimensioni minime): per togliere contenuto si eliminano righe o colonne.
- `backgroundIconId` sceglie uno sfondo del catalogo e scarta quello caricato; `null` mantiene lo sfondo caricato,
  se c'è.
- Le icone si scelgono solo dal catalogo, categoria `umbrella`, con lo stato giusto (422 `map.icon_not_allowed`).
  Un'icona senza scelta mostra la predefinita (`isDefault: true`); `iconId` null con `url` valorizzato è un'icona
  caricata nel vecchio gestionale.
- `url` è l'URL pubblico del file, composto da `Storage:PublicBaseUrls` (chiavi `s3_us_east_2`, `s3_us_east_1`,
  `cloudfront`); senza configurazione è null.

**Postazioni** (`MapSpotDto`): `{ id, row, column, rowSpan, columnSpan, spotType, label, sectorId, areaSeatCount,
isAgreementOnly, rotationDegrees, offsetX, offsetY, beachElementId }`. Righe da 1 = lato mare, colonne da 1 =
sinistra; `row`/`column` sono l'angolo in alto a sinistra.

- `spotType`: `umbrella`, `sunbed`, `sunbed_xl`, `cabin`, `custom_cabin`, `custom_article1`…`custom_article8`,
  `tent`, `parking`, `ticket`, le aree `umbrella_area`, `cabin_area`, `sunbed_area`, `parking_area`, e
  `beach_element`.
- **Riempimento** (`POST /map/spots`): `{ cells: [{ row, column }], spotType, firstLabel, sectorId, areaSeatCount,
  isAgreementOnly, beachElementId, numbering }`, massimo tutta la griglia (4.000 caselle).
  - Articoli e aree: una postazione per casella, numerate da `firstLabel` («A1» → A1, A2…; «A01» → A02; «A» → A1,
    A2…); un'area chiede `areaSeatCount`.
  - `numbering: { vertical, seaToInland, leftToRight }` (default: lungo le righe, dal mare, da sinistra).
  - Elemento spiaggia: `beachElementId`; un solo elemento sull'intero rettangolo selezionato (422
    `map.selection_not_rectangular` se la selezione non è un rettangolo pieno).
  - Caselle fuori dal reticolo: 422 `map.cell_outside_grid`; già occupate: 409 `map.cell_occupied` (si svuotano
    prima); numero già usato, senza distinguere maiuscole e minuscole: 409 `map.label_taken` con `args: { label }`.
- Un elemento spiaggia non si modifica (422 `map.beach_element_not_editable`): si elimina e si reinserisce.
- Rotazione a passi di 45° (0–315), spostamento in quinti di casella da −2 a 2 (0 = centro). Non vanno nel
  registro.
- Inserendo o eliminando una riga o colonna che attraversa un elemento spiaggia, l'elemento si allarga o si riduce.
- **Non ancora disponibile** (dipende da Booking, PROMEMORIA-85-16): l'effetto di eliminazioni, svuotamenti e reset
  sulle prenotazioni secondo `resetDeletesBookings`. La gestione del catalogo di icone ed elementi spiaggia è del
  systemadmin (incremento successivo).

## Errori del modulo

| Status | `code` | Quando |
|---|---|---|
| 403 | `catalog.property_required` | token senza stabilimento |
| 404 | `catalog.property_not_found` | stabilimento del token inesistente o eliminato |
| 422 | `catalog.invalid_image` · `catalog.invalid_pdf` | file non ammesso o troppo grande (`args: { maxBytes, contentTypes }`) |
| 422 | `catalog.country_not_found` | codice del Paese sconosciuto |
| 409 | `catalog.slug_taken` | slug già usato (`args: { slug }`) |
| 422 | `catalog.amenity_not_found` | codici delle dotazioni sconosciuti (`args: { codes }`) |
| 404/422 | `catalog.photo_not_found` | foto inesistente; nel riordino, elenco che non corrisponde alla galleria |
| 422 | `catalog.gallery_full` | galleria al massimo (`args: { max }`) |
| 422 | `catalog.too_many_files` | troppe foto nella registrazione (`args: { max }`) |
| 422 | `catalog.human_verification_failed` | verifica anti-robot non superata |
| 404 | `catalog.stay_limit_not_found` | limite di soggiorno inesistente |
| 409 | `catalog.stay_limit_overlap` | periodo sovrapposto a un altro limite della stessa fila |
| 422 | `catalog.vat_rate_not_allowed` | aliquota non prevista per il Paese dello stabilimento (`args: { rates }`) |
| 404 | `service.not_found` | servizio inesistente o di un altro stabilimento |
| 422 | `service.kind_immutable` | cambio del tipo di servizio |
| 404 | `service.icon_not_found` | icona del servizio inesistente |
| 404 | `service.automatic_price_not_found` | prezzo automatico inesistente |
| 404 | `service.table_service_not_found` | servizio tavoli inesistente |
| 404 | `pricing.price_list_not_found` | voce di listino inesistente |
| 422 | `pricing.price_list_kind_immutable` | cambio da pacchetto ad aggiuntivo o viceversa |
| 422 | `pricing.calculation_method_not_found` | metodo di calcolo sconosciuto |
| 404 | `pricing.reduced_season_not_found` | voce «stagionale ridotto» inesistente |
| 422 | `pricing.import_file_invalid` | file illeggibile o con intestazione diversa (`args: { headers }`) |
| 422 | `pricing.import_price_invalid` | prezzo non numerico in una riga importata |
| 404 | `pricing.discount_not_found` | sconto inesistente |
| 409 | `pricing.discount_code_taken` | codice sconto già usato (`args: { code }`) |
| 422 | `pricing.discount_max_uses_below_used` | massimo inferiore agli utilizzi già fatti (`args: { usedCount }`) |
| 404 | `agreement.partner_not_found` | struttura convenzionata inesistente |
| 404 | `agreement.report_not_found` | report inesistente |
| 404 | `catalog.bathhouse_not_found` | lo stabilimento non ha ancora la pagina della vetrina |
| 409 | `catalog.property_in_trash` | stabilimento nel cestino: va prima ripristinato |
| 409 | `catalog.property_not_in_trash` | ripristino o eliminazione definitiva di uno stabilimento che non è nel cestino |
| 404 | `catalog.badge_not_found` · `catalog.region_not_found` | badge o regione inesistente |
| 409 | `catalog.region_name_taken` | regione con lo stesso nome nello stesso Paese |
| 404 | `map.icon_category_not_found` | categoria di icone inesistente |
| 409 | `map.icon_is_default` | l'icona predefinita non si elimina: prima se ne sceglie un'altra |
| 422 | `map.icon_default_required` | serve sempre un'icona predefinita per categoria e stato |
| 409 | `map.beach_element_in_use` | elemento usato sulle mappe (`args: { usageCount }`): si può disattivare |
| 422 | `pricing.not_a_package` · `pricing.not_an_additional` | voce di listino del tipo sbagliato |
| 422 | `pricing.package_not_for_sector` | pacchetto limitato ad altre file |
| 422 | `pricing.additional_not_linked` · `pricing.too_many_additionals` | aggiuntivo non legato al pacchetto o oltre il massimo (`args: { max }`) |
| 404 | `pricing.variant_not_found` · `pricing.time_slot_not_found` · `pricing.voucher_not_found` | variante, fascia o voucher inesistente |
| 422 | `pricing.discount_not_valid` | codice sconto inesistente o non valido per quelle date e quel canale |
| 404 | `catalog.snapshot_not_found` | snapshot inesistente per quel tipo o quello stabilimento |
| 404 | `pricing.suggested_price_not_found` | nessun prezzo suggerito per il giorno e la fila indicati: va prima calcolato |
| 404 | `inventory.item_not_found` | articolo inesistente o di un altro stabilimento |
| 409 | `inventory.name_taken` | nome già usato |
| 409 | `inventory.item_in_use` | articolo incluso in pacchetti o articoli aggiuntivi |
| 422 | `pricing.season_required` | periodo senza stagione impostata |
| 404 | `pricing.period_not_found` | periodo inesistente |
| 409 | `pricing.period_name_taken` | nome del periodo già usato nella stagione |
| 422 | `pricing.period_outside_season` | intervallo fuori dalla stagione corrente |
| 409 | `pricing.period_in_use` | periodo con prezzi del listino |
| 404 | `catalog.sector_not_found` | settore inesistente o di un altro stabilimento |
| 409 | `catalog.sector_name_taken` | nome del settore già usato |
| 409 | `catalog.sector_in_use` | settore usato da postazioni, listini, limiti o sconti |
| 404 | `map.spot_not_found` | postazione inesistente o di un altro stabilimento |
| 422 | `map.cell_outside_grid` | casella fuori dal reticolo (`args: { row, column }`) |
| 409 | `map.cell_occupied` | casella già occupata (`args: { row, column }`) |
| 409 | `map.label_taken` | numero della postazione già usato (`args: { label }`) |
| 409 | `map.spots_outside_grid` | reticolo ridotto sotto le postazioni presenti |
| 422 | `map.index_outside_grid` | riga o colonna inesistente (`args: { index, max }`) |
| 422 | `map.grid_full` | reticolo già al massimo di righe o colonne |
| 422 | `map.selection_not_rectangular` | elemento spiaggia su una selezione non rettangolare |
| 422 | `map.beach_element_not_editable` | modifica di un elemento spiaggia |
| 404 | `map.icon_not_found` | icona inesistente o disattivata; articolo personalizzato fuori da 1–8 |
| 422 | `map.icon_not_allowed` | icona di un'altra categoria o stato (`args: { category, state }`) |
| 404 | `map.beach_element_not_found` | elemento spiaggia inesistente o disattivato |
| 422 | `map.invalid_image` | file non immagine o troppo grande (`args: { maxBytes, contentTypes }`) |
