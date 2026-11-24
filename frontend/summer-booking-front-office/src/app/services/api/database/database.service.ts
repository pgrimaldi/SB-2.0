import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { MoveToTrashRequest } from '../../../entities/database/move-to-trash-request';
import { ExistingBookingItem } from '../../../entities/database/existing-booking-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { BulkResult } from '../../../entities/shared/bulk-result';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class DatabaseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/booking`;

  readonly existingBookingList = (request: PageRequest): Observable<Page<ExistingBookingItem>> =>
    this.httpClient.post<Page<ExistingBookingItem>>(
      `${this.endpoint}/reservations/reservation-list`,
      request,
    );

  readonly trashItemList = (request: PageRequest): Observable<Page<ExistingBookingItem>> =>
    this.httpClient.post<Page<ExistingBookingItem>>(`${this.endpoint}/trash/trash-list`, request);

  readonly affiliatedHotelCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(
      `${environment.apiBaseUrl}/catalog/agreement-partners/agreement-partner-combobox`,
      null,
    );

  readonly moveToTrash = (request: MoveToTrashRequest): Observable<BulkResult> =>
    this.httpClient.post<BulkResult>(
      `${this.endpoint}/cleanup/move-to-trash`,
      onlyFieldsOf(MoveToTrashRequest, request),
    );

  readonly emptyTrash = (): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/trash/empty`, null);

  readonly restoreBookingItem = (ids: string[]): Observable<BulkResult> =>
    this.httpClient.post<BulkResult>(`${this.endpoint}/trash/restore`, { ids });

  readonly deleteSelectedItem = (ids: string[]): Observable<BulkResult> =>
    this.httpClient.post<BulkResult>(`${this.endpoint}/trash/purge`, { ids });
}
