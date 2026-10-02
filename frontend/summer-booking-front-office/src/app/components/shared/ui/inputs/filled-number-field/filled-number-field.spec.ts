import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FilledNumberField } from './filled-number-field';

@Component({
  imports: [FilledNumberField],
  template: `<app-filled-number-field label="Soglia" [(value)]="threshold" />`,
})
class FilledNumberFieldHost {
  readonly threshold = signal<number | null>(3);
}

describe('FilledNumberField', () => {
  const setup = async () => {
    const fixture = TestBed.createComponent(FilledNumberFieldHost);
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    const type = (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
    };
    return {
      host: fixture.componentInstance,
      element: fixture.nativeElement as HTMLElement,
      input,
      type,
    };
  };

  it('should show its label and the number', async () => {
    const { element, input } = await setup();

    expect(element.querySelector('label')?.textContent?.trim()).toBe('Soglia');
    expect(input.type).toBe('number');
    expect(input.value).toBe('3');
  });

  it('should give back the typed number, and null when the field is empty', async () => {
    const { host, type } = await setup();

    type('12');
    expect(host.threshold()).toBe(12);

    type('');
    expect(host.threshold()).toBeNull();
  });
});
