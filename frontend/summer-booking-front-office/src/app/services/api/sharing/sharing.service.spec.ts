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
        method === 'GET' && url === `${environment.apiBaseUrl}/booking/sharing/general-information`,
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
      isSharingEnabled: true,
      minimumBookingDayQuantity: 3,
      version: 'v1',
    });
    let version: string | null | undefined;

    sharing
      .saveInformation(information, information.version)
      .subscribe((saved) => (version = saved));

    const post = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/booking/sharing/save-information`,
    );
    expect(post.request.body).toEqual({
      ...new SharingSaveInformationRequest(),
      isSharingEnabled: true,
      minimumBookingDayQuantity: 3,
    });
    expect(post.request.headers.get('If-Match')).toBe('"v1"');
    post.flush(null, { status: 204, statusText: 'No Content', headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });
});
