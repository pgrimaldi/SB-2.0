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
import { UploadContentType } from '../../../entities/enums/upload-content-type';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import {
  BathhouseGeneralInformation,
  MAX_BATHHOUSE_PHONE_LENGTH,
} from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseDocument } from '../../../entities/enums/bathhouse-document';
import { BathhouseServiceGroup } from '../../../entities/enums/bathhouse-service-group';
import { PetPolicy } from '../../../entities/enums/pet-policy';
import {
  BathhouseMedia,
  MAX_BATHHOUSE_PDF_MEGABYTES,
  MAX_BATHHOUSE_PHOTO_QUANTITY,
  MAX_VIRTUAL_TOUR_URL_LENGTH,
} from '../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseMediaRequest } from '../../../entities/settings/bathhouse-configuration/bathhouse-media-request';
import { PropertyInfoDocument } from '../../../entities/settings/bathhouse-configuration/property-info-document';
import { PropertyInfoMedia } from '../../../entities/settings/bathhouse-configuration/property-info-media';
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
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../entities/shared/image-upload-types';
import { Language } from '../../../entities/shared/language';
import { Country } from '../../../entities/shared/country';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import {
  MockFieldError,
  notFoundWithoutBody,
  problem,
  resourceNotFound,
} from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

/** ISO 3166-1 alpha-2 codes, in the order of `catalog.countries.sort_order`. */
const COUNTRY_CODES = ['IT', 'FR', 'DE', 'CN'] as const;

/**
 * `GET /api/catalog/lookups/countries`: the active countries, `code` = ISO code and `name` in the language
 * of `Accept-Language` (the backend has a translation per country and language), in the order of
 * `sort_order`. Open also without an access token, as in the backend.
 */
export const lookupCountriesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  const names = new Intl.DisplayNames([request.headers.get('Accept-Language') ?? 'it'], {
    type: 'region',
  });
  const countries: Country[] = COUNTRY_CODES.map((code) => ({
    code,
    name: names.of(code) ?? code,
  }));
  return of(new HttpResponse({ status: 200, url: request.url, body: countries })).pipe(delay(150));
};

const SERVICE_LANGUAGES = ['it', 'en', 'fr', 'es', 'de', 'zh', 'ar'] as const;

/**
 * The services of the catalogue as the backend has them (`catalog.service_amenities`, in its order):
 * id, category, icon and the name in it, en, fr, es, de, zh, ar.
 */
