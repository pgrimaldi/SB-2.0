import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
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
});
