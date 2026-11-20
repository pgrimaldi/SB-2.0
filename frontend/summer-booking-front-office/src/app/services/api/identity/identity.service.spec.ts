import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { UserRole } from '../../../entities/enums/user-role';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { UserItem } from '../../../entities/settings/user-management/user-item';
import { UserSelectedItem } from '../../../entities/settings/user-management/user-selected-item';
import { IdentityService } from './identity.service';

describe('IdentityService', () => {
  it('should ask the users of the combobox without a body', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);

    identity.userCombobox().subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/identity/user-combobox`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should send only the fields of the new user, without the property, and read the new user', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new CreateUserRequest(), {
      email: 'nuovo.utente@example.com',
      emailIsContact: true,
      password: 'Sabbia-2026',
      phoneCountryCode: '39',
      phone: '3401112299',
      role: UserRole.Waiter,
      firstName: 'extra-field',
    });

    let created: UserSelectedItem | undefined;

    identity.createUser(request).subscribe((answer) => (created = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/identity/create-user`);
    expect(call.request.body).toEqual({
      email: 'nuovo.utente@example.com',
      emailIsContact: true,
      password: 'Sabbia-2026',
      phoneCountryCode: '39',
      phone: '3401112299',
      phoneIsContact: false,
      role: UserRole.Waiter,
    });
    const answer = Object.assign(new UserSelectedItem(), {
      idUser: 'u9',
      email: 'nuovo.utente@example.com',
      role: UserRole.Waiter,
    });
    call.flush(answer, { status: 201, statusText: 'Created' });
    expect(created).toEqual(answer);
    controller.verify();
  });

  it('should send the edited user with its id, null password included, and the version read, and answer the new version', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    const request = Object.assign(new UpdateUserRequest(), {
      idUser: 'u7',
      email: 'utente.modificato@example.com',
      phoneIsContact: true,
      phoneCountryCode: '41',
      phone: '3401112277',
      role: UserRole.Manager,
    });

    let version: string | null | undefined;

    identity.updateUser(request, 'v1').subscribe((answer) => (version = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/identity/update-user`);
    expect(call.request.body).toEqual({
      idUser: 'u7',
      email: 'utente.modificato@example.com',
      emailIsContact: false,
      password: null,
      phoneCountryCode: '41',
      phone: '3401112277',
      phoneIsContact: true,
      role: UserRole.Manager,
    });
    expect(call.request.headers.get('If-Match')).toBe('"v1"');
    call.flush({ idUser: 'u7', role: UserRole.Manager }, { headers: { ETag: '"v2"' } });
    expect(version).toBe('v2');
    controller.verify();
  });

  it('should ask for the user to edit by its id in the query string and read it', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    let selected: UserSelectedItem | undefined;

    identity.selectedUser('u7').subscribe((answer) => (selected = answer));

    const call = controller.expectOne(
      ({ method, url, params }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/identity/get-selected-user` &&
        params.get('idUser') === 'u7',
    );
    expect(call.request.body).toBeNull();
    const answer = Object.assign(new UserSelectedItem(), { idUser: 'u7', role: UserRole.Waiter });
    call.flush(answer);
    expect(selected).toEqual(answer);
    controller.verify();
  });

  it('should delete the user by its id in the path, without a body', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);

    identity.deleteUser('u7 8').subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/identity/property-users/u7%208`,
    ).request;
    expect(request.method).toBe('DELETE');
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask one page of the users with the request of the table and read the page', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const identity = TestBed.inject(IdentityService);
    const controller = TestBed.inject(HttpTestingController);
    const pageRequest: PageRequest = {
      page: 2,
      pageSize: 10,
      search: 'ferri',
      filters: [],
      sortField: 'role',
      sortDirection: 'Ascending',
    };
    const page: Page<UserItem> = {
      total: 11,
      rows: [
        Object.assign(new UserItem(), {
          idUser: 'u1',
          email: 'giulia.ferri@example.com',
          role: UserRole.Admin,
        }),
      ],
    };

    let answer: Page<UserItem> | undefined;

    identity.usersList(pageRequest).subscribe((value) => (answer = value));

    const call = controller.expectOne(`${environment.apiBaseUrl}/identity/users-list`);
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual(pageRequest);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });
});
