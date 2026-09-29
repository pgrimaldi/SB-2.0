import { HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { ManagementRequest } from '../../../entities/management/management-request';
import { AuthService } from '../../api/auth/auth.service';
import { authInterceptor } from '../../api/auth.interceptor';
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
  const TEST_PASSWORD_SHA256 = '42862e8e5e2e0915ad980297cc224059dcc424323ea325a9754839c79bca93f5';

  /** Warehouse API of a signed-in user (the endpoint wants a valid access token). */
  const service = async (signedIn = true) => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor, mockApiInterceptor]))],
    });
    if (signedIn) {
      const bytes = TEST_PASSWORD_SHA256.match(/../g)!.map((hex) => parseInt(hex, 16));
      vi.spyOn(crypto.subtle, 'digest').mockResolvedValue(new Uint8Array(bytes).buffer);
      const session = await firstValueFrom(
        TestBed.inject(AuthService).signIn({
          username: 'summertest465@gmail.com',
          password: 'typed',
          remember: false,
        }),
      );
      TestBed.inject(AuthBehaviour).start(session, false);
    }
    return TestBed.inject(WarehouseService);
  };

  afterEach(() => {
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  it('should refuse the list without a valid access token (401)', async () => {
    const error = await firstValueFrom(
      (await service(false)).list({ ...MANAGEMENT, page: 1, pageSize: 10 }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((error as HttpErrorResponse).status).toBe(401);
  });

  it("should answer the articles with total minus today's held and confirmed consumptions", async () => {
    const { total, rows } = await firstValueFrom(
      (await service()).list({ ...MANAGEMENT, page: 1, pageSize: 10 }),
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
    const page = await firstValueFrom(
      (await service()).list({ ...MANAGEMENT, page: 2, pageSize: 4 }),
    );

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
      (await service()).list({ ...MANAGEMENT, page: 1, pageSize: 10, search: 'LETT' }),
    );

    expect(page.total).toBe(2);
    expect(page.rows.map((item) => item.name)).toEqual(['Lettino', 'Lettino king size']);
  });

  it('should answer no articles for another property', async () => {
    const page = await firstValueFrom(
      (await service()).list({ ...MANAGEMENT, idProperty: 'another', page: 1, pageSize: 10 }),
    );

    expect(page).toEqual({ total: 0, rows: [] });
  });

  it('should refuse a period whose end comes before its start', async () => {
    const error = await firstValueFrom(
      (await service()).list({
        ...MANAGEMENT,
        datetimeTo: '2026-09-27T00:00:00.000Z',
        page: 1,
        pageSize: 10,
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((error as HttpErrorResponse).status).toBe(400);
  });
});
