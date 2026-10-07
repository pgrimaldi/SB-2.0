export class SignInRequest {
  username: string | null = null;
  password: string | null = null;
  /** The server keeps the refresh cookie after the browser is closed. */
  remember = false;
}

export interface AuthUser {
  email: string;
  idProperty: string;
  roles: string[];
}

export interface AuthSession {
  accessToken: string;
  /** Seconds the access token is valid for. */
  expiresIn: number;
  user: AuthUser;
}
