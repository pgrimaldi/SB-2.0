import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { I18nText } from '../../../i18n/i18n-text/i18n-text';

/** A column of `app-table`: which field of the row it shows and under which header. */
export interface TableColumn<T> {
  field: keyof T & string;
  /** Translation key of the header. */
  header: string;
  /** Horizontal alignment of header and cells; start by default. */
  align?: 'start' | 'center' | 'end';
}

/**
 * Generic table (Angular Material table): rows of any shape, columns chosen by the caller.
 * `<app-table [rows]="items" [columns]="columns" />` with `columns: TableColumn<Item>[]`.
 */
@Component({
  selector: 'app-table',
  imports: [I18nText, MatTableModule],
  templateUrl: './table.html',
  styleUrl: './table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Table<T> {
  readonly rows = input.required<readonly T[]>();
  readonly columns = input.required<readonly TableColumn<T>[]>();

  protected readonly fields = computed(() => this.columns().map((column) => column.field));
}
