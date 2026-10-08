export enum LogoutOutcome {
  /** The server ended the session. */
  Revoked,
  /** The server could not be reached. */
  Failed,
  /** The session had already changed hands: nothing was sent. */
  Gone,
}
