# Module.Privacy — gestione dei diritti dell'interessato e retention

> Modulo trasversale sottile per l'orchestrazione dei workflow GDPR (cancellazione, export, retention),
> nel monolite modulare descritto in [linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md).
> Complementare a [01-criticita-e-indicazioni-backend.md](01-criticita-e-indicazioni-backend.md) (criticità
> di sicurezza sui dati personali già censite nel legacy) e [03-gestione-dati.md](03-gestione-dati.md)
> (assegnazione delle tabelle ai domini C1-C8).
>
> **Nota sul nome**: il modulo si chiama `Module.Privacy`, non `Module.Compliance`. In
> [03-gestione-dati.md](03-gestione-dati.md) "compliance" è già usato come sottoarea di **C5 —
> Identity/Account** per `contract_acceptation` (accettazione di condizioni contrattuali B2B
> piattaforma↔stabilimento) — un concetto distinto dai diritti dell'interessato sui dati personali
> trattato qui. Usare nomi diversi evita di sovrapporre due significati di "compliance" nello stesso
> vocabolario di progetto.

---

## 1. Perché serve un modulo dedicato, e perché è sottile

Vincolo architetturale di partenza: **ogni modulo possiede il proprio schema**
([linee-guida-implementative-dotnet.md §1-2](pre/linee-guida-implementative-dotnet.md)). I dati personali di
un cliente sono sparsi per costruzione su più moduli (Booking, Payment, Notification, Switchboard,
Marketing, Identity...), quindi ogni operazione che riguarda "tutti i dati di una persona" — cancellazione
(Art. 17 GDPR), accesso/portabilità (Art. 15/20), retention programmata — è per forza **cross-modulo**.

Due collocazioni sbagliate, scartate:
- **Dominio Configurazioni (C2)**: in questo progetto C2 è catalogo/pricing/stagioni — dominio di business
  diverso, nessuna attinenza con i diritti dell'interessato.
- **Dentro Identity (C5)**: entangolerebbe un workflow di conformità trasversale con il core di
  autenticazione/autorizzazione, facendolo crescere oltre il suo perimetro naturale.

La collocazione coerente con la regola "config/infrastruttura trasversale → host; contenuto/proprietà →
modulo" ([linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md)) è un **modulo
sottile dedicato**: `Module.Privacy` **orchestra**, non possiede. Non contiene mai una riga di dato personale
di un cliente/utente — quelli restano dove sono, nello schema del modulo che li possiede e ne governa il
ciclo di vita per la propria logica di business.

---

## 2. Cosa possiede `Module.Privacy` (schema `privacy`)

| Aggregato/entità | Scopo |
|---|---|
| `ErasureRequest` | Stato di una richiesta di cancellazione (Art. 17): subject id, timestamp, stato per modulo coinvolto, esito finale (`Completed`/`PartiallyCompleted` con motivazioni) |
| `ExportRequest` | Stato di una richiesta di accesso/portabilità (Art. 15/20), stesso meccanismo di `ErasureRequest` |
| `RetentionPolicy` | Configurazione dichiarativa: schema/tabella, campo/i, data di riferimento, durata, azione, fonte normativa/motivazione — **letta** dai job di retention di ciascun modulo, mai eseguita da qui |
| `LegalHold` | Flag per singolo soggetto/record che sospende una cancellazione automatica per contenzioso in corso |
| `ComplianceAuditLog` | Traccia di ogni azione di cancellazione/anonimizzazione/export eseguita, riportata dai moduli proprietari — prova di conformità (Art. 5.2, accountability) |

**Cosa non fa mai**: non esegue DELETE/UPDATE sullo schema di un altro modulo. Orchestra via eventi, ma
l'esecuzione reale resta sempre nell'Infrastructure del modulo proprietario di quello schema — la stessa
regola di confine applicata a tutto il resto dell'architettura.

**Nessuna tabella esistente vi si sposta.** Non `contract_documents`/`contract_acceptation` (restano in C5:
sono un registro legale B2B con una propria logica di dominio — versionamento, hash del documento — non un
dato del ciclo GDPR-cliente), e in generale nessuna tabella con dati personali già assegnata a un dominio.
Il fatto che una tabella contenga dati personali (es. `contract_acceptation.ipAddress`/`userId`) non la
rende un candidato allo spostamento: la rende una **riga nella policy dichiarativa di retention** (§5),
eseguita dal job del modulo che già la possiede.

---

## 3. Quadro normativo applicabile — verificato, non presunto

