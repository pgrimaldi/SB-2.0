import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { OnlinePaymentMethod } from '../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../entities/enums/supplement-type';
import { OnlineBookingOptions } from '../../../entities/settings/online-booking-configuration/online-booking-options';
import { OnlineBookingOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-options-update-request';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineBookingServicesOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { OnlineSettingsService } from './online-settings.service';

describe('OnlineSettingsService', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      onlineSettings: TestBed.inject(OnlineSettingsService),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  it('should read the online booking options with the days at midnight UTC and the times without seconds', () => {
    const { onlineSettings, controller } = setup();
    let options: OnlineBookingOptions | undefined;

    onlineSettings.onlineBookingOptions().subscribe((read) => (options = read));

    controller
      .expectOne(
        ({ method, url, params }) =>
          method === 'GET' &&
          url === `${environment.apiBaseUrl}/catalog/online-settings` &&
          params.keys().length === 0,
      )
      .flush({
        ...new OnlineBookingOptions(),
        allowedPeriods: [{ startsOn: '2027-05-15', endsOn: '2027-09-20' }],
        morningFrom: '08:00:00',
        afternoonConversionTime: '12:30:00',
        stayLimits: [
          {
            id: 'l1',
            sectorId: 's1',
            startsOn: '2027-08-01',
            endsOn: '2027-08-31',
            minDays: 3,
            maxDays: null,
          },
        ],
        version: 'v1',
      });
    expect(options).toEqual({
      ...new OnlineBookingOptions(),
      allowedPeriods: [
        { startsOn: '2027-05-15T00:00:00.000Z', endsOn: '2027-09-20T00:00:00.000Z' },
      ],
      morningFrom: '08:00',
      afternoonConversionTime: '12:30',
      stayLimits: [
        {
          id: 'l1',
          sectorId: 's1',
          startsOn: '2027-08-01T00:00:00.000Z',
          endsOn: '2027-08-31T00:00:00.000Z',
          minDays: 3,
          maxDays: null,
        },
      ],
      version: 'v1',
    });
    controller.verify();
  });

  it('should not read options without a field the save would send back empty', () => {
    const { onlineSettings, controller } = setup();
    const failed = vi.fn();

    onlineSettings.onlineBookingOptions().subscribe({ error: failed });

    const { legend, ...answer } = new OnlineBookingOptions();
    expect(legend).toEqual([]);
    controller.expectOne(`${environment.apiBaseUrl}/catalog/online-settings`).flush(answer);
    expect(failed).toHaveBeenCalled();
    controller.verify();
  });

  it('should save every online booking option with the days of the backend, the version in If-Match, and give back the new one', () => {
    const { onlineSettings, controller } = setup();
    const options = Object.assign(new OnlineBookingOptions(), {
      allowedSpotIds: ['spot1'],
      isBlocked: true,
      allowedPeriods: [
        { startsOn: '2027-05-15T00:00:00.000Z', endsOn: '2027-09-20T00:00:00.000Z' },
      ],
      morningFrom: '08:00',
      stayLimits: [
        {
          id: 'l1',
          sectorId: 's1',
          startsOn: '2027-08-01T00:00:00.000Z',
          endsOn: '2027-08-31T00:00:00.000Z',
          minDays: 3,
          maxDays: null,
        },
      ],
      legend: [{ id: 'e1', label: 'Libero', iconId: null, iconUrl: '/icon.svg' }],
      version: 'v1',
    });
    let version: string | null | undefined;

    onlineSettings
      .updateOnlineBookingOptions(options, options.version)
      .subscribe((saved) => (version = saved));

    const put = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/catalog/online-settings`,
    );
    expect(put.request.body).toEqual({
      ...new OnlineBookingOptionsUpdateRequest(),
      allowedSpotIds: ['spot1'],
      isBlocked: true,
      allowedPeriods: [{ startsOn: '2027-05-15', endsOn: '2027-09-20' }],
      morningFrom: '08:00',
      stayLimits: [
        {
          id: 'l1',
          sectorId: 's1',
          startsOn: '2027-08-01',
          endsOn: '2027-08-31',
          minDays: 3,
          maxDays: null,
        },
      ],
      legend: [{ id: 'e1', label: 'Libero', iconId: null, iconUrl: '/icon.svg' }],
    });
    expect(put.request.headers.get('If-Match')).toBe('"v1"');
    put.flush({ ...options, version: 'v2' }, { headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });

  it('should ask for the online services options without the property, taken from the token', () => {
    const { onlineSettings, controller } = setup();

    onlineSettings.onlineServicesOptions().subscribe();

    const request = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/online-settings/services` &&
        params.keys().length === 0,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save every option read without the property, with its version in If-Match, and give back the new one', () => {
    const { onlineSettings, controller } = setup();
    const options = Object.assign(new OnlineBookingServicesOptions(), {
      surchargeType: SupplementType.Fixed,
      surchargeValue: 2.5,
      paymentMethods: [OnlinePaymentMethod.PayNow],
      version: 'v1',
      unexpectedFlag: 'extra-field',
    });
    let version: string | null | undefined;

    onlineSettings
      .updateOnlineServicesOptions(options, options.version)
      .subscribe((saved) => (version = saved));

    const put = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/catalog/online-settings/services`,
    );
    expect(put.request.body).toEqual({
      ...new OnlineBookingServicesOptionsUpdateRequest(),
      surchargeType: 'fixed',
      surchargeValue: 2.5,
      paymentMethods: ['pay_now'],
    });
    expect(put.request.headers.get('If-Match')).toBe('"v1"');
    put.flush({ ...options, version: 'v2' }, { headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });
});
