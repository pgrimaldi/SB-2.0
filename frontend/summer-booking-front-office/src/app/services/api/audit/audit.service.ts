import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/audit`;

  readonly entriesList = (request: PageRequest): Observable<Page<AuditEntryItem>> =>
    this.httpClient.post<Page<AuditEntryItem>>(`${this.endpoint}/entries-list`, request);
}
