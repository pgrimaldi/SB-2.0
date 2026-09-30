import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  isDevMode,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { DATE_FORMATS, DateLanguageBehaviour } from './behaviours/i18n/date-language.behaviour';
import { AuthBehaviour } from './behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from './behaviours/i18n/language.behaviour';
import { authInterceptor } from './services/api/auth.interceptor';
import { timeZoneInterceptor } from './services/api/time-zone.interceptor';
import { MOCK_INTERCEPTORS } from './services/mocks/mock-interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([timeZoneInterceptor, authInterceptor, ...MOCK_INTERCEPTORS]),
    ),
    // Dates through the browser (Intl), as each language writes them; no date library.
    provideNativeDateAdapter(DATE_FORMATS),
    provideTranslateService({
      loader: provideTranslateHttpLoader({
        prefix: '/assets/i18n/',
        suffix: '.json',
        failOnError: true,
      }),
      fallbackLang: 'it',
      lang: 'it',
    }),
    // Pages without a language in the URL (private area) use the saved preference.
    provideAppInitializer(() => {
      const languageBehaviour = inject(LanguageBehaviour);
      const preferred = languageBehaviour.preferred();
      languageBehaviour.use(preferred);
      return languageBehaviour.loadAll(preferred);
    }),
    // Calendars and date fields follow the language (locale of the date adapter).
    provideAppInitializer(() => {
      inject(DateLanguageBehaviour);
    }),
    // A previous sign-in: new access token from the refresh cookie before the first navigation.
    provideAppInitializer(() => inject(AuthBehaviour).restore()),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
