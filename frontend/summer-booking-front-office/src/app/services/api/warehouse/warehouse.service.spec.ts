import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';
import { WarehouseService } from './warehouse.service';

describe('WarehouseService', () => {
  it('should send only the fields of the row, with the property of the session', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const warehouse = TestBed.inject(WarehouseService);
    const controller = TestBed.inject(HttpTestingController);
    // A row from the server with more than the contract: another property and an unknown field.
    const row = Object.assign(new WarehouseItem(), {
      idArticle: 'a1',
      totalQuantity: 10,
      idProperty: 'p2',
      unexpectedFlag: 'extra-field',
    });

    warehouse.addWarehouseItem('p1', row).subscribe();
    warehouse.editWarehouseItem('p1', row).subscribe();

    for (const route of ['add-warehouse-item', 'edit-warehouse-item']) {
      const body = controller.expectOne(`${environment.apiBaseUrl}/warehouse/${route}`).request
        .body;
      expect(body).toEqual({
        ...new WarehouseItem(),
        idArticle: 'a1',
        totalQuantity: 10,
        idProperty: 'p1',
      });
    }
    controller.verify();
  });
});
