import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { BarServiceCategory } from '../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../entities/enums/bar-service-icon';
import { Weekday } from '../../../entities/enums/weekday';
import { ServiceTimeSlot } from '../../../entities/enums/service-time-slot';
import { StandardServiceIcon } from '../../../entities/enums/standard-service-icon';
import { TableServiceAvailabilityMode } from '../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../entities/enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../../entities/enums/table-service-deposit-mode';
import { BathhouseDocument } from '../../../entities/enums/bathhouse-document';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../../entities/settings/bathhouse-configuration/bathhouse-media';

import { BathhouseMediaRequest } from '../../../entities/settings/bathhouse-configuration/bathhouse-media-request';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { AutomaticPriceAddItemRequest } from '../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceUpdateGeneralRequest } from '../../../entities/settings/services/automatic-price-update-general-request';
import { AutomaticPriceUpdatePricesValidityRequest } from '../../../entities/settings/services/automatic-price-update-prices-validity-request';
import { BarServiceAddItemRequest } from '../../../entities/settings/services/bar-service-add-item-request';
import { BarServiceUpdateBookingRequest } from '../../../entities/settings/services/bar-service-update-booking-request';
import { BarServiceUpdateGeneralRequest } from '../../../entities/settings/services/bar-service-update-general-request';
import { BarServiceUpdatePricesRequest } from '../../../entities/settings/services/bar-service-update-prices-request';
import { HourlyServiceAddItemRequest } from '../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceBookingOption } from '../../../entities/settings/services/hourly-service-booking-option';
import { HourlyServiceUpdateBookingRequest } from '../../../entities/settings/services/hourly-service-update-booking-request';
import { HourlyServiceUpdateGeneralRequest } from '../../../entities/settings/services/hourly-service-update-general-request';
import { HourlyServiceUpdatePricesRequest } from '../../../entities/settings/services/hourly-service-update-prices-request';
import { StandardServiceAddItemRequest } from '../../../entities/settings/services/standard-service-add-item-request';
import { StandardServiceUpdateBookingRequest } from '../../../entities/settings/services/standard-service-update-booking-request';
import { StandardServiceUpdateGeneralRequest } from '../../../entities/settings/services/standard-service-update-general-request';
import { StandardServiceUpdatePricesRequest } from '../../../entities/settings/services/standard-service-update-prices-request';
import { TableServiceAddItemRequest } from '../../../entities/settings/services/table-service-add-item-request';
import { TableServiceUpdateAvailabilityBookingsRequest } from '../../../entities/settings/services/table-service-update-availability-bookings-request';
import { TableServiceUpdateGeneralRequest } from '../../../entities/settings/services/table-service-update-general-request';
import { Language } from '../../../entities/shared/language';
import { Country } from '../../../entities/shared/country';
import { etag, ifMatchOf } from '../versions/version-headers';
import { CatalogService } from './catalog.service';

const READ_VERSION = 'v1';
const SAVED_VERSION = 'v2';

