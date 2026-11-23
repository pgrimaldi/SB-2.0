import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_SEARCH_MAX_LENGTH,
} from '../../../components/shared/ui/tables/table/table';
import { sameTextIgnoringCase } from '../../../components/shared/forms/text-comparison';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageRequest, SortDirection } from '../../../entities/pagination/page';
import {
  MAX_SERVICE_NAME_LENGTH,
  ServiceTranslation,
} from '../../../entities/settings/services/service-translation';
import { Language } from '../../../entities/shared/language';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, MockProblemDetails, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

const SORT_DIRECTIONS: readonly SortDirection[] = ['Ascending', 'Descending'];

const COPY_SUFFIXES: Readonly<Record<Language, string>> = {
  [Language.It]: ' copia',
  [Language.En]: ' copy',
  [Language.Fr]: ' copie',
  [Language.Es]: ' copia',
  [Language.De]: ' Kopie',
  [Language.Zh]: ' 副本',
  [Language.Ar]: ' نسخة',
};

interface ServiceRow {
  propertyId: number;
  serviceName: string | null;
}

interface TranslatedDetails {
  translations: ServiceTranslation[];
}

/** Why an element cannot be copied, besides its name. */
export interface MockCopyError {
  code: ApiErrorCode;
  args: MockProblemDetails['args'];
}

/** The rows of a tab of Servizi and, by id, what their panel saves. */
export interface ServiceTable<Row extends ServiceRow, Details extends TranslatedDetails> {
  rows: Row[];
  idField: keyof Row & string;
  details: Map<string, Details>;
  detailsOf: (row: Row) => Details;
  notFoundCode: ApiErrorCode;
  /** What else a copy takes from the original, by their ids. */
  copyMore?: (id: string, idCopy: string) => void;
  /** What else the delete of an element changes, by its id. */
  deleteMore?: (id: string) => void;
  /** Why an element cannot be copied; none when it can. */
  copyError?: (details: Details) => MockCopyError | null;
}

/**
 * The table of a tab: searched and sorted by the name in the language of `Accept-Language`, in its
 * natural order unless sorted; `choices` are the values of a field in the order of their enum, the
 * empty ones always last. 400 `validation.invalid` on `page`, `pageSize`, `sortField`,
 * `sortDirection` or `filters` out of the table's rules and `validation.max_length` on a `search`
 * over 100 characters; 401 without a valid access token.
 */
export function serviceListAnswer<
  Item extends { serviceName: string | null },
  Row extends ServiceRow & Item,
  Details extends TranslatedDetails,
>(
  request: HttpRequest<unknown>,
  table: ServiceTable<Row, Details>,
  item: new () => Item,
  sortFields: readonly (keyof Item & string)[],
  choices: Partial<Record<string, readonly unknown[]>> = {},
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body, sortFields);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = TABLE_PAGE_SIZE_OPTIONS[0] } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = rowsOfProperty(table)
    .map((row): Item => ({
      ...onlyFieldsOf(item, row),
      serviceName: shownName(table, row, languageOf(request)),
    }))
    .filter(({ serviceName }) => !search || serviceName?.trim().toLowerCase().includes(search));
  const field = sortFields.find((name) => name === body.sortField) ?? 'serviceName';
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  found.sort(
    (first, second) =>
      compare(first[field as keyof Item], second[field as keyof Item], sign, choices[field]) ||
      naturalOrder(first.serviceName, second.serviceName),
  );
  const answer: Page<Item> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
}

/**
 * All or nothing: 404 with the code of the tab when one id is not one of its rows, and nothing is
 * deleted; 400 `validation.required` without ids; repeated ids count once. 204 without a body; 401
 * without a valid access token.
 */
export function deleteServicesAnswer<Row extends ServiceRow, Details extends TranslatedDetails>(
  request: HttpRequest<unknown>,
  table: ServiceTable<Row, Details>,
  idsField: string,
): Observable<HttpEvent<unknown>> {
  return withRows(request, table, idsField, (rows) => {
    for (const row of rows) {
      table.rows.splice(table.rows.indexOf(row), 1);
      table.deleteMore?.(idOf(table, row));
    }
    return null;
  });
}

/**
 * All or nothing, like the delete: each copy has every data of the original and the first free name,
 * also in the other languages, each with its own suffix ("Caffè copia", then "Caffè copia 2"). 422
 * for an element that cannot be copied, with its name (`copyError`).
 */
export function duplicateServicesAnswer<Row extends ServiceRow, Details extends TranslatedDetails>(
  request: HttpRequest<unknown>,
  table: ServiceTable<Row, Details>,
  idsField: string,
): Observable<HttpEvent<unknown>> {
  return withRows(request, table, idsField, (rows) => {
    for (const row of rows) {
      const error = table.copyError?.(table.detailsOf(row));
      if (error) {
        const serviceName = shownName(table, row, languageOf(request)) ?? '';
        return problem(request, 422, error.code, { args: { ...error.args, serviceName } });
      }
    }
    for (const row of rows) {
      const id = idOf(table, row);
      const idCopy = crypto.randomUUID();
      const details = structuredClone(table.detailsOf(row));
      const serviceName = copyName(
        row.serviceName ?? '',
        Language.It,
        rowsOfProperty(table).map((other) => other.serviceName),
      );
      details.translations = details.translations.map((translation) =>
        translation.serviceName && translation.language
          ? {
              ...translation,
              serviceName:
                translation.language === Language.It
                  ? serviceName
                  : copyName(
                      translation.serviceName,
                      translation.language,
                      translatedNames(table, translation.language),
                    ),
            }
          : translation,
      );
      table.rows.push({ ...row, [table.idField]: idCopy, serviceName });
      table.details.set(idCopy, details);
      table.copyMore?.(id, idCopy);
    }
    return null;
  });
}

