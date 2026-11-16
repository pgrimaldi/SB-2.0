import { UserRole } from '../../enums/user-role';

export class UserSelectedItem {
  idUser: string | null = null;
  email: string | null = null;
  emailIsContact = false;
  phoneCountryCode: string | null = null;
  phone: string | null = null;
  phoneIsContact = false;
  role: UserRole | null = null;
}
