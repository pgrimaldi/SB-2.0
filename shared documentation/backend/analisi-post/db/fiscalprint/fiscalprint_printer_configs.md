# Tabella `fiscalprint_printer_configs`

> **Dominio**: C4bis — `fiscalprint` (Stampa fiscale su hardware RT) · **sottoarea**: — · **schema**:
> `fiscalprint` · **nome flat**: `fiscalprint_printer_configs` · **forma canonica**:
> `fiscalprint.printer_configs`.
>
> Sostituisce [`printer_configs`](../../../analisi-pre/db/printer_configs.md) (58 righe; 17
> `propertyId`, ~3-4 stabilimenti reali). Fonti:
> [`analisi-pre/db/printer_configs.md`](../../../analisi-pre/db/printer_configs.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.1bis, §3.5](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/business-logic/stabilimento/03-cassa.md` §6](../../../analisi-pre/business-logic/stabilimento/03-cassa.md),
> [`analisi-pre/business-logic/stabilimento/11-configurazione.md`](../../../analisi-pre/business-logic/stabilimento/11-configurazione.md).
>
> **Contesto C4bis**: meccanismo legacy **alternativo** a Fiskaly/SIGN IT — stampa dello scontrino su
> registratore di cassa RT fisico, gestita quasi tutta lato client (adapter Epson TM/FP, RCH in
> `front-pre/src/services/printers/`). **Si mantiene come componente separato** — decisione di
> business del committente, indipendente dalla copertura dello scontrino elettronico (C4) o
> dall'uso attuale. Qui è modellata come **target pulito ridisegnato** (decisione **C4bis-1**),
> non come migrazione 1:1.

---

## Descrizione

**Una riga = una stampante (termica / fiscale RT) configurata da uno stabilimento**: modello,
connessione di rete, modalità d'uso, intestazione/piè di pagina dello scontrino. È la controparte
"hardware" del flusso che alimenta [`fiscalprint_receipts`](fiscalprint_receipts.md) e
[`fiscalprint_serial_numbers`](fiscalprint_serial_numbers.md). N righe per `property_id`.

Correzioni rispetto al legacy:

- **`deletePrinter` non azzera `isActive`** → 37/56 righe con `isActive = 1` **e** `isdeleted = 1`
  (due flag scollegati) → un solo `deleted_at` + `is_active` reale
  ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)).
- **`port varchar(255)` non validata** (valori: IP nel campo porta, nome device USB
  `Port_#0001.Hub_#0001`, `"09100"`) → `connection_kind` enum (`tcp`/`usb`) + `port int` validato
  **o** `usb_device` — separati.
- **`model varchar` libero** con refusi (`"dddd"`, `"Asdax"`, `"fi"`) — vocabolario di fatto chiuso
  (ogni modello ha un **adapter dedicato**, serve codice per aggiungerne uno) → `enum`, non lookup
  ([`docs/03 §2.2.A / §2.4ter`](../../../progettazione/03-gestione-dati.md)); i refusi scartati in migrazione.
- **`mode varchar` libero** (`fiscal`/`nonfiscal`/`orders`) → `enum`.
- **`department_1/2/3` come `varchar` `"22%"`** (numero-con-simbolo; 3 reparti fissi; legame
  reparto↔aliquota **posizionale**) → tabella figlia
  [`fiscalprint_printer_departments`](fiscalprint_printer_departments.md)
  (`dept_number`, `vat_rate numeric`), N reparti, `UNIQUE(printer_id, dept_number)`.
- **`SSL tinyint` (nome maiuscolo, fuori standard)** → `use_tls` `boolean`.
- **Nessuna FK** verso `properties`/`users`, **nessun `UNIQUE`** → FK cross-modulo per valore
  (`property_id uuid`) + `UNIQUE` parziale candidato "una stampante attiva per `mode`".
- **`userId` senza FK** → `created_by_user_id` / `updated_by_user_id` `uuid`
  ([`docs/03 §3.2`](../../../progettazione/03-gestione-dati.md)).

## Verifica di esclusività dei flag

| Gruppo | Campi | Esito |
|---|---|---|
| Modello | `model` | **enum** esclusivo (un adapter per riga): `fp_81` / `fp_90` / `rch` / `tm_t88` / `tm_t20` / `online`. |
| Modalità d'uso | `mode` | **enum** esclusivo: `fiscal` / `non_fiscal` / `orders`. |
| Tipo di connessione | `connection_kind` | **enum** esclusivo: `tcp` / `usb`. Determina se sono valorizzati `host`+`port` **o** `usb_device`. |
| Stato / trasporto | `is_active`, `use_tls` | **due booleani indipendenti** (una stampante attiva può usare TLS o no). Non esclusivi, non enum. |

## Snapshot

Nessuno. Anagrafica di configurazione viva. Lo scontrino stampato conserva la sua "fotografia"
(intestazioni, matricola) dentro
[`fiscalprint_receipts.device_response`](fiscalprint_receipts.md) (`jsonb` grezzo del dispositivo).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiscalprint_printer_configs` | `id` | PK | — |
| `ix_fiscalprint_printer_configs_property` | `(property_id) WHERE deleted_at IS NULL` | btree parziale | `getPrinterConfiguration` (stampanti di uno stabilimento) |
| `uq_fiscalprint_printer_configs_active_mode` | `(property_id, mode) WHERE is_active AND deleted_at IS NULL` | UNIQUE parziale | **candidato**: una sola stampante attiva per modalità (mancava nel legacy — da confermare in fase implementativa) |

## Campi

### id
- **Legacy**: `printer_configs.id`
- **Descrizione**: PK tecnica. Usata come `printerId` negli endpoint update/status/delete e in
  `booking_receipts.printerId`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiscalprint_printer_departments.printer_id`,
  `fiscalprint_receipts.printer_id`.
- **Note**: **solo `bigint`** — referenziata solo entro lo schema `fiscalprint`. Nessun `public_id`.

### property_id
- **Legacy**: `printer_configs.propertyId` (`int`, `properties.id`, **nessuna FK**)
- **Descrizione**: stabilimento proprietario della stampante.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md);
  [D-01](../_domande-committente.md).

