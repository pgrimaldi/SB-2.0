import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { AutomaticPriceItem } from '../../../entities/settings/services/automatic-price-item';
import { BarServiceAddItemRequest } from '../../../entities/settings/services/bar-service-add-item-request';
import { BarServiceItem } from '../../../entities/settings/services/bar-service-item';
import { BarServiceSelectedGeneralInfo } from '../../../entities/settings/services/bar-service-selected-general-info';
import { BarServiceSelectedPrices } from '../../../entities/settings/services/bar-service-selected-prices';
import { BarServiceUpdateBookingRequest } from '../../../entities/settings/services/bar-service-update-booking-request';
import { BarServiceUpdateGeneralRequest } from '../../../entities/settings/services/bar-service-update-general-request';
import { BarServiceUpdatePricesRequest } from '../../../entities/settings/services/bar-service-update-prices-request';
import { DeleteBarServiceItemsRequest } from '../../../entities/settings/services/delete-bar-service-items-request';
import { DuplicateBarServiceItemsRequest } from '../../../entities/settings/services/duplicate-bar-service-items-request';
import { DeleteStandardServiceItemsRequest } from '../../../entities/settings/services/delete-standard-service-items-request';
import { DuplicateStandardServiceItemsRequest } from '../../../entities/settings/services/duplicate-standard-service-items-request';
import { DeleteHourlyServiceItemsRequest } from '../../../entities/settings/services/delete-hourly-service-items-request';
import { DuplicateHourlyServiceItemsRequest } from '../../../entities/settings/services/duplicate-hourly-service-items-request';
import { DeleteAutomaticPriceItemsRequest } from '../../../entities/settings/services/delete-automatic-price-items-request';
import { DuplicateAutomaticPriceItemsRequest } from '../../../entities/settings/services/duplicate-automatic-price-items-request';
import { DeleteTableServiceItemsRequest } from '../../../entities/settings/services/delete-table-service-items-request';
import { DuplicateTableServiceItemsRequest } from '../../../entities/settings/services/duplicate-table-service-items-request';
import { HourlyServiceItem } from '../../../entities/settings/services/hourly-service-item';
import { ServiceBookingOption } from '../../../entities/settings/services/service-booking-option';
import { ServicePeriodPrice } from '../../../entities/settings/services/service-period-price';
import { ServiceTranslation } from '../../../entities/settings/services/service-translation';
import { ServiceVariant } from '../../../entities/settings/services/service-variant';
import { StandardServiceAddItemRequest } from '../../../entities/settings/services/standard-service-add-item-request';
import { StandardServiceItem } from '../../../entities/settings/services/standard-service-item';
import { StandardServiceSelectedGeneralInfo } from '../../../entities/settings/services/standard-service-selected-general-info';
import { StandardServiceSelectedPrices } from '../../../entities/settings/services/standard-service-selected-prices';
import { StandardServiceUpdateBookingRequest } from '../../../entities/settings/services/standard-service-update-booking-request';
import { StandardServiceUpdateGeneralRequest } from '../../../entities/settings/services/standard-service-update-general-request';
import { StandardServiceUpdatePricesRequest } from '../../../entities/settings/services/standard-service-update-prices-request';
import { TableServiceItem } from '../../../entities/settings/services/table-service-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  /** Names in the language of the request (Accept-Language); `id` is the ISO 3166-1 alpha-2 code. */
  readonly countriesCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.get<ComboboxItem[]>(`${this.endpoint}/countries-combobox`);

  readonly bathhouseConfigurationGeneral = (
    idProperty: string,
  ): Observable<BathhouseGeneralInformation> =>
    this.httpClient.get<BathhouseGeneralInformation>(
      `${this.endpoint}/bathhouse-configuration-general`,
      { params: { idProperty } },
    );

  /**
   * Every service of the catalogue, with the name in the language of the request (Accept-Language)
   * and whether the property offers it.
   */
  readonly bathhouseConfigurationServices = (
    idProperty: string,
  ): Observable<BathhouseServiceItem[]> =>
    this.httpClient.get<BathhouseServiceItem[]>(
      `${this.endpoint}/bathhouse-configuration-services`,
      { params: { idProperty } },
    );

  readonly bathhouseConfigurationMedia = (idProperty: string): Observable<BathhouseMedia> =>
    this.httpClient.get<BathhouseMedia>(`${this.endpoint}/bathhouse-configuration-media`, {
      params: { idProperty },
    });

  /** Every field of the tab, changed or not. Answers 204 with no body. */
  readonly saveBathhouseConfigurationGeneral = (
    idProperty: string,
    information: BathhouseGeneralInformation,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-bathhouse-configuration-general`, {
      ...onlyFieldsOf(BathhouseGeneralInformation, information),
      idProperty,
    });

  /** Only the services the property offers. Answers 204 with no body. */
  readonly saveBathhouseConfigurationServices = (
    idProperty: string,
    services: readonly BathhouseServiceItem[],
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-bathhouse-configuration-services`, {
      services: services.map((service) => onlyFieldsOf(BathhouseServiceItem, service)),
      idProperty,
    });

  readonly barServiceList = (request: PageRequest): Observable<Page<BarServiceItem>> =>
    this.httpClient.post<Page<BarServiceItem>>(`${this.endpoint}/bar-service-list`, request);

  /** The Info Generali tab of a bar service, for the edit panel. */
  readonly barServiceGeneralInfo = (
    idBarService: string,
  ): Observable<BarServiceSelectedGeneralInfo> =>
    this.httpClient.get<BarServiceSelectedGeneralInfo>(
      `${this.endpoint}/get-bar-service-general-info`,
      { params: { idBarService } },
    );

  /** The Prezzi tab of a bar service, for the edit panel. */
  readonly barServicePrices = (idBarService: string): Observable<BarServiceSelectedPrices> =>
    this.httpClient.get<BarServiceSelectedPrices>(`${this.endpoint}/get-bar-service-prices`, {
      params: { idBarService },
    });

  /** The Prenotazioni tab of a bar service, its booking options, for the edit panel. */
  readonly barServiceBookingOptions = (idBarService: string): Observable<ServiceBookingOption[]> =>
    this.httpClient.get<ServiceBookingOption[]>(
      `${this.endpoint}/get-bar-service-booking-options`,
      { params: { idBarService } },
    );

  /** Salva of the Info Generali tab of the edit panel. Answers 204 with no body. */
  readonly updateBarServiceGeneral = (
    idProperty: string,
    request: BarServiceUpdateGeneralRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-bar-service-general`, {
      ...onlyFieldsOf(BarServiceUpdateGeneralRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      idProperty,
    });

  /** Salva of the Prezzi tab of the edit panel. Answers 204 with no body. */
  readonly updateBarServicePrices = (
    idProperty: string,
    request: BarServiceUpdatePricesRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-bar-service-prices`, {
      ...onlyFieldsOf(BarServiceUpdatePricesRequest, request),
      variants: request.variants.map((variant) => onlyFieldsOf(ServiceVariant, variant)),
      periodPrices: request.periodPrices.map((periodPrice) =>
        onlyFieldsOf(ServicePeriodPrice, periodPrice),
      ),
      idProperty,
    });

  /** Salva of the Prenotazioni tab of the edit panel. Answers 204 with no body. */
  readonly updateBarServiceBooking = (
    idProperty: string,
    request: BarServiceUpdateBookingRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-bar-service-booking`, {
      ...onlyFieldsOf(BarServiceUpdateBookingRequest, request),
      bookingOptions: request.bookingOptions.map((bookingOption) =>
        onlyFieldsOf(ServiceBookingOption, bookingOption),
      ),
      idProperty,
    });

  /** The data of the three tabs of the add panel. Answers 204 with no body. */
  readonly createBarService = (
    idProperty: string,
    request: BarServiceAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-bar-service`, {
      ...onlyFieldsOf(BarServiceAddItemRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      variants: request.variants.map((variant) => onlyFieldsOf(ServiceVariant, variant)),
      periodPrices: request.periodPrices.map((periodPrice) =>
        onlyFieldsOf(ServicePeriodPrice, periodPrice),
      ),
      bookingOptions: request.bookingOptions.map((bookingOption) =>
        onlyFieldsOf(ServiceBookingOption, bookingOption),
      ),
      idProperty,
    });

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteBarServiceItems = (request: DeleteBarServiceItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-bar-service-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateBarServiceItems = (
    request: DuplicateBarServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-bar-service-item`, request);

  readonly standardServiceList = (request: PageRequest): Observable<Page<StandardServiceItem>> =>
    this.httpClient.post<Page<StandardServiceItem>>(
      `${this.endpoint}/standard-service-list`,
      request,
    );

  /** The Info Generali tab of a service, for the edit panel. */
  readonly standardServiceGeneralInfo = (
    idStandardService: string,
  ): Observable<StandardServiceSelectedGeneralInfo> =>
    this.httpClient.get<StandardServiceSelectedGeneralInfo>(
      `${this.endpoint}/get-standard-service-general-info`,
      { params: { idStandardService } },
    );

  /** The Prezzi tab of a service, for the edit panel. */
  readonly standardServicePrices = (
    idStandardService: string,
  ): Observable<StandardServiceSelectedPrices> =>
    this.httpClient.get<StandardServiceSelectedPrices>(
      `${this.endpoint}/get-standard-service-prices`,
      { params: { idStandardService } },
    );

  /** The Prenotazioni tab of a service, its booking options, for the edit panel. */
  readonly standardServiceBookingOptions = (
    idStandardService: string,
  ): Observable<ServiceBookingOption[]> =>
    this.httpClient.get<ServiceBookingOption[]>(
      `${this.endpoint}/get-standard-service-booking-options`,
      { params: { idStandardService } },
    );

  /** Salva of the Info Generali tab of the edit panel. Answers 204 with no body. */
  readonly updateStandardServiceGeneral = (
    idProperty: string,
    request: StandardServiceUpdateGeneralRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-standard-service-general`, {
      ...onlyFieldsOf(StandardServiceUpdateGeneralRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      idProperty,
    });

  /** Salva of the Prezzi tab of the edit panel. Answers 204 with no body. */
  readonly updateStandardServicePrices = (
    idProperty: string,
    request: StandardServiceUpdatePricesRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-standard-service-prices`, {
      ...onlyFieldsOf(StandardServiceUpdatePricesRequest, request),
      periodPrices: request.periodPrices.map((periodPrice) =>
        onlyFieldsOf(ServicePeriodPrice, periodPrice),
      ),
      idProperty,
    });

  /** Salva of the Prenotazioni tab of the edit panel. Answers 204 with no body. */
  readonly updateStandardServiceBooking = (
    idProperty: string,
    request: StandardServiceUpdateBookingRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-standard-service-booking`, {
      ...onlyFieldsOf(StandardServiceUpdateBookingRequest, request),
      bookingOptions: request.bookingOptions.map((bookingOption) =>
        onlyFieldsOf(ServiceBookingOption, bookingOption),
      ),
      idProperty,
    });

  /** The data of the three tabs of the add panel. Answers 204 with no body. */
  readonly createStandardService = (
    idProperty: string,
    request: StandardServiceAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-standard-service`, {
      ...onlyFieldsOf(StandardServiceAddItemRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      periodPrices: request.periodPrices.map((periodPrice) =>
        onlyFieldsOf(ServicePeriodPrice, periodPrice),
      ),
      bookingOptions: request.bookingOptions.map((bookingOption) =>
        onlyFieldsOf(ServiceBookingOption, bookingOption),
      ),
      idProperty,
    });

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteStandardServiceItems = (
    request: DeleteStandardServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-standard-service-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateStandardServiceItems = (
    request: DuplicateStandardServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-standard-service-item`, request);

  /**
   * An arrow property on purpose: the page hands it to `app-table` as its loader
   * (`[load]="catalog.hourlyServiceList"`), and only an arrow function keeps `this` when it is
   * passed around on its own.
   */
  readonly hourlyServiceList = (request: PageRequest): Observable<Page<HourlyServiceItem>> =>
    this.httpClient.post<Page<HourlyServiceItem>>(`${this.endpoint}/hourly-service-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteHourlyServiceItems = (
    request: DeleteHourlyServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-hourly-service-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateHourlyServiceItems = (
    request: DuplicateHourlyServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-hourly-service-item`, request);

  readonly automaticPriceList = (request: PageRequest): Observable<Page<AutomaticPriceItem>> =>
    this.httpClient.post<Page<AutomaticPriceItem>>(
      `${this.endpoint}/automatic-price-list`,
      request,
    );

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteAutomaticPriceItems = (
    request: DeleteAutomaticPriceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-automatic-price-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateAutomaticPriceItems = (
    request: DuplicateAutomaticPriceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-automatic-price-item`, request);

  /**
   * An arrow property on purpose: the page hands it to `app-table` as its loader
   * (`[load]="catalog.tableServiceList"`), and only an arrow function keeps `this` when it is
   * passed around on its own.
   */
  readonly tableServiceList = (request: PageRequest): Observable<Page<TableServiceItem>> =>
    this.httpClient.post<Page<TableServiceItem>>(`${this.endpoint}/table-service-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deleteTableServiceItems = (request: DeleteTableServiceItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-table-service-item`, request);

  /** All or nothing, like the delete. The backend names the copies. Answers 204 with no body. */
  readonly duplicateTableServiceItems = (
    request: DuplicateTableServiceItemsRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/duplicate-table-service-item`, request);
}
