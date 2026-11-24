import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PaymentService } from './payment.service';

describe('PaymentService', () => {
  it('should ask for the Stripe information without the property, taken from the token', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const payment = TestBed.inject(PaymentService);
    const controller = TestBed.inject(HttpTestingController);

    payment.stripeInformation().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/payment/get-stripe-information` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });
});
