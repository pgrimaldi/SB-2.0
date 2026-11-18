import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { AdditionalItemAddItemRequest } from '../../../entities/settings/catalog/additional-item-add-item-request';
import { AdditionalItemSelectedItem } from '../../../entities/settings/catalog/additional-item-selected-item';
import { PriceCalculationMethod } from '../../../entities/enums/price-calculation-method';
import { PriceMonthlySetting } from '../../../entities/enums/price-monthly-setting';
import { PriceWeekendSetting } from '../../../entities/enums/price-weekend-setting';
import {
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
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem, resourceNotFound } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';
import { activeArticleIdNamed, activeArticleName } from '../warehouse/warehouse.mock';

type PackageRow = PackageItem & { propertyId: number };

/** The backend's ranges (`SavePriceListRequestValidator`): wider than the frontend's lists. */
const PEOPLE_QUANTITY_RANGE = { from: 1, to: 100 };
const ADDITIONAL_ITEM_QUANTITY_RANGE = { from: 0, to: 100 };
const MAX_ARTICLE_QUANTITY = 100;
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
      umbrellaQuantity: 1,
      sunbedQuantity: 2,
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
 * `POST /api/catalog/packages-list`: one page of the packages of the property of the token, in
 * their own order unless sorted, each with its name in the language of `Accept-Language` (see
 * `packageNameIn`), which the search and the sorting use too. 401 without a valid access token.
 */
export const packageListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const language = request.headers.get('Accept-Language') ?? 'it';
  const found = PACKAGES.filter((row) => row.propertyId === property.id)
    .map((row) => ({
      ...onlyFieldsOf(PackageItem, row),
      packageName: packageNameIn(row, language),
    }))
    .filter(({ packageName }) => !search || packageName?.toLowerCase().includes(search));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<PackageItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/delete-package-item`: deletes the given packages, all or nothing. 204 with no
 * body; 404 when an id is not a package of the property; 401 as the list.
 */
export const deletePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idPackages?: string[] };
  const property = DEMO_PROPERTY;
  const packages = packagesOf(property.id, body.idPackages);
  if (!packages) {
    return resourceNotFound(request);
  }
  for (const item of packages) {
    PACKAGES.splice(PACKAGES.indexOf(item), 1);
    PACKAGE_DETAILS.delete(item.idPackage ?? '');
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-package-item`: one copy of each given package at the end of the
 * list, all or nothing, named by the backend ("X (copia)", then "X (copia 2)"…). 204 with no body;
 * 404 when an id is not a package of the property; 401 as the list.
 */
