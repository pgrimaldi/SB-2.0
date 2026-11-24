import { HttpEvent, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, delay, of } from 'rxjs';
import { comparableJson, trimmedText } from '../../../components/shared/forms/text-comparison';
import { isEmailAddress } from '../../../components/shared/forms/validators';
import { SmtpSecurity } from '../../../entities/enums/smtp-security';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { EmailConfigurationData } from '../../../entities/settings/email-configuration/email-configuration-data';
import {
  EmailConfigurationRequest,
  MAX_SMTP_EMAIL_LENGTH,
  MAX_SMTP_HOST_LENGTH,
  MAX_SMTP_NAME_LENGTH,
  MAX_SMTP_PORT,
  MAX_TEST_RECIPIENT_QUANTITY,
  SMTP_CONNECTION_FIELDS,
  SMTP_SSL_PORT,
  SMTP_START_TLS_PORT,
} from '../../../entities/settings/email-configuration/email-configuration-request';
import { isAuthorized, unauthorized } from '../auth/auth.mock';
import { MockFieldError, problem } from '../errors/problem.mock';
import { savedWithVersion, versionConflict, versionOf } from '../versions/versions.mock';

type Configuration = Omit<EmailConfigurationData, 'hasSmtpPassword' | 'version'>;

/** Invented values: never a real mail server or real credentials. */
const CONFIGURATION: Configuration = {
  fromEmail: 'prenotazioni@lido-demo.example',
  fromName: 'Lido Demo',
  smtpHost: 'smtp.lido-demo.example',
  smtpPort: 587,
  smtpUsername: 'prenotazioni@lido-demo.example',
  security: SmtpSecurity.StartTls,
  verifyCertificate: true,
  testRecipients: ['prova@lido-demo.example'],
  isActive: true,
  lastVerificationAt: null,
  lastVerificationError: null,
};
/** Kept apart, like the real server keeps it as a secret: it is never part of an answer. */
let smtpPassword: string | null = 'password-di-esempio';

/** `GET /api/notification/email-configuration`. */
export const emailConfigurationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body: EmailConfigurationData = {
    ...CONFIGURATION,
    testRecipients: [...CONFIGURATION.testRecipients],
    hasSmtpPassword: smtpPassword !== null,
    version: versionOf(versionedData()),
  };
  return of(new HttpResponse({ status: 200, url: request.url, body })).pipe(delay(150));
};

/** `POST /api/notification/send-test-email`: a server named "errore" refuses the test. */
export const sendTestEmailMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<EmailConfigurationRequest>;
  const errors = [
    ...configurationErrors(body),
    ...(body.testRecipients?.length
      ? []
      : [{ field: 'testRecipients', code: ApiErrorCode.ValidationRequired }]),
  ];
  const wrong = errors.length ? errors : passwordErrors(body);
  if (wrong.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors: wrong });
  }
  const detail = body.smtpHost?.startsWith('errore') ? '535 Authentication failed' : null;
  if (!trimmedText(body.smtpPassword ?? null) && isSaved(body)) {
    CONFIGURATION.lastVerificationAt = new Date().toISOString();
    CONFIGURATION.lastVerificationError = detail;
  }
  return detail
    ? problem(request, 422, ApiErrorCode.NotificationTestEmailFailed, { args: { detail } })
    : of(new HttpResponse({ status: 204, url: request.url })).pipe(delay(1000));
};

/** `POST /api/notification/save-email-configuration`: the mock keeps the values until the page is loaded again. */
export const saveEmailConfigurationMock = (
  request: HttpRequest<unknown>,
): Observable<HttpEvent<unknown>> => {
  if (!isAuthorized(request)) {
    return unauthorized(request);
  }
  const body = (request.body ?? {}) as Partial<EmailConfigurationRequest>;
  const errors = configurationErrors(body);
  if (errors.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors });
  }
  const conflict = versionConflict(request, versionedData());
  if (conflict) {
    return conflict;
  }
  const wrong = passwordErrors(body);
  if (wrong.length) {
    return problem(request, 400, ApiErrorCode.ValidationFailed, { errors: wrong });
  }
  const { smtpPassword: typed, ...values } = body as EmailConfigurationRequest;
  if (trimmedText(typed) || isOtherConnection(body)) {
    CONFIGURATION.lastVerificationAt = null;
    CONFIGURATION.lastVerificationError = null;
  }
  Object.assign(CONFIGURATION, values);
  smtpPassword = typed ?? smtpPassword;
  return savedWithVersion(request, versionedData());
};

