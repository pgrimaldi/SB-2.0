import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { PackageAddItemRequest } from '../../../../entities/settings/catalog/package-add-item-request';

type PackageField = keyof PackageAddItemRequest;

export enum PanelTab {
  GeneralInfo,
  PriceList,
}

const TAB_FIELDS: Readonly<Record<PanelTab, readonly PackageField[]>> = {
  [PanelTab.GeneralInfo]: [
    'translations',
    'maxAdditionalItemQuantity',
    'maxPeopleQuantity',
    'items',
    'includedServices',
    'idSectorAreas',
    'idSectors',
    'isDefault',
    'isDefaultOnline',
    'isOnlineEnabled',
    'isOnlineOnly',
  ],
  [PanelTab.PriceList]: [
    'reducedSeasonalPrices',
    'sectorPrices',
    'periodPrices',
    'vatRate',
    'calculationMethod',
    'isCumulative',
    'monthlySetting',
    'weekendSetting',
    'isPublicHolidaysIncluded',
    'publicHolidayDates',
    'isIntermediatePricesIncluded',
    'intermediatePrices',
  ],
};

/** The codes of the backend that come without `errors[]` and are about one field of the form. */
const FIELD_OF_CODE: Readonly<Partial<Record<string, PackageField>>> = {
  [ApiErrorCode.CatalogVatRateNotAllowed]: 'vatRate',
  [ApiErrorCode.PricingCalculationMethodNotFound]: 'calculationMethod',
};

/** The tab of the first wrong field, where its error shows; null when no field is wrong. */
export function panelTabOf(problem: ApiProblem): PanelTab | null {
  const fieldOfCode = FIELD_OF_CODE[problem.code];
  const fields: readonly string[] = fieldOfCode
    ? [fieldOfCode]
    : (problem.errors ?? []).map(({ field }) => field.split(/[.[]/, 1)[0]);
  return (
    [PanelTab.GeneralInfo, PanelTab.PriceList].find((tab) =>
      TAB_FIELDS[tab].some((field) => fields.includes(field)),
    ) ?? null
  );
}
