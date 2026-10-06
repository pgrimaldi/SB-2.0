import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  catchError,
  defer,
  finalize,
  firstValueFrom,
  map,
  of,
  retry,
  shareReplay,
  throwError,
  timeout,
  timer,
} from 'rxjs';
import { AuthSession, AuthUser, SignInRequest } from '../../entities/auth/credentials';
import { AuthService } from '../../services/api/auth/auth.service';
import { AUTH_LOCK } from './auth-lock';

/**
 * Non-secret mark that a sign-in happened in this browser, so that start-up asks for a new access
 * token only when a refresh cookie may exist (anonymous visitors make no request). It follows
 * "remember me": sessionStorage (ends with the browser) or localStorage.
 */
const SIGNED_IN_KEY = 'sb.signed-in';
/**
 * Random, non-secret name of the session that owns the refresh cookie of this browser, written by
 * the tab that signed in. Shared by every tab (localStorage) whatever "remember me" says, as the
 * cookie is: the tabs of that session take it at start-up, and a tab that finds another name there
 * knows the cookie is no longer its own.
 */
const SESSION_NAME_KEY = 'sb.session-name';
/** Where older versions kept the token (readable by scripts): removed at start-up. */
const LEGACY_SESSION_KEY = 'sb.session';
const CHANNEL_NAME = 'sb-auth';
export const LOGOUT_RETRY_DELAY = 1000;
/** A call on the refresh cookie that has not answered by then fails, and the next one can start. */
export const COOKIE_CALL_TIMEOUT = 15_000;

interface Session {
  accessToken: string;
  user: AuthUser;
}

/** Between the tabs: always for one session, so an old message cannot reach a newer one. */
interface TabMessage {
  type: 'signin' | 'logout';
  session: string;
}

