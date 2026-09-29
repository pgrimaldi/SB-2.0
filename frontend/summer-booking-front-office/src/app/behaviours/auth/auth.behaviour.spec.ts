import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AuthSession } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';
import { AuthBehaviour } from './auth.behaviour';

describe('AuthBehaviour', () => {
  const session = (accessToken: string): AuthSession => ({
    accessToken,
    expiresIn: 900,
    user: { email: 'user@example.com', idProperty: 'property-1', roles: ['Manager'] },
  });
  let server: { refresh: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    server = { refresh: vi.fn(), logout: vi.fn().mockReturnValue(of(undefined)) };
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

  it('should revoke the session on the server, forget it here and go back to the home on logout', () => {
    const auth = load();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    auth.start(session('first'), true);

    auth.logout();

    expect(server.logout).toHaveBeenCalled();
    expect(auth.isAuthenticated()).toBe(false);
    expect(storedValues()).toBe('');
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('should remove the token that older versions kept in storage', () => {
    localStorage.setItem('sb.session', '{"token":"old"}');

    load();

    expect(localStorage.getItem('sb.session')).toBeNull();
  });
});
