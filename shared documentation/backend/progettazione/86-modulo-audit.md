# Modulo Audit: registro trasversale "chi ha fatto cosa"

> Decisione del 2026-10-07. Nasce dalla pagina «Tutti i log» di Impostazioni
> ([`analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md` §3](../analisi-pre/azioni/stabilimento/13-gestionale-impostazioni.md)),
> per cui né il legacy né il nuovo schema avevano un dato (la legacy `booking_logs` è un contatore, non
> un registro), e dai registri di sicurezza e degli amministratori previsti da [15-logs.md](15-logs.md)
> ma mai modellati. Scheda della tabella: [`audit_trail_entries`](../analisi-post/db/audit/audit_trail_entries.md).

---

## 1. Decisione

Si crea **`Module.Audit`** (schema PostgreSQL `audit`), dodicesimo modulo del monolite, con un solo
compito: conservare e rendere consultabile il **registro trasversale delle azioni** — chi ha fatto cosa,
quando, su quale oggetto, in quale stabilimento.

**Non** si spostano in Audit i log che appartengono a un dominio. La regola che decide:

> Se il log serve **al modulo per funzionare** (stato, ritentativi, idempotenza, prova legale del
> proprio dominio), resta nel modulo. Se serve a ricostruire **chi ha fatto cosa attraverso più
> moduli**, va in Audit.

## 2. Cosa entra e cosa resta dov'è

### Entra in Audit (tre categorie)

| Categoria | Esempi | Fonte oggi |
|---|---|---|
| `operator` — azioni degli operatori di stabilimento | prenotazione creata/spostata/annullata, incasso, scontrino emesso, contatto modificato, listino cambiato | nessuna (pagina «Tutti i log», nuova) |
| `security` — eventi di autenticazione e sicurezza | login riuscito/fallito, blocco per troppi tentativi, reset password, 2FA, cambio ruolo, creazione/eliminazione utente | previsto in DB da [15-logs.md](15-logs.md), mai modellato |
| `platform` — azioni del systemadmin | approvazione/rifiuto stabilimento, modifica abbonamento, impersonazione ("accesso come gestore") | previsto come registro immutabile da [15-logs.md](15-logs.md) (Provv. Garante 27/11/2008) |

### Resta nel modulo proprietario

| Tabella | Modulo | Perché resta |
|---|---|---|
| `fiskaly_receipt_logs` | Fiskaly | Scritta nella **stessa transazione** dello scontrino e letta per riprendere un'emissione interrotta: se fosse asincrona, dopo un crash ci sarebbe uno scontrino senza traccia del suo stato |
| `payment_webhook_events` | Payment | Letta in modo sincrono per l'idempotenza dei webhook Stripe |
| `notification_deliveries` | Notification | Stato di consegna che guida i ritentativi |
| `switchboard_messaging_wa_event_logs`, `switchboard_voice_*` | Switchboard | Dati del centralino, con PII e retention propria già validata |
| `identity_account_contract_acceptances` | Identity | Prova legale di dominio, letta per decidere se mostrare il contratto |
| `privacy_compliance_audit_logs`, `privacy_retention_runs` | Privacy | Fanno parte del flusso di cancellazione e retention stesso |

Le due cose convivono: quando un operatore emette uno scontrino, il log tecnico resta in Fiskaly e
Fiskaly pubblica **anche** un evento di audit `fiskaly.receipt_issued` per la pagina «Tutti i log».

## 3. Perché un modulo e non le alternative

