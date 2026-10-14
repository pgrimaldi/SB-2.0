# Tabella `notification_inapp_messages`

> **Dominio**: C6 — `notification` · **sottoarea**: `inapp` · **schema**: `notification` · **nome flat**:
> `notification_inapp_messages` · **forma canonica**: `notification.inapp_messages`.
>
> Sostituisce [`notification_details`](../../../analisi-pre/db/notification_details.md) (**317.300
> righe**, una delle tabelle più grandi del DB) — il **centro notifiche in-app per stabilimento**
> ("campanella" del gestionale). Fonti:
> [`analisi-pre/db/notification_details.md`](../../../analisi-pre/db/notification_details.md) (§"Ottimizzazione"),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un evento operativo di uno stabilimento** mostrato agli operatori nel feed della
campanella: *prenotazione creata*, *pagamento ricevuto via link*, *errore nella generazione dello
scontrino elettronico*, *creazione automatica ricevuta non riuscita*, *spostamento di prenotazione
già scontrinata bloccato*.

**Non si memorizza il testo, si memorizza l'evento** ([`notification_details.md` §"Ottimizzazione"](../../../analisi-pre/db/notification_details.md)):
`kind` (il tipo) + `payload` (le sole variabili che servono al template, come **snapshot**). Il testo
è **renderizzato alla lettura** da template localizzati del modulo (`.resx`/JSON per lingua), con
catena di fallback `lingua richiesta → default → chiave`. Lo stato di lettura è **per utente**, nella
figlia [`notification_inapp_message_reads`](notification_inapp_message_reads.md).

Correzioni rispetto al legacy:

- **`notification_text` + `notification_text_it`** (due `varchar(255)` col messaggio già concatenato
  in 6+ punti, IT quasi sempre copia dell'altro, `NULL` su 11 329 righe senza fallback, `amount`/
  `undefined` letterali, troncamento a 255) → `kind` + `payload` + rendering a template.
- **`type` `int` `1`/`2`** (soggetto) → `subject_kind` enum, **stesso vocabolario** di
  `booking_payments.type` / `service_reservation_payments.type` (§2.4bis).
- **`status` `int` `1`/`2`** (flag di *visibilità* mascherato, naming in collisione con
  `bookings.status`) → `visibility` enum `visible` / `pending_confirmation`.
- **`read` per property** (il primo operatore azzera per tutti) → stato-lettura **per utente**
  (figlia).
- **`user_id` che contiene un `customers.id` mai riletto** → **non migrato**; il destinatario nel
  target è l'operatore (per property).
- **`reference_id` che è un `bookings.id` / `service_reservation.id` con spazi id sovrapposti** →
  `subject_public_id` (uuid) tipizzato da `subject_kind` — nessuna ambiguità.
- **`bookingNumber varchar DEFAULT '0'`** → `booking_number` nullable, senza default fittizio.
- **`getNotification` senza `LIMIT`** (carica ~13 000 righe/chiamata, paginazione `slice` in JS) →
  query paginata su indice `(property_id, created_at DESC)`.
- **crescita illimitata, nessuna retention** → `deleted_at` + purge a **6 mesi** ✅ (valore validato dal
  titolare — estremo inferiore del range 6–12 proposto,
  [`docs/82-retention-data.md` §3.6](../../../progettazione/82-retention-data.md);
  [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md), job `PurgeInAppNotifications`).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `subject_kind` | **enum esclusivo**: `booking` / `service_reservation` (ex `type` `1`/`2`). |
| `visibility` | **enum esclusivo**: `visible` / `pending_confirmation` (ex `status` `1`/`2`; `0` — 40 righe — normalizzato a `visible`). |
| `kind` | **enum esclusivo**: un evento per riga. |
| `read` | **non** un campo qui: è **per utente** → figlia `notification_inapp_message_reads`. |

## Snapshot

