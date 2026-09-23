import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Booking } from '../../../entities/booking/booking';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly endpoint = `${environment.apiBaseUrl}/bookings`;

  constructor(private readonly httpClient: HttpClient) {}

  getAll(): Observable<Booking[]> {
    return this.httpClient.get<Booking[]>(this.endpoint);
  }
}
