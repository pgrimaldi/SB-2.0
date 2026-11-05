using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Npgsql;
using NpgsqlTypes;

namespace Module.Catalog.Infrastructure.Repositories;

/// <summary>
/// Ricalcolo della proiezione di ricerca, in SQL e per i soli stabilimenti toccati. Pubblicata = vetrina con data di
/// pubblicazione, stabilimento approvato o importato, fuori dal cestino. Il prezzo «da» è quello della home: oggi se la
/// stagione è in corso, altrimenti il più basso della stagione in arrivo (o della successiva, finita quella corrente).
/// </summary>
public sealed class SearchProjection : ISearchProjection
{
	private readonly DbContext _db;

	public SearchProjection(DbContext db) => _db = db;

	public async Task<int> RefreshAsync(SearchScope scope, CancellationToken ct = default)
	{
		if (scope.IsEmpty)
			return 0;
		var parameters = new object[]
		{
			new NpgsqlParameter("all", scope.IsAll),
			Ids("properties", scope.PropertyIds),
			Ids("bathhouses", scope.BathhouseIds),
			Ids("price_lists", scope.PriceListIds),
			Ids("seasons", scope.SeasonIds),
		};
		await _db.Database.ExecuteSqlRawAsync(DeleteSql, parameters, ct);
		return await _db.Database.ExecuteSqlRawAsync(UpsertSql, parameters.Select(Clone).ToArray(), ct);
	}

	private static NpgsqlParameter Ids(string name, IReadOnlyCollection<long> ids)
		=> new(name, NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = ids.ToArray() };

	private static object Clone(object p) => ((NpgsqlParameter)p).Clone();

	private const string Targets = """
		targets AS (
		    SELECT unnest(@properties) AS property_id
		    UNION SELECT property_id FROM catalog.bathhouses WHERE id = ANY(@bathhouses)
		    UNION SELECT property_id FROM catalog.pricing_price_lists WHERE id = ANY(@price_lists)
		    UNION SELECT property_id FROM catalog.pricing_seasons WHERE id = ANY(@seasons))
		""";

	private const string DeleteSql = "WITH " + Targets + """

		DELETE FROM catalog.search_bathhouses s
		WHERE (@all OR s.property_id IN (SELECT property_id FROM targets))
		  AND NOT EXISTS (SELECT 1 FROM catalog.bathhouses b WHERE b.public_id = s.bathhouse_public_id AND b.deleted_at IS NULL)
		""";

