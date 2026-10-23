import { UserRole } from '../../enums/user-role';

export class CreateUserRequest {
  email: string | null = null;
  isEmailActive = false;
  password: string | null = null;
  phone: string | null = null;
  isPhoneActive = false;
  role: UserRole | null = null;
}
