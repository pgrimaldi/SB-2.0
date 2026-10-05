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
    [(value)]="chosen"
  />`,
})
class FilledSelectHost {
  readonly options = [
    { value: 'a1', label: 'Lettino' },
    { value: 'a2', label: 'Ombrellone' },
  ];
  readonly chosen = signal<string | null>(null);
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

  it('should give back the chosen value', async () => {
    const { fixture, host, element } = await setup();

    element.querySelector<HTMLElement>('.mat-mdc-select-trigger')!.click();
    await fixture.whenStable();
    document.querySelectorAll<HTMLElement>('mat-option')[0].click();
    await fixture.whenStable();

    expect(host.chosen()).toBe('a1');
    expect(element.querySelector('.mat-mdc-select-trigger')?.textContent?.trim()).toBe('Lettino');
  });
});
