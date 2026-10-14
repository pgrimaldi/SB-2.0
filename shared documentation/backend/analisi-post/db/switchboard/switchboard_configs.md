# Tabella `switchboard_configs`

> **Dominio**: C7 — `switchboard` · **sottoarea**: — (radice del dominio) · **schema**: `switchboard`
> · **nome flat**: `switchboard_configs` · **forma canonica**: `switchboard.configs`.
>
> **Redesign** di [`switchboard_config`](../../../analisi-pre/db/switchboard_config.md) (0 righe in
> `local_sb` **e** nel dump di produzione `sb_prod_dump_2026_09_08` — ma **verificato 2026-09-11**: 1
> riga reale, attiva e collaudata, su un terzo ambiente `summer_booking_dev` — vedi la nota
> "Aggiornamento 2026-09-11" in cima alla scheda legacy). Hub del dominio: una riga = il centralino di
> uno stabilimento. I dati regolatori/KYC del numero Telnyx escono in
> [`switchboard_voice_number_registrations`](switchboard_voice_number_registrations.md) (PII ad
> alto rischio, accesso separato — **confermato reale**: la riga osservata ha tutti i campi `reg_*`
> valorizzati con un KYC vero); i segreti SIP escono nel secret manager.
>
> **Fonti**: [`analisi-pre/db/switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md),
> [`docs/03-gestione-dati.md` §2.4quinquies / §3.1bis (sottoaree `voice`/`messaging`)](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §9](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/11-modulo-privacy.md` §4/§5](../../../progettazione/11-modulo-privacy.md),
> [`docs/17-secrets.md`](../../../progettazione/17-secrets.md),
> [`docs/35-integrazioni-esterne.md` §Telnyx](../../../progettazione/35-integrazioni-esterne.md),
> [`docs/80-retention.md` §3.7](../../../progettazione/80-retention.md).

---

## Descrizione

Configurazione del **centralino virtuale** (IVR Telnyx + inoltro SIP + aggancio WhatsApp Business) di
uno stabilimento. **Una riga per `property_id`** (`UNIQUE`). È la chiave di lookup di tutto il flusso
runtime: una chiamata in arrivo sul numero Telnyx viene servita solo se esiste la riga con
`telnyx_phone_e164` corrispondente **e** `is_active = true`.

Correzioni rispetto al legacy:

- **Segreti in chiaro** (`sip_password` `varchar`, usata come chiave di lookup, restituita da API e via
  email) → **`sip_password_secret_ref`** (ARN/nome nel secret manager, come `fiskaly_accounts.*_secret_ref`
  di C4-2; [`docs/17`](../../../progettazione/17-secrets.md), [`docs/11 §4`](../../../progettazione/11-modulo-privacy.md)).
  L'autenticazione degli endpoint softphone non confronta più `sip_username`+`sip_password` in chiaro.
- **KYC / dati regolatori del numero** (`reg_*`: codice fiscale, documento d'identità, data/luogo di
  nascita, id documenti caricati) → tabella dedicata
  [`switchboard_voice_number_registrations`](switchboard_voice_number_registrations.md) con
  accesso ristretto e audit di lettura ([`docs/11 §4`](../../../progettazione/11-modulo-privacy.md)).
- **`device_fcm_token` / `device_voip_token`** (2 colonne, 1 dispositivo per centralino) →
  [`switchboard_voice_softphone_devices`](switchboard_voice_softphone_devices.md) (1 riga per
  dispositivo, con piattaforma e ultimo uso), stesso criterio di C6 `notification_push_device_tokens`.
  *(Il naming camelCase `deviceFcmToken`/`deviceVoipToken` osservato a schema su `local_sb` è
  un'anomalia isolata di quella copia: verificato 2026-09-11 che l'ambiente realmente popolato,
  `summer_booking_dev`, usa correttamente `device_fcm_token`/`device_voip_token` — nessun drift di
  naming da correggere nel target, solo la de-colonnizzazione in tabella figlia.)*
