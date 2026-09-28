import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { WarehouseService } from '../../api/warehouse/warehouse.service';
import { mockApiInterceptor } from '../mock-api.interceptor';

describe('warehouseMock', () => {
  it("should answer the articles with total minus today's held and confirmed consumptions", async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([mockApiInterceptor]))],
    });

    const items = await firstValueFrom(TestBed.inject(WarehouseService).getAll());
    const byName = Object.fromEntries(items.map((item) => [item.name, item]));

    expect(items.length).toBe(6);
    expect(byName['Lettino']).toEqual(expect.objectContaining({ total: 90, available: 66 }));
    expect(byName['Cabina']).toEqual(expect.objectContaining({ total: 99, available: 99 }));
    expect(byName['Ombrellone'].idArticle).toBe('01a0cc35-c471-74d6-bd87-130a7999659e');
    expect(byName['Ombrellone'].thresholdNumber).toBe(2);
  });
});
