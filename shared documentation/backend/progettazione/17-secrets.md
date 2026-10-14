# Secret management

> Come si gestiscono i valori sensibili di configurazione (chiavi API, connection string, chiavi di
> firma) nel nuovo backend **.NET 10**, in locale e su AWS, **senza modifiche al codice** dopo
> l'impostazione iniziale.
>
> Complemento a [01-criticita-e-indicazioni-backend.md §2](01-criticita-e-indicazioni-backend.md)
> (segreti hardcoded / nel repo nel legacy), [02-strategia-di-test.md §4, §15](02-strategia-di-test.md)
> (i test non toccano mai i secret reali; gate anti-secret in CI), [15-logs.md](15-logs.md) (redazione
> di segreti nei log), [pre/linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md)
> (dove vive la configurazione nella solution) e [../infra/infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md)
> (Secrets Manager, `ManageMasterUserPassword` per RDS).
>
> **Documento vivo**: aggiornare l'inventario (§4) quando si integra un nuovo servizio esterno.

---

## 0. Principio guida

**Un solo meccanismo di lettura, sorgenti diverse.** Il codice non deve mai sapere *da dove viene* un
valore: legge sempre da `IConfiguration`/`IOptions<T>`. Cambia solo **chi popola** `IConfiguration`
prima che l'app parta.

| Ambiente | Sorgente del valore | Meccanismo |
|---|---|---|
| Sviluppo locale | `appsettings.Local.json` (file nella cartella del progetto, **in `.gitignore`**) | Caricato in coda alla catena di configurazione standard, dopo `appsettings.{Environment}.json` (§2) |
| CI (test di integrazione) | Variabili d'ambiente del runner, o container Testcontainers effimeri | I test non toccano mai i secret reali (Stripe/Fiskaly/Telnyx sempre stubbati — [02-strategia-di-test.md §4](02-strategia-di-test.md)) |
| AWS (staging/prod) | AWS Secrets Manager / SSM Parameter Store | Iniettati come **variabili d'ambiente del container ECS** al lancio del task (`Secrets:` nella task definition), con lo **stesso nome** di chiave usato in locale |

---

## 1. Convenzione di naming: la chiave di volta

.NET mappa `Sezione__Chiave` (doppio underscore) su `IConfiguration["Sezione:Chiave"]`. Usando lo
**stesso nome** (`Stripe__SecretKey`) sia nel file locale sia nella variabile d'ambiente iniettata da
ECS, il codice applicativo **non cambia mai** tra i due ambienti: cambia solo chi scrive quella
variabile prima dell'avvio.

```jsonc
// appsettings.json — committato, SENZA valori sensibili (solo la forma / i default non sensibili)
{
  "Stripe": { "SecretKey": "" },
  "ConnectionStrings": { "Default": "" },
  "Jwt": {
    "Issuer": "summerbooking-identity",
    // HS256 per ora; elenco di chiavi con kid per poter ruotare (pre/autenticazione-autorizzazione-dotnet.md §4bis)
    "UserKeys": [ { "Kid": "", "Key": "" } ],
    "ApiKeys":  [ { "Kid": "", "Key": "" } ]
  }
}
```

```csharp
// Module.cs del modulo Payment — binding tipizzato, mai IConfiguration sparso nei service
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));

public sealed class StripeOptions
{
    public string SecretKey { get; init; } = string.Empty;
}

public sealed class StripePaymentGateway(IOptions<StripeOptions> options) : IPaymentGateway
{
    private readonly string _secretKey = options.Value.SecretKey;
    // ...
}
```

---

## 2. Locale — file editabile a mano, nessun comando per chiave

- Un file `appsettings.Local.json` nella cartella del progetto, **in `.gitignore`**, editabile come un
  file normale.
- Un `appsettings.Local.json.example` (chiavi vuote) **committato** come guida per ogni sviluppatore.
- Caricamento in coda alla catena di configurazione:
  ```csharp
  // Program.cs — dopo appsettings.{Environment}.json, prima di Build()
  builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
  ```
