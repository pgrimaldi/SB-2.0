import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, defer, firstValueFrom, of, throwError } from 'rxjs';
import { AuthSession } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';
import { AuthBehaviour, LOGOUT_RETRY_DELAY } from './auth.behaviour';

describe('AuthBehaviour', () => {
  const session = (accessToken: string): AuthSession => ({
    accessToken,
    expiresIn: 900,
    user: { email: 'user@example.com', idProperty: 'property-1', roles: ['Manager'] },
  });
  let server: {
    signIn: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    server = {
      signIn: vi.fn(),
      refresh: vi.fn(),
      logout: vi.fn().mockReturnValue(of(undefined)),
    };
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  /** A new page load of the app. */
  const load = () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [{ provide: AuthService, useValue: server }] });
    return TestBed.inject(AuthBehaviour);
  };

  const storedValues = () =>
    [...Object.values(localStorage), ...Object.values(sessionStorage)].join(' ');

  it('should start the session when the server accepts the credentials', async () => {
    const auth = load();
    server.signIn.mockReturnValue(of(session('first')));
    const credentials = { username: 'user@example.com', password: 'secret', remember: true };

    await firstValueFrom(auth.signIn(credentials));

    expect(server.signIn).toHaveBeenCalledWith(credentials);
    expect(auth.token()).toBe('first');
    expect(localStorage.getItem('sb.signed-in')).toBe('true'); // remembered
  });

  it('should fail like the server and start nothing with wrong credentials', async () => {
    const auth = load();
    server.signIn.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));

    const failure = await firstValueFrom(
      auth.signIn({ username: 'user@example.com', password: 'wrong', remember: false }),
    ).catch((error: HttpErrorResponse) => error);

    expect((failure as HttpErrorResponse).status).toBe(401);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('should keep the access token only in memory, never in browser storage', () => {
    load().start(session('secret-access-token'), true);

    expect(storedValues()).not.toContain('secret-access-token');
  });

  it('should get the session back from the refresh cookie on the next page load', async () => {
    load().start(session('first'), false);
    server.refresh.mockReturnValue(of(session('second')));

    const auth = load();
    await auth.restore();

    expect(auth.token()).toBe('second');
    expect(auth.user()?.idProperty).toBe('property-1');
  });

  it('should ask the server nothing at start-up when nobody signed in in this browser', async () => {
    await load().restore();

    expect(server.refresh).not.toHaveBeenCalled();
  });

  it('should end a standard session with the browser, keep a remembered one', async () => {
    load().start(session('first'), false);
    sessionStorage.clear(); // browser closed
    await load().restore();
    expect(server.refresh).not.toHaveBeenCalled();

    load().start(session('first'), true);
    sessionStorage.clear();
    server.refresh.mockReturnValue(of(session('second')));
    const auth = load();
    await auth.restore();
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('should sign out when the server refuses the refresh cookie (expired or revoked)', async () => {
    load().start(session('first'), true);
    server.refresh.mockReturnValue(throwError(() => new Error('401')));

    const auth = load();
    await auth.restore();

    expect(auth.isAuthenticated()).toBe(false);
    expect(storedValues()).toBe('');
  });

  it('should ask for one new token even when several requests need it at once', () => {
    const auth = load();
    const answer = new Subject<AuthSession>();
    server.refresh.mockReturnValue(answer);
    const tokens: (string | null)[] = [];

    auth.refresh().subscribe((token) => tokens.push(token));
    auth.refresh().subscribe((token) => tokens.push(token));
    answer.next(session('fresh'));
    answer.complete();

    expect(server.refresh).toHaveBeenCalledTimes(1);
    expect(tokens).toEqual(['fresh', 'fresh']);
  });

  it('should end the session here and go back to the home once the server has revoked it', async () => {
    const auth = load();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);

    expect(await auth.logout()).toBe(true);

    expect(server.logout).toHaveBeenCalledTimes(1);
    expect(auth.isAuthenticated()).toBe(false);
    expect(storedValues()).toBe('');
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('should consider the logout done when the server has no session left (401)', async () => {
    const auth = load();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);
    server.logout.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));

    expect(await auth.logout()).toBe(true);
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('should keep the user signed in when the server cannot revoke the session, after one more try', async () => {
    vi.useFakeTimers();
    const auth = load();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);
    let attempts = 0; // each try is a new subscription to the same request, as with HttpClient
    server.logout.mockReturnValue(
      defer(() => {
        attempts++;
        return throwError(() => new HttpErrorResponse({ status: 0 }));
      }),
    );

    const result = auth.logout();
    await vi.advanceTimersByTimeAsync(LOGOUT_RETRY_DELAY);

    expect(await result).toBe(false);
    expect(attempts).toBe(2);
    expect(auth.token()).toBe('first');
    expect(storedValues()).toContain('true'); // still marked as signed in
    expect(navigate).not.toHaveBeenCalled();
    vi.useRealTimers();
  });

  it('should succeed when the second try reaches the server', async () => {
    vi.useFakeTimers();
    const auth = load();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);
    let attempts = 0;
    server.logout.mockReturnValue(
      defer(() =>
        ++attempts === 1 ? throwError(() => new HttpErrorResponse({ status: 503 })) : of(undefined),
      ),
    );

    const result = auth.logout();
    await vi.advanceTimersByTimeAsync(LOGOUT_RETRY_DELAY);

    expect(await result).toBe(true);
    expect(auth.isAuthenticated()).toBe(false);
    vi.useRealTimers();
  });

  it('should remove the token that older versions kept in storage', () => {
    localStorage.setItem('sb.session', '{"token":"old"}');

    load();

    expect(localStorage.getItem('sb.session')).toBeNull();
  });
});
