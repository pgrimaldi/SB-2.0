import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  afterRenderEffect,
  computed,
  contentChildren,
  effect,
  inject,
  input,
  linkedSignal,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  MatPaginatorIntl,
  MatPaginatorModule,
  MatPaginatorSelectConfig,
  PageEvent,
} from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable, catchError, of, switchMap } from 'rxjs';
import { DATA_RELOAD } from '../../../data/data-reload';
import { SelectionCheckbox } from '../../checkboxes/selection-checkbox/selection-checkbox';
import { resolveIcons } from '../../icons/icons';
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

/** Header of the column of the row buttons, and names (and tooltips) of the buttons: delete, duplicate, edit. */
export interface TableRowActionTexts {
  /** Header of the column (e.g. "Azioni"). */
  header?: string;
  delete?: string;
  duplicate?: string;
  edit?: string;
}

/**
 * Names of the checkboxes of the rows and names (and tooltips) of the buttons acting on the chosen
 * rows (unless `hideMassiveActions`).
 */
export interface TableSelectionTexts {
  /** Checkbox of the header: all the rows of the page (e.g. "Scegli tutte le righe della pagina"). */
  all?: string;
  /** Checkbox of a row (e.g. "Scegli la riga"). */
  row?: string;
  duplicate?: string;
  delete?: string;
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
  /** The buttons on every row (unless `hideEditButtons`). */
  actions?: TableRowActionTexts;
  /** The checkboxes of the rows and the buttons on the chosen rows (unless `hideMassiveActions`). */
  selection?: TableSelectionTexts;
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
/** Id of the last column, with the buttons of the row: not a field, so it never meets a column of the rows. */
const ROW_ACTIONS_COLUMN = 'table__row__actions';
/** Id of the first column, with the checkboxes of the rows (not a field either). */
const SELECT_COLUMN = 'table__row__select';
/** Numbers the tables of the page, for the id of their title. */
let nextTableId = 0;

/**
 * Generic table (Angular Material table) that loads its rows page by page by itself:
 * `<app-table [columns]="columns" [params]="filters" [load]="service.list" />`.
 * It only needs:
 * - `columns`: which fields of the rows it shows (`TableColumn<Item>[]`);
 * - `load`: the function that asks a page, e.g. an API of a service;
 * - `params`: what that function needs besides the page (e.g. filters); `null` loads nothing.
 * Headers and texts (`texts`, see `TableTexts`) arrive already translated.
 * The base table shows everything; each table hides what it does not need (`hideCreateButton`,
 * `hideMassiveActions`, `hideEditButtons`).
 * On top a row with, on the left, the `title` (and its icon, usually the one of the section) and the
 * search (`searchable`, sent to `load` as `search`) and, on the right, the create button
 * (`createLabel`, emits `create`).
 * Under it, above the rows, a bar: icon buttons marked `appTableIconAction` on the left and text
 * buttons marked `appTableTextAction` on the right; each part shows only when it is used. The first column has a checkbox on every row (and one in the header for the whole page) and,
 * when more than one row is chosen, the bar shows the buttons duplicate and delete of the chosen rows
 * (`duplicateSelected`, `deleteSelected`, with the rows). The last column has the buttons of every
 * row: delete, duplicate and edit (`deleteRow`, `duplicateRow`, `editRow`, with the row).
 * Icons (see `Icons`): [search magnifier, search X, delete, duplicate, edit, duplicate chosen,
 * delete chosen, create, title].
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
  imports: [
    FormatTextPipe,
    MatIconModule,
    MatPaginatorModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule,
    SearchField,
    SelectionCheckbox,
  ],
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
export class Table<T, P extends object> implements OnDestroy {
  readonly columns = input.required<readonly TableColumn<T>[]>();
  /** Parameters of every request besides the page; `null` loads nothing (e.g. signed out). */
  readonly params = input.required<P | null>();
  readonly load = input.required<TableLoad<P, T>>();
  /** Rows in a page, two-way: the user changes it with the selector of the paginator. */
  readonly pageSize = model(TABLE_PAGE_SIZE);
  /** Choices of the page size selector. */
  readonly pageSizeOptions = input<readonly number[]>(TABLE_PAGE_SIZE_OPTIONS);
  readonly texts = input<TableTexts | null>();
  /** Title above the table, already translated (e.g. "Impostazioni magazzino"); without it, no title. */
  readonly title = input<string>();
  /** Text of the create button, already translated (e.g. "Aggiungi articolo"). */
  readonly createLabel = input<string>();
  /**
   * The app's look (blue header, striped rows, rounded corners, `align` of the columns) instead of the
   * default one (clean and minimal: blue texts on white, a line under the header, everything on the
   * left, the rows shown out of the total between the arrows of the paginator).
   */
  readonly useAppTheme = input(false);
  /** Shows the search in the title row, on the left (after the title). */
  readonly searchable = input(false);
  /** Hides the buttons of every row (delete, duplicate, edit), shown by default in the last column. */
  readonly hideEditButtons = input(false);
  /**
   * Hides the checkboxes of the rows and the buttons on the chosen rows (duplicate, delete), shown by
   * default: the checkboxes in the first column, the buttons in the bar above the rows.
   */
  readonly hideMassiveActions = input(false);
  /** Hides the create button, shown by default on the right of the title row. */
  readonly hideCreateButton = input(false);
  /**
   * Icons as Material icon names (Material Symbols font): [search magnifier, search X, delete,
   * duplicate, edit, duplicate chosen, delete chosen, create, title].
   */
  readonly matIcon = input<readonly string[] | null>();
  /**
   * Icons as image paths, used when `matIcon` is not given: [search magnifier, search X, delete,
   * duplicate, edit, duplicate chosen, delete chosen, create, title].
   */
  readonly pathIcon = input<readonly string[] | null>();
  /** A page could not be loaded; the table shows no rows meanwhile. */
  readonly loadError = output<unknown>();
  /** The delete button of a row was pressed: the row. */
  readonly deleteRow = output<T>();
  /** The duplicate button of a row was pressed: the row. */
  readonly duplicateRow = output<T>();
  /** The edit button of a row was pressed: the row. */
  readonly editRow = output<T>();
  /** The duplicate button of the chosen rows was pressed: the chosen rows, in page order. */
  readonly duplicateSelected = output<T[]>();
  /** The delete button of the chosen rows was pressed: the chosen rows, in page order. */
  readonly deleteSelected = output<T[]>();
  /** The create button was pressed. */
  readonly create = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly document = inject(DOCUMENT);
  private readonly paginatorIntl = inject(TablePaginatorIntl);
  /** When it changes (e.g. the language), the current page is loaded again, as it is. */
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  private readonly grid = viewChild.required<string, ElementRef<HTMLTableElement>>('grid', {
    read: ElementRef,
  });
  private readonly iconActions = contentChildren(TableIconAction);
  private readonly textActions = contentChildren(TableTextAction);
  protected readonly hasIconActions = computed(() => this.iconActions().length > 0);
  protected readonly hasTextActions = computed(() => this.textActions().length > 0);
  /** Left part of the bar: the buttons on the chosen rows and the icon buttons of the page. */
  protected readonly hasIconZone = computed(
    () => !this.hideMassiveActions() || this.hasIconActions(),
  );
  protected readonly hasBar = computed(() => this.hasIconZone() || this.hasTextActions());
  protected readonly hasHeading = computed(
    () => !!this.title() || this.searchable() || !this.hideCreateButton(),
  );
  protected readonly titleId = `table__title__${nextTableId++}`;

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
  /** Counts the changes of the table width (window, breakpoints) and the fonts arriving: the headers are measured again. */
  private readonly layoutChanges = signal(0);
  /** Widths in px of the columns when their shares would leave a header without room; null: the shares. */
  private readonly fittedWidths = signal<readonly number[] | null>(null);
  private resizeObserver?: ResizeObserver;
  /** Measures the header titles; created on the first measure, null where there is no canvas. */
  private textMeasurer?: OffscreenCanvasRenderingContext2D | null;

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
  /** Chosen rows of the page; none again whenever a page arrives (other page, search, sorting…). */
  protected readonly selected = linkedSignal<readonly T[], ReadonlySet<T>>({
    source: this.rows,
    computation: () => new Set<T>(),
  });
  /** The chosen rows, in page order. */
  protected readonly selectedRows = computed(() =>
    this.rows().filter((row) => this.selected().has(row)),
  );
  protected readonly allSelected = computed(
    () => this.rows().length > 0 && this.selectedRows().length === this.rows().length,
  );
  protected readonly someSelected = computed(
    () => this.selectedRows().length > 0 && !this.allSelected(),
  );
  /**
   * The server answered with no rows. `NO_ROWS` is never an answer (before the first one, after an
   * error, without params), so nothing is said while loading or when the request failed.
   */
  protected readonly empty = computed(() => this.result() !== NO_ROWS && this.rows().length === 0);
  protected readonly total = computed(() => this.result().total);
  /** Fields of the columns, with the checkboxes first and the row buttons at the end when shown. */
  protected readonly fields = computed(() => [
    ...(this.hideMassiveActions() ? [] : [SELECT_COLUMN]),
    ...this.columns().map((column) => column.field),
    ...(this.hideEditButtons() ? [] : [ROW_ACTIONS_COLUMN]),
  ]);
  protected readonly rowActionsColumn = ROW_ACTIONS_COLUMN;
  protected readonly selectColumn = SELECT_COLUMN;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  /** Width of every column header: the share of the column (`width`), or its fitted width. */
  protected readonly headerWidths = computed(() => {
    const fitted = this.fittedWidths();
    return this.columns().map((column, index) =>
      fitted ? `${fitted[index]}px` : column.width ? `${column.width}%` : null,
    );
  });
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
    // The paginator texts follow the given texts.
    effect(() => this.paginatorIntl.setTexts(this.texts()?.paginator));
    // Every column has at least the room of its header (title and sort arrow): on small screens the
    // other columns give up room and, when there is none left, the rows scroll sideways inside the
    // table instead of overlapping.
    afterRenderEffect({
      earlyRead: () => this.fitColumns(),
      write: (widths) => this.fittedWidths.set(widths()),
    });
    afterNextRender(() => {
      if (typeof ResizeObserver !== 'undefined') {
        this.resizeObserver = new ResizeObserver(this.layoutChanged);
        this.resizeObserver.observe(this.host.nativeElement);
      }
      void this.document.fonts?.ready.then(this.layoutChanged);
    });
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

