# Tabella `parent_bookings`

> Fonti: schema; **0 righe sia nel DB dev sia nel dump di produzione `sb_prod_dump_2026_09_08`**
> (esportata solo come schema, nessun `INSERT`) → conferma definitiva: mai usata. Model
> [`back-pre/app/models/parent_booking.model.js`](../../../../../../back-pre/app/models/parent_booking.model.js)
> (`sequelize.define("parent_bookings", ...)`), registrato in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js) come `db.parent_booking`.
>
> **Ricerca completa in `back-pre`**: l'unico riferimento è
> [`helpers/booking.helper.js:17`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> `const parentbooking = db.parentbooking;` — che punta a `db.parentbooking` (**inesistente**, il
> model è `db.parent_booking`), quindi `parentbooking` è `undefined` e non viene mai usato. Nessun
> `create`/`update`/`findAll`. Nel model `booking.modle.js` un campo `parent_booking_id` è **commentato**.
> Nessun riferimento nei frontend.
>
> **Nota di contesto** — struttura prevista per raggruppare più prenotazioni sotto un "pagamento
> genitore" (es. un unico pagamento online che copre più prenotazioni / più giorni-postazione). **Mai
> implementata**: rimane solo lo scheletro.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. (nessun dato reale)

### payment
- **Tipo**: `longtext` (`Sequelize.JSON`)
- **Nullabile**: Sì
- **Descrizione**: previsto per contenere i dati del pagamento aggregato (importo, riferimento Stripe,
  ecc.). Mai popolato.

### status
- **Tipo**: `int` — (commento nel model: *"defalut-0 , linksent -1 , cancel -2 , success -3"*)
- **Nullabile**: Sì
- **Descrizione**: stato previsto del pagamento genitore: `0` default, `1` link inviato, `2` annullato,
  `3` completato. Mai scritto.
- **Candidato a enumerazione**: **Sì** (`ParentPaymentStatus`) — ma solo se la feature viene
  ripristinata; altrimenti irrilevante.

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì

---

## Osservazioni verificate nel codice (criticità)

- **Deprecato / dead code** — *riverificato*: `grep -rn "parent_booking|parentbooking|ParentBooking"
  back-pre/` trova solo: la definizione del model (`models/parent_booking.model.js`,
  `models/index.js` → `db.parent_booking`), un import errato in `helpers/booking.helper.js:17`
  (`const parentbooking = db.parentbooking;` → `db.parentbooking` **non esiste**, quindi `undefined`),
  e un campo `parent_booking_id` **commentato** in `booking.modle.js`. **Nessun**
  `create`/`update`/`findAll`. **`0` righe anche in produzione** (`sb_prod_dump_2026_09_08`). Da **non migrare**.
- Il concetto di "pagamento che copre più prenotazioni" è oggi coperto in modo diverso da
  [`bookings_send_payment_summaries`](bookings_send_payment_summaries.md) (link Stripe per
  prenotazione) e dai `depositList`/`paidAmountData` nel JSON `bookings.booking_payments`. Se in
  riscrittura serve un vero "ordine/pagamento multi-prenotazione", va progettato ex novo.
