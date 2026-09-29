import { Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { TranslateService } from '@ngx-translate/core';

/** Texts of the Material paginator, from the translations; they follow the language change. */
@Injectable()
export class TablePaginatorIntl extends MatPaginatorIntl {
  private readonly translateService = inject(TranslateService);

  constructor() {
    super();
    this.translateService
      .stream([
        'table.paginator.first',
        'table.paginator.previous',
        'table.paginator.next',
        'table.paginator.last',
      ])
      .pipe(takeUntilDestroyed())
      .subscribe((texts: Record<string, string>) => {
        this.firstPageLabel = texts['table.paginator.first'];
        this.previousPageLabel = texts['table.paginator.previous'];
        this.nextPageLabel = texts['table.paginator.next'];
        this.lastPageLabel = texts['table.paginator.last'];
        this.changes.next();
      });
  }

  /** "11 – 20 di 25". */
  override getRangeLabel = (page: number, pageSize: number, length: number): string =>
    this.translateService.instant('table.paginator.range', {
      start: Math.min(page * pageSize + 1, length),
      end: Math.min((page + 1) * pageSize, length),
      total: length,
    });
}
