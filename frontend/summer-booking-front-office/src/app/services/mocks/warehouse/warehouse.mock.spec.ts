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
import { DEMO_PROPERTY, MOCK_PROPERTIES } from '../properties/properties.mock';

/** The invented "Lido Demo" property, 29/09/2026, full day. */
const MANAGEMENT: ManagementRequest = {
  idProperty: DEMO_PROPERTY.publicId,
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
      (await service(false)).list({
        ...MANAGEMENT,
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((error as HttpErrorResponse).status).toBe(401);
    expect((error as HttpErrorResponse).error).toEqual(
      expect.objectContaining({ code: 'auth.invalid_token' }),
    );
  });

  it("should answer the articles with total minus today's held and confirmed consumptions", async () => {
    const { total, rows } = await firstValueFrom(
      (await service()).list({
        ...MANAGEMENT,
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    );
    const byName = Object.fromEntries(rows.map((item) => [item.name, item]));

    expect(total).toBe(12); // the inactive article is left out
    expect(rows.length).toBe(10);
    expect(byName['Lettino']).toEqual(
      expect.objectContaining({ totalQuantity: 120, availableQuantity: 84 }),
    );
    expect(byName['Cabina']).toEqual(
      expect.objectContaining({ totalQuantity: 24, availableQuantity: 24 }),
    ); // released
    expect(byName['Ombrellone'].idArticle).toBe('276a67d2-d0eb-47f5-8223-4804b9a9dcf9');
    expect(byName['Ombrellone'].thresholdQuantity).toBe(3);
  });

  it('should answer only the requested page, with the total of all pages', async () => {
    const page = await firstValueFrom(
      (await service()).list({
        ...MANAGEMENT,
        page: 2,
        pageSize: 4,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    );

    expect(page.total).toBe(12);
    expect(page.rows.map((item) => item.name)).toEqual([
      'Doccia',
      'Spogliatoio',
      'Parcheggio',
      'Lettino XL',
    ]);
  });

  it('should answer only the articles whose name contains the search, ignoring case', async () => {
    const page = await firstValueFrom(
      (await service()).list({
        ...MANAGEMENT,
        page: 1,
        pageSize: 10,
        search: ' LETTINO ',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    );

    expect(page.total).toBe(2);
    expect(page.rows.map((item) => item.name)).toEqual(['Lettino', 'Lettino XL']);
  });

  it('should sort by the requested field, numbers by value and texts alphabetically', async () => {
    const warehouse = await service();
    const request = { ...MANAGEMENT, page: 1, pageSize: 3, search: '' };
    const byName = await firstValueFrom(
      warehouse.list({ ...request, sortField: 'name', sortDirection: 'Ascending' }),
    );
    const byTotal = await firstValueFrom(
      warehouse.list({ ...request, sortField: 'totalQuantity', sortDirection: 'Descending' }),
    );

    expect(byName.rows.map((item) => item.name)).toEqual(['Cabina', 'Doccia', 'Lettino']);
    expect(byTotal.rows.map((item) => item.totalQuantity)).toEqual([120, 80, 60]);
    expect(byTotal.total).toBe(12);
  });

  it('should sort by threshold, with the thresholds not set first ascending and last descending', async () => {
    const warehouse = await service();
    const request = { ...MANAGEMENT, search: '', sortField: 'thresholdQuantity' };
    const ascending = await firstValueFrom(
      warehouse.list({ ...request, page: 1, pageSize: 3, sortDirection: 'Ascending' }),
    );
    const descending = await firstValueFrom(
      warehouse.list({ ...request, page: 1, pageSize: 12, sortDirection: 'Descending' }),
    );

    expect(ascending.rows.map((item) => [item.name, item.thresholdQuantity])).toEqual([
      ['Doccia', null],
      ['Parcheggio', null],
      ['Tavolino', null],
    ]);
    expect(descending.rows.slice(0, 2).map((item) => item.thresholdQuantity)).toEqual([5, 4]);
    expect(descending.rows.slice(-3).map((item) => item.thresholdQuantity)).toEqual([
      null,
      null,
      null,
    ]);
  });

  it('should answer only the articles of the requested property, none for an unknown one', async () => {
    const warehouse = await service();
    const other = await firstValueFrom(
      warehouse.list({
        ...MANAGEMENT,
        idProperty: MOCK_PROPERTIES[1].publicId,
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    );
    const unknown = await firstValueFrom(
      warehouse.list({
        ...MANAGEMENT,
        idProperty: 'unknown',
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    );

    expect(other.rows.map((item) => item.name)).toEqual(['Pedalò', 'Canoa']);
    expect(unknown).toEqual({ total: 0, rows: [] });
  });

  it('should answer the articles of the property for a select: id and name, by name', async () => {
    const warehouse = await service();
    const demo = await firstValueFrom(
      warehouse.comboboxList({ idProperty: MANAGEMENT.idProperty }),
    );
    const other = await firstValueFrom(
      warehouse.comboboxList({ idProperty: MOCK_PROPERTIES[1].publicId }),
    );
    const unknown = await firstValueFrom(warehouse.comboboxList({ idProperty: 'unknown' }));

    expect(demo.length).toBe(12); // the inactive article is left out
    expect(demo[0]).toEqual({ id: expect.any(String), value: 'Cabina' });
    expect(Object.keys(demo[0])).toEqual(['id', 'value']);
    expect(demo.map((item) => item.value)).toEqual(
      [...demo.map((item) => item.value)].sort((a, b) => a.localeCompare(b, 'it')),
    );
    expect(other.map((item) => item.value)).toEqual(['Canoa', 'Pedalò']);
    expect(unknown).toEqual([]);
  });

  it('should refuse the select list without a valid access token (401) or without the property (400)', async () => {
    const unauthorized = await firstValueFrom(
      (await service(false)).comboboxList({ idProperty: MANAGEMENT.idProperty }),
    ).catch((failure: HttpErrorResponse) => failure);
    TestBed.resetTestingModule();
    const invalid = await firstValueFrom((await service()).comboboxList({ idProperty: '' })).catch(
      (failure: HttpErrorResponse) => failure,
    );

    expect((unauthorized as HttpErrorResponse).status).toBe(401);
    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        code: 'validation.invalid_request',
        errors: [{ field: 'idProperty', code: 'validation.invalid_value' }],
      }),
    );
  });

  it('should add the pieces to the article and set its threshold (204)', async () => {
    const warehouse = await service();
    const other = { ...MANAGEMENT, idProperty: MOCK_PROPERTIES[1].publicId };
    const canoa = async () =>
      (
        await firstValueFrom(
          warehouse.list({
            ...other,
            page: 1,
            pageSize: 10,
            search: 'canoa',
            sortField: '',
            sortDirection: 'Ascending',
          }),
        )
      ).rows[0];
    const before = await canoa();

    const answer = await firstValueFrom(
      warehouse.addWarehouseItem({
        idProperty: other.idProperty,
        idArticle: before.idArticle,
        articleQuantity: 3,
        thresholdQuantity: 2,
        isThresholdWarningActive: true,
      }),
    );

    expect(answer).toBeNull();
    expect(await canoa()).toEqual(
      expect.objectContaining({ totalQuantity: before.totalQuantity + 3, thresholdQuantity: 2 }),
    );
  });

  it('should refuse an article to add without a valid access token (401) or with wrong fields (400)', async () => {
    const request = {
      idProperty: MANAGEMENT.idProperty,
      idArticle: 'unknown',
      articleQuantity: 0,
      thresholdQuantity: null,
      isThresholdWarningActive: true,
    };
    const unauthorized = await firstValueFrom(
      (await service(false)).addWarehouseItem(request),
    ).catch((failure: HttpErrorResponse) => failure);
    TestBed.resetTestingModule();
    const invalid = await firstValueFrom((await service()).addWarehouseItem(request)).catch(
      (failure: HttpErrorResponse) => failure,
    );

    expect((unauthorized as HttpErrorResponse).status).toBe(401);
    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        code: 'validation.invalid_request',
        errors: [
          { field: 'idArticle', code: 'validation.invalid_value' },
          { field: 'articleQuantity', code: 'validation.invalid_value' },
          { field: 'isThresholdWarningActive', code: 'validation.invalid_value' },
        ],
      }),
    );
  });

  it('should refuse a period whose end comes before its start', async () => {
    const error = await firstValueFrom(
      (await service()).list({
        ...MANAGEMENT,
        datetimeTo: '2026-09-27T00:00:00.000Z',
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((error as HttpErrorResponse).status).toBe(400);
    expect((error as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        code: 'validation.invalid_request',
        errors: [{ field: 'datetimeTo', code: 'validation.end_before_start' }],
      }),
    );
  });

  it('should set total, threshold and alert of the article to change (204)', async () => {
    const warehouse = await service();
    const other = { ...MANAGEMENT, idProperty: MOCK_PROPERTIES[1].publicId };
    const pedalo = async () =>
      (
        await firstValueFrom(
          warehouse.list({
            ...other,
            page: 1,
            pageSize: 10,
            search: 'pedal',
            sortField: '',
            sortDirection: 'Ascending',
          }),
        )
      ).rows[0];
    const before = await pedalo();
    expect(before.isThresholdWarningActive).toBe(true);

    const answer = await firstValueFrom(
      warehouse.editWarehouseItem({
        idProperty: other.idProperty,
        idItem: before.idArticle,
        articleQuantity: 9,
        thresholdQuantity: null,
        isThresholdWarningActive: false,
      }),
    );

    expect(answer).toBeNull();
    expect(await pedalo()).toEqual(
      expect.objectContaining({
        totalQuantity: 9,
        thresholdQuantity: null,
        isThresholdWarningActive: false,
      }),
    );
  });

  it('should refuse an article to change of another property or with wrong fields (400)', async () => {
    const warehouse = await service();
    const canoa = (
      await firstValueFrom(
        warehouse.list({
          ...MANAGEMENT,
          idProperty: MOCK_PROPERTIES[1].publicId,
          page: 1,
          pageSize: 10,
          search: 'canoa',
          sortField: '',
          sortDirection: 'Ascending',
        }),
      )
    ).rows[0];
    const invalid = await firstValueFrom(
      warehouse.editWarehouseItem({
        idProperty: MANAGEMENT.idProperty,
        idItem: canoa.idArticle,
        articleQuantity: 0,
        thresholdQuantity: 0,
        isThresholdWarningActive: true,
      }),
    ).catch((failure: HttpErrorResponse) => failure);

    expect((invalid as HttpErrorResponse).status).toBe(400);
    expect((invalid as HttpErrorResponse).error).toEqual(
      expect.objectContaining({
        errors: [
          { field: 'idItem', code: 'validation.invalid_value' },
          { field: 'articleQuantity', code: 'validation.invalid_value' },
          { field: 'isThresholdWarningActive', code: 'validation.invalid_value' },
        ],
      }),
    );
  });
});