const SERVICES: readonly {
  idService: number;
  categoryCode: BathhouseServiceGroup;
  icon: string;
  names: readonly [string, string, string, string, string, string, string];
}[] = [
  {
    idService: 14,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 1,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 31,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
    icon: 'Shower',
    names: ['Doccia', 'Shower', 'Douche', 'Ducha', 'Dusche', '淋浴', 'دش'],
  },
  {
    idService: 36,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 34,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 38,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
    icon: 'Parking',
    names: ['Wifi', 'Wi-Fi', 'Wi-Fi', 'Wifi', 'WLAN', '无线网络', 'واي فاي'],
  },
  {
    idService: 35,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
    icon: 'Wc',
    names: ['Toilette', 'Toilets', 'Toilettes', 'Aseos', 'Toiletten', '卫生间', 'دورات مياه'],
  },
  {
    idService: 23,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 30,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 7,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 22,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
    icon: 'Parking',
    names: [
      'Parcheggio incluso',
      'Parking included',
      'Parking inclus',
      'Aparcamiento incluido',
      'Parkplatz inklusive',
      '含停车位',
      'موقف سيارات مشمول',
    ],
  },
  {
    idService: 29,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 8,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 21,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
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
    idService: 18,
    categoryCode: BathhouseServiceGroup.AccessibilityComfort,
    icon: 'Infirmary',
    names: [
      'Infermeria',
      'Infirmary',
      'Infirmerie',
      'Enfermería',
      'Krankenstation',
      '医务室',
      'مستوصف',
    ],
  },
  {
    idService: 4,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 5,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 15,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 17,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 10,
    categoryCode: BathhouseServiceGroup.SportActivities,
    icon: 'Luggage',
    names: ['Escursioni', 'Excursions', 'Excursions', 'Excursiones', 'Ausflüge', '远足', 'رحلات'],
  },
  {
    idService: 2,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 19,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 33,
    categoryCode: BathhouseServiceGroup.SportActivities,
    icon: 'Surfing',
    names: ['Surf', 'Surfing', 'Surf', 'Surf', 'Surfen', '冲浪', 'ركوب الأمواج'],
  },
  {
    idService: 9,
    categoryCode: BathhouseServiceGroup.SportActivities,
    icon: 'Canoe',
    names: ['Canoa', 'Canoeing', 'Canoë', 'Canoa', 'Kanu', '皮划艇', 'قوارب الكانو'],
  },
  {
    idService: 37,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 20,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 32,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 24,
    categoryCode: BathhouseServiceGroup.SportActivities,
    icon: 'PedalBoat',
    names: ['Pedalò', 'Pedal boat', 'Pédalo', 'Hidropedal', 'Tretboot', '脚踏船', 'قارب بدواسات'],
  },
  {
    idService: 27,
    categoryCode: BathhouseServiceGroup.SportActivities,
    icon: 'Pool',
    names: ['Piscina', 'Swimming pool', 'Piscine', 'Piscina', 'Pool', '游泳池', 'مسبح'],
  },
  {
    idService: 13,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 16,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 6,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 11,
    categoryCode: BathhouseServiceGroup.SportActivities,
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
    idService: 3,
    categoryCode: BathhouseServiceGroup.FoodExtra,
    icon: 'Cafe',
    names: ['Bar', 'Bar', 'Bar', 'Bar', 'Bar', '酒吧', 'بار'],
  },
  {
    idService: 28,
    categoryCode: BathhouseServiceGroup.FoodExtra,
    icon: 'Restaurant',
    names: ['Ristorante', 'Restaurant', 'Restaurant', 'Restaurante', 'Restaurant', '餐厅', 'مطعم'],
  },
  {
    idService: 26,
    categoryCode: BathhouseServiceGroup.FoodExtra,
    icon: 'Pizza',
    names: ['Pizzeria', 'Pizzeria', 'Pizzeria', 'Pizzería', 'Pizzeria', '披萨店', 'مطعم بيتزا'],
  },
  {
    idService: 12,
    categoryCode: BathhouseServiceGroup.FoodExtra,
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
    idService: 57,
    categoryCode: BathhouseServiceGroup.FoodExtra,
    icon: 'Leaf',
    names: [
      'Cibo vegano - Cibo senza glutine',
      'Vegan food - Gluten free food',
      'Cuisine végane - Cuisine sans gluten',
      'Comida vegana - Comida sin gluten',
      'Veganes Essen - Glutenfreies Essen',
      '纯素食品 - 无麸质食品',
      'طعام نباتي - طعام خالٍ من الغلوتين',
    ],
  },
  {
    idService: 25,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 40,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 48,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 53,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 44,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 49,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 56,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 39,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 45,
    categoryCode: BathhouseServiceGroup.Beach,
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
    idService: 52,
    categoryCode: BathhouseServiceGroup.Beach,
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
 * `GET /api/catalog/property-info/bathhouse-configuration-general`: the general information of the property of
 * the token, for the tab "Informazioni generali"; all empty until saved, a form to fill in, except
 * `isFreeCancellation` false and `petPolicy` NotAllowed, the defaults of the database. 401 without a
 * valid access token.
 */
export const bathhouseConfigurationGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: { ...generalInformation } }),
  ).pipe(delay(300));
};

/**
 * `GET /api/catalog/property-info/bathhouse-configuration-services`: every service of the catalogue, in the order
 * of the categories and then of the services, with the name in the language of `Accept-Language`;
 * offered by the property of the token as last saved, none at the start. 401 without a valid access
 * token.
 */
export const bathhouseConfigurationServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const language = SERVICE_LANGUAGES.indexOf(
    request.headers.get('Accept-Language') as (typeof SERVICE_LANGUAGES)[number],
  );
  const services: BathhouseServiceItem[] = SERVICES.map(
    ({ idService, categoryCode, icon, names }) => ({
      idService,
      name: names[Math.max(language, 0)],
      categoryCode,
      iconUrl: `/assets/images/BathhouseConfiguration_${icon}.svg`,
      isSelected: idOfferedServices.has(idService),
    }),
  );
  return of(new HttpResponse({ status: 200, url: request.url, body: services })).pipe(delay(300));
};

/**
 * `POST /api/catalog/property-info/save-bathhouse-configuration-general` with every field of the tab, for the
 * property of the token: keeps them (until the page is reloaded). 204 without a body; 400 without a
 * name, with a phone or WhatsApp number that is not digits and spaces up to 32 characters (see
 * `phoneErrors`), without a `petPolicy` of `PetPolicy`, or with a `closingTime` not after the
 * `openingTime`; 401 without a valid access token.
 */
export const saveBathhouseConfigurationGeneralMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<BathhouseGeneralInformation>;
  const errors: MockFieldError[] = [];
  if (typeof body.bathhouseName !== 'string' || !body.bathhouseName.trim()) {
    errors.push({ field: 'bathhouseName', code: ApiErrorCode.ValidationRequired });
  }
  errors.push(...phoneErrors('phone', body.phone), ...phoneErrors('whatsapp', body.whatsapp));
  if (!Object.values<unknown>(PetPolicy).includes(body.petPolicy)) {
    errors.push({ field: 'petPolicy', code: ApiErrorCode.ValidationInvalid });
  }
  if (body.openingTime && body.closingTime && body.closingTime <= body.openingTime) {
    errors.push({ field: 'closingTime', code: ApiErrorCode.ValidationOutOfRange });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  generalInformation = onlyFieldsOf(BathhouseGeneralInformation, {
    ...generalInformation,
    ...body,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** A phone number of the property as the backend checks it: at most 32 characters, only digits and spaces. */
function phoneErrors(field: string, value: unknown): MockFieldError[] {
  if (value === null || value === undefined) {
    return [];
  }
  if (typeof value !== 'string') {
    return [{ field, code: ApiErrorCode.ValidationPhone }];
  }
  if (value.length > MAX_BATHHOUSE_PHONE_LENGTH) {
    return [
      { field, code: ApiErrorCode.ValidationMaxLength, args: { max: MAX_BATHHOUSE_PHONE_LENGTH } },
    ];
  }
  return /^[0-9 ]*$/.test(value) ? [] : [{ field, code: ApiErrorCode.ValidationPhone }];
}

/**
 * `POST /api/catalog/property-info/save-bathhouse-configuration-services` with `services`, the services the
 * property of the token offers: keeps them (until the page is reloaded). 204 without a body; 401
 * without a valid access token; 404 for a service that does not exist.
 */
export const saveBathhouseConfigurationServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { services?: unknown };
  const services = Array.isArray(body.services) ? (body.services as BathhouseServiceItem[]) : [];
  const idServices = services.map(({ idService }) => idService);
  if (!idServices.every(isServiceId)) {
    return resourceNotFound(request);
  }
  idOfferedServices = new Set(idServices);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

function isServiceId(value: unknown): value is number {
  return SERVICES.some(({ idService }) => idService === value);
}

/**
 * `POST /api/catalog/property-info/save-bathhouse-configuration-media` with `virtualTourUrl` (null or empty removes
 * it), `latitude` and `longitude` (both or neither) of the property of the token: keeps them (until
 * the page is reloaded). 204 without a body; 400 with an error per field for a link that is not http
 * or https or longer than 500 characters, a coordinate out of range or only one of the two; 401
 * without a valid access token.
 */
export const saveBathhouseConfigurationMediaMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<Record<keyof BathhouseMediaRequest, unknown>>;
  const link =
    typeof body.virtualTourUrl === 'string' && body.virtualTourUrl ? body.virtualTourUrl : null;
  const latitude = typeof body.latitude === 'number' ? body.latitude : null;
  const longitude = typeof body.longitude === 'number' ? body.longitude : null;
  const errors: MockFieldError[] = [];
  if (link && link.length > MAX_VIRTUAL_TOUR_URL_LENGTH) {
    errors.push({
      field: 'virtualTourUrl',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: MAX_VIRTUAL_TOUR_URL_LENGTH },
    });
  } else if (link && !isHttpLink(link)) {
    errors.push({ field: 'virtualTourUrl', code: ApiErrorCode.ValidationInvalid });
  }
  errors.push(
    ...coordinateErrors('latitude', latitude, longitude, MAX_LATITUDE),
    ...coordinateErrors('longitude', longitude, latitude, MAX_LONGITUDE),
  );
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  bathhouseMedia = { ...bathhouseMedia, virtualTourUrl: link, latitude, longitude };
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

const MAX_LATITUDE = 90;
const MAX_LONGITUDE = 180;

/** As the backend: within ±`max`, and given only together with the other coordinate. */
function coordinateErrors(
  field: keyof BathhouseMediaRequest,
  value: number | null,
  other: number | null,
  max: number,
): MockFieldError[] {
  if (value === null) {
    return other === null ? [] : [{ field, code: ApiErrorCode.ValidationRequired }];
  }
  return Math.abs(value) > max ? [{ field, code: ApiErrorCode.ValidationOutOfRange }] : [];
}

function isHttpLink(link: string): boolean {
  try {
    return ['http:', 'https:'].includes(new URL(link).protocol);
  } catch {
    return false;
  }
}

/** The media of the property of the token: the Media tab reads them and its uploads change them. */
let bathhouseMedia: BathhouseMedia = {
  mainPhotoUrl: '/assets/images/hub/hero-beach-desktop.webp',
  photos: [
    {
      idPhoto: '0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e01',
      url: '/assets/images/hub/hero-beach-mobile-863.webp',
    },
    {
      idPhoto: '0193f2a4-6b1c-7d2e-8f30-4a5b6c7d8e02',
      url: '/assets/images/hub/hero-beach-mobile-1290.webp',
    },
  ],
  virtualTourUrl: 'https://tour.example.com/lido-demo',
  latitude: 44.063812,
  longitude: 12.580143,
  priceListPdfUrl: 'https://files.example.com/lido-demo/listino-prezzi.pdf',
  priceListPdfFileName: 'listino-prezzi.pdf',
  menuPdfUrl: null,
  menuPdfFileName: null,
};
const MAX_PHOTO_BYTES = MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE;
const MAX_PDF_BYTES = MAX_BATHHOUSE_PDF_MEGABYTES * MEGABYTE;

/**
 * `GET /api/catalog/property-info/bathhouse-configuration-media`: links of the main photo, of the gallery (in
 * order), of the virtual tour and of the two PDF documents of the property of the token. 401 without
 * a valid access token.
 */
export const bathhouseConfigurationMediaMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: structuredClone(bathhouseMedia) }),
  ).pipe(delay(300));
};