SummerBooking tratta dati di clienti finali per conto di ~500 stabilimenti balneari/hotel, con sub-fornitori
extra-UE per alcuni servizi. Punti verificati sul testo di legge (non parafrasati a memoria):

- **Art. 5 GDPR** (minimizzazione, lett. c; limitazione della conservazione, lett. e): oggi violato
  sistematicamente nel legacy — campi PII in colonne denormalizzate mai lette, snapshot JSON senza retention
  definita, nessuna cancellazione/anonimizzazione programmata.
- **Art. 9 GDPR** (categorie particolari): il campo note libero di `service_reservation`/`bookings` può
  contenere dati sulla salute (esempio reale osservato: *"Cliente allergico lattosio"*). Il divieto generale
  dell'art. 9(1) copre esplicitamente i "dati relativi alla salute"; le allergie alimentari rientrano in
  questa categoria nell'interpretazione prevalente. Non essendo raccolto oggi con una base giuridica
  esplicita ex art. 9(2) — il consenso esplicito (lett. a) è l'unica base plausibile per un caso come
  questo, dato che le basi sanitarie (lett. h/i) non si applicano a un gestore di stabilimento
  balneare — va trattato come rischio da mitigare: minimizzazione del campo, non un consenso raccolto a
  posteriori per dati già impropriamente registrati. **Dubbio esplicito**: se esiste un'esigenza operativa
  reale (sicurezza alimentare nel servizio di ristorazione), continuare a raccoglierlo — con consenso
  esplicito distinto e campo dedicato — è una decisione di business/legale da far validare da un DPO/legale,
  non una scelta tecnica.
- **Art. 32** (sicurezza del trattamento): dati di pagamento in chiaro (`online_reservations.cardNumber/cvc`)
  sono anche una violazione PCI-DSS diretta, non solo GDPR — le due normative convergono sullo stesso fix.
- **Art. 44-49** (trasferimenti extra-UE): Stripe, Twilio, SendGrid, Meta/WhatsApp sono fornitori USA — serve
  un DPA firmato con Clausole Contrattuali Standard (SCC) per ciascuno, verificato per questo progetto e non
  assunto per il solo fatto che siano fornitori "enterprise".
- **Normativa fiscale italiana** (non GDPR, ma vincola la cancellazione): **due obblighi distinti**, non uno
  solo. (1) **Art. 2220 c.c.**: scritture contabili, fatture e documenti fiscali conservati **10 anni
  dall'ultima registrazione**. (2) **Art. 43 DPR 600/1973** (termine di accertamento): 4 anni dalla
  presentazione della dichiarazione (5 se omessa), ridotto di 2 anni per chi adotta la fattura elettronica
  (D.Lgs. 127/2015) — termine più breve, che **non sostituisce** l'obbligo civilistico più lungo. Regola
  operativa sicura: applicare il termine più lungo (10 anni, art. 2220 c.c.), che soddisfa automaticamente
  anche il secondo. **Eccezione**: in presenza di contenzioso/accertamento in corso, la conservazione si
  estende fino alla conclusione della vertenza — gestita con un `LegalHold` esplicito (§2), impostato
  dall'ufficio legale/amministrativo, mai dal job automatico.
- **Art. 17(3)(b) GDPR** — eccezione per obbligo legale, testo verificato: *"[il diritto alla cancellazione]
  non si applica nella misura in cui il trattamento sia necessario [...] per adempiere un obbligo legale che
  richieda il trattamento [...]"* — è la base che copre la non-cancellabilità dei dati fiscali/Fiskaly
  durante il periodo di conservazione obbligatoria.
- **Art. 30** (registro dei trattamenti) e **Art. 37** (DPO): da valutare in base al volume reale — il
  trattamento sistematico di dati di pagamento e (incidentalmente) di salute su larga scala rende opportuna
  una verifica formale con un consulente privacy sulla necessità di nomina DPO; non è una decisione tecnica.

**Dubbio esplicito aperto** (richiede parere di uno specialista in diritto delle telecomunicazioni): non è
stato possibile stabilire con certezza se SummerBooking, come cliente API di Telnyx (non operatore
licenziatario), sia essa stessa soggetta all'obbligo di conservazione di 72 mesi sui dati di traffico
telefonico/telematico del D.L. 7/2015 conv. L. 43/2015 (antiterrorismo), o se tale obbligo gravi solo su
Telnyx come fornitore. Rilevante per `switchboard_config`, `switchboard_call_log`, `switchboard_wa_msg_log`
(§5).

---

