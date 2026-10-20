import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SupplementType } from '../../../entities/enums/supplement-type';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { BookingService } from './booking.service';

describe('BookingService', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      booking: TestBed.inject(BookingService),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  it('should ask for the online services options of the property in the query string', () => {
    const { booking, controller } = setup();

    booking.onlineServicesOptions('p1').subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/booking/get-online-services-options` &&
        params.get('idProperty') === 'p1',
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save every option with the property in the body', () => {
    const { booking, controller } = setup();
    const options = Object.assign(new OnlineBookingServicesOptions(), {
      supplementType: SupplementType.Fixed,
      supplementValue: 2.5,
      idProperty: 'p2',
      unexpectedFlag: 'extra-field',
    });

    booking.updateOnlineServicesOptions('p1', options).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/booking/update-online-services-options`,
    ).request;
    expect(request.body).toEqual({
      ...new OnlineBookingServicesOptions(),
      supplementType: SupplementType.Fixed,
      supplementValue: 2.5,
      idProperty: 'p1',
    });
    controller.verify();
  });
});
