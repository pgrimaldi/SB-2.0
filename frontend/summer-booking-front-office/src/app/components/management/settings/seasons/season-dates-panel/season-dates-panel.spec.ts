import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { SeasonDatesBehaviour } from '../../../../../behaviours/settings/season-dates.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
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
    version: 'v1',
  };

  const setup = async (saveSeasonDates = vi.fn()) => {
    const seasonDates = vi.fn((): Observable<SeasonDates> => of(DATES));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        SeasonDatesBehaviour,
        {
          provide: SeasonService,
          useValue: { seasonDates, periodList: () => of([]), saveSeasonDates },
        },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(SeasonDatesPanel);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    const buttons = () => [
      ...element.querySelectorAll<HTMLButtonElement>('.season__dates__panel__actions button'),
    ];
    buttons()[0].click();
    await fixture.whenStable();
    const endsOn = fixture.debugElement.queryAll(By.directive(FilledDateField))[1]
      .componentInstance as FilledDateField;
    const choose = async (date: string) => {
      endsOn.value.set(date);
      await fixture.whenStable();
    };
    return { fixture, element, buttons, endsOn, choose, seasonDates };
  };

  it('should not count as a change a date chosen again as it was', async () => {
    const { fixture, choose } = await setup();

    await choose('2026-09-29T00:00:00.000Z');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await choose(DATES.endsOn!);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should read the dates again and say why when someone changed them after the read', async () => {
    const saveSeasonDates = vi.fn(() =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    const { fixture, element, buttons, endsOn, choose, seasonDates } = await setup(saveSeasonDates);
    await choose('2026-09-29T00:00:00.000Z');

    buttons()[1].click();
    await fixture.whenStable();
    expect(saveSeasonDates).toHaveBeenCalledWith(
      { ...DATES, endsOn: '2026-09-29T00:00:00.000Z' },
      'v1',
    );
    expect(seasonDates).toHaveBeenCalledTimes(2);
    expect(endsOn.value()).toBe(DATES.endsOn);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.season__dates__panel__error')).toBeNull();
  });
});