`payload` (`jsonb`) è uno **snapshot delle variabili di rendering** (nome cliente, canale, date,
postazioni, operatore, importo, sintesi errore) congelate alla creazione: la notifica resta
auto-consistente anche se la prenotazione referenziata cambia o è cancellata, senza rileggere C1.
`legacy_text` (sola lettura) conserva la stringa originale non ri-parsabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_inapp_messages` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | navigazione UI, deep-link |
| `ix_..._feed` | `(property_id, created_at DESC) WHERE visibility = 'visible' AND deleted_at IS NULL` | btree parziale | il feed della campanella (unica query di lettura) |
| `ix_..._subject` | `(subject_kind, subject_public_id)` | btree | rendere `visible` le notifiche di una prenotazione alla conferma pagamento (ex `update … where reference_id`, ora tipizzato) |

## Campi

### id
- **Legacy**: `notification_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `notification_inapp_message_reads.message_id`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile (deep-link dalla campanella).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `notification_details.property_id`
- **Descrizione**: stabilimento destinatario. È l'**unico** criterio di targeting (la notifica è vista
  da tutti gli operatori della property — comportamento legacy mantenuto,
  [regola generale](../_domande-committente.md)).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### kind
- **Legacy**: dedotto dallo scrittore (6+ punti che concatenano una stringa diversa)
- **Descrizione**: tipo di evento (determina template di rendering e navigazione UI al click).
- **Tipo**: `enum notification.inapp_kind` — valori: `booking_created`, `service_reservation_created`,
  `payment_received`, `ereceipt_data_error`, `auto_receipt_failed`, `receipted_booking_move_blocked`,
  `generic`.
- **Default**: `generic`.
- **Nullabile**: No.
- **Relazioni**: —.

### subject_kind
- **Legacy**: `notification_details.type` (`1` / `2`)
- **Descrizione**: soggetto della notifica (rotta di navigazione: `1` → `/beachmap`, `2` →
  `/edit-services-reservation/<id>`).
- **Tipo**: `enum notification.inapp_subject_kind` — valori: `booking`, `service_reservation`.
  **Stesso vocabolario** concettuale di `booking_payments.type` / `service_reservation_payments.type`.
- **Default**: `booking`.
- **Nullabile**: No.
- **Relazioni**: tipizza `subject_public_id`.

### subject_public_id
- **Legacy**: `notification_details.reference_id` (`bookings.id` / `service_reservation.id`, spazi id sovrapposti)
- **Descrizione**: prenotazione/servizio a cui l'evento si riferisce.
- **Tipo**: `uuid` (= `public_id` di `booking_reservations`).
- **Default**: `NULL`.
- **Nullabile**: Sì (`119` righe legacy — notifiche di errore Fiskaly — non lo passano).
- **Relazioni**: **`public_id` (uuid) copiato** da C1 [`booking_reservations`](../booking/booking_reservations.md)
  (testata unica postazioni+servizi).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 317.503 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### booking_number
