# Tabella `catalog_property_online_booking_settings`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_property_online_booking_settings` · **forma canonica**:
> `catalog.property_online_booking_settings`.
>
> Consolida la configurazione **"prenotazione online delle postazioni"** oggi sparsa su 3 punti del
> legacy: la tabella [`online_reservations`](../../../analisi-pre/db/online_reservations.md) (16 righe,
> relitto), il flag `properties.onlineReservation`, e il JSON `properties.onlineBookingOptions` (+
> `properties.paymentOptions`). Fonti:
> [`analisi-pre/db/online_reservations.md`](../../../analisi-pre/db/online_reservations.md),
> [`analisi-pre/db/properties.md` (campi `onlineReservation` / `paymentOptions` / `onlineBookingOptions`)](../../../analisi-pre/db/properties.md),
> [`docs/03-gestione-dati.md` §5](../../../progettazione/03-gestione-dati.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §5, §11.6](../../../progettazione/01-criticita-e-indicazioni-backend.md).
>
> **Decisione (confermata committente)**: **Option A** — tabella **1:1 dedicata** (1 riga per property,
> `property_id` UNIQUE) + tabelle figlie
> [`catalog_property_online_allowed_locations`](catalog_property_online_allowed_locations.md),
> [`catalog_property_online_allowed_periods`](catalog_property_online_allowed_periods.md),
> [`catalog_property_online_stay_limits`](catalog_property_online_stay_limits.md). Modellata da
> `onlineBookingOptions` (config viva); `online_reservations` **non** migrata come tabella (colonne carta
> in chiaro eliminate — PCI-DSS). **`is_map_pick` / `is_location_restricted` / `is_period_restricted`
> restano 3 boolean indipendenti** (nei dati reali sono combinati: `map`+`location` è la combinazione
> dominante) — la proposta enum a 4 valori di `docs/03 §5` è **da correggere** in quel documento.

---

## Descrizione

Una riga per stabilimento che ha configurato il canale "prenotazione online postazioni". Se lo
stabilimento non ha mai configurato il canale → **nessuna riga** (come nel legacy: 16 righe su ~175).

Regola i **se/come** il cliente finale prenota online una postazione: canale attivo/sospeso, restrizioni
(mappa / sottoinsieme di postazioni / finestre di date), opzioni di dettaglio (sovrapprezzo, conferma
manuale, prenotabilità del giorno stesso, mezza giornata, stagionale, sharing, limite posti), le opzioni
di pagamento ammesse, il testo informativo, e la regola "chi può prenotare".

Correzioni rispetto al legacy:

- **Fine tripla fonte di verità**: `onlineReservation` (0/1), `online_reservations.isDisable`,
  `onlineBookingOptions` → un solo `is_enabled`.
- **Tipi reali**: i booleani travestiti da stringa (`"true"`/`"false"`, `"disable"`/`"Disable"`)
  diventano `boolean` / `enum`.
- **Enforcement server-side**: nel legacy questi flag erano applicati **solo dal frontend**
  (`bookingLimit` incluso) → nel target il motore di prenotazione (C1) li verifica al momento del
  `quote`/`confirm`.
