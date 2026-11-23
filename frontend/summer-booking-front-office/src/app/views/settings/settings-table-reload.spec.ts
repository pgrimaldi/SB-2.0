import { Type, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthBehaviour } from '../../behaviours/auth/auth.behaviour';
import { DateLanguageBehaviour } from '../../behaviours/i18n/date-language.behaviour';
import { TableRowId } from '../../components/shared/ui/tables/table/table';
import { ApiErrorCode } from '../../entities/errors/api-error-codes';
import { ApiProblem } from '../../entities/errors/api-problem';
import { CatalogService } from '../../services/api/catalog/catalog.service';
import { PriceListService } from '../../services/api/catalog/price-list.service';
import { AdditionalItems } from './catalog/additional-items/additional-items';
import { Packages } from './catalog/packages/packages';
import { AutomaticPrices } from './services/automatic-prices/automatic-prices';
import { ServicesBar } from './services/services-bar/services-bar';
import { ServicesGeneral } from './services/services-general/services-general';
import { ServicesHourly } from './services/services-hourly/services-hourly';
import { TableBooking } from './services/table-booking/table-booking';

interface TableView {
  reloadIfGone(problem: ApiProblem, table: { reload(): void }, ids: readonly string[]): void;
  reloadIfChanged(problem: ApiProblem, table: { reload(): void }): void;
}

const VIEWS: readonly (readonly [Type<unknown>, string, ApiErrorCode])[] = [
  [ServicesBar, 'idSelectedBarServices', ApiErrorCode.ServiceNotFound],
  [ServicesGeneral, 'idSelectedStandardServices', ApiErrorCode.ServiceNotFound],
  [ServicesHourly, 'idSelectedHourlyServices', ApiErrorCode.ServiceNotFound],
  [AutomaticPrices, 'idSelectedAutomaticPrices', ApiErrorCode.ServiceAutomaticPriceNotFound],
  [TableBooking, 'idSelectedTableServices', ApiErrorCode.ServiceTableServiceNotFound],
  [Packages, 'idSelectedPackages', ApiErrorCode.PricingPriceListNotFound],
  [AdditionalItems, 'idSelectedAdditionalItems', ApiErrorCode.PricingPriceListNotFound],
];

describe('tables of the settings', () => {
  const setup = (viewType: Type<unknown>, selectionField: string) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        { provide: CatalogService, useValue: {} },
        { provide: PriceListService, useValue: {} },
        { provide: DateLanguageBehaviour, useValue: { locale: signal('it-IT') } },
      ],
    });
    const view = TestBed.runInInjectionContext(() => new viewType()) as TableView;
    const fields = view as unknown as Record<string, WritableSignal<readonly TableRowId[]>>;
    const selection = fields[selectionField];
    selection.set(['a', 'b']);
    return { view, selection, table: { reload: vi.fn() } };
  };

  it.each(VIEWS)(
    '%o should reload and forget the rows no longer there, only with their code',
    (viewType, selectionField, code) => {
      const { view, selection, table } = setup(viewType, selectionField);

      view.reloadIfGone({ status: 422, code: ApiErrorCode.ValidationFailed }, table, ['a']);
      expect(table.reload).not.toHaveBeenCalled();
      expect(selection()).toEqual(['a', 'b']);
      view.reloadIfGone({ status: 404, code }, table, ['a']);
      expect(table.reload).toHaveBeenCalledOnce();
      expect(selection()).toEqual(['b']);
    },
  );

  it.each(VIEWS)(
    '%o should reload after a version conflict, keeping the selection',
    (viewType, selectionField, code) => {
      const { view, selection, table } = setup(viewType, selectionField);

      view.reloadIfChanged({ status: 404, code }, table);
      expect(table.reload).not.toHaveBeenCalled();
      view.reloadIfChanged({ status: 409, code: ApiErrorCode.ResourceVersionConflict }, table);
      expect(table.reload).toHaveBeenCalledOnce();
      expect(selection()).toEqual(['a', 'b']);
    },
  );
});