/** The Italian name, or the one in the language of the request when there is no Italian. */
export function nameToSave(
  translations: readonly ServiceTranslation[],
  language: string,
): string | null {
  return nameIn(translations, Language.It) ?? nameIn(translations, language);
}

function withRows<Row extends ServiceRow, Details extends TranslatedDetails>(
  request: HttpRequest<unknown>,
  table: ServiceTable<Row, Details>,
  idsField: string,
  change: (rows: Row[]) => Observable<HttpEvent<unknown>> | null,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const value = (request.body as Record<string, unknown> | null)?.[idsField];
  const ids = Array.isArray(value) ? [...new Set<unknown>(value)] : [];
  if (!ids.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      errors: [{ field: idsField, code: ApiErrorCode.ValidationRequired }],
    });
  }
  const rows = rowsOfProperty(table).filter((row) => ids.includes(idOf(table, row)));
  if (rows.length !== ids.length) {
    return problem(request, 404, table.notFoundCode);
  }
  return change(rows) ?? of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

function rowsOfProperty<Row extends ServiceRow, Details extends TranslatedDetails>(
  table: ServiceTable<Row, Details>,
): Row[] {
  return table.rows.filter((row) => row.propertyId === DEMO_PROPERTY.id);
}

function idOf<Row extends ServiceRow, Details extends TranslatedDetails>(
  table: ServiceTable<Row, Details>,
  row: Row,
): string {
  return String(row[table.idField]);
}

export function languageOf(request: HttpRequest<unknown>): string {
  return request.headers.get('Accept-Language') ?? Language.It;
}

function nameIn(translations: readonly ServiceTranslation[], language: string): string | null {
  return (
    translations.find((translation) => translation.language === language)?.serviceName?.trim() ||
    null
  );
}

/** The Italian name is the one of the row; the other languages fall back on it. */
export function shownName<Row extends ServiceRow, Details extends TranslatedDetails>(
  table: ServiceTable<Row, Details>,
  row: Row,
  language: string,
): string | null {
  return language === Language.It
    ? row.serviceName
    : (nameIn(table.detailsOf(row).translations, language) ?? row.serviceName);
}

function translatedNames<Row extends ServiceRow, Details extends TranslatedDetails>(
  table: ServiceTable<Row, Details>,
  language: Language,
): (string | null)[] {
  return rowsOfProperty(table).map((row) => nameIn(table.detailsOf(row).translations, language));
}

/** As `CopyNames.Next` of the backend: a copy of a copy starts again from the first name. */
export function copyName(
  name: string,
  language: Language,
  taken: readonly (string | null)[],
): string {
  const suffix = COPY_SUFFIXES[language];
  const base = name.trim().replace(new RegExp(`${suffix}(?: \\d{1,9})?$`, 'i'), '');
  for (let number = 1; ; number++) {
    const end = number === 1 ? suffix : `${suffix} ${number}`;
    const copy = base.slice(0, MAX_SERVICE_NAME_LENGTH - end.length).trimEnd() + end;
    if (!taken.some((other) => sameTextIgnoringCase(other, copy))) {
      return copy;
    }
  }
}

function compare(
  first: unknown,
  second: unknown,
  sign: number,
  choices?: readonly unknown[],
): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? 1 : -1;
  }
  if (choices) {
    return sign * (choices.indexOf(first) - choices.indexOf(second));
  }
  return (
    sign *
    (typeof first === 'number' && typeof second === 'number'
      ? first - second
      : naturalOrder(String(first), String(second)))
  );
}

/** "Cabina 2" before "Cabina 10"; capitals and accents do not count. */
export function naturalOrder(first: string | null, second: string | null): number {
  return (first ?? '')
    .trim()
    .localeCompare((second ?? '').trim(), 'it', { sensitivity: 'base', numeric: true });
}

export function tableRequestErrors(
  body: Partial<PageRequest>,
  sortFields: readonly string[],
): MockFieldError[] {
  const wrong = [
    { field: 'page', isWrong: !Number.isInteger(body.page) || (body.page ?? 0) < 1 },
    { field: 'pageSize', isWrong: !TABLE_PAGE_SIZE_OPTIONS.includes(body.pageSize ?? 0) },
    {
      field: 'sortField',
      isWrong: !!body.sortField && !sortFields.some((name) => name === body.sortField),
    },
    {
      field: 'sortDirection',
      isWrong: !SORT_DIRECTIONS.some((direction) => direction === body.sortDirection),
    },
    { field: 'filters', isWrong: !Array.isArray(body.filters) || body.filters.length > 0 },
  ];
  const errors: MockFieldError[] = wrong
    .filter(({ isWrong }) => isWrong)
    .map(({ field }) => ({ field, code: ApiErrorCode.ValidationInvalid }));
  if (typeof body.search === 'string' && body.search.length > TABLE_SEARCH_MAX_LENGTH) {
    errors.push({
      field: 'search',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: TABLE_SEARCH_MAX_LENGTH, min: 0 },
    });
  }
  return errors;
}
