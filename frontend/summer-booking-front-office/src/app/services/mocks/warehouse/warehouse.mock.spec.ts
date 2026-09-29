import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../../entities/management/management-request';
import { WarehouseService } from '../../api/warehouse/warehouse.service';
import { mockApiInterceptor } from '../mock-api.interceptor';

/** "La Capannina", 29/09/2026, full day. */
const MANAGEMENT: ManagementRequest = {
  idProperty: '01a0cc35-02f7-7ef1-904e-fe147403481b',
  datetimeFrom: '2026-09-29T00:00:00.000Z',
  datetimeTo: '2026-09-29T23:59:59.999Z',
  bookingDayType: BookingDayType.FullDay,
};

describe('warehouseMock', () => {
  const service = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([mockApiInterceptor]))],
    });
    return TestBed.inject(WarehouseService);
  };

  it("should answer the articles with total minus today's held and confirmed consumptions", async () => {
    const { total, rows } = await firstValueFrom(
      service().list({ ...MANAGEMENT, page: 1, pageSize: 10 }),
    );
    const byName = Object.fromEntries(rows.map((item) => [item.name, item]));

    expect(total).toBe(12);
    expect(rows.length).toBe(10);
    expect(byName['Lettino']).toEqual(expect.objectContaining({ total: 90, available: 66 }));
    expect(byName['Cabina']).toEqual(expect.objectContaining({ total: 99, available: 99 }));
    expect(byName['Ombrellone'].idArticle).toBe('01a0cc35-c471-74d6-bd87-130a7999659e');
    expect(byName['Ombrellone'].thresholdNumber).toBe(2);
  });

  it('should answer only the requested page, with the total of all pages', async () => {
    const page = await firstValueFrom(service().list({ ...MANAGEMENT, page: 2, pageSize: 4 }));

    expect(page.total).toBe(12);
    expect(page.rows.map((item) => item.name)).toEqual([
      'Doccia',
      'Cabina',
      'Parcheggio',
      'Lettino king size',
    ]);
  });

  it('should search in the name, ignoring case', async () => {
    const page = await firstValueFrom(
      service().list({ ...MANAGEMENT, page: 1, pageSize: 10, search: 'LETT' }),
    );

    expect(page.total).toBe(2);
    expect(page.rows.map((item) => item.name)).toEqual(['Lettino', 'Lettino king size']);
  });

  it('should answer no articles for another property', async () => {
    const page = await firstValueFrom(
      service().list({ ...MANAGEMENT, idProperty: 'another', page: 1, pageSize: 10 }),
    );

    expect(page).toEqual({ total: 0, rows: [] });
  });

  it('should refuse a period whose end comes before its start', async () => {
    const error = await firstValueFrom(
      service().list({
        ...MANAGEMENT,
        datetimeTo: '2026-09-27T00:00:00.000Z',
        page: 1,
        pageSize: 10,
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((error as HttpErrorResponse).status).toBe(400);
  });
});
