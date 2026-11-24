export const MAX_ACCOUNT_NAME_LENGTH = 128;
export const MAX_ACCOUNT_STREET_NUMBER_LENGTH = 16;
export const MAX_ACCOUNT_POSTAL_CODE_LENGTH = 5;
export const MAX_ACCOUNT_TAX_CODE_LENGTH = 16;
export const MAX_ACCOUNT_VAT_NUMBER_LENGTH = 13;

export class ElectronicReceiptAccount {
  companyName: string | null = null;
  companyAddress: string | null = null;
  streetNumber: string | null = null;
  postalCode: string | null = null;
  city: string | null = null;
  country: string | null = null;
  taxCode: string | null = null;
  revenueAgencyCode: string | null = null;
  vatNumber: string | null = null;
  pin: string | null = null;
  password: string | null = null;
}
