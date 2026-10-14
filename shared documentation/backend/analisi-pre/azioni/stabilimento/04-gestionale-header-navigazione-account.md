# stabilimento — Gestionale: header, navigazione e pannello Account

> Area funzionale: **shell del nuovo gestionale Summer Booking Hub** (header a barra bianca, sidebar
> verticale, sotto-menu di sezione, pannello Account). Non è una singola schermata di prenotazione ma
> il contorno comune a tutte le pagine del gestionale.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezioni «HEADER E MENU SUMMER BOOKING HUB»,
> «PRENOTAZIONI » (comportamento del sotto-menu), «PANNELLO ACCOUNT»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node `3068-332195` header, `1165-2812` sidebar,
> `7154-428427` pannello Account, `7154-438413` sotto-menu Prenotazioni).
>
> Logica di business correlata:
> [`../../business-logic/stabilimento/13-assistente-notifiche-account.md`](../../business-logic/stabilimento/13-assistente-notifiche-account.md)
> (Account) e [`../../business-logic/_indice.md`](../../business-logic/_indice.md)
> per la mappatura voce di menù → file. Qui si descrive **solo** il contratto client↔API.
>
> Molte firme sono **ipotizzate**: i mockup del nuovo gestionale non sono ancora legati a endpoint
> pubblicati. Dove un endpoint attuale copre già la funzione è indicato fra parentesi.

---

## 1. Caricamento del contesto di lavoro dell'header

**Descrizione**: all'apertura di qualunque pagina del gestionale l'header mostra i selettori di data,
il toggle «Oggi / Non oggi», il menu della tipologia di prenotazioni (Intera giornata / Solo mattina /
Solo pomeriggio) e il campo di ricerca per nome. Il valore selezionato qui filtra la maggior parte
delle pagine operative (Mappa, Planner, Cassa).
**Schermata**: header (node `3068-332195`).
**Ruolo**: gestore.

**Input → API** (`sb/getpersonalDetails` + `sb/getPropertyWithBathHouseDetails`; « firma ipotizzata »
per il riepilogo di contesto)
- `propertyId` (int) / `bathHouseId` (int)
- `date` (date) — data corrente selezionata (default: oggi)
- `slot` (string) — `"full" | "morning" | "afternoon"`

**Output ← API**
- `property` ({ id, name, timezone, seasonStart, seasonEnd, slotEnabled })
- `user` ({ id, fullName, email, phone, role, licenseName, monthsToExpiry })
- `unreadNotifications` (int) — badge sull'icona Notifiche
- `soundAlerts` ({ bookingDesk, bookingOnline, serviceDesk, serviceOnline }) — flag avvisi sonori

**Note**: il toggle «Oggi / Non oggi» è stato client (blocca/sblocca la data su oggi); non genera una
chiamata dedicata. La lingua non è più selezionabile dal pannello Account (vedi azione 8) ma resta un
parametro di ogni richiesta.

---

## 2. Ricerca rapida per nome dall'header

**Descrizione**: dal campo di ricerca dell'header il gestore cerca un cliente o una postazione; il
risultato porta alla prenotazione o evidenzia la postazione sulla mappa.
**Schermata**: header.
**Ruolo**: gestore.

**Input → API** (`sb/locationAutosuggestion` per le postazioni; `sb/customer-list` per i clienti)
- `propertyId` / `bathHouseId` (int)
- `query` (string) — testo digitato (nome/cognome cliente, nome/numero postazione)
- `date` (date) — contesto data corrente

**Output ← API**
- `customers` (array) — `{ customerId, fullName, phone, activeBookingId? }`
- `locations` (array) — `{ elementId, label, sector, status, bookingId? }`

---

## 3. Navigazione della sidebar (voci principali)

**Descrizione**: la sidebar verticale mostra solo icone e si espande all'hover con le etichette. Voci:
Mappa, Planner, Cassa, Contatti, Prenotazioni, Preventivi, Convenzioni, Statistiche, Magazzino,
Ricavi, Marketing, Logout. La voce attiva resta evidenziata.
**Schermata**: sidebar (node `1165-2812`; stato attivo `1165-2305` chiusa, `1174-13603` aperta).
**Ruolo**: gestore.

**Input → API**: nessuna per la navigazione in sé (routing client). Ogni voce carica la propria
pagina con le API descritte nei rispettivi documenti
([`05-gestionale-mappa.md`](05-gestionale-mappa.md),
[`07-gestionale-planner.md`](07-gestionale-planner.md),
[`08-gestionale-cassa.md`](08-gestionale-cassa.md),
[`09-gestionale-contatti.md`](09-gestionale-contatti.md),
[`10-gestionale-convenzioni.md`](10-gestionale-convenzioni.md),
[`11-gestionale-prenotazioni.md`](11-gestionale-prenotazioni.md)).

**Output ← API**: n/a.

**Note**: la voce **Convenzioni** è nuova, posizionata dopo Prenotazioni; la voce **Prenotazioni**
sostituisce l'attuale «Crediti». Per entrambe compare un sotto-menu (azione 4).

---

## 4. Apertura di un sotto-menu di sezione (Prenotazioni / Convenzioni)