- **`whatsapp_business_account_id`** (scritto, mai riletto) → **non migrato**.
- **`telnyx_webhook_url`** (solo specchio a DB, mai riletto dalla logica) → **non migrato**: l'URL del
  webhook vive in configurazione ([`docs/03 §1`](../../../progettazione/03-gestione-dati.md)).
- **`sip_domain`** (sempre = `env.TELNYX_SIP_DOMAIN`, `"sip.telnyx.com"`) → **non migrato**: valore di
  configurazione, non di riga.
- **`is_active smallint 0/1`** → `boolean`.
- **Modalità IVR implicita** (`telnyx_call_control_app_id IS NOT NULL` ⇒ risponde via API e suona
  l'audio prima di squillare; altrimenti auto-bridge diretto al SIP) → colonna esplicita
  `routing_mode` enum.
- **Timeout cablati** (`SIP_FORWARD_TIMEOUT_SECS = 90`, timeout `gather` DTMF) → colonne di
  configurazione (`forward_ring_timeout_seconds`, `gather_timeout_seconds`).
- **Indici unici ridondanti** su `property_id` (**tre** a schema su `summer_booking_dev` —
  `property_id`, `property_id_2`, `property_id_3` — verificato 2026-09-11 via `SHOW CREATE TABLE`) →
  un solo `UNIQUE` parziale.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `routing_mode` | **enum esclusivo**: `ivr` **o** `direct_sip`, mai entrambi (rami di codice distinti in `handleIncomingCall`). |
| `forward_caller_id_mode` | **enum esclusivo**: `caller` / `centralino` / `custom` (un solo ramo in `executeMenuAction`). `forward_custom_caller_id_e164` ha senso solo con `custom` (`CHECK`). |
| `is_active` | booleano autonomo (interruttore del centralino); nessun altro flag dello stesso argomento → nessun `enum[]`. |

Nessun gruppo di flag booleani correlati (a differenza di `catalog_property_online_booking_settings`):
niente `enum[]` in questa tabella.

## Snapshot

Non applicabile: valori di configurazione vivi, nessun dato congelato copiato da un'entità mutabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_configs` | `id` | PK | — |
| `uq_switchboard_configs_public_id` | `(public_id)` | UNIQUE | id pubblico (API back-office, GDPR) |
| `uq_switchboard_configs_property` | `(property_id) WHERE deleted_at IS NULL` | UNIQUE parziale | una configurazione per stabilimento |
| `uq_switchboard_configs_phone` | `(telnyx_phone_e164) WHERE deleted_at IS NULL AND telnyx_phone_e164 IS NOT NULL` | UNIQUE parziale | lookup della chiamata in ingresso (`payload.to` → config); il numero Telnyx è assegnato a un solo stabilimento |
| `uq_switchboard_configs_sip_username` | `(sip_username) WHERE deleted_at IS NULL AND sip_username IS NOT NULL` | UNIQUE parziale | autenticazione degli endpoint softphone (`sip/call-history`, `sip/register-push`) |

## Campi

### id
- **Legacy**: `switchboard_config.id`
- **Descrizione**: PK tecnica interna allo schema `switchboard`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `config_id` di
  [`switchboard_voice_number_registrations`](switchboard_voice_number_registrations.md),
  [`switchboard_voice_menu_items`](switchboard_voice_menu_items.md),
  [`switchboard_voice_softphone_devices`](switchboard_voice_softphone_devices.md),
  [`switchboard_voice_call_records`](switchboard_voice_call_records.md),
  [`switchboard_voice_call_diagnostics`](switchboard_voice_call_diagnostics.md). FK reali
  **solo** interne allo schema.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, usato dal pannello back-office (`front-admin/`, ruolo
  `systemadmin`) e negli export GDPR.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato applicativamente.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `switchboard_config.property_id` (`int`, FK `switchboard_config_ibfk_3` → `properties.id`)
- **Descrizione**: stabilimento proprietario del centralino. Chiave di accesso di tutti gli endpoint
  del modulo e del controllo di autorizzazione (dal claim del token, mai dal body —
  [`docs/34-multitenant.md` §1](../../../progettazione/34-multitenant.md)).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)), copiato
  per valore (C7-1, come C6-8 / C4-5 / C3-3 / C1-1).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento cross-modulo a C2 per valore; **nessuna** FK cross-schema.

