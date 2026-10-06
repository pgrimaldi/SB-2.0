import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { FilledSelect } from './filled-select';

@Component({
  imports: [FilledSelect],
  template: `<app-filled-select
    label="Articolo"
    placeholder="Seleziona"
    [options]="options"
    [disabled]="disabled()"
    [isMandatory]="mandatory()"
    [error]="error()"
    [(value)]="chosen"
  />`,
})
class FilledSelectHost {
  readonly options = [
    { value: 'a1', label: 'Lettino' },
    { value: 'a2', label: 'Ombrellone' },
  ];
  readonly chosen = signal<string | null>(null);
  readonly disabled = signal(false);
  readonly mandatory = signal(false);
  readonly error = signal<string | null>(null);
}

describe('FilledSelect', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(FilledSelectHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    return { fixture, host: fixture.componentInstance, element };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should show its label, named by it, and the placeholder while nothing is chosen', async () => {
    const { element } = await setup();
    const label = element.querySelector('label')!;

    expect(label.textContent?.trim()).toBe('Articolo');
    expect(element.querySelector('mat-select')?.getAttribute('aria-labelledby')).toContain(
      label.id,
    );
    expect(element.querySelector('.mat-mdc-select-trigger')?.textContent?.trim()).toBe('Seleziona');
  });

  it('should show the error under the select and link it for screen readers', async () => {
    const { fixture, host, element } = await setup();
    host.error.set('Il valore scelto non è valido.');
    await fixture.whenStable();
    const message = element.querySelector('.filled__select__error')!;
    const select = element.querySelector('mat-select')!;

    expect(message.textContent?.trim()).toBe('Il valore scelto non è valido.');
    expect(select.getAttribute('aria-invalid')).toBe('true');
    expect(select.getAttribute('aria-describedby')).toContain(message.id);
    expect(element.querySelector('.filled__select__invalid')).not.toBeNull();
  });

  it('should give back the chosen value', async () => {
    const { fixture, host, element } = await setup();

    element.querySelector<HTMLElement>('.mat-mdc-select-trigger')!.click();
    await fixture.whenStable();
    document.querySelectorAll<HTMLElement>('mat-option')[0].click();
    await fixture.whenStable();

    expect(host.chosen()).toBe('a1');
    expect(element.querySelector('.mat-mdc-select-trigger')?.textContent?.trim()).toBe('Lettino');
  });

  it('should show the value without opening while disabled', async () => {
    const { fixture, host, element } = await setup();
    host.chosen.set('a2');
    host.disabled.set(true);
    await fixture.whenStable();

    expect(element.querySelector('.mat-mdc-select-trigger')?.textContent?.trim()).toBe(
      'Ombrellone',
    );
    expect(element.querySelector('mat-select')?.getAttribute('aria-disabled')).toBe('true');
    element.querySelector<HTMLElement>('.mat-mdc-select-trigger')!.click();
    await fixture.whenStable();
    expect(document.querySelectorAll('mat-option').length).toBe(0);
  });

  it('should mark a mandatory select with an asterisk and as required', async () => {
    const { fixture, host, element } = await setup();
    host.mandatory.set(true);
    await fixture.whenStable();

    expect(element.querySelector('label .filled__select__mandatory')?.textContent).toBe('*');
    expect(element.querySelector('mat-select')?.getAttribute('aria-required')).toBe('true');
  });
});
