import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({ selector: '[appCarouselSlide]' })
export class CarouselSlide {
  readonly template = inject(TemplateRef);
}
