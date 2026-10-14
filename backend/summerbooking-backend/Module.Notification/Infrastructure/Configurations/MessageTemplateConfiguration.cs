using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
	private static readonly ValueConverter<List<PlaceholderKey>, string[]> PlaceholderKeysConverter = new(
		keys => keys.Select(k => k.ToString()).ToArray(),
		values => values.Select(v => Enum.Parse<PlaceholderKey>(v)).ToList());

	private static readonly ValueComparer<List<PlaceholderKey>> PlaceholderKeysComparer = new(
		(a, b) => a!.SequenceEqual(b!),
		l => l.Aggregate(0, (h, k) => HashCode.Combine(h, k)),
		l => l.ToList());

	public void Configure(EntityTypeBuilder<MessageTemplate> builder)
	{
		builder.ToTable("templates", "notification", t => t.HasComment(
			"Catalogo unico dei template di messaggio: oggetto + corpo con segnaposto per uno scopo, un canale e una lingua."));

		builder.HasKey(x => x.Id).HasName("pk_notification_templates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato in API/URL gestionale e riferito da notification_deliveries.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_templates_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL = template di piattaforma/seed di sistema.");

		builder.Property(x => x.TemplateKind)
			.HasColumnName("template_kind")
			.HasColumnType("notification.template_kind")
			.HasDefaultValueSql("'generic'")
			.IsRequired()
			.HasComment("Scopo del template (riepilogo prenotazione, report convenzione, campagna, automazione, transazionale, generico).");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("notification.template_channel")
			.HasDefaultValueSql("'email'")
			.IsRequired()
			.HasComment("Canale per cui è pensato il contenuto; 'any' = il renderer applica l'escaping del canale d'invio effettivo.");

		builder.Property(x => x.Culture)
			.HasColumnName("culture")
			.HasMaxLength(10)
			.HasDefaultValue("it")
			.IsRequired()
			.HasComment("Cultura del contenuto, formato BCP-47.");

		builder.Property(x => x.Title)
			.HasColumnName("title")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Etichetta identificativa mostrata nella tendina di selezione (non è il contenuto).");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.HasComment("Oggetto del messaggio con segnaposto {{ … }}; obbligatorio in validazione per i canali email/push.");

		builder.Property(x => x.Body)
			.HasColumnName("body")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Corpo del messaggio con segnaposto {{ … }} mustache: HTML per email, testo per gli altri canali.");

		builder.Property(x => x.PlaceholderKeysUsed)
			.HasColumnName("placeholder_keys_used")
			.HasColumnType("text[]")
			.HasConversion(PlaceholderKeysConverter)
			.Metadata.SetValueComparer(PlaceholderKeysComparer);
		builder.Property(x => x.PlaceholderKeysUsed)
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Segnaposto canonici realmente usati in subject+body, derivati dal parsing e mantenuti in sync ad ogni salvataggio.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Template selezionabile dal gestore; i lettori filtrano is_active AND deleted_at IS NULL.");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Template predefinito per la combinazione (property_id, template_kind, channel, culture).");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione del template.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: ex status=0 di custom_templates / isDeleted di hotel_message_templates.");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.TemplateKind, x.Channel, x.Culture }, "ix_notification_templates_lookup")
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_templates_lookup");

		builder.HasIndex(x => new { x.PropertyId, x.TemplateKind, x.Channel, x.Culture }, "uq_notification_templates_default")
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_notification_templates_default");

		// query filter soft-delete — applicato una volta nel DbContext, non qui
	}
}
