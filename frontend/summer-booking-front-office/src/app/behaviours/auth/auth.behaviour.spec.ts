import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthBehaviour } from './auth.behaviour';

describe('AuthBehaviour', () => {
  const DAY = 24 * 60 * 60 * 1000;
  const response = {
    token: 'token',
    user: { email: 'user@example.com', idProperty: 'property-1' },
  };

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-25T10:00:00Z'));
  });

  afterEach(() => vi.useRealTimers());

  /** A new page load of the app. */
  const reload = () => {
    TestBed.resetTestingModule();
    return TestBed.inject(AuthBehaviour);
  };

  it('should keep a standard session across reloads, only for the open tab', () => {
    reload().start(response, false);

    vi.advanceTimersByTime(40 * DAY);
    expect(reload().token()).toBe('token');
    expect(sessionStorage.getItem('sb.session')).not.toBeNull();
    expect(localStorage.getItem('sb.session')).toBeNull();

    // Closing the tab or the browser empties sessionStorage.
    sessionStorage.clear();
    expect(reload().isAuthenticated()).toBe(false);
  });

  it('should keep a remembered session for 30 days, also after closing the browser', () => {
    reload().start(response, true);
    sessionStorage.clear();

    vi.advanceTimersByTime(29 * DAY);
    expect(reload().isAuthenticated()).toBe(true);

    vi.advanceTimersByTime(2 * DAY);
    expect(reload().isAuthenticated()).toBe(false);
    expect(localStorage.getItem('sb.session')).toBeNull();
  });

  it('should forget the session everywhere and go back to the home on logout', () => {
    const session = reload();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    session.start(response, true);
    session.logout();

    expect(session.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('sb.session')).toBeNull();
    expect(sessionStorage.getItem('sb.session')).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/');
    expect(reload().isAuthenticated()).toBe(false);
  });
});
