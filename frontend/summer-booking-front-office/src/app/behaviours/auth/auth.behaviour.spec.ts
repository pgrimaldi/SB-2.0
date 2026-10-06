import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, defer, firstValueFrom, of, throwError } from 'rxjs';
import { AuthSession } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';
import { AUTH_LOCK, inTabLock } from './auth-lock';
import { AuthBehaviour, COOKIE_CALL_TIMEOUT, LOGOUT_RETRY_DELAY } from './auth.behaviour';

describe('AuthBehaviour', () => {
  const session = (accessToken: string, email = 'user@example.com'): AuthSession => ({
    accessToken,
    expiresIn: 900,
    user: { email, idProperty: `property-of-${email}`, roles: ['Manager'] },
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

  /** Calls on the refresh cookie start once the lock is free, after the current task. */
  const settle = () => new Promise((resolve) => setTimeout(resolve));

  /** Two tabs of the same browser: same storage, same cookie lock, messages between them. */
  const twoTabs = () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: server },
        // Shared by both tabs, as the browser's Web Locks are.
        { provide: AUTH_LOCK, useValue: inTabLock() },
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const [firstTab, secondTab] = [0, 1].map(() =>
      TestBed.runInInjectionContext(() => new AuthBehaviour()),
    );
    onTestFinished(() => {
      firstTab.ngOnDestroy();
      secondTab.ngOnDestroy();
    });
    return { firstTab, secondTab, navigate };
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
    expect(auth.user()?.idProperty).toBe('property-of-user@example.com');
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

  it('should ask for one new token even when several requests need it at once', async () => {
    const auth = load();
    const answer = new Subject<AuthSession>();
    server.refresh.mockReturnValue(answer);
    const tokens: (string | null)[] = [];

    auth.refresh().subscribe((token) => tokens.push(token));
    auth.refresh().subscribe((token) => tokens.push(token));
    await settle();
    answer.next(session('fresh'));
    answer.complete();

    expect(server.refresh).toHaveBeenCalledTimes(1);
    expect(tokens).toEqual(['fresh', 'fresh']);
  });

  it('should not sign in again with a refresh answer that arrives after a logout in another tab', async () => {
    const auth = load();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);
    const late = new Subject<AuthSession>();
    server.refresh.mockReturnValue(late);
    const tokens: (string | null)[] = [];
    auth.refresh().subscribe((token) => tokens.push(token));
    await settle();

    auth.expire();
    late.next(session('late'));
    late.complete();

    expect(tokens).toEqual([null]);
    expect(auth.isAuthenticated()).toBe(false);
    expect(storedValues()).toBe('');
  });

  it('should keep the new user when a refresh of the previous session answers late', async () => {
    const auth = load();
    auth.start(session('first', 'anna@example.com'), false);
    const late = new Subject<AuthSession>();
    server.refresh.mockReturnValue(late);
    auth.refresh().subscribe();
    await settle();

    auth.start(session('bea-token', 'bea@example.com'), false);
    late.next(session('anna-late', 'anna@example.com'));
    late.complete();

    expect(auth.token()).toBe('bea-token');
    expect(auth.user()?.email).toBe('bea@example.com');
  });

  it('should keep the new session when a refresh of the previous one fails late', async () => {
    const auth = load();
    auth.start(session('first', 'anna@example.com'), true);
    const late = new Subject<AuthSession>();
    server.refresh.mockReturnValue(late);
    auth.refresh().subscribe();
    await settle();

    auth.start(session('bea-token', 'bea@example.com'), true);
    late.error(new HttpErrorResponse({ status: 401 }));

    expect(auth.token()).toBe('bea-token');
    expect(localStorage.getItem('sb.signed-in')).toBe('true');
  });

  it('should ask the server again for a new session, not share the refresh of the previous one', async () => {
    const auth = load();
    auth.start(session('first', 'anna@example.com'), false);
    const late = new Subject<AuthSession>();
    server.refresh.mockReturnValueOnce(late);
    auth.refresh().subscribe();
    await settle();

    auth.start(session('bea-token', 'bea@example.com'), false);
    server.refresh.mockReturnValueOnce(of(session('bea-fresh', 'bea@example.com')));
    let token: string | null = null;
    auth.refresh().subscribe((fresh) => (token = fresh));
    late.complete();
    await settle();

    expect(server.refresh).toHaveBeenCalledTimes(2);
    expect(token).toBe('bea-fresh');
  });

  it('should let a sign-in reach the server only once the refresh still running has answered', async () => {
    const auth = load();
    const refresh = new Subject<AuthSession>();
    server.refresh.mockReturnValue(refresh);
    server.signIn.mockReturnValue(of(session('bea-token', 'bea@example.com')));
    auth.refresh().subscribe();
    await settle();

    const signedIn = firstValueFrom(
      auth.signIn({ username: 'bea@example.com', password: 'secret', remember: false }),
    );
    await settle();
    expect(server.signIn).not.toHaveBeenCalled();

    refresh.error(new HttpErrorResponse({ status: 401 }));
    await signedIn;
    expect(auth.user()?.email).toBe('bea@example.com');
  });

  it('should log out only after the refresh still running, and not refresh while logging out', async () => {
    const auth = load();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);
    const refresh = new Subject<AuthSession>();
    server.refresh.mockReturnValue(refresh);
    const logout = new Subject<void>();
    server.logout.mockReturnValue(logout);
    auth.refresh().subscribe();
    await settle();

    const loggedOut = auth.logout();
    await settle();
    expect(server.logout).not.toHaveBeenCalled();
    refresh.next(session('second'));
    refresh.complete();
    await settle();
    expect(server.logout).toHaveBeenCalledTimes(1);

    const waiting = firstValueFrom(auth.refresh());
    logout.next();
    logout.complete();
    expect(await loggedOut).toBe(true);
    expect(await waiting).toBeNull();
    expect(server.refresh).toHaveBeenCalledTimes(1); // the waiting refresh asked nothing
  });

  it('should let the next call start when one does not answer within the time limit', async () => {
    vi.useFakeTimers();
    const auth = load();
    server.refresh.mockReturnValue(new Subject<AuthSession>()); // never answers
    server.signIn.mockReturnValue(of(session('bea-token', 'bea@example.com')));
    const refreshed = firstValueFrom(auth.refresh());
    const signedIn = firstValueFrom(
      auth.signIn({ username: 'bea@example.com', password: 'secret', remember: false }),
    );

    await vi.advanceTimersByTimeAsync(COOKIE_CALL_TIMEOUT);

    expect(await refreshed).toBeNull();
    await signedIn;
    expect(auth.user()?.email).toBe('bea@example.com');
  });

  it('should make the tabs of the browser take turns on the cookie', async () => {
    const { firstTab, secondTab } = twoTabs();
    firstTab.start(session('first'), true);
    server.refresh.mockReturnValueOnce(of(session('first'))); // the second tab opens
    await secondTab.restore();
    const refresh = new Subject<AuthSession>();
    server.refresh.mockReturnValue(refresh);
    firstTab.refresh().subscribe();
    await settle();

    const loggedOut = secondTab.logout();
    await settle();
    expect(server.logout).not.toHaveBeenCalled();

    refresh.next(session('second'));
    refresh.complete();
    expect(await loggedOut).toBe(true);
    expect(server.logout).toHaveBeenCalledTimes(1);
    await settle();
    expect(firstTab.isAuthenticated()).toBe(false); // told by the other tab
  });

  it('should leave this tab when someone signs in in another one, without logging out', async () => {
    const { firstTab, secondTab, navigate } = twoTabs();
    firstTab.start(session('anna-token', 'anna@example.com'), true);
    await settle();

    secondTab.start(session('bea-token', 'bea@example.com'), true);
    await settle();

    expect(firstTab.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith('/');
    expect(server.logout).not.toHaveBeenCalled();
    expect(localStorage.getItem('sb.signed-in')).toBe('true'); // the other tab is still signed in
    expect(secondTab.user()?.email).toBe('bea@example.com');
  });

  it('should leave this tab when the refresh cookie now belongs to another user', async () => {
    const auth = load();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('anna-token', 'anna@example.com'), true);
    server.refresh.mockReturnValue(of(session('bea-token', 'bea@example.com')));

    expect(await firstValueFrom(auth.refresh())).toBeNull();

    expect(auth.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith('/');
    expect(server.logout).not.toHaveBeenCalled();
    expect(localStorage.getItem('sb.signed-in')).toBe('true');
  });

  it('should keep a sign-in waiting until both tries of a logout are over', async () => {
    vi.useFakeTimers();
    const auth = load();
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('anna-token', 'anna@example.com'), true);
    const order: string[] = [];
    let attempts = 0;
    server.logout.mockReturnValue(
      defer(() => {
        order.push('logout');
        return ++attempts === 1
          ? throwError(() => new HttpErrorResponse({ status: 503 }))
          : of(undefined);
      }),
    );
    server.signIn.mockReturnValue(
      defer(() => {
        order.push('signin');
        return of(session('bea-token', 'bea@example.com'));
      }),
    );

    const loggedOut = auth.logout();
    await vi.advanceTimersByTimeAsync(LOGOUT_RETRY_DELAY / 2);
    const signedIn = firstValueFrom(
      auth.signIn({ username: 'bea@example.com', password: 'secret', remember: true }),
    );
    await vi.advanceTimersByTimeAsync(LOGOUT_RETRY_DELAY);

    expect(await loggedOut).toBe(true);
    await signedIn;
    expect(order).toEqual(['logout', 'logout', 'signin']);
    expect(auth.user()?.email).toBe('bea@example.com');
  });

  it('should ignore a logout message of another tab about an older session', async () => {
    const { firstTab } = twoTabs();
    const otherTab = new BroadcastChannel('sb-auth');
    onTestFinished(() => otherTab.close());
    otherTab.postMessage({ type: 'logout', session: 'an-older-session' });
    firstTab.start(session('bea-token', 'bea@example.com'), true);

    await settle();

    expect(firstTab.user()?.email).toBe('bea@example.com');
  });

  it('should keep a session when the sign-in message of an earlier one arrives after it', async () => {
    const { firstTab, secondTab } = twoTabs();
    firstTab.start(session('anna-token', 'anna@example.com'), true);
    secondTab.start(session('bea-token', 'bea@example.com'), true); // before Anna's message arrives

    await settle();

    expect(secondTab.user()?.email).toBe('bea@example.com');
    expect(firstTab.isAuthenticated()).toBe(false); // Bea's message reached Anna's tab
  });

  it('should not open the new session on reload after leaving, unless the browser remembers it', async () => {
    const { firstTab } = twoTabs();
    firstTab.start(session('anna-token', 'anna@example.com'), false); // mark of this tab only
    // Bea signs in, not remembered, in a tab of her own:
    localStorage.setItem('sb.session-name', 'bea-session');
    const beaTab = new BroadcastChannel('sb-auth');
    onTestFinished(() => beaTab.close());
    beaTab.postMessage({ type: 'signin', session: 'bea-session' });
    await settle();
    expect(firstTab.isAuthenticated()).toBe(false);

    const reloaded = load();
    await reloaded.restore();

    expect(server.refresh).not.toHaveBeenCalled();
    expect(reloaded.isAuthenticated()).toBe(false);
  });

  it('should keep the remembered sign-in of a new session when a late logout closes an old tab', async () => {
    const { firstTab, secondTab } = twoTabs();
    firstTab.start(session('anna-token', 'anna@example.com'), true);
    const annaSession = localStorage.getItem('sb.session-name');
    // Another tab of Anna logs out; Bea signs in, remembered, before that message arrives here:
    const annaOtherTab = new BroadcastChannel('sb-auth');
    onTestFinished(() => annaOtherTab.close());
    annaOtherTab.postMessage({ type: 'logout', session: annaSession });
    secondTab.start(session('bea-token', 'bea@example.com'), true);
    await settle();
    expect(firstTab.isAuthenticated()).toBe(false);

    server.refresh.mockReturnValue(of(session('bea-fresh', 'bea@example.com')));
    const reloaded = load();
    await reloaded.restore();

    expect(reloaded.user()?.email).toBe('bea@example.com');
  });

  it('should send no logout once another tab has signed in, as the cookie is no longer ours', async () => {
    const { firstTab, secondTab } = twoTabs();
    firstTab.start(session('anna-token', 'anna@example.com'), true);
    secondTab.start(session('bea-token', 'bea@example.com'), true);

    expect(await firstTab.logout()).toBe(true); // before the message of the other tab arrives

    expect(server.logout).not.toHaveBeenCalled();
    expect(firstTab.isAuthenticated()).toBe(false);
    expect(secondTab.user()?.email).toBe('bea@example.com');
    expect(localStorage.getItem('sb.signed-in')).toBe('true');
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
  });

  it('should remove the token that older versions kept in storage', () => {
    localStorage.setItem('sb.session', '{"token":"old"}');

    load();

    expect(localStorage.getItem('sb.session')).toBeNull();
  });
});
