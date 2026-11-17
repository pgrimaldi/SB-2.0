import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { BarServiceCategory } from '../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../entities/enums/bar-service-icon';
import { BookingTimeInterval } from '../../../entities/enums/booking-time-interval';
import { ServiceTimeSlot } from '../../../entities/enums/service-time-slot';
import { StandardServiceIcon } from '../../../entities/enums/standard-service-icon';
import { TableServiceAvailabilityMode } from '../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../entities/enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../../entities/enums/table-service-deposit-mode';
import { TableServiceMealDuration } from '../../../entities/enums/table-service-meal-duration';
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
import { CatalogService } from './catalog.service';

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

  it('should ask for the countries with a GET and no parameters', () => {
    const { catalog, controller } = setup();

    catalog.countriesCombobox().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/countries-combobox` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
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

    catalog.saveBathhouseConfigurationGeneral(information).subscribe();

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

    catalog.saveBathhouseConfigurationServices([service]).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url ===
          `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-services`,
    ).request;
    expect(request.body).toEqual({
      services: [{ ...new BathhouseServiceItem(), idService: 3, name: 'Doccia', isSelected: true }],
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

    catalog.saveBathhouseConfigurationMedia(media).subscribe();

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
      timeInterval: BookingTimeInterval.ThirtyMinutes,
      minItemPerBookingQuantity: 1,
      maxItemPerBookingQuantity: 2,
    };
    const request = {
      translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
      category: BarServiceCategory.Cafeteria,
      icon: BarServiceIcon.Coffee,
      isHighlighted: true,
      price: 1.5,
      vatRate: 10,
      variants: [{ variantName: 'Grande', price: 2 }],
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 1, price: 1.8 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [bookingOption],
    };

    catalog
      .createBarService(
        Object.assign(new BarServiceAddItemRequest(), request, { unexpectedFlag: 'extra-field' }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-bar-service`,
    ).request.body;
    expect(body).toEqual({ ...request, translations: [translation] });
    controller.verify();
  });

  it('should save the Info Generali of the bar service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
      translations: [{ language: Language.It, serviceName: 'Caffè', description: 'Espresso' }],
      category: BarServiceCategory.Cafeteria,
      icon: BarServiceIcon.Coffee,
      isHighlighted: true,
    };

    catalog
      .updateBarServiceGeneral(
        Object.assign(new BarServiceUpdateGeneralRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the prices of the bar service without the property', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
      price: 1,
      vatRate: 10,
      variants: [{ variantName: 'Doppio', price: 1.8 }],
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-prices`,
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
          timeInterval: BookingTimeInterval.ThirtyMinutes,
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-booking`,
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
        url === `${environment.apiBaseUrl}/catalog/get-bar-service-general-info` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-bar-service-prices` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-bar-service-booking-options` &&
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
          timeInterval: BookingTimeInterval.FifteenMinutes,
          minItemPerBookingQuantity: 1,
          maxItemPerBookingQuantity: 2,
        },
      ],
    };

    catalog
      .createStandardService(
        Object.assign(new StandardServiceAddItemRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-standard-service`,
    ).request.body;
    expect(body).toEqual({ ...request, translations: [translation] });
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-standard-service-general`,
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-standard-service-prices`,
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
          timeInterval: BookingTimeInterval.OneHour,
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-standard-service-booking`,
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
        url === `${environment.apiBaseUrl}/catalog/get-standard-service-general-info` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-standard-service-prices` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-standard-service-booking-options` &&
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
    const periodOption = availability();
    const bookingOption = availability();
    const request = {
      translations: [translation],
      icon: StandardServiceIcon.Surfing,
      isHighlighted: true,
      isPeriodPricingEnabled: true,
      periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [periodOption.clean] }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [
        {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          availabilityOption: bookingOption.clean,
        },
      ],
    };

    catalog
      .createHourlyService(
        Object.assign(new HourlyServiceAddItemRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
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
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-hourly-service`,
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-hourly-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the prices of the hourly service with only the contract fields', () => {
    const { catalog, controller } = setup();
    const option = availability();
    const request = {
      idHourlyService: 'h1',
      isPeriodPricingEnabled: true,
      periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [option.clean] }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateHourlyServicePrices(
        Object.assign(new HourlyServiceUpdatePricesRequest(), {
          ...request,
          periodAvailabilities: [{ idPeriod: 1, availabilityOptions: [option.sent] }],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-hourly-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the booking options of the hourly service with only the contract fields', () => {
    const { catalog, controller } = setup();
    const option = availability();
    const bookingOption = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      availabilityOption: option.clean,
    };
    const request = { idHourlyService: 'h1', bookingOptions: [bookingOption] };

    catalog
      .updateHourlyServiceBooking(
        Object.assign(new HourlyServiceUpdateBookingRequest(), {
          ...request,
          bookingOptions: [{ ...bookingOption, availabilityOption: option.sent }],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-hourly-service-booking`,
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
        url === `${environment.apiBaseUrl}/catalog/get-hourly-service-general-info` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-hourly-service-prices` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-hourly-service-booking-options` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-automatic-price-services` &&
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
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-automatic-price`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should ask for the general information of the automatic price in the query string', () => {
    const { catalog, controller } = setup();

    catalog.automaticPriceGeneralInfo('a1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/get-automatic-price-general-info` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-automatic-price-prices-validity` &&
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-automatic-price-general`,
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-automatic-price-prices-validity`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should create the table service with only the contract fields, without the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Pranzo', description: 'Ai tavoli' };
    const day = { dayOfWeek: 1, isEnabled: true, startTime: '12:00', endTime: '15:00' };
    const availability = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      isSameTimeForAllDays: true,
      startTime: '12:00',
      endTime: '15:00',
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
      averageMealDuration: TableServiceMealDuration.OneHourThirtyMinutes,
      maxBookingPerTimeSlotQuantity: 8,
      bookingPayment: TableServiceBookingPayment.Deposit,
      depositMode: TableServiceDepositMode.PerPerson,
      depositPercentage: 20,
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
              days: [{ ...day, unexpectedFlag: 'extra-field' }],
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
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-table-service`,
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
        url === `${environment.apiBaseUrl}/catalog/get-table-service-general-info` &&
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
        url === `${environment.apiBaseUrl}/catalog/get-table-service-availability-bookings` &&
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
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-table-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });

  it('should save the Disponibilità e Prenotazioni of the table service with only its fields', () => {
    const { catalog, controller } = setup();
    const day = { dayOfWeek: 6, isEnabled: true, startTime: '19:00', endTime: '23:00' };
    const availability = {
      dateFrom: '2026-07-01T00:00:00.000Z',
      dateTo: '2026-07-31T00:00:00.000Z',
      isSameTimeForAllDays: false,
      startTime: null,
      endTime: null,
      days: [day],
    };
    const request = {
      idTableService: 't1',
      availabilities: [availability],
      availabilityMode: TableServiceAvailabilityMode.Seats,
      tableGroups: [],
      totalSeatQuantity: 40,
      averageMealDuration: TableServiceMealDuration.TwoHours,
      maxBookingPerTimeSlotQuantity: 10,
      bookingPayment: TableServiceBookingPayment.None,
      depositMode: TableServiceDepositMode.Total,
      depositPercentage: null,
      isOnlineBookingEnabled: false,
    };

    catalog
      .updateTableServiceAvailabilityBookings(
        Object.assign(new TableServiceUpdateAvailabilityBookingsRequest(), {
          ...request,
          availabilities: [
            {
              ...availability,
              days: [{ ...day, unexpectedFlag: 'extra-field' }],
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
        url === `${environment.apiBaseUrl}/catalog/update-table-service-availability-bookings`,
    ).request.body;
    expect(body).toEqual({ ...request });
    controller.verify();
  });
});
