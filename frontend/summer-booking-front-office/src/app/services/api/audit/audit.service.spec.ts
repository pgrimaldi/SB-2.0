import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FilterField } from '../../../entities/enums/filter-field';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AuditEntryItem } from '../../../entities/settings/user-management/audit-entry-item';
import { AuditLogFiltersRequest } from '../../../entities/settings/user-management/audit-log-filters-request';
import { AuditService } from './audit.service';

describe('AuditService', () => {
  it('should send the same filters of the table to export-log and print-log, asking for a file', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const audit = TestBed.inject(AuditService);
    const controller = TestBed.inject(HttpTestingController);
    const filters: AuditLogFiltersRequest = {
      search: 'ferri',
      filters: [{ field: FilterField.DatetimeFrom, value: ['2026-10-20T00:00:00.000Z'] }],
      sortField: 'occurredAt',
      sortDirection: 'Descending',
    };

    audit.exportLog(filters).subscribe();
    audit.printLog(filters).subscribe();

    for (const route of ['export-log', 'print-log']) {
      const request = controller.expectOne(`${environment.apiBaseUrl}/audit/${route}`).request;
      expect(request.method).toBe('POST');
      expect(request.responseType).toBe('blob');
      expect(request.body).toEqual(filters);
    }
    controller.verify();
  });

  it('should ask one page of the log with the request of the table and read the page', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const audit = TestBed.inject(AuditService);
    const controller = TestBed.inject(HttpTestingController);
    const pageRequest: PageRequest = {
      page: 1,
      pageSize: 10,
      search: '',
      filters: [{ field: FilterField.IdUser, value: ['3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d01'] }],
      sortField: 'occurredAt',
      sortDirection: 'Descending',
    };
    const page: Page<AuditEntryItem> = { total: 0, rows: [] };

    let answer: Page<AuditEntryItem> | undefined;

    audit.entriesList(pageRequest).subscribe((value) => (answer = value));

    const call = controller.expectOne(`${environment.apiBaseUrl}/audit/entries-list`);
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(pageRequest);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });
});