### is_active
- **Legacy**: `switchboard_config.is_active` (`smallint NOT NULL DEFAULT 0`)
- **Descrizione**: interruttore del centralino. `handleIncomingCall` accetta la chiamata solo se la
  config esiste **e** `is_active = true`; altrimenti `rejectCall`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### routing_mode
- **Legacy**: dedotto da `switchboard_config.telnyx_call_control_app_id` (`NULL` ⇒ auto-bridge; valorizzato ⇒ IVR)
- **Descrizione**: come viene servita la chiamata in ingresso.
  - `ivr` → il numero punta alla Call Control App; `handleIncomingCall` risponde via API e avvia
    `gather_using_audio` sull'audio di benvenuto **prima** di squillare sul SIP.
  - `direct_sip` → il numero punta direttamente alla credential connection; la chiamata è
    auto-bridgeata al SIP senza IVR.
- **A cosa serve come colonna esplicita** (non solo `telnyx_call_control_app_id IS NOT NULL`, come nel
  legacy): rappresenta la **decisione di prodotto/intenzione** dell'operatore ("questo stabilimento
  vuole l'IVR"), distinta dallo **stato di provisioning** Telnyx del momento. Motivi concreti:
  1. **Ordine di setup**: la Call Control App si crea *dopo* aver scelto `routing_mode = 'ivr'` (è il
     comando che la crea a leggere la scelta, non viceversa); dedurre il mode dalla presenza dell'id
     renderebbe indistinguibili "ho scelto direct_sip" da "ho scelto ivr ma il provisioning non è
     ancora partito/è fallito".
  2. **Validazione**: l'invariante `routing_mode = 'ivr' ⇒ telnyx_call_control_app_id IS NOT NULL
     (a regime)` diventa un `CHECK`/una regola di dominio verificabile, non un'inferenza a posteriori.
  3. **Pannello e reportistica**: "quanti centralini sono in modalità IVR" è un filtro diretto sulla
     colonna, non un `IS NOT NULL` su un id esterno di un altro sistema.
- **Tipo**: `enum switchboard.call_routing_mode` — valori: `ivr`, `direct_sip`.
- **Default**: `direct_sip`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **verificato 2026-09-11** su `summer_booking_dev` — la config reale ha
  `telnyx_call_control_app_id` valorizzato e i log (`switchboard_call_log.handler_result.mode =
  "call_control_app"`, `action = "answered_via_api"`) confermano che gira realmente in modalità `ivr`.

### telnyx_phone_e164
- **Legacy**: `switchboard_config.telnyx_phone_number` (`varchar(20)`)
- **Descrizione**: numero PSTN geografico assegnato allo stabilimento su Telnyx, in E.164. Chiave di
  lookup della chiamata in ingresso (`payload.to`). Valorizzato al completamento dell'ordine numero
  (vedi [`switchboard_voice_number_registrations`](switchboard_voice_number_registrations.md));
  azzerato al rilascio.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (finché il numero non è provisionato).
- **Relazioni**: `UNIQUE` parziale.

### telnyx_connection_id
- **Legacy**: `switchboard_config.telnyx_connection_id` (`varchar(50)`)
- **Descrizione**: id della *credential connection* Telnyx creata con l'endpoint SIP. Identificatore
  esterno opaco (passthrough).
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### telnyx_call_control_app_id
- **Legacy**: `switchboard_config.telnyx_call_control_app_id` (`varchar(50)`)
- **Descrizione**: id della *Call Control Application* Telnyx. Presente ⟺ `routing_mode = 'ivr'`.
  Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### telnyx_messaging_profile_id
- **Legacy**: `switchboard_config.telnyx_messaging_profile_id` (`varchar(100)`)
- **Descrizione**: id del *messaging profile* Telnyx per gli SMS (es. OTP di verifica del numero
  WhatsApp). Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sip_endpoint_id
- **Legacy**: `switchboard_config.sip_endpoint_id` (`varchar(50)`)
- **Descrizione**: id della *telephony credential* Telnyx dell'utenza softphone dello stabilimento.
  Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sip_username
- **Legacy**: `switchboard_config.sip_username` (`varchar(100)`)
- **Descrizione**: username SIP dell'utenza softphone. Generato server-side (`sb0<propertyId>`). Usato
  per comporre il SIP URI di inoltro (`sip:<sip_username>@<dominio da config>`) e come identità negli
  endpoint softphone.
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: Sì (assente finché l'endpoint SIP non è creato).
- **Relazioni**: `UNIQUE` parziale; copiato in `sip_destination` di
  [`switchboard_voice_call_records`](switchboard_voice_call_records.md) al momento della chiamata.

### sip_password_secret_ref
- **Legacy**: `switchboard_config.sip_password` (`varchar(255)` in chiaro)
- **Descrizione**: **riferimento** (ARN / nome SSM) al segreto che custodisce la password SIP; nessun
  materiale segreto in tabella. La rotazione = nuova versione del segreto. Se serve comunicarla
  all'operatore, canale cifrato / one-time link, mai email in chiaro
  ([`docs/11 §4`](../../../progettazione/11-modulo-privacy.md)).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: risolto da `ISecretResolver` (Infrastructure).
- **In locale**: questo è un segreto **dinamico per riga** (generato dall'app alla creazione
  dell'endpoint SIP, uno per stabilimento — non un valore statico di configurazione come una API key),
  quindi il pattern "chiave fissa in `appsettings.Local.json`" di
  [`docs/17-secrets.md` §2](../../../progettazione/17-secrets.md) non si applica direttamente (quello serve per
  1 segreto noto a deploy-time, non per N segreti creati a runtime). Segue lo **stesso pattern già
  adottato per `fiskaly_accounts.*_secret_ref`**
  ([`fiskaly_accounts.md` → Implementazione .NET](../fiskaly/fiskaly_accounts.md)): in sviluppo
  `ISecretResolver` risolve un nome logico (`local:switchboard/config/{id}`) da **LocalStack** (mock
  Secrets Manager, stesso SDK — flusso realistico) **oppure** da `dotnet user-secrets` /
  `appsettings.Development.json`; in staging/prod lo stesso nome risolve su AWS Secrets Manager reale.
  Il codice applicativo (`ISecretResolver.Resolve(sip_password_secret_ref)`) **non cambia** fra i due
  ambienti — cambia solo l'implementazione iniettata.

### sip_client_type
- **Legacy**: `switchboard_config.sip_client_type` (`varchar(50) DEFAULT 'generic'`)
- **Descrizione**: etichetta descrittiva del client SIP dello stabilimento (softphone / webrtc /
  mobile). **Nessun effetto sulla logica di chiamata**: solo DTO/pannello.
- **Tipo**: `varchar(50)` (stringa libera — [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md)
  la lascia non vincolata salvo esigenza esplicita).
- **Default**: `'generic'`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sip_client_notes
- **Legacy**: `switchboard_config.sip_client_notes` (`text`)
- **Descrizione**: note libere sull'installazione del client SIP. Solo pannello.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### whatsapp_phone_number_id
- **Legacy**: `switchboard_config.whatsapp_phone_number_id` (`varchar(50)`)
- **Descrizione**: id (Meta Cloud API) del numero WhatsApp Business **specifico** dello stabilimento.
  Se valorizzato, ha priorità sul numero generico di piattaforma per l'invio dei template dell'azione
  IVR `send_whatsapp`. Identificatore esterno opaco.
- **Tipo**: `varchar(64)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### whatsapp_phone_e164
- **Legacy**: — (nuovo; il legacy non salvava il numero in chiaro, solo l'id Meta)
- **Descrizione**: numero WhatsApp Business in formato leggibile (E.164), per la visualizzazione nel
  pannello.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 1 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### welcome_audio_asset_id
- **Legacy**: `switchboard_config.welcome_audio_file_id` (`int`, FK → `switchboard_audio_file.id`)
- **Descrizione**: audio riprodotto alla risposta; su di esso si avvia il `gather_using_audio` (DTMF).
  Ri-riprodotto al timeout senza input.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_media_assets`](switchboard_media_assets.md)`.id` (stesso schema).

### fallback_audio_asset_id
- **Legacy**: `switchboard_config.fallback_audio_file_id` (`int`, FK → `switchboard_audio_file.id`)
- **Descrizione**: audio riprodotto al chiamante quando un `forward_sip` fallisce (nessuna risposta /
  occupato), prima di chiudere. Se `NULL`, chiusura senza messaggio.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_media_assets`](switchboard_media_assets.md)`.id`.

