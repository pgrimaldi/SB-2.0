using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementReport"/> su catalog.agreement_reports.</summary>
public sealed class AgreementReportConfiguration : IEntityTypeConfiguration<AgreementReport>
{
	public void Configure(EntityTypeBuilder<AgreementReport> builder)
	{
		builder.ToTable("agreement_reports", "catalog", t =>
		{
			t.HasComment(
				"Report periodico (arrivi/partenze/ricavi) che uno stabilimento invia a una struttura " +
				"partner. report_data e' una cache: il dato grezzo resta in booking.*. Il PDF archiviato " +
				"all'invio sta in notification.media_assets (kind = 'rendered_pdf').");

			// Un intervallo non puo' finire prima di cominciare. Un report di un solo giorno e' legittimo.
			t.HasCheckConstraint("ck_catalog_agreement_reports_period",
				"period_end >= period_start");

			// I totali sono conteggi e importi: mai negativi.
			t.HasCheckConstraint("ck_catalog_agreement_reports_totals",
				"total_reservations >= 0 AND total_revenue >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_reports");
		builder.Property(x => x.Id).HasColumnType("integer").UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) del report, per il pannello e per i link di invio.");

		builder.Property(x => x.PartnerId)
			.HasColumnName("partner_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Struttura partner destinataria del report.");

		builder.Property(x => x.SearchType)
			.HasColumnName("search_type")
			.HasColumnType("catalog.agreement_report_search_type")
			.IsRequired()
			.HasDefaultValueSql("'check_in'")
			.HasComment("Se l'intervallo di date filtra le prenotazioni per arrivo (check_in) o per partenza (check_out).");

		builder.Property(x => x.PeriodStart)
			.HasColumnName("period_start")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Primo giorno del periodo coperto dal report. E' un giorno, non un istante.");

		builder.Property(x => x.PeriodEnd)
			.HasColumnName("period_end")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Ultimo giorno del periodo coperto dal report, incluso.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("catalog.agreement_report_status")
			.IsRequired()
			.HasDefaultValueSql("'pending'")
			.HasComment("Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il " +
				"soft-delete della riga: nel legacy i due erano scritti sempre insieme.");

		builder.Property(x => x.ReportData)
			.HasColumnName("report_data")
			.HasColumnType("jsonb")
			.HasComment("Cache dei dati del report, NON la fonte: il dato grezzo resta in booking.* e il " +
				"documento e' ricostruibile da la'. Serve a ristampare esattamente cio' che e' stato inviato.");

		builder.Property(x => x.TotalReservations)
			.HasColumnName("total_reservations")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di prenotazioni comprese nel periodo, al momento del calcolo.");

		builder.Property(x => x.TotalRevenue)
			.HasColumnName("total_revenue")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasDefaultValue(0m)
			.HasComment("Ricavo totale del periodo, al momento del calcolo.");

		builder.Property(x => x.GeneratedByUserId)
			.HasColumnName("generated_by_user_id")
			.HasComment("Utente che ha generato il report: riferimento logico a identity.access_users.public_id, non FK (altro schema).");

		builder.Property(x => x.Notes)
			.HasColumnName("notes")
			.HasColumnType("text")
			.HasComment("Note libere di chi gestisce il report.");

		// Riferimento LOGICO verso notification.media_assets: non e' una FK di proposito, perche'
		// attraverserebbe lo schema di un altro modulo (nessun modulo vede le tabelle di un altro). Della
		// stessa natura dei riferimenti testuali di privacy_retention_policies.schema_name/table_name.
		// Costo accettato: su questa colonna non c'e' integrita' referenziale a database, e la coerenza
		// la garantisce il modulo che archivia il PDF.
		builder.Property(x => x.PdfMediaAssetId)
			.HasColumnName("pdf_media_asset_id")
			.HasComment("PDF archiviato all'invio: riferimento logico a notification.media_assets " +
				"(kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL " +
				"finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare " +
				"il vecchio oggetto in privacy.privacy_file_deletions, altrimenti si ripete il difetto del " +
				"legacy che ha lasciato 24 oggetti orfani su 28.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della riga (Global Query Filter EF standard).");

		builder.HasOne(x => x.Partner)
			.WithMany(x => x.Reports)
			.HasForeignKey(x => x.PartnerId)
			.OnDelete(DeleteBehavior.Cascade)
			.HasConstraintName("fk_catalog_agreement_reports_partner");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_agreement_reports_public_id");

		builder.HasIndex(x => x.PartnerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_reports_partner");

		// "I report di questo periodo": l'elenco che il gestore apre.
		builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_reports_period");

		// La retention del PDF (policy F5) parte da notification.media_assets, ma da qui serve la
		// risalita inversa: "quale report nominava questo asset", per azzerare il riferimento quando
		// l'oggetto viene cancellato a 24 mesi.
		builder.HasIndex(x => x.PdfMediaAssetId)
			.HasFilter("pdf_media_asset_id IS NOT NULL")
			.HasDatabaseName("ix_catalog_agreement_reports_pdf_media_asset");
	}
}
