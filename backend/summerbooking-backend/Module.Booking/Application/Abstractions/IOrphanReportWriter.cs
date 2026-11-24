using Module.Booking.Application.Dtos;
using Shared.Dtos;
using Shared.Localization;

namespace Module.Booking.Application.Abstractions;

/// <summary>PDF scaricabile del rapporto delle prenotazioni rimaste senza postazione.</summary>
public interface IOrphanReportWriter
{
	TabularFile Write(IReadOnlyList<OrphanLineDto> rows, string propertyName, AppLanguage language, DateTime nowUtc);
}
