import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
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

const SERVICE_LANGUAGES = ['it', 'en', 'fr', 'es', 'de', 'zh', 'ar'] as const;

/** The services of the catalogue: category, icon and the name in it, en, fr, es, de, zh, ar. */
const SERVICES: readonly {
  categoryCode: string;
  icon: string;
  names: readonly [string, string, string, string, string, string, string];
}[] = [
  {
    categoryCode: 'accessibility_comfort',
    icon: 'AccessibleForward',
    names: [
      'Accesso disabili',
      'Disabled access',
      'Accès handicapés',
      'Acceso para discapacitados',
      'Barrierefreier Zugang',
      '无障碍通道',
      'مدخل لذوي الإعاقة',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Accessible',
    names: [
      'Doccia per disabili',
      'Accessible shower',
      'Douche accessible',
      'Ducha adaptada',
      'Behindertengerechte Dusche',
      '无障碍淋浴',
      'دش لذوي الإعاقة',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Shower',
    names: ['Doccia', 'Shower', 'Douche', 'Ducha', 'Dusche', '淋浴', 'دش'],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Shower',
    names: [
      'Doccia calda',
      'Hot shower',
      'Douche chaude',
      'Ducha caliente',
      'Warme Dusche',
      '热水淋浴',
      'دش ساخن',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Shower',
    names: [
      'Tabacchi/edicola',
      'Tobacconist/newsstand',
      'Tabac/presse',
      'Estanco/quiosco',
      'Tabak/Zeitungen',
      '烟草/报刊亭',
      'تبغ/صحف',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Parking',
    names: ['Wifi', 'Wi-Fi', 'Wi-Fi', 'Wifi', 'WLAN', '无线网络', 'واي فاي'],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Wc',
    names: ['Toilette', 'Toilets', 'Toilettes', 'Aseos', 'Toiletten', '卫生间', 'دورات مياه'],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Parking',
    names: [
      'Parcheggio',
      'Parking',
      'Parking',
      'Aparcamiento',
      'Parkplatz',
      '停车场',
      'موقف سيارات',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Parking',
    names: [
      'Parcheggio custodito',
      'Guarded parking',
      'Parking gardé',
      'Aparcamiento vigilado',
      'Bewachter Parkplatz',
      '有人看管停车场',
      'موقف سيارات بحراسة',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Parking',
    names: [
      'Parcheggio bici',
      'Bike parking',
      'Parking vélos',
      'Aparcamiento de bicis',
      'Fahrradparkplatz',
      '自行车停车处',
      'موقف دراجات',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Lock',
    names: [
      'Cassette di sicurezza',
      'Safe deposit boxes',
      'Coffres-forts',
      'Cajas de seguridad',
      'Schließfächer',
      '保险箱',
      'خزائن أمانات',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Cabin',
    names: [
      'Cabine',
      'Changing cabins',
      'Cabines',
      'Casetas',
      'Umkleidekabinen',
      '更衣室',
      'كبائن',
    ],
  },
  {
    categoryCode: 'accessibility_comfort',
    icon: 'Translate',
    names: [
      'Personale multilingua',
      'Multilingual staff',
      'Personnel multilingue',
      'Personal multilingüe',
      'Mehrsprachiges Personal',
      '多语种员工',
      'موظفون متعددو اللغات',
    ],
  },
];

let generalInformation = new BathhouseGeneralInformation();

/**
 * `GET /api/catalog/bathhouse-configuration-general?idProperty=…`: the general information of the
 * property, for the tab "Informazioni generali"; all empty until saved, a form to fill in. 401
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
    new HttpResponse({ status: 200, url: request.url, body: { ...generalInformation } }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/bathhouse-configuration-services?idProperty=…`: every service of the catalogue,
 * in the order of the categories and then of the services, with the name in the language of
 * `Accept-Language`; none offered yet. 401 without a valid access token; 403 for a missing or
 * unknown property.
 */
export const bathhouseConfigurationServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  if (!propertyOf(request.params.get('idProperty'))) {
    return forbidden(request);
  }
  const language = SERVICE_LANGUAGES.indexOf(
    request.headers.get('Accept-Language') as (typeof SERVICE_LANGUAGES)[number],
  );
  const services: BathhouseServiceItem[] = SERVICES.map(({ categoryCode, icon, names }, index) => ({
    idService: index + 1,
    name: names[Math.max(language, 0)],
    categoryCode,
    iconUrl: `/assets/images/BathhouseConfiguration_${icon}.svg`,
    isSelected: false,
  }));
  return of(new HttpResponse({ status: 200, url: request.url, body: services })).pipe(delay(300));
};

/**
 * `POST /api/catalog/save-bathhouse-configuration-general` with every field of the tab and
 * `idProperty`: keeps them (until the page is reloaded). 204 without a body; 401 without a valid
 * access token; 403 for a missing or unknown property.
 */
export const saveBathhouseConfigurationGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<BathhouseGeneralInformation> & {
    idProperty?: unknown;
  };
  if (!propertyOf(body.idProperty)) {
    return forbidden(request);
  }
  generalInformation = onlyFieldsOf(BathhouseGeneralInformation, {
    ...generalInformation,
    ...body,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};
