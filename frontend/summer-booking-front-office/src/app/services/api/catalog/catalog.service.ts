import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { AutomaticPriceAddItemRequest } from '../../../entities/settings/services/automatic-price-add-item-request';
import { AutomaticPriceItem } from '../../../entities/settings/services/automatic-price-item';
import { AutomaticPriceSelectedGeneralInfo } from '../../../entities/settings/services/automatic-price-selected-general-info';
import { AutomaticPriceSelectedPricesValidity } from '../../../entities/settings/services/automatic-price-selected-prices-validity';
import { AutomaticPriceServices } from '../../../entities/settings/services/automatic-price-services';
import { AutomaticPriceTimeSlot } from '../../../entities/settings/services/automatic-price-time-slot';
import { AutomaticPriceUpdateGeneralRequest } from '../../../entities/settings/services/automatic-price-update-general-request';
import { AutomaticPriceUpdatePricesValidityRequest } from '../../../entities/settings/services/automatic-price-update-prices-validity-request';
import { AutomaticPriceValidityPeriod } from '../../../entities/settings/services/automatic-price-validity-period';
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
import { HourlyServiceAddItemRequest } from '../../../entities/settings/services/hourly-service-add-item-request';
import { HourlyServiceBookingOption } from '../../../entities/settings/services/hourly-service-booking-option';
import { HourlyServiceItem } from '../../../entities/settings/services/hourly-service-item';
import { HourlyServiceSelectedGeneralInfo } from '../../../entities/settings/services/hourly-service-selected-general-info';
import { HourlyServiceSelectedPrices } from '../../../entities/settings/services/hourly-service-selected-prices';
import { HourlyServiceUpdateBookingRequest } from '../../../entities/settings/services/hourly-service-update-booking-request';
import { HourlyServiceUpdateGeneralRequest } from '../../../entities/settings/services/hourly-service-update-general-request';
import { HourlyServiceUpdatePricesRequest } from '../../../entities/settings/services/hourly-service-update-prices-request';
import { ServiceAvailabilityOption } from '../../../entities/settings/services/service-availability-option';
import { ServiceBookingOption } from '../../../entities/settings/services/service-booking-option';
import { ServicePeriodAvailability } from '../../../entities/settings/services/service-period-availability';
import { ServicePeriodPrice } from '../../../entities/settings/services/service-period-price';
import { ServiceTimeSlotPrice } from '../../../entities/settings/services/service-time-slot-price';
import { ServiceTranslation } from '../../../entities/settings/services/service-translation';
import { ServiceVariant } from '../../../entities/settings/services/service-variant';
import { StandardServiceAddItemRequest } from '../../../entities/settings/services/standard-service-add-item-request';
import { StandardServiceItem } from '../../../entities/settings/services/standard-service-item';
import { StandardServiceSelectedGeneralInfo } from '../../../entities/settings/services/standard-service-selected-general-info';
import { StandardServiceSelectedPrices } from '../../../entities/settings/services/standard-service-selected-prices';
import { StandardServiceUpdateBookingRequest } from '../../../entities/settings/services/standard-service-update-booking-request';
import { StandardServiceUpdateGeneralRequest } from '../../../entities/settings/services/standard-service-update-general-request';
import { StandardServiceUpdatePricesRequest } from '../../../entities/settings/services/standard-service-update-prices-request';
import { TableServiceAddItemRequest } from '../../../entities/settings/services/table-service-add-item-request';
import { TableServiceAvailability } from '../../../entities/settings/services/table-service-availability';
import { TableServiceAvailabilityDay } from '../../../entities/settings/services/table-service-availability-day';
import { TableServiceItem } from '../../../entities/settings/services/table-service-item';
import { TableServiceTableGroup } from '../../../entities/settings/services/table-service-table-group';
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

  /** The Info Generali tab of an hourly service, for the edit panel. */
  readonly hourlyServiceGeneralInfo = (
    idHourlyService: string,
  ): Observable<HourlyServiceSelectedGeneralInfo> =>
    this.httpClient.get<HourlyServiceSelectedGeneralInfo>(
      `${this.endpoint}/get-hourly-service-general-info`,
      { params: { idHourlyService } },
    );

  /** The Prezzi tab of an hourly service, for the edit panel. */
  readonly hourlyServicePrices = (
    idHourlyService: string,
  ): Observable<HourlyServiceSelectedPrices> =>
    this.httpClient.get<HourlyServiceSelectedPrices>(`${this.endpoint}/get-hourly-service-prices`, {
      params: { idHourlyService },
    });

  /** The Prenotazioni tab of an hourly service, its booking options, for the edit panel. */
  readonly hourlyServiceBookingOptions = (
    idHourlyService: string,
  ): Observable<HourlyServiceBookingOption[]> =>
    this.httpClient.get<HourlyServiceBookingOption[]>(
      `${this.endpoint}/get-hourly-service-booking-options`,
      { params: { idHourlyService } },
    );

  /** Salva of the Info Generali tab of the edit panel. Answers 204 with no body. */
  readonly updateHourlyServiceGeneral = (
    idProperty: string,
    request: HourlyServiceUpdateGeneralRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-hourly-service-general`, {
      ...onlyFieldsOf(HourlyServiceUpdateGeneralRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      idProperty,
    });

  /** Salva of the Prezzi tab of the edit panel. Answers 204 with no body. */
  readonly updateHourlyServicePrices = (
    idProperty: string,
    request: HourlyServiceUpdatePricesRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-hourly-service-prices`, {
      ...onlyFieldsOf(HourlyServiceUpdatePricesRequest, request),
      periodAvailabilities: request.periodAvailabilities.map(periodAvailabilityFields),
      idProperty,
    });

  /** Salva of the Prenotazioni tab of the edit panel. Answers 204 with no body. */
  readonly updateHourlyServiceBooking = (
    idProperty: string,
    request: HourlyServiceUpdateBookingRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-hourly-service-booking`, {
      ...onlyFieldsOf(HourlyServiceUpdateBookingRequest, request),
      bookingOptions: request.bookingOptions.map(hourlyBookingOptionFields),
      idProperty,
    });

  /** The data of the three tabs of the add panel. Answers 204 with no body. */
  readonly createHourlyService = (
    idProperty: string,
    request: HourlyServiceAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-hourly-service`, {
      ...onlyFieldsOf(HourlyServiceAddItemRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      periodAvailabilities: request.periodAvailabilities.map(periodAvailabilityFields),
      bookingOptions: request.bookingOptions.map(hourlyBookingOptionFields),
      idProperty,
    });

  readonly automaticPriceList = (request: PageRequest): Observable<Page<AutomaticPriceItem>> =>
    this.httpClient.post<Page<AutomaticPriceItem>>(
      `${this.endpoint}/automatic-price-list`,
      request,
    );

  /** The bar, standard and hourly services of the property, for "Validità sui servizi". */
  readonly automaticPriceServices = (idProperty: string): Observable<AutomaticPriceServices> =>
    this.httpClient.get<AutomaticPriceServices>(`${this.endpoint}/get-automatic-price-services`, {
      params: { idProperty },
    });

  /** The data of the two tabs of the add panel. Answers 204 with no body. */
  readonly createAutomaticPrice = (
    idProperty: string,
    request: AutomaticPriceAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-automatic-price`, {
      ...onlyFieldsOf(AutomaticPriceAddItemRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      validityPeriods: request.validityPeriods.map(validityPeriodFields),
      idProperty,
    });

  /** The Info Generali tab of an automatic price, for the edit panel. */
  readonly automaticPriceGeneralInfo = (
    idAutomaticPrice: string,
  ): Observable<AutomaticPriceSelectedGeneralInfo> =>
    this.httpClient.get<AutomaticPriceSelectedGeneralInfo>(
      `${this.endpoint}/get-automatic-price-general-info`,
      { params: { idAutomaticPrice } },
    );

  /** The Prezzi e Validità tab of an automatic price, for the edit panel. */
  readonly automaticPricePricesValidity = (
    idAutomaticPrice: string,
  ): Observable<AutomaticPriceSelectedPricesValidity> =>
    this.httpClient.get<AutomaticPriceSelectedPricesValidity>(
      `${this.endpoint}/get-automatic-price-prices-validity`,
      { params: { idAutomaticPrice } },
    );

  /** Salva of the Info Generali tab of the edit panel. Answers 204 with no body. */
  readonly updateAutomaticPriceGeneral = (
    idProperty: string,
    request: AutomaticPriceUpdateGeneralRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-automatic-price-general`, {
      ...onlyFieldsOf(AutomaticPriceUpdateGeneralRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      idProperty,
    });

  /** Salva of the Prezzi e Validità tab of the edit panel. Answers 204 with no body. */
  readonly updateAutomaticPricePricesValidity = (
    idProperty: string,
    request: AutomaticPriceUpdatePricesValidityRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-automatic-price-prices-validity`, {
      ...onlyFieldsOf(AutomaticPriceUpdatePricesValidityRequest, request),
      validityPeriods: request.validityPeriods.map(validityPeriodFields),
      idProperty,
    });

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

  /** The data of the two tabs of the add panel. Answers 204 with no body. */
  readonly createTableService = (
    idProperty: string,
    request: TableServiceAddItemRequest,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-table-service`, {
      ...onlyFieldsOf(TableServiceAddItemRequest, request),
      translations: request.translations.map((translation) =>
        onlyFieldsOf(ServiceTranslation, translation),
      ),
      availabilities: request.availabilities.map(tableAvailabilityFields),
      tableGroups: request.tableGroups.map((tableGroup) =>
        onlyFieldsOf(TableServiceTableGroup, tableGroup),
      ),
      idProperty,
    });
}

/** Only the fields of the contract, at every level: the time slots inside the option too. */
function availabilityOptionFields(option: ServiceAvailabilityOption): ServiceAvailabilityOption {
  return {
    ...onlyFieldsOf(ServiceAvailabilityOption, option),
    timeSlotPrices: option.timeSlotPrices.map((timeSlotPrice) =>
      onlyFieldsOf(ServiceTimeSlotPrice, timeSlotPrice),
    ),
  };
}

function periodAvailabilityFields(
  periodAvailability: ServicePeriodAvailability,
): ServicePeriodAvailability {
  return {
    ...onlyFieldsOf(ServicePeriodAvailability, periodAvailability),
    availabilityOptions: periodAvailability.availabilityOptions.map(availabilityOptionFields),
  };
}

function validityPeriodFields(
  validityPeriod: AutomaticPriceValidityPeriod,
): AutomaticPriceValidityPeriod {
  return {
    ...onlyFieldsOf(AutomaticPriceValidityPeriod, validityPeriod),
    timeSlots: validityPeriod.timeSlots.map((timeSlot) =>
      onlyFieldsOf(AutomaticPriceTimeSlot, timeSlot),
    ),
  };
}

function hourlyBookingOptionFields(
  bookingOption: HourlyServiceBookingOption,
): HourlyServiceBookingOption {
  return {
    ...onlyFieldsOf(HourlyServiceBookingOption, bookingOption),
    availabilityOption: availabilityOptionFields(bookingOption.availabilityOption),
  };
}

function tableAvailabilityFields(availability: TableServiceAvailability): TableServiceAvailability {
  return {
    ...onlyFieldsOf(TableServiceAvailability, availability),
    days: availability.days.map((day) => onlyFieldsOf(TableServiceAvailabilityDay, day)),
  };
}