- **Colonne carta** (`cvc`, `cardNumber`, `cardHodler`, `expiry`) → **eliminate**.
- **`paymentMethod` / i 5 flag `paymentOptions`** → un'unica colonna **`enabled_payment_methods`**
  `catalog.online_payment_method[]` (non 5 booleane; non tabella figlia — insieme piccolo, sempre letto
  con la riga). Stesso in
  [`catalog_property_service_booking_settings`](catalog_property_service_booking_settings.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_property_online_booking_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | 1:1 con la property |

## Campi

### id
- **Legacy**: `online_reservations.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata dalle 3 tabelle figlie.

### property_id
- **Legacy**: `online_reservations.propertyId` / `properties.id`
- **Descrizione**: stabilimento a cui si applica la configurazione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`; `UNIQUE`.

### is_enabled
- **Legacy**: `properties.onlineReservation` (`1`/`0`) · `online_reservations.isDisable` (invertito) · `onlineBookingOptions.locationOptionObj.blockReservation`
- **Descrizione**: canale prenotazione online postazioni attivo per lo stabilimento.
- **Tipo**: `boolean`.
- **Default**: `false` (la riga esiste solo se il canale è stato configurato; `is_enabled` esplicito).
- **Nullabile**: No.
- **Relazioni**: soggetto anche al kill-switch di piattaforma
  [`catalog_properties.is_online_sales_suspended`](catalog_properties.md).
- **Note**: sostituisce `blockReservation` (kill-switch locale) e la doppia negazione `isDisable`.

### is_map_pick
- **Legacy**: `onlineBookingOptions.isEnabledMap` · `online_reservations.isEnableMap`
- **Descrizione**: il cliente sceglie la postazione dalla **mappa** interattiva (vs. lista/richiesta).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **indipendente** da `is_location_restricted` e `is_period_restricted` (combinabili).

### is_location_restricted
- **Legacy**: `onlineBookingOptions.isEnableLocation` · `online_reservations.isEnableLocation`
- **Descrizione**: la prenotazione online è limitata al sottoinsieme di postazioni elencate in
  [`catalog_property_online_allowed_locations`](catalog_property_online_allowed_locations.md).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true`, la figlia `allowed_locations` deve avere ≥1 riga (validato in applicazione).
- **Note**: assorbe anche i sotto-campi legacy `enableResrInSelectedLoc` (variante ridondante).

### is_period_restricted
- **Legacy**: `onlineBookingOptions.isEnablePeriod` · `online_reservations.isEnablePeriod`
- **Descrizione**: la prenotazione online è consentita solo nelle finestre di date elencate in
  [`catalog_property_online_allowed_periods`](catalog_property_online_allowed_periods.md).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true`, la figlia `allowed_periods` deve avere ≥1 riga.
- **Note**: assorbe anche `enableResrInPeriod` (variante ridondante).

### surcharge_type
- **Legacy**: `onlineBookingOptions.locationOptionObj.surchargeType` (`"disable"`/`"Disable"` · `"percentage"` · `"fixed"`)
- **Descrizione**: come interpretare `surcharge_value`.
- **Tipo**: `enum catalog.surcharge_type` — valori: `disable`, `percentage`, `fixed`.
- **Default**: `disable`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: elimina la variante di capitalizzazione osservata nei dati reali.

### surcharge_value
- **Legacy**: `onlineBookingOptions.locationOptionObj.addSurcharge` (stringa numerica, es. `"0"`)
- **Descrizione**: importo (se `surcharge_type = fixed`, in euro) o percentuale (se `percentage`) del
  sovrapprezzo applicato alle prenotazioni online.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: il calcolo del sovrapprezzo va fatto **server-side** (motore prezzo C1) — nel legacy era
  client-side.

### is_online_payment_blocked
- **Legacy**: `onlineBookingOptions.locationOptionObj.blockOnlinePayment` (`"disable"`/`"enable"`)
- **Descrizione**: se `true`, il cliente non può pagare online (solo opzioni "paga in struttura", a
  prescindere dalle opzioni di pagamento sotto).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: booleano travestito da stringa nel legacy. **Override** rispetto a `enabled_payment_methods`
  (sotto): se `true`, il pagamento online è bloccato a prescindere dall'insieme (di fatto equivale a un
  insieme con soli metodi `pay_at_property*`).

### requires_manual_confirmation
- **Legacy**: `onlineBookingOptions.locationOptionObj.requestBookingConfirmation`
- **Descrizione**: le prenotazioni online restano "da confermare" e richiedono approvazione manuale del
  gestore.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: consumato da C1 (stato iniziale della prenotazione online).

### allows_same_day_booking
- **Legacy**: `onlineBookingOptions.locationOptionObj.currentDayBookable` (`false` / `"true"`)
- **Descrizione**: se `true`, il cliente può prenotare online **per oggi** (lo stesso giorno di calendario
  in cui invia la richiesta); se `false`, le prenotazioni online devono essere per una data futura
  (da domani in poi).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### allows_half_day
- **Legacy**: `onlineBookingOptions.locationOptionObj.enableHalfDayBook`
- **Descrizione**: abilita la prenotazione online per mezza giornata (mattina/pomeriggio).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### allows_seasonal
- **Legacy**: `onlineBookingOptions.locationOptionObj.seasonalReservation` (`false` / `"true"`)
- **Descrizione**: abilita la prenotazione online "stagionale" (pass multi-settimana/mese).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### allows_sharing
- **Legacy**: `onlineBookingOptions.locationOptionObj.sharingOptions` (`true` / `"false"`)
- **Descrizione**: il cliente può attivare la funzione "sharing" (rivendita/condivisione del posto)
  durante la prenotazione online. I parametri operativi dello sharing sono **C1** (non C2 —
  [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)); qui c'è solo l'interruttore di
  esposizione nel flusso online.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### max_spots_per_booking
