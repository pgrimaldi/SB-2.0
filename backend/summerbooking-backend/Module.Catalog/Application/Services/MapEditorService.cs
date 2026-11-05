using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

public enum MapAxis
{
	Row,
	Column,
}

/// <summary>
/// Configurazione mappa: elementi nelle caselle del reticolo e operazioni su righe e colonne. Le prenotazioni delle
/// postazioni eliminate restano a Booking (ricollegamento per numero o cancellazione secondo l'impostazione).
/// </summary>
public sealed class MapEditorService
{
	private readonly IMapRepository _map;
	private readonly ISectorRepository _sectors;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly MapProjection _projection;
	private readonly FillMapCellsRequestValidator _fillValidator;
	private readonly UpdateMapSpotRequestValidator _updateValidator;
	private readonly PlaceMapSpotsRequestValidator _placeValidator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public MapEditorService(
		IMapRepository map, ISectorRepository sectors, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user,
		CatalogAudit audit, MapProjection projection, FillMapCellsRequestValidator fillValidator,
		UpdateMapSpotRequestValidator updateValidator, PlaceMapSpotsRequestValidator placeValidator, BulkIdsRequestValidator bulkValidator)
	{
		_map = map;
		_sectors = sectors;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_projection = projection;
		_fillValidator = fillValidator;
		_updateValidator = updateValidator;
		_placeValidator = placeValidator;
		_bulkValidator = bulkValidator;
	}

