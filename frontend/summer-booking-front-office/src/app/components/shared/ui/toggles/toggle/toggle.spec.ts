import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Toggle } from './toggle';

@Component({
  imports: [Toggle],
  template: `<app-toggle [(checked)]="alert">Abilita avviso di soglia</app-toggle>`,
})
class ToggleHost {
  readonly alert = signal(false);
}

describe('Toggle', () => {
  it('should show its label and switch on and off', async () => {
    const fixture = TestBed.createComponent(ToggleHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const button = element.querySelector<HTMLButtonElement>('button[role="switch"]')!;

    expect(element.textContent?.trim()).toBe('Abilita avviso di soglia');
    expect(button.getAttribute('aria-checked')).toBe('false');

    button.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.alert()).toBe(true);
    expect(button.getAttribute('aria-checked')).toBe('true');
  });
});