/**
 * `POST /api/catalog/property-info/main-photo` with the image in the multipart field `file`: it
 * becomes the main photo (an object URL stands for the address of the backend's storage). 422
 * catalog.invalid_image for a type other than png, jpeg or webp or a file over 5 MB; 401 without a
 * valid access token. Like every call of `property-info`, it answers with the public card, here
 * only its media.
 */
export const uploadBathhouseMainPhotoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withImage(request, (image) => {
    bathhouseMedia = { ...bathhouseMedia, mainPhotoUrl: URL.createObjectURL(image) };
    return propertyInfoAnswer(request);
  });

/** `DELETE /api/catalog/property-info/main-photo`: no main photo any more. 401 without a token. */
export const deleteBathhouseMainPhotoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  bathhouseMedia = { ...bathhouseMedia, mainPhotoUrl: null };
  return propertyInfoAnswer(request);
};

/**
 * `POST /api/catalog/property-info/photos` with the image in the multipart field `file`: added at
 * the end of the gallery. 422 catalog.invalid_image as for the main photo, catalog.gallery_full
 * with 30 photos already; 401 without a valid access token.
 */
export const addBathhousePhotoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> =>
  withImage(request, (image) => {
    if (bathhouseMedia.photos.length >= MAX_BATHHOUSE_PHOTO_QUANTITY) {
      return problem(request, 422, ApiErrorCode.CatalogGalleryFull, {
        title: 'Gallery full',
        args: { max: MAX_BATHHOUSE_PHOTO_QUANTITY },
      });
    }
    bathhouseMedia = {
      ...bathhouseMedia,
      photos: [
        ...bathhouseMedia.photos,
        { idPhoto: crypto.randomUUID(), url: URL.createObjectURL(image) },
      ],
    };
    return propertyInfoAnswer(request);
  });

/**
 * `DELETE /api/catalog/property-info/photos/{id}`: the photo leaves the gallery. 404
 * catalog.photo_not_found for an id that is not in it; 401 without a valid access token.
 */
export const deleteBathhousePhotoMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idPhoto = decodeURIComponent(request.url.split('/').pop() ?? '');
  if (!bathhouseMedia.photos.some((photo) => photo.idPhoto === idPhoto)) {
    return problem(request, 404, ApiErrorCode.CatalogPhotoNotFound, { title: 'Photo not found' });
  }
  bathhouseMedia = {
    ...bathhouseMedia,
    photos: bathhouseMedia.photos.filter((photo) => photo.idPhoto !== idPhoto),
  };
  return propertyInfoAnswer(request);
};

/**
 * `POST /api/catalog/property-info/documents/{price-list|menu}` with the PDF in the multipart field
 * `file`: it replaces the one of that document. 422 catalog.invalid_pdf for another type or a file
 * over 10 MB; 404 without a body for another document; 401 without a valid access token.
 */
export const uploadBathhouseDocumentMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const document = documentOf(request);
  if (!document) {
    return notFoundWithoutBody(request);
  }
  const pdf = request.body instanceof FormData ? request.body.get('file') : null;
  if (!(pdf instanceof File) || pdf.type !== UploadContentType.Pdf || pdf.size > MAX_PDF_BYTES) {
    return problem(request, 422, ApiErrorCode.CatalogInvalidPdf, {
      title: 'Invalid PDF',
      args: { maxBytes: MAX_PDF_BYTES, contentTypes: [UploadContentType.Pdf] },
    });
  }
  setDocument(document, URL.createObjectURL(pdf), pdf.name);
  return propertyInfoAnswer(request);
};