## 4. Campi PII per tabella: cosa fare e perché

> ✅ = durata **validata dal titolare** il 2026-09-17
> ([`82-retention-data.md`](82-retention-data.md)); i nomi di tabella/campo di questa colonna sono quelli
> **legacy** (è una mappa di cosa fare in migrazione), i target del nuovo schema e le righe di policy
> corrispondenti sono nel
> [§ Seed di `privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md).

| Tabella | Campo | Cosa fare | Perché |
|---|---|---|---|
| `online_reservations` | `cardNumber`, `cvc`, `cardHodler`, `expiry` | **Eliminare la colonna** (non solo deprecarla): tokenizzare lato client con Stripe.js/Elements, il backend non riceve mai il PAN | Violazione PCI-DSS diretta + dato di pagamento non necessario una volta tokenizzato (Art. 5.1.c) |
| `fiskaly_account` | `taxpayer_fiscal_code`, `taxpayer_fisconline_fiscal_code`, `taxpayer_fisconline_pin`, `taxpayer_fisconline_password`, `subject_credential_key/secret` | Cifrare **tutti** questi campi (oggi solo `taxpayer_fisconline_password` lo è) con la stessa chiave AES-256-GCM già in uso; accesso solo da servizio applicativo dedicato, mai restituiti da un endpoint di lettura generico | Credenziali che autenticano l'emissione di scontrini verso l'Agenzia delle Entrate; un leak abilita frode fiscale, non solo un data breach GDPR |
| `switchboard_config` | `sip_password`, `reg_identity_doc_number`, `reg_personal_tax_code`, `reg_date_of_birth`, `reg_place_of_birth`, id documenti caricati (`reg_*_doc_id`) | Cifrare a riposo; accesso ristretto a ruolo SystemAdmin con audit log di ogni lettura; non includere mai in export/log | Documenti d'identità e codice fiscale personale = dati ad alto rischio in caso di leak |
| `switchboard_config` | `sip_password` restituita via email | Rimuovere l'invio in chiaro via email; se serve comunicarla, canale cifrato/one-time link | Password inviata in chiaro via un canale non sicuro |
| `users` | `password` | Aumentare bcrypt cost factor (≥12, vedi [01-criticita §7](01-criticita-e-indicazioni-backend.md)); mai restituito da alcun endpoint | Cost factor 8 è sotto lo standard raccomandato |
| `users` | `email`, `mobile`, `firstname`, `lastname`, `profilePic`, `googleId/facebookId/appleId` | Mascheramento **strutturale** (DTO diverso per lista vs dettaglio, non un `if` a runtime) su ogni endpoint che non richiede il dato in chiaro; no export senza autorizzazione | Minimizzazione — nel legacy il mascheramento esiste nel codice ma è disattivato per bug (ternario no-op) |
| `users` | `birthDate`, `gender` | **Rimuovere la colonna** nella nuova base dati (campi morti, mai raccolti da alcun frontend) | Dato personale raccolto in schema ma non usato = solo rischio, zero beneficio |
| `users` | `two_factor_otp` | TTL breve, cancellazione dopo il consumo o la scadenza, mai loggato; i **log di autenticazione** che lo circondano vivono **6 mesi in DB** ✅ | Dato temporaneo sensibile per l'autenticazione |
| `customers` | `fullName`, `email`, `mobile`, `otherMobileNumber`, `phone`, `address`, `city`, `cap`, `dateOfbirth`, `fiscalCode`, `vat` | Consolidare i tre campi telefono ridondanti in uno; retention: **anonimizzare dopo 48 mesi** dall'ultima prenotazione ✅ (valore deciso dal titolare il 2026-09-17), cancellazione completa a 10 anni se legata al fiscale. Resta una **policy aziendale**, non un termine imposto dal GDPR: da far confermare al DPO/legale | Minimizzazione (Art. 5.1.c) e limitazione della conservazione (Art. 5.1.e) |
| `customers` | `note` | Validare/limitare il contenuto libero; se necessario per esigenze operative, trattarlo esplicitamente come **dato di categoria particolare (Art. 9)** con accesso ristretto, non come nota generica. Retention **N−1** ✅: il campo si azzera **una stagione prima** del resto dell'anagrafica (48 mesi) — riga di policy dedicata, non un effetto collaterale | Rischio concreto già osservato: dati sulla salute infilati in un campo non pensato per questo |
| `customers` | `isBlackList` | Documentare la base giuridica (legittimo interesse dello stabilimento) e prevedere diritto di rettifica/contestazione. **Revisione annuale a carico del gestore dello stabilimento** ✅ (decisione del titolare): promemoria nel gestionale, non un job che cancella il flag | Dato valutativo su una persona, impatta l'accesso a un servizio |
| `bathouses` / `properties` | `mobile`, `email`, `fullName`, `address` | Trattare come dato personale solo se il titolare è persona fisica/ditta individuale; comunque mascherare nella ricerca pubblica (bug noto: mascheramento no-op) | [01-criticita §5 "Mascheramento PII disattivato"](01-criticita-e-indicazioni-backend.md) |
| `contract_acceptation` | `ipAddress` | Troncamento dell'ultimo ottetto a **24 mesi** ✅ (confermato dal titolare); la riga vive contratto + 10 anni a fini probatori | Dato personale (Art. 4.1) raccolto senza policy di retention oggi |
| `email_logs` | `emailTo`, `body` (spesso contiene nome/cognome nel testo) | Troncamento del `body` a **150 giorni** ✅ *(la proposta era 90)*, cancellazione della riga a 12 mesi; mai includere PAN/OTP nel corpo loggato | I log email sono spesso l'ultimo posto dove i dati sopravvivono dopo una cancellazione altrove |
| `bathhouse_email_configrations` | `smtpPassword` | Cifrare a riposo, mai restituita in chiaro dall'API (oggi lo è) | Credenziale, non solo dato personale |
| `contact_list` / `contact_list_member` | riferimento a `customers` (nome/email/telefono raggiungibili) | Autenticazione + autorizzazione per-stabilimento sull'endpoint di lettura (oggi assente) | Endpoint pubblico che espone rubrica completa di clienti di terzi |
| `subscribes` | `email` | Tracciare **consenso esplicito** (timestamp, versione informativa, fonte) con doppio opt-in; cancellazione self-service con conferma via email | ePrivacy Directive + Art. 7 GDPR: oggi non c'è prova che il consenso sia mai stato dato |
| `csv_manager_details` | `fullName`, `email`, `phoneNumber`, `message` | Autenticazione/rate-limit sull'endpoint pubblico; retention **12 mesi** ✅ (è un lead, non un cliente) | Oggi endpoint pubblico senza rate limit, dati salvati e inoltrati via email a indirizzo hardcoded |
| `marketing_email_campaign` / `marketing_automation` | `recipientList`/`recipients` (JSON array di email) | Verificare che ogni destinatario abbia consenso marketing attivo **al momento dell'invio**, non solo al momento della creazione della lista | Il consenso può essere revocato tra la creazione della campagna e l'invio |
| `switchboard_call_log` / `switchboard_wa_msg_log` | numeri, `contact_name`, `message_body`, `raw_payload` completo | Retention decisa dal titolare ✅: **30 giorni** il log diagnostico di chiamata, **6 mesi** il registro chiamate e il log webhook WhatsApp, in entrambi i casi **cancellazione** (non pseudonimizzazione). Il dubbio **D.L. 7/2015** (72 mesi) resta aperto → le policy nascono `basis_source = disputed` e **in dry-run** | Payload di terze parti con dati personali completi, oggi senza scadenza — vedi il dubbio sul D.L. 7/2015 in §3 |

### Esempio concreto — mascheramento strutturale, non un flag a runtime

**Prima** (il bug reale del legacy, `property.controller.js:313-322`): un ternario che restituisce lo stesso
valore in entrambi i rami, quindi non maschera nulla nonostante la funzione di masking esista nel codice.

**Dopo**: il mascheramento non è un `if` — è espresso con due DTO diversi per due casi d'uso diversi, così
non esiste un percorso di codice in cui il DTO "lista" possa contenere il dato in chiaro:

```csharp
// Application/Dtos — endpoint di ricerca/lista pubblica: nessun campo di contatto in chiaro
public sealed record CustomerListItemDto(Guid Id, string FullName, string MaskedEmail, string MaskedMobile);

// Application/Dtos — endpoint di dettaglio: richiede autorizzazione per-risorsa
public sealed record CustomerDetailDto(Guid Id, string FullName, string Email, string Mobile);
```

L'endpoint di ricerca mappa **sempre** verso `CustomerListItemDto`; l'endpoint di dettaglio richiede la
policy di autorizzazione per-risorsa prima di restituire `CustomerDetailDto`. Il tipo di ritorno, non un
flag, garantisce che il dato in chiaro non possa uscire dal percorso sbagliato — coerente con
[50-standard-di-codifica.md §4](50-standard-di-codifica.md).

---

## 5. API per cancellazione ed export (Art. 15/17/20)

```
DELETE /api/customers/{id}/personal-data        → avvia la richiesta di cancellazione (Art. 17)
GET    /api/customers/{id}/personal-data-export → esporta i dati personali posseduti (Art. 15/20)
GET    /api/customers/{id}/erasure-requests/{requestId} → stato della richiesta (accolta / parziale per obbligo legale / rifiutata)
```

**Flusso:**

1. L'endpoint `DELETE` **non cancella subito**: `Module.Privacy` crea un `ErasureRequest` con stato
   `Pending` e pubblica un evento di dominio `CustomerErasureRequested(customerId)`.
2. Ogni modulo che possiede dati riconducibili a quella persona (Booking, Payment, Notification,
   Switchboard, Marketing, Identity) si sottoscrive all'evento e applica **la propria regola**, non una
   cancellazione generica:
   - **Nessun vincolo di conservazione** (marketing, chatbot, log chiamate/WhatsApp oltre la retention) →
     cancellazione fisica.
   - **Vincolo di integrità referenziale** (prenotazioni, pagamenti collegati a fatture) →
     **anonimizzazione**: sostituire nome/email/telefono con placeholder (`"Cliente cancellato #<id>"`),
     mantenere id e importi per la contabilità.
   - **Vincolo di conservazione fiscale** (Fiskaly, fatture) → **non cancellare né anonimizzare** fino alla
     scadenza del termine legale (§3); la richiesta risulta "accolta parzialmente", con motivazione
     esplicita (Art. 17.3.b).
   - Ogni modulo risponde con un evento `ErasureAppliedInModule(requestId, modulo, esito)`.
3. Quando tutti i moduli coinvolti hanno risposto, `Module.Privacy` consolida la richiesta in `Completed` (o
   `PartiallyCompleted` con l'elenco dei dati trattenuti e il motivo/scadenza) — coerente con l'obbligo di
   **rispondere entro un mese** (Art. 12.3 GDPR, estendibile a tre mesi per richieste complesse, che questo
   caso è).
4. **Audit**: la richiesta stessa (chi, quando, cosa è stato fatto) va conservata con una retention minima —
   è la prova di conformità, non un dato da minimizzare a zero.

Lo stesso meccanismo copre l'**export** (Art. 15/20): ogni modulo risponde con il proprio sottoinsieme di
dati, `Module.Privacy` li aggrega in un unico JSON scaricabile.

### Rapporto con il soft-delete

Non sono la stessa cosa e non vanno confusi in un solo flag. Il soft-delete (`deleted_at` + Global Query
Filter EF, [03-gestione-dati.md §3.5](03-gestione-dati.md)) è uno **stato di ciclo di vita applicativo**:
nasconde il record dai flussi normali ma non tocca il contenuto, il dato personale resta fisicamente
leggibile. La cancellazione GDPR (Art. 17) richiede la rimozione o anonimizzazione **effettiva** del
contenuto. Un cliente può essere soft-deleted (account chiuso) mentre i suoi dati fiscali restano leggibili
per obbligo di conservazione — sono due fatti indipendenti. I campi vanno comunque cancellati/anonimizzati
realmente quando l'obbligo che li tratteneva scade: il soft-delete da solo non soddisfa mai l'Art. 17.

### Natura del flusso: non una transazione, un saga in-process

Quanto sopra **non è una transazione atomica** — è coerente con la regola già fissata *"ogni modulo scrive
nella sua transazione [...] coordina con eventi (+ outbox)"*
([linee-guida-implementative-dotnet.md §6](pre/linee-guida-implementative-dotnet.md)). È già, di fatto, un
**process manager/saga in-process** (l'aggregato `ErasureRequest` traccia lo stato per modulo — orchestrazione
preferita alla pura choreography perché serve un esito finale certo e auditabile). Quando un modulo verrà
estratto in microservizio, il bus in-memory sarà sostituito da un broker reale (già previsto, stesso
documento §6: *"gli handler non cambiano"*) e questo flusso diventerà una **saga distribuita orchestrata** in
senso proprio — transizione meccanica, non un redisegno, perché il design odierno è già strutturato come
tale.

---

## 6. Retention: policy dichiarativa + esecuzione decentrata

Per i casi in cui l'unica azione richiesta è "supera la durata N → cancella/anonimizza", un job schedulato è
sufficiente — ma non un'unica utility monolitica che tocca tutti gli schemi (violerebbe "uno schema per
modulo").

- **Policy dichiarativa** (non codice): per ogni riga, `schema.tabella`, campo o gruppo di campi, sorgente
  della data di riferimento, durata, azione (`delete_row | null_fields | anonymize | archive_cold`),
  riferimento normativo/motivazione. Vive come configurazione in `Module.Privacy` (`RetentionPolicy`, §2).
- **Esecuzione decentrata**: ogni modulo espone un job (`BackgroundService`/Hangfire, stesso pattern di
  [20-jobs.md](20-jobs.md)) che applica la propria fetta di policy sulla propria tabella tramite il proprio
  `DbContext` — mai una query cross-schema da un servizio centrale.
- **Dry-run obbligatorio**: il job produce un report (righe che verrebbero toccate, azione, motivo) senza
  eseguire nulla, revisionabile da SystemAdmin, prima di abilitare l'esecuzione reale in produzione.
- **Legal hold**: un flag esplicito (`LegalHold`, §2) per singolo record/cliente che sospende l'eliminazione
  automatica in presenza di un contenzioso in corso — impostato manualmente dall'ufficio legale/
  amministrativo, mai dal job stesso.
- **Audit**: ogni riga toccata (id, campo, azione, policy applicata, motivazione) loggata in
  `ComplianceAuditLog` con una propria retention breve (1-2 anni) — prova di conformità richiesta
  dall'accountability (Art. 5.2 GDPR).

### Quando serve una data di riferimento dedicata

Non per ogni campo automaticamente:
- Se la retention decorre dalla **creazione del record**, la colonna `created_at` standard
  ([50-standard-di-codifica.md §3](50-standard-di-codifica.md)) basta.
- Se decorre da un **evento successivo** (es. "clienti inattivi da N anni" parte dall'ultima prenotazione,
  non dalla creazione dell'anagrafica), serve una data mantenuta (`customers.last_activity_at`, aggiornata
  dagli eventi di dominio) o calcolata a query-time.
- Se un **sottoinsieme di campi** della stessa riga ha una retention diversa dal resto (es.
  `customers.note`), serve sapere quando *quel campo* è stato scritto l'ultima volta — una colonna dedicata
  solo se non esiste già un audit trail delle modifiche.

### Dati legacy con data di inserimento sconosciuta

Usare la **data di go-live della migrazione** come riferimento conservativo, mai una data anteriore stimata
a caso — allunga la retention reale per i record più vecchi ma non la accorcia mai (nessuna cancellazione
prematura). Da documentare esplicitamente come limitazione nota nel registro dei trattamenti (Art. 30 GDPR):
non è una violazione nascosta, è una scelta motivata e tracciata, coerente con l'accountability dell'Art. 5.2.

### Tabella delle durate

Colonna "Fonte": **Legge** = obbligo verificato su un testo normativo; **Policy consigliata** = proposta
ragionevole da far approvare da un DPO/legale prima dell'attivazione, non un termine imposto dal GDPR;
**Dubbio** = qualificazione giuridica non risolta, non fissare la durata senza parere specialistico.

> **Aggiornamento 2026-09-17/21 — durate operative validate dal titolare.** Quelle che qui erano
> "proposte" o "N da definire" sono ora **decise**: il verbale riga-per-riga è in
> [82-retention-data.md](82-retention-data.md), l'elaborazione completa in
> [80-retention.md §3/§7](80-retention.md). Le righe sotto riportano il valore **validato**, con la
> proposta originaria fra parentesi dove è cambiata.
>
> **La validazione del titolare non è un parere legale**: la colonna "Fonte" resta invariata — una durata
> decisa dal titolare resta `Policy consigliata` (o `Dubbio`) finché un DPO/legale non la qualifica come
> `Legge`. È la stessa distinzione che il seed di
> [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md) porta nella
> colonna `basis_source`, e il motivo per cui le righe `disputed` nascono `is_dry_run_only = true` anche
> se il titolare ha indicato un numero.

| Tabella (target nuovo schema) | Campo/i | Durata massima ✅ validata | Fonte |
|---|---|---|---|
| `fiskaly_accounts`, `fiskaly_receipts`, movimenti `payment_*` collegati a prenotazioni/transazioni | dati fiscali/contabili | **10 anni** dall'ultima registrazione (esteso in caso di contenzioso — legal hold); online **36 mesi** per gli scontrini *(proposta: 24)*, poi archiviazione fredda | **Legge**: art. 2220 c.c.; art. 43 DPR 600/1973 come termine minimo più breve, assorbito dal primo |
| `payment_pos_charges` (spese POS esterne) | importi/righe di addebito | **10 anni** *(la precedente ipotesi "archivio cancellato a 5 anni" è stata **corretta** dal titolare: obbligo fiscale pieno)* | **Legge**: art. 2220 c.c. |
| `switchboard_voice_number_registrations` | `reg_*` (KYC numero telefonico) | Durata della registrazione attiva + periodo post-cessazione da definire | **Dubbio**: qualificazione di SummerBooking come cliente-API o soggetto regolato (§3) |
| `switchboard_voice_call_records` | numeri chiamante/chiamato | **6 mesi** poi cancellazione *(proposta: 12 mesi con pseudonimizzazione)* | **Policy consigliata** — resta aperto il dubbio D.L. 7/2015 (72 mesi): la policy nasce in dry-run |
| `switchboard_voice_call_diagnostics` | payload completo con numeri | **30 giorni** *(proposta: 30–90)* | **Policy consigliata** |
| `switchboard_messaging_wa_event_logs` | numeri, `message_body`, `raw_payload` | **6 mesi** poi cancellazione *(proposta: 6–12 mesi con pseudonimizzazione del payload)* | **Policy consigliata** — stesso dubbio sull'applicabilità del D.L. 7/2015 (72 mesi) |
| `identity_customer_customers` | anagrafica, contatti, no dati fiscali | **48 mesi** dall'ultima prenotazione → pseudonimizzazione; cancellazione completa a 10 anni se legata al fiscale *(proposta: 24–36 mesi)* | **Policy consigliata** (analogia con prescrizione ordinaria, art. 2946 c.c.) |
| `identity_access_users` | anagrafica, contatti dello staff | **12 mesi dalla revoca** → **cancellazione** *(proposta: pseudonimizzazione a 24 mesi)* — per l'audit fiscale basta il riferimento allo **stabilimento**, non l'identità dell'utente | **Policy consigliata** |
| `identity_customer_customers.note`, note su prenotazioni/servizi | contenuto potenzialmente Art. 9 | **N−1** (una stagione in meno della prenotazione che la contiene), poi cancellazione | **Legge** (Art. 5.1.e + Art. 9: nessuna base per conservazione prolungata senza consenso esplicito dedicato) |
| `identity_account_contract_acceptances` | `ip_address` | Troncamento dell'ultimo ottetto a **24 mesi**; la riga vive contratto + 10 anni | **Policy consigliata** |
| `notification_deliveries` | `rendered_body` | **150 giorni** *(proposta: 90)*; metadati d'invio 12 mesi | **Policy consigliata** |
| `notification_inapp_messages` | testo con nome cliente/prenotazione | **6 mesi** *(proposta: 6–12)* | **Policy consigliata** |
| `notification_marketing_*` (campagne, automazioni, liste) | email destinatari | **24 mesi dall'ultimo uso**; snapshot destinatari **6 mesi**; lista di **soppressione conservata a vita** per provare l'opt-out | **Policy consigliata** (prassi comune delle Autorità di controllo) |
| `notification_establishment_claim_requests` | lead (nome, email, telefono, messaggio) | **12 mesi** *(proposta: 6–12)* | **Policy consigliata** |
| `payment_customer_gateway_profiles` | mappatura utente → Stripe Customer (token, mai PAN) | **24 mesi di inattività** — **la chiusura dell'account non anticipa la cancellazione** *(proposta: cancellazione immediata alla chiusura)* | **Policy consigliata** |
| `chatbot_conversation_turns` | `input_text`/`output_text` | **12 mesi** (confermato) | **Policy consigliata** — vedi [D-54](../analisi-post/db/_domande-committente.md) |
| `identity_access_user_auth_challenges` (OTP/2FA) | OTP | Fino al consumo o alla scadenza (minuti); log di autenticazione **6 mesi in DB** *(proposta: 6–12)* | **Policy consigliata** |

### Utility di supporto — prompt per generarla (Python, GUI Windows)

Per la consultazione/revisione della policy da parte di chi non ha un ambiente .NET (es. un responsabile
privacy), **non** per eseguire la cancellazione stessa:

> Crea un'applicazione desktop Python per Windows (usa Tkinter o PySimpleGUI, nessuna dipendenza che richieda
> compilazione) che gestisce una policy di data retention GDPR letta/scritta da un file JSON o da una tabella
> di un database PostgreSQL (connessione via `psycopg2`, parametri di connessione in un file di config
> separato, mai hardcoded). La finestra principale mostra una tabella con colonne: Schema, Tabella, Campo/i,
> Data di riferimento (nome colonna sorgente), Durata (numero + unità: giorni/mesi/anni), Azione
> (delete_row/null_fields/anonymize/archive_cold), Base giuridica/motivazione, Fonte (Legge/Policy interna),
> Legal hold (sì/no). Deve permettere: aggiungere/modificare/eliminare una riga di policy con un form di
> dettaglio; validare che "Durata" sia un numero positivo e che "Azione" sia uno dei valori consentiti;
> esportare la tabella corrente in CSV per revisione; un pulsante "Genera report dry-run" che, dato un
> collegamento a un database PostgreSQL di sola lettura, esegue per ogni riga di policy una query di conteggio
> (quante righe della tabella target supererebbero la durata configurata rispetto a oggi) e mostra il
> risultato in una finestra separata, senza mai eseguire DELETE/UPDATE. Nessuna scrittura sul database di
> produzione: l'applicazione è uno strumento di configurazione e verifica, l'esecuzione reale della
> cancellazione resta nei job .NET del backend. Includi un file `requirements.txt` e un file `README.md` con
> le istruzioni di avvio su Windows.

---

## 7. Riepilogo dei dubbi legali aperti

Da chiudere con uno specialista prima di fissare parametri definitivi.

> **La validazione del titolare (2026-09-17) non ha chiuso nessuno di questi punti.** Il titolare ha
> deciso i tempi **operativi** — quanto tenere un dato online, quando archiviarlo, quando cancellarlo —
> e sono decisioni sue da prendere. Non ha dato (né poteva dare) un parere legale: un termine deciso
> resta `recommended_policy` o `disputed` nel seed di
> [`privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md) finché un
> DPO/legale non lo qualifica `law`, e le righe `disputed` restano `is_dry_run_only = true`.
>
> **Aggiornamento del 2026-09-30: il punto 1 è stato chiuso**, e non da un parere legale ma da una
> verifica di fatto — l'obbligo vincola chi ha un'autorizzazione generale AGCOM, e SummerBooking non ce
> l'ha. Il testo originale di questo cappello usava il punto 1 come esempio del perché nessun dubbio
> fosse chiuso: vale ancora per i punti 2, 3 e 4, non più per il primo. Le due policy a 6 mesi
> (registro chiamate e log webhook WhatsApp) possono quindi diventare eseguibili.

