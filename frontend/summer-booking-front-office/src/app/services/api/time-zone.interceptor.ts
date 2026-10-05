import { HttpInterceptorFn } from '@angular/common/http';
import { isApiUrl } from './api-url';

/** Carries an IANA time zone, e.g. `Europe/Rome`. */
export const TIME_ZONE_HEADER = 'X-Time-Zone';

/**
 * Adds the user's time zone to every request to our API (never to other hosts): dates travel in
 * UTC and the server uses the zone to find the user's days (daylight saving time included).
 */
export const timeZoneInterceptor: HttpInterceptorFn = (request, next) => {
  if (!isApiUrl(request.url)) {
    return next(request);
  }
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  return next(request.clone({ setHeaders: { [TIME_ZONE_HEADER]: timeZone } }));
};