type LogoutOutcome = 'revoked' | 'failed' | 'gone';

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
  private readonly lock = inject(AUTH_LOCK);

  private readonly session = signal<Session | null>(null);
  private readonly channel =
    typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
  private refreshing: Observable<string | null> | null = null;
  /**
   * Goes up whenever the session changes hands (sign-in, sign-out, expiry, sign-out in another tab),
   * not with a normal refresh: an answer that arrives later for an older generation is ignored.
   */
  private generation = 0;
  /** See SESSION_NAME_KEY; null while this tab has no session. */
  private sessionName: string | null = null;

  /**
   * The same while the same person works on the same property: a refresh of the token gives a new
   * object with the same values, which must change nothing in the pages (what is typed stays, tables
   * keep their page).
   */
  readonly user = computed(() => this.session()?.user ?? null, { equal: sameUser });

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
    return this.oneAtATime(() => this.authService.signIn(request)).pipe(
      map((session) => this.start(session, request.remember)),
    );
  }

  /**
   * After sign-in: the server has already set the refresh cookie, so the other tabs leave the session
   * they had (see `leaveHere`).
   */
  start({ accessToken, user }: AuthSession, remember: boolean): void {
    this.changeHands();
    this.session.set({ accessToken, user });
    this.forgetSignIn();
    this.storage(remember ? 'local' : 'session')?.setItem(SIGNED_IN_KEY, 'true');
    this.sessionName = crypto.randomUUID();
    this.storage('local')?.setItem(SESSION_NAME_KEY, this.sessionName);
    this.tell('signin');
  }

  /**
   * Called at start-up. When the server cannot be reached the app opens without a session and keeps
   * the marks: the next visit tries again.
   */
  async restore(): Promise<void> {
    if (
      this.storage('session')?.getItem(SIGNED_IN_KEY) ||
      this.storage('local')?.getItem(SIGNED_IN_KEY)
    ) {
      await firstValueFrom(this.refresh()).catch(() => undefined);
    }
  }

  /**
   * New access token from the refresh cookie; concurrent callers share one request. Null when the
   * server refuses (session expired or revoked): the user is then signed out here and in the other
   * tabs of the session, and goes back to the home if a session was in use (at start-up nobody was
   * signed in yet). Null when the cookie
   * now belongs to another user or property: this tab leaves (see `leaveHere`). Null also when
   * the session changed hands meanwhile: the answer belongs to the old one and changes nothing, so a
   * late answer can neither reopen a closed session nor replace or close the next one. A refresh
   * that waited for a sign-in or a logout asks the server nothing once the session has changed hands,
   * nor when the cookie now belongs to another session (this tab leaves), nor, at start-up, when
   * someone signed in in another tab while this one was opening (not the session it was opening).
   * Fails, changing nothing, when the server cannot be reached (offline, server down, time limit):
   * only the server's refusal ends a session.
   */
  refresh(): Observable<string | null> {
    const generation = this.generation;
    const ownerAtStart = this.cookieOwner();
    const refreshing = (this.refreshing ??= this.oneAtATime(() => {
      if (generation !== this.generation) {
        return of(null);
      }
      if (!this.isAuthenticated() && this.cookieOwner() !== ownerAtStart) {
        this.storage('session')?.removeItem(SIGNED_IN_KEY);
        return of(null);
      }
      if (!this.ownsCookie()) {
        this.leaveHere();
        return of(null);
      }
      return this.authService.refresh();
    }).pipe(
      map((answer) => {
        if (!answer || generation !== this.generation) {
          return null;
        }
        const { accessToken, user } = answer;
        const current = this.user();
        if (current && (current.email !== user.email || current.idProperty !== user.idProperty)) {
          this.leaveHere();
          return null;
        }
        this.session.set({ accessToken, user });
        if (!this.sessionName) {
          // A tab that opens on a session started elsewhere (or by an older version, without a name).
          this.sessionName = this.cookieOwner() ?? crypto.randomUUID();
          this.storage('local')?.setItem(SESSION_NAME_KEY, this.sessionName);
        }
        return accessToken;
      }),
      catchError((error: unknown) => {
        if (!isSessionOver(error)) {
          return throwError(() => error);
        }
        if (generation === this.generation) {
          if (this.isAuthenticated()) {
            this.tell('logout');
            this.expire();
          } else {
            this.clear();
          }
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
   * Both tries run in one turn of the lock and only for the session that asked: once it has changed
   * hands, or the cookie belongs to another session, nothing more is sent (the cookie could be that
   * of the new session) and the result changes nothing.
   */
  async logout(): Promise<boolean> {
    const generation = this.generation;
    const name = this.sessionName;
    const stillOurs = () => generation === this.generation && this.ownsCookie();
    const outcome = await firstValueFrom(
      this.oneAtATime(() =>
        defer(() => (stillOurs() ? this.authService.logout() : of(undefined))).pipe(
          retry({
            count: 1,
            delay: (error) =>
              isSessionOver(error) ? throwError(() => error) : timer(LOGOUT_RETRY_DELAY),
          }),
          map((): LogoutOutcome => (stillOurs() ? 'revoked' : 'gone')),
          catchError((error) => of<LogoutOutcome>(isSessionOver(error) ? 'revoked' : 'failed')),
        ),
      ).pipe(catchError(() => of<LogoutOutcome>('failed'))),
    );
    if (outcome === 'failed') {
      return false;
    }
    if (outcome === 'revoked' && generation === this.generation) {
      this.tell('logout', name);
      this.expire();
    } else if (generation === this.generation) {
      this.leaveHere();
    }
    return true;
  }

  /** The session is over (refused by the server, or ended in another tab): back to the home. */
  expire(): void {
    this.clear();
    void this.router.navigateByUrl('/');
  }

  /**
   * The mark of this tab always goes. What the whole browser shares (the remembered mark, the session
   * name) goes only while the cookie is still this session's: another tab may have signed in since,
   * and that session must still open on the next visit.
   */
  private clear(): void {
    const ownsCookie = this.ownsCookie();
    this.changeHands();
    this.session.set(null);
    this.sessionName = null;
    this.storage('session')?.removeItem(SIGNED_IN_KEY);
    if (ownsCookie) {
      this.storage('local')?.removeItem(SIGNED_IN_KEY);
      this.storage('local')?.removeItem(SESSION_NAME_KEY);
    }
  }

  /**
   * False when another tab has signed in since: the cookie is then that session's. True without a
   * name to compare (start-up, storage blocked): nothing says otherwise.
   */
  private ownsCookie(): boolean {
    const owner = this.cookieOwner();
    return !this.sessionName || !owner || owner === this.sessionName;
  }

  private cookieOwner(): string | null {
    return this.storage('local')?.getItem(SESSION_NAME_KEY) ?? null;
  }

  private tell(type: TabMessage['type'], session = this.sessionName): void {
    if (session) {
      this.channel?.postMessage({ type, session } satisfies TabMessage);
    }
  }

  /**
   * Runs a call on the refresh cookie once no other one is running, in this tab or another (see
   * AuthLock), and lets the next one start when it answers, fails, is cancelled or times out.
   */
  private oneAtATime<T>(call: () => Observable<T>): Observable<T> {
    return new Observable<T>((subscriber) => {
      const abort = new AbortController();
      this.lock
        .run(
          () =>
            new Promise<void>((release) => {
              if (subscriber.closed) {
                release();
                return;
              }
              subscriber.add(
                call().pipe(timeout(COOKIE_CALL_TIMEOUT), finalize(release)).subscribe(subscriber),
              );
            }),
          abort.signal,
        )
        .catch((error: unknown) => {
          if (!abort.signal.aborted) {
            subscriber.error(error);
          }
        });
      return () => abort.abort();
    });
  }

  /**
   * Someone signed in in another tab, or the cookie now belongs to someone else: this tab must not go
   * on with an identity the user did not choose here. It drops its session and goes back to the home,
   * where signing in is needed again, unless the browser remembers the new session ("remember me"),
   * as for any new tab. Without asking the server to log out and without removing what is shared by
   * the whole browser (the remembered mark, the session name): they belong to the other session.
   * Only the mark of this tab goes, so that reloading it does not open the other session.
   */
  private leaveHere(): void {
    this.changeHands();
    this.session.set(null);
    this.sessionName = null;
    this.storage('session')?.removeItem(SIGNED_IN_KEY);
    void this.router.navigateByUrl('/');
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

  private readonly otherTabMessage = ({ data }: MessageEvent<Partial<TabMessage>>): void => {
    if (!this.isAuthenticated() || !data.session) {
      return;
    }
    // Messages arrive later than the calls on the cookie: a sign-in counts only while its session
    // still owns the cookie, or an older one could make a newer session leave.
    const owner = this.cookieOwner();
    if (data.type === 'logout' && data.session === this.sessionName) {
      this.expire();
    } else if (
      data.type === 'signin' &&
      data.session !== this.sessionName &&
      (!owner || owner === data.session)
    ) {
      this.leaveHere();
    }
  };

  ngOnDestroy(): void {
    this.channel?.removeEventListener('message', this.otherTabMessage);
    this.channel?.close();
  }
}

function sameUser(a: AuthUser | null, b: AuthUser | null): boolean {
  return (
    a === b ||
    (!!a &&
      !!b &&
      a.email === b.email &&
      a.idProperty === b.idProperty &&
      a.roles.length === b.roles.length &&
      a.roles.every((role, index) => role === b.roles[index]))
  );
}

/** 401 from refresh or logout: the server has no valid session for this browser any more. */
function isSessionOver(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === 401;
}