### forward_caller_id_mode
- **Legacy**: `switchboard_config.forward_caller_id_mode` (`varchar(20) NOT NULL DEFAULT 'caller'`)
- **Descrizione**: da quale numero il SIP endpoint vede arrivare la chiamata inoltrata:
  `caller` = numero PSTN originale; `centralino` = numero Telnyx del centralino; `custom` =
  `forward_custom_caller_id_e164`.
- **Tipo**: `enum switchboard.forward_caller_id_mode` — valori: `caller`, `centralino`, `custom`.
- **Default**: `caller`.
- **Nullabile**: No.
- **Relazioni**: —.

### forward_custom_caller_id_e164
- **Legacy**: `switchboard_config.forward_custom_caller_id` (`varchar(20)`)
- **Descrizione**: caller ID (E.164) mostrato al SIP endpoint quando `forward_caller_id_mode = 'custom'`.
  Ignorato negli altri mode (`CHECK (forward_caller_id_mode <> 'custom' OR forward_custom_caller_id_e164 IS NOT NULL)`).
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### forward_caller_id_name
- **Legacy**: `switchboard_config.forward_caller_id_name` (`varchar(100)`)
- **Descrizione**: display name opzionale mostrato accanto al caller ID durante il `forward_sip`
  (`from_display_name` del `transfer` Telnyx).
