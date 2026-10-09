import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Button, ButtonAppearance } from './button';

@Component({
  imports: [Button],
  template: `
    <app-button class="primary">Accedi</app-button>
    <app-button class="secondary" [appearance]="ButtonAppearance.Secondary">Annulla</app-button>
  `,
})
class ButtonHost {
  protected readonly ButtonAppearance = ButtonAppearance;
}

describe('Button', () => {
  it('should project the label for every appearance', async () => {
    const fixture = TestBed.createComponent(ButtonHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('.primary button')?.textContent?.trim()).toBe('Accedi');
    expect(element.querySelector('.secondary button')?.textContent?.trim()).toBe('Annulla');
  });

  it('should show a spinner and ignore clicks while loading, keeping the label', async () => {
    @Component({
      imports: [Button],
      template: `<app-button [isLoading]="loading()" (clicked)="clicks = clicks + 1"
        >Aggiungi</app-button
      >`,
    })
    class LoadingHost {
      readonly loading = signal(true);
      clicks = 0;
    }
    const fixture = TestBed.createComponent(LoadingHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const button = element.querySelector('button')!;

    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();
    expect(button.textContent?.trim()).toBe('Aggiungi');
    button.click();
    expect(fixture.componentInstance.clicks).toBe(0);

    fixture.componentInstance.loading.set(false);
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-spinner')).toBeNull();
    button.click();
    expect(fixture.componentInstance.clicks).toBe(1);
  });

  it('should show the icon before the text', async () => {
    @Component({
      imports: [Button],
      template: `<app-button [pathIcon]="['/assets/images/mail-send-white.svg']"
        >Invia email test</app-button
      >`,
    })
    class SoftHost {}
    const fixture = TestBed.createComponent(SoftHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const label = element.querySelector('.button__label')!;

    expect(label.firstElementChild?.getAttribute('src')).toBe('/assets/images/mail-send-white.svg');
    expect(label.textContent?.trim()).toBe('Invia email test');
  });
});
