import { Directive, TemplateRef, inject } from '@angular/core';
import { SeasonPeriodItem } from '../../../../../../entities/settings/seasons/season-period-item';

export interface ServicesPeriodContentContext {
  $implicit: SeasonPeriodItem;
}

/** What a period shows under its days instead of the price: `<ng-template appServicesPeriodContent let-period>`. */
@Directive({ selector: 'ng-template[appServicesPeriodContent]' })
export class ServicesPeriodContent {
  readonly template = inject<TemplateRef<ServicesPeriodContentContext>>(TemplateRef);

  static ngTemplateContextGuard(
    _directive: ServicesPeriodContent,
    _context: unknown,
  ): _context is ServicesPeriodContentContext {
    return true;
  }
}
