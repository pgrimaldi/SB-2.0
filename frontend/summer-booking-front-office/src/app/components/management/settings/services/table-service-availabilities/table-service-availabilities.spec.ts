import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { TableServiceAvailabilityDay } from '../../../../../entities/settings/services/table-service-availability-day';
import { TableServiceAvailabilityDraft } from '../../../../../entities/settings/services/table-service-availability-draft';
import { TableServiceAvailabilities } from './table-service-availabilities';

describe('TableServiceAvailabilities', () => {
  const day = (
    dayOfWeek: number,
    isEnabled: boolean,
    startTime: string | null,
    endTime: string | null,
  ) =>
    Object.assign(new TableServiceAvailabilityDay(), { dayOfWeek, isEnabled, startTime, endTime });

  const setup = async (days: TableServiceAvailabilityDay[]) => {
    // jsdom has no matchMedia.
    vi.stubGlobal(
      'matchMedia',
      vi.fn(() => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      })),
    );
    TestBed.configureTestingModule({
      providers: [provideNativeDateAdapter(), provideTranslateService()],
    });
    const model = signal([Object.assign(new TableServiceAvailabilityDraft(), { days })]);
    const availabilities = TestBed.runInInjectionContext(() => form(model));
    const fixture = TestBed.createComponent(TableServiceAvailabilities);
    fixture.componentRef.setInput('form', availabilities);
    await fixture.whenStable();
    const choose = () => fixture.componentInstance['chooseSameTime'](availabilities[0], true);
    const times = () => model()[0].days.map(({ startTime, endTime }) => [startTime, endTime]);
    return { model, choose, times };
  };

  it('should take the hours of the first day chosen for all the days, when none are written', async () => {
    const { model, choose, times } = await setup([
      day(1, false, '08:00', '09:00'),
      day(2, true, '12:00', null),
      day(3, true, '12:00', '15:00'),
      day(4, true, '19:00', '22:00'),
    ]);

    choose();
    expect(model()[0]).toMatchObject({ startTime: '12:00', endTime: '15:00' });
    expect(times()).toEqual(Array(4).fill(['12:00', '15:00']));
  });

  it('should leave the hours of the days when there are none to take', async () => {
    const { model, choose, times } = await setup([
      day(1, true, '12:00', null),
      day(2, true, null, null),
    ]);

    choose();
    expect(model()[0]).toMatchObject({ startTime: null, endTime: null });
    expect(times()).toEqual([
      ['12:00', null],
      [null, null],
    ]);
  });
});
