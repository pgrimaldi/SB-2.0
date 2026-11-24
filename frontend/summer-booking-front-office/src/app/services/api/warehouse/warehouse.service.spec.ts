import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { WarehouseItemAddRequest } from '../../../entities/warehouse/warehouse-item-add-request';
import { WarehouseSelectedItem } from '../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../entities/warehouse/warehouse-setting-item';
import { WarehouseService } from './warehouse.service';

describe('WarehouseService', () => {
  it('should ask a page of articles with the request of the table', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    const request: PageRequest = {
      page: 2,
      pageSize: 10,
      search: 'lett',
      filters: [],
      sortField: 'lowStockThreshold',
      sortDirection: 'Descending',
    };
    const page: Page<WarehouseSettingItem> = {
      total: 11,
      rows: [
        {
          idArticle: 'a11',
          name: 'Lettino',
          totalQuantity: 120,
          lowStockThreshold: null,
        },
      ],
    };
    let answer: Page<WarehouseSettingItem> | undefined;

    warehouse.inventoryItemsList(request).subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/inventory-items-list`,
    );
    expect(call.request.body).toEqual(request);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });

  it('should add pieces to an article with only the fields of the request', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new WarehouseItemAddRequest(), {
      idArticle: 'a1',
      totalQuantity: 10,
      unexpectedFlag: 'extra-field',
    });

    warehouse.addWarehouseItem(request).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/add-warehouse-item`,
    );
    expect(call.request.body).toEqual({
      idArticle: 'a1',
      totalQuantity: 10,
      lowStockThreshold: null,
      lowStockAlertEnabled: null,
    });
    controller.verify();
  });

  it('should read an article and put it back whole, with only the fields of the request', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    const url = `${environment.apiBaseUrl}/catalog/inventory-items/a%2F1`;
    const putUrl = `${environment.apiBaseUrl}/catalog/edit-warehouse-item/a%2F1`;
    // The article read, with more than the request: its id and mapPlaceable stay out.
    const article = Object.assign(new WarehouseSelectedItem(), {
      id: 'a/1',
      articleCode: 'sunbed',
      name: 'Lettino',
      totalQuantity: 120,
      lowStockThreshold: 5,
      lowStockAlertEnabled: true,
      mapPlaceable: true,
      version: 'v1',
    });
    let version: string | null | undefined;

    warehouse.selectedArticle('a/1').subscribe();
    warehouse
      .updateArticle('a/1', { ...article, totalQuantity: 100 }, article.version)
      .subscribe((saved) => (version = saved));

    expect(
      controller.expectOne(
        ({ method, urlWithParams }) => method === 'GET' && urlWithParams === url,
      ),
    ).toBeTruthy();
    const put = controller.expectOne(
      ({ method, urlWithParams }) => method === 'PUT' && urlWithParams === putUrl,
    );
    expect(put.request.body).toEqual({
      articleCode: 'sunbed',
      name: 'Lettino',
      totalQuantity: 100,
      lowStockThreshold: 5,
      lowStockAlertEnabled: true,
    });
    expect(put.request.headers.get('If-Match')).toBe('"v1"');
    put.flush(null, { headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });

  it('should delete one article or more with the same API', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);

    warehouse.deleteArticles(['a1']).subscribe();
    warehouse.deleteArticles(['a1', 'a2']).subscribe();

    const calls = controller.match(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/delete-warehouse-item`,
    );
    expect(calls.map((call) => call.request.body)).toEqual([
      { idItems: ['a1'] },
      { idItems: ['a1', 'a2'] },
    ]);
    controller.verify();
  });

  it('should give the price lists of an article in use as one usedIn list', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    const errors: unknown[] = [];

    warehouse.deleteArticles(['a1']).subscribe({ error: (error: unknown) => errors.push(error) });
    warehouse.deleteArticles(['a2']).subscribe({ error: (error: unknown) => errors.push(error) });
    const [inUse, notFound] = controller.match(
      `${environment.apiBaseUrl}/catalog/delete-warehouse-item`,
    );
    inUse.flush(
      {
        code: ApiErrorCode.InventoryItemInUse,
        args: {
          articleName: 'Lettino',
          priceLists: ['Estate'],
          agreementPriceLists: ['Hotel Mare'],
        },
      },
      { status: 409, statusText: 'Conflict' },
    );
    notFound.flush(
      { code: ApiErrorCode.InventoryItemNotFound },
      { status: 404, statusText: 'Not Found' },
    );

    const [inUseError, notFoundError] = errors as HttpErrorResponse[];
    expect(inUseError.status).toBe(409);
    expect(inUseError.error).toEqual({
      code: ApiErrorCode.InventoryItemInUse,
      args: {
        articleName: 'Lettino',
        priceLists: ['Estate'],
        agreementPriceLists: ['Hotel Mare'],
        usedIn: ['Estate', 'Hotel Mare'],
      },
    });
    expect(notFoundError.error).toEqual({ code: ApiErrorCode.InventoryItemNotFound });
    controller.verify();
  });

  it('should duplicate one article or more with the same API', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);

    warehouse.duplicateWarehouseItems(['a1']).subscribe();
    warehouse.duplicateWarehouseItems(['a1', 'a2']).subscribe();

    const calls = controller.match(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/duplicate-warehouse-item`,
    );
    expect(calls.map((call) => call.request.body)).toEqual([
      { idItems: ['a1'] },
      { idItems: ['a1', 'a2'] },
    ]);
    controller.verify();
  });

  it('should ask the articles of the combobox without a body', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);

    warehouse.articleCombobox().subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/article-combobox`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toBeNull();
    controller.verify();
  });
});
