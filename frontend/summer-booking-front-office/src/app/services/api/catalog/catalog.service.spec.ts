import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { BarServiceCategory } from '../../../entities/enums/bar-service-category';
import { BarServiceIcon } from '../../../entities/enums/bar-service-icon';
import { BookingTimeInterval } from '../../../entities/enums/booking-time-interval';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { BarServiceAddItemRequest } from '../../../entities/settings/services/bar-service-add-item-request';
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
});
