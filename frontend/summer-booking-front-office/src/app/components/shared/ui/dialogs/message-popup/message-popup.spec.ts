import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MessagePopup } from './message-popup';

@Component({
  imports: [MessagePopup],
  template: `<app-message-popup
    title="Messaggio inviato con successo"
    text="Grazie per averci contattato, ti risponderemo al più presto"
    [texts]="{ close: 'Chiudi' }"
    [(open)]="open"
  />`,
})
class MessagePopupHost {
  readonly open = signal(false);
}

describe('MessagePopup', () => {
  it('should show title, text and Chiudi, which closes it', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(MessagePopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = document.querySelector<HTMLElement>('[role="dialog"]')!;

    expect(dialog.querySelector('h2')?.textContent?.trim()).toBe('Messaggio inviato con successo');
    expect(dialog.getAttribute('aria-describedby')).toBe(dialog.querySelector('p')!.id);
    const close = dialog.querySelector<HTMLButtonElement>('app-button button')!;
    expect(close.textContent?.trim()).toBe('Chiudi');
    close.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.open()).toBe(false);

    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });
});
