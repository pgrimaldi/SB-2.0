import { Directive, TemplateRef, inject } from '@angular/core';

/** Marks each slide of an `app-carousel`: `<app-card-hub *appCarouselSlide />`. */
@Directive({ selector: '[appCarouselSlide]' })
export class CarouselSlide {
  readonly template = inject(TemplateRef);
}
