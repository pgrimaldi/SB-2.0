import { PetPolicy } from '../../enums/pet-policy';

export const MAX_BATHHOUSE_PHONE_LENGTH = 32;

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
  petPolicy = PetPolicy.NotAllowed;
  regulation: string | null = null;
}
