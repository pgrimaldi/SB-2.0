import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Table, TableColumn } from '../../components/shared/ui/tables/table/table';
import { WarehouseItem } from '../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../services/api/warehouse/warehouse.service';

/** Warehouse of the property: articles with total and today's available quantity. */
@Component({
  selector: 'app-warehouse',
  imports: [Table],
  templateUrl: './warehouse.html',
  styleUrl: './warehouse.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Warehouse {
  protected readonly items = toSignal(inject(WarehouseService).getAll(), { initialValue: [] });

  protected readonly columns: readonly TableColumn<WarehouseItem>[] = [
    { field: 'name', header: 'management.warehouse.table.name' },
    { field: 'total', header: 'management.warehouse.table.total', align: 'center' },
    { field: 'available', header: 'management.warehouse.table.available', align: 'center' },
  ];
}
