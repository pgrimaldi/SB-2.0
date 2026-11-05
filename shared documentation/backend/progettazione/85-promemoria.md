# Promemoria: cose da fare o da riabilitare

> Elenco vivo di ciò che è stato **volutamente disattivato o rimandato** nel codice, con il motivo e la
> condizione per riattivarlo. Ogni voce ha un'etichetta `PROMEMORIA-85-<n>`: lo stesso testo compare
> come commento nel codice, così `grep -rn "PROMEMORIA-85" backend/` trova tutti i punti da toccare.
>
> Una voce si chiude **cancellandola da qui e dal codice** nello stesso commit, non segnandola come fatta.

---

## Identity

### PROMEMORIA-85-1 — Controlli di abbonamento e licenza disattivati

- **Cosa è disattivato**: ogni controllo che legge `identity.account_subscriptions` o il piano di
  licenza (`basic`/`advanced`) per bloccare o limitare uno stabilimento: blocco delle funzioni con
  abbonamento scaduto o sospeso, abilitazione delle voci riservate ad `advanced`, campi licenza e mesi
  residui restituiti da `GET /api/identity/me`, batch notturno di scadenza.
- **Perché**: dopo la migrazione la tabella è **vuota** — nel legacy gli abbonamenti erano gestiti fuori
  dal portale ([`80-retention.md`](80-retention.md), Q10). Con i controlli attivi ogni stabilimento
  risulterebbe senza abbonamento e verrebbe bloccato.
- **Come è disattivato**: il codice di controllo è scritto ma **commentato**, con l'etichetta; al suo
  posto il comportamento "bypass": ogni stabilimento è trattato come attivo e con **tutto sbloccato**
  (equivalente al piano `advanced`), decisione del 2026-10-07.
- **Quando riattivarlo**: quando il systemadmin avrà inserito gli abbonamenti di tutti gli stabilimenti
  attivi dalle API dedicate (`/api/identity/admin/properties/{propertyId}/subscription`).
- **Come verificarlo prima**: nessuno stabilimento attivo senza riga in `identity.account_subscriptions`.
- **Punti nel codice**: `MeService.GetAsync` (`allFeaturesUnlocked`), `AuthService.CompleteAuthenticationAsync`
  (blocco del login dello staff). Manca ancora il batch notturno di scadenza (`Subscription.Expire`, docs/20-jobs.md).

### PROMEMORIA-85-2 — Unicità di email e username non garantita dal database

- **Situazione**: gli indici `uq_identity_access_users_email_alive` e `uq_identity_access_users_username_alive`
  non esistono; ci sono email (36) e username (4) duplicati fra gli account vivi, tutti dati storici.
- **Impatto**: il login per email o username può trovare più account. Regola decisa il 2026-10-07: si
  usa l'account vivo con `last_login_at` più recente e si registra un warning. Le API di registrazione e
  di creazione staff controllano l'unicità a livello applicativo.
- **Quando chiuderlo**: dopo la bonifica dei duplicati, aggiungere i due indici
  ([`identity_access_users.md`](../analisi-post/db/identity/identity_access_users.md)).

### PROMEMORIA-85-3 — Policy di retention per sessioni (parte Audit chiusa il 2026-10-13)

- **Cosa manca** nel seed di `privacy_retention_policies`: refresh token (30 giorni dopo `expires_at`) e
  challenge (1 giorno dopo `expires_at`) — [`20-jobs.md` §1.4](20-jobs.md).
- **Fatto il 2026-10-13**: le tre categorie di `audit.trail_entries` (`security` 6 mesi, `platform` 12 mesi,
  `operator` 12 mesi, D-66) e `target_filter` nella chiave unica delle policy, con la migration
  `AddAuditRetentionPolicies` di Privacy ([86-modulo-audit.md §7](86-modulo-audit.md)). Il comando
  `retention` di Boot ora carica anche il modulo Audit.
- **Ancora aperto**: il rifiuto, sulla stessa tabella e con la stessa azione, di una regola senza filtro accanto
  a regole con filtro (descritto in [`privacy_retention_policies.md`](../analisi-post/db/privacy/privacy_retention_policies.md)):
  oggi non esiste un seeder applicativo che possa farlo, le policy entrano solo da migration.
