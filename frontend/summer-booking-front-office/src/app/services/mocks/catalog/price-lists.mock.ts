import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { AdditionalItemAddItemRequest } from '../../../entities/settings/catalog/additional-item-add-item-request';
import { AdditionalItemSelectedItem } from '../../../entities/settings/catalog/additional-item-selected-item';
import { PriceCalculationMethod } from '../../../entities/enums/price-calculation-method';
import { PriceMonthlySetting } from '../../../entities/enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../../entities/enums/price-weekend-setting';
import { InventoryArticle } from '../../../entities/enums/inventory-article';
import { PackageArticle } from '../../../entities/settings/catalog/package-article';
import {
  MAX_PACKAGE_INCLUDED_SERVICES_LENGTH,
  MAX_PRICE_LIST_PRICE,
  PackageAddItemRequest,
} from '../../../entities/settings/catalog/package-add-item-request';
import {
  MAX_INTERMEDIATE_PRICE_DAY_QUANTITY,
  PackageIntermediatePrice,
} from '../../../entities/settings/catalog/package-intermediate-price';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { PackagePeriodPrice } from '../../../entities/settings/catalog/package-period-price';
import { MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH } from '../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSectorPrice } from '../../../entities/settings/catalog/package-sector-price';
import {
  MAX_PACKAGE_NAME_LENGTH,
  PackageTranslation,
} from '../../../entities/settings/catalog/package-translation';
import { PackageSelectedItem } from '../../../entities/settings/catalog/package-selected-item';
import { Language } from '../../../entities/shared/language';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem, resourceNotFound } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import {
  activeArticleIdNamed,
  activeArticleName,
  activeArticlesOfCode,
} from '../warehouse/warehouse.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';
import { DELETED_SECTOR } from './sectors-data.mock';
import {
  VAT_RATES,
  isAreaOfProperty,
  isPeriodOfSeason,
  isSectorOfProperty,
  missingSector,
  vatRateNotAllowed,
} from './service-checks.mock';
import { copyName, languageOf, naturalOrder, tableRequestErrors } from './service-tables.mock';

type PackageRow = PackageItem & { propertyId: number };

/** The backend's ranges (`SavePriceListRequestValidator`): wider than the frontend's lists. */
const PEOPLE_QUANTITY_RANGE = { from: 1, to: 100 };
const ADDITIONAL_ITEM_QUANTITY_RANGE = { from: 0, to: 100 };
const MAX_ARTICLE_QUANTITY = 100;
const MAX_ITEM_QUANTITY = 50;
const PACKAGE_NAMES = [
  'Ombrellone e 2 lettini',
  'Ombrellone, sdraio e lettino',
  'Ombrellone e lettino',
  'Ombrellone e 2 sdraio',
  'Ombrellone e 3 lettini',
  'Ombrellone famiglia e 4 lettini',
  'Gazebo e 2 lettini',
  'Gazebo e 4 lettini',
  'Tenda e 2 sdraio',
  'Lettino singolo',
  'Prima fila e 2 lettini',
];

const PACKAGES: PackageRow[] = PACKAGE_NAMES.map((packageName, index) => ({
  propertyId: 1,
  idPackage: `8d3f1b20-6a4c-4b7e-9f10-2e3d4c5b6a${String(index + 1).padStart(2, '0')}`,
  packageName,
}));

const DELETED_PERIOD = 9;
const DELETED_AREA = '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6aff';
const VAT_RATE_NO_LONGER_ALLOWED = 20;

