import { UserRole } from '../../enums/user-role';

export class UserItem {
  idUser: string | null = null;
  email: string | null = null;
  phoneCountryCode: string | null = null;
  phone: string | null = null;
  role: UserRole | null = null;
}
