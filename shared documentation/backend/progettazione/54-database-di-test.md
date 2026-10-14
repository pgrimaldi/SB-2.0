# Database di test aggiornato da produzione — pipeline e sanificazione

> Come si alimenta un ambiente di test/sviluppo con dati realistici **senza mai portarci dati personali
> reali né credenziali reali** verso servizi esterni. Sostituisce l'attuale copia parziale `local_sb`
> ([00-struttura.md](00-struttura.md)).
>
> Complemento a [11-modulo-privacy.md](11-modulo-privacy.md) (mappatura PII, logica di anonimizzazione
> riusata qui), [40-backup.md](40-backup.md) (gli snapshot/`pg_dump` da cui parte la pipeline),
> [17-secrets.md](17-secrets.md) (secret di configurazione — tema distinto dalle credenziali *a DB*
> trattate qui), [02-strategia-di-test.md](02-strategia-di-test.md) (i test non contattano mai i
> provider reali), [../infra/infrastruttura-aws.md §3.7](../../../../../infra/infrastruttura-aws.md) (job
> disaccoppiati dal web) e [80-retention.md](80-retention.md).
>
> **Documento vivo**: aggiornare la tabella §4 quando cambia lo schema o si aggiunge un provider.

---

## 0. Principio guida

Copiare dati reali di produzione in un ambiente di test **non è un refresh neutro**: è un **nuovo
trattamento** di dati personali in un ambiente con controlli più deboli (più sviluppatori con accesso,
meno audit, meno segmentazione di rete). La via che evita di dover replicare in test **tutte** le
protezioni di produzione è **non portarci mai dati personali reali**: sanificare *prima* che i dati
siano raggiungibili da uno sviluppatore.

---

## 1. Non è solo un refresh di dati

Soggetto agli stessi principi già applicati in [11-modulo-privacy.md](11-modulo-privacy.md):
minimizzazione (Art. 5.1.c), sicurezza del trattamento (Art. 32), privacy by design (Art. 25).

**Non è un rischio teorico in questo progetto**: durante la mappatura PII
([11-modulo-privacy.md §4](11-modulo-privacy.md)) è emerso che il DB di sviluppo attuale (`local_sb`)
contiene già almeno un caso concreto di dato personale reale finito per errore in un ambiente non di
produzione — l'email di un dipendente del team, in un record di test della tabella `hotels`. È la
controprova diretta di quanto sia facile "sporcare" un ambiente di test con PII vere senza un processo
esplicito che lo impedisca.

**Rischio operativo distinto dal GDPR, altrettanto grave**: le credenziali verso servizi esterni
presenti *a DB* (`fiskaly_account.subject_credential_*`, numeri/config `switchboard_config`, eventuali
customer id Stripe) **non vanno mai copiate as-is** in un ambiente di test — un job o un test eseguito
per errore contro dati di produzione copiati potrebbe **emettere uno scontrino fiscale reale**, inviare
un **WhatsApp reale** a un cliente reale, o (se i dati carta fossero ancora presenti) tentare un
**addebito reale**. Va risolto **a parte** dall'anonimizzazione dei dati personali: sostituzione
sistematica con credenziali sandbox (§4).

---

## 2. Due ambienti di test, stessa pipeline sanificata

| Ambiente | Chi vi accede | Sorgente dei dati |
|---|---|---|
| DB locale per sviluppatore (sostituisce `local_sb`) | Ogni sviluppatore, macchina individuale | **Sempre** l'output sanificato della pipeline (§3), mai un dump grezzo di produzione |
| DB di staging/QA condiviso | Team QA, ambiente di staging | Stessa pipeline, eseguita con cadenza pianificata (§6) |

Non esiste un percorso in cui un dump di produzione raggiunge direttamente uno di questi due ambienti
senza passare dalla sanificazione — nemmeno "una tantum per un'urgenza": è esattamente il tipo di
eccezione che ha prodotto il leak osservato in `local_sb`.

---

## 3. Pipeline proposta (coerente con l'infrastruttura AWS già scelta)

1. **Estrazione**: snapshot RDS automatico già esistente ([40-backup.md](40-backup.md)) o, per non
   competere con il traffico di produzione, una **read replica** dedicata alla sola estrazione.
2. **Restore isolato**: lo snapshot viene ripristinato su un'istanza RDS **temporanea**, in una subnet
   dedicata, **non raggiungibile da alcuno sviluppatore** (nessun security group che apra l'accesso
   umano) — solo il job di sanificazione vi si connette.
3. **Sanificazione**: uno script per modulo (§5) applica anonimizzazione/pseudonimizzazione sui campi
   PII e **sostituzione** (non semplice cifratura) delle credenziali verso servizi esterni con valori
   sandbox.
