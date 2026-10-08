import { Language } from '../../entities/shared/language';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { environment } from '../../../environments/environment';
import { LanguageBehaviour } from '../../behaviours/i18n/language.behaviour';
import { languageInterceptor } from './language.interceptor';

describe('languageInterceptor', () => {
  it('should send the language chosen in the app to our API only', () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideTranslateService(),
        provideHttpClient(withInterceptors([languageInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const httpClient = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    const api = `${environment.apiBaseUrl}/warehouse/list`;

    httpClient.post(api, {}).subscribe();
    expect(controller.expectOne(api).request.headers.get('Accept-Language')).toBe('it');

    TestBed.inject(LanguageBehaviour).use(Language.En);
    httpClient.post(api, {}).subscribe();
    httpClient.get('https://example.com/other').subscribe();

    expect(controller.expectOne(api).request.headers.get('Accept-Language')).toBe('en');
    expect(
      controller.expectOne('https://example.com/other').request.headers.has('Accept-Language'),
    ).toBe(false);
  });
});
