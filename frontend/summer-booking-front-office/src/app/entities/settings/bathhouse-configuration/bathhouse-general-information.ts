import { CancellationPolicy } from '../../enums/cancellation-policy';
import { PetPolicy } from '../../enums/pet-policy';

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
  cancellationPolicy: CancellationPolicy | null = null;
  petPolicy: PetPolicy | null = null;
  regulation: string | null = null;
}
