import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';
import { AuditLogFiltersRequest } from '../../../entities/settings/user-management/audit-log-filters-request';
import { readBlobError } from '../errors/read-blob-error';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/audit`;

  readonly entriesList = (request: PageRequest): Observable<Page<AuditEntryItem>> =>
    this.httpClient.post<Page<AuditEntryItem>>(`${this.endpoint}/entries-list`, request);

  /** The users that can be chosen in the filters of the log: id and name. */
  readonly userCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/get-user-combobox`, { idProperty });

  /** The log with the filters of the table, as a file: its name is in `Content-Disposition`. */
  readonly exportLog = (request: AuditLogFiltersRequest): Observable<HttpResponse<Blob>> =>
    this.httpClient
      .post(`${this.endpoint}/export-log`, request, { observe: 'response', responseType: 'blob' })
      .pipe(catchError(readBlobError));

  /** The log with the filters of the table, as a document to print, sent as a file like the export. */
  readonly printLog = (request: AuditLogFiltersRequest): Observable<HttpResponse<Blob>> =>
    this.httpClient
      .post(`${this.endpoint}/print-log`, request, { observe: 'response', responseType: 'blob' })
      .pipe(catchError(readBlobError));
}
