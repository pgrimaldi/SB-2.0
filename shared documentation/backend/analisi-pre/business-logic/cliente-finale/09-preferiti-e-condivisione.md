# cliente finale — Preferiti e condivisione

> **Ambito**: il "mi piace" agli stabilimenti (preferiti) e la condivisione di una scheda
> stabilimento / di una prenotazione.
>
> **Metodo**: `front-customer-pre/src/components/consumer/*` + `propertylikelogs.controller.js`
> (`likeLogDetails`), `property.controller.js` (`getPropertyLikes`), `user.controller.js`
> (`myBookingDetails`/`myfavourite`) + `booking.controller.js` (`createSharing`, `getSharingDates`).
> Schede DB: [`property_like_logs.md`](../../db/property_like_logs.md),
> [`bathouses.md`](../../db/bathouses.md) (`likeCount`), [`sharing_details.md`](../../db/sharing_details.md).

---

## 1. Mettere / togliere "mi piace" a uno stabilimento

**Azione** — `POST property/likelogs` (`likeLogDetails`), con `userId`, `bathHouseId`, `islike`.

**Logiche**:

- `islike == true`: crea una riga `property_like_logs` (`likedStatus = 1`) e **incrementa** il
  contatore denormalizzato `bathouses.likeCount` (`likeCount + 1`).
- `islike == false` ("non mi piace"): **elimina** la riga `property_like_logs` corrispondente
  (`userId` + `bathHouseId`) e **decrementa** `bathouses.likeCount` (`likeCount - 1`).
- Il "non mi piace" è quindi una **cancellazione**, non un cambio di stato: `likedStatus` è di fatto
  sempre `1`.
- **Nessun controllo di idempotenza**: like ripetuti creano righe duplicate e incrementano più volte
  il contatore; unlike su una riga inesistente decrementa comunque `likeCount` (può andare negativo).

**Calcoli**: incremento/decremento del contatore `likeCount`. **Eventi**: nessuno (nessuna email/
notifica).

---

## 2. Vedere i preferiti / i like aggregati

**Azioni** — `myfavourite` (i preferiti del cliente), `property/likes` (`getPropertyLikes`, conteggio
per stabilimento).

**Calcoli**: conteggi. **Eventi**: nessuno.

---

## 3. Condividere una scheda stabilimento

**Azione**: pulsanti di condivisione social / copia link (rotta `/property-details/:slug`).

**Logiche**: costruisce l'URL pubblico dello stabilimento a partire dallo `slug`
(`bathouses.slug`); l'eventuale parametro affiliato (`:affiliate`) traccia l'arrivo dal canale.

**Calcoli**: nessuno. **Eventi**: nessuno server‑side (la condivisione avviene nel client / OS).

---

## 4. Condividere una prenotazione (sharing di postazione)

**Azione** — `createSharing` / `getSharingDates` / `get-sharing-and-absence-dates`
(rotta `/booking-sharing`).

**Logiche**: due o più persone si alternano sulla **stessa postazione** in giorni diversi; si
verifica che le date assegnate non si sovrappongano; si registrano gli intervalli per partecipante in
`sharing_details` e si aggiornano le righe prenotazione. Il prezzo di ciascun partecipante è calcolato
sui suoi soli giorni (motore di prezzo lato client).

**Calcoli**: prezzo per partecipante sui suoi giorni. **Eventi**: email di riepilogo/condivisione ai
partecipanti + `email_logs` (dal `createSharing`). Alla conferma di pagamento, il webhook Stripe marca
`isBooked = 1` le date di sharing (vedi [07-pagamento](07-pagamento.md) §3).

---

## 5. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: `likeCount ± 1`; conteggi preferiti; prezzo per partecipante nello sharing.
- **Logiche**: unlike = delete della riga; nessuna idempotenza sui like; non‑sovrapposizione date
  sharing; URL pubblico da slug (+ affiliato).
- **Eventi**: **nessuno** per like/preferiti/condivisione scheda. Per lo **sharing di prenotazione**:
  email ai partecipanti + `email_logs`, e (a valle del pagamento) marcatura date via webhook.
