import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { LoginRequest } from '../../../entities/auth/credentials';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  it('should sign in with only the fields of the form and the app of the build', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const auth = TestBed.inject(AuthService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new LoginRequest(), {
      username: 'giulia.ferri@example.com',
      password: 'Prova-Accesso-1',
      remember: true,
      unexpectedFlag: 'extra-field',
    });

    auth.login(request).subscribe();

    const call = controller.expectOne(`${environment.apiBaseUrl}/identity/auth/login`).request;
    expect(call.method).toBe('POST');
    expect(call.body).toEqual({
      username: 'giulia.ferri@example.com',
      password: 'Prova-Accesso-1',
      remember: true,
      app: environment.authApp,
    });
    controller.verify();
  });

  it('should sign out with a POST without a body, the refresh token travelling in its cookie', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const auth = TestBed.inject(AuthService);
    const controller = TestBed.inject(HttpTestingController);

    auth.logout().subscribe();

    const call = controller.expectOne(`${environment.apiBaseUrl}/identity/auth/logout`).request;
    expect(call.method).toBe('POST');
    expect(call.body).toBeNull();
    controller.verify();
  });
});
