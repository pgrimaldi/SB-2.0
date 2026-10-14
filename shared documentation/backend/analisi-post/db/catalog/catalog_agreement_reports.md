# Tabella `catalog_agreement_reports`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_reports` · **forma canonica**: `catalog.agreement_reports`.
>
> Rimpiazza la legacy [`hotel_reports`](../../../analisi-pre/db/hotel_reports.md) (**2 righe** in
> produzione, entrambe senza PDF). Figlia di
> [`catalog_agreement_partners`](catalog_agreement_partners.md).
>
> **Perché nasce il 2026-10-02**: la tabella era stata lasciata fuori dal nuovo schema, classificata
> come «artefatto della gestione convenzioni». Il titolare ha deciso che la funzione **si rifà e
> archivia il PDF**, e la policy di retention F5 non ha niente su cui agire senza di lei. Analisi e
> prove: [`docs/83-retention-file.md` §7-G1](../../../progettazione/83-retention-file.md); regole:
> [`docs/80-retention.md` §3.11](../../../progettazione/80-retention.md).
>
> **Applicata** il 2026-10-03 con `20261003080145_AddAgreementReports`. **Nasce vuota**: nessun dato
> da migrare.

---

## Descrizione

Report periodico (arrivi, partenze, ricavi) che uno stabilimento invia a una struttura partner
convenzionata. Il gestore scegle un intervallo di date e se filtrarlo per arrivo o per partenza,
il sistema calcola i totali e, all'invio, archivia il PDF generato.

`report_data` è una **cache**, non la fonte: il dato grezzo resta in `booking.*` e il documento è
ricostruibile da lì. Conservarla serve a ristampare *esattamente ciò che è stato inviato*, non a
sapere cosa è successo.

### Perché non c'è una colonna `path`, e perché il riferimento al PDF si chiama così

Nel legacy `hotel_reports.path` era una colonna sola con l'URL S3 del PDF, e aveva **due difetti che
questa tabella non riproduce** — entrambi misurati, non ipotizzati:

1. **veniva sovrascritta a ogni rigenerazione, senza cancellare il file precedente.** È così che 4
   report hanno prodotto **28 oggetti** sul bucket, 24 dei quali versioni superate che nessuna riga
   nominava più. Qui il riferimento è `pdf_media_asset_id` e **sostituire il PDF richiede di accodare
   il vecchio oggetto in `privacy.privacy_file_deletions`**, altrimenti il difetto si ripete
   identico;
2. **archiviava anche la semplice anteprima.** L'endpoint «scarica il PDF» chiamava la stessa
   funzione dell'invio, quindi caricava su S3 ogni volta che qualcuno guardava il documento: un solo
   report ha lasciato 11 oggetti che nessuno ha mai mandato a nessuno. Si archivia **all'invio**.

> ⚠️ **`pdf_media_asset_id` è un riferimento LOGICO, non una FK.** Il PDF vive in
> `notification.media_assets` (`kind = 'rendered_pdf'`), cioè nello schema di un altro modulo, e nel
> progetto nessun modulo vede le tabelle di un altro: tutti i `media_asset_id` esistenti sono
> **intra-modulo**, e `notification.media_assets` non ha nemmeno un `public_id` da copiare
> cross-modulo. Il nome è diverso da `media_asset_id` **di proposito**: chiamarla come le altre
> suggerirebbe un'integrità referenziale che a database non c'è.
>
> **Perché il PDF non sta in `catalog.media_assets`**, che darebbe una FK vera: `catalog.media_kind`
> ha solo `photo | pdf | icon`, quindi la policy F5 avrebbe dovuto filtrare su `kind = 'pdf'` — cioè
> su **tutti** i PDF del modulo, listini e menù compresi — e li avrebbe cancellati a 24 mesi.
> `notification.media_kind` ha invece `rendered_pdf`, che individua esattamente questi documenti e
> nient'altro. Si è preferito perdere una FK che correre quel rischio.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_reports` | `id` | PK | — |
| `uq_catalog_agreement_reports_public_id` | `(public_id)` | UNIQUE | id pubblico del report, usato nei link di invio |
| `ix_catalog_agreement_reports_partner` | `(partner_id) WHERE deleted_at IS NULL` | parziale | «i report di questa convenzione»: l'elenco che il gestore apre |
| `ix_catalog_agreement_reports_period` | `(period_start, period_end) WHERE deleted_at IS NULL` | parziale | «i report di questo periodo» |
| `ix_catalog_agreement_reports_pdf_media_asset` | `(pdf_media_asset_id) WHERE pdf_media_asset_id IS NOT NULL` | parziale | risalita **inversa**: «quale report nominava questo asset», per azzerare il riferimento quando la policy F5 rimuove l'oggetto a 24 mesi. Senza questo indice la retention del PDF lascerebbe un riferimento che punta al nulla |

## Vincoli

