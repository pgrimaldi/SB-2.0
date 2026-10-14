# Tabella `contact_list`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **125 righe**, query
> completa; `created_at` dal 2025-07-10 al 2026-08-13; **`propertyId` distribuito su molti stabilimenti**
> — non più solo `96` come nel DB dev), model
> [`back-pre/app/models/contactList.js`](../../../../../../back-pre/app/models/contactList.js)
> (`sequelize.define("contact_list", …, { freezeTableName: true, timestamps: false })` → nome
> invariato, timestamp `created_at`/`updated_at` **gestiti a mano** con `defaultValue: Sequelize.NOW`),
> registrato come `db.contact_list` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga
> 182). Referenziata (FK reale) da
> [`marketing_email_campaign`](marketing_email_campaign.md)`.contactListId` e
> [`marketing_automation`](marketing_automation.md)`.contactListId` (`ON DELETE SET NULL`); nessuna FK
> in uscita. Associazioni ORM `belongsTo`/`hasMany` con `marketing_email_campaign` e
> `marketing_automation` in [`models/index.js:381-393`](../../../../../../back-pre/app/models/index.js).
>
> **Scrittori** —
> [`Service/marketingContactListService.js`](../../../../../../back-pre/app/Service/marketingContactListService.js)
> via [`controllers/marketingContactList.controller.js`](../../../../../../back-pre/app/controllers/marketingContactList.controller.js)
> (route in [`routes/marketing.routes.js`](../../../../../../back-pre/app/routes/marketing.routes.js), **senza
> alcun middleware di auth**):
> - `createContactList` (`POST /api/sb/create-contact-list`) — crea la lista + i
>   [`contact_list_member`](contact_list_member.md) (`bulkCreate`).
> - `updateContactList` (`PUT /api/sb/update-contact-list`) — aggiorna **solo `name`**; se
>   `contactIds` è presente, `destroy` di tutti i membri e `bulkCreate` dei nuovi.
> - `deleteContactList` (`DELETE /api/sb/delete-contact-list/:id`) — **hard delete**:
>   `ContactListMember.destroy({ where: { contactListId } })` + `contactList.destroy()`.
> - `duplicateContactList` (`POST /api/sb/contact-list/duplicate`) — copia con nome `…_copy[_N]`.
> - `createDefaultContactListAndAutomation` (`POST /api/sb/create-default-contact-list`) — crea la
>   lista `name: "Marketing"`, `isDefaultList: 1` + due [`marketing_automation`](marketing_automation.md)
>   ("Welcome", "Goodbye") via `bulkCreate`.
> - [`Service/marketingContactListService.js`](../../../../../../back-pre/app/Service/marketingContactListService.js)
>   `addCustomer(propertyId, customerId)` — chiamata da
>   [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) quando
>   `req.body.data[0].isMarketingCampaign == true`: trova la lista `isDefaultList: 1, status: 1` della
>   property e vi aggiunge il cliente (solo lettura di `contact_list`, scrive
>   `contact_list_member`).
>
> **Lettori**:
> - `getAllContactLists` (`POST /api/sb/get-contact-lists`) — raw SQL `WHERE propertyId = :propertyId
>   AND status = 1` (opzionale `AND isDefaultList = :isDefaultList`).
> - `getContactList` (`GET /api/sb/get-contact-list/:id`) — raw SQL join
>   `contact_list_member` + `customers`, `cl.status = 1` (`id` **interpolato**, non parametrizzato).
> - `getCampaignById` / `getAutomationById` — join `cl.status = 1`.
>
> **Frontend** (`front-pre`, copia `front-customer-pre`): sezione **Marketing → Contatti**
> ([`components/Marketing/Component/Contact/ContactsList.js`](../../../../../../front-pre/src/components/Marketing/Component/Contact/ContactsList.js),
> `Filter.js`), via
> [`data/actions/marketingActions.js`](../../../../../../front-pre/src/data/actions/marketingActions.js)
> (`sb/get-contact-lists`, `sb/create-contact-list`, `sb/update-contact-list`,
> `sb/delete-contact-list/:id`, `sb/contact-list/duplicate`, `sb/create-default-contact-list`,
> `sb/customer-list`). Esiste una copia dell'intera sezione in
> `components/newsLetter/Marketing/…`.

---

## Nota di contesto

**Liste di contatti** (segmenti di clientela) per l'invio di campagne email e automazioni marketing di
uno stabilimento. Una lista raccoglie N clienti ([`customers`](customers.md)) tramite la tabella ponte
[`contact_list_member`](contact_list_member.md). Ogni property ha (o dovrebbe avere) **una** lista
`isDefaultList: 1` chiamata "Marketing", creata insieme alle automazioni Welcome/Goodbye, in cui i
clienti confluiscono automaticamente al momento della prenotazione se il flag
`bookings.isMarketingCampaign` è attivo.

