import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { entriesListMock, userComboboxMock } from './audit/audit.mock';
import { loginMock, logoutMock, refreshMock } from './auth/auth.mock';
import { onlineServicesOptionsMock, updateOnlineServicesOptionsMock } from './booking/booking.mock';
import {
  automaticPriceListMock,
  automaticPriceGeneralInfoMock,
  automaticPricePricesValidityMock,
  automaticPriceServicesMock,
  updateAutomaticPriceGeneralMock,
  updateAutomaticPricePricesValidityMock,
  createAutomaticPriceMock,
  barServiceBookingOptionsMock,
  barServiceGeneralInfoMock,
  barServiceListMock,
  barServicePricesMock,
  createBarServiceMock,
  updateBarServiceBookingMock,
  updateBarServiceGeneralMock,
  updateBarServicePricesMock,
  createStandardServiceMock,
  standardServiceBookingOptionsMock,
  standardServiceGeneralInfoMock,
  standardServicePricesMock,
  updateStandardServiceBookingMock,
  updateStandardServiceGeneralMock,
  updateStandardServicePricesMock,
  bathhouseConfigurationGeneralMock,
  bathhouseConfigurationMediaMock,
  bathhouseConfigurationServicesMock,
  countriesComboboxMock,
  deleteBarServiceItemsMock,
  deleteStandardServiceItemsMock,
  duplicateStandardServiceItemsMock,
  createHourlyServiceMock,
  hourlyServiceBookingOptionsMock,
  hourlyServiceGeneralInfoMock,
  hourlyServicePricesMock,
  updateHourlyServiceBookingMock,
  updateHourlyServiceGeneralMock,
  updateHourlyServicePricesMock,
  deleteHourlyServiceItemsMock,
  duplicateHourlyServiceItemsMock,
  deleteAutomaticPriceItemsMock,
  duplicateAutomaticPriceItemsMock,
  deleteTableServiceItemsMock,
  duplicateTableServiceItemsMock,
  createTableServiceMock,
  tableServiceAvailabilityBookingsMock,
  tableServiceGeneralInfoMock,
  updateTableServiceAvailabilityBookingsMock,
  updateTableServiceGeneralMock,
  duplicateBarServiceItemsMock,
  hourlyServiceListMock,
  saveBathhouseConfigurationGeneralMock,
  saveBathhouseConfigurationServicesMock,
  standardServiceListMock,
  tableServiceListMock,
} from './catalog/catalog.mock';
import {
  mapSettingsMock,
  removeMapBackgroundMock,
  resetMapMock,
  sectorAreaComboboxMock,
  updateMapBackgroundMock,
  updateMapConfigurationMock,
  uploadMapBackgroundMock,
} from './catalog/map.mock';
import {
  addSectorItemMock,
  deleteSectorItemsMock,
  duplicateSectorItemsMock,
  sectorComboboxMock,
  sectorListMock,
  updateSectorItemMock,
} from './catalog/sectors.mock';
import {
  createPackageMock,
  deletePackageItemsMock,
  duplicatePackageItemsMock,
  packageListMock,
  selectedPackageMock,
  updatePackageMock,
} from './catalog/price-lists.mock';
import {
  affiliatedHotelComboboxMock,
  deleteSelectedItemMock,
  emptyTrashMock,
  existingBookingListMock,
  moveToTrashMock,
  restoreBookingItemMock,
  trashItemListMock,
} from './database/database.mock';
import {
  createUserMock,
  deleteUserMock,
  selectedUserMock,
  updateUserMock,
  usersListMock,
} from './identity/identity.mock';
import {
  addSeasonIntervalMock,
  addSeasonPeriodMock,
  deleteSeasonIntervalsMock,
  deleteSeasonPeriodMock,
  saveSeasonDatesMock,
  seasonDatesMock,
  seasonPeriodListMock,
  updateSeasonIntervalMock,
  updateSeasonPeriodMock,
} from './seasons/seasons.mock';
import { saveSharingInformationMock, sharingGeneralInformationMock } from './sharing/sharing.mock';
import {
  addVoucherItemMock,
  deleteVoucherItemsMock,
  duplicateVoucherItemsMock,
  selectedVoucherItemMock,
  updateVoucherItemMock,
  voucherListMock,
} from './vouchers/vouchers.mock';
import {
  contactSupportMock,
  emailConfigurationMock,
  saveEmailConfigurationMock,
  sendTestEmailMock,
  stripeInformationMock,
  supportInfoMock,
} from './system/system.mock';
import {
  addPrinterMock,
  deletePrinterMock,
  downloadGuideElectronicReceiptMock,
  electronicReceiptActivationMock,
  electronicReceiptMock,
  printerListMock,
  saveElectronicReceiptAccountMock,
  setPrinterIsActiveMock,
} from './taxation/taxation.mock';
import {
  warehouseAddMock,
  articleComboboxMock,
  warehouseDeleteMock,
  warehouseDuplicateMock,
  warehouseEditMock,
  warehouseMock,
} from './warehouse/warehouse.mock';