export const duplicatePackageItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idPackages?: string[] };
  const property = DEMO_PROPERTY;
  const packages = packagesOf(property.id, body.idPackages);
  if (!packages) {
    return resourceNotFound(request);
  }
  for (const item of packages) {
    const copy: PackageRow = {
      propertyId: item.propertyId,
      idPackage: crypto.randomUUID(),
      packageName: copyName(item),
    };
    const detail = PACKAGE_DETAILS.get(item.idPackage ?? '');
    if (detail) {
      PACKAGE_DETAILS.set(copy.idPackage ?? '', {
        ...structuredClone(detail),
        translations: detail.translations.map((translation) =>
          translation.packageName?.trim() === item.packageName
            ? { ...translation, packageName: copy.packageName }
            : { ...translation },
        ),
        isDefault: false,
        isDefaultOnline: false,
      });
    }
    PACKAGES.push(copy);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function copyName(item: PackageRow): string {
  const taken = new Set(
    PACKAGES.filter((row) => row.propertyId === item.propertyId).map((row) =>
      row.packageName?.toLowerCase(),
    ),
  );
  let copy = `${item.packageName} (copia)`;
  for (let number = 2; taken.has(copy.toLowerCase()); number++) {
    copy = `${item.packageName} (copia ${number})`;
  }
  return copy;
}

function packagesOf(propertyId: number, idPackages: unknown): PackageRow[] | null {
  const ids = Array.isArray(idPackages) ? [...new Set(idPackages)] : [];
  const packages = ids
    .map((id) => PACKAGES.find((row) => row.idPackage === id && row.propertyId === propertyId))
    .filter((item): item is PackageRow => item !== undefined);
  return ids.length && packages.length === ids.length ? packages : null;
}

/** The name shown for a package: the one in `language`, otherwise its `name` (`row.packageName`). */
function packageNameIn(row: PackageRow, language: string): string | null {
  const translated = PACKAGE_DETAILS.get(row.idPackage ?? '')
    ?.translations.find((translation) => translation.language === language)
    ?.packageName?.trim();
  return translated || row.packageName;
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
  return first.localeCompare(second, 'it', { sensitivity: 'base', numeric: true });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}

type CreatePackageBody = Partial<PackageAddItemRequest>;

/**
 * `POST /api/catalog/packages/create-package` with `PackageAddItemRequest`, the property from the
 * access token: the new package goes into the list, its `name` the Italian name or, when missing,
 * the one in the language of `Accept-Language` (`savedPackageName`). 204 without a body; 401 without
 * a valid access token; 400 with one error per wrong field (see `packageErrors`).
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
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const row: PackageRow = {
    propertyId: DEMO_PROPERTY.id,
    idPackage: crypto.randomUUID(),
    packageName: savedPackageName(body.translations, language),
  };
  PACKAGES.push(row);
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest)),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
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
      code: 'validation.required',
    });
  }
  translations.forEach(({ packageName }, translationIndex) => {
    if (typeof packageName === 'string' && packageName.trim().length > MAX_PACKAGE_NAME_LENGTH) {
      errors.push({
        field: `translations[${translationIndex}].packageName`,
        code: 'validation.max_length',
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
      errors.push({ field, code: 'validation.out_of_range', args: { ...range } });
    }
  }
  const articles = [
    'umbrellaQuantity',
    'sunbedQuantity',
    'deckchairQuantity',
    'directorChairQuantity',
    'cabinQuantity',
    'tentQuantity',
  ] as const;
  for (const field of articles) {
    const quantity = body[field];
    if (quantity != null && !isWholeNumberIn(quantity, 1, MAX_ARTICLE_QUANTITY)) {
      errors.push({ field, code: 'validation.invalid' });
    }
  }
  errors.push(...choiceErrors(body));
  const reducedPrices = Array.isArray(body.reducedSeasonalPrices) ? body.reducedSeasonalPrices : [];
  reducedPrices.forEach((reduced, reducedIndex) => {
    const field = `reducedSeasonalPrices[${reducedIndex}]`;
    const name = reduced.reducedSeasonalPriceName?.trim();
    if (!name) {
      errors.push({ field: `${field}.reducedSeasonalPriceName`, code: 'validation.required' });
    } else if (name.length > MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH) {
      errors.push({
        field: `${field}.reducedSeasonalPriceName`,
        code: 'validation.max_length',
        args: { max: MAX_REDUCED_SEASONAL_PRICE_NAME_LENGTH },
      });
    }
    if (!reduced.startsOn || !reduced.endsOn) {
      errors.push({ field: `${field}.startsOn`, code: 'validation.required' });
    } else if (reduced.endsOn < reduced.startsOn) {
      errors.push({ field: `${field}.endsOn`, code: 'validation.end_before_start' });
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
      errors.push({ field: `${field}.startsOn`, code: 'validation.invalid' });
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
    errors.push({ field: 'reducedSeasonalPrices', code: 'validation.invalid' });
  }
  return errors;
}

/** The id at the end of `/api/catalog/packages/{idPackage}`. */
function idPackageOf(request: HttpRequest<unknown>): string {
  return decodeURIComponent(request.url.slice(request.url.lastIndexOf('/') + 1));
}

/** The package of the token's property, null when it is not one of them. */
function packageOf(idPackage: string): PackageRow | null {
  return (
    PACKAGES.find((row) => row.idPackage === idPackage && row.propertyId === DEMO_PROPERTY.id) ??
    null
  );
}

/**
 * `GET /api/catalog/packages/{idPackage}`, the property from the access token: everything the
 * package has, as the panels send it; a package never saved has only its name in Italian. 200;
 * 401 without a valid access token; 404 `resource.not_found` for a package that is not one of its.
 */
export const selectedPackageMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = packageOf(idPackageOf(request));
  if (!row) {
    return resourceNotFound(request);
  }
  const detail =
    PACKAGE_DETAILS.get(row.idPackage ?? '') ??
    Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: 'it', packageName: row.packageName }],
    });
  const answer: PackageSelectedItem = { ...structuredClone(detail), idPackage: row.idPackage };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/catalog/packages/update-package` with `PackageAddItemRequest` and `idPackage`, the
 * property from the access token: the package takes all the values sent, its `name` as in
 * `createPackageMock`. 204 without a body; 401 without a valid access token;
 * 404 `resource.not_found` for a package that is not one of its; 400 as `createPackageMock`.
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
    return resourceNotFound(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = packageErrors(body, language);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  row.packageName = savedPackageName(body.translations, language) ?? row.packageName;
  PACKAGE_DETAILS.set(
    row.idPackage ?? '',
    structuredClone(onlyFieldsOf(PackageAddItemRequest, body as PackageAddItemRequest)),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
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
const ADDITIONAL_ITEM_DETAILS = new Map<string, AdditionalItemAddItemRequest>();

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
      (!search || row.additionalItemName?.toLowerCase().includes(search)),
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
      row.additionalItemName?.toLowerCase(),
    ),
  );
  let copy = `${item.additionalItemName} (copia)`;
  for (let number = 2; taken.has(copy.toLowerCase()); number++) {
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
      errors.push({ field, code: 'validation.invalid' });
    }
  }
  const dates = body.publicHolidayDates ?? [];
  if (
    !Array.isArray(dates) ||
    dates.some((date) => typeof date !== 'string' || Number.isNaN(Date.parse(date))) ||
    new Set(dates.map((date: string) => date.slice(0, 10))).size !== dates.length
  ) {
    errors.push({ field: 'publicHolidayDates', code: 'validation.invalid' });
  }
  const sectorPrices = body.sectorPrices ?? [];
  if (
    !Array.isArray(sectorPrices) ||
    !sectorPrices.every(
      (row: Partial<PackageSectorPrice> | null) =>
        !!row && [row.seasonalHalfDayPrice, row.seasonalPrice].every(isPrice),
    )
  ) {
    errors.push({ field: 'sectorPrices', code: 'validation.invalid' });
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
    errors.push({ field: 'periodPrices', code: 'validation.invalid' });
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
    errors.push({ field: 'intermediatePrices', code: 'validation.invalid' });
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
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
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
 * access token: everything the item has, as the panels send it; an item never saved has only the
 * article with its name. 200; 401 without a valid access token; 404 `resource.not_found` for an item
 * that is not one of its.
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
  const detail =
    ADDITIONAL_ITEM_DETAILS.get(row.idAdditionalItem ?? '') ??
    Object.assign(new AdditionalItemAddItemRequest(), {
      idArticle: activeArticleIdNamed(row.propertyId, row.additionalItemName),
    });
  const answer: AdditionalItemSelectedItem = {
    ...structuredClone(detail),
    idAdditionalItem: row.idAdditionalItem,
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/catalog/update-additional-services-article-item` with `AdditionalItemAddItemRequest`,
 * `idAdditionalItem`: the item takes all the values sent and the name of its article. 204 without a
 * body; 401 / 404 / 400 as `createAdditionalItemMock`, 404 also for an item that is not one of the
 * property.
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
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  row.additionalItemName = name;
  ADDITIONAL_ITEM_DETAILS.set(
    row.idAdditionalItem ?? '',
    structuredClone(
      onlyFieldsOf(AdditionalItemAddItemRequest, body as AdditionalItemAddItemRequest),
    ),
  );
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function isWholeNumberIn(value: number, from: number, to: number): boolean {
  return Number.isInteger(value) && value >= from && value <= to;
}
