import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { Table, TableColumn, TablePage as Page, TablePageRequest as PageRequest } from './table';
import { TableIconAction } from './table-icon-action';
import { TableTextAction } from './table-text-action';

interface Row {
  name: string;
  total: number;
  hidden: string;
}

interface Filters {
  day: string;
}

/** 25 rows on the "server"; `load` answers the requested page. */
const ALL: Row[] = Array.from({ length: 25 }, (_, i) => ({
  name: `Row ${i + 1}`,
  total: i,
  hidden: 'x',
}));

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params()"
    [load]="load"
    [(pageSize)]="pageSize"
    [texts]="texts"
    (loadError)="errors.push($event)"
  />`,
})
class TableHost {
  readonly params = signal<Filters | null>({ day: '2026-09-30' });
  readonly pageSize = signal(10);
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome' },
    { field: 'total', header: 'Totale', align: 'center' },
  ];
  readonly texts = {
    paginator: {
      previous: 'Pagina precedente',
      next: 'Pagina successiva',
      range: '{{start}} – {{end}} di {{total}}',
      size: 'Elementi per pagina:',
    },
  };
  readonly errors: unknown[] = [];
  readonly requests: (Filters & PageRequest)[] = [];
  answer: (request: Filters & PageRequest) => Observable<Page<Row>> = (request) =>
    of({
      total: ALL.length,
      rows: ALL.slice((request.page - 1) * request.pageSize, request.page * request.pageSize),
    });
  readonly load = (request: Filters & PageRequest) => {
    this.requests.push(request);
    return this.answer(request);
  };
}

describe('Table', () => {
  const setup = async (prepare?: (host: TableHost) => void) => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableHost);
    prepare?.(fixture.componentInstance);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const names = () =>
      [...element.querySelectorAll('tbody tr td:first-child')].map((cell) =>
        cell.textContent?.trim(),
      );
    const next = () =>
      element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!;
    return { fixture, host: fixture.componentInstance, element, names, next };
  };

  it('should load the first page with the params and show only the chosen columns', async () => {
    const { host, element, names } = await setup();

    expect(host.requests).toEqual([{ day: '2026-09-30', page: 1, pageSize: 10, search: '' }]);
    expect(element.querySelectorAll('thead th').length).toBe(2);
    expect(names()).toEqual(ALL.slice(0, 10).map((row) => row.name));
    expect(element.querySelector<HTMLElement>('tbody td:last-child')?.style.textAlign).toBe(
      'center',
    );
  });

  it('should show the headers and the paginator texts it is given', async () => {
    const { element, next } = await setup();

    expect([...element.querySelectorAll('thead th')].map((th) => th.textContent?.trim())).toEqual([
      'Nome',
      'Totale',
    ]);
    expect(next().getAttribute('aria-label')).toBe('Pagina successiva');
    expect(element.querySelector('.mat-mdc-paginator-range-label')?.textContent?.trim()).toBe(
      '1 – 10 di 25',
    );
    expect(element.querySelector('.mat-mdc-paginator-page-size-label')?.textContent?.trim()).toBe(
      'Elementi per pagina:',
    );
  });

  it('should change the page size with its selector, keeping the first row on screen', async () => {
    const { fixture, host, element, names, next } = await setup();
    next().click();
    await fixture.whenStable();
    next().click();
    await fixture.whenStable(); // page 3: rows 21-25

    element
      .querySelector<HTMLElement>('.mat-mdc-paginator-page-size-select .mat-mdc-select-trigger')!
      .click();
    await fixture.whenStable();
    const options = [...document.querySelectorAll<HTMLElement>('mat-option')];
    expect(options.map((option) => option.textContent?.trim())).toEqual(['10', '20', '50', '100']);

    options[1].click(); // 20
    await fixture.whenStable();
    expect(host.pageSize()).toBe(20); // two-way
    expect(host.requests.at(-1)).toEqual({ day: '2026-09-30', page: 2, pageSize: 20, search: '' });
    expect(names()[0]).toBe('Row 21'); // rows 21-40: row 21 still on screen
  });

  it('should load the next page with its paginator', async () => {
    const { fixture, host, names, next } = await setup();

    next().click();
    await fixture.whenStable();

    expect(host.requests.at(-1)).toEqual({ day: '2026-09-30', page: 2, pageSize: 10, search: '' });
    expect(names()[0]).toBe('Row 11');
  });

  it('should go back to page 1 and load again when the params change', async () => {
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();

    host.params.set({ day: '2026-10-01' });
    await fixture.whenStable();

    expect(host.requests.at(-1)).toEqual({ day: '2026-10-01', page: 1, pageSize: 10, search: '' });
  });

  it('should load nothing while the params are null', async () => {
    const { host, names } = await setup((host) => host.params.set(null));

    expect(host.requests).toEqual([]);
    expect(names()).toEqual([]);
  });

  it('should keep the current rows on screen until the next page arrives', async () => {
    const answer = new Subject<Page<Row>>();
    const { fixture, host, names, next } = await setup();
    host.answer = () => answer;

    next().click();
    await fixture.whenStable();
    expect(names()[0]).toBe('Row 1'); // still the first page

    answer.next({ total: 25, rows: ALL.slice(10, 20) });
    await fixture.whenStable();
    expect(names()[0]).toBe('Row 11');
  });

  it('should show no rows and tell the page when a request fails, then load again', async () => {
    const { fixture, host, names } = await setup((host) => {
      host.answer = () => throwError(() => new Error('server down'));
    });

    expect(names()).toEqual([]);
    expect(host.errors.length).toBe(1);

    host.answer = (request) => of({ total: 1, rows: [ALL[0]] });
    host.params.set({ day: '2026-10-01' });
    await fixture.whenStable();
    expect(names()).toEqual(['Row 1']);
  });

  it('should keep room for the smallest page and hide the paginator only when all rows fit in it', async () => {
    const { fixture, host, element } = await setup((host) => host.pageSize.set(20));
    const table = element.querySelector<HTMLElement>('app-table')!;

    expect(table.style.getPropertyValue('--table-page-size')).toBe('10'); // smallest page, not 20
    expect(element.querySelector('mat-paginator')).not.toBeNull(); // 25 rows

    host.answer = () => of({ total: 12, rows: ALL.slice(0, 12) });
    host.params.set({ day: '2026-10-02' });
    await fixture.whenStable();
    expect(element.querySelector('mat-paginator')).not.toBeNull(); // one page of 20, still back to 10

    host.answer = () => of({ total: 3, rows: ALL.slice(0, 3) });
    host.params.set({ day: '2026-10-01' });
    await fixture.whenStable();
    expect(element.querySelector('mat-paginator')).toBeNull();
  });

  it('should show no bar above the rows when nothing is put in it', async () => {
    const { element } = await setup();

    expect(element.querySelector('.table__bar')).toBeNull();
  });

  it('should size the columns only from their widths, whatever the rows shown', async () => {
    const { element } = await setup((host) => (host.columns[0].width = 40));
    const headers = [...element.querySelectorAll<HTMLElement>('thead th')];

    expect(headers.map((header) => header.style.width)).toEqual(['40%', '']); // the rest shares 60%
    expect(element.querySelector('table')?.classList).toContain('table__grid'); // fixed layout
  });
});

@Component({
  imports: [Table, TableIconAction, TableTextAction],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    [texts]="texts"
    [searchable]="true"
    [pathIcon]="['/search.svg', '/clear.svg']"
  >
    <button appTableIconAction type="button" class="delete">Elimina</button>
    <button appTableTextAction type="button" class="export">Esporta</button>
  </app-table>`,
})
class TableBarHost {
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<Row>[] = [{ field: 'name', header: 'Nome' }];
  readonly texts = {
    search: { placeholder: 'Cerca nella tabella', submit: 'Cerca', clear: 'Svuota' },
  };
  readonly requests: (Filters & PageRequest)[] = [];
  readonly load = (request: Filters & PageRequest) => {
    this.requests.push(request);
    const found = ALL.filter((row) => row.name.includes(request.search));
    return of({
      total: found.length,
      rows: found.slice((request.page - 1) * request.pageSize, request.page * request.pageSize),
    });
  };
}