/** What the panels saved, by idPackage; a package never saved has only its name. */
const PACKAGE_DETAILS = new Map<string, PackageAddItemRequest>([
  [
    PACKAGES[0].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [
        { language: 'it', packageName: PACKAGES[0].packageName },
        { language: 'en', packageName: 'Umbrella and 2 sunbeds' },
      ],
      maxAdditionalItemQuantity: 2,
      maxPeopleQuantity: 4,
      items: [
        { idInventoryItem: activeArticleIdNamed(1, 'Ombrellone'), quantity: 1 },
        { idInventoryItem: activeArticleIdNamed(1, 'Lettino'), quantity: 2 },
        { idInventoryItem: activeArticleIdNamed(1, 'Tavolino'), quantity: 1 },
      ],
      vatRate: 22,
      includedServices: 'Cassetta di sicurezza',
      idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01', '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a02'],
      isDefault: true,
      isOnlineEnabled: true,
      reducedSeasonalPrices: [
        {
          startsOn: '2026-06-01T00:00:00.000Z',
          endsOn: '2026-06-30T00:00:00.000Z',
          reducedSeasonalPriceName: 'Giugno',
          sectorPrices: [
            {
              idSector: '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01',
              seasonalHalfDayPrice: 450,
              seasonalPrice: 800,
            },
          ],
        },
        {
          startsOn: '2026-09-01T00:00:00.000Z',
          endsOn: '2026-09-15T00:00:00.000Z',
          reducedSeasonalPriceName: 'Inizio settembre',
          sectorPrices: [],
        },
      ],
      sectorPrices: [
        {
          idSector: '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01',
          seasonalHalfDayPrice: 600,
          seasonalPrice: 1100,
        },
      ],
      calculationMethod: PriceCalculationMethod.Daily,
      monthlySetting: PriceMonthlySetting.EffectiveMonthDuration,
      weekendSetting: PriceWeekendSetting.SaturdaySunday,
      isPublicHolidaysIncluded: true,
      publicHolidayDates: ['2026-06-02T00:00:00.000Z', '2026-08-15T00:00:00.000Z'],
      isIntermediatePricesIncluded: true,
      intermediatePrices: [
        {
          idPeriod: 0,
          isFirstRow: true,
          dayQuantity: 2,
          price: 35,
        },
        {
          idPeriod: 0,
          isFirstRow: true,
          dayQuantity: 3,
          price: 50,
        },
      ],
    }),
  ],
  [
    PACKAGES[8].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: PACKAGES[8].packageName }],
      vatRate: VAT_RATE_NO_LONGER_ALLOWED,
    }),
  ],
  [
    PACKAGES[7].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: PACKAGES[7].packageName }],
      maxPeopleQuantity: 40,
      idSectorAreas: ['9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01', DELETED_AREA],
    }),
  ],
  [
    PACKAGES[9].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: PACKAGES[9].packageName }],
      periodPrices: [
        Object.assign(new PackagePeriodPrice(), {
          idSector: DELETED_SECTOR,
          idPeriod: 0,
          dailyPrice: 12,
        }),
      ],
      idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01', DELETED_SECTOR],
    }),
  ],
  [
    PACKAGES[10].idPackage ?? '',
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: PACKAGES[10].packageName }],
      periodPrices: [
        Object.assign(new PackagePeriodPrice(), {
          idSector: '5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01',
          idPeriod: DELETED_PERIOD,
          dailyPrice: 40,
        }),
      ],
    }),
  ],
]);

/**
 * Names of the packages and additional items with a price for the tariff period `key` of the current
 * season, as the backend lists the price lists that use a period.
 */
export function priceListsUsingPeriod(key: number): string[] {
  const hasPrice = (price: PackagePeriodPrice): boolean =>
    price.idPeriod === key &&
    Object.entries(price).some(
      ([field, value]) => field !== 'idSector' && field !== 'idPeriod' && typeof value === 'number',
    );
  const uses = (
    details: PackageAddItemRequest | AdditionalItemAddItemRequest | undefined,
  ): boolean =>
    !!details &&
    (details.periodPrices.some(hasPrice) ||
      ('intermediatePrices' in details &&
        details.intermediatePrices.some(({ idPeriod }) => idPeriod === key)));
  return [
    ...PACKAGES.filter(({ idPackage }) => uses(PACKAGE_DETAILS.get(idPackage ?? ''))).map(
      ({ packageName }) => packageName ?? '',
    ),
    ...ADDITIONAL_ITEMS.filter(({ idAdditionalItem }) =>
      uses(ADDITIONAL_ITEM_DETAILS.get(idAdditionalItem ?? '')),
    ).map(({ additionalItemName }) => additionalItemName ?? ''),
  ];
}

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = ['packageName'] as const;

/**
 * `POST /api/catalog/price-lists/packages-list`: one page of the packages of the property of the
 * token, each with its name in the language of `Accept-Language` (see `packageNameIn`), which the
 * search and the natural order use too. 401 without a valid access token; 400 as `serviceListAnswer`.
 */
