export interface ContactSupportRequest {
  idProperty: string;
  firstName: string;
  lastName: string;
  email: string;
  mobilePhone: string | null;
  message: string;
}

/** The form of the page: the mobile phone is text there, empty when not given. */
export interface ContactSupportFields {
  firstName: string;
  lastName: string;
  email: string;
  mobilePhone: string;
  message: string;
}

export const EMPTY_CONTACT_SUPPORT_FIELDS: ContactSupportFields = {
  firstName: '',
  lastName: '',
  email: '',
  mobilePhone: '',
  message: '',
};
