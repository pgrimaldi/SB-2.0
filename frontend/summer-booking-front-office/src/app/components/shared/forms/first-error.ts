import { ElementRef, Injector, afterNextRender } from '@angular/core';

/**
 * Brings into view the first wrong field of a long form, or the first of its own messages (`message`),
 * e.g. when the button that saves is far from it.
 */
export function showFirstError(
  container: ElementRef<HTMLElement> | undefined,
  message: string,
  injector: Injector,
): void {
  afterNextRender(
    () =>
      container?.nativeElement
        .querySelector(`.mat-form-field-invalid, ${message}`)
        ?.scrollIntoView({ block: 'center' }),
    { injector },
  );
}
