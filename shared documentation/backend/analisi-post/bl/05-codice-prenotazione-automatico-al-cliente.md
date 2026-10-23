# Sharing: codice prenotazione creato in automatico e inviato al cliente (email o WhatsApp)

> **Stato**: comportamento **richiesto dal committente** il 2026-10-21, da implementare. Moduli
> `booking` (codice) e `notification` (email e WhatsApp). Decisioni del committente del 2026-10-21 nel §4.

## 1. Com'era nel legacy

Per far condividere una prenotazione, il gestore apriva la pagina della prenotazione, **generava a
mano** il codice prenotazione (voce «Vedi codice prenotazione» del menu «Cosa vuoi fare?»,
[`azioni/stabilimento/06-gestionale-dettagli-e-prenotazioni.md`](../../analisi-pre/azioni/stabilimento/06-gestionale-dettagli-e-prenotazioni.md))
e poi lo **inviava per email** al cliente. Con quel codice il cliente apriva la prenotazione
(`/onlinebooking/consumer/{lingua}/{codice}`) e poteva condividerla.

Erano due passaggi manuali, e il secondo dipendeva dal primo: una prenotazione senza codice non era
condivisibile, e il gestore doveva ricordarsi di crearlo.

## 2. Come diventa

- Il **codice si crea da solo** alla creazione della prenotazione, senza azione del gestore. Nel nuovo
  schema è già così: [`booking_reservations.reservation_code`](../db/booking/booking_reservations.md)
  è `NOT NULL`, generato server-side con RNG crittografico, lunghezza fissa, univoco fra le righe vive.
  Le prenotazioni migrate hanno già il loro codice. Non serve quindi una tabella né un'azione nuova.
- Il **codice va nel messaggio** che il cliente riceve quando il **gestore crea la prenotazione a
  mano**: nel testo, e come link pronto alla pagina pubblica della prenotazione, così il cliente non
  deve copiarlo.
- Il messaggio parte per **email o per WhatsApp**; la stessa aggiunta (codice + link) vale per
  entrambi i canali. Se il cliente ha sia email sia telefono, **sceglie il gestore** il canale.
- La voce «Vedi codice prenotazione» resta per consultazione (il codice c'è sempre); scompare il
  passaggio «crea codice».


## 3. Cosa cambia nel concreto

1. **Contatto obbligatorio**: l'API di creazione della prenotazione da gestore rifiuta la richiesta
   (`400`, errore di validazione) se il cliente non ha **né email né telefono**. Almeno uno dei due
   serve sempre, perché il codice deve poter arrivare al cliente.
2. **Template** ([`notification_templates`](../db/notification/notification_templates.md)): al
   messaggio inviato al cliente alla creazione manuale della prenotazione (`delivery_kind`
   `booking_confirmation`, [`notification_deliveries`](../db/notification/notification_deliveries.md))
   si aggiungono due segnaposto, per esempio `{{reservationCode}}` e `{{reservationUrl}}`, in tutte
   le lingue gestite e in **due versioni**: HTML per l'email, testo semplice per WhatsApp.
3. **Canale scelto dal gestore**: con un solo contatto il canale è quello; con entrambi lo indica il
   gestore. Le due strade sono diverse:
   - **email**: la invia il backend;
   - **WhatsApp**: come nel sistema attuale, **nessuna API Meta**. Il backend restituisce il testo già
     composto (con codice e link) e il numero del cliente; il front apre una finestra WhatsApp con
     il messaggio precompilato, e l'invio lo fa il gestore. Non serve quindi un modello approvato
     da Meta.
4. **Link**: l'URL pubblico si costruisce dal codice e dalla lingua del cliente; l'indirizzo base sta
   in configurazione, non nel template.
5. Il cliente che apre il link e si autentica diventa partecipante
   ([`booking_participants`](../db/booking/booking_participants.md), ruolo `viewer`); se la
   prenotazione richiede conferma (`online_confirmation_state = pending`), è lo stesso codice a
   confermarla.

## 4. Decisioni del committente (2026-10-21)

1. **Quale messaggio**: quello inviato al cliente quando il gestore crea la prenotazione manualmente.
2. **Canali**: email o WhatsApp, con la stessa aggiunta di codice e link.
3. **Contatti**: il cliente deve dare **obbligatoriamente** telefono o email; se li ha entrambi, il
   gestore decide dove inviare. Senza email si usa WhatsApp.
4. **WhatsApp**: testo precompilato in una finestra apposita, inviato dal gestore; niente API Meta.
5. **Nessuna rigenerazione del codice**: resta lo stesso per tutta la vita della prenotazione, come
   nel legacy. Il codice è di fatto una chiave di accesso (chi riceve il messaggio o un suo inoltro
   può aprire la prenotazione): rischio accettato.

## 5. Cosa resta da fare

- [ ] Validazione «email o telefono obbligatorio» nella creazione della prenotazione da gestore.
- [ ] Segnaposto `reservationCode` / `reservationUrl` nei template email e WhatsApp, in tutte le lingue.
- [ ] Invio email dal backend; per WhatsApp, risposta con testo composto e numero del cliente.
- [ ] Front: scelta del canale quando ci sono entrambi i contatti; finestra WhatsApp precompilata.
- [ ] Togliere dal front il passaggio «crea codice» (resta solo la consultazione).