- **Legacy**: `onlineBookingOptions.locationOptionObj.maxSeatReservation` (`null` / intero)
- **Descrizione**: numero massimo di postazioni prenotabili in una singola prenotazione online.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` = nessun limite).
- **Relazioni**: —.

### allow_online_booking_scope
- **Legacy**: `onlineBookingOptions.locationOptionObj.allowOnlineBooking` (`"all"` / `"contacts_only"`; assente ⇒ `all`)
- **Descrizione**: chi può prenotare online. `contacts_only` = solo se esiste già un cliente in rubrica
  per quella property con stesso telefono **e** email.
- **Tipo**: `enum catalog.online_booking_scope` — valori: `all`, `contacts_only`.
- **Default**: `all`.
- **Nullabile**: No.
- **Relazioni**: `contacts_only` risolto contro C5 `identity_customer_customers` (per valore, al bordo
  del modulo di prenotazione).

### useful_info
- **Legacy**: `onlineBookingOptions.locationOptionObj.useFulInfo`
- **Descrizione**: testo libero "informazioni utili" mostrato al cliente nel flusso online.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### good_to_know
- **Legacy**: `onlineBookingOptions.locationOptionObj.goodToKnow`
- **Descrizione**: testo libero aggiuntivo ("da sapere") mostrato al cliente.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### enabled_payment_methods
- **Legacy**: `properties.paymentOptions` — i 5 flag `pay_now_info`, `pay_at_property_info`,
  `pay_at_property_no_credit_info`, `deposit_now_half_info`, `pay_at_property_prepaid_info`
- **Descrizione**: **insieme** dei metodi di pagamento offerti al cliente per la prenotazione online
  postazioni (il cliente ne sceglie uno al checkout). Nel legacy erano 5 checkbox **indipendenti** in
  `PaymentOptionSelection.js` (qualunque combinazione lecita) → una sola colonna array anziché 5 booleane.
- **Tipo**: `catalog.online_payment_method[]` — enum: `pay_now` (intero importo online, Stripe),
  `pay_at_property` (paga in struttura, con carta a garanzia), `pay_at_property_no_card` (paga in
  struttura senza carta), `deposit_half` (acconto 50% online), `pay_at_property_prepaid` (paga in
  struttura con pre-addebito carta).
- **Default**: `'{}'` (nessun metodo → canale online di fatto non prenotabile).
- **Nullabile**: No.
- **Relazioni**: `pay_now` / `deposit_half` / `pay_at_property_prepaid` dipendono da un account Stripe
  Connect collegato (C3): esposti solo se presente. `pay_now` è anche la condizione dello scontrino
  automatico Fiskaly (C4) — verificata via `payment_type` sulla transazione, non qui.
- **Note**: scelta **`enum[]`** (non tabella figlia, non 5 colonne) perché l'insieme è piccolo, stabile,
  **sempre letto insieme alla riga** (nessun join), senza metadati per elemento; indicizzabile GIN
  (`@>` / `&& `) se in futuro serve filtrare "property che offrono `pay_now`". `is_online_payment_blocked`
  (sopra) è **ridondante** con un insieme privo di `pay_now`/`deposit_half`: resta come **override
  esplicito** (blocca il pagamento online a prescindere dall'insieme). In migrazione: un flag `1` → il
  relativo valore enum nell'array; le chiavi "vecchie" `pay_in_structure_info` /
  `advanced_balance_structure_info` **non** si migrano.

### created_at / updated_at
- **Legacy**: `online_reservations.createdAt` / `online_reservations.updatedAt` (spesso `NULL`)
- **Descrizione**: creazione / ultima modifica della configurazione del canale.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `updated_at` di questa riga dice quando il gestore ha toccato l'ultima volta le impostazioni
  online, senza sporcare `catalog_properties.updated_at`.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: normalmente non usato (per disattivare il canale si usa `is_enabled = false`); presente per
  uniformità.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `online_reservations.cvc` / `cardNumber` / `cardHodler` / `expiry` | PAN/CVC in chiaro — **violazione PCI-DSS**, mai a DB |
| `online_reservations.email` | mai valorizzato |
| `online_reservations.paymentMethod` | ridondante con `properties.paymentOptions` → colonne booleane per opzione |
| `properties.paymentOptions.pay_in_structure_info` / `advanced_balance_structure_info` | chiavi "vecchie" non più esposte dal componente attivo — debito da non riportare |
| `onlineBookingOptions.locationOptionObj.enablePlaceOnMap` / `enableResrInPeriod` / `enableResrInSelectedLoc` | varianti UI ridondanti di `is_map_pick` / `is_period_restricted` / `is_location_restricted` |
| `onlineBookingOptions.period[].key` | artefatto `react-date-range` |

## Valutazione JSON

Nessun JSON. `onlineBookingOptions` (il JSON più articolato del legacy) è interamente scomposto: scalari
→ colonne tipizzate; `locations[]` / `period[]` / `bookingLimit[]` → 3 tabelle figlie;
`legendList[]` → [`catalog_map_legend_items`](catalog_map_legend_items.md) (sottoarea `map`).

## Suggerimenti per le prestazioni

- Lookup 1:1 su `property_id` (UNIQUE) — costo trascurabile su ~175 righe. Il motore di prenotazione (C1)
  legge questa riga + le 3 figlie una volta, all'inizio del `quote`.
- Aggiungere opzioni future (giorni di anticipo minimo, cutoff orario, % acconto configurabile) = nuove
  colonne su questa tabella, senza toccare `catalog_properties`.
- Le figlie `allowed_locations` / `allowed_periods` / `stay_limits` sono piccole (≤ decine di righe per
  property); indicizzate solo per `settings_id`.

## Implementazione .NET

`OnlineBookingSettings` è l'**aggregate root** della configurazione del canale "prenotazione online
postazioni" (modulo `Module.Catalog`, radice del dominio, schema `catalog`): relazione **1:1** con
`Property` (`property_id UNIQUE`), possiede per riferimento le tre tabelle figlie
[`catalog_property_online_allowed_locations`](catalog_property_online_allowed_locations.md),
[`catalog_property_online_allowed_periods`](catalog_property_online_allowed_periods.md) e
[`catalog_property_online_stay_limits`](catalog_property_online_stay_limits.md) (entità con propria PK,
esposte come collezioni). Implementa `IAuditable`/`ISoftDeletable`.

### Enum propri

```csharp
// Domain/Enums/SurchargeType.cs
public enum SurchargeType { Disable = 0, Percentage = 1, Fixed = 2 }

