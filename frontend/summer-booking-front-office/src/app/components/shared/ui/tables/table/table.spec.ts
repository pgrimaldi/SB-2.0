import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Table, TableColumn } from './table';

interface Row {
  name: string;
  total: number;
  hidden: string;
}

@Component({
  imports: [Table],
  template: `<app-table [rows]="rows" [total]="rows.length" [columns]="columns" />`,
})
class TableHost {
  readonly rows: Row[] = [
    { name: 'Ombrellone', total: 50, hidden: 'x' },
    { name: 'Lettino', total: 90, hidden: 'y' },
  ];
  readonly columns: TableColumn<Row>[] = [
    { field: 'name', header: 'name' },
    { field: 'total', header: 'total', align: 'center' },
  ];
}

describe('Table', () => {
  it('should show only the chosen columns, in order, for every row', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(TableHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    const cells = [...element.querySelectorAll('tbody tr')].map((row) =>
      [...row.querySelectorAll('td')].map((cell) => cell.textContent?.trim()),
    );
    expect(element.querySelectorAll('thead th').length).toBe(2);
    expect(cells).toEqual([
      ['Ombrellone', '50'],
      ['Lettino', '90'],
    ]);
    expect(element.querySelector<HTMLElement>('tbody td:last-child')?.style.textAlign).toBe(
      'center',
    );
  });

  it('should size the columns only from their widths, whatever the rows shown', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(TableHost);
    fixture.componentInstance.columns[0].width = 40;
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const headers = [...element.querySelectorAll<HTMLElement>('thead th')];

    expect(headers.map((header) => header.style.width)).toEqual(['40%', '']); // the rest shares 60%
    expect(element.querySelector('table')?.classList).toContain('table__grid'); // fixed layout
  });
});

@Component({
  imports: [Table],
  template: `<app-table
    [rows]="rows"
    [total]="25"
    [columns]="columns"
    [pageSize]="10"
    [(page)]="page"
  />`,
})
class PaginatedTableHost {
  readonly page = signal(1);
  readonly rows: Row[] = [{ name: 'Ombrellone', total: 50, hidden: 'x' }];
  readonly columns: TableColumn<Row>[] = [{ field: 'name', header: 'name' }];
}

describe('Table paginator', () => {
  it('should show the range and move the page with the arrows', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(PaginatedTableHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const next = element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!;

    expect(element.querySelector('mat-paginator')).not.toBeNull();
    // Room for a full page, so the paginator does not move on shorter pages.
    expect(
      element.querySelector<HTMLElement>('app-table')!.style.getPropertyValue('--table-page-size'),
    ).toBe('10');

    next.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.page()).toBe(2);

    next.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.page()).toBe(3);
    expect(next.getAttribute('aria-disabled')).toBe('true'); // 25 rows, 10 per page: 3 is the last

    next.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.page()).toBe(3);
  });

  it('should hide the paginator when everything fits in one page', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(TableHost);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('mat-paginator')).toBeNull();
  });
});
