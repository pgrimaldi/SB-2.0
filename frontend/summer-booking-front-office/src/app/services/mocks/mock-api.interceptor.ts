import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { logoutMock, refreshMock, signInMock } from './auth/auth.mock';
import {
  warehouseAddMock,
  warehouseComboboxMock,
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

  return next(request);
};