// Domain/Enums/OnlineBookingScope.cs
public enum OnlineBookingScope { All = 0, ContactsOnly = 1 }

// Domain/Enums/OnlinePaymentMethod.cs
public enum OnlinePaymentMethod
{
    PayNow = 0,
    PayAtProperty = 1,
    PayAtPropertyNoCard = 2,
    DepositHalf = 3,
    PayAtPropertyPrepaid = 4
}
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<SurchargeType>("catalog", "surcharge_type");
modelBuilder.HasPostgresEnum<OnlineBookingScope>("catalog", "online_booking_scope");
modelBuilder.HasPostgresEnum<OnlinePaymentMethod>("catalog", "online_payment_method");
```

```csharp
// Registrazione data source
dataSourceBuilder.MapEnum<SurchargeType>("catalog.surcharge_type");
dataSourceBuilder.MapEnum<OnlineBookingScope>("catalog.online_booking_scope");
dataSourceBuilder.MapEnum<OnlinePaymentMethod>("catalog.online_payment_method");
```

`SurchargeType` e `OnlinePaymentMethod` sono **condivisi** con
[`catalog_property_service_booking_settings`](catalog_property_service_booking_settings.md) — definiti
qui una sola volta, riusati lì.

### Entità

```csharp
// Domain/Entities/OnlineBookingSettings.cs
namespace Module.Catalog.Domain.Entities;

