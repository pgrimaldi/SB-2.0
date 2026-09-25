import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { AuthUser, SignInResponse } from '../../entities/auth/credentials';

interface StoredSession {
  token: string;
  user: AuthUser;
  /** Milliseconds since epoch; only remembered sessions expire by time. */
  expiresAt?: number;
}

const STORAGE_KEY = 'sb.session';
const REMEMBERED_SESSION_DURATION = 30 * 24 * 60 * 60 * 1000;

/**
 * Signed-in session answered by the server. Standard session: it survives page reloads and ends
 * when the tab or the browser is closed. With "remember me" it is kept for 30 days.
 * Either way it ends on sign out.
 */
@Injectable({ providedIn: 'root' })
export class AuthBehaviour {
  private readonly window = inject(DOCUMENT).defaultView;
  private readonly session = signal<StoredSession | null>(this.read());

  readonly user = () => this.current()?.user ?? null;
  readonly token = () => this.current()?.token ?? null;

  isAuthenticated(): boolean {
    return this.current() !== null;
  }

  start(response: SignInResponse, remember: boolean): void {
    const session: StoredSession = { token: response.token, user: response.user };
    if (remember) {
      session.expiresAt = Date.now() + REMEMBERED_SESSION_DURATION;
    }
    this.clear();
    this.storage(remember ? 'local' : 'session')?.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  end(): void {
    this.clear();
    this.session.set(null);
  }

  /** The session while it is still valid, otherwise null. */
  private current(): StoredSession | null {
    const session = this.session();
    return session && !this.isExpired(session) ? session : null;
  }

  private isExpired(session: StoredSession): boolean {
    return session.expiresAt !== undefined && session.expiresAt <= Date.now();
  }

  /** Session saved by a previous page load; an expired one is removed. */
  private read(): StoredSession | null {
    for (const kind of ['session', 'local'] as const) {
      const stored = this.storage(kind)?.getItem(STORAGE_KEY);
      if (!stored) {
        continue;
      }
      try {
        const session = JSON.parse(stored) as StoredSession;
        if (!this.isExpired(session)) {
          return session;
        }
      } catch {
        // Unreadable value: removed below.
      }
      this.storage(kind)?.removeItem(STORAGE_KEY);
    }
    return null;
  }

  private clear(): void {
    this.storage('session')?.removeItem(STORAGE_KEY);
    this.storage('local')?.removeItem(STORAGE_KEY);
  }

  /** Browser storage, or undefined when it is not available (e.g. blocked in private mode). */
  private storage(kind: 'session' | 'local'): Storage | undefined {
    try {
      return kind === 'local' ? this.window?.localStorage : this.window?.sessionStorage;
    } catch {
      return undefined;
    }
  }
}
