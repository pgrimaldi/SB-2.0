import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { MatPaginatorIntl, MatPaginatorModule } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { DEFAULT_PAGE_SIZE } from '../../../../../entities/pagination/page';
import { I18nText } from '../../../i18n/i18n-text/i18n-text';
import { TablePaginatorIntl } from './table-paginator-intl';

/** A column of `app-table`: which field of the row it shows and under which header. */
export interface TableColumn<T> {
  field: keyof T & string;
  /** Translation key of the header. */
  header: string;
  /** Horizontal alignment of header and cells; start by default. */
  align?: 'start' | 'center' | 'end';
  /**
   * Share of the table width, in percent (e.g. 40). Columns without it split the rest equally; with
   * no widths at all every column is the same. Widths never depend on the rows shown.
   */
  width?: number;
}

/**
 * Generic table (Angular Material table) paginated by the server: rows of any shape, columns chosen
 * by the caller (`columns: TableColumn<Item>[]`).
 * `<app-table [rows]="result.rows" [total]="result.total" [columns]="columns" [(page)]="page" />`
 * `rows` are the rows of the current page, `total` the rows of all pages. The paginator appears only
 * with more than one page; moving with it changes `page` (from 1), which the caller asks the API for.
 */
@Component({
  selector: 'app-table',
  imports: [I18nText, MatPaginatorModule, MatTableModule],
  providers: [{ provide: MatPaginatorIntl, useClass: TablePaginatorIntl }],
  templateUrl: './table.html',
  styleUrl: './table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[style.--table-page-size]': 'pageSize()' },
})
export class Table<T> {
  readonly rows = input.required<readonly T[]>();
  readonly columns = input.required<readonly TableColumn<T>[]>();
  /** Rows on the server, in all pages. */
  readonly total = input.required<number>();
  /** Current page, from 1. */
  readonly page = model(1);
  /** Rows in a page. */
  readonly pageSize = input(DEFAULT_PAGE_SIZE);

  protected readonly fields = computed(() => this.columns().map((column) => column.field));
}
