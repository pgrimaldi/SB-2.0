import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { isEmailAddress } from '../../../components/shared/forms/validators';
import { TABLE_PAGE_SIZE_OPTIONS } from '../../../components/shared/ui/tables/table/table';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { UserRole } from '../../../entities/enums/user-role';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { Page, PageRequest, SortDirection } from '../../../entities/pagination/page';
import {
  CreateUserRequest,
  PASSWORD_MAX_LENGTH,
  PASSWORD_MIN_LENGTH,
  PHONE_MAX_LENGTH,
} from '../../../entities/settings/user-management/create-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { UserItem } from '../../../entities/settings/user-management/user-item';
import { UserSelectedItem } from '../../../entities/settings/user-management/user-selected-item';
import { MAX_EMAIL_LENGTH } from '../../../entities/shared/email-address';
import { onlyFieldsOf } from '../../api/only-fields-of';
import { auditUsers } from '../audit/audit.mock';
import { MOCK_SIGNED_IN_USER, isAuthorized, unauthorized } from '../auth/auth.mock';
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
  user(1, '15', 'sito.web', null, UserRole.WebsiteAdmin),
  {
    propertyId: DEMO_PROPERTY.id,
    idUser: MOCK_SIGNED_IN_USER.id,
    email: MOCK_SIGNED_IN_USER.email,
    phoneCountryCode: null,
    phone: null,
    role: MOCK_SIGNED_IN_USER.role,
    emailIsContact: true,
    phoneIsContact: false,
  },
  user(2, '13', 'laura.testa', '3401112213', UserRole.SuperAdmin),
  user(2, '14', 'andrea.villa', '3401112214', UserRole.CashOperator),
];

const SORT_FIELDS = ['email', 'phone', 'role'] as const;
const SORT_DIRECTIONS: readonly SortDirection[] = ['Ascending', 'Descending'];
/** As the backend (`CommonRules.PhonePrefix`). */
const PHONE_PREFIX_MAX_LENGTH = 8;
const PHONE = /^[0-9 ]*$/;
const PHONE_PREFIX = /^\+?[0-9]{1,6}$/;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
/** `UserRole` lists the roles in the backend's order (`Role.SortOrder`). */
const ROLE_ORDER: readonly (UserRole | null)[] = Object.values(UserRole);
const ASSIGNABLE_ROLES: readonly unknown[] = Object.values(UserRole).filter(
  (role) => role !== UserRole.SuperAdmin && role !== UserRole.WebsiteAdmin,
);

/**
 * `POST /api/identity/users-list`: one page of the users of the property of the token, the signed-in
 * one and the owner too, by role (in the backend's order) then email unless sorted; `role` sorts by
 * the same order, `null` values go last ascending and first descending (PostgreSQL), equal values by
 * email. 400 `validation.invalid` on `page`, `pageSize`, `sortField`, `sortDirection` or `filters`
 * out of the table's rules; 401 without a valid access token.
 */
export const usersListMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<PageRequest>;
  const errors = tableRequestErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const { page = 1, pageSize = TABLE_PAGE_SIZE_OPTIONS[0], sortDirection } = body;
  const search = typeof body.search === 'string' ? body.search.trim().toLowerCase() : '';
  const found = USERS.filter(
    (row) =>
      row.propertyId === DEMO_PROPERTY.id &&
      (!search || [row.email, row.phone].some((text) => text?.toLowerCase().includes(search))),
  ).map((row) => onlyFieldsOf(UserItem, row));
  const field = SORT_FIELDS.find((name) => name === body.sortField);
  const sign = field && sortDirection === 'Descending' ? -1 : 1;
  found.sort(
    (first, second) =>
      sign *
        (field && field !== 'role'
          ? compare(first[field], second[field])
          : roleRank(first.role) - roleRank(second.role)) || compare(first.email, second.email),
  );
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
 * with the checks of the backend (see `userErrors`; the list of too common passwords is only in the
 * backend). The password is not kept. In this order: 400 with one error per wrong field; 422
 * `account.role_not_assignable` for a role not given from the panel; 409 `account.email_taken` for an
 * email already used by any account; 201 with the new user (`UserSelectedItem`). 401 without a valid
 * access token.
 */
