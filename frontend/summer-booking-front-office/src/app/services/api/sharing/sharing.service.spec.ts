import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { SharingSaveInformationRequest } from '../../../entities/settings/sharing/sharing-save-information-request';
import { SharingService } from './sharing.service';

describe('SharingService', () => {
  it('should read the settings', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sharing = TestBed.inject(SharingService);
    const controller = TestBed.inject(HttpTestingController);

    sharing.generalInformation().subscribe();

    const get = controller.expectOne(
      ({ method, url }) =>
        method === 'GET' && url === `${environment.apiBaseUrl}/booking/sharing/settings`,
    );
    expect(get.request.params.keys()).toEqual([]);
    controller.verify();
  });

  it('should save every setting read with its version in If-Match and give back the new one', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sharing = TestBed.inject(SharingService);
    const controller = TestBed.inject(HttpTestingController);
    const information = Object.assign(new SharingGeneralInformation(), {
      isEnabled: true,
      minBookingDays: 3,
      version: 'v1',
    });
    let version: string | null | undefined;

    sharing
      .saveInformation(information, information.version)
      .subscribe((saved) => (version = saved));

    const put = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/booking/sharing/settings`,
    );
    expect(put.request.body).toEqual({
      ...new SharingSaveInformationRequest(),
      isEnabled: true,
      minBookingDays: 3,
    });
    expect(put.request.headers.get('If-Match')).toBe('"v1"');
    put.flush(information, { headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });
});
