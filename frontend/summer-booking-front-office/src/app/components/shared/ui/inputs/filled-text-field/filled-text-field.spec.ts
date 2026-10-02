import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FilledTextField } from './filled-text-field';

@Component({
  imports: [FilledTextField],
  template: `<app-filled-text-field label="Articolo" [maxLength]="500" [(value)]="name" />`,
})
class FilledTextFieldHost {
  readonly name = signal('Lettino');
}

describe('FilledTextField', () => {
  it('should show its label, tied to the field, and the most characters allowed', async () => {
    const fixture = TestBed.createComponent(FilledTextFieldHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;

    expect(element.querySelector('label')?.textContent?.trim()).toBe('Articolo');
    expect(element.querySelector('label')?.getAttribute('for')).toBe(input.id);
    expect(input.value).toBe('Lettino');
    expect(input.getAttribute('maxlength')).toBe('500');
  });

  it('should give back what is typed', async () => {
    const fixture = TestBed.createComponent(FilledTextFieldHost);
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');

    input.value = 'Ombrellone';
    input.dispatchEvent(new Event('input'));

    expect(fixture.componentInstance.name()).toBe('Ombrellone');
  });
});