- **Quando**: le due di Identity con le API di sessione.

### PROMEMORIA-85-4 — Audit: permessi DB e partizioni future

- **Situazione**: dal 2026-10-13 Audit salva gli eventi e ha le API ([88-api-audit.md](88-api-audit.md)).
  Mancano i permessi DB (l'utente applicativo deve avere su `audit` solo `INSERT`/`SELECT`, il `DELETE` solo
  il ruolo della retention) e il job che crea le partizioni future: quelle esistenti arrivano a dicembre 2028,
  poi le righe finirebbero nella partizione default ([06-partizioni-manutenzione.md](06-partizioni-manutenzione.md)).
- **Quando chiuderlo**: permessi al provisioning dei ruoli DB, **prima del go-live**; il job delle partizioni
  entro il 2028. I registri di sicurezza e degli amministratori di sistema sono obblighi (Provv. Garante 27/11/2008).

### PROMEMORIA-85-5 — Effetti di Identity su moduli non ancora sviluppati

- **Situazione**: Identity pubblica eventi o chiama contratti in Shared per effetti che appartengono ad
  altri moduli. Per le **email** (reset password, OTP, benvenuto) c'è un'implementazione minima di invio
  in Notification. Gli altri restano eventi **senza gestore**: preferenze «email/cellulare come contatto»
  dello staff (`notification_user_channel_preferences`), consenso newsletter alla registrazione,
  attivazione del contratto per stabilimento (Catalog). Dal 2026-10-26 Catalog gestisce titolare, prenotazione online,
  abbonamento e «mi piace».
- **Quando chiuderlo**: scrivendo i gestori in Notification e Catalog; ogni punto è marcato nel codice
  con questa etichetta.
- **Email di Identity come template**: oggi `SmtpTransactionalEmailSender` ha testi provvisori nel codice. Le
  email intere (reset password, codice 2FA, benvenuto) vanno fatte come **template** di Notification, non
  come stringhe in `.resx`. I testi IT/EN del legacy da cui partire sono in
  `back-pre/app/mailtemplates/userTemplates.js` (`forgotpasswordMail*`, `otpMail`, `signupEmail*`); oggetti in
  `back-pre/app/global/globalVariables.js`. Identity passa già `firstName`, `resetUrl`/`code`, `minutes`, `loginUrl`.

### PROMEMORIA-85-6 — Passare la firma dei JWT da HS256 a RS256

- **Situazione**: i token sono firmati con HS256 (chiave simmetrica), con `kid` ed elenco di chiavi in
  configurazione ([autenticazione-autorizzazione-dotnet.md §4bis](pre/autenticazione-autorizzazione-dotnet.md)).
- **Quando**: **prima di estrarre il primo microservizio**, altrimenti il segreto finisce nel servizio
  estratto, che potrebbe falsificare token.
- **Come**: nuova coppia RS256 con un nuovo `kid`, pubblicazione della chiave pubblica su
  `/.well-known/jwks.json`, per la durata dei token si accettano entrambe le chiavi, poi si rimuove la
  vecchia. Le sessioni non cadono (i refresh token non sono JWT).

### PROMEMORIA-85-7 — Eventi senza outbox

- **Situazione**: gli eventi fra moduli (`IIntegrationEventPublisher`, Boot/Web/InProcessEventPublisher.cs) sono
  consegnati in-process **dopo** il salvataggio, non nella stessa transazione. Se il processo cade in mezzo,
  l'evento si perde (es. un'azione salvata senza la sua voce di audit).
- **Quando chiuderlo**: con l'outbox previsto da [20-jobs.md](20-jobs.md), prima del go-live di Audit.

### PROMEMORIA-85-8 — Mappatura degli enum nelle opzioni EF degli altri moduli

- **Situazione**: EF scrive gli enum nativi come interi se non sono registrati anche in `UseNpgsql(..., o => o.MapEnum<T>(...))`
  ([`_dotnet-template.md` §4](../analisi-post/db/_dotnet-template.md), insidia 3). Corretto in Identity e Infra;
  negli altri nove moduli no, perché nessuno scrive ancora via EF.
- **Quando chiuderlo**: prima della prima API di ciascun modulo; il modello delle migration non cambia.

### PROMEMORIA-85-9 — Stato in memoria del processo

