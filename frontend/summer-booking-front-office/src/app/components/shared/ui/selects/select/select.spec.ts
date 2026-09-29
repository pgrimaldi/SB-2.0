import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Select, SelectOption } from './select';

@Component({
  imports: [Select],
  template: `<app-select label="Periodo" [options]="options" [(selected)]="selected" />`,
})
class SelectHost {
  readonly options: SelectOption<'a' | 'b'>[] = [
    { key: 'a', value: 'Prima' },
    { key: 'b', value: 'Seconda' },
  ];
  readonly selected = signal<'a' | 'b'>('a');
}

describe('Select', () => {
  it('should show the selected option and update it when another one is chosen', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const fixture = TestBed.createComponent(SelectHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('Prima');

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