- **Legacy**: `notification_details.bookingNumber` (`varchar DEFAULT '0'`)
- **Descrizione**: numero prenotazione, per il redirect al planner.
- **Tipo**: `varchar(20)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: correlazione per valore con C1; nessun default fittizio `'0'`.

### visibility
- **Legacy**: `notification_details.status` (`1` mostrata / `2` nascosta fino a conferma pagamento)
- **Descrizione**: flag di **visibilità** nel feed.
- **Tipo**: `enum notification.inapp_visibility` — valori: `visible`, `pending_confirmation`.
- **Default**: `visible`.
- **Nullabile**: No.
- **Relazioni**: portata a `visible` dai flussi di conferma pagamento C3 (evento
  `PaymentReceived` consumato dal modulo, non `UPDATE … WHERE reference_id` non filtrato per tipo).

### payload
- **Legacy**: le variabili erano concatenate dentro `notification_text` (nessun campo strutturato)
- **Descrizione**: **snapshot** delle sole variabili di rendering del template per questo `kind`:
  `{ customer_name, channel, date_from, date_to, locations, operator, amount, currency, error_summary,
  stripe_transaction_id }` — sottoinsieme secondo `kind`. Niente HTML, niente testo pre-renderizzato.
- **Tipo**: `jsonb`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: la forma per `kind` è un contratto di codice (record C#), validato alla scrittura.

### legacy_text
- **Legacy**: `notification_details.notification_text` (per lo storico non ri-parsabile)
- **Descrizione**: stringa originale conservata **as-is** e mostrata in sola lettura quando il parsing
  best-effort verso `kind`+`payload` non ha riconosciuto il pattern (righe 2022, errori Fiskaly lunghi).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: solo per le righe migrate; le nuove notifiche non lo valorizzano.

### created_by_user_id
- **Legacy**: — (`user_id` conteneva un `customers.id`)
- **Descrizione**: operatore/flusso che ha originato l'evento (audit); `NULL` per gli eventi di
  sistema (webhook Stripe, cron Fiskaly).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 317.503 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `notification_details.createdAt` / `updatedAt` / —
- **Descrizione**: creazione (unico ordinamento del feed) / ultima modifica / soft-delete. Retention
  **6 mesi** ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)): è un **feed operativo**,
  non un archivio.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

**`payload jsonb` giustificato**: è uno **snapshot di variabili di rendering** con forma chiusa per
`kind`, mai interrogato per sotto-campo, ricostruzione non necessaria — stesso criterio del read-model
di configurazione ([D-28 approccio A](../_domande-committente.md); [D-49](../_domande-committente.md)).
Non è un modello di dominio: gli *eventi* di dominio (`BookingCreated`, `PaymentReceived`,
`EReceiptFailed`) sono altrove (C1/C3/C4), qui arriva solo la loro proiezione per la campanella. In
alternativa, colonne tipizzate nullable per ogni variabile — scartata: 10+ colonne quasi sempre
`NULL`, una per ogni `kind`.

## Suggerimenti per le prestazioni

- L'indice **parziale** `(property_id, created_at DESC) WHERE visibility = 'visible'` serve l'unica
  query di lettura; niente `findAll` senza `LIMIT` (era ~13 000 righe/chiamata nel legacy).
- **Partizionare per mese di `created_at`**; la retention (**6 mesi**) tiene poche partizioni calde: sei
  partizioni vive, e il purge diventa un `DROP PARTITION` invece di un `DELETE` massivo. Le letture di
  `notification_inapp_message_reads` seguono per FK cascade, senza una policy propria.
- Il badge "non lette" si calcola con un `COUNT` sulla figlia `notification_inapp_message_reads`
  (assenza di riga `read_at` per l'utente), non con un flag globale.
- `payload` in `jsonb` fuori riga (TOAST): non pesa sulle scansioni del feed.
- Realtime: il push al client (SignalR / WebSocket) porta il `public_id` della nuova riga; il client
  fa una `GET` mirata, non ricarica l'intero feed (era il pattern legacy con l'evento socket).

## Implementazione .NET

`InAppMessage` è l'**aggregate root** del feed campanella nel modulo `Module.Notification` (schema
`notification`): espone `public_id` per il deep-link UI e raccoglie la collezione figlia
`notification_inapp_message_reads` (stato di lettura per utente, FK interna `message_id`). `payload` è
uno *snapshot* tipizzato con contratto di codice, non un owned type navigabile in query.

### Enum propri

```csharp
// Domain/Enums/InAppKind.cs
public enum InAppKind
{
	BookingCreated = 0, ServiceReservationCreated = 1, PaymentReceived = 2, EreceiptDataError = 3,
	AutoReceiptFailed = 4, ReceiptedBookingMoveBlocked = 5, Generic = 6
}

// Domain/Enums/InAppSubjectKind.cs
public enum InAppSubjectKind { Booking = 0, ServiceReservation = 1 }

