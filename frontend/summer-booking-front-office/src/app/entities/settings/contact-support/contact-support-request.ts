export const MAX_CONTACT_NAME_LENGTH = 100;
export const MAX_CONTACT_PHONE_LENGTH = 32;
export const MAX_CONTACT_MESSAGE_LENGTH = 2000;

export class ContactSupportRequest {
  firstName: string | null = null;
  lastName: string | null = null;
  email: string | null = null;
  mobilePhone: string | null = null;
  message: string | null = null;
}
