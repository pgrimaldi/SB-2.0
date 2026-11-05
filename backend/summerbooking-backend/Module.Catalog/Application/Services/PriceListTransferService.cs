using System.Globalization;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Esportazione e importazione dei prezzi del listino in CSV/XLSX, con il modello da compilare. Una riga per pacchetto e
/// fila: <c>row = period</c> con le tariffe di un periodo, <c>row = seasonal</c> con il prezzo stagionale. I nomi di fila e
/// periodo sono quelli dello stabilimento; l'id lega la riga a un listino esistente, senza id se ne crea uno nuovo.
/// </summary>
public sealed class PriceListTransferService
{
	public static readonly IReadOnlyList<string> Headers =
	[
		"id", "kind", "name", "row", "sector", "period", "half_day", "half_day_weekend", "daily", "weekend", "half_day_7", "days_7",
		"half_day_14", "days_14", "half_day_monthly", "monthly", "seasonal", "seasonal_half_day",
	];
	private const string PeriodRow = "period";
	private const string SeasonalRow = "seasonal";
	private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

	private readonly IPriceListRepository _repository;
	private readonly ISectorRepository _sectors;
	private readonly ISeasonRepository _seasons;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly ITabularFiles _files;

	public PriceListTransferService(
		IPriceListRepository repository, ISectorRepository sectors, ISeasonRepository seasons, IUnitOfWork unitOfWork, CurrentProperty property,
		ICurrentUser user, CatalogAudit audit, ITabularFiles files)
	{
		_repository = repository;
		_sectors = sectors;
		_seasons = seasons;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_files = files;
	}

	public async Task<TabularFile> ExportAsync(TabularFormat format, CancellationToken ct = default)
	{
		var propertyId = await _property.IdAsync(ct);
		var (sectorNames, periodNames) = await NamesAsync(propertyId, ct);
		var rows = new List<IReadOnlyList<string?>>();
		foreach (var p in await _repository.ListFullAsync(propertyId, ct))
		{
			foreach (var s in p.SectorPrices.Where(s => sectorNames.ContainsKey(s.SectorId)))
				rows.Add([p.PublicId.ToString(), Codes.Of(p.Kind), p.Name, SeasonalRow, sectorNames[s.SectorId], null,
					null, null, null, null, null, null, null, null, null, null, Format(s.BasePrice), Format(s.HalfDayPrice)]);
			foreach (var x in p.PeriodPrices.OrderBy(x => x.SectorId).ThenBy(x => x.TariffPeriodGroup))
				rows.Add([p.PublicId.ToString(), Codes.Of(p.Kind), p.Name, PeriodRow,
					x.SectorId is { } sid ? sectorNames.GetValueOrDefault(sid) : null,
					x.TariffPeriodGroup is { } key ? periodNames.GetValueOrDefault(key) : null,
					Format(x.HalfDayPrice), Format(x.HalfDayWeekendPrice), Format(x.DailyPrice), Format(x.WeekendPrice), Format(x.HalfDay7Price),
					Format(x.Days7Price), Format(x.HalfDay14Price), Format(x.Days14Price), Format(x.HalfDayMonthlyPrice), Format(x.MonthlyPrice), null, null]);
		}
		return _files.Write("listino", "Listino", Headers, rows, format);
	}

	/// <summary>Modello: una riga stagionale e una per periodo per ogni fila, prezzi vuoti.</summary>
	public async Task<TabularFile> TemplateAsync(TabularFormat format, CancellationToken ct = default)
	{
		var (sectorNames, periodNames) = await NamesAsync(await _property.IdAsync(ct), ct);
		var rows = new List<IReadOnlyList<string?>>();
		foreach (var sector in sectorNames.Values.DefaultIfEmpty(null))
		{
			rows.Add([null, Codes.Of(PriceListKind.Package), "Ombrellone e 2 lettini", SeasonalRow, sector, null, .. Enumerable.Repeat<string?>(null, 12)]);
			foreach (var period in periodNames.Values.DefaultIfEmpty(null))
				rows.Add([null, Codes.Of(PriceListKind.Package), "Ombrellone e 2 lettini", PeriodRow, sector, period, .. Enumerable.Repeat<string?>(null, 12)]);
		}
		return _files.Write("modello-listino", "Listino", Headers, rows, format);
	}

	public async Task<PriceListImportResult> ImportAsync(UploadedFile file, CancellationToken ct = default)
	{
		var format = file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ? TabularFormat.Xlsx : TabularFormat.Csv;
		IReadOnlyList<IReadOnlyList<string>> table;
		try
		{
			table = _files.Read(file.Content, format);
		}
		catch (Exception e) when (e is InvalidDataException or FormatException or IOException)
		{
			throw DomainException.Unprocessable(CatalogErrors.ImportFileInvalid);
		}
		if (table.Count == 0 || !table[0].Take(Headers.Count).Select(h => h.ToLowerInvariant()).SequenceEqual(Headers))
			throw DomainException.Unprocessable(CatalogErrors.ImportFileInvalid, new Dictionary<string, object?> { ["headers"] = Headers });

		var propertyId = await _property.IdAsync(ct);
		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.Name.Trim().ToLowerInvariant(), s => s.Id);
		var season = await _seasons.FindAsync(propertyId, ct);
		var periods = (season?.TariffPeriods ?? []).GroupBy(p => p.Name.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First().PeriodGroupKey);
		var existing = (await _repository.ListFullAsync(propertyId, ct)).ToList();

