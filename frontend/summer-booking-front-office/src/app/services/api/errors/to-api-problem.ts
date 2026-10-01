import { HttpErrorResponse } from '@angular/common/http';
import {
  ApiFieldError,
  ApiProblem,
  ApiProblemArgs,
  FRONTEND_ERROR_CODES,
} from '../../../entities/errors/api-problem';

/** Stable code: lowercase dotted words, e.g. `auth.invalid_credentials`. */
const CODE = /^[a-z0-9_]+(\.[a-z0-9_]+)+$/;
const MAX_CODE_LENGTH = 100;
/** Name of a placeholder of the message, e.g. `umbrellaId`. */
const ARG_NAME = /^[A-Za-z0-9_]{1,50}$/;
const MAX_ARG_LENGTH = 200;
/** Trace id of the server logs (e.g. W3C `00-…-…-01`). */
const TRACE_ID = /^[A-Za-z0-9._:-]{1,128}$/;
const MAX_TEXT_LENGTH = 300;
const MAX_FIELD_ERRORS = 50;
/** Statuses the infrastructure (proxy, CDN) answers when our backend cannot. */
const UNAVAILABLE_STATUSES = [502, 503, 504];

/**
 * Turns any error of a call to our API into an `ApiProblem`: the backend's Problem Details with its
 * `code`, or a frontend code when the error does not come from the backend (`FRONTEND_ERROR_CODES`).
 * What the server sends is never trusted as it is: only well-formed fields are kept, and a missing or
 * malformed `code` counts as an answer in another format. So a code is always safe as a translation
 * key and its args as message values.
 */
export function toApiProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, code: FRONTEND_ERROR_CODES.client };
  }
  if (error.status === 0) {
    return { status: 0, code: FRONTEND_ERROR_CODES.network };
  }

  const body: unknown = error.error;
  if (!isRecord(body) || !isCode(body['code'])) {
    const code = UNAVAILABLE_STATUSES.includes(error.status)
      ? FRONTEND_ERROR_CODES.unavailable
      : FRONTEND_ERROR_CODES.unexpected;
    return { status: error.status, code };
  }

  const args = readArgs(body['args']);
  const errors = readFieldErrors(body['errors']);
  const traceId = body['traceId'];
  const type = readText(body['type']);
  const title = readText(body['title']);
  return {
    // The status of the answer is the real one, whatever the body says.
    status: error.status,
    code: body['code'],
    ...(args && { args }),
    ...(errors && { errors }),
    ...(typeof traceId === 'string' && TRACE_ID.test(traceId) && { traceId }),
    ...(type && { type }),
    ...(title && { title }),
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isCode(value: unknown): value is string {
  return typeof value === 'string' && value.length <= MAX_CODE_LENGTH && CODE.test(value);
}

/** Only short texts and finite numbers with well-formed names; nothing when none is left. */
function readArgs(value: unknown): ApiProblemArgs | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const entries = Object.entries(value).filter(
    ([name, arg]) =>
      ARG_NAME.test(name) &&
      ((typeof arg === 'string' && arg.length <= MAX_ARG_LENGTH) ||
        (typeof arg === 'number' && Number.isFinite(arg))),
  );
  return entries.length ? (Object.fromEntries(entries) as ApiProblemArgs) : undefined;
}

/** The well-formed field errors (at most `MAX_FIELD_ERRORS`); nothing when none is left. */
function readFieldErrors(value: unknown): readonly ApiFieldError[] | undefined {
  if (!Array.isArray(value)) {
    return undefined;
  }
  const errors = value
    .filter(
      (item): item is Record<string, unknown> =>
        isRecord(item) && readText(item['field']) !== undefined && isCode(item['code']),
    )
    .slice(0, MAX_FIELD_ERRORS)
    .map((item) => {
      const args = readArgs(item['args']);
      return {
        field: item['field'] as string,
        code: item['code'] as string,
        ...(args && { args }),
      };
    });
  return errors.length ? errors : undefined;
}

function readText(value: unknown): string | undefined {
  return typeof value === 'string' && value.length > 0 && value.length <= MAX_TEXT_LENGTH
    ? value
    : undefined;
}
