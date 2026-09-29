import { HttpInterceptorFn } from '@angular/common/http';
import { mockApiInterceptor } from './mock-api.interceptor';

/**
 * Interceptors that answer the API with mocks (local and DEV builds). The production build replaces
 * this file with `mock-interceptors.none.ts` (angular.json, fileReplacements): mock code, test
 * account included, never reaches the published JavaScript.
 */
export const MOCK_INTERCEPTORS: HttpInterceptorFn[] = [mockApiInterceptor];
