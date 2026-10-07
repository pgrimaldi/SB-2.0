/**
 * `revoked`: the server ended the session; `failed`: it could not be reached; `gone`: the session had
 * already changed hands, nothing was sent.
 */
export type LogoutOutcome = 'revoked' | 'failed' | 'gone';
