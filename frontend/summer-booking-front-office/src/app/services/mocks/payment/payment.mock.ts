import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { isAuthorized, unauthorized } from '../auth/auth.mock';

/**
 * `GET /api/payment/get-stripe-information`: the id of the Stripe account connected to the property,
 * the body is the string alone. 401 without a valid access token.
 */
export const stripeInformationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  return of(
    new HttpResponse({ status: 200, url: request.url, body: 'acct_1QmDemo4Fz8Lw2Xc' }),
  ).pipe(delay(150));
};
