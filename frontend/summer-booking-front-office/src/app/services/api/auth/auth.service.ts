import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthSession, SignInRequest } from '../../../entities/auth/credentials';

/**
 * Authentication API. The refresh token travels only in the HttpOnly cookie the server sets on
 * sign-in and refresh (same origin: the browser sends it by itself) and clears on logout.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/auth`;

  /** Fails with status 401 when the credentials are not valid. */
  signIn(request: SignInRequest): Observable<AuthSession> {
    return this.httpClient.post<AuthSession>(`${this.endpoint}/signin`, request);
  }

  /** New access token (and rotated refresh cookie); fails with 401 when there is no valid session. */
  refresh(): Observable<AuthSession> {
    return this.httpClient.post<AuthSession>(`${this.endpoint}/refresh`, null);
  }

  /**
   * Revokes the session of the refresh cookie on the server and clears the cookie; 401 when there is
   * no valid session any more (already signed out).
   */
  logout(): Observable<void> {
    return this.httpClient.post<void>(`${this.endpoint}/logout`, null);
  }
}
