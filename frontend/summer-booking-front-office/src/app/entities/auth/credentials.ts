/** Body of `POST /api/auth/signin`. */
export interface SignInRequest {
  username: string;
  password: string;
  /** "Remember me": the server keeps the refresh cookie after the browser is closed. */
  remember: boolean;
}

/** The signed-in user, as the server describes it for the interface. */
export interface AuthUser {
  email: string;
  /** Public id (UUID) of the property the user manages. */
  idProperty: string;
  roles: string[];
}

/**
 * Answer of `POST /api/auth/signin` and `POST /api/auth/refresh`. The access token is opaque to the
 * app (never decoded) and lives only in memory. The refresh token is not here: the server keeps it in
 * an HttpOnly, Secure, SameSite=Strict cookie that JavaScript cannot read.
 */
export interface AuthSession {
  accessToken: string;
  /** Seconds the access token is valid for. */
  expiresIn: number;
  user: AuthUser;
}
