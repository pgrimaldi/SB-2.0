import { AuthSession } from './credentials';

export type Session = Pick<AuthSession, 'accessToken' | 'user'>;
