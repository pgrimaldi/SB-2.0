import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/payment`;

  /** The id of the Stripe account connected to the property; null while none is connected. */
  readonly stripeInformation = (): Observable<string | null> =>
    this.httpClient.get<string | null>(`${this.endpoint}/get-stripe-information`);
}
