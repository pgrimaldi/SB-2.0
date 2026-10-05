export interface ContactSupportRequest {
  idProperty: string;
  firstName: string;
  lastName: string;
  email: string;
  mobilePhone: string | null;
  message: string;
}