### name
- **Legacy**: `printer_configs.printername` (`varchar(255)`, obbligatorio in input)
- **Descrizione**: nome libero dato dall'operatore (etichetta in UI).
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### model
- **Legacy**: `printer_configs.model` (`varchar(255)` libero, con refusi)
- **Descrizione**: modello della stampante — determina l'adapter di stampa lato client
  (`createPrinterAdapter`). Un valore non riconosciuto fa **lanciare** l'adapter factory.
- **Tipo**: `enum fiscalprint.printer_model` — valori: `fp_81`, `fp_90`, `rch`, `tm_t88`, `tm_t20`,
  `online`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **enum** (non lookup): aggiungere un modello richiede una classe adapter dedicata →
  codice nuovo ([`docs/03 §2.2.A`](../../../progettazione/03-gestione-dati.md)).

### mode
- **Legacy**: `printer_configs.mode` (`varchar(255)` libero)
- **Descrizione**: modalità d'uso della stampante.
- **Tipo**: `enum fiscalprint.printer_mode` — valori: `fiscal` (scontrino fiscale RT), `non_fiscal`
  (documento non fiscale / preconto), `orders` (comanda).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `reparti` (`fiscalprint_printer_departments`) hanno senso solo per `fiscal`.

### connection_kind
- **Legacy**: implicito nei valori sporchi di `printer_configs.port`
- **Descrizione**: tipo di connessione al dispositivo.
- **Tipo**: `enum fiscalprint.connection_kind` — valori: `tcp`, `usb`.
- **Default**: `tcp`.
- **Nullabile**: No.
- **Relazioni**: `CHECK`: `tcp` ⇒ `host` e `port` valorizzati; `usb` ⇒ `usb_device` valorizzato.

### host
- **Legacy**: `printer_configs.ip` (`varchar(255)`)
- **Descrizione**: indirizzo di rete della stampante (IP LAN o `127.0.0.1` per stampante locale al
  client). Solo per `connection_kind = 'tcp'`.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### port
- **Legacy**: `printer_configs.port` (`varchar(255)` non validata)
- **Descrizione**: porta TCP della stampante. Solo per `connection_kind = 'tcp'`.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `CHECK (port BETWEEN 1 AND 65535)`; in migrazione i valori non numerici / fuori
  range si bonificano (`"09100"` → `9100`) o scartano.