4. **Verifica**: un controllo automatico (query di conteggio) conferma che **zero righe** nelle tabelle
   elencate in §4 contengano ancora pattern riconoscibili come dati reali (email con domini noti
   dell'azienda, prefissi di chiave `sk_live_`/`live_`, ecc.). La pipeline **si ferma** e non pubblica
   l'output se il controllo fallisce — non pubblica "silenziosamente con un warning".
5. **Pubblicazione**: solo a verifica superata, un nuovo `pg_dump`/snapshot dell'istanza sanificata
   viene reso disponibile come sorgente per il refresh di `local_sb`/staging.
6. **Smaltimento**: l'istanza RDS temporanea (con i dati non sanificati) viene **terminata** subito
   dopo, non lasciata "per comodità" — è l'unico punto della pipeline con PII reali e credenziali reali
   insieme, va minimizzata la sua finestra di esistenza.
7. **Audit**: ogni esecuzione (quando, da chi avviata, esito del controllo del punto 4) va loggata —
   stessa logica di accountability di `ComplianceAuditLog` ([11-modulo-privacy.md §2](11-modulo-privacy.md)).

---

## 4. Cosa anonimizzare vs cosa sostituire con valori sandbox

| Categoria | Esempio di campi | Trattamento |
|---|---|---|
| **Dati personali (PII)** | `customers.fullName/email/mobile/fiscalCode`, `users.email/mobile`, `switchboard_config.reg_*`, `contract_acceptation.ipAddress` | **Anonimizzazione/pseudonimizzazione deterministica** (§5): non `NULL`/stringa fissa uguale per tutti, ma un valore fittizio **coerente per record** (stesso cliente → stesso nome fittizio in tutte le tabelle), per mantenere significativi i JOIN durante i test |
| **Dati potenzialmente Art. 9** | `customers.note`, `service_reservation.note` | **Sostituzione completa** con testo neutro, non anonimizzazione parziale — categoria a rischio più alto, nessun motivo di conservarne anche una forma offuscata in test |
| **Credenziali verso servizi esterni** | `fiskaly_account.subject_credential_*`, `taxpayer_fisconline_*` | **Sostituzione** con credenziali sandbox Fiskaly (ambiente `test`, coerente con lo sviluppo — [17-secrets.md §4](17-secrets.md)) — mai un valore anonimizzato ma comunque valido |
| **Credenziali verso servizi esterni** | `bathhouse_email_configrations.smtpPassword`, `switchboard_config.sip_password` | Sostituzione con credenziali sandbox/dummy, mai propagate |
| **Riferimenti a pagamento** | eventuale `stripeCustomerId`/`stripeCustomerCardId` | Sostituzione con id Stripe di **test** (sandbox, chiavi `sk_test_`/`pk_test_`) — mai un id reale, anche se punta solo a un token e non al PAN |
| **Log con testo libero che incorpora PII** | `email_logs.body`, `notification_details.notification_text` | Troncamento/sostituzione del corpo, non solo mascheramento dei metadati |

---

## 5. Riuso della logica di anonimizzazione già prevista per il GDPR

Non va costruito un secondo meccanismo parallelo a quello di [11-modulo-privacy.md §6](11-modulo-privacy.md)
(azione `anonymize` della `RetentionPolicy`, eseguita da ogni modulo sul proprio schema): è la **stessa
trasformazione**, applicata da un **trigger diverso** — non la scadenza di una `RetentionPolicy`, ma
l'esecuzione della pipeline di sanificazione.

Ogni modulo espone quindi **una sola** implementazione di "come anonimizzo la mia fetta di dati",
richiamabile sia dal job di retention sia dal job di sanificazione. Evita di mantenere due logiche di
anonimizzazione che inevitabilmente divergono nel tempo (lo stesso pattern di duplicazione già
segnalato come problema nel legacy — [01-criticita-e-indicazioni-backend.md §5](01-criticita-e-indicazioni-backend.md)).

**Determinismo**: a differenza dell'anonimizzazione per cancellazione GDPR (dove il record non serve
più avere un valore "plausibile"), qui serve **pseudonimizzazione deterministica seedata sull'id
originale** (es. Bogus/Faker con seed = hash dell'id) — stesso cliente produce sempre lo stesso nome
fittizio in ogni esecuzione della pipeline e in ogni tabella, così i test che verificano comportamenti
cross-tabella restano significativi.

---

## 6. Automazione e frequenza

- **Trigger**: EventBridge Scheduler → step di orchestrazione (Step Functions, o un `ECS RunTask`
  sequenziale se non serve la sofisticazione di Step Functions) — coerente con il pattern dei job
  disaccoppiati dal web ([../infra/infrastruttura-aws.md §3.7](../../../../../infra/infrastruttura-aws.md),
  [20-jobs.md](20-jobs.md)).
- **Frequenza**: non a ogni richiesta di uno sviluppatore — un ciclo pianificato (proposta: **mensile**,
  o on-demand ma passando comunque dalla pipeline completa, mai da una scorciatoia manuale) bilancia
  dati "abbastanza aggiornati" contro il costo di eseguire l'intera pipeline (snapshot + istanza
  temporanea + sanificazione + verifica).
- **Dry-run/report**: prima di sostituire il DB di staging condiviso (usato da più persone), un report
  di cosa cambierebbe (righe aggiunte/rimosse rispetto alla versione corrente) via notifica al team —
  stesso principio dei job di retention ([11-modulo-privacy.md §6](11-modulo-privacy.md)).
