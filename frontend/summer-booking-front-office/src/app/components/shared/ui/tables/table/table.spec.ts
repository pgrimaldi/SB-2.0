import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { DATA_RELOAD } from '../../../data/data-reload';
import {
  Table,
  TableColumn,
  TablePage as Page,
  TablePageRequest as PageRequest,
  TableRowAction,
  TableRowId,
} from './table';
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
    [useAppTheme]="true"
    [hideCreateButton]="true"
    [hideMassiveActions]="true"
    (loadError)="errors.push($event)"
  />`,
})
class TableHost {
  readonly params = signal<Filters | null>({ day: '2026-09-30' });
  readonly pageSize = signal(10);
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome', sortable: true },
    { field: 'total', header: 'Totale', align: 'center' },
  ];
  readonly texts = {
    empty: 'La tabella non contiene elementi',
    sort: { action: 'Ordina per {{column}}' },
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
      [...element.querySelectorAll('tbody tr.table__row td:first-child')].map((cell) =>
        cell.textContent?.trim(),
      );
    const next = () =>
      element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!;
    return { fixture, host: fixture.componentInstance, element, names, next };
  };

  it('should load the first page with the params and show only the chosen columns', async () => {
    const { host, element, names } = await setup();

    expect(host.requests).toEqual([
      {
        day: '2026-09-30',
        page: 1,
        pageSize: 10,
        search: '',
        sortField: '',
        sortDirection: 'Ascending',
      },
    ]);
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
    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 2,
      pageSize: 20,
      search: '',
      sortField: '',
      sortDirection: 'Ascending',
    });
    expect(names()[0]).toBe('Row 21'); // rows 21-40: row 21 still on screen
  });

  it('should load the next page with its paginator', async () => {
    const { fixture, host, names, next } = await setup();

    next().click();
    await fixture.whenStable();

    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 2,
      pageSize: 10,
      search: '',
      sortField: '',
      sortDirection: 'Ascending',
    });
    expect(names()[0]).toBe('Row 11');
  });

  it('should go back to page 1 and load again when the params change', async () => {
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();

    host.params.set({ day: '2026-10-01' });
    await fixture.whenStable();

    expect(host.requests.at(-1)).toEqual({
      day: '2026-10-01',
      page: 1,
      pageSize: 10,
      search: '',
      sortField: '',
      sortDirection: 'Ascending',
    });
  });

  it('should load the current page again, as it is, when the data context changes (e.g. the language)', async () => {
    const language = signal('it');
    TestBed.configureTestingModule({ providers: [{ provide: DATA_RELOAD, useValue: language }] });
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();
    const loads = host.requests.length;

    language.set('en');
    await fixture.whenStable();

    expect(host.requests.length).toBe(loads + 1);
    expect(host.requests.at(-1)).toEqual(host.requests.at(-2)); // page 2, same search and sorting
    expect(host.requests.at(-1)?.page).toBe(2);
  });

  it('should load the current page again, as it is, with reload()', async () => {
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();
    const loads = host.requests.length;

    fixture.debugElement
      .query((node) => node.componentInstance instanceof Table)
      .componentInstance.reload();
    await fixture.whenStable();

    expect(host.requests.length).toBe(loads + 1);
    expect(host.requests.at(-1)).toEqual(host.requests.at(-2));
  });

  it('should say that the table has no items only when the server answers with none', async () => {
    const pending = new Subject<Page<Row>>();
    const { fixture, host, element } = await setup((host) => (host.answer = () => pending));
    const message = () => element.querySelector('.table__empty__cell')?.textContent?.trim();
    expect(message()).toBe(''); // still loading

    pending.next({ total: 0, rows: [] });
    await fixture.whenStable();
    expect(message()).toBe('La tabella non contiene elementi');

    host.answer = () => throwError(() => new Error('offline'));
    host.params.set({ day: '2026-10-02' });
    await fixture.whenStable();
    expect(message()).toBe(''); // a failed request is not an empty table
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

  it('should sort by a sortable column: ascending, descending, then none, back to page 1', async () => {
    const { fixture, host, element, next } = await setup();
    next().click();
    await fixture.whenStable();
    const [name, total] = [...element.querySelectorAll<HTMLElement>('thead th')];
    const sortName = async () => {
      name.querySelector<HTMLElement>('.mat-sort-header-container')!.click();
      await fixture.whenStable();
      return host.requests.at(-1);
    };

    const base = { day: '2026-09-30', page: 1, pageSize: 10, search: '' };
    expect(await sortName()).toEqual({ ...base, sortField: 'name', sortDirection: 'Ascending' });
    expect(name.getAttribute('aria-sort')).toBe('ascending');
    expect(await sortName()).toEqual({ ...base, sortField: 'name', sortDirection: 'Descending' });
    expect(await sortName()).toEqual({ ...base, sortField: '', sortDirection: 'Ascending' });

    total.querySelector<HTMLElement>('.mat-sort-header-container')?.click(); // not sortable
    await fixture.whenStable();
    expect(host.requests.at(-1)?.sortField).toBe('');
    const description = name.querySelector('[aria-describedby]')?.getAttribute('aria-describedby');
    expect(document.getElementById(description!)?.textContent).toBe('Ordina per Nome');
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
    [hideCreateButton]="true"
    [hideMassiveActions]="true"
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

describe('Table column room', () => {
  /** Width the table has on screen, and the callback of its size watcher. */
  let available = 0;
  let resized: () => void = () => undefined;

  const setup = async (columns: TableColumn<Row>[]) => {
    vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockImplementation(function (
      this: HTMLElement,
    ) {
      return this.classList.contains('table') ? available : 0;
    });
    // 10px for every character of a title.
    vi.stubGlobal(
      'OffscreenCanvas',
      class {
        getContext() {
          return { font: '', measureText: (text: string) => ({ width: text.length * 10 }) };
        }
      },
    );
    vi.stubGlobal(
      'ResizeObserver',
      class {
        constructor(callback: () => void) {
          resized = callback;
        }
        observe() {
          return undefined;
        }
        disconnect() {
          return undefined;
        }
      },
    );
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableHost);
    fixture.componentInstance.columns.splice(0, 2, ...columns);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const widths = () =>
      [...element.querySelectorAll<HTMLElement>('thead th')].map((header) => header.style.width);
    const resize = async (width: number) => {
      available = width;
      resized();
      await fixture.whenStable();
    };
    return { widths, resize };
  };

  it('should keep the shares of the columns while every header has its room', async () => {
    available = 1000;
    const { widths } = await setup([
      { field: 'name', header: 'Nome', sortable: true, width: 40 },
      { field: 'total', header: 'Totale' },
    ]);

    expect(widths()).toEqual(['40%', '']);
  });

  it('should give a header its room and the rest to the others, without scrolling', async () => {
    available = 1000;
    const { widths, resize } = await setup([
      { field: 'name', header: 'Nome', sortable: true, width: 40 },
      { field: 'total', header: 'Totale disponibili' }, // 180px of title
    ]);

    await resize(330); // the shares would give 198px to "Totale disponibili", short of its paddings
    const [name, total] = widths().map((width) => parseFloat(width));

    expect(widths().every((width) => width.endsWith('px'))).toBe(true);
    expect(total).toBeGreaterThanOrEqual(180);
    expect(name).toBeGreaterThanOrEqual(40);
    expect(name + total).toBeCloseTo(330); // the table stays as wide as the screen

    await resize(1000); // room again: back to the shares
    expect(widths()).toEqual(['40%', '']);
  });

  it('should make the table wider than the screen when the headers need it', async () => {
    available = 100;
    const { widths } = await setup([
      { field: 'name', header: 'Nome', sortable: true, width: 40 },
      { field: 'total', header: 'Totale disponibili' },
    ]);
    const [name, total] = widths().map((width) => parseFloat(width));

    expect(name).toBeGreaterThanOrEqual(40);
    expect(total).toBeGreaterThanOrEqual(180);
    expect(name + total).toBeGreaterThan(100); // the rows scroll sideways inside the table
  });
});

describe('Table bar', () => {
  beforeEach(() => vi.useFakeTimers());
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableBarHost);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    return { fixture, host: fixture.componentInstance, element };
  };

  it('should put the search in the title row and the actions in the bar under it', () => {
    const { element } = setup();

    const parts = [...element.querySelector('.table__bar')!.children].map((part) => part.className);
    expect(parts.length).toBe(2);
    expect(parts[0]).toContain('table__bar__icons');
    expect(parts[1]).toContain('table__bar__buttons');
    expect(element.querySelector('.table__bar__icons .delete')).not.toBeNull();
    expect(element.querySelector('.table__bar__buttons .export')).not.toBeNull();
    const input = element.querySelector<HTMLInputElement>('.table__heading .table__search input')!;
    expect(input.placeholder).toBe('Cerca nella tabella');
    expect(element.querySelector('.table__search img')?.getAttribute('src')).toBe('/search.svg');
  });

  it('should send the search with the request and go back to page 1', () => {
    const { fixture, host, element } = setup();
    element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!.click();
    fixture.detectChanges();
    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 2,
      pageSize: 10,
      search: '',
      sortField: '',
      sortDirection: 'Ascending',
    });

    const input = element.querySelector<HTMLInputElement>('.table__search input')!;
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
      sortField: '',
      sortDirection: 'Ascending',
    });
    const names = [...element.querySelectorAll('tbody td')].map((cell) => cell.textContent?.trim());
    expect(names).toEqual(['Row 2', 'Row 20', 'Row 21', 'Row 22', 'Row 23', 'Row 24', 'Row 25']);
  });
});

describe('Table default look', () => {
  it('should align the columns by their align and show the rows out of the total in the paginator', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableDefaultHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('app-table')?.className).toContain('table__theme__default');
    const cells = [...element.querySelectorAll<HTMLElement>('thead th, tbody tr:first-child td')];
    expect(cells.map((cell) => cell.style.textAlign)).toEqual(['', 'center', '', 'center']);
    expect(element.querySelectorAll('thead th')[1].classList).toContain('table__cell__center');
    expect(element.querySelector('.mat-mdc-paginator-range-label')?.textContent?.trim()).toBe(
      '1 – 10 di 25',
    );
  });
});

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    [texts]="texts"
    [hideCreateButton]="true"
    [hideMassiveActions]="true"
  />`,
})
class TableDefaultHost {
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome' },
    { field: 'total', header: 'Totale', align: 'center' },
  ];
  readonly texts = { paginator: { range: '{{start}} – {{end}} di {{total}}' } };
  readonly load = (request: Filters & PageRequest) =>
    of({ total: ALL.length, rows: ALL.slice(0, request.pageSize) });
}

