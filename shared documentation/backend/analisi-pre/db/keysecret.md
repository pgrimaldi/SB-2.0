# Tabella `keysecret`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **2 righe**, query
> completa: `appName` = `OfSale` e `Transactions`, entrambe create il `2026-03-02`, `isEnabled = 1`,
> `expiresAt` `NULL`; **`apiKey` e `secret` in chiaro**; `params` = `{"property_id": 10588}` sulla riga
> `Transactions`, `NULL` su `OfSale`), model
> [`back-pre/app/models/keysecret.model.js`](../../../../../../back-pre/app/models/keysecret.model.js)
> (`{ freezeTableName: true, timestamps: false }`),
> unico accesso via
> [`Service/keySecretService.js`](../../../../../../back-pre/app/Service/keySecretService.js)
> `checkApiKey(apiKey)` → `db.keysecret.findOne({ where: { apiKey, isEnabled: true }, attributes:
> ['apiKey', 'secret', 'appName', 'params'] })`;
> chiamato da
> [`auth.controller.js`](../../../../../../back-pre/app/controllers/auth.controller.js):
> - `signinApi` (`POST /api/auth/signinApi`) — autenticazione HMAC per API partner: il client firma
>   `` `${apiKey}:${timestamp}` `` con `secret` (HMAC-SHA256); a verifica ok emette un JWT
>   `{ apiKey, appName, params }` firmato con `API_JWT_SECRET`, durata 24h;
> - `teamSystemAccessToken` (`GET {HOST}/oauth/access_token` — OAuth2 *client credentials* di
>   TeamSystem/Cassa in Cloud): `client_id = apiKey`, `client_secret = secret`, richiede
>   `appName === "TeamSystem"` (`APP_NAMES.TEAMSYSTEM`, [`utils/constants.js`](../../../../../../back-pre/app/utils/constants.js));
>   emette un `access_token` JWT `{ apiKey, appName, params }` (risposta in ISO-8859-1), durata
>   `TEAMSYSTEM_TOKEN_EXPIRES_IN` (default 7d).
> Il JWT è poi verificato in
> [`middleware/authJwt.js`](../../../../../../back-pre/app/middleware/authJwt.js) (`req.client = decoded`);
> `params.property_id` è letto da
> [`teamsystem.controller.js`](../../../../../../back-pre/app/controllers/teamsystem.controller.js)
> (`getTokenPropertyId` / `resolveBeachId`) e
> [`transactionApi.controller.js`](../../../../../../back-pre/app/controllers/transactionApi.controller.js)
> (scoping delle transazioni per stabilimento). Nessuna associazione ORM, nessun uso dal frontend
> (è un'integrazione server-to-server).
>
> **Nota di contesto** — **catalogo delle credenziali API per le integrazioni esterne/partner**:
> **1 riga per applicazione integrata**. **In produzione ci sono solo 2 righe**: `OfSale` (POS/cassa
> OfSale) e `Transactions` (export transazioni giornaliere, scoping `property_id: 10588`). Il codice
> prevede anche `TeamSystem` (Cassa in Cloud) ma **non esiste alcuna riga `TeamSystem` in produzione** →
> coerente con [`teamsystem_expenses`](teamsystem_expenses.md) a 0 righe: quell'integrazione non è mai
> stata attivata. Non contiene dati utente: è la tabella "chiavi d'accesso" del backend verso i sistemi
> terzi.
>
> **Dominio nella riscrittura**: **INFRA / Integrazioni** (autenticazione delle API partner). I tre
> consumatori gravitano tutti su **C3 (Payment)**: `OfSale` e `TeamSystem` sono POS le cui
> spese/incassi confluiscono nel conto della prenotazione
> ([`teamsystem_expenses`](teamsystem_expenses.md), `ofsale_payments` in
> [`booking_number_informations`](booking_number_informations.md)); `Transactions` è l'export delle
> transazioni giornaliere ([`transactions_payments`](transactions_payments.md)). Elencata tra i
> domini non ancora coperti in [`_feature-tabelle.md`](_feature-tabelle.md).
>
> **Soluzione suggerita per la riscrittura** (i partner sono 2‑3, non centinaia):
> - una tabella leggera `api_clients` **senza colonna segreto**: `id`, `app_name` (`enum`),
>   `client_id` (pubblico, `UNIQUE`), `property_id` (FK tipizzata, non JSON), `scopes`, `is_active`,
>   `created_at`, `rotated_at`, `expires_at` (**reale**, non morta);
> - il **client secret** vive in **AWS Secrets Manager** (o SSM Parameter Store SecureString),
>   path es. `payments/api-clients/{client_id}`; il servizio di auth lo legge alla verifica con una
>   cache in memoria breve (~5 min) e supporta la **rotazione** (due segreti validi nella finestra di
>   rotazione). Così il segreto non è mai in DB, mai in un backup, mai in un log;
> - **alternativa "zero codice di gestione token"**: registrare i partner come *App Client
>   machine‑to‑machine* di un **IdP** (AWS Cognito M2M, Auth0/Keycloak `client_credentials`).
>   L'IdP emette e valida i token; `keysecret` **sparisce**, resta solo la mappatura
>   `client_id → property_id/scopes` (custom claim o `api_clients`).
> In entrambi i casi la verifica HMAC/`client_credentials` resta server‑side, ma il segreto è
> gestito fuori dall'applicazione e la scadenza/rotazione è un fatto reale.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### appName
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: nome dell'applicazione/integrazione a cui la chiave appartiene. Valori usati dal
  codice (`APP_NAMES` in [`utils/constants.js`](../../../../../../back-pre/app/utils/constants.js)):
  `"OfSale"`, `"Transactions"`, `"TeamSystem"`. Portato nel JWT e verificato per instradare le
  autorizzazioni (es. `teamSystemAccessToken` esige `appName === "TeamSystem"`;
  `teamsystem.controller` rifiuta `req.client.appName !== APP_NAMES.TEAMSYSTEM`).
- **Candidato a enumerazione**: **Sì**, `IntegrationApp { OfSale, Transactions, TeamSystem }` —
  vocabolario chiuso già centralizzato in `APP_NAMES` (un valore nuovo richiede comunque codice
  dedicato). In alternativa **tabella di lookup** `integration_apps` se si vuole gestirle da
  pannello.
- **Esempio di valori**: `"OfSale"`, `"Transactions"`

### apiKey
- **Tipo**: `varchar(255)`, `NOT NULL` — **credenziale (client id)**
- **Nullabile**: No
- **Descrizione**: chiave pubblica/identificativo del client (commento nel model: 128 bit,
  caratteri + cifre). È il `client_id` dell'OAuth TeamSystem e la `apiKey` firmata in `signinApi`.
  Chiave di ricerca in `checkApiKey` (nessun indice a schema — vedi criticità).
- **Esempio di valori**: `"IBli5C0L…"`, `"fcPb9F3b…"`

### secret
- **Tipo**: `varchar(255)`, `NOT NULL` — **credenziale (segreto), in CHIARO**
- **Nullabile**: No
- **Descrizione**: segreto condiviso (commento nel model: 256 bit, caratteri + cifre + speciali).
  Usato come chiave HMAC in `signinApi` e come `client_secret` (confronto per uguaglianza) in
  `teamSystemAccessToken`. **Salvato in chiaro.**
- **Esempio di valori**: `"s2,&h*…"`, `"XxKsBI…"`

### expiresAt
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Deprecato**: **Sì** — colonna dichiarata nel model ma **mai letta né scritta** da alcun percorso
  che usa `keysecret` (`checkApiKey` non la seleziona nemmeno; nessun controllo di scadenza sulla
  chiave). Nei dati: sempre `NULL`. La scadenza effettiva è quella del **JWT emesso** (24h /
  `TEAMSYSTEM_TOKEN_EXPIRES_IN`), non della chiave.
- **Descrizione**: (previsto) data di scadenza della chiave API.
- **Esempio di valori**: `null`

### isEnabled
- **Tipo**: `tinyint(1)`, `NOT NULL`, `DEFAULT 1`
- **Nullabile**: No
- **Descrizione**: interruttore attivo/revocato della chiave. `checkApiKey` filtra
  `isEnabled: true`: una chiave con `isEnabled = 0` non autentica più.
- **Candidato a enumerazione**: **booleano** (`is_enabled`), non enum.
- **Esempio di valori**: `1`

### createdAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: creazione riga. `timestamps: false`: valore solo dal default; nei dati la riga
  `OfSale` ha `createdAt = NULL` (inserita a mano prima che il default fosse in vigore).
- **Esempio di valori**: `null`, `2026-03-02 16:11:00`

### updatedAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: ultimo aggiornamento. Non aggiornato automaticamente.
- **Esempio di valori**: `null`, `2026-03-02 16:11:00`

### params
- **Tipo**: `longtext` — **oggetto JSON** (`CHECK (json_valid(params))`); `DEFAULT NULL`; commento a
  schema: *"JSON field for additional parameters (e.g. { property_id: 123 })"*
- **Nullabile**: Sì
- **Descrizione**: parametri di **scoping** della chiave, copiati dentro il JWT e letti a valle.
  L'unica chiave usata dal codice è `property_id` (`getTokenPropertyId` in `teamsystem.controller`;
  `transactionApi.controller` che estrae il `property_id` per filtrare le transazioni). Se `NULL`
  la chiave non è vincolata a uno stabilimento.
  Campi osservati/usati:
  - **property_id** — `number`: id dello stabilimento a cui la chiave dà accesso.
    Es.: `10588`.
