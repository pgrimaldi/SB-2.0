# Tabella `contact_list_member`

> Fonti: schema e dati reali del DB dev `summer_booking_dev` (**7 626 righe**, `AUTO_INCREMENT=231812`,
> `status` sempre `1`, `created_at` sempre valorizzato), model
> [`back-pre/app/models/contactListMember.js`](../../back-pre/app/models/contactListMember.js)
> (`sequelize.define("contact_list_member", …, { freezeTableName: true, timestamps: false })`,
> `created_at`/`updated_at` con `defaultValue: Sequelize.NOW`), registrato come `db.contact_list_member`
> in [`models/index.js`](../../back-pre/app/models/index.js) (riga 183). **Nessuna associazione ORM,
> nessuna FK, nessun indice** oltre la PK (pur essendo una pura tabella ponte).
>
> **Scrittori** —
> [`Service/marketingContactListService.js`](../../back-pre/app/Service/marketingContactListService.js):
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

Stato dei dati (DB dev):
- **7 626 righe**, `AUTO_INCREMENT = 231 812` → oltre **223 000** righe create e distrutte: ogni
  modifica a una lista fa `destroy` + `bulkCreate` di tutti i membri.
- **5 587 righe su 7 626 (≈73 %) sono orfane**: `contactListId` che non corrisponde a nessuna
  [`contact_list`](contact_list.md) (liste `deleteContactList` non sempre hanno propagato la `destroy`
  dei membri, o membri creati per liste poi rimosse per altra via).
- `status` sempre `1`; 61 `contactListId` distinti referenziati a fronte di 51 liste esistenti; 1 952
  clienti distinti.

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing**. Va rifatta come vera tabella ponte:
FK verso lista e verso il contatto, `UNIQUE(list_id, contact_id)`, soft-delete o rimozione puntuale
(non "cancella tutti e ricrea"), niente id in churn.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### contactId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`customers`](customers.md)`.id` — **nessuna FK**. Nome fuorviante ("contact" ma è un
  `customers.id`). 0 righe orfane verso `customers` nei dati.
- **Descrizione**: cliente incluso nella lista. In `createContactList`/`updateContactList` proviene da
  `req.body.contactIds` (array); in `addCustomer` è il `customerId` della prenotazione.
- **Esempio di valori**: `10329`, `10270`, `10228`

### contactListId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`contact_list`](contact_list.md)`.id` — **nessuna FK**. **5 587/7 626 righe puntano
  a una lista inesistente** (dati orfani).
- **Descrizione**: lista di appartenenza.
- **Esempio di valori**: `258`, `257`, `255`

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
- **Esempio di valori**: `2026-05-07 10:35:04`

---

## Osservazioni verificate nel codice (criticità)

- **73 % di righe orfane** (`contactListId` senza `contact_list`): nessuna FK, nessun `ON DELETE
  CASCADE`, e i percorsi di cancellazione non coprono tutti i casi.
- **Churn estremo degli id** (`AUTO_INCREMENT` ~231 k per 7,6 k righe): `updateContactList` cancella e
  ricrea **tutti** i membri anche solo per rinominare la lista se il client rimanda `contactIds`.
- **Nessun `UNIQUE(contactListId, contactId)`**: solo `addCustomer` de-duplica in applicazione;
  `bulkCreate` può inserire doppioni se `contactIds` contiene ripetizioni.
- **`status` colonna morta**; **`updated_at` mai aggiornato**.
- **Nome `contactId` fuorviante** (è `customers.id`, non un id di "contatto" separato).
- Nessuna FK verso `contact_list` né verso `customers`; la coerenza è solo nelle JOIN raw.

### Campi candidati a enumeratore

Nessuno: `status` è un `int` inutilizzato da modellare come `bool`/eliminare.