export const packageListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body, SORT_FIELDS);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = DEFAULT_PAGE_SIZE } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  const found = packagesOfProperty()
    .map((row) => ({
      ...onlyFieldsOf(PackageItem, row),
      packageName: packageNameIn(row, languageOf(request)),
    }))
    .filter(({ packageName }) => !search || packageName?.trim().toLowerCase().includes(search))
    .sort((first, second) => sign * naturalOrder(first.packageName, second.packageName));
  const answer: Page<PackageItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/price-lists/delete-package-item`: deletes the given packages, all or nothing
 * (see `withPackages`); they leave the additional items they are linked to. 409
 * `pricing.price_list_in_use` when one of these would be left with no packages, which means all.
 */
export const deletePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withPackages(request, (packages) => {
    const used = packages
      .map((item) => ({ item, additionalItems: onlyWith(item, packages) }))
      .find(({ additionalItems }) => additionalItems.length);
    if (used) {
      return problem(request, 409, ApiErrorCode.PricingPriceListInUse, {
        args: {
          priceListName: packageNameIn(used.item, languageOf(request)) ?? '',
          additionalItems: used.additionalItems,
        },
      });
    }
    for (const { idPackage } of packages) {
      PACKAGES.splice(
        PACKAGES.findIndex((row) => row.idPackage === idPackage),
        1,
      );
      PACKAGE_DETAILS.delete(idPackage ?? '');
      for (const [idAdditionalItem, details] of ADDITIONAL_ITEM_DETAILS) {
        ADDITIONAL_ITEM_DETAILS.set(idAdditionalItem, {
          ...details,
          idPackages: details.idPackages.filter((id) => id !== idPackage),
        });
      }
    }
    return null;
  });

/**
 * `POST /api/catalog/price-lists/duplicate-package-item`: one copy of each given package, all or
 * nothing (see `withPackages`), with every data of the original, never the default one, and the first
 * free name in each language ("X copia", then "X copia 2"). 422 `pricing.duplicate_period_not_found`
 * or `pricing.duplicate_sector_not_found` for a package with prices for a deleted period or data of
 * a deleted sector.
 */
export const duplicatePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withPackages(request, (packages) => {
    for (const item of packages) {
      const code = copyError(PACKAGE_DETAILS.get(item.idPackage ?? ''));
      if (code) {
        return problem(request, 422, code, {
          args: {
            priceListName: packageNameIn(item, languageOf(request)) ?? '',
            rates: VAT_RATES,
          },
        });
      }
    }
    for (const item of packages) {
      const copy: PackageRow = {
        propertyId: item.propertyId,
        idPackage: crypto.randomUUID(),
        packageName: copyName(
          item.packageName ?? '',
          Language.It,
          packagesOfProperty().map(({ packageName }) => packageName),
        ),
      };
      const details = PACKAGE_DETAILS.get(item.idPackage ?? '');
      if (details) {
        PACKAGE_DETAILS.set(copy.idPackage ?? '', {
          ...structuredClone(details),
          translations: details.translations.map((translation) =>
            copiedTranslation(translation, copy.packageName),
          ),
          isDefault: false,
          isDefaultOnline: false,
        });
      }
      PACKAGES.push(copy);
    }
    return null;
  });

function copyError(details: PackageAddItemRequest | undefined): ApiErrorCode | null {
  if (details?.vatRate != null && !VAT_RATES.includes(details.vatRate)) {
    return ApiErrorCode.PricingDuplicateVatRateNotAllowed;
  }
  if (details?.idSectorAreas.some((id) => !isAreaOfProperty(id))) {
    return ApiErrorCode.PricingDuplicateAreaNotFound;
  }
  if (details?.periodPrices.some(({ idPeriod }) => !isPeriodOfSeason(idPeriod))) {
    return ApiErrorCode.PricingDuplicatePeriodNotFound;
  }
  return details && !sectorsOf(details).every(isSectorOfProperty)
    ? ApiErrorCode.PricingDuplicateSectorNotFound
    : null;
}

/** Every sector the package has data of. */
function sectorsOf(details: PackageAddItemRequest): (string | null)[] {
  return [
    ...details.idSectors,
    ...[
      ...details.sectorPrices,
      ...details.periodPrices,
      ...details.reducedSeasonalPrices.flatMap(({ sectorPrices }) => sectorPrices),
    ].map(({ idSector }) => idSector),
  ];
}

/** As the backend: the rows of a deleted sector or period, or without prices, are not saved. */
function savedRows(details: PackageAddItemRequest): PackageAddItemRequest {
  const isKept = (row: PackageSectorPrice | PackagePeriodPrice): boolean =>
    isSectorOfProperty(row.idSector) &&
    Object.entries(row).some(
      ([field, value]) => field !== 'idSector' && field !== 'idPeriod' && value !== null,
    );
  return {
    ...details,
    sectorPrices: details.sectorPrices.filter(isKept),
    periodPrices: details.periodPrices.filter(
      (price) => isKept(price) && isPeriodOfSeason(price.idPeriod),
    ),
    reducedSeasonalPrices: details.reducedSeasonalPrices.map((price) => ({
      ...price,
      sectorPrices: price.sectorPrices.filter(isKept),
    })),
  };
}

/** The names of the additional items linked to the package and only to those being deleted. */
function onlyWith({ idPackage }: PackageRow, deleted: readonly PackageRow[]): string[] {
  return ADDITIONAL_ITEMS.filter(({ propertyId, idAdditionalItem }) => {
    const links = ADDITIONAL_ITEM_DETAILS.get(idAdditionalItem ?? '')?.idPackages ?? [];
    return (
      propertyId === DEMO_PROPERTY.id &&
      links.some((id) => id === idPackage) &&
      links.every((id) => deleted.some((row) => row.idPackage === id))
    );
  }).map(({ additionalItemName }) => additionalItemName ?? '');
}

function copiedTranslation(
  translation: PackageTranslation,
  italianName: string | null,
): PackageTranslation {
  const { language, packageName } = translation;
  if (!language || !packageName) {
    return { ...translation };
  }
  return {
    ...translation,
    packageName:
      language === Language.It
        ? italianName
        : copyName(
            packageName,
            language,
            packagesOfProperty().map((row) => translatedNameOf(row, language)),
          ),
  };
}

/**
 * All or nothing: 404 `pricing.price_list_not_found` when one of `idPackages` is not a package of the
 * property, and nothing changes; 400 `validation.required` without ids; repeated ids count once. 204
 * without a body; 401 without a valid access token.
 */
function withPackages(
  request: HttpRequest<unknown>,
  change: (packages: PackageRow[]) => Observable<HttpEvent<unknown>> | null,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const { idPackages } = (request.body ?? {}) as { idPackages?: unknown };
  const ids = Array.isArray(idPackages) ? [...new Set<unknown>(idPackages)] : [];
  if (!ids.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: 'idPackages', code: ApiErrorCode.ValidationRequired }],
    });
  }
  const packages = ids.flatMap((id) =>
    packagesOfProperty().filter(({ idPackage }) => idPackage === id),
  );
  if (packages.length !== ids.length) {
    return problem(request, 404, ApiErrorCode.PricingPriceListNotFound);
  }
  return (
    change(packages) ?? of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600))
  );
}

function packagesOfProperty(): PackageRow[] {
  return PACKAGES.filter(({ propertyId }) => propertyId === DEMO_PROPERTY.id);
}

function translatedNameOf(row: PackageRow, language: string): string | null {
  return (
    PACKAGE_DETAILS.get(row.idPackage ?? '')
      ?.translations.find((translation) => translation.language === language)
      ?.packageName?.trim() || null
  );
}

/** The name shown for a package: the one in `language`, otherwise its `name` (`row.packageName`). */
function packageNameIn(row: PackageRow, language: string): string | null {
  return translatedNameOf(row, language) ?? row.packageName;
}

/** The `name` of a saved package: the Italian name when written, otherwise the one in `language`. */
function savedPackageName(
  translations: readonly Partial<PackageTranslation>[] | undefined,
  language: string,
): string | null {
  const nameIn = (code: string): string | undefined =>
    translations?.find((translation) => translation.language === code)?.packageName?.trim();
  return nameIn('it') || nameIn(language) || null;
}

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.trim().localeCompare(second.trim(), 'it', { sensitivity: 'base', numeric: true });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

type CreatePackageBody = Partial<PackageAddItemRequest>;

/**
 * `POST /api/catalog/price-lists/create-package` with `PackageAddItemRequest`, the property from the
 * access token: the new package goes into the list, its `name` the Italian name or, when missing,
 * the one in the language of `Accept-Language` (`savedPackageName`). 204 without a body; 401 without
 * a valid access token; 400 with one error per wrong field (see `packageErrors`); 404
 * `catalog.sector_not_found` for a deleted sector among the enabled ones.
 */
export const createPackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreatePackageBody;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = packageErrors(body, language);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const missing = referenceProblem(request, body);
  if (missing) {
    return missing;
  }
  const row: PackageRow = {
    propertyId: DEMO_PROPERTY.id,
    idPackage: crypto.randomUUID(),
    packageName: savedPackageName(body.translations, language),
  };
  PACKAGES.push(row);
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    savedRows(structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest))),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The VAT, the sectors, the areas and the items of the body, in the order of the backend: 422
 * `catalog.vat_rate_not_allowed`, 404 `catalog.sector_not_found`, `map.spot_not_found` and
 * `inventory.item_not_found`.
 */
function referenceProblem(
  request: HttpRequest<unknown>,
  body: CreatePackageBody,
): Observable<never> | null {
  const missing =
    vatRateNotAllowed(request, body.vatRate) ?? missingSector(request, body.idSectors);
  if (missing) {
    return missing;
  }
  if (!(body.idSectorAreas ?? []).every(isAreaOfProperty)) {
    return problem(request, 404, ApiErrorCode.MapSpotNotFound);
  }
  return (body.items ?? []).every(({ idInventoryItem }) =>
    activeArticleName(DEMO_PROPERTY.id, idInventoryItem),
  )
    ? null
    : problem(request, 404, ApiErrorCode.InventoryItemNotFound);
}

/**
 * `GET /api/catalog/price-lists/get-package-articles`: the articles of the backend, always all, each
 * with its items in the warehouse, if any. 401 without a valid access token.
 */
export const packageArticlesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const articles = Object.values(InventoryArticle).map((article): PackageArticle => ({
    article,
    items: activeArticlesOfCode(
      DEMO_PROPERTY.id,
      article.replace(/(?!^)([A-Z])/g, '_$1').toLowerCase(),
    ),
  }));
  return of(new HttpResponse({ status: 200, url: request.url, body: articles })).pipe(delay(300));
};

/**
 * The name in the language of the request (the user's), as the panel requires, and every name at most
 * `MAX_PACKAGE_NAME_LENGTH` characters; the quantities, when given, within the choices of their
 * dropdowns; calculation method, monthly and weekend settings, when given, among the values of their
 * enums; prices between 0 and `MAX_PRICE_LIST_PRICE`; every reduced seasonal price with its name (at
 * most `MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH` characters) and its days in order, without days in
 * common with another one.
 */
function packageErrors(body: CreatePackageBody, language: string): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const translations = Array.isArray(body.translations) ? body.translations : [];
  const index = translations.findIndex((translation) => translation.language === language);
  const main = translations[index];
  if (typeof main?.packageName !== 'string' || !main.packageName.trim()) {
    errors.push({
      field: `translations[${Math.max(index, 0)}].packageName`,
      code: ApiErrorCode.ValidationRequired,
    });
  }
  translations.forEach(({ packageName }, translationIndex) => {
    if (typeof packageName === 'string' && packageName.trim().length > MAX_PACKAGE_NAME_LENGTH) {
      errors.push({
        field: `translations[${translationIndex}].packageName`,
        code: ApiErrorCode.ValidationMaxLength,
        args: { max: MAX_PACKAGE_NAME_LENGTH },
      });
    }
  });
  const ranges = [
    ['maxAdditionalItemQuantity', ADDITIONAL_ITEM_QUANTITY_RANGE],
    ['maxPeopleQuantity', PEOPLE_QUANTITY_RANGE],
  ] as const;
  for (const [field, range] of ranges) {
    const quantity = body[field];
    if (quantity != null && !isWholeNumberIn(quantity, range.from, range.to)) {
      errors.push({ field, code: ApiErrorCode.ValidationOutOfRange, args: { ...range } });
    }
  }
  const items = body.items ?? [];
  const idItems = items.map(({ idInventoryItem }) => idInventoryItem);
  if (
    items.length > MAX_ITEM_QUANTITY ||
    new Set(idItems).size !== idItems.length ||
    items.some(
      ({ quantity }) => quantity === null || !isWholeNumberIn(quantity, 1, MAX_ARTICLE_QUANTITY),
    )
  ) {
    errors.push({ field: 'items', code: ApiErrorCode.ValidationInvalid });
  }
  if ((body.includedServices ?? '').trim().length > MAX_PACKAGE_INCLUDED_SERVICES_LENGTH) {
    errors.push({
      field: 'includedServices',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: MAX_PACKAGE_INCLUDED_SERVICES_LENGTH },
    });
  }
  if (body.vatRate != null && (body.vatRate <= 0 || body.vatRate >= 100)) {
    errors.push({ field: 'vatRate', code: ApiErrorCode.ValidationInvalid });
  }
  errors.push(...choiceErrors(body));
  const reducedPrices = Array.isArray(body.reducedSeasonalPrices) ? body.reducedSeasonalPrices : [];
  reducedPrices.forEach((reduced, reducedIndex) => {
    const field = `reducedSeasonalPrices[${reducedIndex}]`;
    const name = reduced.reducedSeasonalPriceName?.trim();
    if (!name) {
      errors.push({
        field: `${field}.reducedSeasonalPriceName`,
        code: ApiErrorCode.ValidationRequired,
      });
    } else if (name.length > MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH) {
      errors.push({
        field: `${field}.reducedSeasonalPriceName`,
        code: ApiErrorCode.ValidationMaxLength,
        args: { max: MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH },
      });
    }
    if (!reduced.startsOn || !reduced.endsOn) {
      errors.push({ field: `${field}.startsOn`, code: ApiErrorCode.ValidationRequired });
    } else if (reduced.endsOn < reduced.startsOn) {
      errors.push({ field: `${field}.endsOn`, code: ApiErrorCode.ValidationEndBeforeStart });
    } else if (
      reducedPrices.some(
        (other, otherIndex) =>
          otherIndex !== reducedIndex &&
          !!other.startsOn &&
          !!other.endsOn &&
          (reduced.startsOn as string).slice(0, 10) <= other.endsOn.slice(0, 10) &&
          other.startsOn.slice(0, 10) <= (reduced.endsOn as string).slice(0, 10),
      )
    ) {
      errors.push({ field: `${field}.startsOn`, code: ApiErrorCode.ValidationInvalid });
    }
  });
  if (
    !reducedPrices.every(
      ({ sectorPrices }) =>
        Array.isArray(sectorPrices) &&
        sectorPrices.every(({ seasonalHalfDayPrice, seasonalPrice }) =>
          [seasonalHalfDayPrice, seasonalPrice].every(isPrice),
        ),
    )
  ) {
    errors.push({ field: 'reducedSeasonalPrices', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

/** The package of the token's property, null when it is not one of them. */
function packageOf(idPackage: string): PackageRow | null {
  return (
    PACKAGES.find((row) => row.idPackage === idPackage && row.propertyId === DEMO_PROPERTY.id) ??
    null
  );
}

/**
 * `GET /api/catalog/price-lists/get-package`, the property from the access token: everything the
 * package has, as the panels send it, and its version; a package never saved has only its name in
 * Italian. 200; 401 without a valid access token; 404 `pricing.price_list_not_found` for a package
 * that is not one of its.
 */
export const selectedPackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = packageOf(request.params.get('idPackage') ?? '');
  if (!row) {
    return problem(request, 404, ApiErrorCode.PricingPriceListNotFound);
  }
  const data = packageData(row);
  const answer: PackageSelectedItem = { ...data, version: versionOf(data) };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/** What `GET /api/catalog/price-lists/get-package` answers besides the version: the version covers it. */
function packageData(row: PackageRow): Omit<PackageSelectedItem, 'version'> {
  const detail =
    PACKAGE_DETAILS.get(row.idPackage ?? '') ??
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: row.packageName }],
    });
  return { ...structuredClone(detail), idPackage: row.idPackage };
}

/**
 * `POST /api/catalog/price-lists/update-package` with `PackageAddItemRequest` and `idPackage`, the
 * property from the access token: the package takes all the values sent, its `name` as in
 * `createPackageMock`. 204 without a body, with the new version in `ETag`; 401 without a valid access
 * token; 404 `pricing.price_list_not_found` for a package that is not one of its; 400 as
 * `createPackageMock`; 409 `resource.version_conflict` when `If-Match` is not the version of the
 * package; 404 `catalog.sector_not_found` for a deleted sector among the enabled ones. The rows of
 * deleted sectors and periods, or without prices, are not saved.
 */
export const updatePackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreatePackageBody & { idPackage?: unknown };
  const row = typeof body.idPackage === 'string' ? packageOf(body.idPackage) : null;
  if (!row) {
    return problem(request, 404, ApiErrorCode.PricingPriceListNotFound);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = packageErrors(body, language);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const conflict = versionConflict(request, packageData(row));
  if (conflict) {
    return conflict;
  }
  const missing = referenceProblem(request, body);
  if (missing) {
    return missing;
  }
  row.packageName = savedPackageName(body.translations, language) ?? row.packageName;
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    savedRows(structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest))),
  );
  return savedWithVersion(request, packageData(row));
};

type AdditionalItemRow = AdditionalItem & { propertyId: number };

const ADDITIONAL_ITEM_NAMES = [
  'Lettino',
  'Sdraio',
  'Sedia regista',
  'Ombrellone aggiuntivo',
  'Telo mare',
  'Cassetta di sicurezza',
  'Doccia calda',
  'Posto auto',
  'Cabina giornaliera',
  'Tenda',
  'Lettino extra famiglia',
];

const ADDITIONAL_ITEMS: AdditionalItemRow[] = ADDITIONAL_ITEM_NAMES.map(
  (additionalItemName, index) => ({
    propertyId: 1,
    idAdditionalItem: `4b7e9a31-2c5d-4f8e-a1b2-3c4d5e6f7a${String(index + 1).padStart(2, '0')}`,
    additionalItemName,
  }),
);

/** What the panels saved, by idAdditionalItem; an item never saved has only its article. */
const ADDITIONAL_ITEM_DETAILS = new Map<string, AdditionalItemAddItemRequest>([
  [
    ADDITIONAL_ITEMS[2].idAdditionalItem ?? '',
    Object.assign(new AdditionalItemAddItemRequest(), {
      idArticle: activeArticleIdNamed(
        ADDITIONAL_ITEMS[2].propertyId,
        ADDITIONAL_ITEMS[2].additionalItemName,
      ),
      idPackages: [PACKAGES[5].idPackage ?? ''],
    }),
  ],
]);

const ADDITIONAL_ITEM_SORT_FIELDS = ['additionalItemName'] as const;

/**
 * `POST /api/catalog/additional-services-articles-list`: one page of the additional items/services
 * of the property of the token, in their own order unless sorted. 401 without a valid access token.
 */
export const additionalItemListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = ADDITIONAL_ITEMS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.additionalItemName?.trim().toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(AdditionalItem, row));
  const field = ADDITIONAL_ITEM_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<AdditionalItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/delete-additional-services-article-item`: deletes the given additional
 * items/services, all or nothing. 204 with no body; 404 when an id is not an additional item of the
 * property; 401 as the list.
 */
