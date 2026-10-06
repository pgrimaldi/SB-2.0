import { Injectable, Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, ValidationError } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { ErrorTextBehaviour } from '../errors/error-text.behaviour';
import { ApiValidationError } from './api-field-errors';

/** Properties of an error that are not values for the message. */
const NOT_VALUES = new Set(['kind', 'message', 'fieldTree', 'formField']);

/**
 * Message of the first error of a form field, from the generic translations `invalidate.<kind>`
 * (e.g. `invalidate.greater_than`): the error's own values and, when given, the translated field
 * name fill the placeholders. An error of the backend on the field (`apiFieldErrors`) uses
 * the text of `ErrorTextBehaviour`. Nothing until the user has changed or
 * left the field.
 */
@Injectable({ providedIn: 'root' })
export class ValidationTextBehaviour {
  private readonly translate = inject(TranslateService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly language = toSignal(this.translate.onLangChange, { initialValue: null });

  message(field: FieldTree<unknown>, fieldNameKey?: string): Signal<string | null> {
    return computed(() => {
      const state = field();
      const [error] = state.errors();
      if (!error || !(state.dirty() || state.touched())) {
        return null;
      }
      this.language();
      if (isApiError(error)) {
        return this.errorText.instant(error.code, error.args);
      }
      const values = Object.fromEntries(
        Object.entries(error).filter(([property]) => !NOT_VALUES.has(property)),
      );
      return this.translate.instant(`invalidate.${error.kind}`, {
        ...values,
        ...(fieldNameKey ? { field: this.translate.instant(fieldNameKey) } : {}),
      }) as string;
    });
  }
}

function isApiError(error: ValidationError): error is ApiValidationError {
  return error.kind === 'api';
}