- **Tipo**: `varchar(100)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### forward_ring_timeout_seconds
- **Legacy**: costante `SIP_FORWARD_TIMEOUT_SECS = 90` (cablata)
- **Descrizione**: secondi di squillo del leg SIP prima di considerare fallito l'inoltro e riprodurre
  `fallback_audio_asset_id`.
- **Tipo**: `smallint`.
- **Default**: `90`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (forward_ring_timeout_seconds BETWEEN 5 AND 300)`.

### gather_timeout_seconds
- **Legacy**: parametro `gatherDTMF` cablato in [`telnyx.helper.js`](../../../../../../../back-pre/app/controllers/helpers/telnyx.helper.js)
- **Descrizione**: secondi di attesa dell'input DTMF prima del timeout (ri-riproduzione audio o
  chiusura).
- **Tipo**: `smallint`.
- **Default**: `7`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (gather_timeout_seconds BETWEEN 3 AND 60)`.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_config` — `timestamps: false` (nessun timestamp nel legacy)
- **Descrizione**: creazione / ultima modifica / soft-delete (cessazione del centralino →
  [`docs/80 §5`](../../../progettazione/80-retention.md): 🗑️ config + audio S3 alla cessazione dello
  stabilimento).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Il legacy `switchboard_config` non aveva colonne JSON; tutte le informazioni restano
