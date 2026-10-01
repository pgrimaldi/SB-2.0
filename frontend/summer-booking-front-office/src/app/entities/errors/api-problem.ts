/**
 * Values for the placeholders of an error message, e.g. `{ umbrellaId: '42', date: '2026-08-01' }`:
 * raw values (ISO 8601 dates, numbers, `currency` codes) that the translation formats.
 */
export type ApiProblemArgs = Readonly<Record<string, string | number>>;

/** Validation error of one field of the request (e.g. `datetimeTo` before `datetimeFrom`). */
export interface ApiFieldError {
  /** Name of the field, as in the request body. */
  readonly field: string;
  /** Stable code, also the translation key under `error.` (e.g. `validation.end_before_start`). */
  readonly code: string;
  readonly args?: ApiProblemArgs;
}

/**
 * An error of a call to our API, in one shape whatever happened: the backend's Problem Details
 * (RFC 9457) extended with `code`, or what the frontend builds for errors that do not come from the
 * backend (no connection, proxy pages, browser errors). `code` is the translation key under `error.`
 * (e.g. `auth.invalid_credentials` → `error.auth.invalid_credentials`): no conversion in between.
 * `title` and `type` are for developers and logs only, never shown to the user.
 */
export interface ApiProblem {
  /** HTTP status of the answer; 0 when there was no answer (no connection). */
  readonly status: number;
  readonly code: string;
  readonly args?: ApiProblemArgs;
  /** Validation errors, one per field (status 400). */
  readonly errors?: readonly ApiFieldError[];
  /** Id that links the error to the server logs, for support. */
  readonly traceId?: string;
  readonly type?: string;
  readonly title?: string;
}

/** Codes the frontend gives to errors that come without a backend `code`. */
export const FRONTEND_ERROR_CODES = {
  /** No answer at all: the network is down or the server cannot be reached. */
  network: 'network.unavailable',
  /** 502, 503 or 504 from the infrastructure (proxy, CDN) instead of our backend. */
  unavailable: 'server.unavailable',
  /** Any other answer that is not our error format (e.g. an HTML page). */
  unexpected: 'server.unexpected',
  /** An error raised in the browser, not by a call. */
  client: 'client.unexpected',
} as const;
