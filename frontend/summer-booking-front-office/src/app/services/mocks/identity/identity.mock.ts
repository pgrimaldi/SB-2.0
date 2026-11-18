import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { UserRole } from '../../../entities/enums/user-role';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { UserItem } from '../../../entities/settings/user-management/user-item';
import { UserSelectedItem } from '../../../entities/settings/user-management/user-selected-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { auditUsers } from '../audit/audit.mock';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { DEMO_PROPERTY } from '../properties/properties.mock';

type UserRow = UserItem & UserSelectedItem & { propertyId: number };
type CreateUserBody = Partial<CreateUserRequest>;
type UpdateUserBody = Partial<UpdateUserRequest>;

const USERS: UserRow[] = [
  user(1, '01', 'giulia.ferri', '3401112201', UserRole.SuperAdmin),
  user(1, '02', 'marco.bianchi', '3401112202', UserRole.Admin),
  user(1, '03', 'sara.conti', '3401112203', UserRole.Manager),
  user(1, '04', 'luca.moretti', '3401112204', UserRole.CashOperator),
  user(1, '05', 'elena.gallo', null, UserRole.DailyCashOperator),
  user(1, '06', 'paolo.costa', '3401112206', UserRole.Lifeguard),
  user(1, '07', 'chiara.fontana', '3401112207', UserRole.AdvancedLifeguard),
  user(1, '08', 'davide.rinaldi', '3401112208', UserRole.Waiter),
  user(1, '09', 'anna.caruso', null, UserRole.Affiliate),
  user(1, '10', 'matteo.lombardi', '3401112210', UserRole.Marketing),
  user(1, '11', 'francesca.greco', '3401112211', UserRole.Waiter),
  user(1, '12', 'simone.marini', '3401112212', UserRole.Lifeguard),
  user(2, '13', 'laura.testa', '3401112213', UserRole.SuperAdmin),
  user(2, '14', 'andrea.villa', '3401112214', UserRole.CashOperator),
];

const MAX_PAGE_SIZE = 100;
const SORT_FIELDS = ['email', 'phone', 'role'] as const;
const PASSWORD_MIN_LENGTH = 8;
const PASSWORD_MAX_LENGTH = 128;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE = /^[0-9 ]{1,32}$/;
const PHONE_PREFIX = /^\+?[0-9]{1,6}$/;
/** `UserRole` lists the roles in the backend's order (`Role.SortOrder`). */
const ROLE_ORDER: readonly (UserRole | null)[] = Object.values(UserRole);
const ASSIGNABLE_ROLES: readonly unknown[] = Object.values(UserRole).filter(
  (role) => role !== UserRole.SuperAdmin && role !== UserRole.WebsiteAdmin,
);

/**
 * `POST /api/identity/users-list`: one page of the users of the property of the token, by role (in
 * the backend's order) then email unless sorted; `role` sorts by the same order. 401 without a valid
 * access token.
 */