### usb_device
- **Legacy**: `printer_configs.port` quando conteneva un nome device (`"Port_#0001.Hub_#0001"`)
- **Descrizione**: identificatore del device USB locale. Solo per `connection_kind = 'usb'`.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### use_tls
- **Legacy**: `printer_configs.SSL` (`tinyint(1)` `DEFAULT 0`, nome maiuscolo)
- **Descrizione**: `true` = comunicazione con la stampante via HTTPS/TLS; `false` = HTTP in chiaro.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_active
- **Legacy**: `printer_configs.isActive` (`tinyint` `DEFAULT 0`; **non** azzerata da `deletePrinter`)
- **Descrizione**: stampante attualmente selezionata/in uso per la sua `mode`.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE` parziale candidato `(property_id, mode) WHERE is_active AND deleted_at IS
  NULL`.

### heading_line_1 / heading_line_2
- **Legacy**: `printer_configs.heading_1` / `heading_2` (`varchar(255)` ciascuno)
- **Descrizione**: righe di intestazione stampate in testa allo scontrino/comanda.
- **Tipo**: `varchar(160)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### footer_line
- **Legacy**: `printer_configs.footer` (`varchar(255)`)
- **Descrizione**: riga di piè di pagina.
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_by_user_id / updated_by_user_id
- **Legacy**: `printer_configs.userId` (`int`, `users.id`, nessuna FK) / — (nuovo)
- **Descrizione**: operatore che ha configurato / modificato la stampante.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`) ciascuno.
- **Default**: nessuno / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 43 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `printer_configs.createdAt` / `updatedAt` (`datetime` `NOT NULL`, gestiti da Sequelize)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: automatici.

### deleted_at
- **Legacy**: `printer_configs.isdeleted` (`tinyint` `DEFAULT 0`)
- **Descrizione**: soft-delete. `getPrinterConfiguration` filtra le righe non eliminate.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF; alla cancellazione, `is_active` va comunque a `false`
  (correzione del bug legacy).

## Valutazione JSON

Nessun JSON. Il legacy non ne aveva; i 3 campi `department_*` (numeri-in-stringa posizionali)
diventano una **tabella figlia**, non un JSON.

## Suggerimenti per le prestazioni

- Tabella minuscola (decine di righe totali a regime). Nessun problema di volume.
- L'unico accesso è "stampanti dello stabilimento X" → indice parziale su `property_id`.
- Tabella minuscola, nessun bisogno di investire oltre gli indici elencati.

## Implementazione .NET

Modulo `fiscalprint` (stampa fiscale su hardware RT, meccanismo legacy alternativo allo scontrino
elettronico Fiskaly, mantenuto come componente separato — decisione di business, vedi
`_feature-tabelle.md` C4bis). `PrinterConfig` è l'**aggregate root** del modulo
`Fiscalprint`, schema `fiscalprint`: espone la collection `Departments` (entità figlia reale, tabella
propria — vedi [`fiscalprint_printer_departments`](fiscalprint_printer_departments.md)) e ancora, per
id per-valore, `fiscalprint_receipts`/`fiscalprint_serial_numbers`. Implementa `ISoftDeletable` +
`IAuditable` (ha `created_at`/`updated_at`/`deleted_at` reali).

### Enum propri

```csharp
// Domain/Enums/PrinterModel.cs
public enum PrinterModel { Fp81 = 0, Fp90 = 1, Rch = 2, TmT88 = 3, TmT20 = 4, Online = 5 }

// Domain/Enums/PrinterMode.cs
public enum PrinterMode { Fiscal = 0, NonFiscal = 1, Orders = 2 }

// Domain/Enums/ConnectionKind.cs
public enum ConnectionKind { Tcp = 0, Usb = 1 }
```

```csharp
// FiscalprintDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PrinterModel>("fiscalprint", "printer_model");
modelBuilder.HasPostgresEnum<PrinterMode>("fiscalprint", "printer_mode");
modelBuilder.HasPostgresEnum<ConnectionKind>("fiscalprint", "connection_kind");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<PrinterModel>("fiscalprint.printer_model");
dataSourceBuilder.MapEnum<PrinterMode>("fiscalprint.printer_mode");
dataSourceBuilder.MapEnum<ConnectionKind>("fiscalprint.connection_kind");
```

### Configuration

```csharp
namespace Module.Fiscalprint.Infrastructure.Configurations;