export const createUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as CreateUserBody;
  const errors = userErrors(body, true);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  if (!ASSIGNABLE_ROLES.includes(body.role)) {
    return problem(request, 422, ApiErrorCode.AccountRoleNotAssignable);
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, ApiErrorCode.AccountEmailTaken);
  }
  const phone = body.phone?.trim() || null;
  const user: UserRow = {
    propertyId: DEMO_PROPERTY.id,
    idUser: crypto.randomUUID(),
    email,
    emailIsContact: body.emailIsContact === true,
    phoneCountryCode: phone ? prefixOf(body.phoneCountryCode) : null,
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
 * a body; 401 without a valid access token; then the checks of `manageableUser`.
 */
export const deleteUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const user = manageableUser(request, decodeURIComponent(request.url.split('/').pop() ?? ''));
  if (user instanceof Observable) {
    return user;
  }
  USERS.splice(USERS.indexOf(user), 1);
  return of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(600));
};

/**
 * `GET /api/identity/get-selected-user?idUser=…`: the user of the property of the token with every
 * field of the edit popup, never the password. 200 with `UserSelectedItem`; 401 without a valid access
 * token; then the checks of `manageableUser`.
 */
export const selectedUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const user = manageableUser(request, request.params.get('idUser'));
  if (user instanceof Observable) {
    return user;
  }
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(300));
};

/**
 * `POST /api/identity/update-user` with `UpdateUserRequest`: the user `idUser` of the property of the
 * token takes the new values; the password is optional: null keeps the saved one, and the mock never
 * keeps it. In this order: 400 with one error per wrong field (see `userErrors`); the checks of
 * `manageableUser`; 409 `account.email_taken` for an email of another account; 422
 * `account.role_not_assignable` for a role not given from the panel; 200 with the user
 * (`UserSelectedItem`). 401 without a valid access token.
 */
export const updateUserMock = (request: HttpRequest<unknown>): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as UpdateUserBody;
  const errors = userErrors(body, false);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const user = manageableUser(request, body.idUser);
  if (user instanceof Observable) {
    return user;
  }
  const email = (body.email ?? '').trim();
  if (USERS.some((row) => row !== user && row.email?.toLowerCase() === email.toLowerCase())) {
    return problem(request, 409, ApiErrorCode.AccountEmailTaken);
  }
  if (!ASSIGNABLE_ROLES.includes(body.role)) {
    return problem(request, 422, ApiErrorCode.AccountRoleNotAssignable);
  }
  const phone = body.phone?.trim() || null;
  Object.assign(user, {
    email,
    phone,
    phoneCountryCode: phone ? prefixOf(body.phoneCountryCode) : null,
    role: body.role ?? null,
    emailIsContact: body.emailIsContact === true,
    phoneIsContact: body.phoneIsContact === true,
  });
  const answer = onlyFieldsOf(UserSelectedItem, user);
  return of(new HttpResponse({ status: 200, url: request.url, body: answer })).pipe(delay(600));
};

/**
 * The user the panel may read, change or delete, as `StaffService.GetManageableAsync`: 404
 * `account.not_found` for an id that is missing, not a UUID or not of the property; 422
 * `account.cannot_modify_self` for the signed-in user; 403 `account.role_not_assignable` for a role
 * the panel does not manage (the owner, the website admin).
 */
function manageableUser(
  request: HttpRequest<unknown>,
  idUser: string | null | undefined,
): UserRow | Observable<never> {
  const user = USERS.find(
    (row) =>
      typeof idUser === 'string' &&
      UUID.test(idUser) &&
      row.idUser === idUser &&
      row.propertyId === DEMO_PROPERTY.id,
  );
  if (!user) {
    return problem(request, 404, ApiErrorCode.AccountNotFound);
  }
  if (user.idUser === MOCK_SIGNED_IN_USER.id) {
    return problem(request, 422, ApiErrorCode.AccountCannotModifySelf);
  }
  if (!ASSIGNABLE_ROLES.includes(user.role)) {
    return problem(request, 403, ApiErrorCode.AccountRoleNotAssignable);
  }
  return user;
}

/** As the backend sends it: without the `+`. */
function prefixOf(prefix: string | null | undefined): string | null {
  return prefix?.trim().replace(/^\+/, '') || null;
}

