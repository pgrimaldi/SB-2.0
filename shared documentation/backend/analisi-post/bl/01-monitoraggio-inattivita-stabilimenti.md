# Monitoraggio inattività stabilimenti (finestra 01/06–31/08)

> **Stato**: regola **definita** (committente, 2026-09-18). Non ancora nel legacy: nessuna funzionalità
> analoga in `back-pre`/`front-pre` o nelle schede `analisi-pre/`. Prima scheda di `analisi-post/bl/` — a
> differenza di [`analisi-pre/business-logic/`](../../analisi-pre/business-logic/_indice.md), che documenta
> la logica **AS-IS** del gestionale legacy, questa cartella raccoglie la logica di business **nuova**,
> richiesta direttamente per la riscrittura e priva di controparte nel vecchio sistema.

## 1. Requisito (come espresso dal committente)

> "Nel nuovo sw vorremmo implementare una logica che controlla automaticamente dal 01/06 al 31/08 per
> ogni stabilimento gli accessi dei dipendenti o le prenotazioni fatte — se non ci sono record per due
> giorni consecutivi deve arrivarci un alert a noi perché significa che qualche stabilimento non sta
> usando il sw."

## 2. Obiettivo di business

Giugno–agosto è l'alta stagione balneare: è il periodo in cui uno stabilimento dovrebbe usare di più il
gestionale (accessi dello staff, prenotazioni inserite). Un'assenza totale di attività per **due giorni
solari consecutivi** dentro questa finestra è un segnale forte che lo stabilimento ha smesso di usare il
software (guasto, disaffezione, passaggio a un concorrente, chiusura non comunicata...). Non è un bug da
correggere: è un **allarme operativo verso il gestore della piattaforma** (retention/customer success),
analogo nello spirito al monitoraggio abbonamenti già presente lato `systemadmin`
(vedi [`analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md`](../../analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md)).

## 3. Regola (decisa dal committente, 2026-09-18)

- **Periodo di osservazione**: dal 1 giugno al 31 agosto, ricorrente ogni anno. Le due date sono
  **configurabili via variabile d'ambiente** (es. `PROPERTY_ACTIVITY_WINDOW_START` /
  `_END`), inizialmente impostate a `06-01`/`08-31` — non cablate nel codice.
- **Ambito**: ogni stabilimento (property) attivo sulla piattaforma, **escluso** chi ha l'abbonamento
  **sospeso o scaduto** in quel momento: per questi **nessun alert**.
- **Segnali di "utilizzo"** (basta uno dei due — OR logico):
  1. **accessi dello staff** al gestionale — login riusciti di utenti collegati allo stabilimento, di
     **qualunque ruolo** (titolare, operatore cassa, bagnino, bagnino avanzato, ...);
  2. **prenotazioni registrate per lo stabilimento arrivate dal canale online** (cliente finale via
     sito/app) — **non** contano le prenotazioni inserite manualmente dallo staff nel gestionale: quelle
     sono già coperte dal segnale 1 (l'accesso dello staff che le ha inserite).
- **Condizione di allarme**: **due giorni solari consecutivi**, entrambi interamente dentro la finestra
  01/06–31/08, in cui **nessuno dei due segnali** si è verificato per quello stabilimento. Giorno
  calcolato nel **fuso orario dello stabilimento** (nella pratica sempre `Europe/Rome`, quindi irrilevante
  allo stato attuale, ma la regola resta corretta se in futuro si aggiungono stabilimenti fuori Italia).
- **Destinatario dell'alert**: la piattaforma (noi), **non** lo stabilimento interessato.
- **Canale dell'alert**: notifica nel pannello systemadmin (nuovo back-office); se l'integrazione con il
  pannello risultasse troppo complessa in una prima fase, fallback su semplice **email** interna.
- **Contenuto**: **un riepilogo giornaliero cumulativo** con tutti gli stabilimenti in anomalia quel
  giorno — non un alert per singolo stabilimento.
- **Ripetizione**: **un alert** all'apertura dell'anomalia, poi **ripetizioni ogni 7 giorni** finché
  l'anomalia resta aperta (nessun accesso/prenotazione online nel frattempo).
- **Frequenza di controllo**: job schedulato giornaliero — ad ogni esecuzione verifica se gli ultimi due
  giorni (ieri e l'altroieri) risultano entrambi "vuoti" per uno o più stabilimenti, e se per gli
  stabilimenti già in anomalia sono trascorsi 7 giorni dall'ultimo alert.

## 4. Possibili fonti dati (da confermare in fase tecnica)

- **Accessi staff**: evento di login per utente, da correlare allo stabilimento tramite i suoi ruoli/scope
  ([`identity_access_user_resource_scopes`](../db/identity/_Zidentity_access_user_resource_scopes.md)).
  `identity_access_users.last_login_at` ([identity_access_users.md](../db/identity/identity_access_users.md))
  tiene però **solo l'ultimo accesso**, non uno storico — non basta da solo per un controllo giorno per
  giorno; `identity_access_refresh_tokens.issued_at` ([identity_access_refresh_tokens.md](../db/identity/identity_access_refresh_tokens.md))
  è più promettente (un evento per login) ma va verificato se copre tutti i canali di accesso o se serve
  un log accessi dedicato.
- **Prenotazioni online**: `booking_reservations.created_at` per `property_id`, filtrate su
  `booking_channel = 'customer'` (l'enum `booking.booking_channel` distingue `customer` = online da
  `manager` = gestionale — vedi [booking_reservations.md §"booking_channel"](../db/booking/booking_reservations.md)).
- **Stato abbonamento** (per l'esclusione di §3): stato sospeso/scaduto dello stabilimento, da leggere dal
  modulo identity/licenze (`identity_account_subscriptions` e affini).
- **Invio/tracciamento alert**: da allineare a [`notification_deliveries`](../db/notification/notification_deliveries.md)
  oppure a una vista/dashboard interna del nuovo back-office systemadmin (ancora da scrivere, vedi
  `analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md`); serve comunque un piccolo stato
  persistito per stabilimento (data apertura anomalia, data ultimo alert) per gestire la cadenza a 7
  giorni del riepilogo (§3).

## 5. Riferimenti

- Nessun analogo AS-IS: funzionalità assente da `back-pre`/`front-pre` e da `analisi-pre/`.
- Tabelle candidate: `booking_reservations`, `identity_access_refresh_tokens`, `identity_access_users`,
  `identity_access_user_resource_scopes`, `notification_deliveries`.
- Contesto correlato: [`analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md`](../../analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md)
  (monitoraggio/gestione stabilimenti lato piattaforma).
