# Ottimizzazioni DB — secondo giro: coppie `booleano` + `data`, e due difetti emersi

> Secondo giro di ottimizzazioni sullo schema, dopo
> [`05-ottimizzazioni-db.md`](05-ottimizzazioni-db.md). Nato da un'analisi trasversale delle **150
> schede** di [`../analisi-post/db/`](../analisi-post/db/) alla ricerca di un pattern preciso: **una
> colonna booleana e una colonna data che descrivono lo stesso fatto** (es. `is_active` +
> `activated_at`), dove le due possono divergere e una delle due è superflua.
>
> **Tutto quanto è descritto qui è stato applicato il 2026-09-29** — documentazione, codice, migration
> EF Core, database di sviluppo `summer_booking_dev` e utility di migrazione. Il dettaglio per tabella
> vive nelle schede, nel riquadro `✅ Applicato nel backend (2026-09-29)` in testa a ciascuna; questo
> documento tiene il quadro d'insieme, il metodo e le decisioni.

---

## Indice

1. [Sintesi](#sintesi)
2. [Metodo e copertura](#metodo-e-copertura)
3. [Parte A — coppie ridondanti vere](#parte-a--coppie-ridondanti-vere)
4. [Parte B — booleano derivabile da altre colonne](#parte-b--booleano-derivabile-da-altre-colonne-della-stessa-tabella)
5. [Parte C — due difetti emersi durante l'applicazione](#parte-c--due-difetti-emersi-durante-lapplicazione)
6. [Escluso](#escluso)
7. [Migration prodotte](#migration-prodotte)

---

## Sintesi

| # | Tabella | Prima | Dopo |
|---|---|---|---|
| **A.1** | `fiskaly.accounts` | `terms_accepted` + `terms_accepted_at` | solo **`terms_accepted_at`** |
| **A.2** | `notification.email_channel_settings` | `is_verified` + `last_verified_at` + `last_verification_error` | **`last_verification_at`** + `last_verification_error` |
| **B.1** | `identity.account_contract_documents` | `is_current` + `status` + `published_at` | **`superseded_at`** + `status` + `published_at`, **+ `UNIQUE` reale** |
| **B.2** | `catalog.config_snapshots` | `is_stale` + `built_at` | **`stale_since`** + `built_at`, **+ `CHECK`** |
| **C.1** | schema `notification`, 10 tabelle | `notification.notification_*` | **forma canonica** `notification.*` |
| **C.2** | `fiskaly.accounts` | `CHECK` con catena monotòna | **`CHECK` ridotto** alla sola dipendenza reale |

Stato delle verifiche dopo l'applicazione:

- `dotnet ef migrations has-pending-model-changes` negativo su tutti i moduli toccati (nessun drift
  fra modello e migration);
- 66 test di `ArchitectureTests` superati;
- tutte le **129 spec** dell'ETL si caricano e ogni tabella di destinazione esiste a database;
- dati conservati (dettaglio per tabella qui sotto).

**L'ETL completo non è stato rilanciato**: va ricollaudato da zero alla prossima occasione, con la
procedura di [`43-migration-procedure.md`](43-migration-procedure.md).

---

## Metodo e copertura

Estratte tutte le colonne dalle 150 schede, comprese quelle dichiarate in intestazioni combinate
(`### is_verified / last_verified_at / last_verification_error`), che una prima passata aveva perso:

| Tipo | Colonne |
|---|---|
| `boolean` | 113 |
| `timestamptz` | 552 |
| `date` | 41 |

I nomi sono stati incrociati con lemmatizzazione (`active↔activated_at`, `accepted↔accepted_at`,
`verified↔verified_at`, …) escludendo `created_at`/`updated_at`/`deleted_at`, e i candidati sono poi
stati **letti nel merito**: la sola somiglianza di nome produce falsi positivi, per esempio
`payment_request_links.cancel_reservation_on_expiry` + `canceled_at`, che sono due cose diverse — una
policy e un evento.

**Esito**: lo schema era già in gran parte pulito su questo pattern. Le coppie vere erano **2** (Parte
A), più **2** casi di booleano derivabile da altre colonne della stessa tabella (Parte B).

---

## Parte A — coppie ridondanti vere

Il booleano e la data codificavano lo **stesso fatto**: si potevano contraddire a vicenda e nessun
vincolo lo impediva.

### A.1 — `fiskaly.accounts`: eliminato `terms_accepted`

**Scheda**: [`fiskaly_accounts.md`](../analisi-post/db/fiskaly/fiskaly_accounts.md)

`terms_accepted ≡ terms_accepted_at IS NOT NULL`. Il vecchio schema ammetteva due stati incoerenti —
`true` + `NULL` («ha accettato, non si sa quando») e `false` + data valorizzata — senza alcun `CHECK`
che li vietasse. Il booleano era **precondizione bloccante** di `updateAccount` / `getCashRegisters` /
auto-scontrino: se i due campi divergevano, la decisione dipendeva da quale dei due leggeva il codice.

**Applicato**: eliminata `terms_accepted`, resta **`terms_accepted_at timestamptz NULL`**. Il predicato
bloccante è `terms_accepted_at IS NOT NULL`.

**Travaso**: la migration, *prima* della `DROP`, deduce la data da `created_at` dove il flag era `true`
senza data, e azzera la data dove il flag era `false` (il booleano era la fonte letta dal codice,
quindi vince lui). **Verificato su `summer_booking_dev`**: 7 account, tutti con `terms_accepted = true`
**e** `terms_accepted_at` valorizzato già prima della migration — nessuna divergenza reale; 7 date
intatte dopo.

Il `CHECK` della catena di attivazione è stato riscritto, e poi ridotto per un motivo indipendente —
vedi [C.2](#c2--fiskalyaccounts-la-catena-di-attivazione-era-uninvariante-falsa).

| Dove | File toccati |
|---|---|
| Documentazione | `analisi-post/db/fiskaly/fiskaly_accounts.md` (campo, §"Verifica di esclusività", `fiscal_setup_level` derivato, blocco EF Core, riquadro in testa), `analisi-post/db/_feature-tabelle.md` (3 punti, incl. decisione C4-3) |
| Codice | `Module.Fiskaly/Domain/Entities/FiskalyAccount.cs`, `.../Configurations/FiskalyAccountConfiguration.cs` |
| Utility | `mappings/fiskaly/fiskaly_accounts.yaml`, `custom_transforms/fiskaly_accounts.py` — il calcolo di `fiscal_setup_level = fiskaly_active` leggeva `target_row["terms_accepted"]`, ora deriva da `terms_accepted_at is not None` |

### A.2 — `notification.email_channel_settings`: eliminato `is_verified`

**Scheda**: [`notification_email_channel_settings.md`](../analisi-post/db/notification/notification_email_channel_settings.md)

Erano tre colonne per **un solo esito** — quello dell'ultima verifica SMTP. `is_verified` era
derivabile: `last_verified_at IS NOT NULL AND last_verification_error IS NULL`. C'era anche
un'ambiguità di nome irrisolta: `last_verified_at` era la data dell'ultimo **tentativo** o dell'ultimo
**successo**?

**Applicato**: da 3 colonne a 2, con l'ambiguità sciolta nel nome.

| Colonna | Significato |
|---|---|
| `last_verification_at timestamptz NULL` | quando la verifica è stata **tentata** (`NULL` = mai) |
| `last_verification_error text NULL` | `NULL` = riuscita; testo = motivo del fallimento |

**Guadagno di informazione**: l'avviso in gestionale è diventato
`is_active AND (last_verification_at IS NULL OR last_verification_error IS NOT NULL)` e ora distingue
**«mai verificata»** da **«verificata e fallita»**, che con il booleano erano lo stesso stato.

**Nota sulla migration**: EF scaffolda la rinomina come coppia `DropColumn`/`AddColumn`, che
**azzererebbe i valori**. Sostituita a mano con `RenameColumn`. Il travaso gestisce anche i due casi
incoerenti (verificata senza data → `updated_at`; non verificata con data e senza errore → esito
negativo esplicito, senza inventare un messaggio del provider). Su `summer_booking_dev`: 20 righe,
nessuna mai verificata — il legacy `sendTestEmail` non salvava l'esito, quindi nulla da travasare.

| Dove | File toccati |
|---|---|
| Documentazione | `analisi-post/db/notification/notification_email_channel_settings.md` (intestazione combinata, tabella di esclusività, blocco EF Core, note di migrazione, riquadro in testa) |
| Codice | `Module.Notification/Domain/Entities/EmailChannelSettings.cs`, `.../Configurations/EmailChannelSettingsConfiguration.cs` |
| Utility | `mappings/notification/notification_email_channel_settings.yaml` |

---

## Parte B — booleano derivabile da altre colonne della stessa tabella

Non era la coppia `is_x` + `x_at`, ma la sostanza era la stessa: una colonna che ripeteva
un'informazione già presente in riga, mantenuta coerente **solo a codice**.

### B.1 — `identity.account_contract_documents`: `is_current` → `superseded_at`

**Scheda**: [`identity_account_contract_documents.md`](../analisi-post/db/identity/identity_account_contract_documents.md)

Tre colonne per un unico ciclo di vita: `is_current` era la riga con `status = 'published'` e
`published_at` massimo — e, come emerso leggendo il codice, **anche ricavabile da `supersedes_id`**
(chi ha un successore è superato), FK che esisteva già. L'invariante «al più una riga corrente» era
tenuta a mano da due scritture che dovevano restare in accordo; se la seconda non avveniva, il sistema
proponeva due documenti correnti.

**Applicato**: `is_current` sostituita da **`superseded_at timestamptz NULL`** (`NULL` = versione
corrente), che dà in più la **data di supersessione**, prima non ricostruibile.

**L'invariante è diventata un vincolo reale di database**, non più una promessa del codice:

```sql
CREATE UNIQUE INDEX uq_identity_account_contract_documents_single_current
    ON identity.account_contract_documents ((true))
 WHERE superseded_at IS NULL
   AND status = 'published'::identity.contract_document_status
   AND deleted_at IS NULL;
```

È un `UNIQUE` su **espressione costante** (indice a zero colonne): non esprimibile in Fluent API,
quindi SQL a mano nella migration ([`42-migration.md`](42-migration.md) §7). **Verificato**: inserendo
due documenti `published` non superati, il secondo viene rifiutato dal DB.

L'indice `ix_..._current` è passato da `(is_current) WHERE is_current` a
`(status) WHERE superseded_at IS NULL AND deleted_at IS NULL`. `status = 'retired'` resta la
dismissione definitiva, concettualmente distinta dall'essere stato superato.

**Travaso** in due passaggi: (1) chi ha un successore prende la data di pubblicazione del successore —
esatto, via `supersedes_id`; (2) le righe pubblicate non correnti e senza successore dichiarato
prendono `updated_at`, così l'insieme «pubblicate non superate» coincide con quello che aveva
`is_current = true` e l'indice `UNIQUE` regge. Le bozze restano `NULL`: non sono superate, non sono mai
state correnti. Tabella vuota in `summer_booking_dev` (0 righe).

| Dove | File toccati |
|---|---|
| Documentazione | `analisi-post/db/identity/identity_account_contract_documents.md` (campo, tabella Indici, note `created_at`, blocco EF Core, riquadro in testa), `analisi-post/db/_feature-tabelle.md` |
| Codice | `Module.Identity/Domain/Entities/ContractDocument.cs`, `.../Configurations/ContractDocumentConfiguration.cs` |
| Utility | — (tabella nuova, nessuna sorgente legacy, nessun mapping) |

### B.2 — `catalog.config_snapshots`: `is_stale` → `stale_since`

**Scheda**: [`catalog_config_snapshots.md`](../analisi-post/db/catalog/catalog_config_snapshots.md)

`is_stale` era un flag di invalidazione **senza data**: diceva *che* lo snapshot va rigenerato, non *da
quando*. Ma la decisione che la scheda descrive — servire un payload invalidato con un header di
avviso — dipende da **quanto è vecchia** l'invalidazione, non dal fatto che esista.

**Applicato**: 1 colonna → 1 colonna, informazione in più. **`stale_since timestamptz NULL`**
(`NULL` = aggiornato). «È stale da troppo» = `stale_since < now() - interval '…'` è la soglia su cui
alzare l'avviso o rifiutare la lettura; con il booleano non era esprimibile.

Nuovo vincolo `ck_catalog_config_snapshots_stale_after_build`:
`stale_since IS NULL OR stale_since >= built_at`. L'indice `ix_..._stale` è passato da
`(snapshot_kind) WHERE is_stale` a `(stale_since) WHERE stale_since IS NOT NULL`, che dà anche la coda
dei rebuild **in ordine di anzianità**.

Nella scheda il metodo di dominio `MarkStale()` è diventato `MarkStale(DateTime utcNow)` con
`StaleSince ??= utcNow` — **idempotente**: una seconda invalidazione non sposta in avanti la data della
prima, altrimenti «stale da quanto» tornerebbe sempre a zero e la soglia non scatterebbe mai.

Resta vero quanto nota la scheda: con l'aggiornamento **sincrono** la colonna è sempre `NULL`; serve
solo se un `kind` passa ad aggiornamento asincrono.

**Travaso**: `GREATEST(built_at, updated_at)` dove il flag era `true`. Tabella vuota in
`summer_booking_dev` (0 righe).

| Dove | File toccati |
|---|---|
| Documentazione | `analisi-post/db/catalog/catalog_config_snapshots.md` (campo, descrizione, Indici, entità + metodi di dominio, blocco EF Core, riquadro in testa), `analisi-post/db/_config-snapshot-proposta.md` (DDL, indice, §7, escape hatch async, tabella dei rischi), [`03-gestione-dati.md`](03-gestione-dati.md) (3 punti), [`../utility/TODO.md`](../../../../../utility/TODO.md) (voce 3.7) |
| Codice | `Module.Catalog/Domain/Entities/ConfigSnapshot.cs`, `.../Configurations/ConfigSnapshotConfiguration.cs` |
| Utility | nessun mapping da toccare (`mappings/catalog/catalog_config_snapshots.yaml` è `status: pending` con `columns: []` — read-model, non target dell'ETL legacy). Il **rebuild di massa** (voce **1.12** di `utility/TODO.md`, ancora da scrivere) dovrà scrivere `stale_since` |

---

## Parte C — due difetti emersi durante l'applicazione

Nessuno dei due riguardava le coppie booleano/data: sono stati visti lavorando sulle tabelle sopra, e
corretti nello stesso ciclo.

### C.1 — schema `notification`: completata la rinomina alla forma canonica

**Problema.** 10 tabelle si chiamavano `notification.notification_*` invece della **forma canonica**
dichiarata dalle loro stesse schede (`notification.deliveries`, `notification.templates`, …). Negli
altri moduli la forma canonica era rispettata (`fiskaly.accounts`,
`identity.account_contract_documents`, `catalog.config_snapshots`) e nel modulo `notification` esisteva
già una migration `RenameMarketingContactsToCanonicalForm` che aveva sistemato **una sola** tabella:
una rinomina lasciata a metà.

**Applicato.** Tutte e 10 rinominate, migration `RenameNotificationTablesToCanonicalForm`:

| Prima | Dopo |
|---|---|
| `notification.notification_deliveries` | `notification.deliveries` |
| `notification.notification_email_channel_settings` | `notification.email_channel_settings` |
| `notification.notification_establishment_claim_requests` | `notification.establishment_claim_requests` |
| `notification.notification_inapp_message_reads` | `notification.inapp_message_reads` |
| `notification.notification_inapp_messages` | `notification.inapp_messages` |
| `notification.notification_media_assets` | `notification.media_assets` |
| `notification.notification_push_device_tokens` | `notification.push_device_tokens` |
| `notification.notification_template_attachments` | `notification.template_attachments` |
| `notification.notification_templates` | `notification.templates` |
| `notification.notification_user_channel_preferences` | `notification.user_channel_preferences` |

**I nomi di indici e vincoli mantengono il prefisso flat** e non sono stati toccati: è la convenzione
già in uso nel progetto (`pk_notification_marketing_contacts` sulla tabella
`notification.marketing_contacts`, `pk_identity_access_users` su `identity.access_users`). Nessuna
sequenza né vista da sistemare: le PK usano `GENERATED ALWAYS AS IDENTITY` e lo schema non ha viste.

**Nomi di indice fissati** (migration `PinNotificationFkIndexNames`). EF deriva il nome degli indici di
supporto alle FK dal nome della tabella: rinominando le tabelle avrebbe rinominato anche
`ix_notification_deliveries_template_id` e `ix_notification_template_attachments_media_asset_id`,
rompendo la convenzione. Sono stati fissati con `HasDatabaseName` esplicito. **Nello stesso passaggio**
sono stati allineati altri **6 indici** delle tabelle `marketing_*` che già portavano il prefisso
canonico invece di quello flat (`ix_marketing_automations_template_id` →
`ix_notification_marketing_automations_template_id` e simili): erano indici-ombra mai nominati
esplicitamente, la stessa incoerenza preesistente. Dopo l'intervento **ogni** indice dello schema
`notification` porta il prefisso flat, tranne la PK di `__ef_migrations_history` che è gestita da EF.

| Dove | File toccati |
|---|---|
| Documentazione | i blocchi `ToTable(...)` di **11 schede** `analisi-post/db/notification/` (le 10 rinominate + `notification_marketing_contacts.md`, rimasta stale dalla rinomina precedente). Il resto delle schede era già corretto: la **forma canonica era quella dichiarata**, era il database a divergere |
| Codice | 10 `ToTable(...)` nei `*Configuration.cs` di `Module.Notification`, più 8 `HasDatabaseName` espliciti su `DeliveryConfiguration`, `TemplateAttachmentConfiguration`, `MarketingAutomationConfiguration`, `EmailCampaignConfiguration`, `MarketingEventConfiguration` |
| Utility | 9 `target.table` in `mappings/notification/*.yaml` (`inapp_message_reads` non è un target dell'ETL) e **l'SQL qualificato** in `custom_transforms/`: `INSERT`/`DELETE`/`TRUNCATE`/`SELECT` su `notification.notification_templates`, `notification.notification_media_assets`, `notification.notification_template_attachments`, `notification.notification_push_device_tokens`. **Attenzione**: `source.primary_table: notification_details` in `notification_inapp_messages.yaml` è una tabella **MySQL legacy** e non va toccata |

### C.2 — `fiskaly.accounts`: la "catena di attivazione" era un'invariante falsa

**Problema.** Il vincolo `ck_fiskaly_accounts_activation_chain` conteneva anche
`(is_enabled = false OR terms_accepted_at IS NOT NULL)`, cioè «Fiskaly abilitato ⇒ termini accettati».
Documentato nella scheda come **gate monotòno**
`autoreceipt_enabled ⇒ is_enabled ⇒ terms_accepted`.

**È falso.** I due atti hanno **attori e ordine diversi**, confermati dal committente il 2026-09-29:

1. il **SystemAdmin** abilita Fiskaly per lo stabilimento (`is_enabled`, e può impostare anche
   `autoreceipt_enabled`);
2. **poi** il **gestore** accetta termini e condizioni (`terms_accepted_at`).

Quindi `is_enabled = true` con `terms_accepted_at IS NULL` è lo stato **normale e transitorio** fra il
passo 1 e il passo 2 — non un errore. Il vecchio `CHECK` lo vietava, rendendo impossibile a database la
sequenza reale di attivazione.

Il difetto era **preesistente** e nella forma precedente (`is_enabled = false OR terms_accepted = true`,
introdotta con `InitialCreate`): non è stato creato dal collasso del flag di [A.1](#a1--fiskalyaccounts-eliminato-terms_accepted),
solo reso visibile mentre lo si riscriveva.

**Applicato.** Migration `RelaxFiskalyActivationChain`: resta la sola dipendenza strutturale.

```sql
CHECK (autoreceipt_enabled = false OR is_enabled = true)
```

L'auto-scontrino è una sotto-opzione di Fiskaly attivo, e quello sì è un vincolo di forma.

**L'accettazione dei termini resta una precondizione bloccante, ma a runtime** (`updateAccount`,
`getCashRegisters`, emissione dell'auto-scontrino) e nella derivazione di
`fiscal_setup_level = fiskaly_active`. Non è un'invariante di tabella: la tabella deve poter
rappresentare lo stato intermedio, è il codice che rifiuta le operazioni finché i termini mancano.

**Effetto collaterale utile sull'ETL**: il travaso non può più violare il vincolo. L'utility ricava
`is_enabled` da `properties.fiskalyEnabled` senza guardare `accept_terms`; con il vecchio `CHECK` una
property abilitata con termini non accettati avrebbe fatto fallire l'`INSERT`.

| Dove | File toccati |
|---|---|
| Documentazione | `analisi-post/db/fiskaly/fiskaly_accounts.md`: §"Verifica di esclusività" (la riga della catena spezzata in due, perché `terms_accepted_at` non ne fa parte), campi `terms_accepted_at` e `is_enabled`, Descrizione, blocco EF Core, nuovo riquadro in testa |
| Codice | `Module.Fiskaly/Infrastructure/Configurations/FiskalyAccountConfiguration.cs` |
| Utility | — (nessuna modifica necessaria: il vincolo più debole rende superfluo il guard che sarebbe servito) |

---

## Escluso

**Booleani di configurazione.** La grande maggioranza dei 113 booleani descrive **impostazioni**, non
eventi: una data non avrebbe significato. Rientrano qui tutti gli `is_active` / `is_selectable` /
`is_purchasable` di catalogo (che significano «proponibile nelle UI», non «attivato il giorno X»), gli
`allows_*` / `requires_*` di
[`catalog_property_online_booking_settings`](../analisi-post/db/catalog/catalog_property_online_booking_settings.md),
`is_enabled` / `autoreceipt_enabled` di Fiskaly, `verify_certificate`, `also_send_push`,
`send_copy_to_manager`, `is_muted`, `reminders_enabled`, `is_dry_run_only`, `is_default`, `is_split` /
`is_area`, `charges_enabled` / `payouts_enabled` (specchio dei campi Stripe).

**Non ridondante per disegno.**
[`catalog_search_bathhouses.is_published`](../analisi-post/db/catalog/catalog_search_bathhouses.md) vs
[`catalog_bathhouses.published_at`](../analisi-post/db/catalog/catalog_bathhouses.md): sono due tabelle
diverse e la prima è una **proiezione di lettura** per la ricerca pubblica. La denormalizzazione è
voluta, documentata e serve agli indici parziali `WHERE is_published`.

**Già corretto.**
[`payment_gateway_connected_accounts`](../analisi-post/db/payment/payment_gateway_connected_accounts.md)
ha `revoked_at` **senza** un `is_revoked` corrispondente: è esattamente il pattern applicato qui.

---

## Migration prodotte

Applicate a `summer_booking_dev` nell'ordine in cui compaiono.

| Modulo | Migration | Contenuto |
|---|---|---|
| `Module.Fiskaly` | `CollapseRedundantBooleanFlags` | A.1 |
| `Module.Notification` | `CollapseRedundantBooleanFlags` | A.2 |
| `Module.Identity` | `CollapseRedundantBooleanFlags` | B.1 |
| `Module.Catalog` | `CollapseRedundantBooleanFlags` | B.2 |
| `Module.Fiskaly` | `RelaxFiskalyActivationChain` | C.2 |
| `Module.Notification` | `RenameNotificationTablesToCanonicalForm` | C.1, le 10 tabelle |
| `Module.Notification` | `PinNotificationFkIndexNames` | C.1, gli 8 nomi di indice |

### Nota operativa sulle migration di questo giro

Due cose costate tempo, da ricordare per il prossimo giro.

1. **EF scaffolda la `DROP` prima del travaso.** Tutte e quattro le migration della Parte A/B
   eliminavano la colonna di origine **prima** delle `UPDATE` che la leggono. Le `Sql()` di travaso
   vanno inserite a mano nel punto giusto, e in `Down()` il booleano va ricostruito **prima** di
   eliminare la data. Vale anche la rinomina: EF scaffolda `DropColumn` + `AddColumn` dove serve
   `RenameColumn`, e la coppia azzera i valori.
2. **`--no-build` con `dotnet ef` legge un assembly obsoleto.** Una `migrations add --no-build` scrive
   il file `.cs` senza compilarlo: la successiva `migrations remove` o `database update --no-build`
   lavora su un assembly che non contiene la migration appena creata, e fallisce con messaggi
   fuorvianti (`has already been applied`, `has pending changes`). Ricompilare sempre fra un comando
   `ef` e il successivo.
