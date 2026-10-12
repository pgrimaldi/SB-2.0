# Collegare GitHub ad AWS CodePipeline con CodeConnections

Guida operativa per creare una connessione GitHub, una pipeline AWS e il relativo progetto CodeBuild. La procedura e' pensata per applicazioni frontend statiche pubblicate su Amazon S3 e distribuite tramite Amazon CloudFront.

Ultima verifica della procedura: 7 ottobre 2026.

## 1. Obiettivo e architettura

Il flusso descritto e' il seguente:

```text
Push su GitHub
    |
    v
AWS CodeConnections (GitHub App)
    |
    v
AWS CodePipeline - fase Source
    |
    v
AWS CodeBuild - build e deploy
    |                    |
    v                    v
Amazon S3          Invalidazione CloudFront
```

La connessione stabilisce il collegamento autorizzato tra AWS e GitHub. La pipeline orchestra le fasi. CodeBuild esegue i comandi definiti nel `buildspec.yml` e utilizza un proprio ruolo IAM per pubblicare i file.

Nel progetto Summer Booking il deploy e' eseguito direttamente dal `buildspec.yml`; non serve quindi una fase CodeDeploy separata.

## 2. Convenzioni consigliate

Creare risorse separate per ogni ambiente. Non riutilizzare la stessa pipeline e gli stessi ruoli per DEV e PROD.

Esempio DEV, con i nomi reali dell'ambiente DEV attuale (gli stessi di `README_DEPLOY.md`):

| Risorsa | Nome |
| --- | --- |
| Connessione | a scelta, es. `summer-booking-dev-auto-release` |
| Pipeline | `summer-booking-frontend-dev` |
| Progetto CodeBuild | `summer-booking-front-office-dev` |
| Ruolo pipeline | `AWSCodePipelineServiceRole-eu-north-1-summer-booking-frontend-d` (creato da AWS: `AWSCodePipelineServiceRole-<regione>-<pipeline>`, nome troncato) |
| Ruolo CodeBuild | `codebuild-summer-booking-front-office-dev-service-role` (creato da AWS insieme al progetto) |

Per un nuovo ambiente si sostituisce `dev` con il suo nome.

Usare la stessa Regione AWS per connessione, pipeline e progetto CodeBuild. Annotare prima di iniziare:

- Regione AWS;
- proprietario e nome del repository GitHub;
- branch da distribuire;
- bucket S3 di destinazione;
- ID della distribuzione CloudFront;
- percorso del `buildspec.yml` dalla root del repository.

## 3. Prerequisiti e responsabilita'

### 3.1 GitHub

Sono necessari:

- un repository GitHub esistente;
- un branch esistente, ad esempio `dev`;
- il proprietario GitHub disponibile durante l'installazione di **AWS Connector for GitHub**.

Per un repository posseduto da un account personale GitHub, AWS richiede che la connessione sia completata dal proprietario del repository. Un repository personale ha un proprietario e collaboratori: non e' possibile trasformare un collaboratore nel vero amministratore del repository.

Per un repository appartenente a un'organizzazione, coinvolgere un owner dell'organizzazione. Le policy dell'organizzazione possono impedire ai normali membri o ai repository admin di installare GitHub App.

### 3.2 AWS

L'operatore deve poter gestire almeno:

- AWS CodeConnections;
- AWS CodePipeline;
- AWS CodeBuild;
- i ruoli e le policy IAM usati dalla pipeline;
- il bucket S3 e la distribuzione CloudFront dell'ambiente.

Preferire IAM Identity Center e privilegi minimi. Negli account creati con la nuova esperienza AWS, l'accesso umano puo' essere gestito da `https://settings.aws.com/team`: un membro invitato riceve accesso amministrativo all'intero progetto, non soltanto alla pipeline. Valutare questa ampiezza prima di invitarlo.

### 3.3 Repository

Il repository deve contenere un `buildspec.yml`. Per Summer Booking il percorso e':

```text
frontend/summer-booking-front-office/buildspec.yml
```

Il file esegue:

