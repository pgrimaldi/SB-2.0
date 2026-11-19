import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { isUploadable } from '../../../behaviours/files/uploadable-file';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { MapBackgroundImage } from '../../../entities/enums/map-background-image';
import { MapMixedSpotIcon } from '../../../entities/enums/map-mixed-spot-icon';
import { MapUmbrellaIcon } from '../../../entities/enums/map-umbrella-icon';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import {
  MAP_CUSTOM_ARTICLE_QUANTITY,
  MAX_MAP_ROW_ITEM_QUANTITY,
  MAX_MAP_ROW_QUANTITY,
  MAX_MAP_TILT_DEGREES,
  MIN_MAP_TILT_DEGREES,
  MapGrid,
  OCCUPIED_UMBRELLA_ICONS,
} from '../../../entities/settings/map-configuration/map-grid';
import {
  MapCustomArticleIcons,
  MapIcon,
  MapSettings,
} from '../../../entities/settings/map-configuration/map-settings';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../entities/shared/image-upload-types';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

const MAX_BYTES = MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE;
const SECTOR_AREAS: (ComboboxItem & { propertyId: number })[] = [
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a01', value: 'Area lettini' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a02', value: 'Area ombrelloni' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a03', value: 'Area cabine' },
  { propertyId: 1, id: '9e4b2c71-5d3a-4f8e-b6a1-2c3d4e5f6a04', value: 'Parcheggio' },
];

/** As the backend: a name without the spaces at the sides; none chosen is the default (`name` null). */
function icon(name: string | null): MapIcon {
  const chosen = name?.trim() || null;
  return { iconId: null, name: chosen, url: null, isDefault: chosen === null };
}

function article(
  variant: number | null,
  free: string | null = null,
  occupied: string | null = null,
): MapCustomArticleIcons {
  return { variant, icons: { free: icon(free), occupied: icon(occupied) } };
}

let settings: MapSettings = {
  ...new MapSettings(),
  rows: 6,
  columns: 16,
  tiltDegrees: 8,
  background: {
    iconId: null,
    name: MapBackgroundImage.BackgroundBeach,
    url: null,
    isCustom: false,
  },
  icons: {
    umbrella: {
      free: icon(MapUmbrellaIcon.Umbrella3),
      occupied: icon(MapMixedSpotIcon.MixedSpot4),
    },
    customArticles: [
      article(1, MapMixedSpotIcon.MixedSpot1, MapMixedSpotIcon.MixedSpot13),
      article(2, MapMixedSpotIcon.MixedSpot8),
      ...Array.from({ length: MAP_CUSTOM_ARTICLE_QUANTITY - 2 }, (_, index) => article(index + 3)),
    ],
  },
};

function noContent(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(300));
}

/** `null` (no icon) or one of the names, without the spaces at the sides. */
function isOneOf(value: unknown, names: readonly string[]): boolean {
  return value === null || (typeof value === 'string' && names.includes(value.trim()));
}

function rangeError(field: string, value: unknown, from: number, to: number): MockFieldError[] {
  if (value === null || value === undefined) {
    return [{ field, code: ApiErrorCode.ValidationRequired }];
  }
  return Number.isInteger(value) && (value as number) >= from && (value as number) <= to
    ? []
    : [{ field, code: ApiErrorCode.ValidationOutOfRange, args: { from, to } }];
}

function answer(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(settings) }),
  ).pipe(delay(300));
}

/**
 * `GET /api/catalog/map/settings`: everything the page shows, icons and backgrounds as names of the
 * frontend. 401 without a valid access token.
 */
export const mapSettingsMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> =>
  isAuthorized(request) ? answer(request) : unauthorized(request);

/**
 * `POST /api/catalog/map/background` with the image in the multipart field `file`: it becomes the
 * custom background (an object URL stands for the address of the backend's storage). 422
 * map.invalid_image for a type other than png, jpeg or webp, an empty file or one over 5 MB; 401 without a
 * valid access token.
 */
export const uploadMapBackgroundMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const image = request.body instanceof FormData ? request.body.get('file') : null;
  if (
    !(image instanceof File) ||
    !isUploadable(image, IMAGE_UPLOAD_TYPES, MAX_IMAGE_UPLOAD_MEGABYTES)
  ) {
    return problem(request, 422, ApiErrorCode.MapInvalidImage, {
      args: { maxBytes: MAX_BYTES, contentTypes: IMAGE_UPLOAD_TYPES },
    });
  }
  settings = {
    ...settings,
    background: { iconId: null, name: null, url: URL.createObjectURL(image), isCustom: true },
  };
  return answer(request);
};

/** `DELETE /api/catalog/map/background`: no background any more. 401 without a valid access token. */
export const removeMapBackgroundMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  settings = { ...settings, background: null };
  return answer(request);
};

