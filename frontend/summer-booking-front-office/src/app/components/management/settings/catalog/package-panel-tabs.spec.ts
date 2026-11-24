import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { PanelTab, panelTabOf } from './package-panel-tabs';

describe('panelTabOf', () => {
  const invalid = (...fields: string[]): ApiProblem => ({
    status: 400,
    code: ApiErrorCode.ValidationFailed,
    errors: fields.map((field) => ({ field, code: ApiErrorCode.ValidationInvalid })),
  });

  it.each([
    [invalid('vatRate'), PanelTab.PriceList],
    [invalid('reducedSeasonalPrices[0].endsOn'), PanelTab.PriceList],
    [invalid('vatRate', 'translations[0].packageName'), PanelTab.GeneralInfo],
    [{ status: 422, code: ApiErrorCode.CatalogVatRateNotAllowed }, PanelTab.PriceList],
    [{ status: 422, code: ApiErrorCode.PricingCalculationMethodNotFound }, PanelTab.PriceList],
    [{ status: 404, code: ApiErrorCode.CatalogSectorNotFound }, null],
  ])('should tell the tab of the first wrong field', (problem, tab) => {
    expect(panelTabOf(problem)).toBe(tab);
  });
});
