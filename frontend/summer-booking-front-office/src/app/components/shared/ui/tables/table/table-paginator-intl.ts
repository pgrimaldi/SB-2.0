import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { formatText } from '../../texts/format-text';
import type { TablePaginatorTexts } from './table';

/** Texts of the Material paginator, from the table `texts`; a missing one stays empty. */
@Injectable()
export class TablePaginatorIntl extends MatPaginatorIntl {
  private range?: string;
  private pageOnly = false;

  /**
   * Applies the texts (again at every language change): the paginator redraws itself. With `pageOnly`
   * (the default look) the range shows just the current page number.
   */
  setTexts(texts: TablePaginatorTexts | null | undefined, pageOnly = false): void {
    this.pageOnly = pageOnly;
    this.firstPageLabel = texts?.first ?? '';
    this.previousPageLabel = texts?.previous ?? '';
    this.nextPageLabel = texts?.next ?? '';
    this.lastPageLabel = texts?.last ?? '';
    this.itemsPerPageLabel = texts?.size ?? '';
    this.range = texts?.range;
    this.changes.next();
  }

  /** "11 – 20 di 25". */
  override getRangeLabel = (page: number, pageSize: number, length: number): string =>
    this.pageOnly
      ? String(page + 1)
      : (formatText(this.range, {
          start: Math.min(page * pageSize + 1, length),
          end: Math.min((page + 1) * pageSize, length),
          total: length,
        }) ?? '');
}
