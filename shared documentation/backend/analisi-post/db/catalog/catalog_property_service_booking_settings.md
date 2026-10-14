# Tabella `catalog_property_service_booking_settings`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_property_service_booking_settings` · **forma canonica**:
> `catalog.property_service_booking_settings`.
>
> Consolida la configurazione **"prenotazione online dei servizi"** (bar / normali / a ore), oggi su
> `properties.serviceOnlineBooking` + `properties.serviceBookingOptions` (JSON) +
> `properties.servicebookingPaymentOptions` (JSON). Fonti:
> [`analisi-pre/db/properties.md` (campi `serviceOnlineBooking` / `serviceBookingOptions` / `servicebookingPaymentOptions`)](../../../analisi-pre/db/properties.md),
> [`docs/03-gestione-dati.md` §5](../../../progettazione/03-gestione-dati.md).
>
> **Decisione (confermata committente)**: canale **separato** da
> [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md) (i set di
> campi differiscono troppo per un discriminatore `channel`). 1 riga per property, `property_id` UNIQUE;
> nessuna riga se il canale servizi non è stato configurato.

---

## Descrizione

Impostazioni del canale online per la vendita di **servizi** (non postazioni): interruttore, sovrapprezzo,
conferma manuale, titolo/testo personalizzati, opzioni di pagamento. Struttura più ridotta della
controparte "postazioni" (niente mappa/settori/limiti di soggiorno — i servizi non hanno una posizione
fisica). Nei dati legacy il JSON `serviceBookingOptions` è quasi sempre `{}` e
`servicebookingPaymentOptions` >99% `NULL` — feature poco configurata.

Correzioni rispetto al legacy: booleani travestiti da stringa → `boolean`/`enum`; `serviceOnlineBooking`
messo a `0` come **effetto collaterale** della disconnessione Stripe (`deleteStripeAccount`) — nel target
è un evento esplicito, `is_enabled` non viene toccato di nascosto.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_property_service_booking_settings` | `id` | PK | — |
| `uq_..._property` | `(property_id)` | UNIQUE | 1:1 con la property |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `properties.id`
- **Descrizione**: stabilimento a cui si applica.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`; `UNIQUE`.

### is_enabled
- **Legacy**: `properties.serviceOnlineBooking` (`1`/`0`)
- **Descrizione**: canale prenotazione online servizi attivo.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: soggetto al kill-switch
  [`catalog_properties.is_online_sales_suspended`](catalog_properties.md).
- **Note**: nel legacy portato a `0` da `deleteStripeAccount` — nel target la disconnessione Stripe
  emette un evento C3 che C2 gestisce esplicitamente.

### surcharge_type
- **Legacy**: `serviceBookingOptions.surchargeType` (`"Disable"`/`"disable"` · `"percentage"` · `"fixed"`)
- **Descrizione**: come interpretare `surcharge_value`.
- **Tipo**: `enum catalog.surcharge_type` (stesso enum di
  [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md)) — valori:
  `disable`, `percentage`, `fixed`.
- **Default**: `disable`.
- **Nullabile**: No.
- **Relazioni**: —.

### surcharge_value
- **Legacy**: `serviceBookingOptions.addSurcharge` (stringa numerica / `null`)
- **Descrizione**: importo o percentuale del sovrapprezzo sulle prenotazioni online di servizi.
- **Tipo**: `numeric(12,2)`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### requires_manual_confirmation
- **Legacy**: `serviceBookingOptions.requestBookingConfirmation` (`true` / `"true"`)
- **Descrizione**: le prenotazioni servizi online restano "da confermare".
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: consumato da C1.

### section_title
- **Legacy**: `serviceBookingOptions.serviceBookingTitle`
- **Descrizione**: titolo personalizzato della sezione "prenota i servizi online" sul sito pubblico.
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### useful_info
- **Legacy**: `serviceBookingOptions.useFulInfo`
- **Descrizione**: testo informativo mostrato durante la prenotazione online di un servizio.
- **Tipo**: `text`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### enabled_payment_methods
- **Legacy**: `properties.servicebookingPaymentOptions.{pay_now_info, pay_at_property_info, pay_at_property_no_credit_info, deposit_now_half_info, pay_at_property_prepaid_info}`
- **Descrizione**: **insieme** dei metodi di pagamento offerti per la prenotazione online di **servizi** —
  stesso significato e stesso enum di
  [`catalog_property_online_booking_settings.enabled_payment_methods`](catalog_property_online_booking_settings.md).
- **Tipo**: `catalog.online_payment_method[]` — valori `pay_now`, `pay_at_property`,
  `pay_at_property_no_card`, `deposit_half`, `pay_at_property_prepaid`.
- **Default**: `'{}'`.
- **Nullabile**: No.
- **Relazioni**: `pay_now` / `deposit_half` / `pay_at_property_prepaid` dipendono da un account Stripe
  Connect collegato (C3).
- **Note**: **`enum[]`** anziché 5 booleane, come per il canale postazioni. `blockOnlinePayment` era
  presente ma **commentato** nel form legacy → non modellato (nessun `is_online_payment_blocked` qui).
  In produzione `servicebookingPaymentOptions` è quasi sempre `{}` → array vuoto nella maggior parte
  delle righe.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. `serviceBookingOptions` e `servicebookingPaymentOptions` → colonne tipizzate.

