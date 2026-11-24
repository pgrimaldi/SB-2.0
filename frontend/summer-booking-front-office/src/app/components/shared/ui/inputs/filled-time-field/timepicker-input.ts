import { Directive, forwardRef } from '@angular/core';
import { MAT_INPUT_VALUE_ACCESSOR } from '@angular/material/input';
import { MatTimepickerInput } from '@angular/material/timepicker';

/** Material empties the time with Esc, which also closes the popup around the field. */
@Directive({
  selector: 'input[appTimepickerInput]',
  providers: [
    { provide: MAT_INPUT_VALUE_ACCESSOR, useExisting: forwardRef(() => TimepickerInput) },
  ],
})
export class TimepickerInput extends MatTimepickerInput<Date> {
  protected override _handleKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape') {
      super._handleKeydown(event);
    }
  }
}