  /** The checkbox of the header: all the rows of the page, or none. */
  protected selectAll(checked: boolean): void {
    this.selected.set(new Set(checked ? this.rows() : []));
  }

  /** The checkbox of a row. */
  protected selectRow(row: T, checked: boolean): void {
    const selected = new Set(this.selected());
    if (checked) {
      selected.add(row);
    } else {
      selected.delete(row);
    }
    this.selected.set(selected);
  }

  /**
   * Widths of the columns that give every header its room, or null when the shares already do:
   * `width` columns take their percentage of the table, the others share equally what is left after
   * them and after the columns of the checkboxes and of the row buttons. A column without room keeps
   * just its room; the others share what is left by their shares, and the table grows (scrolling)
   * when nothing is left.
   */
  private fitColumns(): readonly number[] | null {
    this.layoutChanges();
    const columns = this.columns();
    const first = this.hideMassiveActions() ? 0 : 1; // the checkboxes come before the columns
    const others = first + (this.hideEditButtons() ? 0 : 1);
    this.useAppTheme(); // paddings and font weight of the headers depend on the look
    const grid = this.grid().nativeElement;
    const headers = [...grid.querySelectorAll<HTMLElement>('thead th')];
    const available = grid.parentElement?.clientWidth ?? 0;
    // Not laid out (hidden, or no layout at all): nothing to fit yet.
    if (!available || headers.length < columns.length + others) {
      return null;
    }
    // Columns of checkboxes and row buttons: their own fixed widths.
    const fixed = headers
      .filter((_, index) => index < first || index >= first + columns.length)
      .reduce((sum, header) => sum + header.getBoundingClientRect().width, 0);
    const rooms = columns.map((column, index) =>
      this.headerRoom(headers[first + index], column.header, !!column.sortable),
    );
    const shared = 1 - columns.reduce((sum, column) => sum + (column.width ?? 0), 0) / 100;
    const sharing = columns.filter((column) => !column.width).length;
    const shares = columns.map((column) => (column.width ? column.width / 100 : shared / sharing));
    const natural = columns.map((column) =>
      column.width ? (available * column.width) / 100 : (available * shared - fixed) / sharing,
    );
    if (natural.every((width, index) => width >= rooms[index])) {
      return null;
    }
    const width = Math.max(available, fixed + rooms.reduce((sum, room) => sum + room, 0));
    const kept = new Set<number>();
    for (;;) {
      const free = shares.map((_, index) => index).filter((index) => !kept.has(index));
      const space = width - fixed - [...kept].reduce((sum, index) => sum + rooms[index], 0);
      const weight = free.reduce((sum, index) => sum + shares[index], 0);
      const widths = rooms.map((room, index) =>
        kept.has(index)
          ? room
          : weight > 0
            ? (space * shares[index]) / weight
            : space / free.length,
      );
      const short = free.filter((index) => widths[index] < rooms[index]);
      if (!short.length) {
        return widths;
      }
      short.forEach((index) => kept.add(index));
    }
  }

