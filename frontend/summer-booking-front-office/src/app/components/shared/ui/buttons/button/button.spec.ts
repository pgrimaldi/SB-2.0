import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Button } from './button';

@Component({
  imports: [Button],
  template: `
    <app-button class="primary">Accedi</app-button>
    <app-button class="secondary" appearance="secondary">Annulla</app-button>
  `,
})
class ButtonHost {}

describe('Button', () => {
  it('should project the label for every appearance', async () => {
    const fixture = TestBed.createComponent(ButtonHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('.primary button')?.textContent?.trim()).toBe('Accedi');
    expect(element.querySelector('.secondary button')?.textContent?.trim()).toBe('Annulla');
  });
});
