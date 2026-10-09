import { TestBed } from '@angular/core/testing';
import { DateAdapter, provideNativeDateAdapter } from '@angular/material/core';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import {
  TranslateService,
  TranslationObject,
  provideTranslateCompiler,
  provideTranslateService,
} from '@ngx-translate/core';
import { MessageFormatCompiler } from '../../components/shared/i18n/message-format';
import { Datepicker } from '../../components/shared/ui/datepickers/datepicker/datepicker';
import { DateLanguageBehaviour } from './date-language.behaviour';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from './language.behaviour';

describe('DateLanguageBehaviour', () => {
  /** Pretends the browser has these languages set. */
  const browser = (languages: string[]) => {
    vi.spyOn(navigator, 'languages', 'get').mockReturnValue(languages);
  };

  const setup = (language: Language = Language.It) => {
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
    setup(Language.It);
    const adapter = TestBed.inject<DateAdapter<Date>>(DateAdapter);
    const written = () =>
      adapter.format(new Date(2026, 9, 3), { day: '2-digit', month: '2-digit', year: 'numeric' });

    TestBed.tick();
    expect(written()).toBe('03/10/2026');

    TestBed.inject(LanguageBehaviour).use(Language.En);
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
    expect(setup(Language.En).locale()).toBe('en-US'); // American browser language, wherever the user is
  });

  it('should read English dates in the order of the browser region', () => {
    browser(['en-AU', 'en']);
    const dates = setup(Language.En);

    expect(dates.locale()).toBe('en-AU');
    expect(dates.parse('03/10/2026')).toEqual(new Date(2026, 9, 3)); // day first in Australia
  });

  it('should use en-GB for English when the browser names no English region', () => {
    browser(['it-IT', 'en']);
    expect(setup(Language.En).locale()).toBe('en-GB');
  });

  it('should keep the usual locale of the other languages when the browser names none', () => {
    browser(['en-US']);
    expect(setup(Language.It).locale()).toBe('it-IT');
    TestBed.resetTestingModule();

    browser(['it-CH', 'de-CH']);
    expect(setup(Language.It).locale()).toBe('it-CH'); // Swiss Italian browser
  });

  const setupCalendar = async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService({ compiler: provideTranslateCompiler(MessageFormatCompiler) }),
        provideNativeDateAdapter(),
      ],
    });
    const translate = TestBed.inject(TranslateService);
    for (const language of Object.values(Language)) {
      const file = (await import(`../../../assets/i18n/${language}.json`)) as {
        default: TranslationObject;
      };
      translate.setTranslation(language, file.default);
    }
    TestBed.inject(DateLanguageBehaviour);
    return TestBed.inject(LanguageBehaviour);
  };

  it.each([
    [Language.It, 'Mese precedente', 'Mese successivo', 'Scegli mese e anno', 'Da 2016 a 2039'],
    [Language.En, 'Previous month', 'Next month', 'Choose month and year', 'From 2016 to 2039'],
    [Language.Fr, 'Mois précédent', 'Mois suivant', 'Choisir le mois et l’année', 'De 2016 à 2039'],
    [Language.Es, 'Mes anterior', 'Mes siguiente', 'Elegir mes y año', 'De 2016 a 2039'],
    [
      Language.De,
      'Vorheriger Monat',
      'Nächster Monat',
      'Monat und Jahr auswählen',
      'Von 2016 bis 2039',
    ],
    [Language.Zh, '上个月', '下个月', '选择月份和年份', '从2016年到2039年'],
    [Language.Ar, 'الشهر السابق', 'الشهر التالي', 'اختيار الشهر والسنة', 'من 2016 إلى 2039'],
  ])(
    'should translate calendar commands and year ranges in %s',
    async (language, previous, next, choose, range) => {
      const languages = await setupCalendar();
      languages.use(language as Language);
      TestBed.tick();
      const intl = TestBed.inject(MatDatepickerIntl);
      const adapter = TestBed.inject<DateAdapter<Date>>(DateAdapter);
      const start = adapter.getYearName(new Date(2016, 0, 1));
      const end = adapter.getYearName(new Date(2039, 0, 1));

      expect(intl.prevMonthLabel).toBe(previous);
      expect(intl.nextMonthLabel).toBe(next);
      expect(intl.switchToMultiYearViewLabel).toBe(choose);
      expect(intl.formatYearRangeLabel(start, end)).toBe(range);
      expect(intl.formatYearRange(start, end)).toBe(`${start} – ${end}`);
    },
  );

  it('should update an already open calendar on language changes without changing its dates', async () => {
    const languages = await setupCalendar();
    languages.use(Language.Ar);
    const fixture = TestBed.createComponent(Datepicker);
    fixture.componentRef.setInput('start', new Date(2026, 9, 9));
    fixture.componentRef.setInput('end', new Date(2026, 9, 10));
    await fixture.whenStable();
    const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;
    input.click();
    await fixture.whenStable();
    const previous = () =>
      document.querySelector<HTMLButtonElement>('.mat-calendar-previous-button')!;
    expect(previous().getAttribute('aria-label')).toBe('الشهر السابق');

    languages.use(Language.Zh);
    await fixture.whenStable();
    expect(previous().getAttribute('aria-label')).toBe('上个月');
    expect(document.querySelector('.mat-calendar-period-button')?.getAttribute('aria-label')).toBe(
      '选择月份和年份',
    );
    expect(fixture.componentInstance.start()).toEqual(new Date(2026, 9, 9));
    expect(fixture.componentInstance.end()).toEqual(new Date(2026, 9, 10));

    languages.use(Language.It);
    await fixture.whenStable();
    expect(previous().getAttribute('aria-label')).toBe('Mese precedente');
    previous().click();
    await fixture.whenStable();
    expect(document.querySelector('.mat-calendar-period-button')?.textContent).toMatch(/set/i);
    const day = [...document.querySelectorAll<HTMLButtonElement>('.mat-calendar-body-cell')].find(
      (cell) => cell.textContent?.trim() === '15',
    )!;
    day.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.start()).toEqual(new Date(2026, 8, 15));
    expect(fixture.componentInstance.end()).toEqual(new Date(2026, 9, 10));
    await vi.waitFor(() => expect(document.querySelector('mat-calendar')).toBeNull());
  });
});
