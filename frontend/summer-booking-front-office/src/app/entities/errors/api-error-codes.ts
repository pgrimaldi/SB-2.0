/**
 * Error codes the backend sends today, as in the code catalog of `API_CONTRACTS.docx` (in
 * `shared documentation/` at the root of the repository, shared with the backend): add a new code here, in the catalog and in every translation file under
 * `error.` (the translation test fails otherwise). The mocks may only answer with these codes.
 */
export const API_ERROR_CODES = [
  'auth.invalid_credentials',
  'auth.invalid_token',
  'auth.session_expired',
  'validation.invalid_request',
  'validation.invalid_date',
  'validation.end_before_start',
  'validation.invalid_value',
  'resource.not_found',
  'operation.not_allowed',
  'server.unexpected',
] as const;

export type ApiErrorCode = (typeof API_ERROR_CODES)[number];
