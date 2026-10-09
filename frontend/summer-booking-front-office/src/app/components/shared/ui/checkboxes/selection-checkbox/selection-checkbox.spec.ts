import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SelectionCheckbox } from './selection-checkbox';

@Component({
  imports: [SelectionCheckbox],
  template: `<app-selection-checkbox [isIndeterminate]="partial()" [(checked)]="chosen" />`,
})
class SelectionCheckboxHost {
  readonly chosen = signal(false);
  readonly partial = signal(false);
}

describe('SelectionCheckbox', () => {
  it('should tell when it is ticked', async () => {
    const fixture = TestBed.createComponent(SelectionCheckboxHost);
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');

    input.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.chosen()).toBe(true);
  });

  it('should show the dash of a partial choice', async () => {
    const fixture = TestBed.createComponent(SelectionCheckboxHost);
    fixture.componentInstance.partial.set(true);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('input').indeterminate).toBe(true);
  });
});