/** What the version covers: what the save writes, not the password nor the outcome of the tests. */
function versionedData(): unknown {
  const { lastVerificationAt, lastVerificationError, ...saved } = CONFIGURATION;
  void lastVerificationAt;
  void lastVerificationError;
  return { ...saved, hasSmtpPassword: smtpPassword !== null };
}

function isSaved(body: Partial<EmailConfigurationRequest>): boolean {
  return (Object.keys(new EmailConfigurationRequest()) as (keyof EmailConfigurationRequest)[])
    .filter((name) => name !== 'smtpPassword')
    .every(
      (name) =>
        comparableJson(body[name]) === comparableJson(CONFIGURATION[name as keyof Configuration]),
    );
}

function isOtherConnection(body: Partial<EmailConfigurationRequest>): boolean {
  return SMTP_CONNECTION_FIELDS.some(
    (name) => comparableJson(body[name]) !== comparableJson(CONFIGURATION[name]),
  );
}

/** The saved password is never sent anywhere else. */
function passwordErrors(body: Partial<EmailConfigurationRequest>): MockFieldError[] {
  const isMissing = !trimmedText(body.smtpPassword ?? null);
  return isMissing && smtpPassword !== null && isOtherConnection(body)
    ? [{ field: 'smtpPassword', code: ApiErrorCode.ValidationRequired }]
    : [];
}

function configurationErrors(body: Partial<EmailConfigurationRequest>): MockFieldError[] {
  const errors: MockFieldError[] = [
    ...textErrors('fromEmail', body.fromEmail, MAX_SMTP_EMAIL_LENGTH, true),
    ...textErrors('fromName', body.fromName, MAX_SMTP_NAME_LENGTH),
    ...textErrors('smtpHost', body.smtpHost, MAX_SMTP_HOST_LENGTH, true),
    ...textErrors('smtpUsername', body.smtpUsername, MAX_SMTP_EMAIL_LENGTH, true),
  ];
  const fromEmail = trimmedText(body.fromEmail ?? null);
  if (fromEmail && fromEmail.length <= MAX_SMTP_EMAIL_LENGTH && !isEmailAddress(fromEmail)) {
    errors.push({ field: 'fromEmail', code: ApiErrorCode.ValidationEmail });
  }
  const port = body.smtpPort ?? null;
  if (port === null) {
    errors.push({ field: 'smtpPort', code: ApiErrorCode.ValidationRequired });
  } else if (!Number.isInteger(port) || port < 1 || port > MAX_SMTP_PORT) {
    errors.push({
      field: 'smtpPort',
      code: ApiErrorCode.ValidationOutOfRange,
      args: { from: 1, to: MAX_SMTP_PORT },
    });
  }
  if (!body.security) {
    errors.push({ field: 'security', code: ApiErrorCode.ValidationRequired });
  } else if (
    !Object.values(SmtpSecurity).includes(body.security) ||
    (port === SMTP_SSL_PORT && body.security !== SmtpSecurity.Ssl) ||
    (port === SMTP_START_TLS_PORT && body.security !== SmtpSecurity.StartTls)
  ) {
    errors.push({ field: 'security', code: ApiErrorCode.ValidationInvalid });
  }
  const recipients = body.testRecipients;
  if (!recipients) {
    errors.push({ field: 'testRecipients', code: ApiErrorCode.ValidationRequired });
  } else if (
    recipients.length > MAX_TEST_RECIPIENT_QUANTITY ||
    recipients.some((email) => email.length > MAX_SMTP_EMAIL_LENGTH || !isEmailAddress(email))
  ) {
    errors.push({ field: 'testRecipients', code: ApiErrorCode.ValidationInvalid });
  }
  return errors;
}

function textErrors(
  field: string,
  value: string | null | undefined,
  max: number,
  isRequired = false,
): MockFieldError[] {
  const text = trimmedText(value ?? null);
  if (!text) {
    return isRequired ? [{ field, code: ApiErrorCode.ValidationRequired }] : [];
  }
  return text.length > max
    ? [{ field, code: ApiErrorCode.ValidationMaxLength, args: { max, min: 0 } }]
    : [];
}
