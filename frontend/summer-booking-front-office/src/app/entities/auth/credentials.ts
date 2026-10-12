export class SignInRequest {
  username: string | null = null;
  password: string | null = null;
  remember = false;
}

export interface AuthUser {
  email: string;
  idProperty: string;
  roles: string[];
  isElectronicReceiptAccepted: boolean;
}

export interface AuthSession {
  accessToken: string;
  /** Seconds the access token is valid for. */
  expiresIn: number;
  user: AuthUser;
}