| Criterio | Modulo Audit (solo audit trasversale) | Log di dominio spostati in Audit |
|---|---|---|
| **Semplicità** | Un modulo, una tabella, un gestore degli eventi, un'API di lettura. I moduli usano solo un contratto in Shared | Ogni modulo perde il controllo dei propri log; scritture cross-modulo dentro flussi critici |
| **Architettura pulita** | Audit non dipende da nessun modulo e non ne conosce il dominio: salva codice azione, riferimento all'oggetto ed etichette fotografate | Viola "ogni modulo possiede i propri dati", oppure fa perdere l'atomicità |
| **Prestazioni** | Scrittura asincrona via outbox: nessun costo sul percorso caldo. La pagina legge **una** tabella invece di N | La ripresa di un'emissione fiscale leggerebbe da un altro schema o via contratto |
| **Microservizi** | Il caso più facile da estrarre: consuma solo eventi, nessuno lo chiama in modo sincrono | Dipendenza sincrona da Audit nei flussi di Fiskaly e Payment: estrarlo vorrebbe dire chiamate di rete dentro transazioni fiscali |
| **Immutabilità** | Un solo schema con l'utente applicativo autorizzato solo a `INSERT`/`SELECT` | Le stesse garanzie su tabelle che il modulo proprietario deve poter aggiornare: contraddittorio |
| **Retention e privacy** | Policy per categoria in un posto solo; un solo punto per l'informativa ai lavoratori e per l'export GDPR dei dati dello staff | Retention già validate per tabella (Fiskaly 12 mesi, WhatsApp 6–12) da rimappare senza beneficio |

**Alternative scartate:**
- **Audit dentro Infra.** Infra è il registro delle integrazioni partner; mescolarci l'audit lo
  trasformerebbe in un contenitore generico.
- **Una tabella di audit per modulo.** La pagina «Tutti i log» dovrebbe interrogare N moduli e unire i
  risultati, con paginazione e ordinamento difficili.

**Rischi accettati:** la voce compare con qualche secondo di ritardo (consistenza eventuale, accettabile
per una pagina di consultazione); il catalogo dei codici azione va mantenuto (§5); il modulo non deve
diventare una discarica (la regola del §1 serve a questo).

## 4. Come scrivono i moduli

- **Contratto in Shared**: un integration event `AuditEntryRecorded` (oppure l'interfaccia
  `IAuditTrail.Record(...)` che lo pubblica). I moduli non referenziano mai `Module.Audit`.
- **Outbox**: l'evento esce nella stessa transazione dell'azione che descrive, così un'azione salvata ha
  sempre la sua voce di audit e un'azione annullata dal rollback non ne lascia una falsa
  ([linee-guida-implementative-dotnet.md §6](pre/linee-guida-implementative-dotnet.md),
  [20-jobs.md](20-jobs.md)). La consegna è *at-least-once*: Audit deduplica su `source_event_id`.
- **Etichette fotografate**: nome dell'operatore, numero di prenotazione, postazione sono copiati
  nell'evento al momento dell'azione. La pagina non chiama gli altri moduli per mostrarli, e la voce
  resta leggibile anche se l'oggetto viene poi cancellato.
- **Niente dati personali oltre il necessario**: mai password, token, OTP, dati di carta; nel
  `metadata` solo ciò che serve a capire l'azione.
- **Sicurezza sincrona fuori da Audit**: il conteggio dei tentativi di login per bloccare un account
  resta in Identity (serve subito). Audit registra l'evento, non decide.

## 5. Catalogo dei codici azione

- Formato `<modulo>.<azione_al_passato>`, minuscolo, stabile come i codici d'errore
  ([standard-risposta-api-dotnet.md §4](pre/standard-risposta-api-dotnet.md)): `booking.created`,
  `booking.moved`, `payment.collected`, `fiskaly.receipt_issued`, `identity.login_failed`,
  `identity.user_created`, `identity.impersonation_started`.
- **Il backend restituisce il codice, il frontend lo traduce** e sceglie l'icona.
- Un codice rilasciato non si rinomina. Il catalogo vive accanto al contratto in Shared
  (`Shared/Contracts/Audit/AuditActionCodes.cs`: una classe di costanti per modulo più l'elenco `All` con la
  categoria di ogni codice) ed è la fonte del filtro «Tipologia di azione» (`GET /api/audit/action-codes`).
- L'elenco delle azioni operatore è **ancora da definire** (il docx di Impostazioni lo segna «in fase di
  aggiornamento»): i codici si aggiungono man mano che si sviluppano le API dei moduli. Ogni codice nuovo va
  anche tradotto nei `.resx` dell'export di Audit (`Module.Audit/Resources/AuditExport.*.resx`, chiave
  `Action_<codice con _ al posto del punto>`); senza traduzione l'export mostra il codice.