  /** Room a header needs: its title in its font, the sort arrow (when sortable) and the paddings. */
  private headerRoom(header: HTMLElement, title: string, sortable: boolean): number {
    const style = getComputedStyle(header);
    const container = header.querySelector<HTMLElement>('.mat-sort-header-container');
    const arrow = sortable ? header.querySelector<HTMLElement>('.mat-sort-header-arrow') : null;
    const px = (value: string) => parseFloat(value) || 0;
    const containerPadding = container
      ? px(getComputedStyle(container).paddingLeft) + px(getComputedStyle(container).paddingRight)
      : 0;
    const arrowRoom = arrow
      ? arrow.getBoundingClientRect().width +
        px(getComputedStyle(arrow).marginLeft) +
        px(getComputedStyle(arrow).marginRight)
      : 0;
    return (
      this.textWidth(title, style) +
      arrowRoom +
      containerPadding +
      px(style.paddingLeft) +
      px(style.paddingRight) +
      1 // rounding of the browser
    );
  }

  /** Width of a text in the font of an element; 0 where the browser cannot measure it. */
  private textWidth(text: string, style: CSSStyleDeclaration): number {
    if (this.textMeasurer === undefined) {
      this.textMeasurer =
        typeof OffscreenCanvas === 'undefined' ? null : new OffscreenCanvas(0, 0).getContext('2d');
    }
    if (!this.textMeasurer) {
      return 0;
    }
    this.textMeasurer.font = `${style.fontStyle} ${style.fontWeight} ${style.fontSize} ${style.fontFamily}`;
    return this.textMeasurer.measureText(text).width;
  }

  /** Listener of the table size and of the fonts: measure the headers again. */
  private readonly layoutChanged = (): void => {
    this.layoutChanges.update((count) => count + 1);
  };

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }
}
