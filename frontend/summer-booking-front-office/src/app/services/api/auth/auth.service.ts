import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SignInRequest, SignInResponse } from '../../../entities/auth/credentials';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/auth`;

  /** Fails with status 401 when the credentials are not valid. */
  signIn(request: SignInRequest): Observable<SignInResponse> {
    return this.httpClient.post<SignInResponse>(`${this.endpoint}/signin`, request);
  }
}