colonne tipizzate. Gli identificatori esterni Telnyx/Meta (`telnyx_*_id`, `whatsapp_phone_number_id`)
sono stringhe opache passthrough, non JSON da scomporre.

## Suggerimenti per le prestazioni

- Tabella minuscola (≤ 1 riga per stabilimento, ordine delle centinaia a regime). Nessun indice oltre
  a quelli elencati.
- Il lookup di ogni chiamata in ingresso è `SELECT … WHERE telnyx_phone_e164 = ? AND is_active` →
  coperto dall'`UNIQUE` parziale `uq_switchboard_configs_phone`; risultato **cache-abile** in Redis
  con invalidazione sull'`updated_at` (config che cambia raramente, [`docs/16-redis.md`](../../../progettazione/16-redis.md)).
- L'autenticazione degli endpoint softphone (`sip/*`) usa `uq_switchboard_configs_sip_username` +
  verifica del segreto risolto: nessun full-scan come nel legacy (`findOne({ where: { sipUsername, sipPassword } })`).
- La risoluzione di `sip_password_secret_ref` è fuori dal DB (secret manager con cache locale a TTL).

## Implementazione .NET

`SwitchboardConfig` è l'**aggregate root** del dominio C7: una riga per centralino/stabilimento, hub da
cui pendono (per `config_id`, stesso schema `switchboard`) le tabelle figlie del modulo
`Module.Switchboard` — registrazione numero, dispositivi softphone, voci di menù, chiamate, template
WhatsApp. Nessuna riga su `local_sb`/dump di produzione, ma **1 riga reale e attiva** su
`summer_booking_dev` (property 96): il redesign qui sotto non è solo teoria di migrazione, deve anche
poter ospitare quella riga.

### Enum

