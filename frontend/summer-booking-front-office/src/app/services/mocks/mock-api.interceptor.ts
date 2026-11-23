import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { entriesListMock } from './audit/audit.mock';
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
  deleteAutomaticPriceImageMock,
  uploadAutomaticPriceImageMock,
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
  addBathhousePhotoMock,
  deleteBathhouseDocumentMock,
  deleteBathhouseMainPhotoMock,
  deleteBathhousePhotoMock,
  uploadBathhouseDocumentMock,
  uploadBathhouseMainPhotoMock,
  deleteServiceImageMock,
  uploadServiceImageMock,
  bathhouseConfigurationServicesMock,
  lookupCountriesMock,
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
  saveBathhouseConfigurationMediaMock,
  saveBathhouseConfigurationServicesMock,
  standardServiceListMock,
  tableServiceListMock,
} from './catalog/catalog.mock';
import {
  mapSettingsMock,
  removeMapBackgroundMock,
  resetMapMock,
  updateMapBackgroundMock,
  updateMapConfigurationMock,
  uploadMapBackgroundMock,
} from './catalog/map.mock';
import {
  addSectorMock,
  deleteSectorsMock,
  duplicateSectorsMock,
  sectorAreaComboboxMock,
  sectorComboboxMock,
  sectorListMock,
  updateSectorMock,
} from './catalog/sectors.mock';
import {
  additionalItemListMock,
  createAdditionalItemMock,
  createPackageMock,
  deleteAdditionalItemsMock,
  deletePackageItemsMock,
  duplicateAdditionalItemsMock,
  duplicatePackageItemsMock,
  packageArticlesMock,
  packageComboboxMock,
  packageListMock,
  selectedAdditionalItemMock,
  selectedPackageMock,
  updateAdditionalItemMock,
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
  userComboboxMock,
  usersListMock,
} from './identity/identity.mock';
import {
  createSeasonPeriodMock,
  deleteSeasonPeriodMock,
  deleteSeasonRangesMock,
  saveSeasonDatesMock,
  seasonDatesMock,
  seasonPeriodListMock,
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
  deleteArticlesMock,
  inventoryItemsListMock,
  warehouseDuplicateMock,
  selectedArticleMock,
  updateArticleMock,
  warehouseMock,
} from './warehouse/warehouse.mock';

/** The tariff periods of the current or of the next season, with a period key or the bulk delete of ranges. */
const TARIFF_PERIODS_URL =
  /^\/catalog\/(next-season\/)?tariff-periods(?:\/(\d+)|\/(bulk-delete-ranges))?$/;

