import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { BarServiceCategory } from '../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../entities/enums/bar-service-icon';
import { BookingTimeInterval } from '../../../entities/enums/booking-time-interval';
import { ServiceTimeSlot } from '../../../entities/enums/service-time-slot';
import { StandardServiceIcon } from '../../../entities/enums/standard-service-icon';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
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

  it('should ask for the general information of the property in the query string', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationGeneral('p1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-general` &&
        params.get('idProperty') === 'p1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the services of the property in the query string', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationServices('p1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-services` &&
        params.get('idProperty') === 'p1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask for the media of the property in the query string', () => {
    const { catalog, controller } = setup();

    catalog.bathhouseConfigurationMedia('p1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-media` &&
        params.get('idProperty') === 'p1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save every field of the general information with the property in the body', () => {
    const { catalog, controller } = setup();
    const information = Object.assign(new BathhouseGeneralInformation(), {
      bathhouseName: 'Lido Sole',
      idProperty: 'p2',
      unexpectedFlag: 'extra-field',
    });

    catalog.saveBathhouseConfigurationGeneral('p1', information).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/save-bathhouse-configuration-general`,
    ).request;
    expect(request.body).toEqual({
      ...new BathhouseGeneralInformation(),
      bathhouseName: 'Lido Sole',
      idProperty: 'p1',
    });
    controller.verify();
  });

  it('should save the offered services as entities with the property in the body', () => {
    const { catalog, controller } = setup();
    const service = Object.assign(new BathhouseServiceItem(), {
      idService: 3,
      name: 'Doccia',
      isSelected: true,
      unexpectedFlag: 'extra-field',
    });

    catalog.saveBathhouseConfigurationServices('p1', [service]).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/save-bathhouse-configuration-services`,
    ).request;
    expect(request.body).toEqual({
      services: [{ ...new BathhouseServiceItem(), idService: 3, name: 'Doccia', isSelected: true }],
      idProperty: 'p1',
    });
    controller.verify();
  });

  it('should create the bar service with the data of the three tabs and the property', () => {
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
      periodPrices: [{ idPeriod: 'a1', price: 1.8 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: true,
      bookingOptions: [bookingOption],
    };

    catalog
      .createBarService(
        'p1',
        Object.assign(new BarServiceAddItemRequest(), request, { unexpectedFlag: 'extra-field' }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-bar-service`,
    ).request.body;
    expect(body).toEqual({ ...request, translations: [translation], idProperty: 'p1' });
    controller.verify();
  });

  it('should save the Info Generali of the bar service with the property in the body', () => {
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
        'p1',
        Object.assign(new BarServiceUpdateGeneralRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-general`,
    ).request.body;
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the prices of the bar service with the property in the body', () => {
    const { catalog, controller } = setup();
    const request = {
      idBarService: 'b1',
      price: 1,
      vatRate: 10,
      variants: [{ variantName: 'Doppio', price: 1.8 }],
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 'a1', price: 1.2 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateBarServicePrices(
        'p1',
        Object.assign(new BarServiceUpdatePricesRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the booking options of the bar service with the property in the body', () => {
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
        'p1',
        Object.assign(new BarServiceUpdateBookingRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-bar-service-booking`,
    ).request.body;
    expect(body).toEqual({ ...request, idProperty: 'p1' });
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

  it('should create the service with the data of the three tabs and the property', () => {
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
      periodPrices: [{ idPeriod: 'a1', price: 3.5 }],
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
        'p1',
        Object.assign(new StandardServiceAddItemRequest(), request, {
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/create-standard-service`,
    ).request.body;
    expect(body).toEqual({ ...request, translations: [translation], idProperty: 'p1' });
    controller.verify();
  });

  it('should save the Info Generali of the service with the property in the body', () => {
    const { catalog, controller } = setup();
    const request = {
      idStandardService: 's1',
      translations: [{ language: Language.It, serviceName: 'Parcheggio', description: 'Coperto' }],
      icon: StandardServiceIcon.Parking,
      isHighlighted: false,
    };

    catalog
      .updateStandardServiceGeneral(
        'p1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the prices of the service with the property in the body', () => {
    const { catalog, controller } = setup();
    const request = {
      idStandardService: 's1',
      price: 3,
      vatRate: 22,
      isPeriodPricingEnabled: true,
      periodPrices: [{ idPeriod: 'a1', price: 4 }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateStandardServicePrices(
        'p1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the booking options of the service with the property in the body', () => {
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
        'p1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
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

  it('should create the hourly service with only the contract fields and the property', () => {
    const { catalog, controller } = setup();
    const translation = { language: Language.It, serviceName: 'Sup', description: 'Tavola a ore' };
    const periodOption = availability();
    const bookingOption = availability();
    const request = {
      translations: [translation],
      icon: StandardServiceIcon.Surfing,
      isHighlighted: true,
      isPeriodPricingEnabled: true,
      periodAvailabilities: [{ idPeriod: 'a1', availabilityOptions: [periodOption.clean] }],
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
        'p1',
        Object.assign(new HourlyServiceAddItemRequest(), {
          ...request,
          translations: [{ ...translation, unexpectedFlag: 'extra-field' }],
          periodAvailabilities: [
            {
              idPeriod: 'a1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the Info Generali of the hourly service with the property in the body', () => {
    const { catalog, controller } = setup();
    const request = {
      idHourlyService: 'h1',
      translations: [{ language: Language.It, serviceName: 'Pedalò', description: 'A ore' }],
      icon: StandardServiceIcon.Sailing,
      isHighlighted: false,
    };

    catalog
      .updateHourlyServiceGeneral(
        'p1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should save the prices of the hourly service with only the contract fields', () => {
    const { catalog, controller } = setup();
    const option = availability();
    const request = {
      idHourlyService: 'h1',
      isPeriodPricingEnabled: true,
      periodAvailabilities: [{ idPeriod: 'a1', availabilityOptions: [option.clean] }],
      idSectors: ['s1'],
      isOnlinePurchaseEnabled: false,
    };

    catalog
      .updateHourlyServicePrices(
        'p1',
        Object.assign(new HourlyServiceUpdatePricesRequest(), {
          ...request,
          periodAvailabilities: [{ idPeriod: 'a1', availabilityOptions: [option.sent] }],
          unexpectedFlag: 'extra-field',
        }),
      )
      .subscribe();

    const body = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-hourly-service-prices`,
    ).request.body;
    expect(body).toEqual({ ...request, idProperty: 'p1' });
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
        'p1',
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
    expect(body).toEqual({ ...request, idProperty: 'p1' });
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
});
