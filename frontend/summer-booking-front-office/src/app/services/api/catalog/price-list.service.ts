import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { AdditionalItemAddItemRequest } from '../../../entities/settings/catalog/additional-item-add-item-request';
import { AdditionalItemSelectedItem } from '../../../entities/settings/catalog/additional-item-selected-item';
import { PackageAddItemRequest } from '../../../entities/settings/catalog/package-add-item-request';
import { PackageArticle } from '../../../entities/settings/catalog/package-article';
import { PackageIntermediatePrice } from '../../../entities/settings/catalog/package-intermediate-price';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { PackageItemQuantity } from '../../../entities/settings/catalog/package-item-quantity';
import { PackagePeriodPrice } from '../../../entities/settings/catalog/package-period-price';
import { PackageReducedSeasonalPrice } from '../../../entities/settings/catalog/package-reduced-seasonal-price';
import { PackageSelectedItem } from '../../../entities/settings/catalog/package-selected-item';
import { PackageSectorPrice } from '../../../entities/settings/catalog/package-sector-price';
import { PackageTranslation } from '../../../entities/settings/catalog/package-translation';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly packageList = (request: PageRequest): Observable<Page<PackageItem>> =>
    this.httpClient.post<Page<PackageItem>>(`${this.endpoint}/price-lists/packages-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deletePackageItems = (idPackages: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/price-lists/delete-package-item`, { idPackages });

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicatePackageItems = (idPackages: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/price-lists/duplicate-package-item`, {
      idPackages,
    });

  /** Every package of the property, in its order: `id` is the idPackage, `value` the name. */
  readonly packageCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/package-combobox`, null);

  readonly additionalItemList = (request: PageRequest): Observable<Page<AdditionalItem>> =>
    this.httpClient.post<Page<AdditionalItem>>(
      `${this.endpoint}/additional-services-articles-list`,
      request,
    );

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteAdditionalItems = (idAdditionalItems: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-additional-services-article-item`, {
      idAdditionalItems,
    });

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateAdditionalItems = (idAdditionalItems: string[]): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-additional-services-article-item`, {
      idAdditionalItems,
    });

  /**
   * The whole package of the add panel; the property comes from the access token. Answers 204
   * with no body.
   */
  readonly createPackage = (request: PackageAddItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/price-lists/create-package`, packageBody(request));

  /** The whole form of the add panel of the additional items. Answers 204 with no body. */
  readonly createAdditionalItem = (request: AdditionalItemAddItemRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-additional-services-article-item`, {
      ...additionalItemBody(request),
    });

  /** Everything the edit panel of an additional item shows. */
  readonly selectedAdditionalItem = (
    idAdditionalItem: string,
  ): Observable<AdditionalItemSelectedItem> =>
    this.httpClient.get<AdditionalItemSelectedItem>(
      `${this.endpoint}/get-additional-services-article-item`,
      { params: { idAdditionalItem } },
    );

  /**
   * The whole form, as in `createAdditionalItem`, with its id and the version read. Answers 204 with
   * no body and the new version.
   */
  readonly updateAdditionalItem = (
    idAdditionalItem: string,
    request: AdditionalItemAddItemRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .post<void>(
        `${this.endpoint}/update-additional-services-article-item`,
        { ...additionalItemBody(request), idAdditionalItem },
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));

  /** The articles of the backend, always all, each with its items in the warehouse, if any. */
  readonly packageArticles = (): Observable<PackageArticle[]> =>
    this.httpClient.get<PackageArticle[]>(`${this.endpoint}/price-lists/get-package-articles`);

  /** Everything the edit panel shows; the property comes from the access token. */
  readonly selectedPackage = (idPackage: string): Observable<PackageSelectedItem> =>
    this.httpClient.get<PackageSelectedItem>(`${this.endpoint}/price-lists/get-package`, {
      params: { idPackage },
    });

  /**
   * The whole package, as in `createPackage`, with its id and the version read. Answers 204 with no
   * body and the new version.
   */
  readonly updatePackage = (
    idPackage: string,
    request: PackageAddItemRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .post<void>(
        `${this.endpoint}/price-lists/update-package`,
        { ...packageBody(request), idPackage },
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));
}

/** Only the fields of the package, also inside its lists. */
function packageBody(request: PackageAddItemRequest): PackageAddItemRequest {
  return {
    ...onlyFieldsOf(PackageAddItemRequest, request),
    translations: request.translations.map((translation) =>
      onlyFieldsOf(PackageTranslation, translation),
    ),
    items: request.items.map((item) => onlyFieldsOf(PackageItemQuantity, item)),
    sectorPrices: request.sectorPrices.map((sectorPrice) =>
      onlyFieldsOf(PackageSectorPrice, sectorPrice),
    ),
    periodPrices: request.periodPrices.map((periodPrice) =>
      onlyFieldsOf(PackagePeriodPrice, periodPrice),
    ),
    reducedSeasonalPrices: request.reducedSeasonalPrices.map((price) => ({
      ...onlyFieldsOf(PackageReducedSeasonalPrice, price),
      sectorPrices: price.sectorPrices.map((sectorPrice) =>
        onlyFieldsOf(PackageSectorPrice, sectorPrice),
      ),
    })),
    intermediatePrices: request.intermediatePrices.map((price) =>
      onlyFieldsOf(PackageIntermediatePrice, price),
    ),
  };
}

/** Only the fields of the additional item, also inside its price rows. */
function additionalItemBody(request: AdditionalItemAddItemRequest): AdditionalItemAddItemRequest {
  return {
    ...onlyFieldsOf(AdditionalItemAddItemRequest, request),
    sectorPrices: request.sectorPrices.map((sectorPrice) =>
      onlyFieldsOf(PackageSectorPrice, sectorPrice),
    ),
    periodPrices: request.periodPrices.map((periodPrice) =>
      onlyFieldsOf(PackagePeriodPrice, periodPrice),
    ),
    intermediatePrices: request.intermediatePrices.map((price) =>
      onlyFieldsOf(PackageIntermediatePrice, price),
    ),
  };
}
