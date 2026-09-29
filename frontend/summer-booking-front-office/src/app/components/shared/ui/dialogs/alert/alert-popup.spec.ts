import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { AlertPopup } from './alert-popup';

@Component({
  imports: [AlertPopup],
  template: `<app-alert-popup title="alert.title" text="alert.text" [(open)]="open" />`,
})
class AlertPopupHost {
  readonly open = signal(false);
}

describe('AlertPopup', () => {
  it('should show title and text as an alert while open, and close with its button', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const fixture = TestBed.createComponent(AlertPopupHost);
    await fixture.whenStable();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = document.querySelector<HTMLElement>('[role="alertdialog"]')!;
    const title = dialog.querySelector('.alert__popup__title')!;
    const text = dialog.querySelector('.alert__popup__text')!;
    expect(title.textContent?.trim()).toBe('alert.title'); // untranslated key in the test
    expect(text.textContent?.trim()).toBe('alert.text');
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
    expect(dialog.getAttribute('aria-describedby')).toBe(text.id);

    dialog.querySelector<HTMLButtonElement>('app-button button')!.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.open()).toBe(false);
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
  });
});
