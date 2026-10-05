import { Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { TableRowAction } from '../ui/tables/table/table';

/** Group `table.actions` of the translations: the names of the standard row buttons. */
interface StandardRowActionLabels {
  delete: string;
  duplicate: string;
  edit: string;
}

/** What the standard row buttons do, with the row they are on; a missing one is not shown. */
export interface StandardRowActionHandlers<T> {
  delete?: (row: T) => void;
  duplicate?: (row: T) => void;
  edit?: (row: T) => void;
}

/** The standard buttons, always in this order, with the app's icons. */
const STANDARD_ROW_ACTIONS = [
  { name: 'delete', icon: '/assets/images/delete.svg' },
  { name: 'duplicate', icon: '/assets/images/duplicate.svg' },
  { name: 'edit', icon: '/assets/images/edit.svg' },
] as const;

/**
 * The standard buttons of the table rows, ready for `[rowActions]`: delete, duplicate and edit, with
 * the app's icons and the names of `table.actions` (they follow the language). A page passes only
 * the functions of the buttons it wants: `standardRowActions({ edit: (item) => … })` gives the edit
 * button alone. Part of the app, not of the component library: it knows our translations and images
 * (images: the table must use `pathIcon`).
 * Call it where `inject` works, e.g. in a field: `protected readonly rowActions = standardRowActions<Item>({ … });`.
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