/** Answers the mocked endpoints; included only in builds with mocks (see `mock-interceptors.ts`). */
export const mockApiInterceptor: HttpInterceptorFn = (request, next) => {
  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/auth/login`
  ) {
    return loginMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/auth/refresh`
  ) {
    return refreshMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/auth/logout`
  ) {
    return logoutMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/users-list`
  ) {
    return usersListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/create-user`
  ) {
    return createUserMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/delete-user`
  ) {
    return deleteUserMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/identity/get-selected-user`
  ) {
    return selectedUserMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/identity/update-user`
  ) {
    return updateUserMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/audit/entries-list`) {
    return entriesListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/audit/user-combobox`
  ) {
    return userComboboxMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/warehouse/list`) {
    return warehouseMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/article-combobox`
  ) {
    return articleComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/add-warehouse-item`
  ) {
    return warehouseAddMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/edit-warehouse-item`
  ) {
    return warehouseEditMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/delete-warehouse-item`
  ) {
    return warehouseDeleteMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/duplicate-warehouse-item`
  ) {
    return warehouseDuplicateMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/system/email-configuration`
  ) {
    return emailConfigurationMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/system/save-email-configuration`
  ) {
    return saveEmailConfigurationMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/system/send-test-email`
  ) {
    return sendTestEmailMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/system/info-support`
  ) {
    return supportInfoMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/system/contact-support`
  ) {
    return contactSupportMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/system/get-stripe-information`
  ) {
    return stripeInformationMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/booking/get-online-services-options`
  ) {
    return onlineServicesOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/booking/update-online-services-options`
  ) {
    return updateOnlineServicesOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/printer-list`
  ) {
    return printerListMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/taxation/electronic-receipt`
  ) {
    return electronicReceiptMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/set-is-active`
  ) {
    return setPrinterIsActiveMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/electronic-receipt-activation`
  ) {
    return electronicReceiptActivationMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/save-electronic-receipt-account`
  ) {
    return saveElectronicReceiptAccountMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/add-printer`
  ) {
    return addPrinterMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/delete-printer`
  ) {
    return deletePrinterMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/taxation/download-guide-electronic-receipt`
  ) {
    return downloadGuideElectronicReceiptMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/existing-booking-list`
  ) {
    return existingBookingListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/affiliated-hotel-combobox`
  ) {
    return affiliatedHotelComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/move-to-trash`
  ) {
    return moveToTrashMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/trash-item-list`
  ) {
    return trashItemListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/empty-trash`
  ) {
    return emptyTrashMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/restore-booking-item`
  ) {
    return restoreBookingItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/database/delete-selected-item`
  ) {
    return deleteSelectedItemMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/seasons/season-dates`
  ) {
    return seasonDatesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/save-season-dates`
  ) {
    return saveSeasonDatesMock(request);
  }

  if (request.method === 'GET' && request.url === `${environment.apiBaseUrl}/seasons/period-list`) {
    return seasonPeriodListMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/seasons/add-period`) {
    return addSeasonPeriodMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/delete-period`
  ) {
    return deleteSeasonPeriodMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/delete-intervals`
  ) {
    return deleteSeasonIntervalsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/update-interval`
  ) {
    return updateSeasonIntervalMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/add-interval`
  ) {
    return addSeasonIntervalMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/seasons/update-period`
  ) {
    return updateSeasonPeriodMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/sharing/general-information`
  ) {
    return sharingGeneralInformationMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/sharing/save-information`
  ) {
    return saveSharingInformationMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/vouchers/voucher-list`
  ) {
    return voucherListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/vouchers/delete-voucher-item`
  ) {
    return deleteVoucherItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/vouchers/duplicate-voucher-item`
  ) {
    return duplicateVoucherItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/vouchers/add-voucher-item`
  ) {
    return addVoucherItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/vouchers/update-voucher-item`
  ) {
    return updateVoucherItemMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/vouchers/get-selected-item`
  ) {
    return selectedVoucherItemMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/countries-combobox`
  ) {
    return countriesComboboxMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-general`
  ) {
    return bathhouseConfigurationGeneralMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-services`
  ) {
    return bathhouseConfigurationServicesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/bathhouse-configuration-media`
  ) {
    return bathhouseConfigurationMediaMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/save-bathhouse-configuration-general`
  ) {
    return saveBathhouseConfigurationGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/save-bathhouse-configuration-services`
  ) {
    return saveBathhouseConfigurationServicesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/bar-service-list`
  ) {
    return barServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/create-bar-service`
  ) {
    return createBarServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-bar-service-general-info`
  ) {
    return barServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-bar-service-prices`
  ) {
    return barServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-bar-service-booking-options`
  ) {
    return barServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-bar-service-general`
  ) {
    return updateBarServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-bar-service-prices`
  ) {
    return updateBarServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-bar-service-booking`
  ) {
    return updateBarServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-bar-service-item`
  ) {
    return deleteBarServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-bar-service-item`
  ) {
    return duplicateBarServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/standard-service-list`
  ) {
    return standardServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/create-standard-service`
  ) {
    return createStandardServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-standard-service-general-info`
  ) {
    return standardServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-standard-service-prices`
  ) {
    return standardServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-standard-service-booking-options`
  ) {
    return standardServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-standard-service-general`
  ) {
    return updateStandardServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-standard-service-prices`
  ) {
    return updateStandardServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-standard-service-booking`
  ) {
    return updateStandardServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-standard-service-item`
  ) {
    return deleteStandardServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-standard-service-item`
  ) {
    return duplicateStandardServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/hourly-service-list`
  ) {
    return hourlyServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/create-hourly-service`
  ) {
    return createHourlyServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-hourly-service-general-info`
  ) {
    return hourlyServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-hourly-service-prices`
  ) {
    return hourlyServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-hourly-service-booking-options`
  ) {
    return hourlyServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-hourly-service-general`
  ) {
    return updateHourlyServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-hourly-service-prices`
  ) {
    return updateHourlyServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-hourly-service-booking`
  ) {
    return updateHourlyServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-hourly-service-item`
  ) {
    return deleteHourlyServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-hourly-service-item`
  ) {
    return duplicateHourlyServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/automatic-price-list`
  ) {
    return automaticPriceListMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-automatic-price-services`
  ) {
    return automaticPriceServicesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/create-automatic-price`
  ) {
    return createAutomaticPriceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-automatic-price-general-info`
  ) {
    return automaticPriceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-automatic-price-prices-validity`
  ) {
    return automaticPricePricesValidityMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-automatic-price-general`
  ) {
    return updateAutomaticPriceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-automatic-price-prices-validity`
  ) {
    return updateAutomaticPricePricesValidityMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-automatic-price-item`
  ) {
    return deleteAutomaticPriceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-automatic-price-item`
  ) {
    return duplicateAutomaticPriceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-service-list`
  ) {
    return tableServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-table-service-item`
  ) {
    return deleteTableServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-table-service-item`
  ) {
    return duplicateTableServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/create-table-service`
  ) {
    return createTableServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-table-service-general-info`
  ) {
    return tableServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/get-table-service-availability-bookings`
  ) {
    return tableServiceAvailabilityBookingsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-table-service-general`
  ) {
    return updateTableServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-table-service-availability-bookings`
  ) {
    return updateTableServiceAvailabilityBookingsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/sectors-list`
  ) {
    return sectorListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/sector-combobox`
  ) {
    return sectorComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/add-sector-item`
  ) {
    return addSectorItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/update-sector-item`
  ) {
    return updateSectorItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-sector-item`
  ) {
    return deleteSectorItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-sector-item`
  ) {
    return duplicateSectorItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/packages-list`
  ) {
    return packageListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-package-item`
  ) {
    return deletePackageItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-package-item`
  ) {
    return duplicatePackageItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/packages/create-package`
  ) {
    return createPackageMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/packages/update-package`
  ) {
    return updatePackageMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url.startsWith(`${environment.apiBaseUrl}/catalog/packages/`)
  ) {
    return selectedPackageMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/settings`
  ) {
    return mapSettingsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/background`
  ) {
    return uploadMapBackgroundMock(request);
  }

  if (
    request.method === 'DELETE' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/background`
  ) {
    return removeMapBackgroundMock(request);
  }

  if (
    request.method === 'PUT' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/update-background`
  ) {
    return updateMapBackgroundMock(request);
  }

  if (
    request.method === 'PUT' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/update-configuration`
  ) {
    return updateMapConfigurationMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/catalog/map/reset`) {
    return resetMapMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/map/sector-area-combobox`
  ) {
    return sectorAreaComboboxMock(request);
  }

  return next(request);
};
