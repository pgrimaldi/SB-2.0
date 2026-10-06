import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthBehaviour } from '../../behaviours/auth/auth.behaviour';
import { AuthSession } from '../../entities/auth/credentials';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const api = environment.apiBaseUrl;
  const unauthorized = { status: 401, statusText: 'Unauthorized' };
  const session = (accessToken: string, email = 'anna@example.com'): AuthSession => ({
    accessToken,
    expiresIn: 900,
    user: { email, idProperty: `property-of-${email}`, roles: [] },
  });

  const setup = () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const auth = TestBed.inject(AuthBehaviour);
    auth.start(session('first'), false);
    return {
      auth,
      http: TestBed.inject(HttpClient),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  afterEach(() => sessionStorage.clear());

  /** The refresh starts once the lock of the cookie is free, after the current task. */
  const settle = () => new Promise((resolve) => setTimeout(resolve));

  it('should send the access token to our API only', () => {
    const { http, controller } = setup();

    http.get(`${api}/warehouse/list`).subscribe();
    http.get('https://example.com/other').subscribe();

    const ours = controller.expectOne(`${api}/warehouse/list`);
    const other = controller.expectOne('https://example.com/other');
    expect(ours.request.headers.get('Authorization')).toBe('Bearer first');
    expect(other.request.headers.has('Authorization')).toBe(false);
  });

  it('should renew an expired token with the refresh cookie and repeat the request once', async () => {
    const { http, controller } = setup();
    let answer: unknown;

    http.get(`${api}/warehouse/list`).subscribe((body) => (answer = body));
    controller.expectOne(`${api}/warehouse/list`).flush(null, unauthorized);
    await settle();
    const refresh = controller.expectOne(`${api}/auth/refresh`);
    expect(refresh.request.headers.has('Authorization')).toBe(false); // the cookie is enough
    refresh.flush(session('second'));
    const repeated = controller.expectOne(`${api}/warehouse/list`);
    repeated.flush({ total: 0, rows: [] });

    expect(repeated.request.headers.get('Authorization')).toBe('Bearer second');
    expect(answer).toEqual({ total: 0, rows: [] });
  });

  it('should sign out and go back to the home when the session is over', async () => {
    const { auth, http, controller } = setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    let status = 0;

    http.get(`${api}/warehouse/list`).subscribe({ error: (error) => (status = error.status) });
    controller.expectOne(`${api}/warehouse/list`).flush(null, unauthorized);
    await settle();
    controller.expectOne(`${api}/auth/refresh`).flush(null, unauthorized);

    expect(status).toBe(401);
    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('should not renew nor repeat for the new user a request sent by the previous one', () => {
    const { auth, http, controller } = setup();
    let status = 0;

    http.post(`${api}/warehouse/delete-warehouse-item`, {}).subscribe({
      error: (error) => (status = error.status),
    });
    auth.start(session('bea-token', 'bea@example.com'), false);
    controller.expectOne(`${api}/warehouse/delete-warehouse-item`).flush(null, unauthorized);

    expect(status).toBe(401);
    controller.expectNone(`${api}/auth/refresh`);
    controller.expectNone(`${api}/warehouse/delete-warehouse-item`);
    expect(auth.token()).toBe('bea-token');
  });

  it('should not repeat the request nor sign out the new user when the session changes during the refresh', async () => {
    const { auth, http, controller } = setup();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    let status = 0;

    http.get(`${api}/warehouse/list`).subscribe({ error: (error) => (status = error.status) });
    controller.expectOne(`${api}/warehouse/list`).flush(null, unauthorized);
    await settle();
    const refresh = controller.expectOne(`${api}/auth/refresh`);
    auth.start(session('bea-token', 'bea@example.com'), false);
    refresh.flush(session('anna-late'));

    expect(status).toBe(401);
    controller.expectNone(`${api}/warehouse/list`);
    expect(auth.token()).toBe('bea-token');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should log out with the cookie only, and never renew the session while logging out', () => {
    const { http, controller } = setup();

    http.post(`${api}/auth/logout`, null).subscribe({ error: () => undefined });
    const logout = controller.expectOne(`${api}/auth/logout`);
    expect(logout.request.headers.has('Authorization')).toBe(false);
    logout.flush(null, unauthorized);

    controller.expectNone(`${api}/auth/refresh`);
  });
});
