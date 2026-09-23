import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly authenticated = signal(false);

  readonly isAuthenticated = this.authenticated.asReadonly();
}
