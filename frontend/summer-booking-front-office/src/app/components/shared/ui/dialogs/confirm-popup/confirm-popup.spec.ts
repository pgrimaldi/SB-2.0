import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { ConfirmPopup } from './confirm-popup';

@Component({
  imports: [ConfirmPopup],
  template: `<app-confirm-popup
    title="Elimina articolo"
    text="Vuoi eliminare l'articolo Lettino?"
    [texts]="{ cancel: 'Annulla', confirm: 'Elimina' }"
    [danger]="danger()"
    [isLoading]="loading()"
    [error]="error()"
    [(open)]="open"
    (confirmed)="confirmations = confirmations + 1"
  />`,
})
class ConfirmPopupHost {
  readonly open = signal(false);
  readonly danger = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  confirmations = 0;
}

describe('ConfirmPopup', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(ConfirmPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = () => document.querySelector<HTMLElement>('mat-dialog-container');
    const buttons = () => [...dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')];
    const escape = async () => {
      document.body.dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }),
      );
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, dialog, buttons, escape };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should ask the question with Annulla and Elimina, and close with Annulla', async () => {
    const { fixture, host, dialog, buttons } = await setup();

    expect(dialog()!.querySelector('h2')?.textContent?.trim()).toBe('Elimina articolo');
    expect(buttons().map((button) => button.textContent?.trim())).toEqual(['Annulla', 'Elimina']);
    buttons()[0].click();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.confirmations).toBe(0);
  });

  it('should only tell the caller on Elimina, staying open', async () => {
    const { fixture, host, buttons } = await setup();

    buttons()[1].click();
    await fixture.whenStable();
    expect(host.confirmations).toBe(1);
    expect(host.open()).toBe(true);
  });

  it('should not close or confirm again while loading, and show the error', async () => {
    const { fixture, host, dialog, buttons, escape } = await setup();
    host.loading.set(true);
    await fixture.whenStable();

    expect(buttons()[0].disabled).toBe(true);
    expect(dialog()!.querySelector('mat-progress-spinner')).not.toBeNull();
    buttons()[1].click();
    await escape();
    expect(host.confirmations).toBe(0);
    expect(host.open()).toBe(true);

    host.loading.set(false);
    host.error.set('Alcuni dati non sono validi. Controlla e riprova.');
    await fixture.whenStable();
    expect(dialog()!.querySelector('.confirm__popup__error')?.textContent?.trim()).toBe(
      'Alcuni dati non sono validi. Controlla e riprova.',
    );
    await escape();
    expect(host.open()).toBe(false);
  });

  it('should make the confirm button red only for an action that cannot be undone', async () => {
    const { fixture, host, dialog } = await setup();
    const confirm = () => dialog()!.querySelectorAll('app-button')[1];

    expect(confirm().classList).not.toContain('confirm__popup__danger');
    host.danger.set(true);
    await fixture.whenStable();
    expect(confirm().classList).toContain('confirm__popup__danger');
  });
});