1. installazione deterministica con `npm ci`;
2. verifiche (`npm run verify`: lint, tipi dei test, test unitari): se una fallisce non si pubblica nulla;
3. build della configurazione dell'ambiente (`BUILD_CONFIGURATION`: `dev` o `production`);
4. caricamento su S3 con le intestazioni di cache e rimozione dei file vecchi (i bundle con hash 24 ore dopo essere stati sostituiti, vedi `README_DEPLOY.md`);
5. invalidazione CloudFront.

Credenziali, access key, secret, password e token non devono essere salvati nel repository o nelle variabili in chiaro.

## 4. Creare la connessione GitHub

### 4.1 Aprire la sezione corretta

1. Accedere alla console AWS nel progetto/account corretto.
2. Selezionare la Regione del workload.
3. Aprire **Developer Tools**.
4. Aprire **Settings > Connections**.
5. Selezionare **Create connection**.

Non scegliere i flussi per GitHub Actions runner, nuove build o template di rilascio. Una CodeConnection standard non richiede `Category` o `Template`. Se questi campi compaiono, si e' entrati in una procedura diversa.

### 4.2 Impostazioni della connessione

Compilare:

- **Provider**: `GitHub`;
- **Connection name**: nome conforme alla convenzione aziendale;
- **Tags**: facoltativi ma consigliati per ambiente, applicazione e centro di costo;
- **Host**: nessuno per GitHub.com. Un host serve per GitHub Enterprise Server self-hosted.

Selezionare **Connect to GitHub**.

### 4.3 Installare e autorizzare la GitHub App

1. Verificare di essere autenticati su GitHub con l'account proprietario corretto.
2. Se richiesto, scegliere **Authorize AWS Connector for GitHub**.
3. Nel campo **GitHub Apps/App installation**:
   - scegliere un'installazione esistente, se gia' corretta;
   - altrimenti scegliere **Install new app**.
4. Selezionare l'account personale o l'organizzazione che possiede il repository.
5. Preferire **Only select repositories**.
6. Selezionare esclusivamente i repository richiesti.
7. Esaminare i permessi mostrati da GitHub.
8. Selezionare **Install & Authorize**.
9. Tornati su AWS, verificare che l'installazione sia selezionata e premere **Connect**.

La connessione e' pronta soltanto quando lo stato AWS diventa **Available**.

### 4.4 Controlli immediati

Verificare:

- Regione corretta;
- provider `GitHub`;
- stato `Available`;
- installazione GitHub appartenente al proprietario corretto;
- repository incluso nell'accesso della GitHub App.

Se il repository non compare in **Only select repositories**, quasi sempre si e' autenticati con l'account GitHub sbagliato oppure non si possiedono i permessi necessari sul proprietario del repository.

## 5. Creare il progetto CodeBuild

CodeBuild riceve il sorgente da CodePipeline e utilizza il `buildspec.yml` presente nel repository.

### 5.1 Configurazione generale

Creare un progetto CodeBuild con:

- **Project name**: nome conforme alla convenzione dell'ambiente;
- **Source provider**: `CodePipeline` quando il progetto viene creato dal wizard della pipeline;
- **Environment**: immagine Linux gestita da AWS compatibile con il runtime richiesto;
- **Privileged mode**: disattivato, salvo build Docker esplicitamente necessarie;
- **Service role**: un ruolo dedicato all'ambiente;
- **Buildspec**: `Use a buildspec file`;
- **Buildspec name**: percorso del file dalla root del repository;
- **Artifacts**: `CodePipeline`;
- **Logs**: CloudWatch Logs abilitato.

Non collegare direttamente CodeBuild a GitHub quando e' CodePipeline a fornire il sorgente.

Il runtime Node.js e i comandi devono essere dichiarati nel `buildspec.yml`, evitando configurazioni manuali divergenti nella console.

### 5.2 Variabili d'ambiente

Per Summer Booking configurare almeno:

| Nome | Tipo | Valore |
| --- | --- | --- |
| `BUILD_CONFIGURATION` | Plaintext | `dev` o `production` |
| `S3_BUCKET` | Plaintext | nome del bucket dell'ambiente |
| `CLOUDFRONT_DISTRIBUTION_ID` | Plaintext | ID della distribuzione dell'ambiente |

Questi valori identificano risorse e non sono secret. Eventuali valori sensibili devono provenire da AWS Secrets Manager o Systems Manager Parameter Store.

### 5.3 Permessi minimi del ruolo CodeBuild

