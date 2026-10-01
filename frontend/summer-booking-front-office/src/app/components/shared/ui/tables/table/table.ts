import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  effect,
  inject,
  input,
  linkedSignal,
  model,
  output,
  signal,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  MatPaginatorIntl,
  MatPaginatorModule,
  MatPaginatorSelectConfig,
  PageEvent,
} from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Observable, catchError, of, switchMap } from 'rxjs';
import { DATA_RELOAD } from '../../../data/data-reload';
import { SearchField, SearchFieldTexts } from '../../inputs/search-field/search-field';
import { FormatTextPipe } from '../../texts/format-text';
import { TableIconAction } from './table-icon-action';
import { TablePaginatorIntl } from './table-paginator-intl';
import { TableTextAction } from './table-text-action';

/** A column of `app-table`: which field of the row it shows and under which header. */
export interface TableColumn<T> {
  field: keyof T & string;
  /** Header text, already translated. */
  header: string;
  /** Horizontal alignment of header and cells; start by default. */
  align?: 'start' | 'center' | 'end';
  /**
   * Share of the table width, in percent (e.g. 40). Columns without it split the rest equally; with
   * no widths at all every column is the same. Widths never depend on the rows shown.
   */
  width?: number;
  /** A click on the header sorts by this column (done by the server, see `TablePageRequest`). */
  sortable?: boolean;
}

/** Texts of the `app-table` paginator, already translated; a missing one is left out. */
export interface TablePaginatorTexts {
  /** Accessible names of the paginator buttons. */
  first?: string;
  previous?: string;
  next?: string;
  last?: string;
  /** Label of the page size selector (e.g. "Elementi per pagina:"). */
  size?: string;
  /** Rows shown, with `{{start}}`, `{{end}}` and `{{total}}` (e.g. "{{start}} – {{end}} di {{total}}"). */
  range?: string;
}

/** Texts of the `app-table` search: placeholder (also its accessible name) and its two buttons. */
export interface TableSearchTexts extends SearchFieldTexts {
  placeholder?: string;
}

/** Texts of the `app-table` sorting. */
export interface TableSortTexts {
  /** Description of a sortable header for screen readers, with `{{column}}` (e.g. "Ordina per {{column}}"). */
  action?: string;
}

/**
 * Texts of `app-table`, already translated, in groups: `{ paginator: { … }, search: { … }, sort: { … } }`.
 * With our translation file the group `table` has the same shape: `[texts]="'table' | translate"`.
 */
export interface TableTexts {
  /** Shown in place of the rows when the server answers with none (e.g. "La tabella non contiene elementi"). */
  empty?: string;
  paginator?: TablePaginatorTexts;
  search?: TableSearchTexts;
  sort?: TableSortTexts;
}

/** Rows in a page when `pageSize` is not given: the standard of every table. */
export const TABLE_PAGE_SIZE = 10;

/** Choices of the page size selector when `pageSizeOptions` is not given. */
export const TABLE_PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50, 100];

/** The options of the page size selector open in the shared select panel (see the theme). */
const SELECT_CONFIG: MatPaginatorSelectConfig = { panelClass: 'select__panel' };

/** Direction of the sorting, as the API (.NET enum names) expects it. */
export type TableSortDirection = 'Ascending' | 'Descending';

/**
 * Page asked by the table: `page` starts from 1; `search` is the searched text ('' for none);
 * `sortField` is the `field` of the sorted column ('' for none: the server's own order) and
 * `sortDirection` its direction (meaningful only with a `sortField`).
 */
export interface TablePageRequest {
  page: number;
  pageSize: number;
  search: string;
  sortField: string;
  sortDirection: TableSortDirection;
}

/** Page answered to the table: the rows of the page and how many rows there are in all pages. */
export interface TablePage<T> {
  total: number;
  rows: readonly T[];
}

/**
 * How the table gets a page: the caller's parameters plus page, page size and search → `{ total, rows }`.
 * Any function (e.g. an API of a service) with this shape works: the table imports nothing else.
 */
export type TableLoad<P, T> = (request: P & TablePageRequest) => Observable<TablePage<T>>;

const NO_ROWS: TablePage<never> = { total: 0, rows: [] };

/**
 * Generic table (Angular Material table) that loads its rows page by page by itself:
 * `<app-table [columns]="columns" [params]="filters" [load]="service.list" />`.
 * It only needs:
 * - `columns`: which fields of the rows it shows (`TableColumn<Item>[]`);
 * - `load`: the function that asks a page, e.g. an API of a service;
 * - `params`: what that function needs besides the page (e.g. filters); `null` loads nothing.
 * Headers and texts (`texts`, see `TableTexts`) arrive already translated.
 * Above the rows an optional bar: icon buttons marked `appTableIconAction`, the search (`searchable`,
 * sent to `load` as `search`) and text buttons marked `appTableTextAction`; each part shows only
 * when it is used. Icons (see `Icons`): [search magnifier, search X].
 * Columns with `sortable` sort on a click of their header (sent to `load` as `sortField` and
 * `sortDirection`). Two looks: the default one (clean, minimal) and, with `useAppTheme`, the app's one.
 * The paginator, under the rows, has a page size selector (`pageSizeOptions`, 10 by default): with
 * more rows the page scrolls and the paginator stays at the bottom of the screen.
 * Then it works on its own: it asks page 1, moves with its paginator, goes back to page 1 and loads
 * again whenever `params` or the search change, keeps the current rows on screen until the next page arrives and,
 * when a request fails, shows no rows and emits `loadError` (the next requests still work).
 */
