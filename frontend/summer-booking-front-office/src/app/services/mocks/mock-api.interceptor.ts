import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { signInMock } from './auth/auth.mock';
import { warehouseMock } from './warehouse/warehouse.mock';

export const mockApiInterceptor: HttpInterceptorFn = (request, next) => {
  if (!environment.features.useMocks) {
    return next(request);
  }

  if (request.method === 'POST' && request.url === `${environment.apiBaseUrl}/auth/signin`) {
    return signInMock(request);
  }

  if (request.method === 'GET' && request.url === `${environment.apiBaseUrl}/warehouse/list`) {
    return warehouseMock(request);
  }

  return next(request);
};
