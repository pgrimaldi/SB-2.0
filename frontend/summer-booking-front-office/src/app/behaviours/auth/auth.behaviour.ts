import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  finalize,
  firstValueFrom,
  map,
  of,
  retry,
  shareReplay,
  throwError,
  timer,
} from 'rxjs';
import { AuthSession, AuthUser, SignInRequest } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';

/**
 * Non-secret mark that a sign-in happened in this browser, so that start-up asks for a new access
 * token only when a refresh cookie may exist (anonymous visitors make no request). It follows
 * "remember me": sessionStorage (ends with the browser) or localStorage.
 */
const SIGNED_IN_KEY = 'sb.signed-in';
/** Where older versions kept the token (readable by scripts): removed at start-up. */
const LEGACY_SESSION_KEY = 'sb.session';
const CHANNEL_NAME = 'sb-auth';
export const LOGOUT_RETRY_DELAY = 1000;

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
 * says. Logout ends the session only once the server has revoked it (OWASP: server-side
 * invalidation), then in every tab.
 */
@Injectable({ providedIn: 'root' })
export class AuthBehaviour implements OnDestroy {
  private readonly window = inject(DOCUMENT).defaultView;
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  private readonly session = signal<Session | null>(null);
  private readonly channel =
    typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
  private refreshing: Observable<string | null> | null = null;
  /**
   * Goes up whenever the session changes hands (sign-in, sign-out, expiry, sign-out in another tab),
   * not with a normal refresh: an answer that arrives later for an older generation is ignored.
   */
  private generation = 0;

  readonly user = computed(() => this.session()?.user ?? null);

  constructor() {
    this.storage('session')?.removeItem(LEGACY_SESSION_KEY);
    this.storage('local')?.removeItem(LEGACY_SESSION_KEY);
    this.channel?.addEventListener('message', this.otherTabMessage);
  }

  isAuthenticated(): boolean {
    return this.session() !== null;
  }

  token(): string | null {
    return this.session()?.accessToken ?? null;
  }

  /** For whoever sends a request with the token: is the answer still about the same session? */
  sessionGeneration(): number {
    return this.generation;
  }

  signIn(request: SignInRequest): Observable<void> {
    return this.authService
      .signIn(request)
      .pipe(map((session) => this.start(session, request.remember)));
  }

  /** After sign-in: the server has already set the refresh cookie. */
  start({ accessToken, user }: AuthSession, remember: boolean): void {
    this.changeHands();
    this.session.set({ accessToken, user });
    this.forgetSignIn();
    this.storage(remember ? 'local' : 'session')?.setItem(SIGNED_IN_KEY, 'true');
  }

  /** Called at start-up. */
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
   * server refuses (session expired or revoked): the user is then signed out here too. Null also when
   * the session changed hands meanwhile: the answer belongs to the old one and changes nothing, so a
   * late answer can neither reopen a closed session nor replace or close the next one.
   */
  refresh(): Observable<string | null> {
    const generation = this.generation;
    const refreshing = (this.refreshing ??= this.authService.refresh().pipe(
      map(({ accessToken, user }) => {
        if (generation !== this.generation) {
          return null;
        }
        this.session.set({ accessToken, user });
        return accessToken;
      }),
      catchError(() => {
        if (generation === this.generation) {
          this.clear();
        }
        return of(null);
      }),
      finalize(() => {
        if (this.refreshing === refreshing) {
          this.refreshing = null;
        }
      }),
      shareReplay(1),
    ));
    return refreshing;
  }

  /**
   * Signs out: asks the server to revoke the session (one more try after a failure). Only when the
   * server confirms, or answers that there is no session left (401), the session ends here and in the
   * other tabs and the user goes home. Resolves false when the server could not be reached: the user
   * is then still signed in, as the session is still valid on the server.
   */
  async logout(): Promise<boolean> {
    const revoked = await firstValueFrom(
      this.authService.logout().pipe(
        retry({
          count: 1,
          delay: (error) =>
            isSessionOver(error) ? throwError(() => error) : timer(LOGOUT_RETRY_DELAY),
        }),
        map(() => true),
        catchError((error) => of(isSessionOver(error))),
      ),
    );
    if (revoked) {
      this.channel?.postMessage('logout');
      this.expire();
    }
    return revoked;
  }

  /** The session is over (refused by the server, or ended in another tab): back to the home. */
  expire(): void {
    this.clear();
    void this.router.navigateByUrl('/');
  }

  private clear(): void {
    this.changeHands();
    this.session.set(null);
    this.forgetSignIn();
  }

  /** A refresh still running belongs to the session before: the next one asks again. */
  private changeHands(): void {
    this.generation++;
    this.refreshing = null;
  }

  private forgetSignIn(): void {
    this.storage('session')?.removeItem(SIGNED_IN_KEY);
    this.storage('local')?.removeItem(SIGNED_IN_KEY);
  }

  /** Undefined when storage is blocked (e.g. private mode). */
  private storage(kind: 'session' | 'local'): Storage | undefined {
    try {
      return kind === 'local' ? this.window?.localStorage : this.window?.sessionStorage;
    } catch {
      return undefined;
    }
  }

  private readonly otherTabMessage = ({ data }: MessageEvent): void => {
    if (data === 'logout' && this.isAuthenticated()) {
      this.expire();
    }
  };

  ngOnDestroy(): void {
    this.channel?.removeEventListener('message', this.otherTabMessage);
    this.channel?.close();
  }
}

/** 401 from the logout: the server has no valid session for this browser any more. */
function isSessionOver(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}
