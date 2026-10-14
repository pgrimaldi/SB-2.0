using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class BathhouseLikeConfiguration : IEntityTypeConfiguration<BathhouseLike>
{
	public void Configure(EntityTypeBuilder<BathhouseLike> builder)
	{
		builder.ToTable("customer_bathhouse_likes", "identity", t => t.HasComment(
			"Registro dei \"mi piace\" delle strutture (bathhouse) da parte degli utenti consumer."));

		builder.HasKey(x => x.Id).HasName("pk_identity_customer_bathhouse_likes");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("Utente consumer che ha messo mi piace.");

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("public_id del bathhouse (catalog) a cui è stato messo mi piace, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.LikedAt)
			.HasColumnName("liked_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando è stato messo il mi piace (aggiornato se il like viene riattivato).");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => new { x.UserId, x.BathhouseId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_customer_bathhouse_likes_user_bathhouse");

		builder.HasIndex(x => x.UserId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_bathhouse_likes_user");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_identity_customer_bathhouse_likes_bathhouse");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.HasConstraintName("fk_identity_customer_bathhouse_likes_user")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
