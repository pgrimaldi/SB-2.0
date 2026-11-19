import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { SeasonDatesBehaviour } from '../../../../../behaviours/settings/season-dates.behaviour';
import { SeasonDates } from '../../../../../entities/settings/seasons/season-dates';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { SeasonDatesPanel } from './season-dates-panel';

describe('SeasonDatesPanel', () => {
  const DATES: SeasonDates = {
    startsOn: '2026-05-01T00:00:00.000Z',
    endsOn: '2026-09-30T00:00:00.000Z',
    nextStartsOn: '2027-05-01T00:00:00.000Z',
    nextEndsOn: '2027-09-30T00:00:00.000Z',
  };

  it('should not count as a change a date chosen again as it was', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        SeasonDatesBehaviour,
        {
          provide: SeasonService,
          useValue: {
            seasonDates: () => of(DATES),
            periodList: vi.fn(),
            saveSeasonDates: vi.fn(),
          },
        },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(SeasonDatesPanel);
    await fixture.whenStable();
    (fixture.nativeElement as HTMLElement)
      .querySelector<HTMLButtonElement>('.season__dates__panel__actions button')!
      .click();
    await fixture.whenStable();
    const endsOn = fixture.debugElement.queryAll(By.directive(FilledDateField))[1]
      .componentInstance as FilledDateField;
    const choose = async (date: string) => {
      endsOn.value.set(date);
      await fixture.whenStable();
    };

    await choose('2026-09-29T00:00:00.000Z');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await choose(DATES.endsOn!);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });
});
