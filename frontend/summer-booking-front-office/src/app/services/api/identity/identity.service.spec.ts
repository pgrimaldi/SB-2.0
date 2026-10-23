import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { UserRole } from '../../../entities/enums/user-role';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { IdentityService } from './identity.service';

describe('IdentityService', () => {
  it('should send only the fields of the new user, with the property of the session', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new CreateUserRequest(), {
      email: 'nuovo.utente@example.com',
      isEmailActive: true,
      password: 'Sabbia-2026',
      phone: '3401112299',
      role: UserRole.Waiter,
      idProperty: 'p2',
      firstName: 'extra-field',
    });

    identity.createUser('p1', request).subscribe();

    const body = controller.expectOne(`${environment.apiBaseUrl}/identity/create-user`).request
      .body;
    expect(body).toEqual({
      email: 'nuovo.utente@example.com',
      isEmailActive: true,
      password: 'Sabbia-2026',
      phone: '3401112299',
      isPhoneActive: false,
      role: UserRole.Waiter,
      idProperty: 'p1',
    });
    controller.verify();
  });

  it('should send the edited user with its id, null password included, to update-user', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new UpdateUserRequest(), {
      idUser: 'u7',
      email: 'utente.modificato@example.com',
      isPhoneActive: true,
      phone: '3401112277',
      role: UserRole.Manager,
      idProperty: 'p2',
    });

    identity.updateUser('p1', request).subscribe();

    const body = controller.expectOne(`${environment.apiBaseUrl}/identity/update-user`).request
      .body;
    expect(body).toEqual({
      idUser: 'u7',
      email: 'utente.modificato@example.com',
      isEmailActive: false,
      password: null,
      phone: '3401112277',
      isPhoneActive: true,
      role: UserRole.Manager,
      idProperty: 'p1',
    });
    controller.verify();
  });

  it('should send the property and the user to delete-user', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);

    identity.deleteUser({ idProperty: 'p1', idUser: 'u7' }).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/identity/delete-user`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idProperty: 'p1', idUser: 'u7' });
    controller.verify();
  });
});
