import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { ConfirmDeleteBookingRequest } from '../../../entities/database/confirm-delete-booking-request';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class DatabaseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/database`;

  readonly existingBookingList = (request: PageRequest): Observable<Page<ExistingBookingItem>> =>
    this.httpClient.post<Page<ExistingBookingItem>>(
      `${this.endpoint}/existing-booking-list`,
      request,
    );

  readonly affiliatedHotelCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/get-affiliated-hotel-combobox`, {
      idProperty,
    });

  readonly confirmDeleteBooking = (
    idProperty: string,
    request: ConfirmDeleteBookingRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/confirm-delete-booking`, {
      ...onlyFieldsOf(ConfirmDeleteBookingRequest, request),
      idProperty,
    });
}