/**
 * `DELETE /api/catalog/property-info/documents/{price-list|menu}`: the document has no PDF any more.
 * 404 without a body for another document; 401 without a valid access token.
 */
export const deleteBathhouseDocumentMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const document = documentOf(request);
  if (!document) {
    return notFoundWithoutBody(request);
  }
  setDocument(document, null, null);
  return propertyInfoAnswer(request);
};

function withImage(
  request: HttpRequest<unknown>,
  keep: (image: File) => Observable<HttpEvent<unknown>>,
): Observable<HttpEvent<unknown>> {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const image = request.body instanceof FormData ? request.body.get('file') : null;
  if (
    !(image instanceof File) ||
    !IMAGE_UPLOAD_TYPES.includes(image.type) ||
    image.size > MAX_PHOTO_BYTES
  ) {
    return problem(request, 422, ApiErrorCode.CatalogInvalidImage, {
      title: 'Invalid image',
      args: { maxBytes: MAX_PHOTO_BYTES, contentTypes: IMAGE_UPLOAD_TYPES },
    });
  }
  return keep(image);
}

/** The last part of `/property-info/documents/{document}`, if it is one of the two. */
function documentOf(request: HttpRequest<unknown>): BathhouseDocument | undefined {
  const code = request.url.split('/').pop();
  return Object.values(BathhouseDocument).find((document) => document === code);
}

function setDocument(document: BathhouseDocument, url: string | null, name: string | null): void {
  bathhouseMedia =
    document === BathhouseDocument.PriceList
      ? { ...bathhouseMedia, priceListPdfUrl: url, priceListPdfFileName: name }
      : { ...bathhouseMedia, menuPdfUrl: url, menuPdfFileName: name };
}

