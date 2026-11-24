import { ElementRef, Injector, afterNextRender } from '@angular/core';

/** The wrappers with a wrong value, or with an error of their own. */
const WRONG_FIELDS =
  '.mat-form-field-invalid, .toggle__error, .checkbox__error, .rich__text__field__error';

/**
 * Brings into view the first wrong field of a long form, or the first of its own messages (`message`),
 * e.g. when the button that saves is far from it.
 */
export function showFirstError(
  container: ElementRef<HTMLElement> | undefined,
  message: string,
  injector: Injector,
): void {
  showMessage(container, `${WRONG_FIELDS}, ${message}`, injector);
}

/** Brings into view the first `message` of a long form, e.g. an error that is about no field. */
export function showMessage(
  container: ElementRef<HTMLElement> | undefined,
  message: string,
  injector: Injector,
): void {
  afterNextRender(
    () => container?.nativeElement.querySelector(message)?.scrollIntoView({ block: 'center' }),
    { injector },
  );
}
