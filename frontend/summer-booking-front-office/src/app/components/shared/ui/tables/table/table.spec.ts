import { Component } from '@angular/core';
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
  template: `<app-table [rows]="rows" [columns]="columns" />`,
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
});
