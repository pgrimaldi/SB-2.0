import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LanguageBehaviour } from '../../behaviours/i18n/language.behaviour';
import { isApiUrl } from './api-url';

/**
 * Sends the language chosen in the app (e.g. `en`), not the browser's, to every request to our API
 * (never to other hosts), as the backend's response standard expects: the backend uses it only when it
 * must write text itself. Error messages do not need it: the frontend translates their `code`.
 */
export const languageInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url)) {
    return next(request);
  }
  const language = inject(LanguageBehaviour).current();
  return next(request.clone({ setHeaders: { 'Accept-Language': language } }));
};
