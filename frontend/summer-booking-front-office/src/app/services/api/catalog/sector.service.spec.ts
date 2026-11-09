import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { SectorItem } from '../../../entities/settings/rows-and-sectors/sector-item';
import { SectorService } from './sector.service';

describe('SectorService', () => {
  it('should ask a page of sectors with the request of the table', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    const request: PageRequest = {
      idProperty: 'p1',
      page: 2,
      pageSize: 10,
      search: 'fila',
      filters: [],
      sortField: 'sectorName',
      sortDirection: 'Descending',
    };
    const page: Page<SectorItem> = {
      total: 11,
      rows: [{ idSector: 's11', sectorName: 'Fila 11' }],
    };
    let answer: Page<SectorItem> | undefined;

    sectors.sectorList(request).subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sectors-list`,
    );
    expect(call.request.body).toEqual(request);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });
});
