import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SupplementType } from '../../../entities/enums/supplement-type';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineBookingServicesOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
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

  it('should ask for the online services options without the property, taken from the token', () => {
    const { booking, controller } = setup();

    booking.onlineServicesOptions().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/booking/get-online-services-options` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save every option read without the property, with its version in If-Match, and give back the new one', () => {
    const { booking, controller } = setup();
    const options = Object.assign(new OnlineBookingServicesOptions(), {
      supplementType: SupplementType.Fixed,
      supplementValue: 2.5,
      version: 'v1',
      unexpectedFlag: 'extra-field',
    });
    let version: string | null | undefined;

    booking
      .updateOnlineServicesOptions(options, options.version)
      .subscribe((saved) => (version = saved));

    const post = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/booking/update-online-services-options`,
    );
    expect(post.request.body).toEqual({
      ...new OnlineBookingServicesOptionsUpdateRequest(),
      supplementType: SupplementType.Fixed,
      supplementValue: 2.5,
    });
    expect(post.request.headers.get('If-Match')).toBe('"v1"');
    post.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });
});
