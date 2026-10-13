/** Translation group `management.settings.database.filters.<filter>.options`. */
export type FilterOptionTexts = Record<string, string>;

/** Translation group `management.settings.database.filters`. */
export interface ExistingBookingFilterTexts {
  booking_typology: { label: string; options: FilterOptionTexts };
  agreement_booking: { label: string; options: FilterOptionTexts };
  agreement_structure: { label: string };
  payment_status: { label: string; options: FilterOptionTexts };
  chips: {
    booking_typology: string;
    agreement_booking: string;
    agreement_structure: string;
    payment_status: string;
    more_options: string;
    clear: string;
  };
}

/** Enum values as `PascalCase`, translation keys as `snake_case`. */
export function filterOptionsOf<K extends string>(
  values: readonly K[],
  texts: FilterOptionTexts | undefined,
): readonly { value: K; label: string }[] {
  return values.map((value) => ({
    value,
    label: texts?.[value.replace(/([a-z0-9])([A-Z])/g, '$1_$2').toLowerCase()] ?? '',
  }));
}
