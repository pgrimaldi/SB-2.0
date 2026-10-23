import { UserRole } from '../../enums/user-role';

export class UpdateUserRequest {
  idUser: string | null = null;
  email: string | null = null;
  isEmailActive = false;
  password: string | null = null;
  phone: string | null = null;
  isPhoneActive = false;
  role: UserRole | null = null;
}