1. ~~**Retention telefonica**: SummerBooking come cliente-API di Telnyx è soggetta al D.L. 7/2015 (72 mesi sui
   dati di traffico), o l'obbligo grava solo su Telnyx come operatore?~~ → ✅ **CHIUSA il 2026-09-30**,
   e il dubbio era mal posto in due modi.
   - **La norma citata è sbagliata**: non il D.L. 7/2015 ma l'**art. 24 della L. 167/2017**, che porta a
     72 mesi **in deroga** e solo per i reati di cui agli artt. 51 c.3-quater e 407 c.2 lett. a) c.p.p.
     Il termine ordinario è quello dell'**art. 132 del Codice Privacy**: 24 mesi per il traffico
     telefonico, 12 per il telematico, 30 giorni per le chiamate senza risposta.
   - **L'obbligo non ci riguarda.** L'art. 132 vincola il «fornitore di servizi di comunicazione
     elettronica **accessibili al pubblico**», cioè chi ha un'autorizzazione generale AGCOM.
     SummerBooking **non ne ha una** (confermato dal committente il 2026-09-30): compra numeri e
     traffico da Telnyx, che è l'operatore e su cui l'obbligo grava. I **6 mesi** già validati dal
     titolare restano, e `basis_source` va portato da `disputed` a `recommended_policy`.

   Verifica e riferimenti in
   [`_mancanti-da-sistemare.md`](../analisi-pre/db-no-copied/_mancanti-da-sistemare.md), voce **A2**.
2. **Campo note / Art. 9**: se esiste un'esigenza operativa reale per raccogliere dati sanitari (allergie),
   la base giuridica (consenso esplicito dedicato) e il disegno del campo vanno validati da un DPO/legale
   prima di riattivare la raccolta in qualunque forma. (§3)
3. **Necessità di un DPO** (Art. 37): da valutare formalmente in base al volume reale di trattamento di dati
   di pagamento e (incidentalmente) di categorie particolari. (§3)
4. **DPA/SCC con i fornitori extra-UE** (Stripe, Twilio, SendGrid, Meta): da verificare che esistano
   effettivamente per questo progetto, non da assumere. (§3)
