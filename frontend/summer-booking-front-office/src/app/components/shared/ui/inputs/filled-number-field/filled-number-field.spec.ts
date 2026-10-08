import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FilledNumberField } from './filled-number-field';

@Component({
  imports: [FilledNumberField],
  template: `<app-filled-number-field
    label="Soglia"
    [isDecimal]="decimal()"
    [isCurrency]="currency()"
    [pathIcon]="['/assets/images/euro.svg']"
    [error]="error()"
    [(value)]="threshold"
  />`,
})
class FilledNumberFieldHost {
  readonly threshold = signal<number | null>(3);
  readonly decimal = signal(false);
  readonly currency = signal(false);
  readonly error = signal<string | null>(null);
}

@Component({
  imports: [FilledNumberField],
  template: `<app-filled-number-field
    labelledBy="port-title"
    autocomplete="new-password"
    [isMasked]="true"
    [pathIcon]="['', '/assets/images/eye-close.svg', '/assets/images/eye-start.svg']"
    [texts]="{ show: 'Mostra il contenuto' }"
    [value]="587"
  />`,
})
class MaskedNumberFieldHost {}

describe('FilledNumberField', () => {
  const setup = async (decimal = false, currency = false) => {
    const fixture = TestBed.createComponent(FilledNumberFieldHost);
    fixture.componentInstance.decimal.set(decimal);
    fixture.componentInstance.currency.set(currency);
    await fixture.whenStable();
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    const type = async (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return {
      fixture,
      host: fixture.componentInstance,
      element: fixture.nativeElement as HTMLElement,
      input,
      type,
    };
  };

  it('should show its label and the number', async () => {
    const { element, input } = await setup();

    expect(element.querySelector('label')?.textContent?.trim()).toBe('Soglia');
    expect(input.getAttribute('inputmode')).toBe('numeric');
    expect(input.value).toBe('3');
  });

  it('should give back the typed number, and null when the field is empty', async () => {
    const { host, type } = await setup();

    await type('12');
    expect(host.threshold()).toBe(12);

    await type('');
    expect(host.threshold()).toBeNull();
  });

  it('should keep only the digits: no letters, exponent, signs or separators', async () => {
    const { host, input, type } = await setup();

    await type('1e5-+a,.2');
    expect(input.value).toBe('152');
    expect(host.threshold()).toBe(152);

    await type('e');
    expect(input.value).toBe('');
    expect(host.threshold()).toBeNull();
  });

  it('should accept one separator, comma or dot, with decimal', async () => {
    const { host, input, type } = await setup(true);

    expect(input.getAttribute('inputmode')).toBe('decimal');
    await type('3,');
    expect(input.value).toBe('3,');
    expect(host.threshold()).toBe(3);

    await type('3,505,1');
    expect(input.value).toBe('3,5051');
    expect(host.threshold()).toBe(3.5051);

    await type('0.25');
    expect(host.threshold()).toBe(0.25);
  });

  it('should keep at most 2 decimal digits and show the icon only for an amount', async () => {
    const { fixture, host, element, input, type } = await setup(true, true);

    expect(element.querySelector('img')?.getAttribute('src')).toBe('/assets/images/euro.svg');
    await type('12,345');
    expect(input.value).toBe('12,34');
    expect(host.threshold()).toBe(12.34);

    host.currency.set(false);
    await fixture.whenStable();
    expect(element.querySelector('img')).toBeNull();
  });

  it('should write a number given from outside', async () => {
    const { fixture, host, input } = await setup(true);

    host.threshold.set(7.5);
    await fixture.whenStable();
    expect(input.value).toBe('7.5');

    host.threshold.set(null);
    await fixture.whenStable();
    expect(input.value).toBe('');
  });

  it('should show the error under the field and link it for screen readers', async () => {
    const { fixture, host, element, input } = await setup();
    host.error.set('Il valore totale deve essere superiore a 0');
    await fixture.whenStable();
    const message = element.querySelector('.filled__number__field__error')!;

    expect(message.textContent?.trim()).toBe('Il valore totale deve essere superiore a 0');
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.getAttribute('aria-describedby')).toBe(message.id);
    expect(element.querySelector('.filled__number__field__invalid')).not.toBeNull();
  });

  it('should show the digits as dots when masked, and the eye shows and hides them', async () => {
    const fixture = TestBed.createComponent(MaskedNumberFieldHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    const eye = element.querySelector<HTMLButtonElement>('.filled__number__field__toggle')!;

    expect(element.querySelector('label')).toBeNull();
    expect(input.getAttribute('aria-labelledby')).toBe('port-title');
    expect(input.type).toBe('password');
    expect(input.getAttribute('autocomplete')).toBe('new-password');
    expect(eye.getAttribute('aria-label')).toBe('Mostra il contenuto');
    expect(eye.querySelector('img')?.getAttribute('src')).toBe('/assets/images/eye-start.svg');

    eye.click();
    await fixture.whenStable();
    expect(input.type).toBe('text');
    expect(input.value).toBe('587');
    expect(eye.getAttribute('aria-pressed')).toBe('true');
    expect(eye.querySelector('img')?.getAttribute('src')).toBe('/assets/images/eye-close.svg');
  });
});
