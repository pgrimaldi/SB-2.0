import { Injectable, Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';

/**
 * Message of the first error of a form field, from the generic translations `invalidate.<kind>`
 * (e.g. `invalidate.greater_than`): the error's own values and the translated field name fill the
 * placeholders. Nothing until the user has changed or left the field.
 */
/** Properties of an error that are not values for the message. */
const NOT_VALUES = new Set(['kind', 'message', 'fieldTree', 'formField']);

@Injectable({ providedIn: 'root' })
export class ValidationTextBehaviour {
  private readonly translate = inject(TranslateService);
  private readonly language = toSignal(this.translate.onLangChange, { initialValue: null });

  message(field: FieldTree<unknown>, fieldNameKey: string): Signal<string | null> {
    return computed(() => {
      const state = field();
      const [error] = state.errors();
      if (!error || !(state.dirty() || state.touched())) {
        return null;
      }
      this.language();
      const values = Object.fromEntries(
        Object.entries(error).filter(([property]) => !NOT_VALUES.has(property)),
      );
      return this.translate.instant(`invalidate.${error.kind}`, {
        ...values,
        field: this.translate.instant(fieldNameKey),
      }) as string;
    });
  }
}
