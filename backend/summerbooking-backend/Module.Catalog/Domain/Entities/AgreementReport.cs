using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Report periodico (arrivi/partenze/ricavi) che uno stabilimento invia a una struttura partner.
/// Entità figlia di <see cref="AgreementPartner"/>. Rimpiazza la legacy <c>hotel_reports</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Due difetti del legacy che questa tabella non riproduce</b>:
/// </para>
/// <para>
/// 1. il legacy teneva una colonna <c>path</c> sola, sovrascritta a ogni rigenerazione del PDF e senza
/// cancellare il file precedente: è così che 4 report hanno prodotto 28 oggetti su S3, 24 dei quali
/// versioni superate che nessuna riga nominava più. Qui il riferimento al PDF è
/// <see cref="PdfMediaAssetId"/> e la sostituzione deve passare dalla coda
/// <c>privacy.privacy_file_deletions</c>, che rimuove l'oggetto vecchio.
/// </para>
/// <para>
/// 2. il legacy archiviava anche la semplice <b>anteprima</b>: l'endpoint "scarica il PDF" chiamava la
/// stessa funzione dell'invio e caricava su S3. Si archivia <b>all'invio</b>, non quando qualcuno
/// guarda il documento — un solo report, nel legacy, ha lasciato 11 oggetti che nessuno ha mai
/// mandato a nessuno.
/// </para>
/// <para>
/// <b>Dati migrati: nessuno.</b> Le 2 righe di produzione hanno <c>path</c> NULL e nessun PDF, e i 28
/// file sul bucket appartengono all'ambiente di sviluppo. La tabella nasce vuota.
/// </para>
/// </remarks>
public sealed class AgreementReport : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PartnerId { get; private set; }
	public AgreementPartner? Partner { get; private set; }

	/// <summary>Se <see cref="PeriodStart"/>/<see cref="PeriodEnd"/> filtrano per arrivo o per partenza.</summary>
	public AgreementReportSearchType SearchType { get; private set; }

	public DateOnly PeriodStart { get; private set; }
	public DateOnly PeriodEnd { get; private set; }
	public AgreementReportStatus Status { get; private set; }

	/// <summary>
	/// Cache dei dati del report, non la fonte: il dato grezzo resta in <c>booking.*</c> e il documento
	/// è ricostruibile da lì. Conservarla serve a ristampare esattamente ciò che è stato inviato.
	/// </summary>
	public string? ReportData { get; private set; }

	public int TotalReservations { get; private set; }
	public decimal TotalRevenue { get; private set; }

	/// <summary>Utente che ha generato il report (riferimento logico a identity, non FK: altro schema).</summary>
	public Guid? GeneratedByUserId { get; private set; }

	public string? Notes { get; private set; }

	/// <summary>
	/// PDF archiviato all'invio: riferimento <b>logico</b> a <c>notification.media_assets</c>
	/// (<c>kind = 'rendered_pdf'</c>), deliberatamente <b>non</b> una FK perché attraversa uno schema di
	/// un altro modulo. NULL finché il report non è stato inviato.
	/// </summary>
	public long? PdfMediaAssetId { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AgreementReport() { }
}
