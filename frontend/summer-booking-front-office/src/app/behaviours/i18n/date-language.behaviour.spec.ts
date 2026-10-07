import { TestBed } from '@angular/core/testing';
import { DateAdapter, provideNativeDateAdapter } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { DateLanguageBehaviour } from './date-language.behaviour';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from './language.behaviour';

describe('DateLanguageBehaviour', () => {
  /** Pretends the browser has these languages set. */
  const browser = (languages: string[]) => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages);
  };

  const setup = (language: Language = 'it') => {
    TestBed.configureTestingModule({
      providers: [provideTranslateService(), provideNativeDateAdapter()],
    });
    const dates = TestBed.inject(DateLanguageBehaviour);
    TestBed.inject(LanguageBehaviour).use(language);
    return dates;
  };

  it('should read typed dates day first in Italian, with any separator and short years', () => {
    browser(['it-IT', 'it']);
    const dates = setup();

    expect(dates.locale()).toBe('it-IT');
    expect(dates.parse('03/10/2026')).toEqual(new Date(2026, 9, 3));
    expect(dates.parse('3-10-26')).toEqual(new Date(2026, 9, 3));
    expect(dates.parse(' 3.10.2026 ')).toEqual(new Date(2026, 9, 3));
  });

  it("should set the locale of Material's date adapter, following the language", () => {
    browser(['it-IT', 'en-US']);
    setup('it');
    const adapter = TestBed.inject<DateAdapter<Date>>(DateAdapter);
    const written = () =>
      adapter.format(new Date(2026, 9, 3), { day: '2-digit', month: '2-digit', year: 'numeric' });

    TestBed.tick();
    expect(written()).toBe('03/10/2026');

    TestBed.inject(LanguageBehaviour).use('en');
    TestBed.tick();
    expect(written()).toBe('10/03/2026'); // en-US: month first
  });

  it('should refuse texts that are not real dates', () => {
    browser(['it-IT']);
    const dates = setup();

    expect(dates.parse('31/02/2026')).toBeNull();
    expect(dates.parse('13/13/2026')).toBeNull();
    expect(dates.parse('03/10')).toBeNull();
    expect(dates.parse('oggi')).toBeNull();
    expect(dates.parse('')).toBeNull();
  });

  it("should use the browser's region for the site language, wherever the user is", () => {
    browser(['it-IT', 'en-US']);
    expect(setup('en').locale()).toBe('en-US'); // American browser language, wherever the user is
  });

  it('should read English dates in the order of the browser region', () => {
    browser(['en-AU', 'en']);
    const dates = setup('en');

    expect(dates.locale()).toBe('en-AU');
    expect(dates.parse('03/10/2026')).toEqual(new Date(2026, 9, 3)); // day first in Australia
  });

  it('should use en-GB for English when the browser names no English region', () => {
    browser(['it-IT', 'en']);
    expect(setup('en').locale()).toBe('en-GB');
  });

  it('should keep the usual locale of the other languages when the browser names none', () => {
    browser(['en-US']);
    expect(setup('it').locale()).toBe('it-IT');
    TestBed.resetTestingModule();

    browser(['it-CH', 'de-CH']);
    expect(setup('it').locale()).toBe('it-CH'); // Swiss Italian browser
  });
});
