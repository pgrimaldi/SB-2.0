/** Body of `POST /api/auth/signin`. */
export interface SignInRequest {
  username: string;
  password: string;
}

export interface AuthUser {
  email: string;
}

/** Successful answer of `POST /api/auth/signin`. */
export interface SignInResponse {
  token: string;
  user: AuthUser;
}
