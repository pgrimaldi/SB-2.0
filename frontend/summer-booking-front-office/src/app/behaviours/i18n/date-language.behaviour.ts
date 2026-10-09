import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject } from '@angular/core';
import { DateAdapter, MatDateFormats } from '@angular/material/core';
import { DatePart } from '../../entities/shared/date-part';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from './language.behaviour';

/** Used when the browser does not name a region for the language. */
const DATE_PARTS: readonly DatePart[] = [DatePart.Day, DatePart.Month, DatePart.Year];
const DATE_LOCALES: Record<Language, string> = {
  [Language.It]: 'it-IT',
  [Language.En]: 'en-GB',
  [Language.Fr]: 'fr-FR',
  [Language.Es]: 'es-ES',
  [Language.De]: 'de-DE',
  [Language.Zh]: 'zh-CN',
  [Language.Ar]: 'ar-SA',
};

/**
 * Dates always in the Gregorian calendar with the digits 0-9 (user, 09/10/2026): Arabic would
 * otherwise use the Islamic calendar and the Eastern Arabic digits. Locales that already do keep
 * their tag as it is.
 */
function gregorianWithWesternDigits(tag: string): string {
  const { calendar, numberingSystem } = new Intl.DateTimeFormat(tag).resolvedOptions();
  return calendar === 'gregory' && numberingSystem === 'latn'
    ? tag
    : new Intl.Locale(tag, { calendar: 'gregory', numberingSystem: 'latn' }).toString();
}

/** For Material's native date adapter: the browser (Intl) writes dates as each language does. */
export const DATE_FORMATS: MatDateFormats = {
  parse: { dateInput: null },
  display: {
    dateInput: { day: '2-digit', month: '2-digit', year: 'numeric' },
    monthLabel: { month: 'short' },
    monthYearLabel: { month: 'short', year: 'numeric' },
    dateA11yLabel: { day: 'numeric', month: 'long', year: 'numeric' },
    monthYearA11yLabel: { month: 'long', year: 'numeric' },
  },
};

/**
 * Sets the locale of Material's date adapter, so all calendars and date fields follow the language
 * (started with the app). Date fields are read-only by default (dates come from the calendar); a
 * field that accepts typed dates uses `parse`.
 */
@Injectable({ providedIn: 'root' })
export class DateLanguageBehaviour {
  private readonly languageBehaviour = inject(LanguageBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  /** In order of preference, e.g. `['en-AU', 'en', 'it']`. */
  private readonly browserLanguages = inject(DOCUMENT).defaultView?.navigator.languages ?? [];
  /**
   * The browser's own region for the current language when it names one (`en-US`, `en-AU`,
   * `it-CH`…), otherwise `DATE_LOCALES` (English: `en-GB`).
   */
  readonly locale = computed(() => {
    const language = this.languageBehaviour.current();
    return gregorianWithWesternDigits(this.browserLocale(language) ?? DATE_LOCALES[language]);
  });

  private readonly order = computed(() =>
    new Intl.DateTimeFormat(this.locale(), DATE_FORMATS.display.dateInput)
      .formatToParts(new Date(2000, 0, 2))
      .map((part) => part.type)
      .filter((type): type is DatePart => DATE_PARTS.includes(type as DatePart)),
  );

  constructor() {
    effect(() => this.dateAdapter.setLocale(this.locale()));
  }

  /**
   * A date typed by hand, read in the order of the current language: "03/10/2026" is 3 October in
   * Italian, while in a year-first language "2026/10/03" would be. Any non-digit separates the three
   * numbers; a 2-digit year means 20xx. Null when the text is not a real date (e.g. 31/02/2026).
   */
  parse(text: string): Date | null {
    const numbers = text.match(/\d+/g);
    if (numbers?.length !== 3) {
      return null;
    }
    const [day, month, year] = DATE_PARTS.map((type) => {
      const typed = numbers[this.order().indexOf(type)];
      return type === DatePart.Year && typed.length <= 2 ? 2000 + Number(typed) : Number(typed);
    });
    const date = new Date(year, month - 1, day);
    const isReal =
      date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
    return isReal ? date : null;
  }

  private browserLocale(language: Language): string | undefined {
    return this.browserLanguages.find((tag) => {
      try {
        const locale = new Intl.Locale(tag);
        return (
          locale.language === language &&
          !!locale.region &&
          Intl.DateTimeFormat.supportedLocalesOf(tag).length > 0
        );
      } catch {
        return false; // not a valid language tag
      }
    });
  }
}
