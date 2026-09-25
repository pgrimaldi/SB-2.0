import { DestroyRef, Directive, ElementRef, afterNextRender, inject, input } from '@angular/core';

/**
 * Gives every element matching `appEqualHeight` (e.g. "app-card-hub") inside the host the height
 * of the tallest one, exposed as the `--app-equal-height` custom property:
 *
 *   <main appEqualHeight="app-card-hub">…</main>
 *   app-card-hub { min-height: var(--app-equal-height, 23.75rem); }
 *
 * The value is measured, never hard-coded, and follows resizes, late texts and language changes.
 */
@Directive({ selector: '[appEqualHeight]' })
export class EqualHeight {
  readonly selector = input.required<string>({ alias: 'appEqualHeight' });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private frame = 0;

  constructor() {
    const destroyRef = inject(DestroyRef);

    // Any size change of the observed elements (width, texts, fonts) triggers a new measure;
    // it runs in the next frame, so it converges without resize-observer loops.
    afterNextRender(() => {
      this.equalize();
      if (typeof ResizeObserver === 'undefined') {
        return;
      }

      const observer = new ResizeObserver(() => this.schedule());
      this.elements().forEach((element) => observer.observe(element));
      destroyRef.onDestroy(() => {
        observer.disconnect();
        cancelAnimationFrame(this.frame);
      });
    });
  }

  private schedule(): void {
    if (!this.frame) {
      this.frame = requestAnimationFrame(() => {
        this.frame = 0;
        this.equalize();
      });
    }
  }

  private equalize(): void {
    const host = this.host.nativeElement;
    // Measure natural heights first (each element falls back to its own minimum).
    host.style.removeProperty('--app-equal-height');
    const heights = this.elements().map((element) => element.getBoundingClientRect().height);
    if (heights.length) {
      host.style.setProperty('--app-equal-height', `${Math.max(...heights)}px`);
    }
  }

  private elements(): HTMLElement[] {
    return [...this.host.nativeElement.querySelectorAll<HTMLElement>(this.selector())];
  }
}
