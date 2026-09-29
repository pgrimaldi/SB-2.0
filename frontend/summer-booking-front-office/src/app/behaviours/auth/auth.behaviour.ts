import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, firstValueFrom, map, of, shareReplay } from 'rxjs';
import { AuthSession, AuthUser } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';

/**
 * Non-secret mark that a sign-in happened in this browser, so that start-up asks for a new access
 * token only when a refresh cookie may exist (anonymous visitors make no request). It follows
 * "remember me": sessionStorage (ends with the browser) or localStorage.
 */
const SIGNED_IN_KEY = 'sb.signed-in';
/** Where older versions kept the token (readable by scripts): removed at start-up. */
const LEGACY_SESSION_KEY = 'sb.session';
/** Tells the other tabs of this browser that the user signed out. */
const CHANNEL_NAME = 'sb-auth';

interface Session {
  accessToken: string;
  user: AuthUser;
}

/**
 * Signed-in session (OWASP guidance for single-page apps):
 * - the access token (short-lived) lives only in memory, never in storage, and goes only to our API;
 * - the refresh token is in an HttpOnly, Secure, SameSite=Strict cookie handled by the server, with
 *   rotation and revocation: scripts cannot read it;
 * - at start-up and when the access token expires, a new one comes from the cookie (`refresh`).
 * Standard session: it ends when the browser is closed; "remember me": it lasts as long as the server
 * says. Logout revokes it on the server and in every tab.
 */
@Injectable({ providedIn: 'root' })
export class AuthBehaviour {
  private readonly window = inject(DOCUMENT).defaultView;
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly session = signal<Session | null>(null);
  private readonly channel =
    typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
  private refreshing: Observable<string | null> | null = null;

  readonly user = computed(() => this.session()?.user ?? null);

  constructor() {
    this.storage('session')?.removeItem(LEGACY_SESSION_KEY);
    this.storage('local')?.removeItem(LEGACY_SESSION_KEY);
    this.channel?.addEventListener('message', ({ data }) => {
      if (data === 'logout' && this.isAuthenticated()) {
        this.expire();
      }
    });
    inject(DestroyRef).onDestroy(() => this.channel?.close());
  }

  isAuthenticated(): boolean {
    return this.session() !== null;
  }

  /** Access token for our API requests (null when signed out). */
  token(): string | null {
    return this.session()?.accessToken ?? null;
  }

  /** After sign-in: the server has already set the refresh cookie. */
  start({ accessToken, user }: AuthSession, remember: boolean): void {
    this.session.set({ accessToken, user });
    this.forgetSignIn();
    this.storage(remember ? 'local' : 'session')?.setItem(SIGNED_IN_KEY, 'true');
  }

  /** At start-up: gets back the session from the refresh cookie, if a sign-in left one. */
  async restore(): Promise<void> {
    if (
      this.storage('session')?.getItem(SIGNED_IN_KEY) ||
      this.storage('local')?.getItem(SIGNED_IN_KEY)
    ) {
      await firstValueFrom(this.refresh());
    }
  }

  /**
   * New access token from the refresh cookie; concurrent callers share one request. Null when the
   * server refuses (session expired or revoked): the user is then signed out here too.
   */
  refresh(): Observable<string | null> {
    this.refreshing ??= this.authService.refresh().pipe(
      map(({ accessToken, user }) => {
        this.session.set({ accessToken, user });
        return accessToken;
      }),
      catchError(() => {
        this.clear();
        return of(null);
      }),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  /** Signs out: revokes the session on the server, forgets it here and in the other tabs, goes home. */
  logout(): void {
    // Sent before forgetting the token; whatever the answer, the session ends here.
    this.authService.logout().subscribe({ error: () => undefined });
    this.channel?.postMessage('logout');
    this.expire();
  }

  /** The session is over (refused by the server, or ended in another tab): back to the home. */
  expire(): void {
    this.clear();
    void this.router.navigateByUrl('/');
  }

  private clear(): void {
    this.session.set(null);
    this.forgetSignIn();
  }

  private forgetSignIn(): void {
    this.storage('session')?.removeItem(SIGNED_IN_KEY);
    this.storage('local')?.removeItem(SIGNED_IN_KEY);
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