export const usersListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = DEMO_PROPERTY;
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = USERS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || [row.email, row.phone].some((text) => text?.toLowerCase().includes(search))),
  ).map((row) => onlyFieldsOf(UserItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  const sign = body.sortDirection === 'Descending' ? -1 : 1;
  if (field && field !== 'role') {
    found.sort((first, second) => sign * compare(first[field], second[field]));
  } else {
    const roleSign = field ? sign : 1;
    found.sort(
      (first, second) =>
        roleSign * (roleRank(first.role) - roleRank(second.role)) ||
        compare(first.email, second.email),
    );
  }
  const answer: Page<UserItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/identity/user-combobox` without a body: the users of the property, id and name, sorted
 * by name; here those that appear in its log (`auditUsers`), the only list the mocks use. 401
 * without a valid access token.
 */
export const userComboboxMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const answer: ComboboxItem[] = [...auditUsers(DEMO_PROPERTY.id)].sort((first, second) =>
    compare(first.value, second.value),
  );
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/identity/create-user` with `CreateUserRequest`: a new user of the property of the token,
 * with the checks of the backend (email, password and role required, password of 8 to 128
 * characters; the list of too common passwords is only in the backend). The password is not kept.
 * 201 with the new user (`UserSelectedItem`); 401 without a valid access token; 400 with one error
 * per wrong field; 409 `account.email_taken` for an email already used by any account; 422
 * `account.role_not_assignable` for a role not given from the panel.
 */
export const createUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateUserBody;
  const errors = userErrors(body, true);
  if (body.role == null) {
    errors.push({ field: 'role', code: ApiErrorCode.ValidationRequired });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  if (!ASSIGNABLE_ROLES.includes(body.role)) {
    return problem(request, 422, ApiErrorCode.AccountRoleNotAssignable, {
      title: 'Role cannot be assigned from the panel',
    });
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, ApiErrorCode.AccountEmailTaken, { title: 'Email already in use' });
  }
  const phone = body.phone?.trim() || null;
  const user: UserRow = {
    propertyId: DEMO_PROPERTY.id,
    idUser: crypto.randomUUID(),
    email,
    emailIsContact: body.emailIsContact === true,
    phoneCountryCode: phone ? body.phoneCountryCode?.trim() || null : null,
    phone,
    phoneIsContact: body.phoneIsContact === true,
    role: body.role ?? null,
  };
  USERS.push(user);
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 201, url: request.url, body: answer })).pipe(delay(600));
};

/**
 * `DELETE /api/identity/property-users/{id}`: the user leaves the property of the token. 204 without
 * a body; 401 without a valid access token; 404 `account.not_found` for an id that is not one of its
 * users; 403 `account.role_not_assignable` for the owner (Titolare), who cannot be deleted from the
 * panel. The backend also refuses the signed-in user (422 `account.cannot_modify_self`), which the mock
 * does not know.
 */
export const deleteUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idUser = decodeURIComponent(request.url.split('/').pop() ?? '');
  const user = USERS.find((row) => row.idUser === idUser && row.propertyId === DEMO_PROPERTY.id);
  if (!user) {
    return accountNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  USERS.splice(USERS.indexOf(user), 1);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/identity/get-selected-user?idUser=…`: the user of the property of the token with every
 * field of the edit popup, never the password. 200 with `UserSelectedItem`; 401 without a valid access
 * token; 404 `account.not_found` for an id that is not one of its users; 403
 * `account.role_not_assignable` for the owner (Titolare), who cannot be managed from the panel.
 */
export const selectedUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idUser = request.params.get('idUser');
  const user = USERS.find((row) => row.idUser === idUser && row.propertyId === DEMO_PROPERTY.id);
  if (!user) {
    return accountNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/identity/update-user` with `UpdateUserRequest`: the user `idUser` of the property of the
 * token takes the new values after the checks of the add (see `userErrors`), but the password is
 * optional: null keeps the saved one, and the mock never keeps it. 200 with the user
 * (`UserSelectedItem`); 401 without a valid access token; 404 `account.not_found` for an id that is
 * not one of its users; 403 `account.role_not_assignable` for the owner; 400 with one error per wrong
 * field; 422 `account.role_not_assignable` for a role not given from the panel; 409
 * `account.email_taken` for an email of another account. The backend also refuses the signed-in
 * user (422 `account.cannot_modify_self`), which the mock does not know.
 */
export const updateUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateUserBody;
  const user = USERS.find(
    (row) => row.idUser === body.idUser && row.propertyId === DEMO_PROPERTY.id,
  );
  if (!user) {
    return accountNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  const errors = userErrors(body, false);
  if (body.role == null) {
    errors.push({ field: 'role', code: ApiErrorCode.ValidationRequired });
  }
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, {
      title: 'Invalid request',
      errors,
    });
  }
  if (!ASSIGNABLE_ROLES.includes(body.role)) {
    return problem(request, 422, ApiErrorCode.AccountRoleNotAssignable, {
      title: 'Role cannot be assigned from the panel',
    });
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row !== user && row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, ApiErrorCode.AccountEmailTaken, { title: 'Email already in use' });
  }
  const phone = body.phone?.trim() || null;
  Object.assign(user, {
    email,
    phone,
    phoneCountryCode: phone ? body.phoneCountryCode?.trim() || null : null,
    role: body.role ?? null,
    emailIsContact: body.emailIsContact === true,
    phoneIsContact: body.phoneIsContact === true,
  });
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(600));
};

