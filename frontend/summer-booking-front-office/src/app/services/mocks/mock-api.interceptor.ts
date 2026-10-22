import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { loginMock, logoutMock, refreshMock } from './auth/auth.mock';
import { onlineServicesOptionsMock, updateOnlineServicesOptionsMock } from './booking/booking.mock';
import {
  bathhouseConfigurationGeneralMock,
  bathhouseConfigurationServicesMock,
  countriesComboboxMock,
  saveBathhouseConfigurationGeneralMock,
  saveBathhouseConfigurationServicesMock,
} from './catalog/catalog.mock';
import {
  affiliatedHotelComboboxMock,
  deleteSelectedItemMock,
  emptyTrashMock,
  existingBookingListMock,
  moveToTrashMock,
  restoreBookingItemMock,
  trashItemListMock,
} from './database/database.mock';
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
  warehouseComboboxMock,
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

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/warehouse/list`) {
    return warehouseMock(request);
  }

  if (
    request.method === 'POST' &&
    request.url === `${environment.apiBaseUrl}/warehouse/combobox-list`
  ) {
    return warehouseComboboxMock(request);
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
    request.url === `${environment.apiBaseUrl}/database/get-affiliated-hotel-combobox`
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

  return next(request);
};
