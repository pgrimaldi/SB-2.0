import { Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { TableRowAction } from '../ui/tables/table/table';

/** Translation group `table.actions`. */
interface StandardRowActionLabels {
  delete: string;
  duplicate: string;
  edit: string;
}

/** A missing handler hides its button. */
export interface StandardRowActionHandlers<T> {
  delete?: (row: T) => void;
  duplicate?: (row: T) => void;
  edit?: (row: T) => void;
}

/** Always shown in this order. */
const STANDARD_ROW_ACTIONS = [
  { name: 'delete', icon: '/assets/images/delete.svg' },
  { name: 'duplicate', icon: '/assets/images/duplicate.svg' },
  { name: 'edit', icon: '/assets/images/edit.svg' },
] as const;

/**
 * A page passes only the handlers of the buttons it wants. Part of the app, not of the component
 * library: it knows our translations and images (the table must use `pathIcon`).
 * Call it in an injection context, e.g. a field: `protected readonly rowActions = standardRowActions<Item>({ … });`.
 */
export function standardRowActions<T>(
  handlers: StandardRowActionHandlers<T>,
): Signal<readonly TableRowAction<T>[]> {
  const labels = toSignal(
    inject(TranslateService).stream('table.actions') as Observable<StandardRowActionLabels>,
    { initialValue: { delete: '', duplicate: '', edit: '' } },
  );
  return computed(() =>
    STANDARD_ROW_ACTIONS.flatMap(({ name, icon }) => {
      const action = handlers[name];
      return action ? [{ label: labels()[name], icon, action }] : [];
    }),
  );
}
