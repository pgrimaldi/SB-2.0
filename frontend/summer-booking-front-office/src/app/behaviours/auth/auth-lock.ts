import { DOCUMENT } from '@angular/common';
import { InjectionToken, inject } from '@angular/core';

const LOCK_NAME = 'sb-auth-cookie';

/**
 * One call at a time on the refresh cookie (sign-in, refresh, logout). The browser applies the
 * cookie of each answer by itself, and scripts can neither read nor undo it: two of these calls
 * running together could leave the cookie of the wrong one (RFC 6265, 4.1.1).
 */
export interface AuthLock {
  /** Runs `task` once no other call holds the lock; an aborted wait never runs it. */
  run<T>(task: () => Promise<T>, signal: AbortSignal): Promise<T>;
}

/**
 * The browser's Web Locks, shared by every tab of the site (all tabs share the cookie) and released
 * by the browser when a tab closes; a queue of this tab only where they are missing (e.g. tests).
 */
export const AUTH_LOCK = new InjectionToken<AuthLock>('AUTH_LOCK', {
  providedIn: 'root',
  factory: () => {
    const locks = inject(DOCUMENT).defaultView?.navigator.locks as LockManager | undefined;
    return locks
      ? { run: (task, signal) => locks.request(LOCK_NAME, { signal }, () => task()) }
      : inTabLock();
  },
});

export function inTabLock(): AuthLock {
  let last: Promise<unknown> = Promise.resolve();
  return {
    run: (task, signal) => {
      const turn = last.then(() => {
        signal.throwIfAborted();
        return task();
      });
      last = turn.catch(() => undefined);
      return turn;
    },
  };
}
