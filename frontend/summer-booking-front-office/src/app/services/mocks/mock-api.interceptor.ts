import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { logoutMock, refreshMock, signInMock } from './auth/auth.mock';
import {
  contactSupportMock,
  emailConfigurationMock,
  saveEmailConfigurationMock,
  sendTestEmailMock,
  supportInfoMock,
} from './system/system.mock';
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
  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/auth/signin`) {
    return signInMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/auth/refresh`) {
    return refreshMock(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/auth/logout`) {
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

  return next(request);
};