export const deleteAdditionalItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idAdditionalItems?: string[] };
  const property = DEMO_PROPERTY;
  const items = additionalItemsOf(property.id, body.idAdditionalItems);
  if (!items) {
    return resourceNotFound(request);
  }
  for (const item of items) {
    ADDITIONAL_ITEMS.splice(ADDITIONAL_ITEMS.indexOf(item), 1);
    ADDITIONAL_ITEM_DETAILS.delete(item.idAdditionalItem ?? '');
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-additional-services-article-item`: one copy of each given additional
 * item/service at the end of the list, all or nothing, named by the backend ("X (copia)", then "X
 * (copia 2)"…). 204 with no body; 404 when an id is not an additional item of the property; 401 as
 * the list.
 */
export const duplicateAdditionalItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idAdditionalItems?: string[] };
  const property = DEMO_PROPERTY;
  const items = additionalItemsOf(property.id, body.idAdditionalItems);
  if (!items) {
    return resourceNotFound(request);
  }
  for (const item of items) {
    const copy: AdditionalItemRow = {
      propertyId: item.propertyId,
      idAdditionalItem: crypto.randomUUID(),
      additionalItemName: additionalItemCopyName(item),
    };
    const detail = ADDITIONAL_ITEM_DETAILS.get(item.idAdditionalItem ?? '');
    if (detail) {
      ADDITIONAL_ITEM_DETAILS.set(copy.idAdditionalItem ?? '', structuredClone(detail));
    }
    ADDITIONAL_ITEMS.push(copy);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function additionalItemCopyName(item: AdditionalItemRow): string {
  const taken = new Set(
    ADDITIONAL_ITEMS.filter((row) => row.propertyId === item.propertyId).map((row) =>
      row.additionalItemName?.trim().toLowerCase(),
    ),
  );
  let copy = `${item.additionalItemName} (copia)`;
  for (let number = 2; taken.has(copy.trim().toLowerCase()); number++) {
    copy = `${item.additionalItemName} (copia ${number})`;
  }
  return copy;
}

function additionalItemsOf(
  propertyId: number,
  idAdditionalItems: unknown,
): AdditionalItemRow[] | null {
  const ids = Array.isArray(idAdditionalItems) ? [...new Set(idAdditionalItems)] : [];
  const items = ids
    .map((id) =>
      ADDITIONAL_ITEMS.find((row) => row.idAdditionalItem === id && row.propertyId === propertyId),
    )
    .filter((item): item is AdditionalItemRow => item !== undefined);
  return ids.length && items.length === ids.length ? items : null;
}

/**
 * Calculation method, monthly and weekend settings, when given, among the values of their enums;
 * the public holidays real days, each once; every intermediate price with its period, from 1 to 90
 * days and not negative, once for each period, row group and number of days.
 */
function choiceErrors(body: {
  calculationMethod?: unknown;
  monthlySetting?: unknown;
  weekendSetting?: unknown;
  publicHolidayDates?: unknown;
  sectorPrices?: unknown;
  periodPrices?: unknown;
  intermediatePrices?: unknown;
}): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const enums = [
    ['calculationMethod', PriceCalculationMethod],
    ['monthlySetting', PriceMonthlySetting],
    ['weekendSetting', PriceWeekendSetting],
  ] as const;
  for (const [field, values] of enums) {
    const value = body[field];
    if (value != null && !(Object.values(values) as unknown[]).includes(value)) {
      errors.push({ field, code: ApiErrorCode.ValidationInvalid });
    }
  }
  const dates = body.publicHolidayDates ?? [];
  if (
    !Array.isArray(dates) ||
    dates.some((date) => typeof date !== 'string' || Number.isNaN(Date.parse(date))) ||
    new Set(dates.map((date: string) => date.slice(0, 10))).size !== dates.length
  ) {
    errors.push({ field: 'publicHolidayDates', code: ApiErrorCode.ValidationInvalid });
  }
  const sectorPrices = body.sectorPrices ?? [];
  if (
    !Array.isArray(sectorPrices) ||
    !sectorPrices.every(
      (row: Partial<PackageSectorPrice> | null) =>
        !!row && [row.seasonalHalfDayPrice, row.seasonalPrice].every(isPrice),
    )
  ) {
    errors.push({ field: 'sectorPrices', code: ApiErrorCode.ValidationInvalid });
  }
  const periodPrices = body.periodPrices ?? [];
  if (
    !Array.isArray(periodPrices) ||
    !periodPrices.every(
      (row: Partial<PackagePeriodPrice> | null) =>
        !!row &&
        [
          row.halfDayPrice,
          row.dailyPrice,
          row.weekendPrice,
          row.halfDayWeekendPrice,
          row.halfDaySevenDaysPrice,
          row.sevenDaysPrice,
          row.halfDayFourteenDaysPrice,
          row.fourteenDaysPrice,
          row.halfDayMonthlyPrice,
          row.monthlyPrice,
        ].every(isPrice),
    )
  ) {
    errors.push({ field: 'periodPrices', code: ApiErrorCode.ValidationInvalid });
  }
  const prices = body.intermediatePrices ?? [];
  if (
    !Array.isArray(prices) ||
    !prices.every(isIntermediatePrice) ||
    new Set(
      prices.map(
        ({ idPeriod, isFirstRow, dayQuantity }: PackageIntermediatePrice) =>
          `${idPeriod}|${isFirstRow}|${dayQuantity}`,
      ),
    ).size !== prices.length
  ) {
    errors.push({ field: 'intermediatePrices', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

function isIntermediatePrice(value: unknown): boolean {
  const { idPeriod, isFirstRow, dayQuantity, price } = (value ??
    {}) as Partial<PackageIntermediatePrice>;
  return (
    Number.isInteger(idPeriod) &&
    typeof isFirstRow === 'boolean' &&
    typeof dayQuantity === 'number' &&
    Number.isInteger(dayQuantity) &&
    dayQuantity >= 1 &&
    dayQuantity <= MAX_INTERMEDIATE_PRICE_DAY_QUANTITY &&
    typeof price === 'number' &&
    isPrice(price)
  );
}

/** A price of the price list: none, or between 0 and `MAX_PRICE_LIST_PRICE`. */
function isPrice(value: unknown): boolean {
  return (
    value == null ||
    (typeof value === 'number' &&
      Number.isFinite(value) &&
      value >= 0 &&
      value <= MAX_PRICE_LIST_PRICE)
  );
}

/**
 * `POST /api/catalog/package-combobox` without a body: every package of the property, in its order,
 * as `{ id, value }`, the name as in `packageListMock`. 401 without a valid access token.
 */
export const packageComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const items: ComboboxItem[] = PACKAGES.filter((row) => row.propertyId === property.id).map(
    (row) => ({ id: row.idPackage ?? '', value: packageNameIn(row, language) ?? '' }),
  );
  return of(new HttpResponse({ status: 200, url: request.url, body: items })).pipe(delay(150));
};

type CreateAdditionalItemBody = Partial<AdditionalItemAddItemRequest>;

/**
 * `POST /api/catalog/create-additional-services-article-item` with `AdditionalItemAddItemRequest`:
 * the new additional item goes into the list with the name of its article. 204 without a body; 401
 * without a valid access token; 404 `resource.not_found` for an article that is not an active one
 * of the property or a package that is not one of its; 400 with one error per wrong choice (see
 * `choiceErrors`).
 */
export const createAdditionalItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateAdditionalItemBody;
  const property = DEMO_PROPERTY;
  const name = additionalItemName(property.id, body);
  if (name === null) {
    return resourceNotFound(request);
  }
  const errors = choiceErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const row: AdditionalItemRow = {
    propertyId: property.id,
    idAdditionalItem: crypto.randomUUID(),
    additionalItemName: name,
  };
  ADDITIONAL_ITEMS.push(row);
  ADDITIONAL_ITEM_DETAILS.set(
    row.idAdditionalItem ?? '',
    structuredClone(
      onlyFieldsOf(AdditionalItemAddItemRequest, body as AdditionalItemAddItemRequest),
    ),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The name of the article of the item (the item takes it), null when the article is not an active
 * one of the property or a package is not one of its.
 */
function additionalItemName(propertyId: number, body: CreateAdditionalItemBody): string | null {
  const idPackages = Array.isArray(body.idPackages) ? body.idPackages : [];
  const isEveryPackageKnown = idPackages.every((idPackage) =>
    PACKAGES.some((row) => row.idPackage === idPackage && row.propertyId === propertyId),
  );
  return isEveryPackageKnown ? activeArticleName(propertyId, body.idArticle) : null;
}

/**
 * `GET /api/catalog/get-additional-services-article-item?idAdditionalItem=…`, the property of the
 * access token: everything the item has, as the panels send it, and its version; an item never saved
 * has only the article with its name. 200; 401 without a valid access token; 404
 * `resource.not_found` for an item that is not one of its.
 */
export const selectedAdditionalItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idAdditionalItem = request.params.get('idAdditionalItem');
  const row = ADDITIONAL_ITEMS.find(
    (item) => item.idAdditionalItem === idAdditionalItem && item.propertyId === DEMO_PROPERTY.id,
  );
  if (!row) {
    return resourceNotFound(request);
  }
  const data = additionalItemData(row);
  const answer: AdditionalItemSelectedItem = { ...data, version: versionOf(data) };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * What `GET /api/catalog/get-additional-services-article-item` answers besides the version: the
 * version covers it.
 */
function additionalItemData(row: AdditionalItemRow): Omit<AdditionalItemSelectedItem, 'version'> {
  const detail =
    ADDITIONAL_ITEM_DETAILS.get(row.idAdditionalItem ?? '') ??
    Object.assign(new AdditionalItemAddItemRequest(), {
      idArticle: activeArticleIdNamed(row.propertyId, row.additionalItemName),
    });
  return { ...structuredClone(detail), idAdditionalItem: row.idAdditionalItem };
}

/**
 * `POST /api/catalog/update-additional-services-article-item` with `AdditionalItemAddItemRequest`,
 * `idAdditionalItem`: the item takes all the values sent and the name of its article. 204 without a
 * body, with the new version in `ETag`; 401 / 404 / 400 as `createAdditionalItemMock`, 404 also for
 * an item that is not one of the property; 409 `resource.version_conflict` when `If-Match` is not the
 * version of the item.
 */
export const updateAdditionalItemMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateAdditionalItemBody & { idAdditionalItem?: unknown };
  const property = DEMO_PROPERTY;
  const row = ADDITIONAL_ITEMS.find(
    (item) => item.idAdditionalItem === body.idAdditionalItem && item.propertyId === property.id,
  );
  const name = additionalItemName(property.id, body);
  if (!row || name === null) {
    return resourceNotFound(request);
  }
  const errors = choiceErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  const conflict = versionConflict(request, additionalItemData(row));
  if (conflict) {
    return conflict;
  }
  row.additionalItemName = name;
  ADDITIONAL_ITEM_DETAILS.set(
    row.idAdditionalItem ?? '',
    structuredClone(
      onlyFieldsOf(AdditionalItemAddItemRequest, body as AdditionalItemAddItemRequest),
    ),
  );
  return savedWithVersion(request, additionalItemData(row));
};

function isWholeNumberIn(value: number, from: number, to: number): boolean {
  return Number.isInteger(value) && value >= from && value <= to;
}