	public async Task<IReadOnlyList<MapSpotDto>> FillAsync(FillMapCellsRequest request, CancellationToken ct = default)
	{
		_fillValidator.EnsureValid(request);
		Codes.TryParse<SpotType>(request.SpotType, out var spotType);
		var (propertyId, layout, spots) = await LoadAsync(ct);

		foreach (var cell in request.Cells)
		{
			if (cell.Row < 1 || cell.Row > layout.GridRows || cell.Column < 1 || cell.Column > layout.GridColumns)
				throw DomainException.Unprocessable(CatalogErrors.MapCellOutsideGrid, CellArgs(cell));
			if (spots.Any(s => s.Covers(cell.Row, cell.Column)))
				throw DomainException.Conflict(CatalogErrors.MapCellOccupied, CellArgs(cell));
		}

		List<MapSpot> created;
		if (spotType == SpotType.BeachElement)
			created = [await BeachElementAsync(propertyId, request, ct)];
		else
		{
			long? sectorId = request.SectorId is { } sectorPublicId ? (await SectorAsync(propertyId, sectorPublicId, ct)).Id : null;
			var cells = Order(request.Cells, request.Numbering ?? new MapNumberingDto(false, true, true));
			var labels = MapLabelSequence.From(request.FirstLabel!, cells.Count);
			if (labels.Any(l => l.Length > MapSpot.LabelMaxLength))
				throw DomainException.BadRequest(ValidationErrorCodes.Invalid);
			EnsureLabelsFree(spots, labels, null);

			created = cells.Select((cell, i) => MapSpot.CreateArticle(
					propertyId, cell.Row, cell.Column, spotType, labels[i], sectorId,
					MapSpot.IsAreaType(spotType) ? request.AreaSeatCount : null, request.IsAgreementOnly, _user.UserId))
				.ToList();
		}

		_map.AddSpots(created);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapSpotsCreated, new Dictionary<string, object?>
		{
			["spotType"] = request.SpotType,
			["count"] = created.Count,
			["labels"] = created.Select(s => s.Label).Where(l => l is not null).ToList(),
		}, ct);
		return await _projection.SpotsAsync(propertyId, created, ct);
	}

	public async Task<MapSpotDto> UpdateAsync(Guid id, UpdateMapSpotRequest request, CancellationToken ct = default)
	{
		_updateValidator.EnsureValid(request);
		var (propertyId, _, spots) = await LoadAsync(ct);
		var spot = Find(spots, id);
		if (spot.IsBeachElement)
			throw DomainException.Unprocessable(CatalogErrors.MapBeachElementNotEditable);

		EnsureLabelsFree(spots, [request.Label.Trim()], spot.Id);
		long? sectorId = request.SectorId is { } sectorPublicId ? (await SectorAsync(propertyId, sectorPublicId, ct)).Id : null;
		spot.Edit(request.Label, sectorId, request.IsAgreementOnly);
		if (spot.IsArea && request.AreaSeatCount is { } seats)
			spot.SetAreaSeatCount(seats);

		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.MapSpotUpdated, AuditActionCodes.Catalog.Targets.MapSpot, spot.PublicId,
			new Dictionary<string, object?> { ["label"] = spot.Label }, ct);
		return (await _projection.SpotsAsync(propertyId, [spot], ct))[0];
	}

	/// <summary>Rotazione e spostamento nella casella: modifiche grafiche, salvate insieme e senza voce di registro.</summary>
	public async Task<BulkResult> PlaceAsync(PlaceMapSpotsRequest request, CancellationToken ct = default)
	{
		_placeValidator.EnsureValid(request);
		var (_, _, spots) = await LoadAsync(ct);
		var byId = spots.ToDictionary(s => s.PublicId);
		var succeeded = new List<Guid>();
		var failed = new List<BulkFailure>();

		foreach (var item in request.Items)
		{
			if (!byId.TryGetValue(item.Id, out var spot))
			{
				failed.Add(new BulkFailure(item.Id, CatalogErrors.MapSpotNotFound, null));
				continue;
			}
			spot.Place(item.RotationDegrees, item.OffsetX, item.OffsetY);
			succeeded.Add(item.Id);
		}

		await _unitOfWork.SaveChangesAsync(ct);
		return new BulkResult(succeeded, failed);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var (_, _, spots) = await LoadAsync(ct);
		await RemoveAsync([Find(spots, id)], ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var (_, _, spots) = await LoadAsync(ct);
		var byId = spots.ToDictionary(s => s.PublicId);
		var found = request.Ids.Distinct().Where(byId.ContainsKey).Select(i => byId[i]).ToList();

		if (found.Count > 0)
			await RemoveAsync(found, ct);
		return new BulkResult(
			found.Select(s => s.PublicId).ToList(),
			request.Ids.Distinct().Where(i => !byId.ContainsKey(i)).Select(i => new BulkFailure(i, CatalogErrors.MapSpotNotFound, null)).ToList());
	}

	/// <summary>Nuova riga o colonna in posizione <paramref name="at"/>: quelle successive scorrono di uno.</summary>
	public async Task<MapDto> InsertLineAsync(MapAxis axis, short at, CancellationToken ct = default)
	{
		var (propertyId, layout, spots) = await LoadAsync(ct);
		var count = Count(layout, axis);
		if (count >= (axis == MapAxis.Row ? MapLayout.MaxRows : MapLayout.MaxColumns))
			throw DomainException.Unprocessable(CatalogErrors.MapGridFull);
		if (at < 1 || at > count + 1)
			throw DomainException.Unprocessable(CatalogErrors.MapIndexOutsideGrid, IndexArgs(at, count + 1));

		foreach (var spot in spots)
		{
			var (start, span) = Extent(spot, axis);
			if (start >= at)
				Move(spot, axis, (short)(start + 1));
			else if (start + span - 1 >= at)
				Resize(spot, axis, (short)(span + 1));
		}
		Resize(layout, axis, (short)(count + 1));

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordGridAsync(axis, "insert", at, 0, ct);
		return await _projection.MapAsync(propertyId, layout, spots, ct);
	}

	/// <summary>Elimina la riga o colonna e il suo contenuto; quelle successive scorrono indietro di uno.</summary>
	public async Task<MapDto> DeleteLineAsync(MapAxis axis, short index, CancellationToken ct = default)
	{
		var (propertyId, layout, spots) = await LoadAsync(ct);
		var count = Count(layout, axis);
		if (index < 1 || index > count)
			throw DomainException.Unprocessable(CatalogErrors.MapIndexOutsideGrid, IndexArgs(index, count));

		var removed = new List<MapSpot>();
		foreach (var spot in spots)
		{
			var (start, span) = Extent(spot, axis);
			if (start > index)
				Move(spot, axis, (short)(start - 1));
			else if (start + span - 1 >= index)
			{
				if (span == 1)
					removed.Add(spot);
				else
					Resize(spot, axis, (short)(span - 1));
			}
		}
		Resize(layout, axis, (short)(count - 1));
		_map.RemoveSpots(removed);

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordGridAsync(axis, "delete", index, removed.Count, ct);
		return await _projection.MapAsync(propertyId, layout, spots.Except(removed).ToList(), ct);
	}

	/// <summary>Svuota la riga o colonna: la struttura resta, gli elementi che la attraversano si eliminano.</summary>
	public async Task<MapDto> ClearLineAsync(MapAxis axis, short index, CancellationToken ct = default)
	{
		var (propertyId, layout, spots) = await LoadAsync(ct);
		var count = Count(layout, axis);
		if (index < 1 || index > count)
			throw DomainException.Unprocessable(CatalogErrors.MapIndexOutsideGrid, IndexArgs(index, count));

		var removed = spots.Where(s => Extent(s, axis) is var (start, span) && start <= index && index <= start + span - 1).ToList();
		_map.RemoveSpots(removed);

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordGridAsync(axis, "clear", index, removed.Count, ct);
		return await _projection.MapAsync(propertyId, layout, spots.Except(removed).ToList(), ct);
	}

	/// <summary>«Reimposta mappa»: reticolo vuoto e a zero; sfondo, icone e impostazioni restano.</summary>
	public async Task<MapDto> ResetAsync(CancellationToken ct = default)
	{
		var (propertyId, layout, spots) = await LoadAsync(ct);
		_map.RemoveSpots(spots);
		layout.ClearGrid();

		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapReset, new Dictionary<string, object?>
		{
			["removedSpots"] = spots.Count,
			["deletesBookings"] = layout.ResetDeletesBookings,
		}, ct);
		return await _projection.MapAsync(propertyId, layout, [], ct);
	}

	private async Task<(long PropertyId, MapLayout Layout, List<MapSpot> Spots)> LoadAsync(CancellationToken ct)
	{
		var propertyId = await _property.IdAsync(ct);
		var layout = await _map.FindLayoutAsync(propertyId, ct);
		if (layout is null)
		{
			layout = MapLayout.Create(propertyId, _user.UserId);
			_map.AddLayout(layout);
		}
		return (propertyId, layout, await _map.ListSpotsAsync(propertyId, ct));
	}

	private async Task<MapSpot> BeachElementAsync(long propertyId, FillMapCellsRequest request, CancellationToken ct)
	{
		var element = await _map.FindBeachElementAsync(request.BeachElementId!.Value, ct);
		if (element is null || !element.IsActive)
			throw DomainException.NotFound(CatalogErrors.MapBeachElementNotFound);

		var top = request.Cells.Min(c => c.Row);
		var left = request.Cells.Min(c => c.Column);
		var rows = request.Cells.Max(c => c.Row) - top + 1;
		var columns = request.Cells.Max(c => c.Column) - left + 1;
		if (rows * columns != request.Cells.Count)
			throw DomainException.Unprocessable(CatalogErrors.MapSelectionNotRectangular);

		return MapSpot.CreateBeachElement(propertyId, top, left, element.Id, (short)rows, (short)columns, _user.UserId);
	}

	/// <summary>Righe da 1 = lato mare; colonne da 1 = sinistra.</summary>
	private static List<MapCellDto> Order(IReadOnlyList<MapCellDto> cells, MapNumberingDto numbering)
	{
		int Row(MapCellDto c) => numbering.SeaToInland ? c.Row : -c.Row;
		int Column(MapCellDto c) => numbering.LeftToRight ? c.Column : -c.Column;
		return numbering.Vertical
			? cells.OrderBy(Column).ThenBy(Row).ToList()
			: cells.OrderBy(Row).ThenBy(Column).ToList();
	}

	private static void EnsureLabelsFree(IEnumerable<MapSpot> spots, IReadOnlyList<string> labels, long? exceptId)
	{
		var taken = spots.Where(s => s.Label is not null && s.Id != exceptId)
			.Select(s => s.Label!.ToLowerInvariant()).ToHashSet();
		var clash = labels.FirstOrDefault(l => taken.Contains(l.ToLowerInvariant()))
			?? labels.GroupBy(l => l.ToLowerInvariant()).FirstOrDefault(g => g.Count() > 1)?.First();
		if (clash is not null)
			throw DomainException.Conflict(CatalogErrors.MapLabelTaken, new Dictionary<string, object?> { ["label"] = clash });
	}

	private static MapSpot Find(IEnumerable<MapSpot> spots, Guid id)
		=> spots.FirstOrDefault(s => s.PublicId == id) ?? throw DomainException.NotFound(CatalogErrors.MapSpotNotFound);

	private async Task<MapSector> SectorAsync(long propertyId, Guid id, CancellationToken ct)
		=> await _sectors.FindAsync(propertyId, id, ct) ?? throw DomainException.NotFound(CatalogErrors.SectorNotFound);

	private async Task RemoveAsync(IReadOnlyList<MapSpot> spots, CancellationToken ct)
	{
		_map.RemoveSpots(spots);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.MapSpotsDeleted, new Dictionary<string, object?>
		{
			["count"] = spots.Count,
			["labels"] = spots.Select(s => s.Label).Where(l => l is not null).ToList(),
		}, ct);
	}

	private static short Count(MapLayout layout, MapAxis axis) => axis == MapAxis.Row ? layout.GridRows : layout.GridColumns;

	private static (short Start, short Span) Extent(MapSpot spot, MapAxis axis)
		=> axis == MapAxis.Row ? (spot.GridRow ?? 0, spot.RowSpan) : (spot.GridColumn ?? 0, spot.ColumnSpan);

	private static void Move(MapSpot spot, MapAxis axis, short start)
	{
		if (axis == MapAxis.Row)
			spot.MoveTo(start, spot.GridColumn ?? 0);
		else
			spot.MoveTo(spot.GridRow ?? 0, start);
	}

	private static void Resize(MapSpot spot, MapAxis axis, short span)
	{
		if (axis == MapAxis.Row)
			spot.Resize(span, spot.ColumnSpan);
		else
			spot.Resize(spot.RowSpan, span);
	}

	private static void Resize(MapLayout layout, MapAxis axis, short count)
	{
		if (axis == MapAxis.Row)
			layout.SetGrid(count, layout.GridColumns, layout.TiltDegrees);
		else
			layout.SetGrid(layout.GridRows, count, layout.TiltDegrees);
	}

	private static Dictionary<string, object?> CellArgs(MapCellDto cell) => new() { ["row"] = cell.Row, ["column"] = cell.Column };

	private static Dictionary<string, object?> IndexArgs(short index, int max) => new() { ["index"] = index, ["max"] = max };

	private Task RecordGridAsync(MapAxis axis, string operation, short index, int removedSpots, CancellationToken ct)
		=> RecordAsync(AuditActionCodes.Catalog.MapGridChanged, new Dictionary<string, object?>
		{
			["axis"] = axis == MapAxis.Row ? "row" : "column",
			["operation"] = operation,
			["index"] = index,
			["removedSpots"] = removedSpots,
		}, ct);

	private Task RecordAsync(string actionCode, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Map, _property.PublicId, metadata, ct);
}
