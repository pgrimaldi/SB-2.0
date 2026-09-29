/** Body of `POST /api/auth/signin`. */
export interface SignInRequest {
  username: string;
  password: string;
}

export interface AuthUser {
  email: string;
  /** Public id (UUID) of the property the user manages. */
  idProperty: string;
}

/** Successful answer of `POST /api/auth/signin`. */
export interface SignInResponse {
  token: string;
  user: AuthUser;
}