## Suggerimenti per le prestazioni

- Lookup 1:1 su `property_id`. Tabella minuscola.
- Letta dal motore di prenotazione servizi (C1) insieme al catalogo
  [`catalog_service_sale_items`](catalog_service_sale_items.md).

## Implementazione .NET

`ServiceBookingSettings` è un **aggregate root** minimale (modulo `Module.Catalog`, radice del dominio,
schema `catalog`), relazione **1:1** con `Property` (`property_id UNIQUE`), senza figlie proprie (a
differenza della controparte "postazioni": nessuna mappa/settori/limiti di soggiorno). Implementa
`IAuditable`/`ISoftDeletable`. Non definisce enum propri: `SurchargeType` e `OnlinePaymentMethod` sono
quelli già definiti in
[`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md) — non
ridefiniti qui.

### Entità

```csharp
// Domain/Entities/ServiceBookingSettings.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ServiceBookingSettings : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long PropertyId { get; private set; }
    public bool IsEnabled { get; private set; }
    public SurchargeType SurchargeType { get; private set; }
    public decimal SurchargeValue { get; private set; }
    public bool RequiresManualConfirmation { get; private set; }
    public string? SectionTitle { get; private set; }
    public string? UsefulInfo { get; private set; }
    public List<OnlinePaymentMethod> EnabledPaymentMethods { get; private set; } = new();

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<ServiceBookingSettings>`

```csharp
// Infrastructure/Configurations/ServiceBookingSettingsConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ServiceBookingSettingsConfiguration : IEntityTypeConfiguration<ServiceBookingSettings>
{
    public void Configure(EntityTypeBuilder<ServiceBookingSettings> builder)
    {
        builder.ToTable("property_service_booking_settings", "catalog", t => t.HasComment(
            "Configurazione del canale di prenotazione online dei servizi (bar/normali/a ore) per uno stabilimento (1:1)."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_property_service_booking_settings");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.PropertyId)
            .HasColumnName("property_id")
            .IsRequired()
            .HasComment("Stabilimento a cui si applica la configurazione.");

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Canale prenotazione online servizi attivo.");

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
            .HasComment("Importo o percentuale del sovrapprezzo sulle prenotazioni online di servizi.");

        builder.Property(x => x.RequiresManualConfirmation)
            .HasColumnName("requires_manual_confirmation")
            .HasDefaultValue(false)
            .IsRequired()
            .HasComment("Le prenotazioni servizi online restano \"da confermare\" e richiedono approvazione manuale del gestore.");

        builder.Property(x => x.SectionTitle)
            .HasColumnName("section_title")
            .HasMaxLength(255)
            .HasComment("Titolo personalizzato della sezione \"prenota i servizi online\" sul sito pubblico.");

        builder.Property(x => x.UsefulInfo)
            .HasColumnName("useful_info")
            .HasComment("Testo informativo mostrato durante la prenotazione online di un servizio.");

        builder.Property(x => x.EnabledPaymentMethods)
            .HasColumnName("enabled_payment_methods")
            .HasColumnType("catalog.online_payment_method[]")
            .HasDefaultValueSql("'{}'")
            .IsRequired()
            .HasComment("Insieme dei metodi di pagamento offerti per la prenotazione online di servizi.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della configurazione.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della configurazione.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.PropertyId)
            .IsUnique()
            .HasDatabaseName("uq_catalog_property_service_booking_settings_property");

        builder.HasOne<Property>()
            .WithOne()
            .HasForeignKey<ServiceBookingSettings>(x => x.PropertyId)
            .HasConstraintName("fk_catalog_property_service_booking_settings_property")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `property_id` → FK reale 1:1 a `catalog_properties.id` (stesso schema), `ON DELETE CASCADE`, `UNIQUE`.
- `pay_now`/`deposit_half`/`pay_at_property_prepaid` in `enabled_payment_methods` dipendono da un account
  Stripe Connect collegato (C3) — verifica applicativa, non un vincolo DB.

### Note di migrazione

- Nessuna riga se il canale servizi non è mai stato configurato: fonte `serviceBookingOptions` (quasi
  sempre `{}`) e `servicebookingPaymentOptions` (>99% `NULL`) → default `false`/array vuoto quando assenti.
- `serviceOnlineBooking` (0/1) → `is_enabled`; nel legacy portato a `0` come **effetto collaterale** di
  `deleteStripeAccount` — nel target la disconnessione Stripe emette un evento (`StripeAccountDisconnected`
  da C3) che questo modulo gestisce **esplicitamente**, senza toccare `is_enabled` "di nascosto".
- `surchargeType`/`addSurcharge` → stesso trattamento di
  [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md).
- `servicebookingPaymentOptions.{...}` → `enabled_payment_methods` (stesso enum `OnlinePaymentMethod`
  della controparte postazioni). `blockOnlinePayment` era presente ma **commentato** nel form legacy →
  non modellato (nessun `is_online_payment_blocked` in questa tabella).
