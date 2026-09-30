import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { Select, SelectOption } from './select';

@Component({
  imports: [Select],
  template: `<app-select accessibleLabel="Periodo" [options]="options" [(selected)]="selected" />`,
})
class SelectHost {
  readonly options: SelectOption<'a' | 'b'>[] = [
    { value: 'a', label: 'Prima' },
    { value: 'b', label: 'Seconda' },
  ];
  readonly selected = signal<'a' | 'b'>('a');
}

describe('Select', () => {
  it('should show the selected option and update it when another one is chosen', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(SelectHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('Prima');
    expect(element.querySelector('mat-select')?.getAttribute('aria-label')).toBe('Periodo');

    element.querySelector<HTMLElement>('.mat-mdc-select-trigger')!.click();
    await fixture.whenStable();
    const options = document.querySelectorAll<HTMLElement>('mat-option');
    expect(options.length).toBe(2);

    options[1].click();
    await fixture.whenStable();
    expect(fixture.componentInstance.selected()).toBe('b');
    expect(element.querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('Seconda');
  });
});