/** Only the roles that can be given from the panel: not the owner (Titolare) nor the website admin. */
function isManageable(user: UserRow): boolean {
  return user.role === null || ASSIGNABLE_ROLES.includes(user.role);
}

function accountNotFound(request: HttpRequest<unknown>): Observable<never> {
  return problem(request, 404, ApiErrorCode.AccountNotFound, { title: 'Account not found' });
}

function roleNotAssignable(request: HttpRequest<unknown>): Observable<never> {
  return problem(request, 403, ApiErrorCode.AccountRoleNotAssignable, {
    title: 'Role cannot be managed from the panel',
  });
}

/** As the backend checks it; the password is required only for a new user. */
function userErrors(body: CreateUserBody, isPasswordRequired: boolean): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const email = typeof body.email === 'string' ? body.email.trim() : '';
  if (!email) {
    errors.push({ field: 'email', code: ApiErrorCode.ValidationRequired });
  } else if (!EMAIL.test(email)) {
    errors.push({ field: 'email', code: ApiErrorCode.ValidationEmail });
  }
  const password = typeof body.password === 'string' ? body.password : '';
  if (!password) {
    if (isPasswordRequired) {
      errors.push({ field: 'password', code: ApiErrorCode.ValidationRequired });
    }
  } else if (!password.trim()) {
    errors.push({ field: 'password', code: ApiErrorCode.ValidationRequired });
  } else if (password.length < PASSWORD_MIN_LENGTH) {
    errors.push({
      field: 'password',
      code: ApiErrorCode.ValidationPasswordTooShort,
      args: { min: PASSWORD_MIN_LENGTH },
    });
  } else if (password.length > PASSWORD_MAX_LENGTH) {
    errors.push({
      field: 'password',
      code: ApiErrorCode.ValidationPasswordTooLong,
      args: { max: PASSWORD_MAX_LENGTH },
    });
  }
  const phone = typeof body.phone === 'string' ? body.phone.trim() : '';
  if (phone && !PHONE.test(phone)) {
    errors.push({ field: 'phone', code: ApiErrorCode.ValidationPhone });
  }
  const prefix = typeof body.phoneCountryCode === 'string' ? body.phoneCountryCode.trim() : '';
  if (prefix && !PHONE_PREFIX.test(prefix)) {
    errors.push({ field: 'phoneCountryCode', code: ApiErrorCode.ValidationPhonePrefix });
  }
  return errors;
}

function user(
  propertyId: number,
  number: string,
  emailName: string,
  phone: string | null,
  role: UserRole,
): UserRow {
  return {
    propertyId,
    idUser: `3b8f1c2d-5e6a-4b7c-9d0e-1f2a3b4c5d${number}`,
    email: `${emailName}@example.com`,
    phoneCountryCode: phone ? '39' : null,
    phone,
    role,
    emailIsContact: true,
    phoneIsContact: phone !== null,
  };
}

function roleRank(role: UserRole | null): number {
  const rank = ROLE_ORDER.indexOf(role);
  return rank === -1 ? ROLE_ORDER.length : rank;
}

function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? -1 : 1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base' });
}

function positiveInteger(value: unknown, fallback: number): number {
  return Number.isInteger(value) && (value as number) >= 1 ? (value as number) : fallback;
}