- **Situazione**: blocco dei tentativi di login e codici di impersonazione monouso sono nella memoria del
  processo (`MemoryLoginThrottle`, `MemoryReplayGuard`). Con più istanze ognuna conta per sé.
- **Quando chiuderlo**: allo scale-out, spostandoli su Redis ([16-redis.md](16-redis.md)).

### PROMEMORIA-85-10 — Contratti: da collegare e da provare

- **Attivazione per stabilimento**: il documento "in vigore" è oggi l'ultima versione pubblicata per tutti;
  il puntatore per stabilimento (legacy `properties.contractDocumentsId`) vive in Catalog e non è collegato.
- **Prova del caricamento**: upload, pubblicazione e accettazione non sono stati provati end-to-end perché
  LocalStack non era avviato; il resto delle API Identity sì (prova del 2026-10-07).

### PROMEMORIA-85-12 — Le sette lingue negli altri moduli

- **Situazione**: dal 2026-10-08 le lingue gestite sono `it`, `en`, `fr`, `es`, `de`, `zh`, `ar`
  ([12-lingue-e-date.md §1.1](12-lingue-e-date.md)). Identity è allineato. Restano a `it`/`en`:
  `Module.Notification` (testi provvisori di `SmtpTransactionalEmailSender`, che riceve già il codice lingua
  dell'account e per le lingue nuove ripiega sull'italiano) e `fiskaly.language_code` (lingua del documento
  fiscale, legata a ciò che accetta il provider).
- **Quando chiuderlo**: Notification con i modelli di email (PROMEMORIA-85-5); Fiskaly verificando le lingue
  supportate da SIGN IT, e solo allora migration su `fiskaly.language_code`.

## Catalog e moduli collegati

Domande per il committente da riprendere quando si sviluppa il modulo indicato. Nascono dall'analisi del modulo
Catalog (2026-10); non hanno ancora punti nel codice.

### PROMEMORIA-85-13 — Struttura eliminata: prenotazioni pagate online

- **Situazione**: il committente ha deciso che, eliminando uno stabilimento, le prenotazioni future si annullano
  **senza rimborso e senza email** ai clienti (riattivate se lo stabilimento viene ripristinato dal cestino). Chi ha
  pagato online con carta può contestare l'addebito o reclamare.
- **Quando chiuderlo**: sviluppando l'eliminazione in Booking/Payment, rifare la domanda al committente proponendo
  l'elenco delle prenotazioni pagate da rimborsare a mano, mostrato al systemadmin prima di confermare.
- ✅ **Chiuso il 2026-11-02**: il committente conferma che **non si rimborsa nulla**, nemmeno i pagamenti online, e
  non serve l'elenco per il systemadmin.

### PROMEMORIA-85-14 — Le 91 strutture nascoste senza spiegazione

- **Situazione**: delle 731 strutture importate nel 2023 con stato 5 (nascoste a mano), 626 sono doppioni e 14
  corrispondono a stabilimenti poi registrati; per le altre **91** il committente non ha ancora deciso se tenerle
  nascoste, cancellarle o renderle visibili. Elenco in `strutture-nascoste-stato5.xlsx` (root del progetto, fuori
  dal repo). Dal 2026-10-26 l'ETL le migra con lo stato `hidden` (fuori dal sito e fuori dalla coda «in attesa»): il
  systemadmin può approvarle o spostarle nel cestino dal pannello.
- **Quando chiuderlo**: prima della migrazione finale, rifacendo la domanda al committente.

### PROMEMORIA-85-15 — Carta salvata e pre-autorizzazione: caparra tavoli e «paga in struttura»

- **Situazione**: per la caparra dei tavoli si salva la carta alla prenotazione e si pre-autorizza la caparra a
  ridosso della data (una pre-autorizzazione Stripe dura circa 7 giorni); se il gestore non la trattiene entro la
  fine del giorno della prenotazione viene rilasciata. Il committente vuole approfondire la stessa logica per il
  **«paga in struttura»** (prenotazione online, pagamento allo stabilimento con la carta a garanzia).
- **Quando chiuderlo**: sviluppando Payment, discutendo con il committente il «paga in struttura».

### PROMEMORIA-85-16 — Postazioni eliminate dalla configurazione della mappa: le prenotazioni