- La protezione contro il commit accidentale è il `.gitignore` (un `git add -A -f` lo aggirerebbe) —
  per questo la scansione anti-secret (§5) **non è opzionale** con questo approccio: è ciò che rende
  sicura la comodità.

---

## 3. AWS — impostazione una tantum in IaC

Coerente con `infra/summerbooking-aws.yaml`, che già fa questo per la password RDS tramite
`ManageMasterUserPassword`:

```yaml
ContainerDefinitions:
  - Name: api
    Secrets:
      - Name: Stripe__SecretKey
        ValueFrom: !Ref StripeSecretArn
      - Name: ConnectionStrings__Default
        ValueFrom: !GetAtt DbSecret.SecretArn   # già impostato per il DB
```

### 3.1 Serve gestire qualcosa a livello di codice?

- **No, per ruotare o cambiare il valore di un secret esistente** — si tocca solo la sorgente (il file
  locale o Secrets Manager), il codice legge sempre dalla stessa classe `Options`.
- **Sì, ma solo configurazione ordinaria**, quando si integra un servizio esterno per la prima volta:
  una proprietà nuova nella classe `Options` e la riga vuota corrispondente in `appsettings.json` — lo
  stesso lavoro di qualunque impostazione non sensibile, nessun trattamento speciale per il fatto che
  sia un segreto.

### 3.2 Rotazione senza modifiche successive

- **DB**: già gestito nativamente da `ManageMasterUserPassword: true` (RDS + Secrets Manager con
  rotazione automatica) — nessun intervento manuale; l'app deve solo saper riconnettersi su credenziali
  scadute (retry di **connessione**, non retry naïve sulla query).
- **Altri secret** (Stripe, Fiskaly, Twilio, ecc.): iniettati come variabile d'ambiente **al lancio del
  task**. Ruotare = aggiornare il valore in Secrets Manager + forzare un nuovo deployment ECS (rolling,
  zero downtime): task definition e codice non cambiano, cambia solo il valore risolto da `ValueFrom`
  al prossimo avvio del container. Non serve un redeploy applicativo, basta un force-deployment del
  servizio.
