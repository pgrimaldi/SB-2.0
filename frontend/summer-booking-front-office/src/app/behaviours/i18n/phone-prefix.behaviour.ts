import { Injectable, computed, inject } from '@angular/core';
import { IconSelectOption } from '../../components/shared/ui/selects/filled-icon-select/filled-icon-select';
import { PhonePrefix } from '../../entities/enums/phone-prefix';
import { LanguageBehaviour } from './language.behaviour';

export type PhoneCountry = keyof typeof PhonePrefix;

export const DEFAULT_PHONE_COUNTRY: PhoneCountry = 'IT';

const COUNTRIES = Object.keys(PhonePrefix) as PhoneCountry[];

/** The country shown for a prefix of several countries, which the backend saves without the country. */
const MAIN_COUNTRIES: Readonly<Partial<Record<string, PhoneCountry>>> = {
  '1': 'US',
  '7': 'RU',
  '39': 'IT',
  '44': 'GB',
  '47': 'NO',
  '61': 'AU',
  '64': 'NZ',
  '212': 'MA',
  '262': 'RE',
  '290': 'SH',
  '358': 'FI',
  '590': 'GP',
  '599': 'CW',
  '672': 'NF',
};

/** The choices of the phone prefix fields: flag, name of the country and prefix. */
@Injectable({ providedIn: 'root' })
export class PhonePrefixBehaviour {
  private readonly languageBehaviour = inject(LanguageBehaviour);

  /** Every country, the default one first, then by name in the language of the app. */
  readonly options = computed<readonly IconSelectOption<PhoneCountry>[]>(() => {
    const language = this.languageBehaviour.current();
    const names = new Intl.DisplayNames([language], { type: 'region' });
    const options = COUNTRIES.map((country) => ({
      value: country,
      label: names.of(country) ?? country,
      shortLabel: `+${PhonePrefix[country]}`,
      icon: `/assets/flags/${country.toLowerCase()}.svg`,
    })).sort((first, second) => first.label.trim().localeCompare(second.label.trim(), language));
    return [
      ...options.filter(({ value }) => value === DEFAULT_PHONE_COUNTRY),
      ...options.filter(({ value }) => value !== DEFAULT_PHONE_COUNTRY),
    ];
  });

  /** The country of a saved prefix (with or without the +); the default one when there is none. */
  countryOf(prefix: string | null): PhoneCountry {
    const digits = prefix?.trim().replace(/^\+/, '') ?? '';
    return (
      MAIN_COUNTRIES[digits] ??
      COUNTRIES.find((country) => PhonePrefix[country] === digits) ??
      DEFAULT_PHONE_COUNTRY
    );
  }

  /** The prefix sent with a number: none without a number. */
  prefixFor(phone: string | null, prefix: string | null): string | null {
    return phone?.trim() ? (prefix ?? PhonePrefix[DEFAULT_PHONE_COUNTRY]) : null;
  }
}