	private const string UpsertSql = "WITH " + Targets + """
		,
		today AS (SELECT (now() AT TIME ZONE 'Europe/Rome')::date AS d),
		source AS (
		    SELECT b.*, p.status, p.deleted_at AS property_deleted_at, p.purged_at, p.subscribed_since, p.name AS property_name,
		           p.is_online_sales_suspended, p.is_subscription_active
		    FROM catalog.bathhouses b
		    JOIN catalog.properties p ON p.id = b.property_id
		    WHERE b.deleted_at IS NULL AND (@all OR b.property_id IN (SELECT property_id FROM targets))),
		online_packages AS (
		    SELECT pl.id, pl.property_id FROM catalog.pricing_price_lists pl
		    WHERE pl.kind = 'package' AND pl.sales_channel <> 'manager_only' AND pl.deleted_at IS NULL
		      AND pl.property_id IN (SELECT property_id FROM source)),
		price_range AS (
		    SELECT s.property_id,
		           CASE WHEN t.d BETWEEN s.season_starts_on AND s.season_ends_on THEN t.d
		                WHEN t.d < s.season_starts_on THEN s.season_starts_on
		                ELSE s.next_season_starts_on END AS from_d,
		           CASE WHEN t.d BETWEEN s.season_starts_on AND s.season_ends_on THEN t.d
		                WHEN t.d < s.season_starts_on THEN s.season_ends_on
		                ELSE s.next_season_ends_on END AS to_d
		    FROM catalog.pricing_seasons s CROSS JOIN today t
		    WHERE s.deleted_at IS NULL AND s.property_id IN (SELECT DISTINCT property_id FROM online_packages)),
		day_groups AS (
		    SELECT r.property_id, (
		        SELECT tp.period_group_key FROM catalog.pricing_tariff_periods tp
		        JOIN catalog.pricing_seasons ps ON ps.id = tp.season_id
		        WHERE ps.property_id = r.property_id AND tp.deleted_at IS NULL AND tp.starts_on <= d.day AND d.day <= tp.ends_on
		        ORDER BY tp.ends_on - tp.starts_on, tp.id LIMIT 1) AS period_group
		    FROM price_range r
		    CROSS JOIN LATERAL generate_series(r.from_d, r.to_d, interval '1 day') AS d(day)
		    WHERE r.from_d IS NOT NULL AND r.to_d >= r.from_d),
		min_prices AS (
		    SELECT g.property_id, min(coalesce(pp.half_day_price, pp.daily_price)) AS price
		    FROM (SELECT DISTINCT property_id, period_group FROM day_groups WHERE period_group IS NOT NULL) g
		    JOIN online_packages op ON op.property_id = g.property_id
		    JOIN catalog.pricing_price_list_period_prices pp ON pp.price_list_id = op.id AND pp.tariff_period_group = g.period_group AND pp.deleted_at IS NULL
		    WHERE coalesce(pp.half_day_price, pp.daily_price) > 0
		    GROUP BY g.property_id)
		INSERT INTO catalog.search_bathhouses AS sb
		    (bathhouse_public_id, property_id, name, city, region, province, slug, latitude, longitude, min_price, amenity_ids, like_count,
		     is_published, card, content_hash, built_at, region_id, subscribed_since, is_online_bookable, is_contact_only, created_at, updated_at)
		SELECT s.public_id, s.property_id, coalesce(nullif(trim(s.name), ''), s.property_name), s.city, s.region, s.province, s.slug,
		       s.latitude::float8, s.longitude::float8, mp.price,
		       coalesce((SELECT array_agg(a.amenity_id::smallint ORDER BY a.amenity_id) FROM catalog.bathhouse_amenities a
		                 WHERE a.bathhouse_id = s.id AND a.deleted_at IS NULL), ARRAY[]::smallint[]),
		       s.like_count,
		       s.published_at IS NOT NULL AND s.status IN ('approved', 'imported') AND s.property_deleted_at IS NULL AND s.purged_at IS NULL,
		       jsonb_build_object(
		           'mainPhotoAssetId', s.main_photo_id,
		           'description', left(s.description, 300),
		           'photoCount', (SELECT count(*) FROM catalog.bathhouse_photos ph WHERE ph.bathhouse_id = s.id AND ph.deleted_at IS NULL),
		           'badgeIds', coalesce((SELECT jsonb_agg(bg.public_id ORDER BY bg.sort_order, bg.name) FROM catalog.bathhouse_badges bb
		                                 JOIN catalog.showcase_badges bg ON bg.id = bb.badge_id AND bg.deleted_at IS NULL
		                                 WHERE bb.bathhouse_id = s.id AND bb.deleted_at IS NULL), '[]'::jsonb)),
		       '\x'::bytea, now(), s.region_id, s.subscribed_since,
		       s.status = 'approved' AND NOT s.is_online_sales_suspended AND s.is_subscription_active
		           AND EXISTS (SELECT 1 FROM catalog.property_online_booking_settings o WHERE o.property_id = s.property_id AND o.is_enabled AND o.deleted_at IS NULL)
		           AND EXISTS (SELECT 1 FROM online_packages op WHERE op.property_id = s.property_id),
		       s.status = 'imported', now(), now()
		FROM source s
		LEFT JOIN min_prices mp ON mp.property_id = s.property_id
		ON CONFLICT (bathhouse_public_id) DO UPDATE SET
		    property_id = EXCLUDED.property_id, name = EXCLUDED.name, city = EXCLUDED.city, region = EXCLUDED.region,
		    province = EXCLUDED.province, slug = EXCLUDED.slug, latitude = EXCLUDED.latitude, longitude = EXCLUDED.longitude,
		    min_price = EXCLUDED.min_price, amenity_ids = EXCLUDED.amenity_ids, like_count = EXCLUDED.like_count,
		    is_published = EXCLUDED.is_published, card = EXCLUDED.card, built_at = EXCLUDED.built_at, region_id = EXCLUDED.region_id,
		    subscribed_since = EXCLUDED.subscribed_since, is_online_bookable = EXCLUDED.is_online_bookable,
		    is_contact_only = EXCLUDED.is_contact_only, updated_at = now(), deleted_at = NULL
		""";
}