- Se in futuro servisse rotazione **senza restart** (secret letto a runtime, non a boot): un
  `ISecretProvider` che chiama l'SDK di Secrets Manager con cache a TTL breve — da introdurre solo se un
  secret specifico lo richiede (oggi non c'è questo requisito).

---

## 4. Inventario dei secret nel software precedente (`back-pre`/`front-pre`/`front-admin`)

Ricavato da `back-pre/.env`, dai file di config con valori hardcoded, da
`front-pre/src/constants/envrionment.js` e — dal 2026-09-09 — dal pannello systemadmin
`front-admin/src/` (React/Metronic, WIP).

| Servizio | Variabile | Dove (file) | Ambiente | Nota |
|---|---|---|---|---|
| Stripe | `STRIPE_SECRETKEY` (`sk_live_...`) | `back-pre/.env:10` | **PROD** | Chiave live dentro un `.env` con `NODE_ENV='development'` |
| Stripe | `STRIPE_WEBHOOK_SECRET` | `back-pre/.env:95` | non determinabile | |
| Stripe | `pk_live_...` / `ca_...` (Connect client id live) | `front-pre/.../envrionment.js:12-13` | **PROD** | Nel bundle pubblico |
| Stripe | `pk_test_...` / `ca_...` (Connect client id test) | `front-pre/.../envrionment.js:14-15` | **DEV** | |
| Database | host/user/password/db (`.env`) | `back-pre/.env:11-14` | **DEV** (nome db `_dev`, ma IP pubblico reale) | |
| Database | host RDS/user/password attivi nel codice | `back-pre/app/config/db.config.js:18-21` | **DEV** ma su RDS reale | Hardcoded, non da env; è il blocco effettivamente eseguito |
| Database | credenziali PROD commentate | `back-pre/app/config/db.config.js:41-53` | **PROD** | Password prod in chiaro nei commenti, da trattare come compromessa |
| JWT | `config.secret = "summer-booking-secret-key"` | `back-pre/app/config/auth.config.js:2` | non determinabile | Usato realmente per firmare/verificare token |
| JWT | `API_JWT_SECRET` | `back-pre/.env:43` | non determinabile | **Secondo secret JWT** usato in parallelo al primo — anomalia da non replicare |
| Reset password | `Cryptr("myTotalySecretKey")` | `back-pre/app/controllers/password.controller.js:5` **+ `front-admin/src/app/modules/Auth/pages/ResetPassword.js:8`** | non determinabile | Cifra l'email nel link di reset; **stessa chiave hardcoded anche nel pannello systemadmin** |
| Cifratura risposta | `API_RESPONSE_SECRET` / `ENC_KEY` (stessa chiave AES) | `back-pre/.env:44` + `front-pre/.../envrionment.js:7` | non determinabile | Chiave AES condivisa FE/BE |
| Cifratura risposta (pannello admin) | `ENC_KEY` AES-256-GCM = `+pWLeWgB0FjY8bYZNUQYgUORbK0vT7do+c/6Lzp6ZMM=` | `front-admin/src/DecryptionService.js:2` | non determinabile | **Chiave distinta** da quella FE/BE sopra; nel bundle pubblico del pannello, decifra tutte le risposte `Api` |
| "Accesso diretto" (impersonation) | `crypto-js` AES key = `"SB_USER_DATA"` | `front-admin/src/app/modules/PropertyManagement/ApproverdProperties.js` | non determinabile | Cifra il payload di `auth/send-token` passato in querystring a `localhost:5001` |
| AWS S3 | `AWS_S3_ACCESS_KEY` / `AWS_S3_SECRET_KEY` | `back-pre/.env:90-91` + duplicate in `globalConstants.js:6-7` e `audioUpload.js:17-18` | non distinguibile (bucket unico) | Triplicata nel codice |
| Twilio | `TWILIO_AUTH_TOKEN` / `TWILIO_ACCOUNT_SID` | `back-pre/.env:21-22` | non determinabile | |
| Twilio | `TWILIO_FROM_NUMBER = whatsapp:+14155238886` | `back-pre/.env:23` | **DEV/sandbox** | Numero sandbox standard Twilio |
| SendGrid | `SENDGRID_API_KEY` | `back-pre/.env:61` | non determinabile | |
| SMTP | `PROD_SMTP_*` | `back-pre/.env:24-27` | **PROD** | |
| SMTP | `DEV_SMTP_*` | `back-pre/.env:28-31` | etichettato DEV ma **identico a PROD** | Nessuna reale separazione |
| SMTP | Gmail (commentato) | `back-pre/.env:32-35` | **DEV** | Ripreso attivo in `front-pre/.../envrionment.js:50-56` |
| Telnyx | `TELNYX_API_KEY` | `back-pre/.env:64` | non determinabile | |
| WhatsApp/Meta | `WHATSAPP_ACCESS_TOKEN` / `WHATSAPP_APP_SECRET` / `WHATSAPP_WEBHOOK_VERIFY_TOKEN` | `back-pre/.env:74-83` | il verify token sembra prod | |
| Fiskaly | `FISKALY_API_KEY`/`SECRET` attivi (`test_...`) | `back-pre/.env:47-49` | **DEV/TEST** | |
| Fiskaly | `FISKALY_API_KEY`/`SECRET` **live** commentati | `back-pre/.env:51-52` | **PROD** | Chiave live in chiaro anche se disattivata |
| Fiskaly | `FISKALY_FISCONLINE_ENCRYPTION_KEY` | `back-pre/.env:57` | non determinabile | |
| Redis | `REDIS_PASSWORD` | `back-pre/app/config/redis.js:8` | **DEV** (default, nessun valore in `.env`) | Redis gira senza password in dev |

### 4.1 Osservazioni operative per la migrazione

1. Ogni riga di questa tabella è un secret da **ruotare** appena possibile, indipendentemente dalla
   riscrittura (sono già compromessi, essendo nel repository) — coerente con
   [01-criticita-e-indicazioni-backend.md §2](01-criticita-e-indicazioni-backend.md).
2. Le credenziali RDS di produzione **in chiaro nei commenti** vanno ruotate anche se il blocco è
   disattivato.
3. Il doppio secret JWT va unificato: nel nuovo backend una chiave attiva per schema (`Jwt:UserKeys`, `Jwt:ApiKeys`, ciascuna con `kid`) — due schemi
   `UserBearer`/`ApiBearer` con chiavi/audience distinti ([pre/autenticazione-autorizzazione-dotnet.md §4](pre/autenticazione-autorizzazione-dotnet.md)),
   quindi **due**, non una casuale coesistenza di due meccanismi come nel legacy.
4. `infra/summerbooking-aws.yaml` oggi copre solo la password del DB in Secrets Manager: tutti gli altri
   secret vanno aggiunti come `AWS::SecretsManager::Secret` referenziati nella task definition.

---

## 5. Test in pipeline: nessun secret nella solution

Su due livelli complementari.

1. **Scansione a livello di repository (gate bloccante in CI)**: `gitleaks` (o `trufflehog`) su ogni PR,
   con una regola custom che include i pattern noti di questo progetto (`sk_live_`, `sk_test_`,
   `whsec_`, `AKIA[0-9A-Z]{16}`, `pk_live_`) oltre alle regole generiche — già previsto come gate in
   [02-strategia-di-test.md §15](02-strategia-di-test.md). Aggiungere anche un **pre-commit hook** locale
   che esegue la stessa scansione: intercetta l'errore prima che arrivi alla PR.
2. **Un test automatico dedicato nella solution** (xUnit, nel gate veloce insieme agli
   `ArchitectureTests`) che scansiona gli `appsettings*.json` nel repository / output di build e
   fallisce su stringhe che matchano i pattern noti o su valori ad alta entropia in chiavi che nello
   schema dovrebbero essere vuote:
   ```csharp
   [Theory]
   [MemberData(nameof(AppSettingsFiles))]
   public void Appsettings_non_contiene_valori_che_sembrano_segreti(string path)
   {
       var content = File.ReadAllText(path);
       var pattern = new Regex(@"sk_(live|test)_|pk_live_|whsec_|AKIA[0-9A-Z]{16}");
       Assert.False(pattern.IsMatch(content), $"Possibile secret in {path}");
   }
   ```
   È cieco rispetto a segreti non ancora noti (non sostituisce gitleaks), ma dà **feedback immediato
   nell'IDE** sull'incidente più probabile in pratica: "ho incollato una chiave reale in
   appsettings.json".
3. Il gate "pacchetti vulnerabili" (`dotnet list package --vulnerable`) resta **separato**: cerca CVE
   nelle dipendenze, non secret — entrambi vanno tenuti, non sono sostitutivi.

---

## 6. Rapporto con altri documenti

- **Redazione nei log**: i valori sensibili non devono comparire nei log nemmeno per errore — policy di
  redazione in [15-logs.md](15-logs.md).
- **Database di test**: le credenziali verso servizi esterni presenti *a DB* (non in configurazione)
  vanno sostituite con valori sandbox nella pipeline di sanificazione — [54-database-di-test.md §5.4](54-database-di-test.md).
- **Cifratura at-rest** dei campi credenziali *dentro le tabelle* (`fiskaly_account`, `switchboard_config`,
  `smtpPassword`): è un tema diverso dal secret di configurazione, trattato in
  [11-modulo-privacy.md §4](11-modulo-privacy.md).
