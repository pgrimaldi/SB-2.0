import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { forbidden } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

/** ISO 3166-1 alpha-2 codes, in the order of `catalog.countries.sort_order`. */
const COUNTRY_CODES = ['IT', 'FR', 'DE', 'CN'] as const;

/**
 * `GET /api/catalog/countries-combobox`: the selectable countries, `id` = ISO code and `value` = name
 * in the language of `Accept-Language` (the backend has a translation per country and language).
 * 401 without a valid access token.
 */
export const countriesComboboxMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const names = new Intl.DisplayNames([request.headers.get('Accept-Language') ?? 'it'], {
    type: 'region',
  });
  const countries: ComboboxItem[] = COUNTRY_CODES.map((code) => ({
    id: code,
    value: names.of(code) ?? code,
  }));
  return of(new HttpResponse({ status: 200, url: request.url, body: countries })).pipe(delay(150));
};

/**
 * `GET /api/catalog/bathhouse-configuration-general?idProperty=…`: the general information of the
 * property, for the tab "Informazioni generali"; in the mock still all empty, a form to fill in. 401
 * without a valid access token; 403 for a missing or unknown property.
 */
export const bathhouseConfigurationGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = propertyOf(request.params.get('idProperty'));
  if (!property) {
    return forbidden(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: new BathhouseGeneralInformation() }),
  ).pipe(delay(300));
};
