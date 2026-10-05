import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { formatText } from '../../texts/format-text';
import type { TablePaginatorTexts } from './table';

@Injectable()
export class TablePaginatorIntl extends MatPaginatorIntl {
  private range?: string;

  /** Called again at every language change: `changes.next()` makes the paginator redraw. */
  setTexts(texts: TablePaginatorTexts | null | undefined): void {
    this.firstPageLabel = texts?.first ?? '';
    this.previousPageLabel = texts?.previous ?? '';
    this.nextPageLabel = texts?.next ?? '';
    this.lastPageLabel = texts?.last ?? '';
    this.itemsPerPageLabel = texts?.size ?? '';
    this.range = texts?.range;
    this.changes.next();
  }

  override getRangeLabel = (page: number, pageSize: number, length: number): string =>
    formatText(this.range, {
      start: Math.min(page * pageSize + 1, length),
      end: Math.min((page + 1) * pageSize, length),
      total: length,
    }) ?? '';
}
