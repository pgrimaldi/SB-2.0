import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { AdditionalItemAddItemRequest } from '../../../entities/settings/catalog/additional-item-add-item-request';
import { DeleteAdditionalItemsRequest } from '../../../entities/settings/catalog/delete-additional-items-request';
import { DeletePackageItemsRequest } from '../../../entities/settings/catalog/delete-package-items-request';
import { DuplicateAdditionalItemsRequest } from '../../../entities/settings/catalog/duplicate-additional-items-request';
import { DuplicatePackageItemsRequest } from '../../../entities/settings/catalog/duplicate-package-items-request';
import { PackageAddItemRequest } from '../../../entities/settings/catalog/package-add-item-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { PackagePeriodPrice } from '../../../entities/settings/catalog/package-period-price';
import { PackageReducedSeasonalPrice } from '../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSelectedItem } from '../../../entities/settings/catalog/package-selected-item';
import { PackageSectorPrice } from '../../../entities/settings/catalog/package-sector-price';
import { PackageTranslation } from '../../../entities/settings/catalog/package-translation';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly packageList = (request: PageRequest): Observable<Page<PackageItem>> =>
    this.httpClient.post<Page<PackageItem>>(`${this.endpoint}/packages-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deletePackageItems = (request: DeletePackageItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-package-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicatePackageItems = (request: DuplicatePackageItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-package-item`, request);

  /** Every package of the property, in its order: `id` is the idPackage, `value` the name. */
  readonly packageCombobox = (idProperty: string): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/package-combobox`, { idProperty });

  readonly additionalItemList = (request: PageRequest): Observable<Page<AdditionalItem>> =>
    this.httpClient.post<Page<AdditionalItem>>(
      `${this.endpoint}/additional-services-articles-list`,
      request,
    );

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteAdditionalItems = (request: DeleteAdditionalItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-additional-services-article-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateAdditionalItems = (
    request: DuplicateAdditionalItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(
      `${this.endpoint}/duplicate-additional-services-article-item`,
      request,
    );

  /**
   * The whole package of the add panel; the property comes from the access token. Answers 204
   * with no body.
   */
  readonly createPackage = (request: PackageAddItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/packages/create-package`, packageBody(request));

  /** The whole form of the add panel of the additional items. Answers 204 with no body. */
  readonly createAdditionalItem = (
    idProperty: string,
    request: AdditionalItemAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-additional-services-article-item`, {
      ...onlyFieldsOf(AdditionalItemAddItemRequest, request),
      sectorPrices: request.sectorPrices.map((sectorPrice) =>
        onlyFieldsOf(PackageSectorPrice, sectorPrice),
      ),
      periodPrices: request.periodPrices.map((periodPrice) =>
        onlyFieldsOf(PackagePeriodPrice, periodPrice),
      ),
      idProperty,
    });

  /** Everything the edit panel shows; the property comes from the access token. */
  readonly selectedPackage = (idPackage: string): Observable<PackageSelectedItem> =>
    this.httpClient.get<PackageSelectedItem>(
      `${this.endpoint}/packages/${encodeURIComponent(idPackage)}`,
    );

  /** The whole package, as in `createPackage`, with its id. Answers 204 with no body. */
  readonly updatePackage = (idPackage: string, request: PackageAddItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/packages/update-package`, {
      ...packageBody(request),
      idPackage,
    });
}

/** Only the fields of the package, also inside its lists. */
function packageBody(request: PackageAddItemRequest): PackageAddItemRequest {
  return {
    ...onlyFieldsOf(PackageAddItemRequest, request),
    translations: request.translations.map((translation) =>
      onlyFieldsOf(PackageTranslation, translation),
    ),
    sectorPrices: request.sectorPrices.map((sectorPrice) =>
      onlyFieldsOf(PackageSectorPrice, sectorPrice),
    ),
    periodPrices: request.periodPrices.map((periodPrice) =>
      onlyFieldsOf(PackagePeriodPrice, periodPrice),
    ),
    reducedSeasonalPrice:
      request.reducedSeasonalPrice &&
      onlyFieldsOf(PackageReducedSeasonalPrice, request.reducedSeasonalPrice),
  };
}
