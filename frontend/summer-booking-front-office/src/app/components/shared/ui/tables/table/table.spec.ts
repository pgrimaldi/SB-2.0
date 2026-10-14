import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { DATA_RELOAD } from '../../data/data-reload';
import {
  Table,
  TableColumn,
  TableColumnAlign,
  TablePage as Page,
  TablePageRequest as PageRequest,
  TableFilter,
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
    [filters]="filters()"
    [load]="load"
    [(pageSize)]="pageSize"
    [texts]="texts"
    [useAppTheme]="true"
    [isCreateButtonHidden]="true"
    [areMassiveActionsHidden]="true"
    (loadError)="errors.push($event)"
  />`,
})
class TableHost {
  readonly params = signal<Filters | null>({ day: '2026-09-30' });
  readonly filters = signal<TableFilter[] | null>(null);
  readonly pageSize = signal(10);
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'Nome', sortable: true },
    { field: 'total', header: 'Totale', align: TableColumnAlign.Center },
  ];
  readonly texts = {
    empty: 'La tabella non contiene elementi',
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
        filters: [],
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
      filters: [],
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
      filters: [],
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
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
  });

  it('should send the filters of the page and go back to page 1 when they change', async () => {
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();

    host.filters.set([{ field: 'bookingDayType', value: ['Morning'] }]);
    await fixture.whenStable();

    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 1,
      pageSize: 10,
      search: '',
      filters: [{ field: 'bookingDayType', value: ['Morning'] }],
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

  it('should go once to the last page there is when the rows of its page are deleted', async () => {
    const { fixture, host, element, names, next } = await setup();
    next().click();
    await fixture.whenStable();
    next().click();
    await fixture.whenStable();
    expect(names()).toEqual(ALL.slice(20).map((row) => row.name)); // page 3: rows 21-25

    const left = ALL.slice(0, 20); // the 5 rows of page 3 deleted
    host.answer = (request) =>
      of({
        total: left.length,
        rows: left.slice((request.page - 1) * request.pageSize, request.page * request.pageSize),
      });
    fixture.debugElement
      .query((node) => node.componentInstance instanceof Table)
      .componentInstance.reload();
    await fixture.whenStable();

    expect(host.requests.slice(-2).map((request) => request.page)).toEqual([3, 2]);
    expect(names()).toEqual(ALL.slice(10, 20).map((row) => row.name));
    expect(element.querySelector('.table__empty__cell')).toBeNull();
  });

  it('should stay on its page when the request fails, to load it again', async () => {
    const { fixture, host, next } = await setup();
    next().click();
    await fixture.whenStable();
    next().click();
    await fixture.whenStable();
    host.answer = () => throwError(() => new Error('network down'));

    fixture.debugElement
      .query((node) => node.componentInstance instanceof Table)
      .componentInstance.reload();
    await fixture.whenStable();

    expect(host.requests.slice(-2).map((request) => request.page)).toEqual([3, 3]);
    expect(host.errors.length).toBe(1);
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

    host.answer = () => of({ total: 1, rows: [ALL[0]] });
    host.params.set({ day: '2026-10-01' });
    await fixture.whenStable();
    expect(names()).toEqual(['Row 1']);
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

    const base = { day: '2026-09-30', page: 1, pageSize: 10, search: '', filters: [] };
    expect(await sortName()).toEqual({ ...base, sortField: 'name', sortDirection: 'Ascending' });
    expect(await sortName()).toEqual({ ...base, sortField: 'name', sortDirection: 'Descending' });
    expect(await sortName()).toEqual({ ...base, sortField: '', sortDirection: 'Ascending' });

    total.querySelector<HTMLElement>('.mat-sort-header-container')?.click(); // not sortable
    await fixture.whenStable();
    expect(host.requests.at(-1)?.sortField).toBe('');
  });
});

@Component({
  imports: [Table, TableIconAction, TableTextAction],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    [texts]="texts"
    [isSearchable]="true"
    [isCreateButtonHidden]="true"
    [areMassiveActionsHidden]="true"
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
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableBarHost);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    return { fixture, host: fixture.componentInstance, element };
  };

  it('should send the search with the request and go back to page 1', () => {
    const { fixture, host, element } = setup();
    element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!.click();
    fixture.detectChanges();
    expect(host.requests.at(-1)).toEqual({
      day: '2026-09-30',
      page: 2,
      pageSize: 10,
      search: '',
      filters: [],
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
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
    const names = [...element.querySelectorAll('tbody td')].map((cell) => cell.textContent?.trim());
    expect(names).toEqual(['Row 2', 'Row 20', 'Row 21', 'Row 22', 'Row 23', 'Row 24', 'Row 25']);
  });

  it('should empty the search when the page asks, loading everything once from page 1', () => {
    const { fixture, host, element } = setup();
    const input = element.querySelector<HTMLInputElement>('.table__search input')!;
    input.value = 'Row 2';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(500);
    fixture.detectChanges();

    (
      fixture.debugElement.query(By.directive(Table)).componentInstance as Table<Row, Filters>
    ).clearSearch();
    fixture.detectChanges();
    expect(input.value).toBe('');
    expect(host.requests.at(-1)).toMatchObject({ search: '', page: 1 });

    const loads = host.requests.length;
    vi.advanceTimersByTime(500);
    fixture.detectChanges();
    expect(host.requests.length).toBe(loads);
  });

  it('should keep the search when Enter is pressed in the search field', () => {
    const { fixture, host, element } = setup();
    const input = element.querySelector<HTMLInputElement>('.table__search input')!;
    input.value = 'Row 2';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(500);
    fixture.detectChanges();

    // What the browser does on Enter in an <input type="search">.
    input.dispatchEvent(new Event('search', { bubbles: true }));
    fixture.detectChanges();

    expect(host.requests.at(-1)?.search).toBe('Row 2');
  });
});

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

  it('should call the function of the button with the row it is on', async () => {
    const { fixture, host, buttons } = await setup(null);
    host.actions.set(host.all);
    await fixture.whenStable();

    buttons(0)[0].click();
    buttons(1)[1].click();
    buttons(2)[2].click();

    expect(host.events).toEqual(['delete Row 1', 'duplicate Row 2', 'edit Row 3']);
  });
});

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    [texts]="texts"
    [isCreateButtonHidden]="true"
    [areMassiveActionsHidden]="true"
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

  it('should tell the page when the create button is clicked', async () => {
    const { fixture, host, element } = await setup(true);
    const create = element.querySelector<HTMLButtonElement>('.table__create')!;

    create.click();
    await fixture.whenStable();
    expect(host.events).toEqual(['create']);
  });

  it('should show the buttons on the chosen rows only with more than one row chosen', async () => {
    const { fixture, host, boxes, all, massive, click } = await setup(true);

    await click(boxes()[0]);
    expect(massive().length).toBe(0);
    expect(all().indeterminate).toBe(true); // a dash: part of the page is chosen

    await click(boxes()[2]);
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
    [areMassiveActionsHidden]="!show()"
    [isCreateButtonHidden]="!show()"
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

interface SwitchRow {
  idPrinter: string;
  name: string;
  isEnabled: boolean;
}

@Component({
  imports: [Table],
  template: `<app-table
    [columns]="columns"
    [params]="params"
    [load]="load"
    idField="idPrinter"
    [texts]="{ actions: { header: 'Azioni' } }"
    [isCreateButtonHidden]="true"
    [areMassiveActionsHidden]="true"
    [rowActions]="actions"
    [rowToggle]="toggle"
  />`,
})
class TableRowToggleHost {
  readonly params = { day: '2026-09-30' };
  readonly columns: TableColumn<SwitchRow>[] = [{ field: 'name', header: 'Nome' }];
  readonly actions: readonly TableRowAction<SwitchRow>[] = [
    { label: 'Elimina', icon: 'delete.svg', action: () => {} },
  ];
  readonly events: string[] = [];
  readonly toggle = (row: SwitchRow, isEnabled: boolean) =>
    this.events.push(`${row.idPrinter} ${isEnabled}`);
  readonly load = () =>
    of({
      total: 2,
      rows: [
        { idPrinter: 'pr1', name: 'EPSON - 1', isEnabled: false },
        { idPrinter: 'pr2', name: 'EPSON - 2', isEnabled: true },
      ],
    });
}

describe('Table row switch', () => {
  it('should show the isEnabled of each row and give the new state', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableRowToggleHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const cells = [...element.querySelectorAll('tbody td.table__actions')];
    const switches = cells.map((cell) =>
      cell.querySelector<HTMLButtonElement>('app-toggle button')!,
    );

    expect(switches.map((button) => button.classList.contains('mdc-switch--selected'))).toEqual([
      false,
      true,
    ]);

    switches[0].click();
    switches[1].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.events).toEqual(['pr1 true', 'pr2 false']);
  });
});
