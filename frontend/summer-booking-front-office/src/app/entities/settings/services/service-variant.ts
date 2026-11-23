export const MAX_SERVICE_VARIANT_NAME_LENGTH = 120;

export class ServiceVariant {
  variantName: string | null = null;
  /** Added to the price of the service. */
  price: number | null = null;
}
