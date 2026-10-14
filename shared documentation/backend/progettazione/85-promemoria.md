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
  titolare dello stabilimento e prenotazione online abilitata dal systemadmin (Catalog).
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
