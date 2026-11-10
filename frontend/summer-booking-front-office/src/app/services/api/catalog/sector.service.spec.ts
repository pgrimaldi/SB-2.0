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

  it('should ask the sectors of the dropdowns with the property only', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    let answer: unknown;

    sectors.sectorCombobox('p1').subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sector-combobox`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1' });
    call.flush([{ id: 's1', value: 'Fila 1' }]);
    expect(answer).toEqual([{ id: 's1', value: 'Fila 1' }]);
    controller.verify();
  });

  it('should add a sector sending only the property and the name', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors
      .addSectorItem('p1', {
        idSector: null,
        sectorName: 'Fila 9',
        extra: 'not sent',
      } as SectorItem)
      .subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/add-sector-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', sectorName: 'Fila 9' });
    controller.verify();
  });

  it('should rename a sector sending its id and the new name', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors
      .updateSectorItem('p1', {
        idSector: 's1',
        sectorName: 'Fila A',
        extra: 'not sent',
      } as SectorItem)
      .subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/update-sector-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idSector: 's1', sectorName: 'Fila A' });
    controller.verify();
  });

  it('should delete the chosen sectors in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors.deleteSectorItems({ idProperty: 'p1', idSectors: ['s1', 's2'] }).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/delete-sector-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idSectors: ['s1', 's2'] });
    controller.verify();
  });

  it('should duplicate the chosen sectors in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors.duplicateSectorItems({ idProperty: 'p1', idSectors: ['s1', 's2'] }).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/duplicate-sector-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idSectors: ['s1', 's2'] });
    controller.verify();
  });
});