describe('Table row buttons', () => {
  const setup = async (actions: readonly TableRowAction<Row>[] | null, material = false) => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableRowButtonsHost);
    fixture.componentInstance.actions.set(actions);
    fixture.componentInstance.material.set(material);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const buttons = (row: number) => [
      ...element.querySelectorAll<HTMLButtonElement>(
        `tbody tr.table__row:nth-child(${row + 1}) .table__action`,
      ),
    ];
    return { fixture, host: fixture.componentInstance, element, buttons };
  };

  it('should have no row buttons, and no column for them, without rowActions', async () => {
    for (const actions of [null, []]) {
      const { element } = await setup(actions);

      expect(element.querySelectorAll('.table__action').length).toBe(0);
      expect(element.querySelectorAll('thead th').length).toBe(2);
      TestBed.resetTestingModule();
    }
  });

  it('should put the given buttons in the last column, in their order, with names and icons', async () => {
    const { host, element, buttons } = await setup(null);
    host.actions.set(host.all);
    TestBed.tick();

    const headers = [...element.querySelectorAll('thead th')];
    expect(headers.length).toBe(3);
    expect(headers[2].classList).toContain('table__actions');
    expect(headers[2].textContent?.trim()).toBe('Azioni'); // header from texts.actions.header
    expect(buttons(0).map((button) => button.getAttribute('aria-label'))).toEqual([
      'Elimina',
      'Duplica',
      'Modifica',
    ]);
    expect(buttons(0).map((button) => button.querySelector('img')?.getAttribute('src'))).toEqual([
      'delete.svg',
      'duplicate.svg',
      'edit.svg',
    ]);
  });

  it('should show only the buttons a table needs', async () => {
    const edit: TableRowAction<Row> = { label: 'Modifica', icon: 'edit.svg', action: () => {} };
    const { element, buttons } = await setup([edit]);

    expect(element.querySelectorAll('thead th').length).toBe(3);
    expect(buttons(0).map((button) => button.getAttribute('aria-label'))).toEqual(['Modifica']);
  });

  it('should call the function of the button with the row it is on', async () => {
    const { fixture, host, buttons } = await setup(null);
    host.actions.set(host.all);
    await fixture.whenStable();

    buttons(0)[0].click();
    buttons(1)[1].click();
    buttons(2)[2].click();

    expect(host.events).toEqual(['delete Row 1', 'duplicate Row 2', 'edit Row 3']);
  });

  it('should take the icons as Material icon names when the table uses matIcon', async () => {
    const edit: TableRowAction<Row> = { label: 'Modifica', icon: 'edit', action: () => {} };
    const { buttons } = await setup([edit], true);

    expect(buttons(0)[0].querySelector('mat-icon')?.textContent?.trim()).toBe('edit');
    expect(buttons(0)[0].querySelector('img')).toBeNull();
  });
});

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    [texts]="texts"
    [hideCreateButton]="true"
    [hideMassiveActions]="true"
    [rowActions]="actions()"
    [matIcon]="material() ? ['search', 'close'] : null"
    [pathIcon]="['search.svg', 'clear.svg']"
  />`,
})
class TableRowButtonsHost {
  readonly actions = signal<readonly TableRowAction<Row>[] | null>(null);
  readonly material = signal(false);
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome' },
    { field: 'total', header: 'Totale' },
  ];
  readonly texts = { actions: { header: 'Azioni' } };
  readonly events: string[] = [];
  readonly all: readonly TableRowAction<Row>[] = [
    {
      label: 'Elimina',
      icon: 'delete.svg',
      action: (row) => this.events.push('delete ' + row.name),
    },
    {
      label: 'Duplica',
      icon: 'duplicate.svg',
      action: (row) => this.events.push('duplicate ' + row.name),
    },
    { label: 'Modifica', icon: 'edit.svg', action: (row) => this.events.push('edit ' + row.name) },
  ];
  readonly load = (request: Filters & PageRequest) =>
    of({ total: ALL.length, rows: ALL.slice(0, request.pageSize) });
}

describe('Table base', () => {
  it('should show the checkboxes and the create button when nothing is hidden, no row buttons without rowActions', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableBaseHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const headers = [...element.querySelectorAll('thead th')];

    expect(headers.length).toBe(3);
    expect(headers[0].classList).toContain('table__select');
    expect(element.querySelector('.table__actions')).toBeNull();
    expect(element.querySelector('.table__create')).not.toBeNull();
    expect(element.querySelector('.table__bar__icons')).not.toBeNull(); // room of the buttons on chosen rows
  });
});

@Component({
  imports: [Table],
  template: `<app-table [columns]="columns" [params]="params" [load]="load" />`,
})
class TableBaseHost {
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome' },
    { field: 'total', header: 'Totale' },
  ];
  readonly load = (request: Filters & PageRequest) =>
    of({ total: ALL.length, rows: ALL.slice(0, request.pageSize) });
}

describe('Table title, create button and chosen rows', () => {
  const setup = async (show: boolean) => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableMassiveHost);
    fixture.componentInstance.show.set(show);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const boxes = () => [
      ...element.querySelectorAll<HTMLInputElement>('tbody app-selection-checkbox input'),
    ];
    const all = () =>
      element.querySelector<HTMLInputElement>('thead app-selection-checkbox input')!;
    const massive = () => [...element.querySelectorAll<HTMLButtonElement>('.table__massive')];
    const click = async (input: HTMLInputElement) => {
      input.click();
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, element, boxes, all, massive, click };
  };

  it('should show no title row, no checkboxes and no buttons on chosen rows when hidden', async () => {
    const { element } = await setup(false);

    expect(element.querySelector('.table__heading')).toBeNull();
    expect(element.querySelector('app-selection-checkbox')).toBeNull();
    expect(element.querySelector('.table__bar')).toBeNull();
    expect(element.querySelectorAll('thead th').length).toBe(2);
  });

  it('should show the title with its icon and the create button', async () => {
    const { fixture, host, element } = await setup(true);
    const title = element.querySelector('.table__title')!;
    const create = element.querySelector<HTMLButtonElement>('.table__create')!;

    expect(title.textContent?.trim()).toBe('Impostazioni magazzino');
    expect(title.querySelector('img')?.getAttribute('src')).toBe('warehouse.svg');
    expect(element.querySelector('table')?.getAttribute('aria-labelledby')).toBe(title.id);
    expect(create.textContent?.trim()).toBe('Aggiungi articolo');
    expect(create.querySelector('img')?.getAttribute('src')).toBe('add.svg');

    create.click();
    await fixture.whenStable();
    expect(host.events).toEqual(['create']);
  });

  it('should show no icon next to the title when none is given', async () => {
    const { fixture, host, element } = await setup(true);

    host.icons.set(['search.svg', 'clear.svg', 'duplicate-selected.svg', 'delete-selected.svg']);
    await fixture.whenStable();

    expect(element.querySelector('.table__title')?.textContent?.trim()).toBe(
      'Impostazioni magazzino',
    );
    expect(element.querySelector('.table__title img')).toBeNull();
  });

  it('should put a checkbox on every row, first, and one in the header for the whole page', async () => {
    const { element, boxes, all } = await setup(true);

    expect(element.querySelectorAll('thead th').length).toBe(3);
    expect(element.querySelector('thead th')?.classList).toContain('table__select');
    expect(boxes().length).toBe(10);
    expect(all().getAttribute('aria-label')).toBe('Scegli tutte le righe della pagina');
    expect(boxes()[0].getAttribute('aria-label')).toBe('Scegli la riga');
  });

  it('should show the buttons on the chosen rows only with more than one row chosen', async () => {
    const { fixture, host, boxes, all, massive, click } = await setup(true);

    await click(boxes()[0]);
    expect(massive().length).toBe(0);
    expect(all().indeterminate).toBe(true); // a dash: part of the page is chosen

    await click(boxes()[2]);
    expect(massive().map((button) => button.getAttribute('aria-label'))).toEqual([
      'Duplica selezione',
      'Elimina selezione',
    ]);
    expect(massive().map((button) => button.querySelector('img')?.getAttribute('src'))).toEqual([
      'duplicate-selected.svg',
      'delete-selected.svg',
    ]);

    massive()[0].click();
    massive()[1].click();
    await fixture.whenStable();
    expect(host.events).toEqual(['duplicate Row 1,Row 3', 'delete Row 1,Row 3']);
  });

  it('should choose all the rows of the page, or none, from the header', async () => {
    const { boxes, all, massive, click } = await setup(true);

    await click(all());
    expect(boxes().every((box) => box.checked)).toBe(true);
    expect(all().checked).toBe(true);
    expect(massive().length).toBe(2);

    await click(all());
    expect(boxes().some((box) => box.checked)).toBe(false);
    expect(massive().length).toBe(0);
  });

  it('should keep the chosen rows, by id, across pages', async () => {
    const { fixture, host, element, boxes, all, massive, click } = await setup(true);
    const page = async (button: 'next' | 'previous') => {
      element.querySelector<HTMLButtonElement>(`.mat-mdc-paginator-navigation-${button}`)!.click();
      await fixture.whenStable();
    };

    await click(boxes()[0]);
    await click(boxes()[1]);
    await page('next');
    expect(boxes().some((box) => box.checked)).toBe(false); // nothing chosen on page 2
    expect(all().checked || all().indeterminate).toBe(false); // the header speaks for this page
    expect(massive().length).toBe(2); // two rows chosen in all

    await click(boxes()[0]);
    await page('previous');
    expect(
      boxes()
        .map((box) => box.checked)
        .slice(0, 3),
    ).toEqual([true, true, false]);
    expect(host.selection()).toEqual(['Row 1', 'Row 2', 'Row 11']);

    massive()[1].click();
    await fixture.whenStable();
    expect(host.events).toEqual(['delete Row 1,Row 2,Row 11']);
  });

  it('should take away a chosen row when its checkbox is clicked again', async () => {
    const { host, boxes, click } = await setup(true);

    await click(boxes()[0]);
    await click(boxes()[1]);
    await click(boxes()[0]);

    expect(host.selection()).toEqual(['Row 2']);
  });

  it('should add the rows of the page to the chosen ones from the header, keeping the others', async () => {
    const { fixture, host, element, boxes, all, click } = await setup(true);

    await click(boxes()[0]);
    element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!.click();
    await fixture.whenStable();
    await click(all());
    expect(host.selection().length).toBe(11); // Row 1 and the 10 rows of page 2

    await click(all());
    expect(host.selection()).toEqual(['Row 1']);
  });

  it('should empty the chosen rows of every page with its clear button', async () => {
    const { fixture, host, element, boxes, massive, click } = await setup(true);
    const page = async (button: 'next' | 'previous') => {
      element.querySelector<HTMLButtonElement>(`.mat-mdc-paginator-navigation-${button}`)!.click();
      await fixture.whenStable();
    };

    await click(boxes()[0]);
    await page('next');
    await click(boxes()[0]);
    const clear = element.querySelector<HTMLButtonElement>('.table__clear')!;
    expect(clear.textContent?.trim()).toBe('Cancella selezioni');

    clear.click();
    await fixture.whenStable();
    expect(host.selection()).toEqual([]);
    expect(massive().length).toBe(0);
    expect(element.querySelector('.table__clear')).toBeNull();
    expect(boxes().some((box) => box.checked)).toBe(false);
    await page('previous');
    expect(boxes().some((box) => box.checked)).toBe(false);
  });

  it('should let the page empty the chosen rows', async () => {
    const { fixture, host, boxes, massive, click } = await setup(true);

    await click(boxes()[0]);
    await click(boxes()[1]);
    host.selection.set([]);
    await fixture.whenStable();

    expect(boxes().some((box) => box.checked)).toBe(false);
    expect(massive().length).toBe(0);
  });
});

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    idField="name"
    [(selection)]="selection"
    [texts]="texts"
    [title]="show() ? 'Impostazioni magazzino' : undefined"
    [createLabel]="'Aggiungi articolo'"
    [hideMassiveActions]="!show()"
    [hideCreateButton]="!show()"
    [pathIcon]="icons()"
    (duplicateSelected)="events.push('duplicate ' + $event.join(','))"
    (deleteSelected)="events.push('delete ' + $event.join(','))"
    (create)="events.push('create')"
  />`,
})
class TableMassiveHost {
  readonly show = signal(false);
  readonly icons = signal([
    'search.svg',
    'clear.svg',
    'duplicate-selected.svg',
    'delete-selected.svg',
    'add.svg',
    'warehouse.svg',
  ]);
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome' },
    { field: 'total', header: 'Totale' },
  ];
  readonly texts = {
    selection: {
      all: 'Scegli tutte le righe della pagina',
      row: 'Scegli la riga',
      duplicate: 'Duplica selezione',
      delete: 'Elimina selezione',
      clear: 'Cancella selezioni',
    },
  };
  readonly events: string[] = [];
  readonly selection = signal<readonly TableRowId[]>([]);
  readonly load = (request: Filters & PageRequest) =>
    of({
      total: ALL.length,
      rows: ALL.slice((request.page - 1) * request.pageSize, request.page * request.pageSize),
    });
}
