import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { MoveToTrashRequest } from '../../../entities/database/move-to-trash-request';
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

  readonly trashItemList = (request: PageRequest): Observable<Page<ExistingBookingItem>> =>
    this.httpClient.post<Page<ExistingBookingItem>>(`${this.endpoint}/trash-item-list`, request);

  readonly affiliatedHotelCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/get-affiliated-hotel-combobox`, {
      idProperty,
    });

  readonly moveToTrash = (idProperty: string, request: MoveToTrashRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/move-to-trash`, {
      ...onlyFieldsOf(MoveToTrashRequest, request),
      idProperty,
    });

  readonly emptyTrash = (idProperty: string): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/empty-trash`, { idProperty });

  readonly restoreBookingItem = (
    idProperty: string,
    idBookings: readonly string[],
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/restore-booking-item`, {
      idProperty,
      idBookings,
    });

  readonly deleteSelectedItem = (
    idProperty: string,
    idBookings: readonly string[],
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-selected-item`, {
      idProperty,
      idBookings,
    });
}
