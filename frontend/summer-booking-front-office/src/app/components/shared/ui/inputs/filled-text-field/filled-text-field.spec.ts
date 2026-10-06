import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FilledTextField } from './filled-text-field';

@Component({
  imports: [FilledTextField],
  template: `<app-filled-text-field
    label="Articolo"
    placeholder="Mario"
    [isMandatory]="mandatory()"
    [readonly]="locked()"
    [maxLength]="500"
    [error]="error()"
    [(value)]="name"
  />`,
})
class FilledTextFieldHost {
  readonly name = signal('Lettino');
  readonly error = signal<string | null>(null);
  readonly mandatory = signal(false);
  readonly locked = signal(false);
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

  it('should show the example and, with an error, the message tied for screen readers', async () => {
    const fixture = TestBed.createComponent(FilledTextFieldHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    expect(input.placeholder).toBe('Mario');

    fixture.componentInstance.error.set('Questo campo è obbligatorio');
    await fixture.whenStable();
    const message = element.querySelector('.filled__text__field__error')!;
    expect(message.textContent?.trim()).toBe('Questo campo è obbligatorio');
    expect(input.getAttribute('aria-describedby')).toBe(message.id);
    expect(element.querySelector('.filled__text__field__invalid')).not.toBeNull();
  });

  it('should mark a mandatory field with an asterisk and as required', async () => {
    const fixture = TestBed.createComponent(FilledTextFieldHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.filled__text__field__mandatory')).toBeNull();
    expect(element.querySelector('input')!.required).toBe(false);

    fixture.componentInstance.mandatory.set(true);
    await fixture.whenStable();
    const asterisk = element.querySelector('label .filled__text__field__mandatory')!;
    expect(asterisk.textContent).toBe('*');
    expect(asterisk.getAttribute('aria-hidden')).toBe('true');
    expect(element.querySelector('input')!.required).toBe(true);
    expect(element.querySelector('input')!.getAttribute('aria-required')).toBe('true');
  });

  it('should look locked (grey) when read-only, editable (white) otherwise', async () => {
    const fixture = TestBed.createComponent(FilledTextFieldHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const control = () => element.querySelector('.filled__text__field__control')!;

    expect(control().classList).not.toContain('filled__text__field__locked');
    fixture.componentInstance.locked.set(true);
    await fixture.whenStable();
    expect(control().classList).toContain('filled__text__field__locked');
    expect(element.querySelector('input')!.readOnly).toBe(true);
  });
});
