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

export interface TableColumn<T> {
  field: keyof T & string;
  header: string;
  align?: 'start' | 'center' | 'end';
  /**
   * Share of the table width, in percent (e.g. 40). Columns without it split the rest equally.
   * Widths never depend on the rows shown.
   */
  width?: number;
  /** Sorted by the server (see `TablePageRequest`). */
  sortable?: boolean;
}

export interface TablePaginatorTexts {
  first?: string;
  previous?: string;
  next?: string;
  last?: string;
  /** Label of the page size selector. */
  size?: string;
  /** With `{{start}}`, `{{end}}` and `{{total}}` (e.g. "{{start}} – {{end}} di {{total}}"). */
  range?: string;
}

/** The placeholder is also the accessible name of the search. */
export interface TableSearchTexts extends SearchFieldTexts {
  placeholder?: string;
}

export interface TableRowActionTexts {
  header?: string;
}

export interface TableRowAction<T> {
  /** Accessible name and tooltip of the button. */
  label: string;
  /**
   * Of the same kind as the icons of the table: a Material icon name when the table has `matIcon`,
   * otherwise an image path.
   */
  icon: string;
  action: (row: T) => void;
}

export interface TableSelectionTexts {
  /** Label of the header checkbox (all the rows of the page). */
  all?: string;
  /** Label of a row checkbox. */
  row?: string;
  duplicate?: string;
  delete?: string;
  clear?: string;
}

export interface TableSortTexts {
  /** Screen-reader description of a sortable header, with `{{column}}`. */
  action?: string;
}

/** The `table` group of our translation file has this shape: `[texts]="'table' | translate"`. */
export interface TableTexts {
  empty?: string;
  paginator?: TablePaginatorTexts;
  search?: TableSearchTexts;
  sort?: TableSortTexts;
  actions?: TableRowActionTexts;
  selection?: TableSelectionTexts;
}

export const TABLE_PAGE_SIZE = 10;

export const TABLE_PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50, 100];

/** The options of the page size selector open in the shared select panel (see the theme). */
const SELECT_CONFIG: MatPaginatorSelectConfig = { panelClass: 'select__panel' };

/** .NET enum names, as the API expects them. */
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

export interface TablePage<T> {
  /** Rows of all the pages. */
  total: number;
  rows: readonly T[];
}

export type TableLoad<P, T> = (request: P & TablePageRequest) => Observable<TablePage<T>>;

export type TableRowId = string | number;

const NO_ROWS: TablePage<never> = { total: 0, rows: [] };
/** Column ids that are not field names, so they never clash with a column of the rows. */
const ROW_ACTIONS_COLUMN = 'table__row__actions';
const SELECT_COLUMN = 'table__row__select';
let nextTableId = 0;

