import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { TIME_ZONE_HEADER, timeZoneInterceptor } from './time-zone.interceptor';

describe('timeZoneInterceptor', () => {
  it("should send the user's time zone to our API only", () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([timeZoneInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const httpClient = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);

    httpClient.post(`${environment.apiBaseUrl}/warehouse/list`, {}).subscribe();
    httpClient.get('https://example.com/other').subscribe();

    const api = controller.expectOne(`${environment.apiBaseUrl}/warehouse/list`);
    const other = controller.expectOne('https://example.com/other');
    expect(api.request.headers.get(TIME_ZONE_HEADER)).toBe(
      Intl.DateTimeFormat().resolvedOptions().timeZone,
    );
    expect(other.request.headers.has(TIME_ZONE_HEADER)).toBe(false);
  });
});
