import { UserRole } from '../../enums/user-role';

export class UserSelectedItem {
  idUser: string | null = null;
  email: string | null = null;
  isEmailActive = false;
  phone: string | null = null;
  isPhoneActive = false;
  role: UserRole | null = null;
}
