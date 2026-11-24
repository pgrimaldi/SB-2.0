import { PetPolicy } from '../../enums/pet-policy';

export const MAX_BATHHOUSE_PHONE_LENGTH = 32;
export const MAX_BATHHOUSE_TEXT_LENGTH = 255;
export const MAX_BATHHOUSE_POSTAL_CODE_LENGTH = 16;
/** Characters the user sees in Descrizione and Regolamento, formatting left out. */
export const MAX_BATHHOUSE_RICH_TEXT_LENGTH = 5000;

export class BathhouseGeneralInformation {
  bathhouseName: string | null = null;
  address: string | null = null;
  postalCode: string | null = null;
  city: string | null = null;
  province: string | null = null;
  region: string | null = null;
  country: string | null = null;
  phone: string | null = null;
  whatsapp: string | null = null;
  email: string | null = null;
  website: string | null = null;
  facebook: string | null = null;
  instagram: string | null = null;
  description: string | null = null;
  openingTime: string | null = null;
  closingTime: string | null = null;
  isFreeCancellation = false;
  cancellationNoticeHours: number | null = null;
  petPolicy = PetPolicy.OnRequest;
  regulation: string | null = null;
}
