import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { WarehouseService } from './warehouse.service';

describe('WarehouseService', () => {
  it('should send only the fields of the row, without the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    // A row from the server with more than the contract: another property and an unknown field.
    const row = Object.assign(new WarehouseItem(), {
      idArticle: 'a1',
      totalQuantity: 10,
      unexpectedFlag: 'extra-field',
    });

    warehouse.addWarehouseItem(row).subscribe();
    warehouse.editWarehouseItem(row).subscribe();

    for (const route of ['add-warehouse-item', 'edit-warehouse-item']) {
      const body = controller.expectOne(`${environment.apiBaseUrl}/warehouse/${route}`).request
        .body;
      expect(body).toEqual({
        ...new WarehouseItem(),
        idArticle: 'a1',
        totalQuantity: 10,
      });
    }
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
      `${environment.apiBaseUrl}/warehouse/article-combobox`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toBeNull();
    controller.verify();
  });
});