Oltre ai permessi standard per log e artefatti, il ruolo CodeBuild deve poter pubblicare esclusivamente nel bucket dell'ambiente e invalidare esclusivamente la distribuzione prevista.

Esempio da adattare:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ListDeploymentBucket",
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": "arn:aws:s3:::<bucket-name>"
    },
    {
      "Sid": "DeployFrontendObjects",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:DeleteObject"
      ],
      "Resource": "arn:aws:s3:::<bucket-name>/*"
    },
    {
      "Sid": "InvalidateDistribution",
      "Effect": "Allow",
      "Action": "cloudfront:CreateInvalidation",
      "Resource": "arn:aws:cloudfront::<account-id>:distribution/<distribution-id>"
    }
  ]
}
```

Non assegnare `AdministratorAccess` al ruolo CodeBuild.

## 6. Creare la pipeline

### 6.1 Proprietà

1. Aprire **CodePipeline > Pipelines**.
2. Selezionare **Create pipeline**.
3. Se richiesto, scegliere **Build custom pipeline**.
4. Impostare il nome della pipeline.
5. Scegliere **Pipeline type V2**.
6. Per un ambiente DEV scegliere normalmente **SUPERSEDED**: se arrivano piu' revisioni ravvicinate, quella piu' recente puo' sostituire quella precedente. Usare **QUEUED** quando ogni revisione deve essere elaborata in ordine.
7. Creare un nuovo service role dedicato oppure selezionarne uno esistente gia' conforme.
8. Lasciare l'artifact store gestito dalla pipeline, salvo uno standard aziendale diverso.

Per DEV e PROD creare pipeline distinte, con connessioni, ruoli, bucket, distribuzioni e variabili coerenti con il rispettivo ambiente.

### 6.2 Fase Source

Configurare:

- **Action name**: `Source`;
- **Source provider**: `GitHub (via GitHub App)`;
- **Connection**: la CodeConnection creata in precedenza;
- **Repository name**: `<owner>/<repository>`;
- **Branch name**: branch dell'ambiente, ad esempio `dev`;
- **Output artifact format**: `CodePipeline default`/`CODE_ZIP`;
- **Output artifact**: ad esempio `SourceArtifact`;
- **Variable namespace**: ad esempio `SourceVariables`.

Il formato `Full clone` e' necessario soltanto se la build deve usare metadati Git completi o eseguire veri comandi Git. In tal caso servono ulteriori permessi `UseConnection` sul ruolo CodeBuild. Per una normale build frontend e' sufficiente il formato ZIP predefinito.

### 6.3 Trigger automatico V2

Configurare un trigger esplicito:

- **Source action**: `Source`;
- **Event**: `Push`;
- **Branches > Include**: branch dell'ambiente, ad esempio `dev`.

Con un trigger V2 filtrato per branch e' normale che nella configurazione sorgente `DetectChanges` risulti `false`: l'avvio automatico e' gestito dal trigger filtrato. Non correggerlo portandolo arbitrariamente a `true`.

Creare una pipeline separata per `main`/PROD anziche' far distribuire DEV e PROD dalla stessa pipeline.

### 6.4 Fase Build

Configurare:

- **Action provider**: `AWS CodeBuild`;
- **Region**: la stessa del progetto;
- **Input artifact**: l'artefatto prodotto da `Source`;
- **Project name**: il progetto CodeBuild dell'ambiente;
- **Build type**: build singola, salvo necessita' di batch build;
- **Output artifact**: configurarlo solo se una fase successiva lo richiede.

Nel flusso Summer Booking CodeBuild esegue sia build sia pubblicazione su S3. Non aggiungere una fase Deploy separata soltanto per duplicare il caricamento su S3 gia' presente nel `buildspec.yml`.

### 6.5 Salvare senza confondere il test

Salvare l'azione, la fase e infine l'intera pipeline. Il comando **Release change/Rilascia modifica** avvia manualmente una pipeline: e' utile per testare build e deploy, ma non prova il funzionamento del trigger GitHub.

## 7. Permessi del ruolo CodePipeline

Il service role della pipeline deve poter:

- usare la CodeConnection;
- leggere e scrivere nell'artifact bucket della pipeline;
- avviare e interrogare il progetto CodeBuild;
- passare eventuali ruoli di azione quando la configurazione lo richiede.

Per la connessione moderna aggiungere una policy limitata al suo ARN:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "codeconnections:UseConnection",
      "Resource": "arn:aws:codeconnections:<region>:<account-id>:connection/<connection-id>"
    }
  ]
}
```