**Descrizione**: cliccando su «Prenotazioni» o «Convenzioni» si apre un secondo livello di menu
agganciato alla sidebar (desktop) o un dropdown sotto l'header (tablet/mobile). Per «Prenotazioni» le
voci sono: Prenotazioni, Transazioni, Richieste di pagamento, Prenotazioni online, Prenotazioni online
servizi, Sharing creati, Ingressi, Scadenziario, Gruppi. Per «Convenzioni»: Strutture convenzionate,
Prenotazioni con convenzionati, Tutti i report.
**Schermata**: node `7154-438413` (Prenotazioni), `4274-410217` (Convenzioni).
**Ruolo**: gestore.

**Input → API**: nessuna (le voci sono statiche). La voce di default è la prima di ogni sotto-menu.

**Output ← API**: n/a. La singola sotto-pagina carica i propri dati (vedi
[`11-gestionale-prenotazioni.md`](11-gestionale-prenotazioni.md) e
[`10-gestionale-convenzioni.md`](10-gestionale-convenzioni.md)).

**Note**: la sotto-pagina **Ingressi** è nuova (dopo «Sharing creati»); le liste «Prenotazioni
online» e «Prenotazioni online servizi» vengono **spostate qui dalla pagina Statistiche** senza
modifiche di contratto dati.

---

## 5. Apertura del pannello Account

**Descrizione**: dall'icona Account in alto a destra si apre il pannello con i dati utente (nome,
email, cellulare), la voce «Licenza», «Mesi rimanenti alla scadenza», la sezione avvisi sonori e i
flag «Autenticazione a due fattori» e «Abilita sezione news».
**Schermata**: node `7154-428427`.
**Ruolo**: gestore.

**Input → API** (`sb/getpersonalDetails`)
- `userId` (int)

**Output ← API**
- `user` ({ fullName, email, phone })
- `license` ({ name, monthsToExpiry })
- `soundAlerts` ({ bookingDesk, bookingOnline, serviceDesk, serviceOnline }) — 4 flag booleani
- `twoFactorEnabled` (bool)
- `newsSectionEnabled` (bool)

**Note**: rispetto alla versione attuale vengono **rimossi** dal pannello il selettore lingua e lo
stato abbonamento. I pulsanti finali del pannello restano invariati.

---

## 6. Aggiornamento delle preferenze di avviso sonoro

**Descrizione**: il gestore abilita/disabilita singolarmente i 4 avvisi sonori: nuove prenotazioni da
gestionale, nuove prenotazioni online, nuove prenotazioni servizi da gestionale, nuove prenotazioni
servizi online.
**Schermata**: pannello Account (node `7154-428427`).
**Ruolo**: gestore.

**Input → API** (`sb/updatePersonalDetails` / `sb/updateNotification`; « firma ipotizzata » per il
raggruppamento dei 4 flag)
- `userId` (int)
- `soundAlerts` ({ bookingDesk (bool), bookingOnline (bool), serviceDesk (bool), serviceOnline (bool) })

**Output ← API**
- `status` / `message`
- `soundAlerts` — stato salvato (rimane la fonte per l'header, azione 1)

---

## 7. Attivazione/disattivazione dell'autenticazione a due fattori

**Descrizione**: dal pannello Account il gestore attiva o disattiva la 2FA sul proprio accesso al
gestionale.
**Schermata**: pannello Account.
**Ruolo**: gestore.
**Note**: « firma ipotizzata » — schermata di verifica OTP non presente nei mockup.

**Input → API** (`sb/updatePersonalDetails`; step di verifica via `sb/sendToken`)
- `userId` (int)
- `twoFactorEnabled` (bool) — nuovo stato desiderato
- `otp` (string, opzionale) — codice di conferma quando si attiva

**Output ← API**
- `status` / `message`
- `twoFactorEnabled` (bool) — stato effettivo
- `otpRequired` (bool) — se il server chiede il passo di verifica

---

## 8. Abilitazione della sezione news

**Descrizione**: flag che mostra/nasconde la sezione news nel gestionale.
**Schermata**: pannello Account.
**Ruolo**: gestore.

**Input → API** (`sb/updatePersonalDetails`)
- `userId` (int)
- `newsSectionEnabled` (bool)

**Output ← API**
- `status` / `message`
- `newsSectionEnabled` (bool)

---

## 9. Apertura di Notifiche e Assistente virtuale dall'header

**Descrizione**: le icone Notifiche e Assistente virtuale dell'header aprono i rispettivi pannelli.
**Schermata**: header.
**Ruolo**: gestore.

**Input → API**
- Notifiche: `sb/getNotification` con `{ userId, page, pageSize }` → `{ total, rows: [ { id, type,
  title, body, bookingId?, createdAt, read } ] }`; lettura via `sb/updateNotification`
  `{ ids[], read: true }`.
- Assistente virtuale: `sb/switchboard/menu` / `sb/switchboard/config` per la configurazione del
  centralino/assistente (contratto invariato rispetto all'attuale).

**Output ← API**: come sopra.

**Note**: fuori dal perimetro di questo documento la logica dell'assistente; qui interessa solo che le
icone dell'header sono punti di ingresso e non modificano il contesto data/slot.
