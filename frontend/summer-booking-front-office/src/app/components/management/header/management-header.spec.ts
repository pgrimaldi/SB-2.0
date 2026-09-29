import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { format, startOfToday } from 'date-fns';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { ManagementHeader } from './management-header';

describe('ManagementHeader', () => {
  it('should switch to "Non oggi" when a date moves and bring both dates back to today on click', async () => {
    TestBed.configureTestingModule({
      providers: [provideTranslateService(), ManagementFiltersBehaviour],
    });
    const fixture = TestBed.createComponent(ManagementHeader);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const today = element.querySelector<HTMLButtonElement>('.management__header__today')!;
    const dates = () =>
      [...element.querySelectorAll<HTMLInputElement>('.datepicker__input')].map(
        (input) => input.value,
      );
    const todayText = format(startOfToday(), 'dd/MM/yyyy');

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
});
