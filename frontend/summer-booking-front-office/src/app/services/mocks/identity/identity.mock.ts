import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { UserRole } from '../../../entities/enums/user-role';
import { DEFAULT_PAGE_SIZE, Page, PageRequest } from '../../../entities/pagination/page';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
import { DeleteUserRequest } from '../../../entities/settings/user-management/delete-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { UserItem } from '../../../entities/settings/user-management/user-item';
import { UserSelectedItem } from '../../../entities/settings/user-management/user-selected-item';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, forbidden, problem, resourceNotFound } from '../errors/problem.mock';
import { propertyOf } from '../properties/properties.mock';

type UserRow = UserItem & UserSelectedItem & { propertyId: number };
type CreateUserBody = Partial<CreateUserRequest> & { idProperty?: unknown };
type DeleteUserBody = Partial<DeleteUserRequest>;
type UpdateUserBody = Partial<UpdateUserRequest> & { idProperty?: unknown };

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
const PHONE = /^\+?[0-9 ]{6,20}$/;
const ASSIGNABLE_ROLES: readonly unknown[] = Object.values(UserRole).filter(
  (role) => role !== UserRole.SuperAdmin && role !== UserRole.WebsiteAdmin,
);

/**
 * `POST /api/identity/users-list`: one page of the users of the requested property. 401 without a
 * valid access token; 403 for a missing or unknown property.
 */
export const usersListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const page = positiveInteger(body.page, 1);
  const pageSize = Math.min(positiveInteger(body.pageSize, DEFAULT_PAGE_SIZE), MAX_PAGE_SIZE);
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = USERS.filter(
    (row) =>
      row.propertyId === property.id &&
      (!search || [row.email, row.phone].some((text) => text?.toLowerCase().includes(search))),
  ).map((row) => onlyFieldsOf(UserItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  if (field) {
    const sign = body.sortDirection === 'Descending' ? -1 : 1;
    found.sort((first, second) => sign * compare(first[field], second[field]));
  }
  const answer: Page<UserItem> = {
    total: found.length,
    rows: found.slice((page - 1) * pageSize, page * pageSize),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(150));
};

/**
 * `POST /api/identity/create-user` with `CreateUserRequest` and `idProperty`: a new user of the
 * property, with the checks of the backend (email and password required, password of 8 to 128
 * characters; the list of too common passwords is only in the backend). The password is not kept.
 * 204 without a body; 401 without a valid access token; 403 for an unknown property; 400 with one
 * error per wrong field; 409 `account.email_taken` for an email already used by any account.
 */
export const createUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateUserBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const errors = userErrors(body, true);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, 'account.email_taken', { title: 'Email already in use' });
  }
  USERS.push({
    propertyId: property.id,
    idUser: crypto.randomUUID(),
    email,
    phoneCountryCode: null,
    phone: body.phone?.trim() || null,
    role: body.role ?? null,
    isEmailActive: body.isEmailActive === true,
    isPhoneActive: body.isPhoneActive === true,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `POST /api/identity/delete-user` with `DeleteUserRequest`: the user leaves the property. 204 without
 * a body; 401 without a valid access token; 403 for an unknown property; 404 `resource.not_found` for
 * an id that is not one of its users; 403 `account.role_not_assignable` for the owner (Titolare), who
 * cannot be deleted from the panel. The backend also refuses the signed-in user (422
 * `account.cannot_modify_self`), which the mock does not know.
 */
export const deleteUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as DeleteUserBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const user = USERS.find((row) => row.idUser === body.idUser && row.propertyId === property.id);
  if (!user) {
    return resourceNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  USERS.splice(USERS.indexOf(user), 1);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/identity/get-selected-user?idUser=…`: the user with every field of the edit popup, never
 * the password. 200 with `UserSelectedItem`; 401 without a valid access token; 404
 * `resource.not_found` for an unknown id; 403 `account.role_not_assignable` for the owner (Titolare),
 * who cannot be managed from the panel.
 */
export const selectedUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const idUser = request.params.get('idUser');
  const user = USERS.find((row) => row.idUser === idUser);
  if (!user) {
    return resourceNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/identity/update-user` with `UpdateUserRequest` and `idProperty`: the user `idUser` takes
 * the new values after the checks of the add (see `userErrors`), but the password is optional: null
 * keeps the saved one, and the mock never keeps it. 204 without a body; 401 without a valid access
 * token; 403 for an unknown property; 404 `resource.not_found` for an id that is not one of its users;
 * 403 `account.role_not_assignable` for the owner; 400 with one error per wrong field; 409
 * `account.email_taken` for an email of another account.
 */
export const updateUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateUserBody;
  const property = propertyOf(body.idProperty);
  if (!property) {
    return forbidden(request);
  }
  const user = USERS.find((row) => row.idUser === body.idUser && row.propertyId === property.id);
  if (!user) {
    return resourceNotFound(request);
  }
  if (!isManageable(user)) {
    return roleNotAssignable(request);
  }
  const errors = userErrors(body, false);
  if (errors.length) {
    return problem(request, 400, 'validation.failed', { title: 'Invalid request', errors });
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row !== user && row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, 'account.email_taken', { title: 'Email already in use' });
  }
  const phone = body.phone?.trim() || null;
  Object.assign(user, {
    email,
    phone,
    phoneCountryCode: phone ? user.phoneCountryCode : null,
    role: body.role ?? null,
    isEmailActive: body.isEmailActive === true,
    isPhoneActive: body.isPhoneActive === true,
  });
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/** Only the roles that can be given from the panel: not the owner (Titolare) nor the website admin. */
function isManageable(user: UserRow): boolean {
  return user.role === null || ASSIGNABLE_ROLES.includes(user.role);
}

function roleNotAssignable(request: HttpRequest<unknown>): Observable<never> {
  return problem(request, 403, 'account.role_not_assignable', {
    title: 'Role cannot be managed from the panel',
  });
}

/** As the backend checks it; the password is required only for a new user. */
function userErrors(body: CreateUserBody, isPasswordRequired: boolean): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const email = typeof body.email === 'string' ? body.email.trim() : '';
  if (!email) {
    errors.push({ field: 'email', code: 'validation.required' });
  } else if (!EMAIL.test(email)) {
    errors.push({ field: 'email', code: 'validation.email' });
  }
  const password = typeof body.password === 'string' ? body.password : '';
  if (!password) {
    if (isPasswordRequired) {
      errors.push({ field: 'password', code: 'validation.required' });
    }
  } else if (!password.trim()) {
    errors.push({ field: 'password', code: 'validation.required' });
  } else if (password.length < PASSWORD_MIN_LENGTH) {
    errors.push({
      field: 'password',
      code: 'validation.password_too_short',
      args: { min: PASSWORD_MIN_LENGTH },
    });
  } else if (password.length > PASSWORD_MAX_LENGTH) {
    errors.push({
      field: 'password',
      code: 'validation.password_too_long',
      args: { max: PASSWORD_MAX_LENGTH },
    });
  }
  const phone = typeof body.phone === 'string' ? body.phone.trim() : '';
  if (phone && !PHONE.test(phone)) {
    errors.push({ field: 'phone', code: 'validation.phone' });
  }
  if (body.role != null && !ASSIGNABLE_ROLES.includes(body.role)) {
    errors.push({ field: 'role', code: 'validation.invalid' });
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
    phoneCountryCode: phone ? '+39' : null,
    phone,
    role,
    isEmailActive: true,
    isPhoneActive: phone !== null,
  };
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