**Dati reali di produzione** (`sb_prod_dump_2026_09_08`, 125 liste): **tutte** `status = 1`; **99** con
`isDefaultList = 1` (nome `"Marketing"`) → circa 99 stabilimenti hanno la lista di default (feature
marketing adottata da ~100 property); le 26 liste non-default sono per lo più segmenti reali
(`"Seasonal Customer"`, `"tutti i clienti fino a 16 luglio"`, `"Marketing (ALL 2026)"`) o di prova
(`"test"`, `"prod test"`). `id` va da 2 a 241 → un centinaio di liste create e **hard-deleted** nel
tempo.

**Dominio nella riscrittura**: **C6 — Comunicazione**, sotto-area Marketing. Il segmento va modellato
come lista + membri con FK reali e soft-delete coerente; la "lista di default" e l'auto-iscrizione da
prenotazione sono regole del modulo Marketing, non della prenotazione.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `2` … `241` per 125 righe.

### name
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì (model `allowNull: true`; il frontend lo invia sempre)
- **Descrizione**: nome della lista. Il duplicato genera `"<name>_copy"`, `"<name>_copy_1"`, …
  (`generateCopyName`). La lista di default è creata con `name: "Marketing"`.
- **Esempio di valori** (prod): `"Marketing"` (92 righe — le liste di default), `"Seasonal Customer"`
  (5), `"Marketing (ALL 2026)"`, `"tutti i clienti fino a 16 luglio"`, e vari `"test*"`.

### propertyId
- **Tipo**: `int(11)`
- **Nullabile**: Sì (model `allowNull: true`; sempre valorizzato nei dati)
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**. Scoping di tutte le query.
- **Esempio di valori** (prod): `10587`, `10590`, `228`, `10578` — range `42` … `10855` (molti
  stabilimenti, non solo la property 96).

### status
- **Tipo**: `int(11)`, `DEFAULT 1`
- **Nullabile**: Sì
- **Descrizione**: pensato come flag attivo/eliminato (i lettori filtrano `status = 1`), ma
  **nessun percorso lo porta a un valore diverso da `1`**: `deleteContactList` fa `destroy()`
  (hard delete), non `update({ status: 0 })`. **In produzione: `1` su tutte le 125 righe.**
- **Enumeratore**: no — `enum ActiveState` teorico, oggi inutilizzato.
- **Esempio di valori** (prod): `1`
- **Deprecato**: **parzialmente** — filtrato in lettura ma mai scritto ≠ `1`; il delete è fisico.

### created_at / updated_at
- **Tipo**: `datetime`, `DEFAULT NULL` a schema; il model imposta `defaultValue: Sequelize.NOW` (con
  `timestamps: false`, quindi valorizzati **solo alla create**, mai aggiornati sugli update).
- **Nullabile**: Sì (a schema) — nei dati sempre valorizzati (`cnull = 0`)
- **Descrizione**: `updated_at` **non viene aggiornato** su `updateContactList` (timestamps disattivati
  e nessun set manuale) → resta uguale a `created_at`.
- **Esempio di valori** (prod): range `2025-07-10 04:29:50` … `2026-08-13 15:19:03`.

### isDefaultList
- **Tipo**: `int(11)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: `1` = lista "Marketing" predefinita della property, bersaglio dell'auto-iscrizione
  da prenotazione (`addCustomer`) e creata da `createDefaultContactListAndAutomation`. Nessun vincolo
  di unicità: teoricamente una property potrebbe averne più di una.
- **Enumeratore**: no — booleano (`bool is_default`).
- **Esempio di valori** (prod): `1` (99 righe), `0` (26).

---

## Osservazioni verificate nel codice (criticità)

- **Route senza autenticazione**: `marketing.routes.js` non applica alcun `authJwt.*` alle route
  contact-list (né `optionalAuth`). `propertyId` arriva dal body/query → chiunque può creare, leggere
  (con i contatti: nome, email, telefono dei clienti), modificare o cancellare liste di qualsiasi
  stabilimento.
- **Hard delete + churn**: `deleteContactList` e `updateContactList` distruggono fisicamente i membri;
  in produzione [`contact_list_member`](contact_list_member.md) ha **29.488 righe vive** ma `id` fino a
  **63.649** → oltre metà dei membri storici sono stati hard-deleted. Nessuna storicizzazione di chi
  era in quale lista.
- **`updated_at` mai aggiornato** (timestamps disattivati, nessun set manuale sugli update).
- **`status` non usato per il delete** → colonna semi-morta; il concetto "lista archiviata" non esiste.
- **SQL injection**: `getContactList` e `duplicateContactList` interpolano `id` nella query raw;
  `getFilteredCustomers` interpola `searchName`, `propertyId`, età, date.
- **Nessun vincolo di unicità** su `(propertyId, isDefaultList = 1)`.
- **FK in ingresso `ON DELETE SET NULL`** da campagne/automazioni: cancellare una lista lascia le
  campagne collegate con `contactListId = NULL` (ma l'invio usa comunque lo snapshot `recipientList`,
  vedi [`marketing_email_campaign`](marketing_email_campaign.md)).

### Campi candidati a enumeratore

Nessun campo di questa tabella è un enumeratore: `status` → `bool`/`enum ActiveState` (concetto
condiviso, oggi non usato); `isDefaultList` → `bool`.
