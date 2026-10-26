import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
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
      idProperty: 'p1',
      search: 'ferri',
      filters: [{ field: 'datetimeFrom', value: ['2026-10-20T00:00:00.000Z'] }],
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

  it('should ask the users of the log filter for the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const audit = TestBed.inject(AuditService);
    const controller = TestBed.inject(HttpTestingController);

    audit.userCombobox('p1').subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/audit/get-user-combobox`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idProperty: 'p1' });
    controller.verify();
  });
});