/**
 * As the backend checks the body (`CommonRules`, `IdentityRules.NewPassword`): email required, valid
 * and up to 255 characters; password required only for a new user, 8 to 128 characters; phone up to
 * 32 characters, digits and spaces; prefix up to 8 characters, 1 to 6 digits with an optional `+`;
 * role required. One error per field, the first rule that fails.
 */
function userErrors(body: CreateUserBody, isPasswordRequired: boolean): MockFieldError[] {
  const errors: MockFieldError[] = [];
  const email = typeof body.email === 'string' ? body.email.trim() : '';
  if (!email) {
    errors.push({ field: 'email', code: ApiErrorCode.ValidationRequired });
  } else if ((body.email ?? '').length > MAX_EMAIL_LENGTH) {
    errors.push({
      field: 'email',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: MAX_EMAIL_LENGTH, min: 0 },
    });
  } else if (!isEmailAddress(email)) {
    errors.push({ field: 'email', code: ApiErrorCode.ValidationEmail });
  }
  const password = typeof body.password === 'string' ? body.password : null;
  if (password === null || !password.trim()) {
    if (isPasswordRequired || password !== null) {
      errors.push({ field: 'password', code: ApiErrorCode.ValidationRequired });
    }
  } else if (password.length < PASSWORD_MIN_LENGTH) {
    errors.push({
      field: 'password',
      code: ApiErrorCode.ValidationPasswordTooShort,
      args: { min: PASSWORD_MIN_LENGTH, max: -1 },
    });
  } else if (password.length > PASSWORD_MAX_LENGTH) {
    errors.push({
      field: 'password',
      code: ApiErrorCode.ValidationPasswordTooLong,
      args: { max: PASSWORD_MAX_LENGTH, min: 0 },
    });
  }
  const phone = typeof body.phone === 'string' ? body.phone : '';
  if (phone.length > PHONE_MAX_LENGTH) {
    errors.push({
      field: 'phone',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: PHONE_MAX_LENGTH, min: 0 },
    });
  } else if (!PHONE.test(phone)) {
    errors.push({ field: 'phone', code: ApiErrorCode.ValidationPhone });
  }
  const prefix = typeof body.phoneCountryCode === 'string' ? body.phoneCountryCode : null;
  if (prefix !== null && prefix.length > PHONE_PREFIX_MAX_LENGTH) {
    errors.push({
      field: 'phoneCountryCode',
      code: ApiErrorCode.ValidationMaxLength,
      args: { max: PHONE_PREFIX_MAX_LENGTH, min: 0 },
    });
  } else if (prefix !== null && !PHONE_PREFIX.test(prefix)) {
    errors.push({ field: 'phoneCountryCode', code: ApiErrorCode.ValidationPhonePrefix });
  }
  if (body.role === null || body.role === undefined) {
    errors.push({ field: 'role', code: ApiErrorCode.ValidationRequired });
  }
  return errors;
}

/** The common rules of the table requests: one `validation.invalid` per wrong field. */
function tableRequestErrors(body: Partial<PageRequest>): MockFieldError[] {
  const wrong = [
    { field: 'page', isWrong: !Number.isInteger(body.page) || (body.page ?? 0) < 1 },
    { field: 'pageSize', isWrong: !TABLE_PAGE_SIZE_OPTIONS.includes(body.pageSize ?? 0) },
    {
      field: 'sortField',
      isWrong: !!body.sortField && !SORT_FIELDS.some((name) => name === body.sortField),
    },
    {
      field: 'sortDirection',
      isWrong: !SORT_DIRECTIONS.some((direction) => direction === body.sortDirection),
    },
    { field: 'filters', isWrong: !Array.isArray(body.filters) || body.filters.length > 0 },
  ];
  return wrong
    .filter(({ isWrong }) => isWrong)
    .map(({ field }) => ({ field, code: ApiErrorCode.ValidationInvalid }));
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

/** `null` after every value, as PostgreSQL ascending: descending it comes first. */
function compare(first: string | null, second: string | null): number {
  if (first === null || second === null) {
    return first === second ? 0 : first === null ? 1 : -1;
  }
  return first.localeCompare(second, 'it', { sensitivity: 'base' });
}
