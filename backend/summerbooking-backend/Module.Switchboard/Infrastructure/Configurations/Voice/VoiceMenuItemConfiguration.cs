using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceMenuItemConfiguration : IEntityTypeConfiguration<VoiceMenuItem>
{
	public void Configure(EntityTypeBuilder<VoiceMenuItem> builder)
	{
		builder.ToTable("switchboard_voice_menu_items", "switchboard", t => t.HasComment(
			"Voce di menù dell'IVR ad albero del centralino, nel contesto di un genitore (parent_id auto-referenziale; NULL = primo livello)."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_menu_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per l'API del pannello e i deep-link.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui appartiene la voce.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join.");

		builder.Property(x => x.ParentId)
			.HasColumnName("parent_id")
			.HasComment("Voce genitore; NULL = voce di primo livello. Il runtime naviga l'albero usando questo campo come contesto corrente.");

		builder.Property(x => x.Depth)
			.HasColumnName("depth")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Profondità del nodo (0 = radice), mantenuta coerente in applicazione a ogni spostamento.");

		builder.Property(x => x.DtmfKey)
			.HasColumnName("dtmf_key")
			.HasMaxLength(1)
			.IsFixedLength()
			.IsRequired()
			.HasComment("Tasto che il chiamante preme per raggiungere la voce nel contesto del genitore.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Etichetta della voce, mostrata nel pannello e riportata nei log di step.");

		builder.Property(x => x.AudioAssetId)
			.HasColumnName("audio_asset_id")
			.HasComment("Audio associato alla voce (foglia o intro del sotto-menù); NULL se la voce non riproduce audio.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Voce abilitata; disattivata è invisibile all'IVR (input trattato come DTMF non valido).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Ordinamento fra voci con lo stesso genitore, solo per la resa nel pannello — l'IVR seleziona per dtmf_key, non per posizione.");

		builder.ToTable(t =>
		{
			t.HasCheckConstraint("ck_switchboard_voice_menu_items_dtmf_key", "dtmf_key ~ '^[0-9*#]$'");
			t.HasCheckConstraint("ck_switchboard_voice_menu_items_depth", "depth BETWEEN 0 AND 4");
		});

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_menu_items_public_id");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.DtmfKey }, "uq_switchboard_voice_menu_items_dtmf")
			.IsUnique()
			// NULLS NOT DISTINCT: le voci di primo livello hanno parent_id NULL, e senza questo due voci
			// radice con lo stesso tasto non colliderebbero (NULL <> NULL).
			.AreNullsDistinct(false)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_voice_menu_items_dtmf");

		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.DtmfKey }, "ix_switchboard_voice_menu_items_runtime")
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_runtime");

		builder.HasIndex(x => new { x.ConfigId, x.ParentId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_tree");

		builder.HasIndex(x => x.AudioAssetId)
			.HasFilter("audio_asset_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_menu_items_audio");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.MenuItems)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_menu_items_config")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Parent)
			.WithMany(x => x.Children)
			.HasForeignKey(x => x.ParentId)
			.HasConstraintName("fk_switchboard_voice_menu_items_parent")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.AudioAsset)
			.WithMany()
			.HasForeignKey(x => x.AudioAssetId)
			.HasConstraintName("fk_switchboard_voice_menu_items_audio_asset")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