		var errors = new List<ImportRowError>();
		var parsed = new List<(int Row, string Key, PriceListKind Kind, string Name, string Type, long? Sector, short? Period, decimal?[] Prices, PriceList? Target)>();
		for (var i = 1; i < table.Count; i++)
		{
			var c = table[i].Concat(Enumerable.Repeat(string.Empty, Headers.Count)).Take(Headers.Count).ToArray();
			var rowNumber = i + 1;
			PriceList? target = null;
			if (c[0].Length > 0)
			{
				target = Guid.TryParse(c[0], out var id) ? existing.FirstOrDefault(p => p.PublicId == id) : null;
				if (target is null)
				{
					errors.Add(new ImportRowError(rowNumber, CatalogErrors.PriceListNotFound, new Dictionary<string, object?> { ["id"] = c[0] }));
					continue;
				}
			}
			if (!Codes.TryParse<PriceListKind>(c[1].ToLowerInvariant(), out var kind) || c[2].Length == 0 || c[3] is not (PeriodRow or SeasonalRow))
			{
				errors.Add(new ImportRowError(rowNumber, ValidationErrorCodes.Invalid, null));
				continue;
			}
			long? sector = null;
			if (c[4].Length > 0)
			{
				if (!sectors.TryGetValue(c[4].ToLowerInvariant(), out var s))
				{
					errors.Add(new ImportRowError(rowNumber, CatalogErrors.SectorNotFound, new Dictionary<string, object?> { ["sector"] = c[4] }));
					continue;
				}
				sector = s;
			}
			short? period = null;
			if (c[5].Length > 0)
			{
				if (!periods.TryGetValue(c[5].ToLowerInvariant(), out var p))
				{
					errors.Add(new ImportRowError(rowNumber, CatalogErrors.PeriodNotFound, new Dictionary<string, object?> { ["period"] = c[5] }));
					continue;
				}
				period = p;
			}
			if (c[3] == SeasonalRow && sector is null)
			{
				errors.Add(new ImportRowError(rowNumber, CatalogErrors.SectorNotFound, null));
				continue;
			}
			var prices = new decimal?[12];
			var invalid = false;
			for (var k = 0; k < 12; k++)
			{
				if (c[6 + k].Length == 0)
					continue;
				if (decimal.TryParse(c[6 + k].Replace(" ", "").Replace("€", ""), NumberStyles.Number, Italian, out var value) && value >= 0
					|| decimal.TryParse(c[6 + k], NumberStyles.Number, CultureInfo.InvariantCulture, out value) && value >= 0)
					prices[k] = decimal.Round(value, 2);
				else
					invalid = true;
			}
			if (invalid)
			{
				errors.Add(new ImportRowError(rowNumber, CatalogErrors.ImportPriceInvalid, null));
				continue;
			}
			var key = target?.PublicId.ToString() ?? $"{Codes.Of(kind)}|{c[2].ToLowerInvariant()}";
			parsed.Add((rowNumber, key, kind, c[2], c[3], sector, period, prices, target));
		}

		var created = 0;
		var updated = 0;
		foreach (var group in parsed.GroupBy(p => p.Key))
		{
			var first = group.First();
			var priceList = first.Target
				?? existing.FirstOrDefault(p => p.Kind == first.Kind && string.Equals(p.Name, first.Name, StringComparison.OrdinalIgnoreCase));
			if (priceList is null)
			{
				priceList = PriceList.Create(propertyId, first.Kind, _user.UserId);
				priceList.Update(new PriceListData(first.Name, null, null, null, null, null, SalesChannel.ManagerOnly, false, false, false,
					VatRules.DefaultRate, null, false, false));
				_repository.Add(priceList);
				existing.Add(priceList);
				created++;
			}
			else
				updated++;

			var sectorPrices = priceList.SectorPrices.ToDictionary(s => s.SectorId, s => (s.BasePrice, s.HalfDayPrice));
			foreach (var row in group.Where(r => r.Type == SeasonalRow))
				sectorPrices[row.Sector!.Value] = (row.Prices[10], row.Prices[11]);
			priceList.SetSectorPrices(sectorPrices);

			var periodPrices = priceList.PeriodPrices.ToDictionary(x => (x.SectorId, x.TariffPeriodGroup), x => new PeriodPriceData(
				x.SectorId, x.TariffPeriodGroup, x.HalfDayPrice, x.HalfDayWeekendPrice, x.DailyPrice, x.WeekendPrice, x.HalfDay7Price, x.Days7Price,
				x.HalfDay14Price, x.Days14Price, x.HalfDayMonthlyPrice, x.MonthlyPrice, x.StayPriceOverrides.ToDictionary(o => o.StayDays, o => o.Price)));
			foreach (var row in group.Where(r => r.Type == PeriodRow))
			{
				var stays = periodPrices.TryGetValue((row.Sector, row.Period), out var old) ? old.StayOverrides : new Dictionary<short, decimal>();
				var v = row.Prices;
				periodPrices[(row.Sector, row.Period)] = new PeriodPriceData(row.Sector, row.Period, v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7], v[8], v[9], stays);
			}
			priceList.SetPeriodPrices(periodPrices.Values.ToList());
		}

		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.PriceListsImported, AuditActionCodes.Catalog.Targets.Property, _property.PublicId,
			new Dictionary<string, object?> { ["created"] = created, ["updated"] = updated, ["errors"] = errors.Count }, ct);
		return new PriceListImportResult(created, updated, errors);
	}

	private async Task<(IReadOnlyDictionary<long, string> Sectors, IReadOnlyDictionary<short, string> Periods)> NamesAsync(long propertyId, CancellationToken ct)
	{
		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.Id, s => s.Name);
		var season = await _seasons.FindAsync(propertyId, ct);
		var periods = (season?.TariffPeriods ?? []).GroupBy(p => p.PeriodGroupKey).ToDictionary(g => g.Key, g => g.First().Name);
		return (sectors, periods);
	}

	private static string? Format(decimal? value) => value?.ToString("0.00", Italian);
}
