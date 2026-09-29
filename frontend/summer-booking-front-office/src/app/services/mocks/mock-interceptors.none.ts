import { HttpInterceptorFn } from '@angular/common/http';

/** Production: no mocks, every request goes to the real API (see `mock-interceptors.ts`). */
export const MOCK_INTERCEPTORS: HttpInterceptorFn[] = [];