/**
 * `PUT /api/catalog/map/update-background` with `backgroundName`, one of the frontend's backgrounds:
 * it replaces the uploaded one. 204; 400 validation.required without a name, validation.invalid for
 * another name; 401 without a valid access token.
 */
export const updateMapBackgroundMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const name = (request.body as { backgroundName?: unknown } | null)?.backgroundName;
  const isMissing =
    name === undefined || name === null || (typeof name === 'string' && !name.trim());
  if (isMissing || typeof name !== 'string' || !isOneOf(name, Object.values(MapBackgroundImage))) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [
        {
          field: 'backgroundName',
          code: isMissing ? ApiErrorCode.ValidationRequired : ApiErrorCode.ValidationInvalid,
        },
      ],
    });
  }
  settings = {
    ...settings,
    background: { iconId: null, name: name.trim(), url: null, isCustom: false },
  };
  return noContent(request);
};

/**
 * `POST /api/catalog/map/reset`: the spots are deleted and the grid is emptied (0 rows, 0 items per
 * row), as the backend's `MapLayout.ClearGrid`; background, icons and tilt stay. 200 with the whole
 * map; 401 without a valid access token.
 */
export const resetMapMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  settings = { ...settings, rows: 0, columns: 0 };
  return of(
    new HttpResponse({
      status: 200,
      url: request.url,
      body: { settings: structuredClone(settings), spots: [], sectors: [], beachElements: [] },
    }),
  ).pipe(delay(300));
};

/**
 * `PUT /api/catalog/map/update-configuration` with every value of Imposta reticolo (MapGrid): rows,
 * items per row, tilt and the icons of the umbrella and of the 8 custom articles. 204; 400 with
 * validation.required / validation.out_of_range on a number or validation.invalid on an unknown
 * icon name; 401 without a valid access token.
 */
export const updateMapConfigurationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const grid = (request.body ?? {}) as MapGrid;
  const articles = Array.isArray(grid.customArticleIcons) ? grid.customArticleIcons : [];
  const errors = [
    ...rangeError('rowQuantity', grid.rowQuantity, 1, MAX_MAP_ROW_QUANTITY),
    ...rangeError('maxRowItemQuantity', grid.maxRowItemQuantity, 1, MAX_MAP_ROW_ITEM_QUANTITY),
    ...rangeError('tiltDegrees', grid.tiltDegrees, MIN_MAP_TILT_DEGREES, MAX_MAP_TILT_DEGREES),
  ];
  if (
    !grid.umbrellaIcons ||
    !isOneOf(grid.umbrellaIcons.freeIcon, Object.values(MapUmbrellaIcon)) ||
    !isOneOf(grid.umbrellaIcons.occupiedIcon, OCCUPIED_UMBRELLA_ICONS)
  ) {
    errors.push({ field: 'umbrellaIcons', code: ApiErrorCode.ValidationInvalid });
  }
  const variants = new Set(articles.map((article) => article.variant));
  if (
    articles.length !== MAP_CUSTOM_ARTICLE_QUANTITY ||
    variants.size !== MAP_CUSTOM_ARTICLE_QUANTITY ||
    articles.some(
      (article) =>
        !Number.isInteger(article.variant) ||
        (article.variant as number) < 1 ||
        (article.variant as number) > MAP_CUSTOM_ARTICLE_QUANTITY ||
        !isOneOf(article.freeIcon, Object.values(MapMixedSpotIcon)) ||
        !isOneOf(article.occupiedIcon, Object.values(MapMixedSpotIcon)),
    )
  ) {
    errors.push({ field: 'customArticleIcons', code: ApiErrorCode.ValidationInvalid });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors,
    });
  }
  settings = {
    ...settings,
    rows: grid.rowQuantity,
    columns: grid.maxRowItemQuantity,
    tiltDegrees: grid.tiltDegrees,
    icons: {
      umbrella: {
        free: icon(grid.umbrellaIcons.freeIcon),
        occupied: icon(grid.umbrellaIcons.occupiedIcon),
      },
      customArticles: articles.map(({ variant, freeIcon, occupiedIcon }) =>
        article(variant, freeIcon, occupiedIcon),
      ),
    },
  };
  return noContent(request);
};

/**
 * `POST /api/catalog/map/sector-area-combobox` without a body: every area of the property's map, in
 * its order, as `{ id, value }`. 401 without a valid access token.
 */
export const sectorAreaComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
  const items: ComboboxItem[] = SECTOR_AREAS.filter((area) => area.propertyId === property.id).map(
    ({ id, value }) => ({ id, value }),
  );
  return of(new HttpResponse({ status: 200, url: request.url, body: items })).pipe(delay(150));
};
