# Tabella `contact_list_member`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **29.488 righe**, query
> completa; `id` fino a 63.649 → ~metà dei membri storici hard-deleted; `status` sempre `1`;
> `created_at` dal 2025-07-10 al 2026-08-13), model
> [`back-pre/app/models/contactListMember.js`](../../../../../../back-pre/app/models/contactListMember.js)
> (`sequelize.define("contact_list_member", …, { freezeTableName: true, timestamps: false })`,
> `created_at`/`updated_at` con `defaultValue: Sequelize.NOW`), registrato come `db.contact_list_member`
> in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 183). **Nessuna associazione ORM,
> nessuna FK, nessun indice** oltre la PK (pur essendo una pura tabella ponte).
>
> **Scrittori** —
> [`Service/marketingContactListService.js`](../../../../../../back-pre/app/Service/marketingContactListService.js):
> - `createContactList` / `updateContactList` / `duplicateContactList` — `ContactListMember.bulkCreate(
>   contactIds.map(contactId => ({ contactId, contactListId })))`.
> - `updateContactList` / `deleteContactList` — `ContactListMember.destroy({ where: { contactListId } })`
>   (rimozione **fisica** di tutti i membri prima di ricrearli / prima di cancellare la lista).
> - `addCustomer(propertyId, customerId)` — chiamata dal flusso prenotazione quando
>   `bookings.isMarketingCampaign` è attivo: se non esiste già una riga
>   `(contactListId = <lista default>, contactId = customerId)`, la crea (unico punto con
>   de-duplicazione).
>
> **Lettori** — solo in JOIN raw SQL (mai via ORM): `getContactList`, `duplicateContactList`,
> `getCampaignById` ([`marketing_email_campaign`](marketing_email_campaign.md)), `getAutomationById`
> ([`marketing_automation`](marketing_automation.md)):
> `JOIN contact_list_member clm ON cl.id = clm.contactlistId JOIN customers c ON clm.contactId = c.id`.
> Nessuna di queste query filtra su `clm.status`.
>
> **Frontend**: nessun accesso diretto — i membri arrivano dentro il campo `contacts` (array JSON)
> della risposta di `getContactList` / `getCampaignById` / `getAutomationById`.

---

## Nota di contesto

Tabella **ponte molti-a-molti** fra [`contact_list`](contact_list.md) e [`customers`](customers.md):
una riga = "il cliente X è nella lista Y". Malgrado il nome `contactId`, il riferimento è a
`customers.id` (verificato: nelle query di join `clm.contactId = customers.id`; 0 righe orfane lato
contatto).

Stato dei dati (**produzione**, `sb_prod_dump_2026_09_08`):
- **29.488 righe vive**, `id` fino a **63.649** → oltre metà dei membri storici sono stati distrutti:
  ogni modifica a una lista fa `destroy` + `bulkCreate` di tutti i membri.
- **3.960 righe (≈13 %) hanno `contactId` `NULL`** — membri senza cliente collegato (dato sporco non
  segnalato prima; nessun `NOT NULL`, nessuna FK).
- `contactListId` va da 1 a 241 (coerente con l'`id` massimo di [`contact_list`](contact_list.md)).
- `status` sempre `1`.

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing**. Va rifatta come vera tabella ponte:
FK verso lista e verso il contatto, `UNIQUE(list_id, contact_id)`, soft-delete o rimozione puntuale
(non "cancella tutti e ricrea"), niente id in churn.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `63.649` per 29.488 righe vive.

### contactId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`customers`](customers.md)`.id` — **nessuna FK**. Nome fuorviante ("contact" ma è un
  `customers.id`). **In produzione 3.960 righe (13%) hanno `contactId` `NULL`.**
- **Descrizione**: cliente incluso nella lista. In `createContactList`/`updateContactList` proviene da
  `req.body.contactIds` (array); in `addCustomer` è il `customerId` della prenotazione.
- **Esempio di valori** (prod): `67816`, `67678`, `67550` — range `3.428` … `218.175`.

### contactListId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`contact_list`](contact_list.md)`.id` — **nessuna FK**.
- **Descrizione**: lista di appartenenza.
- **Esempio di valori** (prod): `1`, `2`, `3`, `5` — range `1` … `241`.

### status
- **Tipo**: `int(11)`, `DEFAULT 1`
- **Nullabile**: Sì
- **Descrizione**: mai impostato a un valore diverso da `1` (la rimozione di un membro è una
  `destroy` fisica); nessuna query di lettura lo filtra.
- **Esempio di valori**: `1`
- **Deprecato**: **Sì** — scritto solo col default, mai letto, mai aggiornato.

### created_at / updated_at
- **Tipo**: `datetime`, `DEFAULT NULL` a schema; model `defaultValue: Sequelize.NOW`, `timestamps:
  false` → valorizzati alla `bulkCreate`, mai aggiornati.
- **Nullabile**: Sì (a schema) — nei dati `created_at` sempre valorizzato
- **Descrizione**: `created_at` = momento dell'inserimento (di fatto il momento dell'ultima modifica
  della lista, per via del destroy+recreate). `updated_at` = uguale.
- **Esempio di valori** (prod): range `2025-07-10 04:04:22` … `2026-08-13 15:19:03`.

---

## Osservazioni verificate nel codice (criticità)

- **13 % di righe con `contactId` `NULL`** (produzione): membri senza cliente collegato; nessuna FK,
  nessun `NOT NULL`. Da bonificare in migrazione.
- **Churn degli id** (`id` fino a ~63,6 k per 29,5 k righe vive): `updateContactList` cancella e
  ricrea **tutti** i membri anche solo per rinominare la lista se il client rimanda `contactIds`.
- **Nessun `UNIQUE(contactListId, contactId)`**: solo `addCustomer` de-duplica in applicazione;
  `bulkCreate` può inserire doppioni se `contactIds` contiene ripetizioni.
- **`status` colonna morta**; **`updated_at` mai aggiornato**.
- **Nome `contactId` fuorviante** (è `customers.id`, non un id di "contatto" separato).
- Nessuna FK verso `contact_list` né verso `customers`; la coerenza è solo nelle JOIN raw.

### Campi candidati a enumeratore

Nessuno: `status` è un `int` inutilizzato da modellare come `bool`/eliminare.