- **Nome dell'operatore**: se il modulo non lo passa, `AuditTrail` lo fotografa dall'account tramite il
  contratto `IUserDirectory` (Shared, implementato da Identity). I moduli devono passare solo le etichette
  del proprio dominio (numero di prenotazione, postazione).

## 6. Lettura

Catalogo completo degli endpoint in [88-api-audit.md](88-api-audit.md). In sintesi (decisioni del 2026-10-13):

- **Stabilimento** (`audit.read`, superadmin e admin): `GET /api/audit/entries`, `/entries/export`,
  `/action-codes`, `/actors`. Stabilimento sempre dal claim `property_id`. Vede le categorie `operator` e
  `security` del proprio stabilimento, **non** `platform`; non vede IP, user-agent, trace id né l'identità del
  systemadmin: un'azione fatta in impersonazione porta solo `impersonated: true`.
- **Systemadmin** (`platform.manage`): `GET /api/audit/admin/entries`, `/admin/entries/export`,
  `/admin/action-codes`, su tutte le categorie e tutti gli stabilimenti, con IP, user-agent e
  `impersonatorUserId`. Nessuna pagina del back-office le usa ancora.
- **Paginazione a offset** (`page`/`pageSize`, risposta `PagedResult`) come nel resto del progetto, non a
  cursore: la lista è sempre filtrata per stabilimento e il mockup ha pagine numerate con il totale.
  `pageSize` massimo 100.
- **Export**: file diretto, tradotto nella lingua dell'interfaccia di chi esporta, date in ora italiana,
  massimo 50.000 righe (oltre: 422 `audit.export_too_large`). L'export stesso entra nel registro
  (`audit.entries_exported`, `audit.platform_entries_exported`).

## 7. Immutabilità, retention e privacy

- **Immutabilità**: l'utente DB dell'applicazione ha su `audit` solo `INSERT` e `SELECT`; il `DELETE` è
  concesso al solo ruolo del job di retention. Nessun `UPDATE` previsto da nessuno.
- **Retention per categoria** (eseguita dal motore di retention, [81-retention-motore.md](81-retention-motore.md)),
  tre policy distinte da `target_filter` sulla colonna `category` (richiede `target_filter` nella chiave
  delle policy, [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md)):
  - `security`: **6 mesi** (già validato, [80-retention.md](80-retention.md));
  - `platform`: **12 mesi** (decisione del 2026-10-07; il Provv. Garante amministratori di sistema chiede almeno 6);
  - `operator`: **12 mesi** (D-66, decisa il 2026-10-13).

  Le tre policy sono seminate dalla migration `AddAuditRetentionPolicies` di Privacy, che porta anche
  `target_filter` nella chiave unica delle policy; nascono in prova come tutte le altre.
- **Statuto dei Lavoratori, art. 4**: un registro delle attività del personale è uno strumento da cui
  può derivare un controllo a distanza. Serve un'**informativa al personale** e una retention
  proporzionata; il registro va presentato come strumento organizzativo e di sicurezza, non di
  valutazione delle persone — [D-67](../analisi-post/db/_domande-committente.md).
- **GDPR**: le voci contengono dati personali dello staff (id, nome fotografato, IP). L'export e la
  cancellazione passano dal flusso di `Module.Privacy`, che tratta Audit come un modulo proprietario
  qualunque.

## 8. Tempi

**2026-10-07**: `Module.Audit` esiste con la sola persistenza; il contratto `AuditEntryRecorded` è in Shared e
Identity pubblica i propri eventi.

**2026-10-13**: scritti il gestore dell'evento (scrittura idempotente con `ON CONFLICT DO NOTHING`), le API di
lettura ed export ([88-api-audit.md](88-api-audit.md)) e le tre policy di retention. Restano prima del go-live
([85-promemoria.md](85-promemoria.md)): i permessi DB (solo `INSERT`/`SELECT`), il job delle partizioni future
(PROMEMORIA-85-4), l'outbox (PROMEMORIA-85-7) e l'informativa al personale (D-67).
