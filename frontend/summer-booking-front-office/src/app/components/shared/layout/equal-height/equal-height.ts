import { Directive, ElementRef, OnDestroy, afterNextRender, inject, input } from '@angular/core';

/**
 * Gives every element matching `appEqualHeight` (e.g. "app-card") inside the host the height
 * of the tallest one, exposed as the `--app-equal-height` custom property:
 *
 *   <main appEqualHeight="app-card">…</main>
 *   app-card { min-height: var(--app-equal-height, 23.75rem); }
 *
 * The value is measured, never hard-coded, and follows resizes, late texts and language changes.
 */
@Directive({ selector: '[appEqualHeight]' })
export class EqualHeight implements OnDestroy {
  readonly selector = input.required<string>({ alias: 'appEqualHeight' });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Frame of the next measure; 0 when none is waiting. */
  private frame = 0;
  private observer?: ResizeObserver;

  constructor() {
    // Any size change of the observed elements (width, texts, fonts) triggers a new measure;
    // it runs in the next frame, so it converges without resize-observer loops.
    afterNextRender(() => {
      this.equalize();
      if (typeof ResizeObserver === 'undefined') {
        return;
      }
      this.observer = new ResizeObserver(() => this.schedule());
      this.elements().forEach((element) => this.observer?.observe(element));
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

  ngOnDestroy(): void {
    this.observer?.disconnect();
    cancelAnimationFrame(this.frame);
  }
}