`codestar-connections:UseConnection` serve soltanto per risorse legacy che usano ancora il vecchio prefisso. Non sostituire alla cieca gli ARN esistenti: aggiungere il nuovo ARN, testare e rimuovere quello vecchio solo dopo il successo.

## 8. Test di accettazione

### 8.1 Test manuale

Avviare manualmente la pipeline una prima volta per verificare:

- lettura del repository;
- esecuzione CodeBuild;
- verifiche (`npm run verify`) superate nel log;
- build della configurazione dell'ambiente;
- caricamento su S3;
- invalidazione CloudFront;
- raggiungibilita' del sito.

Il test manuale non convalida il webhook o il trigger GitHub.

### 8.2 Test automatico obbligatorio

1. Creare un commit pertinente sul branch configurato.
2. Eseguire il push verso GitHub.
3. Non selezionare **Release change**.
4. Verificare che entro pochi minuti appaia una nuova esecuzione automatica.
5. Confrontare la source revision con lo SHA del commit inviato.
6. Attendere che tutte le fasi risultino **Succeeded**.
7. Verificare il risultato sull'URL CloudFront dell'ambiente.

Solo questo test conferma l'intera catena GitHub App -> CodeConnections -> trigger -> CodePipeline.

## 9. Sostituire una connessione esistente

Seguire questo ordine per mantenere una possibilita' di rollback:

1. creare la nuova connessione e attendere lo stato `Available`;
2. aprire la pipeline e modificare l'azione `Source`;
3. selezionare la nuova connessione senza cambiare repository, branch e formato artefatto;
4. aggiungere il nuovo ARN alla policy `codeconnections:UseConnection` del service role della pipeline;
5. salvare la pipeline;
6. eseguire un test automatico tramite push;
7. attendere il successo di tutte le fasi e verificare il sito;
8. rimuovere il vecchio ARN dalla policy IAM;
9. eliminare la vecchia connessione da **Developer Tools > Settings > Connections**.

Non disinstallare **AWS Connector for GitHub** se l'installazione e' ancora usata dalla nuova connessione. La cancellazione di una CodeConnection AWS non equivale necessariamente alla rimozione della GitHub App.

## 10. Troubleshooting

### 10.1 La pipeline funziona manualmente ma non parte al push

Controllare nell'ordine:

1. pipeline e connessione sono nella stessa Regione;
2. il trigger V2 e' associato all'azione `Source`;
3. evento `Push` e branch incluso sono corretti;
4. `FullRepositoryId` e' nel formato `<owner>/<repository>`;
5. la connessione e' `Available`;
6. AWS Connector for GitHub e' installato sul proprietario corretto;
7. il repository e' incluso nell'installazione della GitHub App;
8. il branch del commit coincide esattamente con il filtro;
9. `DetectChanges=false` e' coerente con un trigger V2 filtrato e non e' la causa del problema.

Se su GitHub non esiste alcuna installazione dell'app sul proprietario del repository, il fetch manuale di un repository pubblico puo' comunque riuscire, mentre il push non genera l'evento necessario.

### 10.2 `Unable to use Connection ... role does not have sufficient permissions`

La CodeConnection e' stata selezionata, ma il service role della pipeline non possiede `codeconnections:UseConnection` sul nuovo ARN.

Procedura:

1. aprire la pipeline;
2. aprire **Settings** e seguire il link del service role;
3. individuare la policy relativa a CodeConnections;
4. aggiungere il nuovo ARN alla proprieta' `Resource`;
5. salvare;
6. usare **Retry failed stage** sull'esecuzione fallita.

### 10.3 Il repository non compare durante l'installazione

Cause frequenti:

- login effettuato con un account GitHub diverso dal proprietario;
- repository personale posseduto da un altro utente;
- organizzazione non selezionata;
- policy dell'organizzazione che richiede approvazione dell'owner;
- repository non incluso in **Only select repositories**.

