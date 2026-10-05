import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { AlertPopup } from './alert-popup';

@Component({
  imports: [AlertPopup],
  template: `<app-alert-popup
    title="Disconnessione fallita"
    text="Riprova tra poco."
    [texts]="{ close: 'Ho capito' }"
    [matIcon]="matIcon()"
    [pathIcon]="pathIcon()"
    [(open)]="open"
  />`,
})
class AlertPopupHost {
  readonly open = signal(false);
  readonly matIcon = signal<string[] | null>(null);
  readonly pathIcon = signal<string[] | null>(null);
}

describe('AlertPopup', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(AlertPopupHost);
    await fixture.whenStable();
    return fixture;
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should show title and text as an alert while open, and close with its button', async () => {
    const fixture = await setup();
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = document.querySelector<HTMLElement>('[role="alertdialog"]')!;
    const title = dialog.querySelector('.alert__popup__title')!;
    const text = dialog.querySelector('.alert__popup__text')!;
    expect(title.textContent?.trim()).toBe('Disconnessione fallita');
    expect(text.textContent?.trim()).toBe('Riprova tra poco.');
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
    expect(dialog.getAttribute('aria-describedby')).toBe(text.id);
    expect(dialog.querySelector('.alert__popup__icon')).toBeNull();

    expect(dialog.querySelector('app-button')?.textContent?.trim()).toBe('Ho capito');
    dialog.querySelector<HTMLButtonElement>('app-button button')!.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.open()).toBe(false);
    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('should be closed at once by its button, so it can open again right away', async () => {
    const fixture = await setup();
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    document.querySelector<HTMLButtonElement>('[role="alertdialog"] app-button button')!.click();
    expect(fixture.componentInstance.open()).toBe(false); // not after the closing animation
    await fixture.whenStable();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    expect(document.querySelector('[role="alertdialog"]')).not.toBeNull();
  });

  it('should show the Material icon, else the image', async () => {
    const fixture = await setup();
    const icon = async () => {
      fixture.componentInstance.open.set(true);
      await fixture.whenStable();
      const element = document.querySelector('[role="alertdialog"] .alert__popup__icon')!;
      fixture.componentInstance.open.set(false);
      await fixture.whenStable();
      return element;
    };

    fixture.componentInstance.pathIcon.set(['/assets/images/info.svg']);
    const image = await icon();
    expect(image.tagName).toBe('IMG');
    expect(image.getAttribute('src')).toBe('/assets/images/info.svg');

    fixture.componentInstance.matIcon.set(['info']); // wins over the image
    const material = await icon();
    expect(material.tagName).toBe('MAT-ICON');
    expect(material.textContent?.trim()).toBe('info');
  });
});
