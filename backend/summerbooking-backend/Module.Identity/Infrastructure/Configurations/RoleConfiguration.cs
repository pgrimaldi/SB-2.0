using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
	public void Configure(EntityTypeBuilder<Role> builder)
	{
		builder.ToTable("access_roles", "identity", t => t.HasComment(
			"Catalogo dei ruoli applicativi (lookup gestibile da pannello, senza deploy)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_access_roles");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();
		// seed id 1–13 forzati via HasData, poi ALTER SEQUENCE ... RESTART in migration.

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice tecnico stabile del ruolo, usato in policy/claim del token.");

		// Nessun HasDefaultValue su queste tre colonne, ed e' deliberato: `HasData` OMETTE dall'INSERT
		// le colonne il cui valore coincide col default CLR del tipo (false per bool, 0 per short),
		// perche' non distingue "impostato a false" da "non impostato". Con un default dichiarato
		// diverso dal default CLR il valore seminato evapora e vince quello del database: e' cosi' che
		// `is_staff` e' finito a true per systemadmin e consumer, che il seed dichiara false (corretto
		// il 2026-09-30). E' la stessa famiglia del tranello sulle colonne enum (HasDefaultValueSql,
		// mai HasDefaultValue). Qui un default non serve a nulla: la tabella semina tutte le proprie righe.
		builder.Property(x => x.IsAssignableViaPanel)
			.HasColumnName("is_assignable_via_panel")
			.IsRequired()
			.HasComment("Se true, un admin/superadmin può assegnare questo ruolo dal pannello gestione utenti.");

		builder.Property(x => x.IsStaff)
			.HasColumnName("is_staff")
			.IsRequired()
			.HasComment("Se true, il ruolo è operativo di stabilimento e richiede property_id sull'account.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordinamento di presentazione nei menu a tendina.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_identity_access_roles_code");

		// CreatedAt/UpdatedAt esplicite: HasData non applica i default DB (now()) alle righe seed.
		var seedTimestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		builder.HasData(
			new { Id = 1L, Code = "systemadmin", IsAssignableViaPanel = false, IsStaff = false, SortOrder = (short)0, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 2L, Code = "superadmin", IsAssignableViaPanel = false, IsStaff = true, SortOrder = (short)10, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 3L, Code = "consumer", IsAssignableViaPanel = false, IsStaff = false, SortOrder = (short)0, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 4L, Code = "admin", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)20, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 5L, Code = "lifeguard", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)30, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 6L, Code = "manager", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)40, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 7L, Code = "cashoperator", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)50, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 8L, Code = "websiteadmin", IsAssignableViaPanel = false, IsStaff = true, SortOrder = (short)60, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 9L, Code = "user1", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)70, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 10L, Code = "waiter", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)80, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 11L, Code = "affiliate", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)90, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 12L, Code = "advancedlifeguard", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)100, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			new { Id = 13L, Code = "marketing", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)110, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp },
			// Come cashoperator ma senza prenotazioni passate (decisione del 2026-10-07).
			new { Id = 14L, Code = "dailycashoperator", IsAssignableViaPanel = true, IsStaff = true, SortOrder = (short)55, CreatedAt = seedTimestamp, UpdatedAt = seedTimestamp });
	}
}