public sealed class OnlineBookingSettings : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long PropertyId { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool IsMapPick { get; private set; }
    public bool IsLocationRestricted { get; private set; }
    public bool IsPeriodRestricted { get; private set; }
    public SurchargeType SurchargeType { get; private set; }
    public decimal SurchargeValue { get; private set; }
    public bool IsOnlinePaymentBlocked { get; private set; }
    public bool RequiresManualConfirmation { get; private set; }
    public bool AllowsSameDayBooking { get; private set; }
    public bool AllowsHalfDay { get; private set; }
    public bool AllowsSeasonal { get; private set; }
    public bool AllowsSharing { get; private set; }
    public short? MaxSpotsPerBooking { get; private set; }
    public OnlineBookingScope AllowOnlineBookingScope { get; private set; }
    public string? UsefulInfo { get; private set; }
    public string? GoodToKnow { get; private set; }
    public List<OnlinePaymentMethod> EnabledPaymentMethods { get; private set; } = new();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private readonly List<OnlineAllowedLocation> _allowedLocations = new();
    private readonly List<OnlineAllowedPeriod> _allowedPeriods = new();
    private readonly List<OnlineStayLimit> _stayLimits = new();
    public IReadOnlyCollection<OnlineAllowedLocation> AllowedLocations => _allowedLocations.AsReadOnly();
    public IReadOnlyCollection<OnlineAllowedPeriod> AllowedPeriods => _allowedPeriods.AsReadOnly();
    public IReadOnlyCollection<OnlineStayLimit> StayLimits => _stayLimits.AsReadOnly();

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<OnlineBookingSettings>`

```csharp
// Infrastructure/Configurations/OnlineBookingSettingsConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class OnlineBookingSettingsConfiguration : IEntityTypeConfiguration<OnlineBookingSettings>
{
    public void Configure(EntityTypeBuilder<OnlineBookingSettings> builder)
    {
        builder.ToTable("property_online_booking_settings", "catalog", t => t.HasComment(
            "Configurazione del canale di prenotazione online delle postazioni per uno stabilimento (1:1)."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_booking_settings");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.PropertyId)
            .HasColumnName("property_id")
            .IsRequired()
            .HasComment("Stabilimento a cui si applica la configurazione.");

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Canale prenotazione online postazioni attivo per lo stabilimento.");

        builder.Property(x => x.IsMapPick)
            .HasColumnName("is_map_pick")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Il cliente sceglie la postazione dalla mappa interattiva (vs. lista/richiesta); indipendente dagli altri due flag di restrizione.");

        builder.Property(x => x.IsLocationRestricted)
            .HasColumnName("is_location_restricted")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("La prenotazione online è limitata al sottoinsieme di postazioni in allowed_locations.");

        builder.Property(x => x.IsPeriodRestricted)
            .HasColumnName("is_period_restricted")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("La prenotazione online è consentita solo nelle finestre di date in allowed_periods.");

        builder.Property(x => x.SurchargeType)
            .HasColumnName("surcharge_type")
            .HasColumnType("catalog.surcharge_type")
            .HasDefaultValue(Domain.Enums.SurchargeType.Disable)
            .IsRequired()
            .HasComment("Come interpretare surcharge_value: disabilitato, percentuale o importo fisso.");

        builder.Property(x => x.SurchargeValue)
            .HasColumnName("surcharge_value")
            .HasColumnType("numeric(12,2)")
            .HasDefaultValue(0m)
            .IsRequired()
            .HasComment("Importo o percentuale del sovrapprezzo sulle prenotazioni online (calcolato server-side dal motore prezzo).");

        builder.Property(x => x.IsOnlinePaymentBlocked)
            .HasColumnName("is_online_payment_blocked")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Se true, il cliente non può pagare online: override che blocca il pagamento a prescindere da enabled_payment_methods.");

        builder.Property(x => x.RequiresManualConfirmation)
            .HasColumnName("requires_manual_confirmation")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Le prenotazioni online restano \"da confermare\" e richiedono approvazione manuale del gestore.");

        builder.Property(x => x.AllowsSameDayBooking)
            .HasColumnName("allows_same_day_booking")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Il cliente può prenotare online per il giorno stesso; se false, solo da domani in poi.");

        builder.Property(x => x.AllowsHalfDay)
            .HasColumnName("allows_half_day")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Abilita la prenotazione online per mezza giornata (mattina/pomeriggio).");

        builder.Property(x => x.AllowsSeasonal)
            .HasColumnName("allows_seasonal")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Abilita la prenotazione online \"stagionale\" (pass multi-settimana/mese).");

        builder.Property(x => x.AllowsSharing)
            .HasColumnName("allows_sharing")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in C1).");

        builder.Property(x => x.MaxSpotsPerBooking)
            .HasColumnName("max_spots_per_booking")
            .HasComment("Numero massimo di postazioni prenotabili in una singola prenotazione online; NULL = nessun limite.");

        builder.Property(x => x.AllowOnlineBookingScope)
            .HasColumnName("allow_online_booking_scope")
            .HasColumnType("catalog.online_booking_scope")
            .HasDefaultValue(Domain.Enums.OnlineBookingScope.All)
            .IsRequired()
            .HasComment("Chi può prenotare online: tutti, o solo clienti già in rubrica (contacts_only).");

        builder.Property(x => x.UsefulInfo)
            .HasColumnName("useful_info")
            .HasComment("Testo libero \"informazioni utili\" mostrato al cliente nel flusso online.");

        builder.Property(x => x.GoodToKnow)
            .HasColumnName("good_to_know")
            .HasComment("Testo libero aggiuntivo \"da sapere\" mostrato al cliente.");

        builder.Property(x => x.EnabledPaymentMethods)
            .HasColumnName("enabled_payment_methods")
            .HasColumnType("catalog.online_payment_method[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired()
            .HasComment("Insieme dei metodi di pagamento offerti al cliente per la prenotazione online postazioni.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della configurazione.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della configurazione.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete (non usato in pratica: per disattivare il canale si usa is_enabled = false).");

        builder.HasIndex(x => x.PropertyId)
            .IsUnique()
            .HasDatabaseName("uq_catalog_property_online_booking_settings_property");

        builder.HasOne<Property>()
            .WithOne()
            .HasForeignKey<OnlineBookingSettings>(x => x.PropertyId)
            .HasConstraintName("fk_catalog_property_online_booking_settings_property")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AllowedLocations)
            .WithOne()
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_allowed_locations_settings")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AllowedPeriods)
            .WithOne()
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_allowed_periods_settings")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StayLimits)
            .WithOne()
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_stay_limits_settings")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `property_id` → FK reale 1:1 a `catalog_properties.id` (stesso schema), `ON DELETE CASCADE`, `UNIQUE`.
- `AllowedLocations`/`AllowedPeriods`/`StayLimits` → FK reale interna delle rispettive figlie su
  `settings_id`, `ON DELETE CASCADE`.
- `allow_online_booking_scope = ContactsOnly` risolto **per valore** contro C5
  `identity_customer_customers` al bordo del motore di prenotazione — nessuna FK.
- `enabled_payment_methods` con `pay_now`/`deposit_half`/`pay_at_property_prepaid` condizionati
  all'esistenza di un account Stripe Connect (C3) — verifica applicativa, non un vincolo DB.

### Validazioni applicative

- `is_location_restricted ⇒ AllowedLocations` non vuota (controllo in Application, non `CHECK` DB — la
  figlia può essere popolata in una transazione separata dall'attivazione del flag).
- `is_period_restricted ⇒ AllowedPeriods` non vuota, stessa logica.
- Il motore `quote`/`confirm` (C1) è l'unico enforcement reale di sovrapprezzo, restrizioni e limiti:
  contratto verso C1 tramite un DTO `OnlineBookingRules` proiettato da questo aggregate — nel legacy
  questi controlli erano **solo lato frontend**.

### Note di migrazione

- Fonte viva: `properties.onlineBookingOptions`; dove assente ma esiste una riga in `online_reservations`
  (16 righe), riconciliare i pochi campi sovrapposti; nessuna riga se il canale non è mai stato
  configurato.
- Colonne carta (`cvc`, `cardNumber`, `cardHodler`, `expiry`) **eliminate** (PCI-DSS), mai portate a DB.
- `properties.paymentOptions` (5 flag booleani indipendenti) → array `enabled_payment_methods`; le chiavi
  "vecchie" `pay_in_structure_info`/`advanced_balance_structure_info` non si migrano.
- `isDisable`/`blockReservation`/le varianti UI ridondanti (`enablePlaceOnMap`,
  `enableResrInPeriod`, `enableResrInSelectedLoc`) collassano nei tre booleani `is_enabled` /
  `is_map_pick` / `is_period_restricted` / `is_location_restricted`.
- Stringhe booleane travestite (`"true"`/`"false"`, `"disable"`/`"Disable"`) normalizzate ai tipi reali.
