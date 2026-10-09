import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  isDevMode,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter, withRouterConfig } from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { provideTranslateCompiler, provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { DATE_FORMATS, DateLanguageBehaviour } from './behaviours/i18n/date-language.behaviour';
import { AuthBehaviour } from './behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from './behaviours/i18n/language.behaviour';
import { DATA_RELOAD } from './components/shared/ui/data/data-reload';
import { MessageFormatCompiler } from './components/shared/i18n/message-format';
import { Language } from './entities/shared/language';
import { authInterceptor } from './services/api/auth.interceptor';
import { languageInterceptor } from './services/api/language.interceptor';
import { timeZoneInterceptor } from './services/api/time-zone.interceptor';
import { MOCK_INTERCEPTORS } from './services/mocks/mock-interceptors';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // "Resta" on the unsaved changes question after the browser's back button: the address goes back
    // to the page the user stays on (Angular's advice with canDeactivate).
    provideRouter(routes, withRouterConfig({ canceledNavigationResolution: 'computed' })),
    // The server may answer with translated data (Accept-Language): a new language loads it again.
    { provide: DATA_RELOAD, useFactory: () => inject(LanguageBehaviour).current },
    provideHttpClient(
      withInterceptors([
        timeZoneInterceptor,
        languageInterceptor,
        authInterceptor,
        ...MOCK_INTERCEPTORS,
      ]),
    ),
    // Dates through the browser (Intl), as each language writes them; no date library.
    provideNativeDateAdapter(DATE_FORMATS),
    provideTranslateService({
      loader: provideTranslateHttpLoader({
        prefix: '/assets/i18n/',
        suffix: '.json',
        failOnError: true,
      }),
      // Plurals, numbers, currencies and dates in the translations (ICU MessageFormat syntax).
      compiler: provideTranslateCompiler(MessageFormatCompiler),
      fallbackLang: Language.It,
      lang: Language.It,
    }),
    // Pages without a language in the URL (private area) use the saved preference.
    provideAppInitializer(() => {
      const languageBehaviour = inject(LanguageBehaviour);
      const preferred = languageBehaviour.preferred();
      languageBehaviour.use(preferred);
      return languageBehaviour.loadAll(preferred);
    }),
    // Calendars and date fields follow the language (date locale and accessible calendar commands).
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
