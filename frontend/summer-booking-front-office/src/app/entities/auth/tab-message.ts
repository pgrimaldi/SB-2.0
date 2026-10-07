export interface TabMessage {
  type: 'signin' | 'logout';
  session: string;
}