/** The public card as `property-info` answers it: here only the media, the fields the tab reads. */
function propertyInfoAnswer(request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> {
  const documentOfLink = (
    url: string | null,
    fileName: string | null,
  ): PropertyInfoDocument | null => (url ? { fileName: fileName ?? 'documento.pdf', url } : null);
  const body: PropertyInfoMedia = {
    mainPhotoUrl: bathhouseMedia.mainPhotoUrl,
    photos: bathhouseMedia.photos.map(({ idPhoto, url }, index) => ({
      id: idPhoto,
      url,
      sortOrder: index,
    })),
    virtualTourUrl: bathhouseMedia.virtualTourUrl,
    latitude: bathhouseMedia.latitude,
    longitude: bathhouseMedia.longitude,
    priceListPdf: documentOfLink(
      bathhouseMedia.priceListPdfUrl,
      bathhouseMedia.priceListPdfFileName,
    ),
    menuPdf: documentOfLink(bathhouseMedia.menuPdfUrl, bathhouseMedia.menuPdfFileName),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(600));
}

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
      periodPrices: [{ idPeriod: 0, price: 1.2 }],
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
 * `POST /api/catalog/bar-service-list`: one page of the bar services of the property of the token,
 * searched in the name. 401 without a valid access token.
 */
export const barServiceListMock = (
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

type CreateBarServiceBody = Partial<BarServiceAddItemRequest>;

/**
 * `POST /api/catalog/create-bar-service` with `BarServiceAddItemRequest`: the new bar service goes
 * into the list, with the name in the language of `Accept-Language`. 204 without a body; 401
 * without a valid access token; 400 with one error per wrong field (see `barServiceErrors`).
 */
export const createBarServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateBarServiceBody;
  const property = DEMO_PROPERTY;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = barServiceErrors(body, language);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
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
    errors.push({ field: `${at}.serviceName`, code: ApiErrorCode.ValidationRequired });
  }
  if (typeof main?.description !== 'string' || !main.description.trim()) {
    errors.push({ field: `${at}.description`, code: ApiErrorCode.ValidationRequired });
  }
  if (body.category != null && !Object.values(BarServiceCategory).includes(body.category)) {
    errors.push({ field: 'category', code: ApiErrorCode.ValidationInvalid });
  }
  if (body.icon != null && !Object.values(BarServiceIcon).includes(body.icon)) {
    errors.push({ field: 'icon', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

/** A price, when given, not below 0. */
function priceErrors(body: { price?: number | null }): MockFieldError[] {
  return body.price != null && !(typeof body.price === 'number' && body.price >= 0)
    ? [{ field: 'price', code: ApiErrorCode.ValidationInvalid }]
    : [];
}

type UpdateBarServiceBody = CreateBarServiceBody & { idBarService?: unknown };

/**
 * The common part of the three updates of the edit panel: 401 without a valid access token; 404
 * `resource.not_found` for an `idBarService` that is not one of its bar services; 400 with one
 * error per wrong field; otherwise `apply` changes the data of its tab and the answer is 204
 * without a body.
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
  const property = DEMO_PROPERTY;
  const row = BAR_SERVICES.find(
    (item) => item.idBarService === body.idBarService && item.propertyId === property.id,
  );
  if (!row?.idBarService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  const details = structuredClone(detailsOf(row));
  apply(details, structuredClone(body), row);
  BAR_SERVICE_DETAILS.set(row.idBarService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-bar-service-general` with `BarServiceUpdateGeneralRequest`: names,
 * descriptions, category, icon and Metti in evidenza; the list shows the new name (language of
 * `Accept-Language`) and category. Errors as the creation (see `generalErrors`).
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
 * `POST /api/catalog/update-bar-service-prices` with `BarServiceUpdatePricesRequest`: the Prezzi
 * tab; the list shows the new price. 400 for a price below 0.
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
 * `POST /api/catalog/update-bar-service-booking` with `BarServiceUpdateBookingRequest`: the booking
 * options of the Prenotazioni tab, all of them.
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
 * `POST /api/catalog/delete-bar-service-item` with `{ idBarServices }`: all or nothing. If one id
 * is not a bar service of the property nothing is deleted (404 `resource.not_found`). Repeated ids
 * count once. 204 without a body; 401 without a valid access token.
 */
export const deleteBarServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idBarServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/duplicate-bar-service-item` with `{ idBarServices }`: all or
 * nothing, like the delete. Each copy keeps the price; the name is chosen by the backend: the mock
 * invents "Caffè (copia)", then "Caffè (copia 2)" and so on.
 */
export const duplicateBarServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idBarServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * the property of the token, searched in the name. 401 without a valid access token.
 */
export const standardServiceListMock = (
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
      periodPrices: [{ idPeriod: 0, price: 2.5 }],
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

type CreateStandardServiceBody = Partial<StandardServiceAddItemRequest>;

/**
 * `POST /api/catalog/create-standard-service` with `StandardServiceAddItemRequest`: the new service
 * goes into the list, with the name in the language of `Accept-Language`. 204 without a body; 401
 * without a valid access token; 400 with one error per wrong field (see `standardGeneralErrors` and
 * `priceErrors`).
 */
export const createStandardServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateStandardServiceBody;
  const property = DEMO_PROPERTY;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [...standardGeneralErrors(body, language), ...priceErrors(body)];
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
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
    errors.push({ field: `${at}.serviceName`, code: ApiErrorCode.ValidationRequired });
  }
  if (typeof main?.description !== 'string' || !main.description.trim()) {
    errors.push({ field: `${at}.description`, code: ApiErrorCode.ValidationRequired });
  }
  if (body.icon != null && !Object.values(StandardServiceIcon).includes(body.icon)) {
    errors.push({ field: 'icon', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

type UpdateStandardServiceBody = CreateStandardServiceBody & { idStandardService?: unknown };

/**
 * The common part of the three updates of the edit panel: 401 without a valid access token; 404
 * `resource.not_found` for an `idStandardService` that is not one of its services; 400 with one
 * error per wrong field; otherwise `apply` changes the data of its tab and the answer is 204
 * without a body.
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
  const property = DEMO_PROPERTY;
  const row = STANDARD_SERVICES.find(
    (item) => item.idStandardService === body.idStandardService && item.propertyId === property.id,
  );
  if (!row?.idStandardService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  const details = structuredClone(standardDetailsOf(row));
  apply(details, structuredClone(body), row);
  STANDARD_SERVICE_DETAILS.set(row.idStandardService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-standard-service-general` with `StandardServiceUpdateGeneralRequest`:
 * names, descriptions, icon and Metti in evidenza; the list shows the new name (language of
 * `Accept-Language`). Errors as the creation (see `standardGeneralErrors`).
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
 * `POST /api/catalog/update-standard-service-prices` with `StandardServiceUpdatePricesRequest`: the
 * Prezzi tab; the list shows the new price. 400 for a price below 0.
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
 * `POST /api/catalog/update-standard-service-booking` with `StandardServiceUpdateBookingRequest`:
 * the booking options of the Prenotazioni tab, all of them.
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
 * `POST /api/catalog/hourly-service-list`: one page of the hourly services of the property of the
 * token, searched in the name. 401 without a valid access token.
 */
export const hourlyServiceListMock = (
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
          idPeriod: 0,
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

type CreateHourlyServiceBody = Partial<HourlyServiceAddItemRequest>;

/**
 * `POST /api/catalog/create-hourly-service` with `HourlyServiceAddItemRequest`: the new hourly
 * service goes into the list, with the name in the language of `Accept-Language`. 204 without a
 * body; 401 without a valid access token; 400 with one error per wrong field (see
 * `standardGeneralErrors` and `hourlyPriceErrors`).
 */
export const createHourlyServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateHourlyServiceBody;
  const property = DEMO_PROPERTY;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [...standardGeneralErrors(body, language), ...hourlyPriceErrors(body)];
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
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
        code: ApiErrorCode.ValidationInvalid,
      })),
    ),
    ...priceErrors({ price: option.customTimeSlotPrice }).map((): MockFieldError => ({
      field: `${at}.customTimeSlotPrice`,
      code: ApiErrorCode.ValidationInvalid,
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
 * The common part of the three updates of the edit panel: 401 without a valid access token; 404
 * `resource.not_found` for an `idHourlyService` that is not one of its hourly services; 400 with
 * one error per wrong field; otherwise `apply` changes the data of its tab and the answer is 204
 * without a body.
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
  const property = DEMO_PROPERTY;
  const row = HOURLY_SERVICES.find(
    (item) => item.idHourlyService === body.idHourlyService && item.propertyId === property.id,
  );
  if (!row?.idHourlyService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  const details = structuredClone(hourlyDetailsOf(row));
  apply(details, structuredClone(body), row);
  HOURLY_SERVICE_DETAILS.set(row.idHourlyService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-hourly-service-general` with `HourlyServiceUpdateGeneralRequest`:
 * names, descriptions, icon and Metti in evidenza; the list shows the new name (language of
 * `Accept-Language`). Errors as the creation (see `standardGeneralErrors`).
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
 * `POST /api/catalog/update-hourly-service-prices` with `HourlyServiceUpdatePricesRequest`: the
 * Prezzi tab. 400 for a price below 0.
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
 * `POST /api/catalog/update-hourly-service-booking` with `HourlyServiceUpdateBookingRequest`: the
 * booking options of the Prenotazioni tab, all of them. 400 for a price below 0.
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
 * `GET /api/catalog/get-automatic-price-services`: the bar, standard and hourly services of the
 * property, as `{ id, value }` with the name, for "Validità sui servizi". 401 without a valid
 * access token.
 */
export const automaticPriceServicesMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const property = DEMO_PROPERTY;
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

type CreateAutomaticPriceBody = Partial<AutomaticPriceAddItemRequest>;

/**
 * `POST /api/catalog/create-automatic-price` with `AutomaticPriceAddItemRequest`: the new automatic
 * price goes into the list, with the name in the language of `Accept-Language`. 204 without a body;
 * 401 without a valid access token; 404 `resource.not_found` for a service that is not one of the
 * property; 400 with one error per wrong field (name and description in the language of the
 * request, icon of one of the two lists, price not below 0).
 */
export const createAutomaticPriceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateAutomaticPriceBody;
  const property = DEMO_PROPERTY;
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
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
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
 * The common part of the two updates of the edit panel: 401 without a valid access token; 404
 * `resource.not_found` for an `idAutomaticPrice` that is not one of its automatic prices (or for a
 * service that is not one of the property); 400 with one error per wrong field; otherwise `apply`
 * changes the data of its tab and the answer is 204 without a body.
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
  const property = DEMO_PROPERTY;
  const row = AUTOMATIC_PRICES.find(
    (item) => item.idAutomaticPrice === body.idAutomaticPrice && item.propertyId === property.id,
  );
  const errors = row ? errorsOf(body, property.id) : null;
  if (!row?.idAutomaticPrice || !errors) {
    return resourceNotFound(request);
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  const details = structuredClone(automaticDetailsOf(row));
  apply(details, structuredClone(body), row);
  AUTOMATIC_PRICE_DETAILS.set(row.idAutomaticPrice, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-automatic-price-general` with `AutomaticPriceUpdateGeneralRequest`:
 * names, descriptions and icon; the list shows the new name (language of `Accept-Language`). Errors
 * as the creation.
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
 * `AutomaticPriceUpdatePricesValidityRequest`: the Prezzi e Validità tab; the list shows the new
 * price. 400 for a price below 0; 404 for a service that is not one of the property.
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
    : [{ field: 'icon', code: ApiErrorCode.ValidationInvalid }];
}

/**
 * `POST /api/catalog/automatic-price-list`: one page of the automatic prices of the property of the
 * token, searched in the name. 401 without a valid access token.
 */
export const automaticPriceListMock = (
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
 * `POST /api/catalog/table-service-list`: one page of the table services (e.g. lunch, dinner) of
 * the property of the token, searched in the name. 401 without a valid access token.
 */
export const tableServiceListMock = (
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
 * `POST /api/catalog/delete-standard-service-item` with `{ idStandardServices }`: all or nothing.
 * If one id is not a service of the property nothing is deleted (404 `resource.not_found`).
 * Repeated ids count once. 204 without a body; 401 without a valid access token.
 */
export const deleteStandardServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idStandardServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/duplicate-standard-service-item` with `{ idStandardServices }`:
 * all or nothing, like the delete. Each copy keeps the price; the name is chosen by the backend:
 * the mock invents "Doccia calda (copia)", then "Doccia calda (copia 2)" and so on.
 */
export const duplicateStandardServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idStandardServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/delete-hourly-service-item` with `{ idHourlyServices }`: all or nothing. If
 * one id is not an hourly service of the property nothing is deleted (404 `resource.not_found`).
 * Repeated ids count once. 204 without a body; 401 without a valid access token.
 */
export const deleteHourlyServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idHourlyServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/duplicate-hourly-service-item` with `{ idHourlyServices }`: all
 * or nothing, like the delete. The name of each copy is chosen by the backend: the mock invents
 * "Sup (copia)", then "Sup (copia 2)" and so on.
 */
export const duplicateHourlyServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idHourlyServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/delete-automatic-price-item` with `{ idAutomaticPrices }`: all or nothing. If
 * one id is not an automatic price of the property nothing is deleted (404 `resource.not_found`).
 * Repeated ids count once. 204 without a body; 401 without a valid access token.
 */
export const deleteAutomaticPriceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idAutomaticPrices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/duplicate-automatic-price-item` with `{ idAutomaticPrices }`:
 * all or nothing, like the delete. Each copy keeps the price; the name is chosen by the backend:
 * the mock invents "Coperto (copia)", then "Coperto (copia 2)" and so on.
 */
export const duplicateAutomaticPriceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idAutomaticPrices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/delete-table-service-item` with `{ idTableServices }`: all or nothing. If one
 * id is not a table service of the property nothing is deleted (404 `resource.not_found`). Repeated
 * ids count once. 204 without a body; 401 without a valid access token.
 */
export const deleteTableServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idTableServices?: string[] };
  const property = DEMO_PROPERTY;
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
 * `POST /api/catalog/duplicate-table-service-item` with `{ idTableServices }`: all or
 * nothing, like the delete. The name of each copy is chosen by the backend: the mock invents
 * "Pranzo (copia)", then "Pranzo (copia 2)" and so on.
 */
export const duplicateTableServiceItemsMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as { idTableServices?: string[] };
  const property = DEMO_PROPERTY;
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

type CreateTableServiceBody = Partial<TableServiceAddItemRequest>;

/**
 * `POST /api/catalog/create-table-service` with `TableServiceAddItemRequest`: the new table service
 * goes into the list, with the name in the language of `Accept-Language`. 204 without a body; 401
 * without a valid access token; 400 with one error per wrong field (name and description in the
 * language of the request, choices among the values of their enums, quantities whole and above 0,
 * deposit from 0 to 100).
 */
export const createTableServiceMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateTableServiceBody;
  const property = DEMO_PROPERTY;
  const language = request.headers.get('Accept-Language') ?? 'it';
  const errors = [
    ...standardGeneralErrors({ translations: body.translations }, language),
    ...tableServiceErrors(body),
  ];
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
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
 * The common part of the two updates of the edit panel: 401 without a valid access token; 404
 * `resource.not_found` for an `idTableService` that is not one of its table services; 400 with one
 * error per wrong field; otherwise `apply` changes the data of its tab and the answer is 204
 * without a body.
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
  const property = DEMO_PROPERTY;
  const row = TABLE_SERVICES.find(
    (item) => item.idTableService === body.idTableService && item.propertyId === property.id,
  );
  if (!row?.idTableService) {
    return resourceNotFound(request);
  }
  const errors = errorsOf(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  const details = structuredClone(tableDetailsOf(row));
  apply(details, structuredClone(body), row);
  TABLE_SERVICE_DETAILS.set(row.idTableService, details);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
}

/**
 * `POST /api/catalog/update-table-service-general` with `TableServiceUpdateGeneralRequest`: names,
 * descriptions and Metti in evidenza; the list shows the new name (language of `Accept-Language`).
 * Errors as the creation.
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
 * `TableServiceUpdateAvailabilityBookingsRequest`: the Disponibilità e Prenotazioni tab. Errors as
 * the creation (see `tableServiceErrors`).
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
    errors.push({ field, code: ApiErrorCode.ValidationInvalid });
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
