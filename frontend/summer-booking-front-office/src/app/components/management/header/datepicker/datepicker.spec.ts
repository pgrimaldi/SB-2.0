import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Datepicker } from './datepicker';

@Component({
  imports: [Datepicker],
  template: `<app-datepicker [(start)]="start" [(end)]="end" />`,
})
class DatepickerHost {
  readonly start = signal(new Date(2026, 8, 29));
  readonly end = signal(new Date(2026, 8, 29));
}

describe('Datepicker', () => {
  const setup = async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
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

  it('should show both dates as dd/mm/yyyy', async () => {
    const { element } = await setup();

    const values = [...element.querySelectorAll('input')].map((input) => input.value);
    expect(values).toEqual(['29/09/2026', '29/09/2026']);
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

  it('should accept a typed date and bring back the current one when the text is not a date', async () => {
    const { element, days } = await setup();
    const input = element.querySelector('input')!;
    const type = async (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      input.dispatchEvent(new Event('change'));
      await Promise.resolve();
    };

    await type('25/09/2026');
    expect(days()).toEqual([25, 29]);

    await type('40/99/2026');
    expect(days()).toEqual([25, 29]);
  });
});
