/**
 * Error codes the backend sends today, as in the code catalog of `API_CONTRACTS.docx` (in
 * `shared documentation/` at the root of the repository, shared with the backend): add a new code here, in the catalog and in every translation file under
 * `error.` (the translation test fails otherwise). The mocks may only answer with these codes.
 */
export const API_ERROR_CODES = [
  'auth.invalid_credentials',
  'auth.unauthenticated',
  'auth.session_expired',
  'auth.app_not_allowed',
  'auth.too_many_attempts',
  'validation.failed',
  'validation.end_before_start',
  'validation.invalid',
  'resource.not_found',
  'auth.forbidden',
  'server.unexpected',
  'validation.required',
  'validation.email',
  'validation.max_length',
  'validation.min_length',
  'validation.length',
  'validation.out_of_range',
  'validation.phone',
  'validation.phone_prefix',
  'validation.must_accept',
  'validation.invalid_file_type',
] as const;

export type ApiErrorCode = (typeof API_ERROR_CODES)[number];