public sealed class PrinterConfigConfiguration : IEntityTypeConfiguration<PrinterConfig>
{
	public void Configure(EntityTypeBuilder<PrinterConfig> builder)
	{
		builder.ToTable("printer_configs", "fiscalprint", t =>
		{
			t.HasComment("Configurazione di una stampante (termica/fiscale RT) di uno stabilimento: modello, connessione, modalità d'uso, intestazioni scontrino.");
			t.HasCheckConstraint("ck_fiscalprint_printer_configs_port_range", "port IS NULL OR port BETWEEN 1 AND 65535");
			t.HasCheckConstraint("ck_fiscalprint_printer_configs_connection", "(connection_kind = 'tcp' AND host IS NOT NULL AND port IS NOT NULL) OR (connection_kind = 'usb' AND usb_device IS NOT NULL)");
		});

		builder.HasKey(x => x.Id).HasName("pk_fiscalprint_printer_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento proprietario), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome libero della stampante assegnato dall'operatore (etichetta in UI).");

		builder.Property(x => x.Model)
			.HasColumnName("model")
			.HasColumnType("fiscalprint.printer_model")
			.IsRequired()
			.HasComment("Modello stampante: determina l'adapter di stampa lato client (createPrinterAdapter).");

		builder.Property(x => x.Mode)
			.HasColumnName("mode")
			.HasColumnType("fiscalprint.printer_mode")
			.IsRequired()
			.HasComment("Modalità d'uso: scontrino fiscale RT, documento non fiscale/preconto, comanda.");

		builder.Property(x => x.ConnectionKind)
			.HasColumnName("connection_kind")
			.HasColumnType("fiscalprint.connection_kind")
			.IsRequired()
			.HasDefaultValue(ConnectionKind.Tcp)
			.HasComment("Tipo di connessione al dispositivo: tcp (host+port) o usb (usb_device).");

		builder.Property(x => x.Host)
			.HasColumnName("host")
			.HasMaxLength(255)
			.HasComment("Indirizzo di rete della stampante (IP LAN o 127.0.0.1), valorizzato solo per connection_kind = tcp.");

		builder.Property(x => x.Port)
			.HasColumnName("port")
			.HasComment("Porta TCP della stampante, valorizzata solo per connection_kind = tcp.");

		builder.Property(x => x.UsbDevice)
			.HasColumnName("usb_device")
			.HasMaxLength(255)
			.HasComment("Identificatore del device USB locale, valorizzato solo per connection_kind = usb.");

		builder.Property(x => x.UseTls)
			.HasColumnName("use_tls")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se la comunicazione con la stampante avviene via HTTPS/TLS anziché HTTP in chiaro.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Stampante attualmente selezionata/in uso per la sua modalità d'uso.");

		builder.Property(x => x.HeadingLine1)
			.HasColumnName("heading_line_1")
			.HasMaxLength(160)
			.HasComment("Prima riga di intestazione stampata in testa allo scontrino/comanda.");

		builder.Property(x => x.HeadingLine2)
			.HasColumnName("heading_line_2")
			.HasMaxLength(160)
			.HasComment("Seconda riga di intestazione stampata in testa allo scontrino/comanda.");

		builder.Property(x => x.FooterLine)
			.HasColumnName("footer_line")
			.HasMaxLength(160)
			.HasComment("Riga di piè di pagina stampata in coda allo scontrino/comanda.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha configurato la stampante — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha modificato per ultimo la stampante — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della configurazione.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica della configurazione.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico (corregge il bug legacy che scollegava i due flag).");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiscalprint_printer_configs_property");

		builder.HasIndex(x => new { x.PropertyId, x.Mode })
			.IsUnique()
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("uq_fiscalprint_printer_configs_active_mode");

		builder.HasMany(x => x.Departments)
			.WithOne(d => d.Printer)
			.HasForeignKey(d => d.PrinterId)
			.HasConstraintName("fk_fiscalprint_printer_departments_printer")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

- Gli adapter di stampa (Epson TM/FP, RCH) **restano lato client** (`front-pre/src/services/printers/`);
  il backend espone solo CRUD della configurazione — nessun `IPrinterAdapter` server-side.
- **Note di migrazione**: scartare/bonificare le 37/56 righe con `isActive = 1` **e** `isdeleted = 1`
  incoerenti, riportando `is_active` al valore reale e `deleted_at` da `isdeleted`; `model` libero →
  enum (refusi `"dddd"`/`"Asdax"`/`"fi"` → riga scartata, nessun default silenzioso); `mode` libero →
  enum; `port varchar` non validata → `connection_kind` (`tcp`/`usb`) derivato dal contenuto + `port
  int` bonificato (`"09100"` → `9100`) o `usb_device` per i nomi device; `SSL tinyint` → `use_tls
  boolean`; `department_1/2/3` → righe figlie in `fiscalprint_printer_departments`.