- **Esempio di valori**: `null` (riga `OfSale`), `{"property_id": 10588}` (riga `Transactions`)

---

## Osservazioni verificate nel codice (criticità)

- **Segreti in chiaro**: `secret` (e `apiKey`) sono `varchar` in chiaro. Un hash non basta (la
  verifica HMAC lato server richiede il segreto in chiaro), quindi la soluzione è un **secret
  manager** (AWS Secrets Manager / SSM SecureString) o la delega a un **IdP** per il flusso
  *client credentials* — dettaglio nella nota di testa ("Soluzione suggerita per la riscrittura").
- **`expiresAt` colonna morta**: nessun controllo di scadenza sulla chiave (la scadenza vive solo
  sul JWT emesso). O la si implementa o la si rimuove.
- **Nessun indice su `apiKey`**: `checkApiKey` fa `findOne({ where: { apiKey } })` senza indice
  (2 righe oggi, ma va indicizzato/unico nel target).
- **`params` come JSON generico**: di fatto contiene solo `property_id`. Nel target una colonna
  tipizzata `property_id` (FK) + eventuale tabella di scope per casi multi-property.
- **`appName` come `varchar` libero** con vocabolario già chiuso in `APP_NAMES` — vedi Enumerazioni.
- **Nessuna FK / associazione**: la tabella è isolata; il legame allo stabilimento è solo dentro il
  JSON `params`.
- **Timestamp non automatici** (`timestamps: false`); una riga con `createdAt`/`updatedAt` `NULL`.