@Component({
  selector: 'app-table',
  imports: [FormatTextPipe, MatPaginatorModule, MatSortModule, MatTableModule, SearchField],
  providers: [TablePaginatorIntl, { provide: MatPaginatorIntl, useExisting: TablePaginatorIntl }],
  templateUrl: './table.html',
  styleUrls: [
    './table.scss',
    './table-bar.scss',
    './table-app-theme.scss',
    './table-default-theme.scss',
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[style.--table-page-size]': 'reservedRows()',
    '[class.table__theme__app]': 'useAppTheme()',
    '[class.table__theme__default]': '!useAppTheme()',
  },
})
export class Table<T, P extends object> {
  readonly columns = input.required<readonly TableColumn<T>[]>();
  /** Parameters of every request besides the page; `null` loads nothing (e.g. signed out). */
  readonly params = input.required<P | null>();
  readonly load = input.required<TableLoad<P, T>>();
  /** Rows in a page, two-way: the user changes it with the selector of the paginator. */
  readonly pageSize = model(TABLE_PAGE_SIZE);
  /** Choices of the page size selector. */
  readonly pageSizeOptions = input<readonly number[]>(TABLE_PAGE_SIZE_OPTIONS);
  readonly texts = input<TableTexts | null>();
  /**
   * The app's look (blue header, striped rows, rounded corners, `align` of the columns) instead of the
   * default one (clean and minimal: blue texts on white, a line under the header, everything on the
   * left, only the page number between the arrows).
   */
  readonly useAppTheme = input(false);
  /** Shows the search in the bar above the rows. */
  readonly searchable = input(false);
  /** Icons as Material icon names (Material Symbols font): [search magnifier, search X]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [search magnifier, search X]. */
  readonly pathIcon = input<readonly string[] | null>();
  /** A page could not be loaded; the table shows no rows meanwhile. */
  readonly loadError = output<unknown>();

  private readonly paginatorIntl = inject(TablePaginatorIntl);
  /** When it changes (e.g. the language), the current page is loaded again, as it is. */
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  private readonly iconActions = contentChildren(TableIconAction);
  private readonly textActions = contentChildren(TableTextAction);
  protected readonly hasIconActions = computed(() => this.iconActions().length > 0);
  protected readonly hasTextActions = computed(() => this.textActions().length > 0);
  protected readonly hasBar = computed(
    () => this.hasIconActions() || this.searchable() || this.hasTextActions(),
  );

  /** Searched text, from the search field (0.5 s after the last key, '' under 3 characters). */
  protected readonly search = signal('');

  /** Sorted column, from the headers; `null` for none (the server's own order). */
  protected readonly sort = signal<Sort | null>(null);

  /** Current page, from 1; back to the first one whenever `params`, the search or the sorting change. */
  protected readonly page = linkedSignal({
    source: () => ({ params: this.params(), search: this.search(), sort: this.sort() }),
    computation: () => 1,
  });

  /** Counts the `reload()` calls: each one loads the current page again. */
  private readonly reloads = signal(0);

  private readonly request = computed(() => {
    this.dataReload?.();
    this.reloads();
    const params = this.params();
    const sort = this.sort();
    return params
      ? {
          ...params,
          page: this.page(),
          pageSize: this.pageSize(),
          search: this.search(),
          sortField: sort?.active ?? '',
          sortDirection: (sort?.direction === 'desc'
            ? 'Descending'
            : 'Ascending') as TableSortDirection,
        }
      : null;
  });

  /** The page answered by `load`; the previous one stays on screen until the next arrives. */
  protected readonly result = toSignal(
    toObservable(this.request).pipe(
      switchMap((request) =>
        request
          ? this.load()(request).pipe(
              catchError((error: unknown) => {
                this.loadError.emit(error);
                return of(NO_ROWS);
              }),
            )
          : of(NO_ROWS),
      ),
    ),
    { initialValue: NO_ROWS },
  );

  protected readonly rows = computed(() => this.result().rows);
  /**
   * The server answered with no rows. `NO_ROWS` is never an answer (before the first one, after an
   * error, without params), so nothing is said while loading or when the request failed.
   */
  protected readonly empty = computed(() => this.result() !== NO_ROWS && this.rows().length === 0);
  protected readonly total = computed(() => this.result().total);
  protected readonly fields = computed(() => this.columns().map((column) => column.field));
  protected readonly selectConfig = SELECT_CONFIG;

  /**
   * Rows of the smallest page: the table always keeps room for them, so the paginator never moves
   * up on a shorter page; bigger pages simply make the table (and the page) longer.
   */
  protected readonly reservedRows = computed(() =>
    Math.min(this.pageSize(), ...this.pageSizeOptions()),
  );
  /** The paginator (and its selector) only when rows do not fit in the smallest page. */
  protected readonly paginated = computed(() => this.total() > this.reservedRows());

  constructor() {
    // The paginator texts follow the given texts and the look.
    effect(() => this.paginatorIntl.setTexts(this.texts()?.paginator, !this.useAppTheme()));
  }

  /**
   * Loads the current page again, as it is (same page, search and sorting): e.g. "retry" after a
   * `loadError`. With a template reference: `<app-table #table … />` and `table.reload()`.
   */
  reload(): void {
    this.reloads.update((count) => count + 1);
  }

  /**
   * A new page or page size. With a new size Material keeps the first row on screen: rows 21-30 at
   * 10 per page become page 2 (rows 21-40) at 20 per page.
   */
  protected changePage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  /** A click on a sortable header: ascending, then descending, then no sorting (Material's cycle). */
  protected changeSort(sort: Sort): void {
    this.sort.set(sort.direction ? sort : null);
  }
}