describe('CatalogService', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      catalog: TestBed.inject(CatalogService),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  it("should read the backend's countries with a GET and no parameters, code and name only", () => {
    const { catalog, controller } = setup();
    let countries: Country[] | undefined;

    catalog.countries().subscribe((answer) => (countries = answer));

    const call = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/lookups/countries` &&
        params.keys().length === 0,
    );
    expect(call.request.body).toBeNull();
    call.flush([{ code: 'DE', name: 'Germania', extra: true }]);
    expect(countries).toEqual([{ code: 'DE', name: 'Germania' }]);
    controller.verify();
  });

  it('should take only the address of the public site from the public card of the property', () => {
    const { catalog, controller } = setup();
    let slug: string | null | undefined;

    catalog.propertySlug().subscribe((answer) => (slug = answer));

    controller
      .expectOne(
        ({ method, url, params }) =>
          method === 'GET' &&
          url === `${environment.apiBaseUrl}/catalog/property-info` &&
          params.keys().length === 0,
      )
      .flush({ name: 'Lido Demo', slug: 'lido-demo' });
    expect(slug).toBe('lido-demo');
    controller.verify();
  });

  it('should ask for the general information without the property, taken from the token', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationGeneral().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-general` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the services without the property, taken from the token', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationServices().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url ===
          `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-services` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the media without the property, taken from the token', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationMedia().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-media` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should upload the main photo as the file field and read the media of the public card', () => {
    const { catalog, controller } = setup();
    const image = new File(['image'], 'spiaggia.webp', { type: 'image/webp' });
    let media: BathhouseMedia | undefined;

    catalog.uploadBathhouseMainPhoto(image).subscribe((answer) => (media = answer));

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/property-info/main-photo`,
    );
    expect((request.request.body as FormData).get('file')).toBe(image);
    request.flush({
      name: 'Lido Demo',
      mainPhotoUrl: 'https://cdn.example.com/main.webp',
      photos: [
        { id: 'p2', url: 'https://cdn.example.com/bar.webp', sortOrder: 2 },
        { id: 'p1', url: 'https://cdn.example.com/beach.webp', sortOrder: 1 },
      ],
      virtualTourUrl: 'https://tour.example.com/lido',
      latitude: 44.063812,
      longitude: 12.580143,
      priceListPdf: { fileName: 'listino.pdf', url: 'https://cdn.example.com/listino.pdf' },
      menuPdf: null,
      amenityCodes: [],
    });
    expect(media).toEqual({
      mainPhotoUrl: 'https://cdn.example.com/main.webp',
      photos: [
        { idPhoto: 'p1', url: 'https://cdn.example.com/beach.webp' },
        { idPhoto: 'p2', url: 'https://cdn.example.com/bar.webp' },
      ],
      virtualTourUrl: 'https://tour.example.com/lido',
      latitude: 44.063812,
      longitude: 12.580143,
      priceListPdfUrl: 'https://cdn.example.com/listino.pdf',
      priceListPdfFileName: 'listino.pdf',
      menuPdfUrl: null,
      menuPdfFileName: null,
    });
    controller.verify();
  });

  it('should delete the main photo without a body', () => {
    const { catalog, controller } = setup();

    catalog.deleteBathhouseMainPhoto().subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' && url === `${environment.apiBaseUrl}/catalog/property-info/main-photo`,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should add a photo to the gallery as the file field', () => {
    const { catalog, controller } = setup();
    const image = new File(['image'], 'bar.png', { type: 'image/png' });

    catalog.addBathhousePhoto(image).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/property-info/photos`,
    ).request;
    expect((request.body as FormData).get('file')).toBe(image);
    controller.verify();
  });

  it('should delete a photo of the gallery by its id in the path', () => {
    const { catalog, controller } = setup();

    catalog.deleteBathhousePhoto('0193f2a4 7d2e').subscribe();

    controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' &&
        url === `${environment.apiBaseUrl}/catalog/property-info/photos/0193f2a4%207d2e`,
    );
    controller.verify();
  });

  it('should upload and delete the PDF of a document by its name in the path', () => {
    const { catalog, controller } = setup();
    const pdf = new File(['pdf'], 'listino.pdf', { type: 'application/pdf' });

    catalog.uploadBathhouseDocument(BathhouseDocument.PriceList, pdf).subscribe();
    catalog.deleteBathhouseDocument(BathhouseDocument.Menu).subscribe();

    const upload = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/property-info/documents/price-list`,
    ).request;
    expect((upload.body as FormData).get('file')).toBe(pdf);
    controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' &&
        url === `${environment.apiBaseUrl}/catalog/property-info/documents/menu`,
    );
    controller.verify();
  });

  it('should save every field of the general information without the property', () => {
    const { catalog, controller } = setup();
    const information = Object.assign(new BathhouseGeneralInformation(), {
      bathhouseName: 'Lido Sole',
      unexpectedFlag: 'extra-field',
    });

    catalog.saveBathhouseConfigurationGeneral(information, null).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-general`,
    ).request;
    expect(request.body).toEqual({
      ...new BathhouseGeneralInformation(),
      bathhouseName: 'Lido Sole',
    });
    controller.verify();
  });

  it('should save the offered services as entities without the property', () => {
    const { catalog, controller } = setup();
    const service = Object.assign(new BathhouseServiceItem(), {
      idService: 3,
      name: 'Doccia',
      isSelected: true,
      unexpectedFlag: 'extra-field',
    });

    const notOffered = Object.assign(new BathhouseServiceItem(), { idService: 7, name: 'Wifi' });
    catalog.saveBathhouseConfigurationServices([service, notOffered], null).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-services`,
    ).request;
    expect(request.body).toEqual({
      services: [
        { ...new BathhouseServiceItem(), idService: 3, name: 'Doccia', isSelected: true },
        { ...new BathhouseServiceItem(), idService: 7, name: 'Wifi', isSelected: false },
      ],
    });
    controller.verify();
  });

  it('should save the link of the virtual tour and the position, without other fields', () => {
    const { catalog, controller } = setup();
    const media = Object.assign(new BathhouseMediaRequest(), {
      virtualTourUrl: 'https://tour.example.com/lido',
      latitude: 44.063812,
      longitude: 12.580143,
      mainPhotoUrl: 'https://cdn.example.com/main.webp',
    });

    catalog.saveBathhouseConfigurationMedia(media, null).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-media`,
    ).request;
    expect(request.body).toEqual({
      virtualTourUrl: 'https://tour.example.com/lido',
      latitude: 44.063812,
      longitude: 12.580143,
    });
    controller.verify();
  });

  it('should create the bar service with the data of the three tabs, without the property', () => {
    const { catalog, controller } = setup();
    const translation = {
      language: Language.It,
      serviceName: 'Caffè ginseng',
      description: 'In tazza piccola',
    };
    const bookingOption = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      managementStartTime: '09:00',
      managementEndTime: '12:00',
      onlineStartTime: '09:00',
      onlineEndTime: '12:00',
      maxBookingPerHourQuantity: 2,
      maxBookingPerDayQuantity: 5,
      weekdays: [Weekday.Saturday],
      maxPerSlot: 5,
      intervalMinutes: 30,
      minItemPerBookingQuantity: 1,
      maxItemPerBookingQuantity: 2,
    };
    const request = {
      translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
      category: BarServiceCategory.Bar,
      icon: BarServiceIcon.Coffee,
      isHighlighted: true,
      price: 1.5,
      vatRate: 10,
      variants: [{ variantName: 'Grande', price: 0.5 }],
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 1, price: 1.8 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [bookingOption],
    };

    let idBarService: string | undefined;

    catalog
      .createBarService(
        Object.assign(new BarServiceAddItemRequest(), request, { unexpectedFlag: 'extra-field' }),
      )
      .subscribe((id) => (idBarService = id));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/create-bar-service`,
    );
    expect(call.request.body).toEqual({ ...request, translations: [translation] });
    call.flush('b9', { status: 201, statusText: 'Created' });
    expect(idBarService).toBe('b9');
    controller.verify();
  });

  it('should upload the image of a service as the file field', () => {
    const { catalog, controller } = setup();
    const image = new File(['image'], 'caffe.png', { type: 'image/png' });

    catalog.uploadServiceImage('b1', image).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/services/b1/image`,
    ).request;
    expect((request.body as FormData).get('file')).toBe(image);
    controller.verify();
  });

  it('should delete the image of a service without a body', () => {
    const { catalog, controller } = setup();

    catalog.deleteServiceImage('b1').subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' && url === `${environment.apiBaseUrl}/catalog/services/b1/image`,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should delete and duplicate the bar services under services', () => {
    const { catalog, controller } = setup();

    catalog.deleteBarServiceItems(['b1', 'b2']).subscribe();
    catalog.duplicateBarServiceItems(['b1']).subscribe();

    const bodyOf = (action: string) =>
      controller.expectOne(
        ({ method, url }) =>
          method === 'POST' &&
          url === `${environment.apiBaseUrl}/catalog/services/${action}-bar-service-item`,
      ).request.body;
    expect(bodyOf('delete')).toEqual({ idBarServices: ['b1', 'b2'] });
    expect(bodyOf('duplicate')).toEqual({ idBarServices: ['b1'] });
    controller.verify();
  });

  it('should save the Info Generali of the bar service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
      translations: [{ language: Language.It, serviceName: 'Caffè', description: 'Espresso' }],
      category: BarServiceCategory.Bar,
      icon: BarServiceIcon.Coffee,
      isHighlighted: true,
    };

    catalog
      .updateBarServiceGeneral(
        Object.assign(new BarServiceUpdateGeneralRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should send a text made only of spaces as no text', () => {
    const { catalog, controller } = setup();

    catalog
      .updateBarServiceGeneral(
        Object.assign(new BarServiceUpdateGeneralRequest(), {
          translations: [
            { language: Language.It, serviceName: 'Pizza', description: 'Al taglio' },
            { language: Language.En, serviceName: '  ', description: ' ' },
          ],
        }),
        null,
      )
      .subscribe();

    const { body } = controller.expectOne(({ method }) => method === 'POST').request;
    expect(body.translations).toEqual([
      { language: Language.It, serviceName: 'Pizza', description: 'Al taglio' },
      { language: Language.En, serviceName: null, description: null },
    ]);
    controller.verify();
  });

  it('should save the prices of the bar service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
      price: 1,
      vatRate: 10,
      variants: [{ variantName: 'Doppio', price: 0.8 }],
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 1, price: 1.2 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateBarServicePrices(
        Object.assign(new BarServiceUpdatePricesRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the booking options of the bar service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
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
          weekdays: [Weekday.Saturday],
          maxPerSlot: 5,
          intervalMinutes: 30,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 4,
        },
      ],
    };

    catalog
      .updateBarServiceBooking(
        Object.assign(new BarServiceUpdateBookingRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-booking`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should ask for the general information of the bar service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.barServiceGeneralInfo('b1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-general-info` &&
        params.get('idBarService') === 'b1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the prices of the bar service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.barServicePrices('b1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-prices` &&
        params.get('idBarService') === 'b1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the booking options of the bar service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.barServiceBookingOptions('b1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-booking-options` &&
        params.get('idBarService') === 'b1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should create the service with the data of the three tabs, without the property', () => {
    const { catalog, controller } = setup();
    const translation = {
      language: Language.It,
      serviceName: 'Doccia calda',
      description: 'Acqua calda tutto il giorno',
    };
    const request = {
      translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
      icon: StandardServiceIcon.Shower,
      isHighlighted: true,
      price: 3,
      vatRate: 22,
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 1, price: 3.5 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          managementStartTime: '09:00',
          managementEndTime: '18:00',
          onlineStartTime: '09:00',
          onlineEndTime: '17:00',
          maxBookingPerHourQuantity: 4,
          maxBookingPerDayQuantity: 30,
          weekdays: [Weekday.Saturday],
          maxPerSlot: 5,
          intervalMinutes: 15,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 2,
        },
      ],
    };

    let idStandardService: string | undefined;

    catalog
      .createStandardService(
        Object.assign(new StandardServiceAddItemRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe((id) => (idStandardService = id));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/create-standard-service`,
    );
    expect(call.request.body).toEqual({ ...request, translations: [translation] });
    call.flush('s9', { status: 201, statusText: 'Created' });
    expect(idStandardService).toBe('s9');
    controller.verify();
  });

  it('should save the Info Generali of the service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idStandardService: 's1',
      translations: [{ language: Language.It, serviceName: 'Parcheggio', description: 'Coperto' }],
      icon: StandardServiceIcon.Parking,
      isHighlighted: false,
    };

    catalog
      .updateStandardServiceGeneral(
        Object.assign(new StandardServiceUpdateGeneralRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the prices of the service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idStandardService: 's1',
      price: 3,
      vatRate: 22,
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 1, price: 4 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateStandardServicePrices(
        Object.assign(new StandardServiceUpdatePricesRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the booking options of the service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idStandardService: 's1',
      bookingOptions: [
        {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          managementStartTime: '09:00',
          managementEndTime: '18:00',
          onlineStartTime: '09:00',
          onlineEndTime: '17:00',
          maxBookingPerHourQuantity: 4,
          maxBookingPerDayQuantity: 30,
          weekdays: [Weekday.Saturday],
          maxPerSlot: 5,
          intervalMinutes: 60,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 1,
        },
      ],
    };

    catalog
      .updateStandardServiceBooking(
        Object.assign(new StandardServiceUpdateBookingRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-booking`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should ask for the general information of the service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.standardServiceGeneralInfo('s1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-standard-service-general-info` &&
        params.get('idStandardService') === 's1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the prices of the service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.standardServicePrices('s1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-standard-service-prices` &&
        params.get('idStandardService') === 's1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the booking options of the service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.standardServiceBookingOptions('s1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-standard-service-booking-options` &&
        params.get('idStandardService') === 's1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  /** An availability as the panel sends it, with an extra field at every level to drop. */
  const availability = () => {
    const clean = {
      startTime: '09:00',
      endTime: '19:00',
      daysOfWeek: [1, 2, 3],
      timeSlotPrices: [
        { timeSlot: ServiceTimeSlot.ThirtyMinutes, isEnabled: true, price: 8 },
        { timeSlot: ServiceTimeSlot.OneHour, isEnabled: false, price: null },
      ],
      isCustomTimeSlotEnabled: true,
      customTimeSlotMinuteQuantity: 20,
      customTimeSlotPrice: 6,
      vatRate: 22,
      maxBookingPerTimeSlotQuantity: 4,
      hasFixedTimeSlots: true,
    };
    const sent = {
      ...clean,
      timeSlotPrices: clean.timeSlotPrices.map((timeSlotPrice) => ({
        ...timeSlotPrice,
        unexpectedFlag: 'extra-field',
      })),
      unexpectedFlag: 'extra-field',
    };
    return { clean, sent };
  };

  it('should create the hourly service with only the contract fields, without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Sup', description: 'Tavola a ore' };
    const seasonOption = availability();
    const periodOption = availability();
    const bookingOption = availability();
    const request = {
      translations: [translation],
      icon: StandardServiceIcon.Surfing,
      isHighlighted: true,
      price: 15,
      vatRate: 22,
      isPeriodPricingEnabled: true,
      availabilityOptions: [seasonOption.clean],
      periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [periodOption.clean] }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        Object.assign(new HourlyServiceBookingOption(), {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          weekdays: [Weekday.Saturday],
          intervalMinutes: 20,
          availabilityOption: bookingOption.clean,
        }),
      ],
    };

    catalog
      .createHourlyService(
        Object.assign(new HourlyServiceAddItemRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          availabilityOptions: [seasonOption.sent],
          periodAvailabilities: [
            {
              idPeriod: 1,
              availabilityOptions: [periodOption.sent],
              unexpectedFlag: 'extra-field',
            },
          ],
          bookingOptions: [
            {
              ...request.bookingOptions[0],
              availabilityOption: bookingOption.sent,
              unexpectedFlag: 'extra-field',
            },
          ],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/create-hourly-service`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the Info Generali of the hourly service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idHourlyService: 'h1',
      translations: [{ language: Language.It, serviceName: 'Pedalò', description: 'A ore' }],
      icon: StandardServiceIcon.Sailing,
      isHighlighted: false,
    };

    catalog
      .updateHourlyServiceGeneral(
        Object.assign(new HourlyServiceUpdateGeneralRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the prices of the hourly service with only the contract fields', () => {
    const { catalog, controller } = setup();
    const option = availability();
    const request = {
      idHourlyService: 'h1',
      price: 15,
      vatRate: null,
      isPeriodPricingEnabled: true,
      availabilityOptions: [option.clean],
      periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [option.clean] }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateHourlyServicePrices(
        Object.assign(new HourlyServiceUpdatePricesRequest(), {
          ...request,
          availabilityOptions: [option.sent],
          periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [option.sent] }],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the booking options of the hourly service with only the contract fields', () => {
    const { catalog, controller } = setup();
    const option = availability();
    const bookingOption = Object.assign(new HourlyServiceBookingOption(), {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      maxPerSlot: 3,
      availabilityOption: option.clean,
    });
    const withoutAvailability = new HourlyServiceBookingOption();
    const request = {
      idHourlyService: 'h1',
      bookingOptions: [bookingOption, withoutAvailability],
    };

    catalog
      .updateHourlyServiceBooking(
        Object.assign(new HourlyServiceUpdateBookingRequest(), {
          ...request,
          bookingOptions: [
            { ...bookingOption, availabilityOption: option.sent },
            withoutAvailability,
          ],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-booking`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should ask for the general information of the hourly service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.hourlyServiceGeneralInfo('h1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-general-info` &&
        params.get('idHourlyService') === 'h1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the prices of the hourly service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.hourlyServicePrices('h1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-prices` &&
        params.get('idHourlyService') === 'h1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the booking options of the hourly service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.hourlyServiceBookingOptions('h1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-booking-options` &&
        params.get('idHourlyService') === 'h1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the services of the automatic prices without the property, taken from the token', () => {
    const { catalog, controller } = setup();

    catalog.automaticPriceServices().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-services` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should create the automatic price with only the contract fields, without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Coperto', description: 'A persona' };
    const timeSlot = { startTime: '12:00', endTime: '15:00' };
    const validityPeriod = {
      dateFrom: '2026-08-01T00:00:00.000Z',
      dateTo: '2026-08-31T00:00:00.000Z',
      daysOfWeek: [1, 2, 3],
      timeSlots: [timeSlot],
    };
    const request = {
      translations: [translation],
      icon: BarServiceIcon.Cutlery,
      price: 2,
      vatRate: 10,
      isOnlinePurchaseEnabled: true,
      validityPeriods: [validityPeriod],
      isAllServicesEnabled: false,
      idBarServices: ['b1'],
      idStandardServices: [],
      idHourlyServices: ['h1'],
      isPricePerServiceEnabled: true,
      idSectors: ['s1'],
    };

    catalog
      .createAutomaticPrice(
        Object.assign(new AutomaticPriceAddItemRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          validityPeriods: [
            {
              ...validityPeriod,
              timeSlots: [{ ...timeSlot, unexpectedFlag: 'extra-field' }],
              unexpectedFlag: 'extra-field',
            },
          ],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/automatic-prices/create-automatic-price`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should upload and delete the image of an automatic price under automatic-prices', () => {
    const { catalog, controller } = setup();
    const image = new File(['image'], 'coperto.png', { type: 'image/png' });

    catalog.uploadAutomaticPriceImage('a1', image).subscribe();
    catalog.deleteAutomaticPriceImage('a1').subscribe();

    const bodyOf = (wanted: string) =>
      controller.expectOne(
        ({ method, url }) =>
          method === wanted &&
          url === `${environment.apiBaseUrl}/catalog/automatic-prices/a1/image`,
      ).request.body;
    expect((bodyOf('POST') as FormData).get('file')).toBe(image);
    expect(bodyOf('DELETE')).toBeNull();
    controller.verify();
  });

  it('should ask for the general information of the automatic price in the query string', () => {
    const { catalog, controller } = setup();

    catalog.automaticPriceGeneralInfo('a1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url ===
          `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-general-info` &&
        params.get('idAutomaticPrice') === 'a1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the prices and validity of the automatic price in the query string', () => {
    const { catalog, controller } = setup();

    catalog.automaticPricePricesValidity('a1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url ===
          `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-prices-validity` &&
        params.get('idAutomaticPrice') === 'a1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save the Info Generali of the automatic price without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Coperto', description: 'A persona' };
    const request = {
      idAutomaticPrice: 'a1',
      translations: [translation],
      icon: StandardServiceIcon.Pool,
    };

    catalog
      .updateAutomaticPriceGeneral(
        Object.assign(new AutomaticPriceUpdateGeneralRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/automatic-prices/update-automatic-price-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the prices and validity of the automatic price with only the contract fields', () => {
    const { catalog, controller } = setup();
    const timeSlot = { startTime: '19:00', endTime: '23:00' };
    const validityPeriod = {
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-09-30T00:00:00.000Z',
      daysOfWeek: [6, 7],
      timeSlots: [timeSlot],
    };
    const request = {
      idAutomaticPrice: 'a1',
      price: 2.5,
      vatRate: 10,
      isOnlinePurchaseEnabled: false,
      validityPeriods: [validityPeriod],
      isAllServicesEnabled: false,
      idBarServices: ['b1'],
      idStandardServices: ['s1'],
      idHourlyServices: [],
      isPricePerServiceEnabled: false,
      idSectors: [],
    };

    catalog
      .updateAutomaticPricePricesValidity(
        Object.assign(new AutomaticPriceUpdatePricesValidityRequest(), {
          ...request,
          validityPeriods: [
            {
              ...validityPeriod,
              timeSlots: [{ ...timeSlot, unexpectedFlag: 'extra-field' }],
              unexpectedFlag: 'extra-field',
            },
          ],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/automatic-prices/update-automatic-price-prices-validity`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should create the table service with only the contract fields, without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Pranzo', description: 'Ai tavoli' };
    const otherTimeSlot = { startTime: '19:00', endTime: '22:00' };
    const day = {
      dayOfWeek: 1,
      isEnabled: true,
      startTime: '12:00',
      endTime: '15:00',
      otherTimeSlots: [otherTimeSlot],
    };
    const availability = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      days: [day],
    };
    const tableGroup = { tableQuantity: 10, seatPerTableQuantity: 2 };
    const request = {
      translations: [translation],
      isHighlighted: true,
      availabilities: [availability],
      availabilityMode: TableServiceAvailabilityMode.Tables,
      tableGroups: [tableGroup],
      totalSeatQuantity: null,
      averageMealMinutes: 90,
      maxBookingPerTimeSlotQuantity: 8,
      minSeatPerBookingQuantity: 2,
      maxSeatPerBookingQuantity: null,
      bookingPayment: TableServiceBookingPayment.Deposit,
      depositMode: TableServiceDepositMode.PerPerson,
      depositPercentage: 20,
      depositAmount: null,
      isOnlineBookingEnabled: true,
    };

    catalog
      .createTableService(
        Object.assign(new TableServiceAddItemRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          availabilities: [
            {
              ...availability,
              isSameTimeForAllDays: true,
              startTime: '12:00',
              endTime: '15:00',
              days: [
                {
                  ...day,
                  otherTimeSlots: [{ ...otherTimeSlot, unexpectedFlag: 'extra-field' }],
                  unexpectedFlag: 'extra-field',
                },
              ],
              unexpectedFlag: 'extra-field',
            },
          ],
          tableGroups: [{ ...tableGroup, unexpectedFlag: 'extra-field' }],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/table-services/create-table-service`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should ask for the general information of the table service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.tableServiceGeneralInfo('t1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/table-services/get-table-service-general-info` &&
        params.get('idTableService') === 't1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the availability and bookings of the table service in the query string', () => {
    const { catalog, controller } = setup();

    catalog.tableServiceAvailabilityBookings('t1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url ===
          `${environment.apiBaseUrl}/catalog/table-services/get-table-service-availability-bookings` &&
        params.get('idTableService') === 't1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save the Info Generali of the table service without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Cena', description: 'Ai tavoli' };
    const request = { idTableService: 't1', translations: [translation], isHighlighted: true };

    catalog
      .updateTableServiceGeneral(
        Object.assign(new TableServiceUpdateGeneralRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/table-services/update-table-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the Disponibilità e Prenotazioni of the table service with only its fields', () => {
    const { catalog, controller } = setup();
    const day = {
      dayOfWeek: 6,
      isEnabled: true,
      startTime: '19:00',
      endTime: '23:00',
      otherTimeSlots: [],
    };
    const availability = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      days: [day],
    };
    const request = {
      idTableService: 't1',
      availabilities: [availability],
      availabilityMode: TableServiceAvailabilityMode.Seats,
      tableGroups: [],
      totalSeatQuantity: 40,
      averageMealMinutes: 100,
      maxBookingPerTimeSlotQuantity: 10,
      minSeatPerBookingQuantity: null,
      maxSeatPerBookingQuantity: 6,
      bookingPayment: TableServiceBookingPayment.None,
      depositMode: TableServiceDepositMode.Total,
      depositPercentage: null,
      depositAmount: null,
      isOnlineBookingEnabled: false,
    };

    catalog
      .updateTableServiceAvailabilityBookings(
        Object.assign(new TableServiceUpdateAvailabilityBookingsRequest(), {
          ...request,
          availabilities: [
            {
              ...availability,
              isSameTimeForAllDays: false,
              startTime: null,
              endTime: null,
              days: [{ ...day, unexpectedFlag: 'extra-field' }],
              unexpectedFlag: 'extra-field',
            },
          ],
          unexpectedFlag: 'extra-field',
        }),
        null,
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/table-services/update-table-service-availability-bookings`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should send the version read in If-Match with every save and answer the new one of the ETag', () => {
    const { catalog, controller } = setup();
    const saves: [string, () => Observable<string | null>][] = [
      [
        'property-info/save-bathhouse-configuration-general',
        () =>
          catalog.saveBathhouseConfigurationGeneral(
            new BathhouseGeneralInformation(),
            READ_VERSION,
          ),
      ],
      [
        'property-info/save-bathhouse-configuration-services',
        () =>
          catalog.saveBathhouseConfigurationServices([new BathhouseServiceItem()], READ_VERSION),
      ],
      [
        'property-info/save-bathhouse-configuration-media',
        () => catalog.saveBathhouseConfigurationMedia(new BathhouseMediaRequest(), READ_VERSION),
      ],
      [
        'services/update-bar-service-general',
        () => catalog.updateBarServiceGeneral(new BarServiceUpdateGeneralRequest(), READ_VERSION),
      ],
      [
        'services/update-bar-service-prices',
        () => catalog.updateBarServicePrices(new BarServiceUpdatePricesRequest(), READ_VERSION),
      ],
      [
        'services/update-bar-service-booking',
        () => catalog.updateBarServiceBooking(new BarServiceUpdateBookingRequest(), READ_VERSION),
      ],
      [
        'services/update-standard-service-general',
        () =>
          catalog.updateStandardServiceGeneral(
            new StandardServiceUpdateGeneralRequest(),
            READ_VERSION,
          ),
      ],
      [
        'services/update-standard-service-prices',
        () =>
          catalog.updateStandardServicePrices(
            new StandardServiceUpdatePricesRequest(),
            READ_VERSION,
          ),
      ],
      [
        'services/update-standard-service-booking',
        () =>
          catalog.updateStandardServiceBooking(
            new StandardServiceUpdateBookingRequest(),
            READ_VERSION,
          ),
      ],
      [
        'services/update-hourly-service-general',
        () =>
          catalog.updateHourlyServiceGeneral(new HourlyServiceUpdateGeneralRequest(), READ_VERSION),
      ],
      [
        'services/update-hourly-service-prices',
        () =>
          catalog.updateHourlyServicePrices(new HourlyServiceUpdatePricesRequest(), READ_VERSION),
      ],
      [
        'services/update-hourly-service-booking',
        () =>
          catalog.updateHourlyServiceBooking(new HourlyServiceUpdateBookingRequest(), READ_VERSION),
      ],
      [
        'automatic-prices/update-automatic-price-general',
        () =>
          catalog.updateAutomaticPriceGeneral(
            new AutomaticPriceUpdateGeneralRequest(),
            READ_VERSION,
          ),
      ],
      [
        'automatic-prices/update-automatic-price-prices-validity',
        () =>
          catalog.updateAutomaticPricePricesValidity(
            new AutomaticPriceUpdatePricesValidityRequest(),
            READ_VERSION,
          ),
      ],
      [
        'table-services/update-table-service-general',
        () =>
          catalog.updateTableServiceGeneral(new TableServiceUpdateGeneralRequest(), READ_VERSION),
      ],
      [
        'table-services/update-table-service-availability-bookings',
        () =>
          catalog.updateTableServiceAvailabilityBookings(
            new TableServiceUpdateAvailabilityBookingsRequest(),
            READ_VERSION,
          ),
      ],
    ];

    for (const [path, save] of saves) {
      let saved: string | null | undefined;
      save().subscribe((version) => (saved = version));
      const call = controller.expectOne(
        ({ method, url }) =>
          method === 'POST' && url === `${environment.apiBaseUrl}/catalog/${path}`,
      );
      expect(ifMatchOf(call.request.headers)).toBe(READ_VERSION);
      call.flush(null, { headers: etag(SAVED_VERSION) });
      expect(saved).toBe(SAVED_VERSION);
    }
    controller.verify();
  });
});
