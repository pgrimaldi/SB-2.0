import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { PriceListService } from './price-list.service';

describe('PriceListService', () => {
  it('should ask a page of packages with the request of the table', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const request: PageRequest = {
      idProperty: 'p1',
      page: 2,
      pageSize: 10,
      search: 'ombrellone',
      filters: [],
      sortField: 'packageName',
      sortDirection: 'Descending',
    };
    const page: Page<PackageItem> = {
      total: 11,
      rows: [{ idPackage: 'k11', packageName: 'Ombrellone e lettino' }],
    };
    let answer: Page<PackageItem> | undefined;

    priceLists.packageList(request).subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/packages-list`,
    );
    expect(call.request.body).toEqual(request);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });
});
