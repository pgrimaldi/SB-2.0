import { UserRole } from '../../enums/user-role';

export const PASSWORD_MIN_LENGTH = 8;
export const PHONE_MAX_LENGTH = 32;

export class CreateUserRequest {
  email: string | null = null;
  emailIsContact = false;
  password: string | null = null;
  phoneCountryCode: string | null = null;
  phone: string | null = null;
  phoneIsContact = false;
  role: UserRole | null = null;
}
