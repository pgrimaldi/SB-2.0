import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SystemService } from './system.service';

describe('SystemService', () => {
  it('should ask for the Stripe information of the property in the query string', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const system = TestBed.inject(SystemService);
    const controller = TestBed.inject(HttpTestingController);

    system.stripeInformation('p1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/system/get-stripe-information` &&
        params.get('idProperty') === 'p1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });
});
