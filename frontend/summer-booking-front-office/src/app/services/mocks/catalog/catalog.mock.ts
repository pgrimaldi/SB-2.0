import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { BarServiceCategory } from '../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../entities/enums/bar-service-icon';
import { BookingTimeInterval } from '../../../entities/enums/booking-time-interval';
import { StandardServiceIcon } from '../../../entities/enums/standard-service-icon';
import { TableServiceAvailabilityMode } from '../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../entities/enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../../entities/enums/table-service-deposit-mode';
import { TableServiceMealDuration } from '../../../entities/enums/table-service-meal-duration';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { AutomaticPriceAddItemRequest } from '../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceItem } from '../../../entities/settings/services/automatic-price-item';
import { AutomaticPriceSelectedGeneralInfo } from '../../../entities/settings/services/automatic-price-selected-general-info';
import { AutomaticPriceSelectedPricesValidity } from '../../../entities/settings/services/automatic-price-selected-prices-validity';
import { AutomaticPriceServices } from '../../../entities/settings/services/automatic-price-services';
import { BarServiceAddItemRequest } from '../../../entities/settings/services/bar-service-add-item-request';
import { BarServiceItem } from '../../../entities/settings/services/bar-service-item';
import { BarServiceSelectedGeneralInfo } from '../../../entities/settings/services/bar-service-selected-general-info';
import { BarServiceSelectedPrices } from '../../../entities/settings/services/bar-service-selected-prices';
import { DeleteBarServiceItemsRequest } from '../../../entities/settings/services/delete-bar-service-items-request';
import { DuplicateBarServiceItemsRequest } from '../../../entities/settings/services/duplicate-bar-service-items-request';
import { DeleteStandardServiceItemsRequest } from '../../../entities/settings/services/delete-standard-service-items-request';
import { DuplicateStandardServiceItemsRequest } from '../../../entities/settings/services/duplicate-standard-service-items-request';
import { DeleteHourlyServiceItemsRequest } from '../../../entities/settings/services/delete-hourly-service-items-request';
import { DuplicateHourlyServiceItemsRequest } from '../../../entities/settings/services/duplicate-hourly-service-items-request';
import { DeleteAutomaticPriceItemsRequest } from '../../../entities/settings/services/delete-automatic-price-items-request';
import { DuplicateAutomaticPriceItemsRequest } from '../../../entities/settings/services/duplicate-automatic-price-items-request';
import { DeleteTableServiceItemsRequest } from '../../../entities/settings/services/delete-table-service-items-request';
import { DuplicateTableServiceItemsRequest } from '../../../entities/settings/services/duplicate-table-service-items-request';
import { ServiceTimeSlot } from '../../../entities/enums/service-time-slot';
import { HourlyServiceAddItemRequest } from '../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceBookingOption } from '../../../entities/settings/services/hourly-service-booking-option';
import { HourlyServiceItem } from '../../../entities/settings/services/hourly-service-item';
import { HourlyServiceSelectedGeneralInfo } from '../../../entities/settings/services/hourly-service-selected-general-info';
import { HourlyServiceSelectedPrices } from '../../../entities/settings/services/hourly-service-selected-prices';
import { ServiceAvailabilityOption } from '../../../entities/settings/services/service-availability-option';
import { ServicePeriodAvailability } from '../../../entities/settings/services/service-period-availability';
import { ServiceTranslation } from '../../../entities/settings/services/service-translation';
import { StandardServiceAddItemRequest } from '../../../entities/settings/services/standard-service-add-item-request';
import { StandardServiceItem } from '../../../entities/settings/services/standard-service-item';
import { StandardServiceSelectedGeneralInfo } from '../../../entities/settings/services/standard-service-selected-general-info';
import { StandardServiceSelectedPrices } from '../../../entities/settings/services/standard-service-selected-prices';
import { TableServiceAddItemRequest } from '../../../entities/settings/services/table-service-add-item-request';
import { TableServiceItem } from '../../../entities/settings/services/table-service-item';
import { TableServiceSelectedAvailabilityBookings } from '../../../entities/settings/services/table-service-selected-availability-bookings';
import { TableServiceSelectedGeneralInfo } from '../../../entities/settings/services/table-service-selected-general-info';
import { Language } from '../../../entities/shared/language';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, forbidden, problem, resourceNotFound } from '../errors/problem.mock';
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
  {
    categoryCode: 'sport_activities',
    icon: 'Soccer',
    names: [
      'Beach soccer',
      'Beach soccer',
      'Beach soccer',
      'Fútbol playa',
      'Beachsoccer',
      '沙滩足球',
      'كرة القدم الشاطئية',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Volleyball',
    names: [
      'Beach volley',
      'Beach volleyball',
      'Beach-volley',
      'Vóley playa',
      'Beachvolleyball',
      '沙滩排球',
      'الكرة الطائرة الشاطئية',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Tennis',
    names: [
      'Beach tennis',
      'Beach tennis',
      'Beach tennis',
      'Tenis playa',
      'Beachtennis',
      '沙滩网球',
      'تنس الشاطئ',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'TableFootball',
    names: [
      'Calcio balilla',
      'Table football',
      'Baby-foot',
      'Futbolín',
      'Tischkicker',
      '桌上足球',
      'كرة قدم الطاولة',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Luggage',
    names: ['Escursioni', 'Excursions', 'Excursions', 'Excursiones', 'Ausflüge', '远足', 'رحلات'],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Celebration',
    names: [
      'Animazione',
      'Entertainment',
      'Animation',
      'Animación',
      'Animation',
      '娱乐活动',
      'أنشطة ترفيهية',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Kitesurfing',
    names: [
      'Kitesurf',
      'Kitesurfing',
      'Kitesurf',
      'Kitesurf',
      'Kitesurfen',
      '风筝冲浪',
      'كايت سيرف',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Surfing',
    names: ['Surf', 'Surfing', 'Surf', 'Surf', 'Surfen', '冲浪', 'ركوب الأمواج'],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Canoe',
    names: ['Canoa', 'Canoeing', 'Canoë', 'Canoa', 'Kanu', '皮划艇', 'قوارب الكانو'],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'JetSki',
    names: [
      'Moto d’acqua',
      'Jet ski',
      'Jet-ski',
      'Moto acuática',
      'Jetski',
      '水上摩托',
      'دراجات مائية',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Motorboat',
    names: [
      'Motoscafo',
      'Motorboat',
      'Bateau à moteur',
      'Lancha motora',
      'Motorboot',
      '摩托艇',
      'قارب بمحرك',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Snorkel',
    names: [
      'Sub/snorkeling',
      'Diving/snorkelling',
      'Plongée/snorkeling',
      'Buceo/snorkel',
      'Tauchen/Schnorcheln',
      '潜水/浮潜',
      'غوص/سنوركل',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'PedalBoat',
    names: ['Pedalò', 'Pedal boat', 'Pédalo', 'Hidropedal', 'Tretboot', '脚踏船', 'قارب بدواسات'],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Pool',
    names: ['Piscina', 'Swimming pool', 'Piscine', 'Piscina', 'Pool', '游泳池', 'مسبح'],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'HotTub',
    names: [
      'Idromassaggio',
      'Hot tub',
      'Bain à remous',
      'Hidromasaje',
      'Whirlpool',
      '按摩浴池',
      'جاكوزي',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Playground',
    names: [
      'Area giochi',
      'Play area',
      'Aire de jeux',
      'Zona de juegos',
      'Spielplatz',
      '游乐区',
      'منطقة ألعاب',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'Anchor',
    names: [
      'Posto barca',
      'Boat mooring',
      "Place d'amarrage",
      'Amarre',
      'Bootsliegeplatz',
      '泊船位',
      'مرسى قوارب',
    ],
  },
  {
    categoryCode: 'sport_activities',
    icon: 'FitnessCenter',
    names: [
      'Palestra',
      'Gym',
      'Salle de sport',
      'Gimnasio',
      'Fitnessraum',
      '健身房',
      'صالة رياضية',
    ],
  },
  {
    categoryCode: 'food_extra',
    icon: 'Cafe',
    names: ['Bar', 'Bar', 'Bar', 'Bar', 'Bar', '酒吧', 'بار'],
  },
  {
    categoryCode: 'food_extra',
    icon: 'Restaurant',
    names: ['Ristorante', 'Restaurant', 'Restaurant', 'Restaurante', 'Restaurant', '餐厅', 'مطعم'],
  },
  {
    categoryCode: 'food_extra',
    icon: 'Pizza',
    names: ['Pizzeria', 'Pizzeria', 'Pizzeria', 'Pizzería', 'Pizzeria', '披萨店', 'مطعم بيتزا'],
  },
  {
    categoryCode: 'food_extra',
    icon: 'Cocktail',
    names: [
      'Happy hour',
      'Happy hour',
      'Happy hour',
      'Happy hour',
      'Happy Hour',
      '欢乐时光',
      'ساعة سعيدة',
    ],
  },
  {
    categoryCode: 'food_extra',
    icon: 'Leaf',
    names: [
      'Cibo vegano',
      'Vegan food',
      'Cuisine végane',
      'Comida vegana',
      'Veganes Essen',
      '纯素食品',
      'طعام نباتي',
    ],
  },
  {
    categoryCode: 'food_extra',
    icon: 'GlutenFree',
    names: [
      'Cibo senza glutine',
      'Gluten-free food',
      'Cuisine sans gluten',
      'Comida sin gluten',
      'Glutenfreies Essen',
      '无麸质食品',
      'طعام خالٍ من الغلوتين',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Pets',
    names: [
      'Animali ammessi',
      'Pets allowed',
      'Animaux acceptés',
      'Se admiten mascotas',
      'Haustiere erlaubt',
      '允许携带宠物',
      'يُسمح بالحيوانات الأليفة',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Person',
    names: [
      'Spiaggia per single',
      'Singles beach',
      'Plage pour célibataires',
      'Playa para solteros',
      'Singlestrand',
      '单身海滩',
      'شاطئ للعزاب',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'BeachUmbrella',
    names: [
      'Servizio spiaggia',
      'Beach service',
      'Service de plage',
      'Servicio de playa',
      'Strandservice',
      '海滩服务',
      'خدمة الشاطئ',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Rainbow',
    names: [
      'Spiaggia LGBT',
      'LGBT beach',
      'Plage LGBT',
      'Playa LGBT',
      'LGBT-Strand',
      'LGBT 海滩',
      'شاطئ مجتمع الميم',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'VolumeOff',
    names: [
      'Spiaggia del silenzio',
      'Quiet beach',
      'Plage du silence',
      'Playa del silencio',
      'Ruhestrand',
      '静音海滩',
      'شاطئ هادئ',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Family',
    names: [
      'Spiaggia per famiglie',
      'Family beach',
      'Plage familiale',
      'Playa familiar',
      'Familienstrand',
      '家庭海滩',
      'شاطئ عائلي',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'PalmTree',
    names: [
      'Spiaggia naturista',
      'Naturist beach',
      'Plage naturiste',
      'Playa nudista',
      'FKK-Strand',
      '天体海滩',
      'شاطئ للعراة',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Inventory',
    names: [
      'Pacchetti all inclusive',
      'All-inclusive packages',
      'Formules tout compris',
      'Paquetes todo incluido',
      'All-inclusive-Pakete',
      '全包套餐',
      'باقات شاملة',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Elderly',
    names: [
      'Spiaggia per anziani',
      'Beach for seniors',
      'Plage pour seniors',
      'Playa para mayores',
      'Seniorenstrand',
      '老年人海滩',
      'شاطئ لكبار السن',
    ],
  },
  {
    categoryCode: 'beach',
    icon: 'Fan',
    names: [
      'Ventilatore sotto l’ombrellone',
      'Fan under the umbrella',
      'Ventilateur sous le parasol',
      'Ventilador bajo la sombrilla',
      'Ventilator unter dem Sonnenschirm',
      '遮阳伞下风扇',
      'مروحة تحت المظلة',
    ],
  },
];

let generalInformation = new BathhouseGeneralInformation();
let idOfferedServices = new Set<number>();

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
 * `Accept-Language`; offered as last saved, none at the start. 401 without a valid access token;
 * 403 for a missing or unknown property.
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
    isSelected: idOfferedServices.has(index + 1),
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

/**
 * `POST /api/catalog/save-bathhouse-configuration-services` with `idProperty` and `services`, the
 * services the property offers: keeps them (until the page is reloaded). 204 without a body; 401
 * without a valid access token; 403 for a missing or unknown property; 404 for a service that does
 * not exist.
 */
export const saveBathhouseConfigurationServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idProperty?: unknown; services?: unknown };
  if (!propertyOf(body.idProperty)) {
    return forbidden(request);
  }
  const services = Array.isArray(body.services) ? (body.services as BathhouseServiceItem[]) : [];
  const idServices = services.map(({ idService }) => idService);
  if (!idServices.every(isServiceId)) {
    return resourceNotFound(request);
  }
  idOfferedServices = new Set(idServices);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function isServiceId(value: unknown): value is number {
  return (
    typeof value === 'number' && Number.isInteger(value) && value >= 1 && value <= SERVICES.length
  );
}

/**
 * `GET /api/catalog/bathhouse-configuration-media?idProperty=…`: links of the photos (the main one
 * and the gallery, in order), of the virtual tour and of the two PDF documents. Example data until
 * the backend decides how files are kept. 401 without a valid access token; 403 for a missing or
 * unknown property.
 */
export const bathhouseConfigurationMediaMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  if (!propertyOf(request.params.get('idProperty'))) {
    return forbidden(request);
  }
  const media: BathhouseMedia = {
    photos: [
      { idPhoto: 1, url: '/assets/images/hub/hero-beach-desktop.webp', isMain: true },
      { idPhoto: 2, url: '/assets/images/hub/hero-beach-mobile-863.webp', isMain: false },
      { idPhoto: 3, url: '/assets/images/hub/hero-beach-mobile-1290.webp', isMain: false },
    ],
    virtualTourUrl: 'https://tour.example.com/lido-demo',
    priceListPdfUrl: 'https://files.example.com/lido-demo/listino-prezzi.pdf',
    menuPdfUrl: null,
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: media })).pipe(delay(300));
};

type BarServiceRow = BarServiceItem & { propertyId: number };

const BAR_SERVICES: BarServiceRow[] = (
  [
    ['01', 'Caffè', BarServiceCategory.Cafeteria, 1],
    ['02', 'Croissant', BarServiceCategory.Desserts, 1.4],
    ['03', 'Panino', BarServiceCategory.SnacksAndSandwiches, 3],
    ['04', 'Succo di frutta', BarServiceCategory.Drinks, 2],
    ['05', 'Pizza', BarServiceCategory.SnacksAndSandwiches, 3],
    ['06', 'Sfoglia', BarServiceCategory.Desserts, 1.3],
    ['07', 'Bibita lattina', BarServiceCategory.Drinks, 2.5],
    ['08', 'Acqua naturale 1L', BarServiceCategory.Drinks, 1.2],
    ['09', 'Acqua frizzante 1L', BarServiceCategory.Drinks, 1.2],
    ['10', 'Schiacciata', BarServiceCategory.SnacksAndSandwiches, 3],
    ['11', 'Tramezzino', BarServiceCategory.SnacksAndSandwiches, 2.8],
    ['12', 'Gelato', BarServiceCategory.IceCreams, 2.5],
  ] as const
).map(([suffix, serviceName, category, price]) => ({
  propertyId: 1,
  idBarService: `8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c${suffix}`,
  serviceName,
  category,
  price,
}));

/**
 * The data of the add panel of a bar service, by id: those created with the panel and Caffè; the
 * other example rows get theirs from the list (see `detailsOf`).
 */
const BAR_SERVICE_DETAILS = new Map<string, BarServiceAddItemRequest>([
  [
    '8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01',
    Object.assign(new BarServiceAddItemRequest(), {
      translations: [
        { language: Language.It, serviceName: 'Caffè', description: 'Espresso in tazzina.' },
        { language: Language.En, serviceName: 'Coffee', description: 'Espresso in a small cup.' },
      ],
      category: BarServiceCategory.Cafeteria,
      icon: BarServiceIcon.Coffee,
      isHighlighted: true,
      price: 1,
      vatRate: 10,
      variants: [{ variantName: 'Doppio', price: 1.8 }],
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01', price: 1.2 }],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          managementStartTime: '08:00',
          managementEndTime: '12:00',
          onlineStartTime: '08:30',
          onlineEndTime: '11:30',
          maxBookingPerHourQuantity: 20,
          maxBookingPerDayQuantity: 80,
          timeInterval: BookingTimeInterval.ThirtyMinutes,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 4,
        },
      ],
    }),
  ],
]);

function detailsOf(row: BarServiceRow): BarServiceAddItemRequest {
  return (
    BAR_SERVICE_DETAILS.get(row.idBarService ?? '') ??
    Object.assign(new BarServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: row.serviceName,
          description: 'Servito al bar dello stabilimento.',
        },
      ],
      category: row.category,
      price: row.price,
      vatRate: 10,
    })
  );
}

/** The bar service of the `idBarService` of the query string, if it exists. */
function requestedBarService(request: HttpRequest<unknown>): BarServiceRow | undefined {
  const idBarService = request.params.get('idBarService');
  return BAR_SERVICES.find((row) => row.idBarService === idBarService);
}

/**
 * `GET /api/catalog/get-bar-service-general-info?idBarService=…`: the Info Generali tab of the edit
 * panel. 200 with `BarServiceSelectedGeneralInfo`, only the languages written; 401 without a valid
 * access token; 404 `resource.not_found` for an unknown id.
 */
export const barServiceGeneralInfoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedBarService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = detailsOf(row);
  const answer: BarServiceSelectedGeneralInfo = {
    idBarService: row.idBarService,
    translations: details.translations.filter(
      ({ serviceName, description }) => serviceName || description,
    ),
    category: details.category,
    icon: details.icon,
    isHighlighted: details.isHighlighted,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-bar-service-prices?idBarService=…`: the Prezzi tab of the edit panel. 200 with
 * `BarServiceSelectedPrices`, a price only for the periods that have one; 401 without a valid access
 * token; 404 `resource.not_found` for an unknown id.
 */
export const barServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedBarService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = detailsOf(row);
  const answer: BarServiceSelectedPrices = {
    price: details.price,
    vatRate: details.vatRate,
    variants: details.variants,
    isPeriodPricingEnabled: details.isPeriodPricingEnabled,
    periodPrices: details.periodPrices.filter(({ price }) => price !== null),
    idSectors: details.idSectors,
    isOnlinePurchaseEnabled: details.isOnlinePurchaseEnabled,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-bar-service-booking-options?idBarService=…`: the Prenotazioni tab of the
 * edit panel, the array of the booking options (empty when there are none). 401 without a valid
 * access token; 404 `resource.not_found` for an unknown id.
 */
export const barServiceBookingOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedBarService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  return of(
    new HttpResponse({
      status: 200,
      url: request.url,
      body: structuredClone(detailsOf(row).bookingOptions),
    }),
  ).pipe(delay(300));
};

const MAX_PAGE_SIZE = 100;
const BAR_SERVICE_SORT_FIELDS = ['serviceName', 'category', 'price'] as const;

/**
 * `POST /api/catalog/bar-service-list`: one page of the bar services of the requested property,
 * searched in the name. 401 without a valid access token; 403 for a missing or unknown property.
 */
export const barServiceListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = BAR_SERVICES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.serviceName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(BarServiceItem, row));
  const field = BAR_SERVICE_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<BarServiceItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

type CreateBarServiceBody = Partial<BarServiceAddItemRequest> & { idProperty?: unknown };

/**
 * `POST /api/catalog/create-bar-service` with `BarServiceAddItemRequest` and `idProperty`: the new
 * bar service goes into the list, with the name in the language of `Accept-Language`. 204 without a
 * body; 401 without a valid access token; 403 for an unknown property; 400 with one error per wrong
 * field (see `barServiceErrors`).
 */
export const createBarServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateBarServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = barServiceErrors(body, language);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const idBarService = crypto.randomUUID();
  BAR_SERVICE_DETAILS.set(
    idBarService,
    structuredClone(onlyFieldsOf(BarServiceAddItemRequest, body as BarServiceAddItemRequest)),
  );
  BAR_SERVICES.push({
    propertyId: property.id,
    idBarService,
    serviceName: main?.serviceName?.trim() ?? null,
    category: body.category ?? null,
    price: body.price ?? null,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function barServiceErrors(body: CreateBarServiceBody, language: string): MockFieldError[] {
  return [...generalErrors(body, language), ...priceErrors(body)];
}

/**
 * Name and description in the language of the request (the user's), as the panel requires; category
 * and icon, when given, among the values of their enums.
 */
function generalErrors(body: CreateBarServiceBody, language: string): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const translations = Array.isArray(body.translations) ? body.translations : [];
  const index = translations.findIndex((translation) => translation.language === language);
  const main = translations[index];
  const at = `translations[${Math.max(index, 0)}]`;
  if (typeof main?.serviceName !== 'string' || !main.serviceName.trim()) {
    errors.push({ field: `${at}.serviceName`, code: 'validation.required' });
  }
  if (typeof main?.description !== 'string' || !main.description.trim()) {
    errors.push({ field: `${at}.description`, code: 'validation.required' });
  }
  if (body.category != null && !Object.values(BarServiceCategory).includes(body.category)) {
    errors.push({ field: 'category', code: 'validation.invalid' });
  }
  if (body.icon != null && !Object.values(BarServiceIcon).includes(body.icon)) {
    errors.push({ field: 'icon', code: 'validation.invalid' });
  }
  return errors;
}

/** A price, when given, not below 0. */
function priceErrors(body: { price?: number | null }): MockFieldError[] {
  return body.price != null && !(typeof body.price === 'number' && body.price >= 0)
    ? [{ field: 'price', code: 'validation.invalid' }]
    : [];
}

type UpdateBarServiceBody = CreateBarServiceBody & { idBarService?: unknown };

/**
 * The common part of the three updates of the edit panel: 401 without a valid access token; 403 for
 * an unknown property; 404 `resource.not_found` for an `idBarService` that is not one of its bar
 * services; 400 with one error per wrong field; otherwise `apply` changes the data of its tab and
 * the answer is 204 without a body.
 */
function updateBarService(
  request: HttpRequest<unknown>,
  errorsOf: (body: UpdateBarServiceBody) => MockFieldError[],
  apply: (
    details: BarServiceAddItemRequest,
    body: UpdateBarServiceBody,
    row: BarServiceRow,
  ) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateBarServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const row = BAR_SERVICES.find(
    (item) => item.idBarService === body.idBarService && item.propertyId === property.id,
  );
  if (!row?.idBarService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const details = structuredClone(detailsOf(row));
  apply(details, structuredClone(body), row);
  BAR_SERVICE_DETAILS.set(row.idBarService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-bar-service-general` with `BarServiceUpdateGeneralRequest` and
 * `idProperty`: names, descriptions, category, icon and Metti in evidenza; the list shows the new name
 * (language of `Accept-Language`) and category. Errors as the creation (see `generalErrors`).
 */
export const updateBarServiceGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const language = request.headers.get('Accept-Language') ?? 'it';
  return updateBarService(
    request,
    (body) => generalErrors(body, language),
    (details, body, row) => {
      details.translations = body.translations ?? [];
      details.category = body.category ?? null;
      details.icon = body.icon ?? null;
      details.isHighlighted = body.isHighlighted === true;
      const main = details.translations.find((translation) => translation.language === language);
      row.serviceName = main?.serviceName?.trim() ?? row.serviceName;
      row.category = details.category;
    },
  );
};

/**
 * `POST /api/catalog/update-bar-service-prices` with `BarServiceUpdatePricesRequest` and `idProperty`:
 * the Prezzi tab; the list shows the new price. 400 for a price below 0.
 */
export const updateBarServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateBarService(request, priceErrors, (details, body, row) => {
    details.price = body.price ?? null;
    details.vatRate = body.vatRate ?? null;
    details.variants = body.variants ?? [];
    details.isPeriodPricingEnabled = body.isPeriodPricingEnabled === true;
    details.periodPrices = body.periodPrices ?? [];
    details.idSectors = body.idSectors ?? [];
    details.isOnlinePurchaseEnabled = body.isOnlinePurchaseEnabled === true;
    row.price = details.price;
  });

/**
 * `POST /api/catalog/update-bar-service-booking` with `BarServiceUpdateBookingRequest` and
 * `idProperty`: the booking options of the Prenotazioni tab, all of them.
 */
export const updateBarServiceBookingMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateBarService(
    request,
    () => [],
    (details, body) => {
      details.bookingOptions = body.bookingOptions ?? [];
    },
  );

/**
 * `POST /api/catalog/delete-bar-service-item` with `DeleteBarServiceItemsRequest`: all or nothing. If
 * one id is not a bar service of the property nothing is deleted (404 `resource.not_found`). Repeated
 * ids count once. 204 without a body; 401 without a valid access token; 403 for an unknown property.
 */
export const deleteBarServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteBarServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const barServices = barServicesOf(property.id, body.idBarServices);
  if (!barServices) {
    return resourceNotFound(request);
  }
  for (const barService of barServices) {
    BAR_SERVICES.splice(BAR_SERVICES.indexOf(barService), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-bar-service-item` with `DuplicateBarServiceItemsRequest`: all or
 * nothing, like the delete. Each copy keeps the price; the name is chosen by the backend: the mock
 * invents "Caffè (copia)", then "Caffè (copia 2)" and so on.
 */
export const duplicateBarServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateBarServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const barServices = barServicesOf(property.id, body.idBarServices);
  if (!barServices) {
    return resourceNotFound(request);
  }
  for (const barService of barServices) {
    BAR_SERVICES.push({
      ...barService,
      idBarService: crypto.randomUUID(),
      serviceName: barServiceCopyName(barService),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The bar services of the property for `idBarServices` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function barServicesOf(propertyId: number, idBarServices: unknown): BarServiceRow[] | null {
  const ids = Array.isArray(idBarServices) ? [...new Set(idBarServices)] : [];
  const barServices = ids
    .map((id) =>
      BAR_SERVICES.find((row) => row.idBarService === id && row.propertyId === propertyId),
    )
    .filter((barService): barService is BarServiceRow => barService !== undefined);
  return ids.length && barServices.length === ids.length ? barServices : null;
}

function barServiceCopyName(barService: BarServiceRow): string {
  const taken = new Set(
    BAR_SERVICES.filter((row) => row.propertyId === barService.propertyId).map(
      (row) => row.serviceName,
    ),
  );
  let copy = `${barService.serviceName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${barService.serviceName} (copia ${number})`;
  }
  return copy;
}

type StandardServiceRow = StandardServiceItem & { propertyId: number };

const STANDARD_SERVICES: StandardServiceRow[] = (
  [
    ['01', 'Doccia calda', 3],
    ['02', 'Cassetta di Sicurezza', 3],
    ['03', 'Parcheggio', 3],
  ] as const
).map(([suffix, serviceName, price]) => ({
  propertyId: 1,
  idStandardService: `9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d${suffix}`,
  serviceName,
  price,
}));

const STANDARD_SERVICE_SORT_FIELDS = ['serviceName', 'price'] as const;

/**
 * `POST /api/catalog/standard-service-list`: one page of the services (neither bar nor hourly) of
 * the requested property, searched in the name. 401 without a valid access token; 403 for a missing
 * or unknown property.
 */
export const standardServiceListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = STANDARD_SERVICES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.serviceName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(StandardServiceItem, row));
  const field = STANDARD_SERVICE_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<StandardServiceItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * The data of the add panel of a service, by id: those created with the panel and Doccia calda; the
 * other example rows get theirs from the list (see `standardDetailsOf`).
 */
const STANDARD_SERVICE_DETAILS = new Map<string, StandardServiceAddItemRequest>([
  [
    '9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01',
    Object.assign(new StandardServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: 'Doccia calda',
          description: 'Acqua calda nelle docce dello stabilimento.',
        },
        {
          language: Language.En,
          serviceName: 'Hot shower',
          description: 'Hot water in the showers of the beach club.',
        },
      ],
      icon: StandardServiceIcon.Shower,
      isHighlighted: true,
      price: 3,
      vatRate: 22,
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01', price: 2.5 }],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        {
          dateFrom: '2026-06-15T00:00:00.000Z',
          dateTo: '2026-09-15T00:00:00.000Z',
          managementStartTime: '09:00',
          managementEndTime: '19:00',
          onlineStartTime: '09:00',
          onlineEndTime: '18:00',
          maxBookingPerHourQuantity: 6,
          maxBookingPerDayQuantity: 50,
          timeInterval: BookingTimeInterval.FifteenMinutes,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 2,
        },
      ],
    }),
  ],
]);

function standardDetailsOf(row: StandardServiceRow): StandardServiceAddItemRequest {
  return (
    STANDARD_SERVICE_DETAILS.get(row.idStandardService ?? '') ??
    Object.assign(new StandardServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: row.serviceName,
          description: 'Servizio offerto dallo stabilimento.',
        },
      ],
      price: row.price,
      vatRate: 22,
    })
  );
}

/** The service of the `idStandardService` of the query string, if it exists. */
function requestedStandardService(request: HttpRequest<unknown>): StandardServiceRow | undefined {
  const idStandardService = request.params.get('idStandardService');
  return STANDARD_SERVICES.find((row) => row.idStandardService === idStandardService);
}

/**
 * `GET /api/catalog/get-standard-service-general-info?idStandardService=…`: the Info Generali tab of
 * the edit panel. 200 with `StandardServiceSelectedGeneralInfo`, only the languages written; 401
 * without a valid access token; 404 `resource.not_found` for an unknown id.
 */
export const standardServiceGeneralInfoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedStandardService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = standardDetailsOf(row);
  const answer: StandardServiceSelectedGeneralInfo = {
    idStandardService: row.idStandardService,
    translations: details.translations.filter(
      ({ serviceName, description }) => serviceName || description,
    ),
    icon: details.icon,
    isHighlighted: details.isHighlighted,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-standard-service-prices?idStandardService=…`: the Prezzi tab of the edit
 * panel. 200 with `StandardServiceSelectedPrices`, a price only for the periods that have one; 401
 * without a valid access token; 404 `resource.not_found` for an unknown id.
 */
export const standardServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedStandardService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = standardDetailsOf(row);
  const answer: StandardServiceSelectedPrices = {
    price: details.price,
    vatRate: details.vatRate,
    isPeriodPricingEnabled: details.isPeriodPricingEnabled,
    periodPrices: details.periodPrices.filter(({ price }) => price !== null),
    idSectors: details.idSectors,
    isOnlinePurchaseEnabled: details.isOnlinePurchaseEnabled,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-standard-service-booking-options?idStandardService=…`: the Prenotazioni tab
 * of the edit panel, the array of the booking options (empty when there are none). 401 without a
 * valid access token; 404 `resource.not_found` for an unknown id.
 */
export const standardServiceBookingOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedStandardService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  return of(
    new HttpResponse({
      status: 200,
      url: request.url,
      body: structuredClone(standardDetailsOf(row).bookingOptions),
    }),
  ).pipe(delay(300));
};

type CreateStandardServiceBody = Partial<StandardServiceAddItemRequest> & { idProperty?: unknown };

/**
 * `POST /api/catalog/create-standard-service` with `StandardServiceAddItemRequest` and `idProperty`:
 * the new service goes into the list, with the name in the language of `Accept-Language`. 204 without
 * a body; 401 without a valid access token; 403 for an unknown property; 400 with one error per wrong
 * field (see `standardGeneralErrors` and `priceErrors`).
 */
export const createStandardServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateStandardServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [...standardGeneralErrors(body, language), ...priceErrors(body)];
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const idStandardService = crypto.randomUUID();
  STANDARD_SERVICE_DETAILS.set(
    idStandardService,
    structuredClone(
      onlyFieldsOf(StandardServiceAddItemRequest, body as StandardServiceAddItemRequest),
    ),
  );
  STANDARD_SERVICES.push({
    propertyId: property.id,
    idStandardService,
    serviceName: main?.serviceName?.trim() ?? null,
    price: body.price ?? null,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * Name and description in the language of the request (the user's), as the panel requires; the
 * icon, when given, among the values of its enum. Also for the hourly services, which have the same
 * icons.
 */
function standardGeneralErrors(
  body: { translations?: ServiceTranslation[]; icon?: StandardServiceIcon | null },
  language: string,
): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const translations = Array.isArray(body.translations) ? body.translations : [];
  const index = translations.findIndex((translation) => translation.language === language);
  const main = translations[index];
  const at = `translations[${Math.max(index, 0)}]`;
  if (typeof main?.serviceName !== 'string' || !main.serviceName.trim()) {
    errors.push({ field: `${at}.serviceName`, code: 'validation.required' });
  }
  if (typeof main?.description !== 'string' || !main.description.trim()) {
    errors.push({ field: `${at}.description`, code: 'validation.required' });
  }
  if (body.icon != null && !Object.values(StandardServiceIcon).includes(body.icon)) {
    errors.push({ field: 'icon', code: 'validation.invalid' });
  }
  return errors;
}

type UpdateStandardServiceBody = CreateStandardServiceBody & { idStandardService?: unknown };

/**
 * The common part of the three updates of the edit panel: 401 without a valid access token; 403 for
 * an unknown property; 404 `resource.not_found` for an `idStandardService` that is not one of its
 * services; 400 with one error per wrong field; otherwise `apply` changes the data of its tab and the
 * answer is 204 without a body.
 */
function updateStandardService(
  request: HttpRequest<unknown>,
  errorsOf: (body: UpdateStandardServiceBody) => MockFieldError[],
  apply: (
    details: StandardServiceAddItemRequest,
    body: UpdateStandardServiceBody,
    row: StandardServiceRow,
  ) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateStandardServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const row = STANDARD_SERVICES.find(
    (item) => item.idStandardService === body.idStandardService && item.propertyId === property.id,
  );
  if (!row?.idStandardService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const details = structuredClone(standardDetailsOf(row));
  apply(details, structuredClone(body), row);
  STANDARD_SERVICE_DETAILS.set(row.idStandardService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-standard-service-general` with `StandardServiceUpdateGeneralRequest` and
 * `idProperty`: names, descriptions, icon and Metti in evidenza; the list shows the new name (language
 * of `Accept-Language`). Errors as the creation (see `standardGeneralErrors`).
 */
export const updateStandardServiceGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const language = request.headers.get('Accept-Language') ?? 'it';
  return updateStandardService(
    request,
    (body) => standardGeneralErrors(body, language),
    (details, body, row) => {
      details.translations = body.translations ?? [];
      details.icon = body.icon ?? null;
      details.isHighlighted = body.isHighlighted === true;
      const main = details.translations.find((translation) => translation.language === language);
      row.serviceName = main?.serviceName?.trim() ?? row.serviceName;
    },
  );
};

/**
 * `POST /api/catalog/update-standard-service-prices` with `StandardServiceUpdatePricesRequest` and
 * `idProperty`: the Prezzi tab; the list shows the new price. 400 for a price below 0.
 */
export const updateStandardServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateStandardService(request, priceErrors, (details, body, row) => {
    details.price = body.price ?? null;
    details.vatRate = body.vatRate ?? null;
    details.isPeriodPricingEnabled = body.isPeriodPricingEnabled === true;
    details.periodPrices = body.periodPrices ?? [];
    details.idSectors = body.idSectors ?? [];
    details.isOnlinePurchaseEnabled = body.isOnlinePurchaseEnabled === true;
    row.price = details.price;
  });

/**
 * `POST /api/catalog/update-standard-service-booking` with `StandardServiceUpdateBookingRequest` and
 * `idProperty`: the booking options of the Prenotazioni tab, all of them.
 */
export const updateStandardServiceBookingMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateStandardService(
    request,
    () => [],
    (details, body) => {
      details.bookingOptions = body.bookingOptions ?? [];
    },
  );

type HourlyServiceRow = HourlyServiceItem & { propertyId: number };

const HOURLY_SERVICES: HourlyServiceRow[] = (
  [
    ['01', 'Sup'],
    ['02', 'Pedalò'],
    ['03', 'Piscina'],
    ['04', 'Idromassaggio'],
  ] as const
).map(([suffix, serviceName]) => ({
  propertyId: 1,
  idHourlyService: `4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a${suffix}`,
  serviceName,
}));

const HOURLY_SERVICE_SORT_FIELDS = ['serviceName'] as const;

/**
 * `POST /api/catalog/hourly-service-list`: one page of the hourly services of the requested
 * property, searched in the name. 401 without a valid access token; 403 for a missing or unknown
 * property.
 */
export const hourlyServiceListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = HOURLY_SERVICES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.serviceName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(HourlyServiceItem, row));
  const field = HOURLY_SERVICE_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<HourlyServiceItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/** An availability of the examples: every day from 9 to 19, four time slots and a custom one. */
function exampleAvailability(): ServiceAvailabilityOption {
  const prices: Partial<Record<ServiceTimeSlot, number>> = {
    [ServiceTimeSlot.TenMinutes]: 3,
    [ServiceTimeSlot.ThirtyMinutes]: 8,
    [ServiceTimeSlot.OneHour]: 15,
    [ServiceTimeSlot.TwoHours]: 25,
  };
  const option = Object.assign(new ServiceAvailabilityOption(), {
    startTime: '09:00',
    endTime: '19:00',
    daysOfWeek: [1, 2, 3, 4, 5, 6, 7],
    isCustomTimeSlotEnabled: true,
    customTimeSlotMinuteQuantity: 20,
    customTimeSlotPrice: 6,
    vatRate: 22,
    maxBookingPerTimeSlotQuantity: 4,
  });
  option.timeSlotPrices = option.timeSlotPrices.map((timeSlotPrice) => {
    const price = timeSlotPrice.timeSlot ? prices[timeSlotPrice.timeSlot] : undefined;
    return price === undefined ? timeSlotPrice : { ...timeSlotPrice, isEnabled: true, price };
  });
  return option;
}

/**
 * The data of the add panel of an hourly service, by id: those created with the panel and Sup; the
 * other example rows get theirs from the list (see `hourlyDetailsOf`).
 */
const HOURLY_SERVICE_DETAILS = new Map<string, HourlyServiceAddItemRequest>([
  [
    '4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01',
    Object.assign(new HourlyServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: 'Sup',
          description: 'Tavola da stand up paddle con pagaia, a noleggio a ore.',
        },
        {
          language: Language.En,
          serviceName: 'Sup',
          description: 'Stand up paddle board with paddle, rented by the hour.',
        },
      ],
      icon: StandardServiceIcon.Surfing,
      isPeriodPricingEnabled: true,
      periodAvailabilities: [
        {
          idPeriod: '3a7c1e52-8d4b-4f60-9e21-5b6a7c8d9e01',
          availabilityOptions: [exampleAvailability()],
        },
      ],
      idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        {
          dateFrom: '2026-06-01T00:00:00.000Z',
          dateTo: '2026-09-15T00:00:00.000Z',
          availabilityOption: exampleAvailability(),
        },
      ],
    }),
  ],
]);

function hourlyDetailsOf(row: HourlyServiceRow): HourlyServiceAddItemRequest {
  return (
    HOURLY_SERVICE_DETAILS.get(row.idHourlyService ?? '') ??
    Object.assign(new HourlyServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: row.serviceName,
          description: 'Servizio a ore offerto dallo stabilimento.',
        },
      ],
    })
  );
}

/** The hourly service of the `idHourlyService` of the query string, if it exists. */
function requestedHourlyService(request: HttpRequest<unknown>): HourlyServiceRow | undefined {
  const idHourlyService = request.params.get('idHourlyService');
  return HOURLY_SERVICES.find((row) => row.idHourlyService === idHourlyService);
}

/**
 * `GET /api/catalog/get-hourly-service-general-info?idHourlyService=…`: the Info Generali tab of the
 * edit panel. 200 with `HourlyServiceSelectedGeneralInfo`, only the languages written; 401 without a
 * valid access token; 404 `resource.not_found` for an unknown id.
 */
export const hourlyServiceGeneralInfoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedHourlyService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = hourlyDetailsOf(row);
  const answer: HourlyServiceSelectedGeneralInfo = {
    idHourlyService: row.idHourlyService,
    translations: details.translations.filter(
      ({ serviceName, description }) => serviceName || description,
    ),
    icon: details.icon,
    isHighlighted: details.isHighlighted,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-hourly-service-prices?idHourlyService=…`: the Prezzi tab of the edit panel.
 * 200 with `HourlyServiceSelectedPrices`, the availability only of the periods that have it; 401
 * without a valid access token; 404 `resource.not_found` for an unknown id.
 */
export const hourlyServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedHourlyService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = hourlyDetailsOf(row);
  const answer: HourlyServiceSelectedPrices = {
    isPeriodPricingEnabled: details.isPeriodPricingEnabled,
    periodAvailabilities: details.periodAvailabilities.filter(
      ({ availabilityOptions }) => availabilityOptions.length > 0,
    ),
    idSectors: details.idSectors,
    isOnlinePurchaseEnabled: details.isOnlinePurchaseEnabled,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-hourly-service-booking-options?idHourlyService=…`: the Prenotazioni tab of
 * the edit panel, the array of the booking options (empty when there are none). 401 without a valid
 * access token; 404 `resource.not_found` for an unknown id.
 */
export const hourlyServiceBookingOptionsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedHourlyService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  return of(
    new HttpResponse({
      status: 200,
      url: request.url,
      body: structuredClone(hourlyDetailsOf(row).bookingOptions),
    }),
  ).pipe(delay(300));
};

type CreateHourlyServiceBody = Partial<HourlyServiceAddItemRequest> & { idProperty?: unknown };

/**
 * `POST /api/catalog/create-hourly-service` with `HourlyServiceAddItemRequest` and `idProperty`: the
 * new hourly service goes into the list, with the name in the language of `Accept-Language`. 204
 * without a body; 401 without a valid access token; 403 for an unknown property; 400 with one error
 * per wrong field (see `standardGeneralErrors` and `hourlyPriceErrors`).
 */
export const createHourlyServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateHourlyServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [...standardGeneralErrors(body, language), ...hourlyPriceErrors(body)];
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const idHourlyService = crypto.randomUUID();
  HOURLY_SERVICE_DETAILS.set(
    idHourlyService,
    structuredClone(onlyFieldsOf(HourlyServiceAddItemRequest, body as HourlyServiceAddItemRequest)),
  );
  HOURLY_SERVICES.push({
    propertyId: property.id,
    idHourlyService,
    serviceName: main?.serviceName?.trim() ?? null,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** Every price of the time slots, of the periods and of the booking options, when given, not below 0. */
function hourlyPriceErrors(body: {
  periodAvailabilities?: ServicePeriodAvailability[];
  bookingOptions?: HourlyServiceBookingOption[];
}): MockFieldError[] {
  const errorsOf = (option: ServiceAvailabilityOption, at: string): MockFieldError[] => [
    ...option.timeSlotPrices.flatMap(({ price }, index) =>
      priceErrors({ price }).map((): MockFieldError => ({
        field: `${at}.timeSlotPrices[${index}].price`,
        code: 'validation.invalid',
      })),
    ),
    ...priceErrors({ price: option.customTimeSlotPrice }).map((): MockFieldError => ({
      field: `${at}.customTimeSlotPrice`,
      code: 'validation.invalid',
    })),
  ];
  return [
    ...(body.periodAvailabilities ?? []).flatMap((periodAvailability, periodIndex) =>
      periodAvailability.availabilityOptions.flatMap((option, optionIndex) =>
        errorsOf(
          option,
          `periodAvailabilities[${periodIndex}].availabilityOptions[${optionIndex}]`,
        ),
      ),
    ),
    ...(body.bookingOptions ?? []).flatMap((bookingOption, index) =>
      errorsOf(bookingOption.availabilityOption, `bookingOptions[${index}].availabilityOption`),
    ),
  ];
}

type UpdateHourlyServiceBody = CreateHourlyServiceBody & { idHourlyService?: unknown };

/**
 * The common part of the three updates of the edit panel: 401 without a valid access token; 403 for
 * an unknown property; 404 `resource.not_found` for an `idHourlyService` that is not one of its
 * hourly services; 400 with one error per wrong field; otherwise `apply` changes the data of its tab
 * and the answer is 204 without a body.
 */
function updateHourlyService(
  request: HttpRequest<unknown>,
  errorsOf: (body: UpdateHourlyServiceBody) => MockFieldError[],
  apply: (
    details: HourlyServiceAddItemRequest,
    body: UpdateHourlyServiceBody,
    row: HourlyServiceRow,
  ) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateHourlyServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const row = HOURLY_SERVICES.find(
    (item) => item.idHourlyService === body.idHourlyService && item.propertyId === property.id,
  );
  if (!row?.idHourlyService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const details = structuredClone(hourlyDetailsOf(row));
  apply(details, structuredClone(body), row);
  HOURLY_SERVICE_DETAILS.set(row.idHourlyService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-hourly-service-general` with `HourlyServiceUpdateGeneralRequest` and
 * `idProperty`: names, descriptions, icon and Metti in evidenza; the list shows the new name
 * (language of `Accept-Language`). Errors as the creation (see `standardGeneralErrors`).
 */
export const updateHourlyServiceGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const language = request.headers.get('Accept-Language') ?? 'it';
  return updateHourlyService(
    request,
    (body) => standardGeneralErrors(body, language),
    (details, body, row) => {
      details.translations = body.translations ?? [];
      details.icon = body.icon ?? null;
      details.isHighlighted = body.isHighlighted === true;
      const main = details.translations.find((translation) => translation.language === language);
      row.serviceName = main?.serviceName?.trim() ?? row.serviceName;
    },
  );
};

/**
 * `POST /api/catalog/update-hourly-service-prices` with `HourlyServiceUpdatePricesRequest` and
 * `idProperty`: the Prezzi tab. 400 for a price below 0.
 */
export const updateHourlyServicePricesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateHourlyService(
    request,
    (body) => hourlyPriceErrors({ periodAvailabilities: body.periodAvailabilities }),
    (details, body) => {
      details.isPeriodPricingEnabled = body.isPeriodPricingEnabled === true;
      details.periodAvailabilities = body.periodAvailabilities ?? [];
      details.idSectors = body.idSectors ?? [];
      details.isOnlinePurchaseEnabled = body.isOnlinePurchaseEnabled === true;
    },
  );

/**
 * `POST /api/catalog/update-hourly-service-booking` with `HourlyServiceUpdateBookingRequest` and
 * `idProperty`: the booking options of the Prenotazioni tab, all of them. 400 for a price below 0.
 */
export const updateHourlyServiceBookingMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateHourlyService(
    request,
    (body) => hourlyPriceErrors({ bookingOptions: body.bookingOptions }),
    (details, body) => {
      details.bookingOptions = body.bookingOptions ?? [];
    },
  );

type AutomaticPriceRow = AutomaticPriceItem & { propertyId: number };

const AUTOMATIC_PRICES: AutomaticPriceRow[] = [
  {
    propertyId: 1,
    idAutomaticPrice: '7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01',
    serviceName: 'Coperto',
    price: 2,
  },
];

const AUTOMATIC_PRICE_SORT_FIELDS = ['serviceName', 'price'] as const;

/**
 * The data of the add panel of an automatic price, by id: those created with the panel and Coperto;
 * the other example rows get theirs from the list (see `automaticDetailsOf`).
 */
const AUTOMATIC_PRICE_DETAILS = new Map<string, AutomaticPriceAddItemRequest>([
  [
    '7e9a1c3d-5b7f-4d9a-8c2e-6a8c0e2b4d01',
    Object.assign(new AutomaticPriceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: 'Coperto',
          description: 'Coperto a persona per i servizi del bar.',
        },
        {
          language: Language.En,
          serviceName: 'Cover charge',
          description: 'Cover charge per person for the bar services.',
        },
      ],
      icon: BarServiceIcon.Cutlery,
      price: 2,
      vatRate: 10,
      isOnlinePurchaseEnabled: true,
      validityPeriods: [
        {
          dateFrom: '2026-06-01T00:00:00.000Z',
          dateTo: '2026-09-30T00:00:00.000Z',
          daysOfWeek: [1, 2, 3, 4, 5, 6, 7],
          timeSlots: [
            { startTime: '12:00', endTime: '15:00' },
            { startTime: '19:00', endTime: '23:00' },
          ],
        },
      ],
      idBarServices: [
        '8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c01',
        '8a2c4e6f-1b3d-4f5a-9c7e-2d4f6a8b0c02',
      ],
      idStandardServices: ['9b3d5f7a-2c4e-4a6b-8d0f-3e5a7b9c1d01'],
      idHourlyServices: ['4c6e8a0b-3d5f-4b7c-9e1a-5f7b9d1e3a01'],
      isPricePerServiceEnabled: true,
      idSectors: ['5c2a7e10-3b4d-4e6f-8a9b-0c1d2e3f4a01'],
    }),
  ],
]);

/** The services of a property as `{ id, value }`, with the name. */
function serviceItemsOf<Row extends { propertyId: number; serviceName: string | null }>(
  rows: readonly Row[],
  propertyId: number,
  idOf: (row: Row) => string | null,
): ComboboxItem[] {
  return rows
    .filter((row) => row.propertyId === propertyId)
    .map((row) => ({ id: idOf(row) ?? '', value: row.serviceName ?? '' }));
}

/** Every id, when the list is given, of a service of the property. */
function areServicesOf<Row extends { propertyId: number }>(
  ids: unknown,
  rows: readonly Row[],
  propertyId: number,
  idOf: (row: Row) => string | null,
): boolean {
  return (
    !Array.isArray(ids) ||
    ids.every((id) => rows.some((row) => row.propertyId === propertyId && idOf(row) === id))
  );
}

/**
 * `GET /api/catalog/get-automatic-price-services?idProperty=…`: the bar, standard and hourly services
 * of the property, as `{ id, value }` with the name, for "Validità sui servizi". 401 without a valid
 * access token; 403 for a missing or unknown property.
 */
export const automaticPriceServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = propertyOf(request.params.get('idProperty'));
  if (!property) {
    return forbidden(request);
  }
  const answer: AutomaticPriceServices = {
    barServices: serviceItemsOf(BAR_SERVICES, property.id, (row) => row.idBarService),
    standardServices: serviceItemsOf(
      STANDARD_SERVICES,
      property.id,
      (row) => row.idStandardService,
    ),
    hourlyServices: serviceItemsOf(HOURLY_SERVICES, property.id, (row) => row.idHourlyService),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

type CreateAutomaticPriceBody = Partial<AutomaticPriceAddItemRequest> & { idProperty?: unknown };

/**
 * `POST /api/catalog/create-automatic-price` with `AutomaticPriceAddItemRequest` and `idProperty`: the
 * new automatic price goes into the list, with the name in the language of `Accept-Language`. 204
 * without a body; 401 without a valid access token; 403 for an unknown property; 404
 * `resource.not_found` for a service that is not one of the property; 400 with one error per wrong
 * field (name and description in the language of the request, icon of one of the two lists, price
 * not below 0).
 */
export const createAutomaticPriceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateAutomaticPriceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  if (!areAutomaticServicesOf(body, property.id)) {
    return resourceNotFound(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [
    ...standardGeneralErrors({ translations: body.translations }, language),
    ...automaticIconErrors(body.icon ?? null),
    ...priceErrors(body),
  ];
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const idAutomaticPrice = crypto.randomUUID();
  AUTOMATIC_PRICE_DETAILS.set(
    idAutomaticPrice,
    structuredClone(
      onlyFieldsOf(AutomaticPriceAddItemRequest, body as AutomaticPriceAddItemRequest),
    ),
  );
  AUTOMATIC_PRICES.push({
    propertyId: property.id,
    idAutomaticPrice,
    serviceName: main?.serviceName?.trim() ?? null,
    price: body.price ?? null,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** The data of the add panel of an automatic price: the saved one, or a minimal one from the list. */
function automaticDetailsOf(row: AutomaticPriceRow): AutomaticPriceAddItemRequest {
  return (
    AUTOMATIC_PRICE_DETAILS.get(row.idAutomaticPrice ?? '') ??
    Object.assign(new AutomaticPriceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: row.serviceName,
          description: 'Prezzo automatico dello stabilimento.',
        },
      ],
      price: row.price,
      vatRate: 10,
    })
  );
}

/** The automatic price of the `idAutomaticPrice` of the query string, if it exists. */
function requestedAutomaticPrice(request: HttpRequest<unknown>): AutomaticPriceRow | undefined {
  const idAutomaticPrice = request.params.get('idAutomaticPrice');
  return AUTOMATIC_PRICES.find((row) => row.idAutomaticPrice === idAutomaticPrice);
}

/**
 * `GET /api/catalog/get-automatic-price-general-info?idAutomaticPrice=…`: the Info Generali tab of the
 * edit panel. 200 with `AutomaticPriceSelectedGeneralInfo`, only the languages written; 401 without a
 * valid access token; 404 `resource.not_found` for an unknown id.
 */
export const automaticPriceGeneralInfoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedAutomaticPrice(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = automaticDetailsOf(row);
  const answer: AutomaticPriceSelectedGeneralInfo = {
    idAutomaticPrice: row.idAutomaticPrice,
    translations: details.translations.filter(
      ({ serviceName, description }) => serviceName || description,
    ),
    icon: details.icon,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-automatic-price-prices-validity?idAutomaticPrice=…`: the Prezzi e Validità
 * tab of the edit panel. 200 with `AutomaticPriceSelectedPricesValidity`; 401 without a valid access
 * token; 404 `resource.not_found` for an unknown id.
 */
export const automaticPricePricesValidityMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedAutomaticPrice(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = automaticDetailsOf(row);
  const answer: AutomaticPriceSelectedPricesValidity = {
    price: details.price,
    vatRate: details.vatRate,
    isOnlinePurchaseEnabled: details.isOnlinePurchaseEnabled,
    validityPeriods: details.validityPeriods,
    idBarServices: details.idBarServices,
    idStandardServices: details.idStandardServices,
    idHourlyServices: details.idHourlyServices,
    isPricePerServiceEnabled: details.isPricePerServiceEnabled,
    idSectors: details.idSectors,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

type UpdateAutomaticPriceBody = CreateAutomaticPriceBody & { idAutomaticPrice?: unknown };

/**
 * The common part of the two updates of the edit panel: 401 without a valid access token; 403 for an
 * unknown property; 404 `resource.not_found` for an `idAutomaticPrice` that is not one of its
 * automatic prices (or for a service that is not one of the property); 400 with one error per wrong
 * field; otherwise `apply` changes the data of its tab and the answer is 204 without a body.
 */
function updateAutomaticPrice(
  request: HttpRequest<unknown>,
  errorsOf: (body: UpdateAutomaticPriceBody, propertyId: number) => MockFieldError[] | null,
  apply: (
    details: AutomaticPriceAddItemRequest,
    body: UpdateAutomaticPriceBody,
    row: AutomaticPriceRow,
  ) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateAutomaticPriceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const row = AUTOMATIC_PRICES.find(
    (item) => item.idAutomaticPrice === body.idAutomaticPrice && item.propertyId === property.id,
  );
  const errors = row ? errorsOf(body, property.id) : null;
  if (!row?.idAutomaticPrice || !errors) {
    return resourceNotFound(request);
  }
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const details = structuredClone(automaticDetailsOf(row));
  apply(details, structuredClone(body), row);
  AUTOMATIC_PRICE_DETAILS.set(row.idAutomaticPrice, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-automatic-price-general` with `AutomaticPriceUpdateGeneralRequest` and
 * `idProperty`: names, descriptions and icon; the list shows the new name (language of
 * `Accept-Language`). Errors as the creation.
 */
export const updateAutomaticPriceGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const language = request.headers.get('Accept-Language') ?? 'it';
  return updateAutomaticPrice(
    request,
    (body) => [
      ...standardGeneralErrors({ translations: body.translations }, language),
      ...automaticIconErrors(body.icon ?? null),
    ],
    (details, body, row) => {
      details.translations = body.translations ?? [];
      details.icon = body.icon ?? null;
      const main = details.translations.find((translation) => translation.language === language);
      row.serviceName = main?.serviceName?.trim() ?? row.serviceName;
    },
  );
};

/**
 * `POST /api/catalog/update-automatic-price-prices-validity` with
 * `AutomaticPriceUpdatePricesValidityRequest` and `idProperty`: the Prezzi e Validità tab; the list
 * shows the new price. 400 for a price below 0; 404 for a service that is not one of the property.
 */
export const updateAutomaticPricePricesValidityMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateAutomaticPrice(
    request,
    (body, propertyId) => (areAutomaticServicesOf(body, propertyId) ? priceErrors(body) : null),
    (details, body, row) => {
      details.price = body.price ?? null;
      details.vatRate = body.vatRate ?? null;
      details.isOnlinePurchaseEnabled = body.isOnlinePurchaseEnabled === true;
      details.validityPeriods = body.validityPeriods ?? [];
      details.idBarServices = body.idBarServices ?? [];
      details.idStandardServices = body.idStandardServices ?? [];
      details.idHourlyServices = body.idHourlyServices ?? [];
      details.isPricePerServiceEnabled = body.isPricePerServiceEnabled === true;
      details.idSectors = body.idSectors ?? [];
      row.price = details.price;
    },
  );

/** The services of the body, when given, all of the property. */
function areAutomaticServicesOf(body: CreateAutomaticPriceBody, propertyId: number): boolean {
  return (
    areServicesOf(body.idBarServices, BAR_SERVICES, propertyId, (row) => row.idBarService) &&
    areServicesOf(
      body.idStandardServices,
      STANDARD_SERVICES,
      propertyId,
      (row) => row.idStandardService,
    ) &&
    areServicesOf(body.idHourlyServices, HOURLY_SERVICES, propertyId, (row) => row.idHourlyService)
  );
}

/** The icon, when given, of the bar services or of the other services. */
function automaticIconErrors(icon: string | null): MockFieldError[] {
  return icon === null ||
    Object.values<string>(BarServiceIcon).includes(icon) ||
    Object.values<string>(StandardServiceIcon).includes(icon)
    ? []
    : [{ field: 'icon', code: 'validation.invalid' }];
}

/**
 * `POST /api/catalog/automatic-price-list`: one page of the automatic prices of the requested
 * property, searched in the name. 401 without a valid access token; 403 for a missing or unknown
 * property.
 */
export const automaticPriceListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = AUTOMATIC_PRICES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.serviceName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(AutomaticPriceItem, row));
  const field = AUTOMATIC_PRICE_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<AutomaticPriceItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

type TableServiceRow = TableServiceItem & { propertyId: number };

const TABLE_SERVICES: TableServiceRow[] = (
  [
    ['01', 'Pranzo'],
    ['02', 'Cena'],
    ['03', 'Colazione'],
  ] as const
).map(([suffix, serviceName]) => ({
  propertyId: 1,
  idTableService: `2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a7${suffix}`,
  serviceName,
}));

const TABLE_SERVICE_SORT_FIELDS = ['serviceName'] as const;

/**
 * The data of the add panel of a table service, by id: those created with the panel and Pranzo; the
 * other example rows get theirs from the list (see `tableDetailsOf`).
 */
const TABLE_SERVICE_DETAILS = new Map<string, TableServiceAddItemRequest>([
  [
    '2a4c6e8f-0b1d-4e3f-a5b7-c9d1e3f5a701',
    Object.assign(new TableServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: 'Pranzo',
          description: 'Tavoli del ristorante per il pranzo.',
        },
        {
          language: Language.En,
          serviceName: 'Lunch',
          description: 'Restaurant tables for lunch.',
        },
      ],
      isHighlighted: true,
      availabilities: [
        {
          dateFrom: '2026-06-01T00:00:00.000Z',
          dateTo: '2026-09-30T00:00:00.000Z',
          isSameTimeForAllDays: true,
          startTime: '12:00',
          endTime: '15:00',
          days: [1, 2, 3, 4, 5, 6, 7].map((dayOfWeek) => ({
            dayOfWeek,
            isEnabled: dayOfWeek !== 1,
            startTime: '12:00',
            endTime: '15:00',
          })),
        },
      ],
      availabilityMode: TableServiceAvailabilityMode.Tables,
      tableGroups: [
        { tableQuantity: 10, seatPerTableQuantity: 2 },
        { tableQuantity: 8, seatPerTableQuantity: 4 },
        { tableQuantity: 6, seatPerTableQuantity: 5 },
      ],
      averageMealDuration: TableServiceMealDuration.OneHourThirtyMinutes,
      maxBookingPerTimeSlotQuantity: 8,
      bookingPayment: TableServiceBookingPayment.Deposit,
      depositMode: TableServiceDepositMode.Total,
      depositPercentage: 20,
      isOnlineBookingEnabled: true,
    }),
  ],
]);

/**
 * `POST /api/catalog/table-service-list`: one page of the table services (e.g. lunch, dinner) of the
 * requested property, searched in the name. 401 without a valid access token; 403 for a missing or
 * unknown property.
 */
export const tableServiceListMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = TABLE_SERVICES.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || row.serviceName?.toLowerCase().includes(search)),
  ).map((row) => onlyFieldsOf(TableServiceItem, row));
  const field = TABLE_SERVICE_SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<TableServiceItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/catalog/delete-standard-service-item` with `DeleteStandardServiceItemsRequest`: all or
 * nothing. If one id is not a service of the property nothing is deleted (404
 * `resource.not_found`). Repeated ids count once. 204 without a body; 401 without a valid access
 * token; 403 for an unknown property.
 */
export const deleteStandardServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteStandardServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const standardServices = standardServicesOf(property.id, body.idStandardServices);
  if (!standardServices) {
    return resourceNotFound(request);
  }
  for (const standardService of standardServices) {
    STANDARD_SERVICES.splice(STANDARD_SERVICES.indexOf(standardService), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-standard-service-item` with `DuplicateStandardServiceItemsRequest`:
 * all or nothing, like the delete. Each copy keeps the price; the name is chosen by the backend:
 * the mock invents "Doccia calda (copia)", then "Doccia calda (copia 2)" and so on.
 */
export const duplicateStandardServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateStandardServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const standardServices = standardServicesOf(property.id, body.idStandardServices);
  if (!standardServices) {
    return resourceNotFound(request);
  }
  for (const standardService of standardServices) {
    STANDARD_SERVICES.push({
      ...standardService,
      idStandardService: crypto.randomUUID(),
      serviceName: standardServiceCopyName(standardService),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The services of the property for `idStandardServices` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function standardServicesOf(
  propertyId: number,
  idStandardServices: unknown,
): StandardServiceRow[] | null {
  const ids = Array.isArray(idStandardServices) ? [...new Set(idStandardServices)] : [];
  const standardServices = ids
    .map((id) =>
      STANDARD_SERVICES.find(
        (row) => row.idStandardService === id && row.propertyId === propertyId,
      ),
    )
    .filter(
      (standardService): standardService is StandardServiceRow => standardService !== undefined,
    );
  return ids.length && standardServices.length === ids.length ? standardServices : null;
}

function standardServiceCopyName(standardService: StandardServiceRow): string {
  const taken = new Set(
    STANDARD_SERVICES.filter((row) => row.propertyId === standardService.propertyId).map(
      (row) => row.serviceName,
    ),
  );
  let copy = `${standardService.serviceName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${standardService.serviceName} (copia ${number})`;
  }
  return copy;
}

/**
 * `POST /api/catalog/delete-hourly-service-item` with `DeleteHourlyServiceItemsRequest`: all or
 * nothing. If one id is not an hourly service of the property nothing is deleted (404
 * `resource.not_found`). Repeated ids count once. 204 without a body; 401 without a valid access
 * token; 403 for an unknown property.
 */
export const deleteHourlyServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteHourlyServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const hourlyServices = hourlyServicesOf(property.id, body.idHourlyServices);
  if (!hourlyServices) {
    return resourceNotFound(request);
  }
  for (const hourlyService of hourlyServices) {
    HOURLY_SERVICES.splice(HOURLY_SERVICES.indexOf(hourlyService), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-hourly-service-item` with `DuplicateHourlyServiceItemsRequest`: all
 * or nothing, like the delete. The name of each copy is chosen by the backend: the mock invents
 * "Sup (copia)", then "Sup (copia 2)" and so on.
 */
export const duplicateHourlyServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateHourlyServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const hourlyServices = hourlyServicesOf(property.id, body.idHourlyServices);
  if (!hourlyServices) {
    return resourceNotFound(request);
  }
  for (const hourlyService of hourlyServices) {
    HOURLY_SERVICES.push({
      ...hourlyService,
      idHourlyService: crypto.randomUUID(),
      serviceName: hourlyServiceCopyName(hourlyService),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The hourly services of the property for `idHourlyServices` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function hourlyServicesOf(
  propertyId: number,
  idHourlyServices: unknown,
): HourlyServiceRow[] | null {
  const ids = Array.isArray(idHourlyServices) ? [...new Set(idHourlyServices)] : [];
  const hourlyServices = ids
    .map((id) =>
      HOURLY_SERVICES.find((row) => row.idHourlyService === id && row.propertyId === propertyId),
    )
    .filter((hourlyService): hourlyService is HourlyServiceRow => hourlyService !== undefined);
  return ids.length && hourlyServices.length === ids.length ? hourlyServices : null;
}

function hourlyServiceCopyName(hourlyService: HourlyServiceRow): string {
  const taken = new Set(
    HOURLY_SERVICES.filter((row) => row.propertyId === hourlyService.propertyId).map(
      (row) => row.serviceName,
    ),
  );
  let copy = `${hourlyService.serviceName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${hourlyService.serviceName} (copia ${number})`;
  }
  return copy;
}

/**
 * `POST /api/catalog/delete-automatic-price-item` with `DeleteAutomaticPriceItemsRequest`: all or
 * nothing. If one id is not an automatic price of the property nothing is deleted (404
 * `resource.not_found`). Repeated ids count once. 204 without a body; 401 without a valid access
 * token; 403 for an unknown property.
 */
export const deleteAutomaticPriceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteAutomaticPriceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const automaticPrices = automaticPricesOf(property.id, body.idAutomaticPrices);
  if (!automaticPrices) {
    return resourceNotFound(request);
  }
  for (const automaticPrice of automaticPrices) {
    AUTOMATIC_PRICES.splice(AUTOMATIC_PRICES.indexOf(automaticPrice), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-automatic-price-item` with `DuplicateAutomaticPriceItemsRequest`:
 * all or nothing, like the delete. Each copy keeps the price; the name is chosen by the backend:
 * the mock invents "Coperto (copia)", then "Coperto (copia 2)" and so on.
 */
export const duplicateAutomaticPriceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateAutomaticPriceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const automaticPrices = automaticPricesOf(property.id, body.idAutomaticPrices);
  if (!automaticPrices) {
    return resourceNotFound(request);
  }
  for (const automaticPrice of automaticPrices) {
    AUTOMATIC_PRICES.push({
      ...automaticPrice,
      idAutomaticPrice: crypto.randomUUID(),
      serviceName: automaticPriceCopyName(automaticPrice),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * The automatic prices of the property for `idAutomaticPrices` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function automaticPricesOf(
  propertyId: number,
  idAutomaticPrices: unknown,
): AutomaticPriceRow[] | null {
  const ids = Array.isArray(idAutomaticPrices) ? [...new Set(idAutomaticPrices)] : [];
  const automaticPrices = ids
    .map((id) =>
      AUTOMATIC_PRICES.find((row) => row.idAutomaticPrice === id && row.propertyId === propertyId),
    )
    .filter((automaticPrice): automaticPrice is AutomaticPriceRow => automaticPrice !== undefined);
  return ids.length && automaticPrices.length === ids.length ? automaticPrices : null;
}

function automaticPriceCopyName(automaticPrice: AutomaticPriceRow): string {
  const taken = new Set(
    AUTOMATIC_PRICES.filter((row) => row.propertyId === automaticPrice.propertyId).map(
      (row) => row.serviceName,
    ),
  );
  let copy = `${automaticPrice.serviceName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${automaticPrice.serviceName} (copia ${number})`;
  }
  return copy;
}

/**
 * `POST /api/catalog/delete-table-service-item` with `DeleteTableServiceItemsRequest`: all or
 * nothing. If one id is not a table service of the property nothing is deleted (404
 * `resource.not_found`). Repeated ids count once. 204 without a body; 401 without a valid access
 * token; 403 for an unknown property.
 */
export const deleteTableServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DeleteTableServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const tableServices = tableServicesOf(property.id, body.idTableServices);
  if (!tableServices) {
    return resourceNotFound(request);
  }
  for (const tableService of tableServices) {
    TABLE_SERVICES.splice(TABLE_SERVICES.indexOf(tableService), 1);
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/catalog/duplicate-table-service-item` with `DuplicateTableServiceItemsRequest`: all or
 * nothing, like the delete. The name of each copy is chosen by the backend: the mock invents
 * "Pranzo (copia)", then "Pranzo (copia 2)" and so on.
 */
export const duplicateTableServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<DuplicateTableServiceItemsRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const tableServices = tableServicesOf(property.id, body.idTableServices);
  if (!tableServices) {
    return resourceNotFound(request);
  }
  for (const tableService of tableServices) {
    TABLE_SERVICES.push({
      ...tableService,
      idTableService: crypto.randomUUID(),
      serviceName: tableServiceCopyName(tableService),
    });
  }
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

type CreateTableServiceBody = Partial<TableServiceAddItemRequest> & { idProperty?: unknown };

/**
 * `POST /api/catalog/create-table-service` with `TableServiceAddItemRequest` and `idProperty`: the new
 * table service goes into the list, with the name in the language of `Accept-Language`. 204 without a
 * body; 401 without a valid access token; 403 for an unknown property; 400 with one error per wrong
 * field (name and description in the language of the request, choices among the values of their
 * enums, quantities whole and above 0, deposit from 0 to 100).
 */
export const createTableServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateTableServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [
    ...standardGeneralErrors({ translations: body.translations }, language),
    ...tableServiceErrors(body),
  ];
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const main = body.translations?.find((translation) => translation.language === language);
  const idTableService = crypto.randomUUID();
  TABLE_SERVICE_DETAILS.set(
    idTableService,
    structuredClone(onlyFieldsOf(TableServiceAddItemRequest, body as TableServiceAddItemRequest)),
  );
  TABLE_SERVICES.push({
    propertyId: property.id,
    idTableService,
    serviceName: main?.serviceName?.trim() ?? null,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** The data of the add panel of a table service: the saved one, or a minimal one from the list. */
function tableDetailsOf(row: TableServiceRow): TableServiceAddItemRequest {
  return (
    TABLE_SERVICE_DETAILS.get(row.idTableService ?? '') ??
    Object.assign(new TableServiceAddItemRequest(), {
      translations: [
        {
          language: Language.It,
          serviceName: row.serviceName,
          description: 'Servizio tavoli dello stabilimento.',
        },
      ],
    })
  );
}

/** The table service of the `idTableService` of the query string, if it exists. */
function requestedTableService(request: HttpRequest<unknown>): TableServiceRow | undefined {
  const idTableService = request.params.get('idTableService');
  return TABLE_SERVICES.find((row) => row.idTableService === idTableService);
}

/**
 * `GET /api/catalog/get-table-service-general-info?idTableService=…`: the Info Generali tab of the
 * edit panel. 200 with `TableServiceSelectedGeneralInfo`, only the languages written; 401 without a
 * valid access token; 404 `resource.not_found` for an unknown id.
 */
export const tableServiceGeneralInfoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedTableService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = tableDetailsOf(row);
  const answer: TableServiceSelectedGeneralInfo = {
    idTableService: row.idTableService,
    translations: details.translations.filter(
      ({ serviceName, description }) => serviceName || description,
    ),
    isHighlighted: details.isHighlighted,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/get-table-service-availability-bookings?idTableService=…`: the Disponibilità e
 * Prenotazioni tab of the edit panel. 200 with `TableServiceSelectedAvailabilityBookings`; 401
 * without a valid access token; 404 `resource.not_found` for an unknown id.
 */
export const tableServiceAvailabilityBookingsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const row = requestedTableService(request);
  if (!row) {
    return resourceNotFound(request);
  }
  const details = tableDetailsOf(row);
  const answer: TableServiceSelectedAvailabilityBookings = {
    availabilities: details.availabilities,
    availabilityMode: details.availabilityMode,
    tableGroups: details.tableGroups,
    totalSeatQuantity: details.totalSeatQuantity,
    averageMealDuration: details.averageMealDuration,
    maxBookingPerTimeSlotQuantity: details.maxBookingPerTimeSlotQuantity,
    bookingPayment: details.bookingPayment,
    depositMode: details.depositMode,
    depositPercentage: details.depositPercentage,
    isOnlineBookingEnabled: details.isOnlineBookingEnabled,
  };
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(answer) }),
  ).pipe(delay(300));
};

type UpdateTableServiceBody = CreateTableServiceBody & { idTableService?: unknown };

/**
 * The common part of the two updates of the edit panel: 401 without a valid access token; 403 for an
 * unknown property; 404 `resource.not_found` for an `idTableService` that is not one of its table
 * services; 400 with one error per wrong field; otherwise `apply` changes the data of its tab and
 * the answer is 204 without a body.
 */
function updateTableService(
  request: HttpRequest<unknown>,
  errorsOf: (body: UpdateTableServiceBody) => MockFieldError[],
  apply: (
    details: TableServiceAddItemRequest,
    body: UpdateTableServiceBody,
    row: TableServiceRow,
  ) => void,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateTableServiceBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const row = TABLE_SERVICES.find(
    (item) => item.idTableService === body.idTableService && item.propertyId === property.id,
  );
  if (!row?.idTableService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const details = structuredClone(tableDetailsOf(row));
  apply(details, structuredClone(body), row);
  TABLE_SERVICE_DETAILS.set(row.idTableService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-table-service-general` with `TableServiceUpdateGeneralRequest` and
 * `idProperty`: names, descriptions and Metti in evidenza; the list shows the new name (language of
 * `Accept-Language`). Errors as the creation.
 */
export const updateTableServiceGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const language = request.headers.get('Accept-Language') ?? 'it';
  return updateTableService(
    request,
    (body) => standardGeneralErrors({ translations: body.translations }, language),
    (details, body, row) => {
      details.translations = body.translations ?? [];
      details.isHighlighted = body.isHighlighted === true;
      const main = details.translations.find((translation) => translation.language === language);
      row.serviceName = main?.serviceName?.trim() ?? row.serviceName;
    },
  );
};

/**
 * `POST /api/catalog/update-table-service-availability-bookings` with
 * `TableServiceUpdateAvailabilityBookingsRequest` and `idProperty`: the Disponibilità e Prenotazioni
 * tab. Errors as the creation (see `tableServiceErrors`).
 */
export const updateTableServiceAvailabilityBookingsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  updateTableService(request, tableServiceErrors, (details, body) => {
    details.availabilities = body.availabilities ?? [];
    details.availabilityMode = body.availabilityMode ?? TableServiceAvailabilityMode.Tables;
    details.tableGroups = body.tableGroups ?? [];
    details.totalSeatQuantity = body.totalSeatQuantity ?? null;
    details.averageMealDuration = body.averageMealDuration ?? null;
    details.maxBookingPerTimeSlotQuantity = body.maxBookingPerTimeSlotQuantity ?? null;
    details.bookingPayment = body.bookingPayment ?? TableServiceBookingPayment.None;
    details.depositMode = body.depositMode ?? TableServiceDepositMode.Total;
    details.depositPercentage = body.depositPercentage ?? null;
    details.isOnlineBookingEnabled = body.isOnlineBookingEnabled === true;
  });

function tableServiceErrors(body: CreateTableServiceBody): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const invalid = (field: string): void => {
    errors.push({ field, code: 'validation.invalid' });
  };
  if (!isValueOf(TableServiceAvailabilityMode, body.availabilityMode)) {
    invalid('availabilityMode');
  }
  if (!isValueOf(TableServiceBookingPayment, body.bookingPayment)) {
    invalid('bookingPayment');
  }
  if (!isValueOf(TableServiceDepositMode, body.depositMode)) {
    invalid('depositMode');
  }
  if (
    body.averageMealDuration != null &&
    !isValueOf(TableServiceMealDuration, body.averageMealDuration)
  ) {
    invalid('averageMealDuration');
  }
  const tableGroups = Array.isArray(body.tableGroups) ? body.tableGroups : [];
  tableGroups.forEach((tableGroup, index) => {
    if (!isQuantity(tableGroup.tableQuantity)) {
      invalid(`tableGroups[${index}].tableQuantity`);
    }
    if (!isQuantity(tableGroup.seatPerTableQuantity)) {
      invalid(`tableGroups[${index}].seatPerTableQuantity`);
    }
  });
  if (!isQuantity(body.totalSeatQuantity)) {
    invalid('totalSeatQuantity');
  }
  if (!isQuantity(body.maxBookingPerTimeSlotQuantity)) {
    invalid('maxBookingPerTimeSlotQuantity');
  }
  const deposit = body.depositPercentage;
  if (deposit != null && !(typeof deposit === 'number' && deposit >= 0 && deposit <= 100)) {
    invalid('depositPercentage');
  }
  return errors;
}

function isValueOf(choices: Record<string, string>, value: unknown): boolean {
  return Object.values(choices).some((choice) => choice === value);
}

/** Empty, or a whole number above 0. */
function isQuantity(value: unknown): boolean {
  return value == null || (Number.isInteger(value) && (value as number) > 0);
}

/**
 * The table services of the property for `idTableServices` (repeated ids once); null when one id is not
 * one of them, or there are none: the whole request is cancelled.
 */
function tableServicesOf(propertyId: number, idTableServices: unknown): TableServiceRow[] | null {
  const ids = Array.isArray(idTableServices) ? [...new Set(idTableServices)] : [];
  const tableServices = ids
    .map((id) =>
      TABLE_SERVICES.find((row) => row.idTableService === id && row.propertyId === propertyId),
    )
    .filter((tableService): tableService is TableServiceRow => tableService !== undefined);
  return ids.length && tableServices.length === ids.length ? tableServices : null;
}

function tableServiceCopyName(tableService: TableServiceRow): string {
  const taken = new Set(
    TABLE_SERVICES.filter((row) => row.propertyId === tableService.propertyId).map(
      (row) => row.serviceName,
    ),
  );
  let copy = `${tableService.serviceName} (copia)`;
  for (let number = 2; taken.has(copy); number++) {
    copy = `${tableService.serviceName} (copia ${number})`;
  }
  return copy;
}

/** Numbers by value, texts alphabetically; a missing value first ascending, last descending. */
function compare(first: string | number | null, second: string | number | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return typeof first === 'number' && typeof second === 'number'
    ? first - second
    : String(first).localeCompare(String(second), 'it', { sensitivity: 'base' });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
