import { TestBed } from '@angular/core/testing';
import { MAT_DATE_LOCALE, provideNativeDateAdapter } from '@angular/material/core';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { DATE_FORMATS } from '../../../behaviours/i18n/date-language.behaviour';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { ManagementHeader } from './management-header';

describe('ManagementHeader', () => {
  it('should switch to "Non oggi" when a date moves and bring both dates back to today on click', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideTranslateService(),
        provideNativeDateAdapter(DATE_FORMATS),
        { provide: MAT_DATE_LOCALE, useValue: 'it-IT' }, // set by DateLanguageBehaviour in the app
        ManagementFiltersBehaviour,
      ],
    });
    const fixture = TestBed.createComponent(ManagementHeader);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const today = element.querySelector<HTMLButtonElement>('.management__header__today')!;
    const dates = () =>
      [...element.querySelectorAll<HTMLInputElement>('.datepicker__input')].map(
        (input) => input.value,
      );
    const todayText = new Intl.DateTimeFormat('it-IT', DATE_FORMATS.display.dateInput).format(
      new Date(),
    );

    expect(today.disabled).toBe(true);

    element.querySelectorAll<HTMLButtonElement>('.datepicker__arrow')[3].click(); // end ▶
    await fixture.whenStable();
    expect(today.disabled).toBe(false);
    expect(today.classList).toContain('management__header__today__other');

    today.click();
    await fixture.whenStable();
    expect(today.disabled).toBe(true);
    expect(dates()).toEqual([todayText, todayText]);
  });
  it('should open the settings with the gear', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideTranslateService(),
        provideNativeDateAdapter(DATE_FORMATS),
        ManagementFiltersBehaviour,
      ],
    });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(ManagementHeader);
    await fixture.whenStable();
    const buttons = [
      ...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>(
        'app-icon-button button',
      ),
    ];

    buttons[1].click(); // the gear, after the configuration button

    expect(navigate).toHaveBeenCalledWith('/settings');
  });
});