- **Situazione**: dal 2026-10-25 Catalog elimina (cancellazione logica) le postazioni con l'eliminazione singola o
  multipla, lo svuotamento o l'eliminazione di righe e colonne e «Reimposta mappa», ma le prenotazioni di quelle
  postazioni restano come sono. L'impostazione del gestore `map_layouts.reset_deletes_bookings` (decisione C4)
  dice cosa farne: `false` = ricollegarle per numero quando la postazione viene ricreata, `true` = cancellarle
  anche se pagate o scontrinate. Le voci di registro `catalog.map_spots_deleted`, `catalog.map_grid_changed` e
  `catalog.map_reset` hanno il numero di postazioni coinvolte.
- **Quando chiuderlo**: sviluppando Booking (evento di Catalog con le postazioni eliminate e l'impostazione, oppure
  conferma con l'elenco delle prenotazioni coinvolte prima dell'eliminazione).
- **Decisione del committente (2026-11-02)**: il ricollegamento avviene **per posizione nel reticolo** (fila e
  colonna) e, se la posizione non esiste più, **per nome** della postazione; mai se il tipo è cambiato (lettino al
  posto di ombrellone). A fine reimpostazione il gestore scarica un **report PDF** con i cambi di postazione e i
  ricollegamenti non riusciti; le prenotazioni senza postazione si vedono nella pagina Prenotazioni.

### PROMEMORIA-85-17 — Registrazione di una struttura: le email

- **Situazione**: dal 2026-10-25 il form pubblico «Registra la tua struttura» (`POST /api/catalog/property-registrations`)
  crea lo stabilimento in attesa e pubblica l'evento `PropertyRegistered` (Shared/Contracts/Catalog), ma nessun modulo
  lo gestisce. Servono in Notification: il benvenuto al referente nella sua lingua (con le foto, come nel legacy),
  l'avviso a info@ a ogni registrazione (decisione E4) e l'iscrizione dell'email alla lista contatti (`subscribes`).
- **Quando chiuderlo**: sviluppando i modelli di email di Notification.

### PROMEMORIA-85-18 — Stato e cestino degli stabilimenti: effetti sugli altri moduli

- **Situazione**: dal 2026-10-26 il pannello di amministrazione (`/api/catalog/admin/properties`) cambia lo stato degli
  stabilimenti e li sposta nel cestino, pubblicando `PropertyStatusChanged` e `PropertyTrashChanged`
  (Shared/Contracts/Catalog), ma nessun modulo li gestisce. Servono: in Identity, il blocco dell'accesso al gestionale
  per gli stabilimenti in attesa, rifiutati, nascosti o nel cestino; in Booking/Payment, l'annullamento delle
  prenotazioni future e dei pagamenti in sospeso quando uno stabilimento va nel cestino (senza rimborso né email,
  PROMEMORIA-85-13) e la riattivazione col ripristino; in Notification, l'email al titolare con il motivo
  dell'approvazione o del rifiuto; in Privacy, l'avvio dei tempi di conservazione con l'eliminazione definitiva.
- **Quando chiuderlo**: sviluppando ciascun modulo; la vetrina pubblica (fase 2) filtrerà da sé stato e cestino.

### PROMEMORIA-85-19 — Periodi della stagione successiva: il passaggio alla stagione corrente

- **Situazione**: dal 2026-11-05 il gestore prepara in anticipo i periodi tariffari della stagione successiva
  (`/api/catalog/next-season/tariff-periods`, tabella `catalog.pricing_next_season_periods`), separati da quelli della
  stagione corrente. Quando la stagione successiva diventa la corrente (il gestore aggiorna le date con `PUT /season`)
  i suoi periodi restano dove sono: non diventano i periodi correnti, e quelli vecchi restano attivi. Il passaggio
  non è automatico perché i prezzi di listini, convenzioni e servizi sono legati ai periodi correnti per chiave:
  sostituirli cambierebbe il significato dei prezzi già inseriti.
- **Da decidere**: se il passaggio sostituisce i periodi correnti e come si portano i prezzi (copiati per nome del
  periodo, oppure da reinserire), e se avviene da sé alla data di inizio o con un'azione del gestore.
- **Quando chiuderlo**: prima della prima stagione gestita con il nuovo sistema.