Non risolvere selezionando indiscriminatamente **All repositories**. Correggere proprietario, autorizzazione o installazione.

### 10.4 La connessione e' `Available`, ma il source fallisce

Controllare:

- policy `UseConnection` del ruolo CodePipeline;
- ARN esatto della connessione selezionata;
- repository autorizzato nella GitHub App;
- repository e branch configurati nell'azione Source;
- eventuali Service Control Policy o permission boundary.

### 10.5 La build fallisce durante il deploy

Aprire i log CloudWatch del progetto CodeBuild e distinguere:

- errore `npm ci` o build applicativa;
- variabile `BUILD_CONFIGURATION`, `S3_BUCKET` o `CLOUDFRONT_DISTRIBUTION_ID` assente;
- variabile `CLOUDFRONT_DISTRIBUTION_ID` assente;
- `AccessDenied` sul bucket;
- `AccessDenied` su `cloudfront:CreateInvalidation`;
- percorso `buildspec.yml` o cartella `dist` errato.

## 11. Checklist finale

- [ ] Regione verificata.
- [ ] GitHub App installata dal proprietario corretto.
- [ ] Accesso limitato ai repository necessari.
- [ ] Connessione in stato `Available`.
- [ ] Pipeline V2 creata per il solo ambiente previsto.
- [ ] Source configurata con connessione, repository e branch corretti.
- [ ] Trigger `Push` filtrato sul branch corretto.
- [ ] Progetto CodeBuild usa il `buildspec.yml` versionato.
- [ ] Variabili di ambiente configurate.
- [ ] Ruolo CodeBuild limitato a bucket e distribuzione dell'ambiente.
- [ ] Ruolo CodePipeline autorizzato a usare il nuovo ARN.
- [ ] Esecuzione manuale riuscita.
- [ ] Esecuzione automatica da push riuscita.
- [ ] Sito verificato tramite CloudFront.
- [ ] Vecchie connessioni eliminate soltanto dopo il collaudo.
- [ ] Nessuna credenziale presente in repository, buildspec o wiki.

## 12. Informazioni che non sono secret e informazioni da proteggere

Account ID AWS, nomi delle risorse, ARN, ID di connessione e JSON delle policy non sono credenziali, ma vanno comunque trattati come metadati aziendali e non pubblicati senza motivo.

Non inserire mai nella documentazione:

- AWS access key e secret access key;
- session token;
- password e codici MFA;
- cookie di sessione;
- GitHub personal access token o token OAuth;
- chiavi private;
- webhook secret;
- URL firmati contenenti credenziali temporanee.

Se uno di questi valori viene esposto, revocarlo o ruotarlo immediatamente.

## 13. Riferimenti ufficiali

- [AWS - Creare una connessione a GitHub](https://docs.aws.amazon.com/dtconsole/latest/userguide/connections-create-github.html)
- [AWS - Connessioni GitHub in CodePipeline](https://docs.aws.amazon.com/codepipeline/latest/userguide/connections-github.html)
- [AWS - Trigger e filtri Git](https://docs.aws.amazon.com/codepipeline/latest/userguide/pipelines-triggers.html)
- [AWS - Filtri push e pull request](https://docs.aws.amazon.com/codepipeline/latest/userguide/pipelines-filter.html)
- [AWS - Permessi IAM per CodeConnections](https://docs.aws.amazon.com/dtconsole/latest/userguide/security-iam.html)
- [AWS - Gestire il service role di CodePipeline](https://docs.aws.amazon.com/codepipeline/latest/userguide/how-to-custom-role.html)
- [AWS - Creare un progetto CodeBuild](https://docs.aws.amazon.com/codebuild/latest/userguide/create-project.html)
- [AWS - Usare CodeBuild con CodePipeline](https://docs.aws.amazon.com/codebuild/latest/userguide/how-to-create-pipeline.html)
- [AWS - Modalita' di esecuzione delle pipeline](https://docs.aws.amazon.com/codepipeline/latest/userguide/execution-modes.html)
- [GitHub - Installare una GitHub App di terze parti](https://docs.github.com/en/apps/using-github-apps/installing-a-github-app-from-a-third-party)
- [GitHub - Permessi dei repository appartenenti ad account personali](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/repository-access-and-collaboration/permission-levels-for-a-personal-account-repository)