| Vincolo | Regola | Perché |
|---|---|---|
| `ck_catalog_agreement_reports_period` | `period_end >= period_start` | un intervallo non può finire prima di cominciare. Un report di un solo giorno è legittimo, quindi `>=` e non `>` |
| `ck_catalog_agreement_reports_totals` | `total_reservations >= 0 AND total_revenue >= 0` | sono un conteggio e un importo |
| `fk_catalog_agreement_reports_partner` | FK verso `agreement_partners(id)`, `ON DELETE CASCADE` | il report esiste solo come figlio del partner, come i listini |

## Campi

| Legacy `hotel_reports` | Nuovo | Tipo | Nota |
|---|---|---|---|
| `id` | `id` + `public_id` | `integer` identity + `uuid` | `integer` come il resto della sottoarea `agreement` |
| `hotelId` | `partner_id` | `integer` | FK reale verso `agreement_partners` |
| `searchType` `ENUM('CHECK_IN','CHECK_OUT')` | `search_type` | `catalog.agreement_report_search_type` | etichette minuscole: `check_in \| check_out`. Default `check_in`, come il legacy |
| `bookingStartDate` / `bookingEndDate` | `period_start` / `period_end` | `date` | sono **giorni**, non istanti: nel legacy erano `DATE` su una colonna `DATETIME` |
| `status` `ENUM('pending','created','cancelled')` | `status` | `catalog.agreement_report_status` | default `pending` |
| `reportData` JSON | `report_data` | `jsonb` | ⚠️ **cache**, non la fonte |
| `totalBookings` | `total_reservations` | `integer`, default 0 | |
| `totalRevenue` | `total_revenue` | `numeric(12,2)`, default 0 | da `DECIMAL(12,2)` |
| `generatedBy` | `generated_by_user_id` | `uuid` | riferimento logico a `identity.access_users.public_id`, non FK (altro schema) |
| `notes` | `notes` | `text` | |
| `path` | ❌ **non riportata** → `pdf_media_asset_id` | `bigint` | vedi il riquadro sopra |
| `isDeleted` | `deleted_at` | `timestamptz` | convenzione dello schema nuovo |

### Una nota su `status` e `deleted_at`

Nel legacy `status = 'cancelled'` e `isDeleted = 1` erano scritti **sempre insieme** su tutte le
righe annullate: due modi di dire la stessa cosa. Qui restano separati con significati distinti —
`cancelled` è lo stato del report (annullato da chi lo gestisce, resta visibile nello storico),
`deleted_at` è il soft-delete della riga. Chi porta avanti la feature non deve riallinearli.

## Dati da migrare

**Nessuno.** Verificato sui tre database il 2026-10-02:

| Database | Righe | `id` presenti | `AUTO_INCREMENT` | `path` valorizzati |
|---|---:|---|---:|---:|
| Produzione (`u811491364_summer_booking`) | **2** | 1, 2 | **3** | **0** |
| Sviluppo (`summer_booking_dev` su RDS) | 8 | 1…8 | 9 | 4 |

In produzione nessun PDF è mai stato prodotto, e l'invio era anche tecnicamente impossibile:
`hotel_message_templates` ha **0 righe** e il servizio pretende un template per comporre il
messaggio. I 28 PDF su `reports/hotel/` appartengono all'ambiente di **sviluppo**, che condivide il
bucket con la produzione: sono classificati come orfani attesi in
`utility/02-migrazione-file/app/groups.py` e **non si migrano**.

## Valutazione JSON

`report_data jsonb` è l'unica colonna JSON e rientra nell'eccezione già ammessa per i dati
**derivati** (come `catalog_config_snapshots.payload`): è uno snapshot chiuso di ciò che è stato
inviato, ricostruibile dalla fonte, e non viene interrogato per campo.

## Suggerimenti per le prestazioni

- il calcolo dei totali è una query su `booking.*` e non va rifatto a ogni apertura dell'elenco: è il
  motivo per cui `total_reservations`/`total_revenue` sono colonne e non viste;
- `ix_..._pdf_media_asset` serve al job di retention, non all'interfaccia: senza di lui la risalita
  inversa diventa una scansione della tabella a ogni oggetto cancellato.

## Implementazione .NET

- entità: `Module.Catalog/Domain/Entities/AgreementReport.cs` (`IAuditable`, `ISoftDeletable`);
- enum nuovi: `Domain/Enums/AgreementReportSearchType.cs`, `Domain/Enums/AgreementReportStatus.cs`,
  registrati in `DbContext.OnModelCreating` (`HasPostgresEnum`) **e** in
  `ModulePersistenceRegistration` + `DesignTimeDbContextFactory` (`MapEnum`, riga identica);
- configurazione: `Module.Catalog/Infrastructure/Configurations/AgreementReportConfiguration.cs`,
  con `HasColumnType` su entrambe le colonne enum e `HasDefaultValueSql` (mai `HasDefaultValue`);
- collezione `Reports` su `AgreementPartner`, come `PriceLists`;
- migration: `20261003080145_AddAgreementReports`.
