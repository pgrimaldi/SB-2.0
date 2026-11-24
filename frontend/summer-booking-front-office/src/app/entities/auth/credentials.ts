export class LoginRequest {
  username: string | null = null;
  password: string | null = null;
  remember = false;
}

export interface AuthUser {
  id: string;
  email: string;
  role: string;
  /** Null for an account without a property (`AuthUserDto.PropertyId`). */
  propertyId: string | null;
}

export interface AuthSession {
  accessToken: string;
  /** Seconds the access token is valid for. */
  expiresIn: number;
  user: AuthUser;
}
