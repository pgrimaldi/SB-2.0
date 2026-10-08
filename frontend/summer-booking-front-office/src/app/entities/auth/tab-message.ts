export interface TabMessage {
  /** `false`: a sign-in. */
  isLogout: boolean;
  session: string;
}
