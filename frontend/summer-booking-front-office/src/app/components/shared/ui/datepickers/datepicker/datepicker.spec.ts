import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  MAT_DATE_LOCALE,
  MAT_NATIVE_DATE_FORMATS,
  provideNativeDateAdapter,
} from '@angular/material/core';
import { Datepicker } from './datepicker';

/** The date adapter of the test: Italian dates with day and month on 2 digits (dd/mm/yyyy). */
const FORMATS = {
  ...MAT_NATIVE_DATE_FORMATS,
  display: {
    ...MAT_NATIVE_DATE_FORMATS.display,
    dateInput: { day: '2-digit', month: '2-digit', year: 'numeric' },
  },
} as const;

@Component({
  imports: [Datepicker],
  template: `<app-datepicker
    [pathIcon]="['/calendar.svg', '/left.svg', '/right.svg']"
    [texts]="texts"
    [(start)]="start"
    [(end)]="end"
  />`,
})
class DatepickerHost {
  readonly start = signal(new Date(2026, 8, 29));
  readonly end = signal(new Date(2026, 8, 29));
  readonly texts = {
    start: { label: 'Data di inizio', previous: 'Inizio indietro', next: 'Inizio avanti' },
    end: { label: 'Data di fine', previous: 'Fine indietro', next: 'Fine avanti' },
  };
}

describe('Datepicker', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(FORMATS),
        { provide: MAT_DATE_LOCALE, useValue: 'it-IT' },
      ],
    });
    const fixture = TestBed.createComponent(DatepickerHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const arrows = element.querySelectorAll<HTMLButtonElement>('.datepicker__arrow');
    const click = async (index: number) => {
      arrows[index].click();
      await fixture.whenStable();
    };
    const days = () => [
      fixture.componentInstance.start().getDate(),
      fixture.componentInstance.end().getDate(),
    ];
    return { element, click, days };
  };

  it('should show both dates as the date adapter writes them (here Italian: dd/mm/yyyy)', async () => {
    const { element } = await setup();

    const values = [...element.querySelectorAll('input')].map((input) => input.value);
    expect(values).toEqual(['29/09/2026', '29/09/2026']);
  });

  it('should name fields and arrows with the given texts', async () => {
    const { element } = await setup();

    const labels = [...element.querySelectorAll('.datepicker [aria-label]')].map((named) =>
      named.getAttribute('aria-label'),
    );
    expect(labels).toEqual([
      'Inizio indietro',
      'Data di inizio',
      'Inizio avanti',
      'Fine indietro',
      'Data di fine',
      'Fine avanti',
    ]);
  });

  it('should place its icons in order [calendar, previous day, next day]', async () => {
    const { element } = await setup();

    const images = [...element.querySelectorAll('.datepicker img')].map((image) =>
      image.getAttribute('src'),
    );
    expect(images).toEqual(['/calendar.svg', '/left.svg', '/right.svg', '/left.svg', '/right.svg']);
  });

  it('should move a date by one day and never let the start pass the end', async () => {
    const { click, days } = await setup();

    await click(1); // start ▶ drags the end along
    expect(days()).toEqual([30, 30]);

    await click(0); // start ◀ moves only the start
    expect(days()).toEqual([29, 30]);

    await click(2); // end ◀
    await click(2); // end ◀ below the start drags the start along
    expect(days()).toEqual([28, 28]);

    await click(3); // end ▶
    expect(days()).toEqual([28, 29]);
  });

  it('should not accept typed dates: the calendar opens instead', async () => {
    const { element } = await setup();

    element.querySelectorAll('input').forEach((input) => expect(input.readOnly).toBe(true));
  });
});
