import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Button } from './button';

describe('Button', () => {
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
});
