import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { delay, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BOOKING_MOCKS } from './bookings/booking.mock';

export const mockApiInterceptor: HttpInterceptorFn = (request, next) => {
  if (!environment.features.useMocks) {
    return next(request);
  }

  const bookingsEndpoint = `${environment.apiBaseUrl}/bookings`;
  if (request.method === 'GET' && request.url === bookingsEndpoint) {
    return of(new HttpResponse({ status: 200, body: BOOKING_MOCKS })).pipe(delay(150));
  }

  return next(request);
};
