# systemadmin — Iscritti newsletter (azioni client↔API)

> **Ruolo**: systemadmin. **Fonte**: [`front-admin/`](../../../../../../../front-admin/) — modulo
> `SubscriptionDetails` (`SubscriptionList.js`, `DeleteSubUser.js`, `api/SubscriptionApi.js`), voce di
> menù "Subscription Details" (`/subscripation-details/subscription-list`). Tabella:
> [`db/subscribes.md`](../../db/subscribes.md).
>
> **Rilievo**: questa è la **prima UI** del repository che **legge e gestisce** la tabella `subscribes`.
> Fino all'arrivo di `front-admin/` gli endpoint `getSubscribe`/`deleteSubscribe` non avevano alcun
> consumer (`subscribes` era "write-only in produzione"). Vedi
> [`db/subscribes.md`](../../db/subscribes.md) §"Verifica utilizzo".

---

## 1. Elenco degli iscritti

**Descrizione**: apertura della pagina "Subscription Details": tabella degli indirizzi iscritti alla
newsletter con data.
**Schermata**: `SubscriptionDetails/SubscriptionList.js`
**Ruolo**: systemadmin (endpoint backend con sola `optionalAuth` — vedi note)

**Input → API** (`POST sb/getSubscribe`)
- corpo: **stringa vuota `""`** (il client invia `Api.post(url, "")`, nessun parametro utile)

**Output ← API**
- `sucess` (bool) — **con questa grafia** (typo lato server, il client lo legge così)
- `data` (array) — per riga: `id` (int), `email` (string), `createdAt` (datetime) → resa come
  `toLocaleDateString()`
- `message` (string) — mostrato se `sucess = false`

**Note**: lato server `getSubscribe` filtra `status = 1` e monta solo `authJwt.optionalAuth` — l'intera
mailing list è di fatto leggibile senza ruolo. Da portare sotto `systemadmin` reale nella riscrittura.
Nessuna paginazione: filtro/ricerca su `email` in memoria.

---

## 2. Eliminazione di un iscritto

**Descrizione**: dalla tabella, il systemadmin conferma l'eliminazione di una riga nella modale.
**Schermata**: `SubscriptionDetails/DeleteSubUser.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/deleteSubscribe`)
- `id` (int) — id iscritto

**Output ← API**
- `sucess` (bool)
- `message` (string) — Snackbar; su successo il client ricarica la lista

**Note**: soft-delete lato server (`update({ status: 0 })`). Stesso problema di autorizzazione del punto 1.

---

## 3. Export in Excel

**Descrizione**: il systemadmin scarica l'elenco (Email + Date) come file `.xlsx`.
**Schermata**: `SubscriptionDetails/SubscriptionList.js` (`exportToExcel`)
**Ruolo**: systemadmin

**Input → API**: nessuna — operazione **interamente client-side** (libreria `xlsx`, file
`subscription_list.xlsx` generato dai dati già in memoria).

**Output**: file scaricato dal browser.