/**
 * Material table that loads its own rows page by page:
 * `<app-table [columns]="columns" [params]="filters" [load]="service.list" />`.
 * Buttons marked `appTableIconAction` go on the left of the bar above the rows, those marked
 * `appTableTextAction` on the right.
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
    '[style.--table-actions-count]': 'rowActions()?.length ?? 0',
    '[class.table__theme__app]': 'useAppTheme()',
    '[class.table__theme__default]': '!useAppTheme()',
  },
})
export class Table<T, P extends object> implements OnDestroy {
  readonly columns = input.required<readonly TableColumn<T>[]>();
  /** `null` loads nothing (e.g. signed out). */
  readonly params = input.required<P | null>();
  readonly load = input.required<TableLoad<P, T>>();
  readonly idField = input<keyof T & string>('id' as keyof T & string);
  readonly pageSize = model(TABLE_PAGE_SIZE);
  /** Ids in the order they were chosen, kept across pages, searches and sorting. */
  readonly selection = model<readonly TableRowId[]>([]);
  readonly pageSizeOptions = input<readonly number[]>(TABLE_PAGE_SIZE_OPTIONS);
  readonly texts = input<TableTexts | null>();
  readonly title = input<string>();
  readonly createLabel = input<string>();
  /** Blue header, striped rows, rounded corners; otherwise the clean, minimal default look. */
  readonly useAppTheme = input(false);
  readonly searchable = input(false);
  /** Buttons of every row, in the last column; empty or `null`: no such column. */
  readonly rowActions = input<readonly TableRowAction<T>[] | null>([]);
  /** Hides the row checkboxes and the buttons on the chosen rows (duplicate, delete). */
  readonly hideMassiveActions = input(false);
  readonly hideCreateButton = input(false);
  /**
   * Icons as Material icon names (Material Symbols font): [search magnifier, search X, duplicate
   * chosen, delete chosen, create, title].
   */
  readonly matIcon = input<readonly string[] | null>();
  /**
   * Icons as image paths, used when `matIcon` is not given: [search magnifier, search X, duplicate
   * chosen, delete chosen, create, title].
   */
  readonly pathIcon = input<readonly string[] | null>();
  readonly loadError = output<unknown>();
  readonly duplicateSelected = output<TableRowId[]>();
  readonly deleteSelected = output<TableRowId[]>();
  readonly create = output<void>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly document = inject(DOCUMENT);
  private readonly paginatorIntl = inject(TablePaginatorIntl);
  /** Its changes (e.g. the language) load the current page again, as it is. */
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  private readonly grid = viewChild.required<string, ElementRef<HTMLTableElement>>('grid', {
    read: ElementRef,
  });
  private readonly iconActions = contentChildren(TableIconAction);
  private readonly textActions = contentChildren(TableTextAction);
  protected readonly hasIconActions = computed(() => this.iconActions().length > 0);
  protected readonly hasTextActions = computed(() => this.textActions().length > 0);
  protected readonly hasIconZone = computed(
    () => !this.hideMassiveActions() || this.hasIconActions(),
  );
  protected readonly hasBar = computed(() => this.hasIconZone() || this.hasTextActions());
  protected readonly hasHeading = computed(
    () => !!this.title() || this.searchable() || !this.hideCreateButton(),
  );
  protected readonly titleId = `table__title__${nextTableId++}`;

  /** From the search field: 0.5 s after the last key, '' under 3 characters. */
  protected readonly search = signal('');

  /** `null`: the server's own order. */
  protected readonly sort = signal<Sort | null>(null);

  /** From 1, unlike Material's page index. */
  protected readonly page = linkedSignal({
    source: () => ({ params: this.params(), search: this.search(), sort: this.sort() }),
    computation: () => 1,
  });

  /** Bumped by `reload()` to load the current page again. */
  private readonly reloads = signal(0);
  /** Bumped when the table width changes or the fonts arrive: the headers are measured again. */
  private readonly layoutChanges = signal(0);
  /** Widths in px of the columns when their shares would leave a header without room; null: the shares. */
  private readonly fittedWidths = signal<readonly number[] | null>(null);
  private resizeObserver?: ResizeObserver;
  /** `undefined`: not created yet; `null`: no canvas in this browser. */
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

  /** The previous page stays on screen until the next one arrives. */
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
  protected readonly selected = computed(() => new Set(this.selection()));
  private readonly selectedOnPage = computed(
    () => this.rows().filter((row) => this.selected().has(this.idOf(row))).length,
  );
  protected readonly allSelected = computed(
    () => this.rows().length > 0 && this.selectedOnPage() === this.rows().length,
  );
  protected readonly someSelected = computed(
    () => this.selectedOnPage() > 0 && !this.allSelected(),
  );
  /**
   * The server answered with no rows. `NO_ROWS` is never an answer (before the first one, after an
   * error, without params), so nothing is said while loading or when the request failed.
   */
  protected readonly empty = computed(() => this.result() !== NO_ROWS && this.rows().length === 0);
  protected readonly total = computed(() => this.result().total);
  protected readonly fields = computed(() => [
    ...(this.hideMassiveActions() ? [] : [SELECT_COLUMN]),
    ...this.columns().map((column) => column.field),
    ...(this.hasRowActions() ? [ROW_ACTIONS_COLUMN] : []),
  ]);
  protected readonly rowActionsColumn = ROW_ACTIONS_COLUMN;
  protected readonly hasRowActions = computed(() => (this.rowActions()?.length ?? 0) > 0);
  protected readonly selectColumn = SELECT_COLUMN;
  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
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
  protected readonly paginated = computed(() => this.total() > this.reservedRows());

  constructor() {
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

  /** Same page, search and sorting: e.g. "retry" after a `loadError`. */
  reload(): void {
    this.reloads.update((count) => count + 1);
  }

  /**
   * With a new size Material keeps the first row on screen: rows 21-30 at 10 per page become page 2
   * (rows 21-40) at 20 per page.
   */
  protected changePage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }

  /** Material cycles ascending, descending, then no direction (no sorting). */
  protected changeSort(sort: Sort): void {
    this.sort.set(sort.direction ? sort : null);
  }

  protected idOf(row: T): TableRowId {
    return row[this.idField()] as TableRowId;
  }

  protected selectAll(checked: boolean): void {
    const page = this.rows().map((row) => this.idOf(row));
    const others = this.selection().filter((id) => !page.includes(id));
    this.selection.set(checked ? [...others, ...page] : others);
  }

  protected selectRow(row: T): void {
    const id = this.idOf(row);
    this.selection.update((ids) =>
      ids.includes(id) ? ids.filter((chosen) => chosen !== id) : [...ids, id],
    );
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
    const others = first + (this.hasRowActions() ? 1 : 0);
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

  /** Arrow function: passed as callback to the ResizeObserver and to `fonts.ready`. */
  private readonly layoutChanged = (): void => {
    this.layoutChanges.update((count) => count + 1);
  };

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }
}
