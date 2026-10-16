import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
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
});
