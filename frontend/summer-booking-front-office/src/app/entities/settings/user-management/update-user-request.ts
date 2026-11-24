import { UserRole } from '../../enums/user-role';

export class UpdateUserRequest {
  idUser: string | null = null;
  email: string | null = null;
  emailIsContact = false;
  password: string | null = null;
  phoneCountryCode: string | null = null;
  phone: string | null = null;
  phoneIsContact = false;
  role: UserRole | null = null;
}
