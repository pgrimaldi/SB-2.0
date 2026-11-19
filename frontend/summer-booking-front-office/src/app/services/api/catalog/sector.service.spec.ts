import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { Sector } from '../../../entities/settings/rows-and-sectors/sector';
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

  it('should ask the sectors of the dropdowns without a body', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    let answer: unknown;

    sectors.sectorCombobox().subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sector-combobox`,
    );
    expect(call.request.body).toBeNull();
    call.flush([{ id: 's1', value: 'Fila 1' }]);
    expect(answer).toEqual([{ id: 's1', value: 'Fila 1' }]);
    controller.verify();
  });

  it('should add a sector sending the name', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    const sector: Sector = { id: 's9', name: 'Fila 9', sortOrder: 8 };
    let answer: Sector | undefined;

    sectors.addSector('Fila 9').subscribe((created) => (answer = created));

    const call = controller.expectOne(
      ({ method, url }) => method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sectors`,
    );
    expect(call.request.body).toEqual({ name: 'Fila 9' });
    call.flush(sector, { status: 201, statusText: 'Created' });
    expect(answer).toEqual(sector);
    controller.verify();
  });

  it('should rename a sector with its id in the address and the new name', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    const sector: Sector = { id: 's1', name: 'Fila A', sortOrder: 0 };
    let answer: Sector | undefined;

    sectors.updateSector('s1', 'Fila A').subscribe((saved) => (answer = saved));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/catalog/sectors/s1`,
    );
    expect(call.request.body).toEqual({ name: 'Fila A' });
    call.flush(sector);
    expect(answer).toEqual(sector);
    controller.verify();
  });

  it('should delete one sector by its id', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors.deleteSector('s1').subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' && url === `${environment.apiBaseUrl}/catalog/sectors/s1`,
    );
    expect(call.request.body).toBeNull();
    controller.verify();
  });

  it('should encode the id of the sector in the address', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);
    const encoded = `${environment.apiBaseUrl}/catalog/sectors/..%2Fsettings%3Fx%3D1`;

    sectors.updateSector('../settings?x=1', 'Fila A').subscribe();
    sectors.deleteSector('../settings?x=1').subscribe();

    controller.expectOne(({ method, url }) => method === 'PUT' && url === encoded);
    controller.expectOne(({ method, url }) => method === 'DELETE' && url === encoded);
    controller.verify();
  });

  it('should delete the chosen sectors in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors.deleteSectors(['s1', 's2']).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sectors/bulk-delete`,
    );
    expect(call.request.body).toEqual({ idSectors: ['s1', 's2'] });
    controller.verify();
  });

  it('should duplicate the chosen sectors in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const sectors = TestBed.inject(SectorService);
    const controller = TestBed.inject(HttpTestingController);

    sectors.duplicateSectors(['s1', 's2']).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/sectors/bulk-duplicate`,
    );
    expect(call.request.body).toEqual({ idSectors: ['s1', 's2'] });
    controller.verify();
  });
});