/** The sectors: the add, one sector by id, or the bulk delete and duplicate. */
const SECTORS_URL = /^\/catalog\/sectors(?:\/(bulk-delete|bulk-duplicate)|\/([^/]+))?$/;
const INVENTORY_ITEM_URL = /^\/catalog\/inventory-items\/([^/]+)$/;
const EDIT_WAREHOUSE_ITEM_URL = /^\/catalog\/edit-warehouse-item\/([^/]+)$/;

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
    request.method === 'DELETE' &&
    request.url.startsWith(`${environment.apiBaseUrl}/identity/property-users/`)
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
    request.url === `${environment.apiBaseUrl}/identity/user-combobox`
  ) {
    return userComboboxMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/warehouse/list`) {
    return warehouseMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/inventory-items-list`
  ) {
    return inventoryItemsListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/article-combobox`
  ) {
    return articleComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/add-warehouse-item`
  ) {
    return warehouseAddMock(request);
  }

  const inventoryItem = request.url.startsWith(environment.apiBaseUrl)
    ? INVENTORY_ITEM_URL.exec(request.url.slice(environment.apiBaseUrl.length))
    : null;
  if (inventoryItem && request.method === 'GET') {
    return selectedArticleMock(request, decodeURIComponent(inventoryItem[1]));
  }
  const editedItem = request.url.startsWith(environment.apiBaseUrl)
    ? EDIT_WAREHOUSE_ITEM_URL.exec(request.url.slice(environment.apiBaseUrl.length))
    : null;
  if (editedItem && request.method === 'PUT') {
    return updateArticleMock(request, decodeURIComponent(editedItem[1]));
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/delete-warehouse-item`
  ) {
    return deleteArticlesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/duplicate-warehouse-item`
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

  if (request.method === 'GET' && request.url === `${environment.apiBaseUrl}/catalog/season`) {
    return seasonDatesMock(request);
  }

  if (request.method === 'PUT' && request.url === `${environment.apiBaseUrl}/catalog/season`) {
    return saveSeasonDatesMock(request);
  }

  const tariffPeriods = request.url.startsWith(environment.apiBaseUrl)
    ? TARIFF_PERIODS_URL.exec(request.url.slice(environment.apiBaseUrl.length))
    : null;
  if (tariffPeriods) {
    const [, nextSeason, key, bulkDeleteRanges] = tariffPeriods;
    const isNextSeason = nextSeason !== undefined;
    if (request.method === 'GET' && key === undefined && bulkDeleteRanges === undefined) {
      return seasonPeriodListMock(request, isNextSeason);
    }
    if (request.method === 'PUT' && key !== undefined) {
      return updateSeasonPeriodMock(request, isNextSeason, Number(key));
    }
    if (request.method === 'DELETE' && key !== undefined) {
      return deleteSeasonPeriodMock(request, isNextSeason, Number(key));
    }
    if (request.method === 'POST' && key === undefined && bulkDeleteRanges === undefined) {
      return createSeasonPeriodMock(request, isNextSeason);
    }
    if (request.method === 'POST' && bulkDeleteRanges !== undefined) {
      return deleteSeasonRangesMock(request, isNextSeason);
    }
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
    request.url === `${environment.apiBaseUrl}/catalog/lookups/countries`
  ) {
    return lookupCountriesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-general`
  ) {
    return bathhouseConfigurationGeneralMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-services`
  ) {
    return bathhouseConfigurationServicesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/property-info/bathhouse-configuration-media`
  ) {
    return bathhouseConfigurationMediaMock(request);
  }

  if (request.url === `${environment.apiBaseUrl}/catalog/property-info/main-photo`) {
    if (request.method === 'POST') {
      return uploadBathhouseMainPhotoMock(request);
    }
    if (request.method === 'DELETE') {
      return deleteBathhouseMainPhotoMock(request);
    }
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/property-info/photos`
  ) {
    return addBathhousePhotoMock(request);
  }

  if (
    request.method === 'DELETE' &&
    request.url.startsWith(`${environment.apiBaseUrl}/catalog/property-info/photos/`)
  ) {
    return deleteBathhousePhotoMock(request);
  }

  if (request.url.startsWith(`${environment.apiBaseUrl}/catalog/property-info/documents/`)) {
    if (request.method === 'POST') {
      return uploadBathhouseDocumentMock(request);
    }
    if (request.method === 'DELETE') {
      return deleteBathhouseDocumentMock(request);
    }
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-general`
  ) {
    return saveBathhouseConfigurationGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-services`
  ) {
    return saveBathhouseConfigurationServicesMock(request);
  }
  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/property-info/save-bathhouse-configuration-media`
  ) {
    return saveBathhouseConfigurationMediaMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/bar-service-list`
  ) {
    return barServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/create-bar-service`
  ) {
    return createBarServiceMock(request);
  }

  if (
    request.url.startsWith(`${environment.apiBaseUrl}/catalog/services/`) &&
    request.url.endsWith('/image')
  ) {
    if (request.method === 'POST') {
      return uploadServiceImageMock(request);
    }
    if (request.method === 'DELETE') {
      return deleteServiceImageMock(request);
    }
  }

  if (
    request.url.startsWith(`${environment.apiBaseUrl}/catalog/automatic-prices/`) &&
    request.url.endsWith('/image')
  ) {
    if (request.method === 'POST') {
      return uploadAutomaticPriceImageMock(request);
    }
    if (request.method === 'DELETE') {
      return deleteAutomaticPriceImageMock(request);
    }
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-general-info`
  ) {
    return barServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-prices`
  ) {
    return barServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-bar-service-booking-options`
  ) {
    return barServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-general`
  ) {
    return updateBarServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-prices`
  ) {
    return updateBarServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-bar-service-booking`
  ) {
    return updateBarServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/delete-bar-service-item`
  ) {
    return deleteBarServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/duplicate-bar-service-item`
  ) {
    return duplicateBarServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/standard-service-list`
  ) {
    return standardServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/create-standard-service`
  ) {
    return createStandardServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-standard-service-general-info`
  ) {
    return standardServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-standard-service-prices`
  ) {
    return standardServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/services/get-standard-service-booking-options`
  ) {
    return standardServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-general`
  ) {
    return updateStandardServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-prices`
  ) {
    return updateStandardServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-standard-service-booking`
  ) {
    return updateStandardServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/delete-standard-service-item`
  ) {
    return deleteStandardServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/duplicate-standard-service-item`
  ) {
    return duplicateStandardServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/hourly-service-list`
  ) {
    return hourlyServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/create-hourly-service`
  ) {
    return createHourlyServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-general-info`
  ) {
    return hourlyServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-prices`
  ) {
    return hourlyServicePricesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/get-hourly-service-booking-options`
  ) {
    return hourlyServiceBookingOptionsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-general`
  ) {
    return updateHourlyServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-prices`
  ) {
    return updateHourlyServicePricesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/update-hourly-service-booking`
  ) {
    return updateHourlyServiceBookingMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/delete-hourly-service-item`
  ) {
    return deleteHourlyServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/services/duplicate-hourly-service-item`
  ) {
    return duplicateHourlyServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/automatic-prices/automatic-price-list`
  ) {
    return automaticPriceListMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-services`
  ) {
    return automaticPriceServicesMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/automatic-prices/create-automatic-price`
  ) {
    return createAutomaticPriceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-general-info`
  ) {
    return automaticPriceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/get-automatic-price-prices-validity`
  ) {
    return automaticPricePricesValidityMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/update-automatic-price-general`
  ) {
    return updateAutomaticPriceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/update-automatic-price-prices-validity`
  ) {
    return updateAutomaticPricePricesValidityMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/automatic-prices/delete-automatic-price-item`
  ) {
    return deleteAutomaticPriceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/automatic-prices/duplicate-automatic-price-item`
  ) {
    return duplicateAutomaticPriceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-services/table-service-list`
  ) {
    return tableServiceListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-services/delete-table-service-item`
  ) {
    return deleteTableServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-services/duplicate-table-service-item`
  ) {
    return duplicateTableServiceItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-services/create-table-service`
  ) {
    return createTableServiceMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/table-services/get-table-service-general-info`
  ) {
    return tableServiceGeneralInfoMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/table-services/get-table-service-availability-bookings`
  ) {
    return tableServiceAvailabilityBookingsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/table-services/update-table-service-general`
  ) {
    return updateTableServiceGeneralMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/table-services/update-table-service-availability-bookings`
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
    request.url === `${environment.apiBaseUrl}/catalog/sectors/sector-combobox`
  ) {
    return sectorComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/sectors/sector-area-combobox`
  ) {
    return sectorAreaComboboxMock(request);
  }

  const sectors = request.url.startsWith(environment.apiBaseUrl)
    ? SECTORS_URL.exec(request.url.slice(environment.apiBaseUrl.length))
    : null;
  if (sectors) {
    const [, bulk, idSector] = sectors;
    if (request.method === 'POST' && bulk === undefined && idSector === undefined) {
      return addSectorMock(request);
    }
    if (request.method === 'PUT' && idSector !== undefined) {
      return updateSectorMock(request, decodeURIComponent(idSector));
    }
    if (request.method === 'POST' && bulk === 'bulk-delete') {
      return deleteSectorsMock(request);
    }
    if (request.method === 'POST' && bulk === 'bulk-duplicate') {
      return duplicateSectorsMock(request);
    }
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/packages-list`
  ) {
    return packageListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/delete-package-item`
  ) {
    return deletePackageItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/duplicate-package-item`
  ) {
    return duplicatePackageItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/additional-services-articles-list`
  ) {
    return additionalItemListMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/delete-additional-services-article-item`
  ) {
    return deleteAdditionalItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/duplicate-additional-services-article-item`
  ) {
    return duplicateAdditionalItemsMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/package-combobox`
  ) {
    return packageComboboxMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/create-additional-services-article-item`
  ) {
    return createAdditionalItemMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/get-additional-services-article-item`
  ) {
    return selectedAdditionalItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url ===
      `${environment.apiBaseUrl}/catalog/price-lists/update-additional-services-article-item`
  ) {
    return updateAdditionalItemMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/create-package`
  ) {
    return createPackageMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/update-package`
  ) {
    return updatePackageMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/get-package-articles`
  ) {
    return packageArticlesMock(request);
  }

  if (
    request.method === 'GET' &&
    request.url === `${environment.apiBaseUrl}/catalog/price-lists/get-package`
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

  return next(request);
};
