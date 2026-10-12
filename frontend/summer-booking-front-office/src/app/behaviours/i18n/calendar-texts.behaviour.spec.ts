import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import {
  TranslateService,
  TranslationObject,
  provideTranslateCompiler,
  provideTranslateService,
} from '@ngx-translate/core';
import { MessageFormatCompiler } from '../../components/shared/i18n/message-format';
import { Datepicker } from '../../components/shared/ui/datepickers/datepicker/datepicker';
import { Language } from '../../entities/shared/language';
import { CalendarTextsBehaviour } from './calendar-texts.behaviour';
import { DateLanguageBehaviour } from './date-language.behaviour';
import { LanguageBehaviour } from './language.behaviour';

describe('CalendarTextsBehaviour', () => {
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
    TestBed.inject(CalendarTextsBehaviour);
    return TestBed.inject(LanguageBehaviour);
  };

  it('should keep the dates of an open calendar when the language changes', async () => {
    const languages = await setupCalendar();
    languages.use(Language.Ar);
    const fixture = TestBed.createComponent(Datepicker);
    fixture.componentRef.setInput('start', new Date(2026, 9, 9));
    fixture.componentRef.setInput('end', new Date(2026, 9, 10));
    await fixture.whenStable();
    (fixture.nativeElement as HTMLElement).querySelector('input')!.click();
    await fixture.whenStable();

    languages.use(Language.Zh);
    await fixture.whenStable();
    expect(fixture.componentInstance.start()).toEqual(new Date(2026, 9, 9));
    expect(fixture.componentInstance.end()).toEqual(new Date(2026, 9, 10));

    languages.use(Language.It);
    await fixture.whenStable();
    document.querySelector<HTMLButtonElement>('.mat-calendar-previous-button')!.click();
    await fixture.whenStable();
    const day = [...document.querySelectorAll<HTMLButtonElement>('.mat-calendar-body-cell')].find(
      (cell) => cell.textContent?.trim() === '15',
    )!;
    day.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.start()).toEqual(new Date(2026, 8, 15));
    expect(fixture.componentInstance.end()).toEqual(new Date(2026, 9, 10));
  });
});
