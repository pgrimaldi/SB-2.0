import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject } from '@angular/core';
import { DateAdapter, MatDateFormats } from '@angular/material/core';
import { Language, LanguageBehaviour } from './language.behaviour';

/** Locale of the dates of each language when the browser does not name a region for it. */
const DATE_LOCALES: Record<Language, string> = { it: 'it-IT', en: 'en-GB' };

/**
 * Formats of Material's native date adapter (`provideNativeDateAdapter`): dates written as each
 * language writes them, by the browser (Intl), with 2-digit day and month (it: 03/10/2026).
 */
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

type DatePart = 'day' | 'month' | 'year';

/**
 * Dates in the current language: sets the locale of Material's date adapter (all calendars and
 * date fields follow the language; started with the app) and reads dates typed by hand. By default date fields are not writable (dates come from the calendar); a field
 * that accepts typed dates uses `parse`.
 */
@Injectable({ providedIn: 'root' })
export class DateLanguageBehaviour {
  private readonly languageBehaviour = inject(LanguageBehaviour);

  /** Languages set in the browser, in order of preference (e.g. `['en-AU', 'en', 'it']`). */
  private readonly browserLanguages = inject(DOCUMENT).defaultView?.navigator.languages ?? [];
  /**
   * Locale of the current language: the browser's own region for that language when it names one
   * (`en-US`, `en-AU`, `it-CH`…), otherwise `DATE_LOCALES` (English: `en-GB`).
   */
  readonly locale = computed(() => {
    const language = this.languageBehaviour.current();
    return this.browserLocale(language) ?? DATE_LOCALES[language];
  });

  /** Order of day, month and year in the current language, asked to the browser (Intl). */
  private readonly order = computed(() =>
    new Intl.DateTimeFormat(this.locale(), DATE_FORMATS.display.dateInput)
      .formatToParts(new Date(2000, 0, 2))
      .map((part) => part.type)
      .filter((type): type is DatePart => type === 'day' || type === 'month' || type === 'year'),
  );

  /** First browser language that is `language` with a region the browser can format dates in. */
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

  constructor() {
    const dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
    effect(() => dateAdapter.setLocale(this.locale()));
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
    const [day, month, year] = (['day', 'month', 'year'] as const).map((type) => {
      const typed = numbers[this.order().indexOf(type)];
      return type === 'year' && typed.length <= 2 ? 2000 + Number(typed) : Number(typed);
    });
    const date = new Date(year, month - 1, day);
    const isReal =
      date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
    return isReal ? date : null;
  }
}