```csharp
// Module.Switchboard.Domain.Enums/CallRoutingMode.cs
public enum CallRoutingMode
{
	Ivr = 0,
	DirectSip = 1
}

// Module.Switchboard.Domain.Enums/ForwardCallerIdMode.cs
public enum ForwardCallerIdMode
{
	Caller = 0,
	Centralino = 1,
	Custom = 2
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<CallRoutingMode>("switchboard", "call_routing_mode");
modelBuilder.HasPostgresEnum<ForwardCallerIdMode>("switchboard", "forward_caller_id_mode");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<CallRoutingMode>("switchboard.call_routing_mode");
dataSourceBuilder.MapEnum<ForwardCallerIdMode>("switchboard.forward_caller_id_mode");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations;

public sealed class SwitchboardConfigConfiguration : IEntityTypeConfiguration<SwitchboardConfig>
{
	public void Configure(EntityTypeBuilder<SwitchboardConfig> builder)
	{
		builder.ToTable("switchboard_configs", "switchboard", t => t.HasComment(
			"Configurazione del centralino virtuale (IVR Telnyx + inoltro SIP + WhatsApp Business) di uno stabilimento; una riga per property_id."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile usato dal pannello back-office e negli export GDPR.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Chiave di accesso di tutti gli endpoint del modulo.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore del centralino: la chiamata è accettata solo se la config esiste ed è attiva.");

		builder.Property(x => x.RoutingMode)
			.HasColumnName("routing_mode")
			.HasColumnType("switchboard.call_routing_mode")
			.IsRequired()
			.HasDefaultValue(CallRoutingMode.DirectSip)
			.HasComment("Modalità di servizio della chiamata in ingresso: ivr (risponde via Call Control App) o direct_sip (auto-bridge diretto).");

		builder.Property(x => x.TelnyxPhoneE164)
			.HasColumnName("telnyx_phone_e164")
			.HasMaxLength(20)
			.HasComment("Numero PSTN geografico Telnyx dello stabilimento, in E.164; chiave di lookup della chiamata in ingresso.");

		builder.Property(x => x.TelnyxConnectionId)
			.HasColumnName("telnyx_connection_id")
			.HasMaxLength(64)
			.HasComment("Id della credential connection Telnyx dell'endpoint SIP (identificatore esterno opaco).");

		builder.Property(x => x.TelnyxCallControlAppId)
			.HasColumnName("telnyx_call_control_app_id")
			.HasMaxLength(64)
			.HasComment("Id della Call Control Application Telnyx; presente quando routing_mode = ivr.");

		builder.Property(x => x.TelnyxMessagingProfileId)
			.HasColumnName("telnyx_messaging_profile_id")
			.HasMaxLength(64)
			.HasComment("Id del messaging profile Telnyx usato per gli SMS (es. OTP di verifica numero WhatsApp).");

		builder.Property(x => x.SipEndpointId)
			.HasColumnName("sip_endpoint_id")
			.HasMaxLength(64)
			.HasComment("Id della telephony credential Telnyx dell'utenza softphone dello stabilimento.");

		builder.Property(x => x.SipUsername)
			.HasColumnName("sip_username")
			.HasMaxLength(100)
			.HasComment("Username SIP dell'utenza softphone, generato server-side; usato per comporre il SIP URI di inoltro.");

		builder.Property(x => x.SipPasswordSecretRef)
			.HasColumnName("sip_password_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto della password SIP nel secret manager — nessun materiale segreto in tabella.");

		builder.Property(x => x.SipClientType)
			.HasColumnName("sip_client_type")
			.HasMaxLength(50)
			.HasDefaultValue("generic")
			.HasComment("Etichetta descrittiva del client SIP dello stabilimento; nessun effetto sulla logica di chiamata.");

		builder.Property(x => x.SipClientNotes)
			.HasColumnName("sip_client_notes")
			.HasColumnType("text")
			.HasComment("Note libere sull'installazione del client SIP, solo pannello.");

		builder.Property(x => x.WhatsappPhoneNumberId)
			.HasColumnName("whatsapp_phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id Meta Cloud API del numero WhatsApp Business specifico dello stabilimento, se presente ha priorità sul numero generico di piattaforma.");

		builder.Property(x => x.WhatsappPhoneE164)
			.HasColumnName("whatsapp_phone_e164")
			.HasMaxLength(20)
			.HasComment("Numero WhatsApp Business in formato E.164, per la visualizzazione nel pannello.");

		builder.Property(x => x.WelcomeAudioAssetId)
			.HasColumnName("welcome_audio_asset_id")
			.HasComment("Audio riprodotto alla risposta, base del gather DTMF di benvenuto.");

		builder.Property(x => x.FallbackAudioAssetId)
			.HasColumnName("fallback_audio_asset_id")
			.HasComment("Audio riprodotto quando un forward_sip fallisce, prima di chiudere la chiamata.");

		builder.Property(x => x.ForwardCallerIdMode)
			.HasColumnName("forward_caller_id_mode")
			.HasColumnType("switchboard.forward_caller_id_mode")
			.IsRequired()
			.HasDefaultValue(ForwardCallerIdMode.Caller)
			.HasComment("Da quale numero il SIP endpoint vede arrivare la chiamata inoltrata: numero del chiamante, del centralino, o custom.");

		builder.Property(x => x.ForwardCustomCallerIdE164)
			.HasColumnName("forward_custom_caller_id_e164")
			.HasMaxLength(20)
			.HasComment("Caller ID mostrato al SIP endpoint quando forward_caller_id_mode = custom.");

		builder.Property(x => x.ForwardCallerIdName)
			.HasColumnName("forward_caller_id_name")
			.HasMaxLength(100)
			.HasComment("Display name opzionale mostrato accanto al caller ID durante l'inoltro SIP.");

		builder.Property(x => x.ForwardRingTimeoutSeconds)
			.HasColumnName("forward_ring_timeout_seconds")
			.IsRequired()
			.HasDefaultValue((short)90)
			.HasComment("Secondi di squillo del leg SIP prima di considerare fallito l'inoltro.");

		builder.Property(x => x.GatherTimeoutSeconds)
			.HasColumnName("gather_timeout_seconds")
			.IsRequired()
			.HasDefaultValue((short)7)
			.HasComment("Secondi di attesa dell'input DTMF prima del timeout.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_configs_forward_custom_caller_id",
				"forward_caller_id_mode <> 'custom' OR forward_custom_caller_id_e164 IS NOT NULL");
			t.HasCheckConstraint("ck_switchboard_configs_forward_ring_timeout",
				"forward_ring_timeout_seconds BETWEEN 5 AND 300");
			t.HasCheckConstraint("ck_switchboard_configs_gather_timeout",
				"gather_timeout_seconds BETWEEN 3 AND 60");
		});

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_configs_public_id");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_configs_property");

		builder.HasIndex(x => x.TelnyxPhoneE164)
			.IsUnique()
			.HasFilter("deleted_at IS NULL AND telnyx_phone_e164 IS NOT NULL")
			.HasDatabaseName("uq_switchboard_configs_phone");

		builder.HasIndex(x => x.SipUsername)
			.IsUnique()
			.HasFilter("deleted_at IS NULL AND sip_username IS NOT NULL")
			.HasDatabaseName("uq_switchboard_configs_sip_username");

		builder.HasOne(x => x.WelcomeAudioAsset)
			.WithMany()
			.HasForeignKey(x => x.WelcomeAudioAssetId)
			.HasConstraintName("fk_switchboard_configs_welcome_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.FallbackAudioAsset)
			.WithMany()
			.HasForeignKey(x => x.FallbackAudioAssetId)
			.HasConstraintName("fk_switchboard_configs_fallback_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

`SwitchboardConfig` implementa `ISoftDeletable`/`IAuditable` (config ha `created_at`/`updated_at`/
`deleted_at` con semantica piena — cessazione dello stabilimento → soft-delete di config + audio S3,
[`docs/80 §5`](../../../progettazione/80-retention.md)).

### Segreti e provider esterni

- `ISecretResolver.Resolve(sip_password_secret_ref)` (Infrastructure): stesso pattern di
  `fiskaly_accounts.*_secret_ref` — segreto **dinamico per riga**, non una chiave fissa di deploy-time;
  in locale risolve da LocalStack Secrets Manager o `user-secrets`/`appsettings.Development.json` sul
  nome logico `local:switchboard/config/{id}`, in staging/prod da AWS Secrets Manager reale, stesso
  codice applicativo nei due casi.
- `ITelephonyProvider` (Infrastructure, wrapper Telnyx) isola il runtime SIP/real-time
  ([`docs/35 §Telnyx`](../../../progettazione/35-integrazioni-esterne.md)).
- Policy di autorizzazione di modulo: `property_id` **sempre** dal claim del token, mai dal body;
  `manager` → solo la propria property, `systemadmin` → qualsiasi.

### Note di migrazione

- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie. **Esiste però 1 riga
  reale** su `summer_booking_dev` (property 96, test interno), da migrare se quell'ambiente va portato
  al nuovo schema: `sip_password` in chiaro → crea segreto e salva `*_secret_ref`; `reg_*` → riga in
  [`switchboard_voice_number_registrations`](switchboard_voice_number_registrations.md);
  `device_fcm_token`/`device_voip_token` → riga in
  [`switchboard_voice_softphone_devices`](switchboard_voice_softphone_devices.md); ricalcolare
  `sip_username` con la convenzione corrente (il valore reale osservato usa un formato più vecchio —
  vedi [`switchboard_config.md`](../../../analisi-pre/db/switchboard_config.md)).
- I tre indici `UNIQUE` ridondanti su `property_id` osservati su `summer_booking_dev`
  (`property_id`/`property_id_2`/`property_id_3`) collassano nel solo `uq_switchboard_configs_property`.
