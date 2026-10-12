import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { Page, PageRequest } from '../../../entities/pagination/page';

@Injectable({ providedIn: 'root' })
export class DatabaseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/database`;

  readonly existingBookingList = (request: PageRequest): Observable<Page<ExistingBookingItem>> =>
    this.httpClient.post<Page<ExistingBookingItem>>(
      `${this.endpoint}/existing-booking-list`,
      request,
    );
}