describe('Table bar', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  const setup = () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableBarHost);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    return { fixture, host: fixture.componentInstance, element };
  };

  it('should place icon actions, search and text actions in this order', () => {
    const { element } = setup();

    const parts = [...element.querySelector('.table__bar')!.children].map((part) => part.className);
    expect(parts[0]).toContain('table__bar__icons');
    expect(parts[1]).toContain('table__bar__search');
    expect(parts[2]).toContain('table__bar__buttons');
    expect(element.querySelector('.table__bar__icons .delete')).not.toBeNull();
    expect(element.querySelector('.table__bar__buttons .export')).not.toBeNull();
    const input = element.querySelector<HTMLInputElement>('.table__bar__search input')!;
    expect(input.placeholder).toBe('Cerca nella tabella');
    expect(element.querySelector('.table__bar__search img')?.getAttribute('src')).toBe(
      '/search.svg',
    );
  });

  it('should send the search with the request and go back to page 1', () => {
    const { fixture, host, element } = setup();
    element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!.click();
    fixture.detectChanges();
    expect(host.requests.at(-1)).toEqual({ day: '2026-09-30', page: 2, pageSize: 10, search: '' });

    const input = element.querySelector<HTMLInputElement>('.table__bar__search input')!;
    input.value = 'Row 2';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(500); // the search field waits 0.5 s after the last key
    fixture.detectChanges();

    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 1,
      pageSize: 10,
      search: 'Row 2',
    });
    const names = [...element.querySelectorAll('tbody td')].map((cell) => cell.textContent?.trim());
    expect(names).toEqual(['Row 2', 'Row 20', 'Row 21', 'Row 22', 'Row 23', 'Row 24', 'Row 25']);
  });
});
