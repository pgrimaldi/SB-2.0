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