// Domain/Enums/InAppVisibility.cs
public enum InAppVisibility { Visible = 0, PendingConfirmation = 1 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<InAppKind>("notification", "inapp_kind");
modelBuilder.HasPostgresEnum<InAppSubjectKind>("notification", "inapp_subject_kind");
modelBuilder.HasPostgresEnum<InAppVisibility>("notification", "inapp_visibility");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<InAppKind>("notification.inapp_kind");
dataSourceBuilder.MapEnum<InAppSubjectKind>("notification.inapp_subject_kind");
dataSourceBuilder.MapEnum<InAppVisibility>("notification.inapp_visibility");
```

`payload` è un `record` unico con le variabili di rendering possibili (sottoinsieme valorizzato secondo
`kind`), serializzato `jsonb` con `System.Text.Json`:

```csharp
// Domain/ValueObjects/InAppMessagePayload.cs
public sealed record InAppMessagePayload(
	string? CustomerName = null, string? Channel = null,
	DateOnly? DateFrom = null, DateOnly? DateTo = null,
	string? Locations = null, string? Operator = null,
	decimal? Amount = null, string? Currency = null,
	string? ErrorSummary = null, string? StripeTransactionId = null);
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class InAppMessageConfiguration : IEntityTypeConfiguration<InAppMessage>
{
	public void Configure(EntityTypeBuilder<InAppMessage> builder)
	{
		builder.ToTable("inapp_messages", "notification", t => t.HasComment(
			"Un evento operativo di uno stabilimento mostrato nel feed della campanella del gestionale; testo renderizzato alla lettura da kind+payload."));

		builder.HasKey(x => x.Id).HasName("pk_notification_inapp_messages");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per il deep-link dalla campanella.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_inapp_messages_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Unico criterio di targeting: visibile a tutti gli operatori della property.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasDefaultValue(InAppKind.Generic)
			.IsRequired()
			.HasComment("Tipo di evento: determina template di rendering e navigazione UI al click.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasDefaultValue(InAppSubjectKind.Booking)
			.IsRequired()
			.HasComment("Soggetto della notifica (rotta di navigazione): prenotazione postazioni o servizio.");

		builder.Property(x => x.SubjectPublicId)
			.HasColumnName("subject_public_id")
			.HasComment("public_id di booking.reservations, copiato — nessuna FK cross-schema. NULL per le notifiche di errore Fiskaly.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(20)
			.HasComment("Numero prenotazione per il redirect al planner; nessun default fittizio.");

		builder.Property(x => x.Visibility)
			.HasColumnName("visibility")
			.HasDefaultValue(InAppVisibility.Visible)
			.IsRequired()
			.HasComment("Visibilità nel feed: visible, oppure pending_confirmation finché il pagamento non è confermato.");

		builder.Property(x => x.Payload)
			.HasColumnName("payload")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Snapshot delle sole variabili di rendering del template per questo kind, congelate alla creazione.");

		builder.Property(x => x.LegacyText)
			.HasColumnName("legacy_text")
			.HasColumnType("text")
			.HasComment("Stringa originale conservata as-is per le righe migrate il cui parsing verso kind+payload non ha riconosciuto il pattern.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. NULL per eventi di sistema (webhook Stripe, cron Fiskaly).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data creazione dell'evento; unico ordinamento del feed.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data ultima modifica (es. transizione di visibility).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; retention operativa 6 mesi (validata dal titolare), non è un archivio.");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasFilter("visibility = 'visible' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_inapp_messages_feed");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectPublicId })
			.HasDatabaseName("ix_notification_inapp_messages_subject");
	}
}
```

`InAppMessage` implementa `ISoftDeletable` e `IAuditable`. Non solleva eventi di dominio propri: è già
la *proiezione* di eventi di dominio altrove (`BookingCreated`, `PaymentReceived`, `EReceiptFailed`),
creata dai relativi handler.

### Note di migrazione

- Parsing best-effort di `notification_details.notification_text`/`notification_text_it` verso i
  pochi pattern noti (6+ punti di scrittura identificati in analisi) → `kind` + `payload` tipizzato;
  quando il parsing non riconosce il pattern (righe 2022, errori Fiskaly lunghi) → tutto in
  `legacy_text`, `kind = Generic`.
- `type` (`1`/`2`) → `subject_kind` (`Booking`/`ServiceReservation`).
- `status` → `visibility`: `1` → `Visible`, `2` → `PendingConfirmation`, `0` (40 righe) normalizzato a
  `Visible`.
- `reference_id` (id legacy con spazi sovrapposti `bookings.id`/`service_reservation.id`) → risolto
  tramite `subject_kind` verso il `public_id` di `booking_reservations` corrispondente; se non
  risolvibile, `subject_public_id = NULL` (stesso trattamento delle 119 righe Fiskaly senza riferimento).
- `bookingNumber = '0'` (default fittizio mai sostituito) → importato come `NULL`, non come stringa
  `'0'`.
- `user_id` (conteneva un `customers.id`, mai lo "scrivente" reale) → **non migrato**;
  `created_by_user_id` nasce `NULL` per tutto lo storico.
- `read` per property (il legacy azzerava per tutti gli operatori) → **non** ha un corrispettivo per
  utente nello storico: si riparte con feed "tutto da leggere" per ogni operatore (nessuna riga in
  `notification_inapp_message_reads` per le righe migrate) — scelta preferita a marcare tutto letto,
  per non nascondere eventi realmente non ancora visti da un operatore specifico.
