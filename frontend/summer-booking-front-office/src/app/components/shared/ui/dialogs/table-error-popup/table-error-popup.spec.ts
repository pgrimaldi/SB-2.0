import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TableErrorPopup } from './table-error-popup';

@Component({
  imports: [TableErrorPopup],
  template: `<app-table-error-popup
    title="Errore"
    text="In questo momento non riusciamo a fornire le informazioni richieste."
    [texts]="{ close: 'Chiudi', retry: 'Riprova' }"
    [isCloseButtonHidden]="hideClose()"
    [isRetryButtonHidden]="hideRetry()"
    [(open)]="open"
    (retry)="retries = retries + 1"
  />`,
})
class TableErrorPopupHost {
  readonly open = signal(false);
  readonly hideClose = signal(false);
  readonly hideRetry = signal(false);
  retries = 0;
}

describe('TableErrorPopup', () => {
  const setup = async (prepare?: (host: TableErrorPopupHost) => void) => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(TableErrorPopupHost);
    prepare?.(fixture.componentInstance);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = () => document.querySelector<HTMLElement>('mat-dialog-container');
    const buttons = () =>
      [...(dialog()?.querySelectorAll<HTMLElement>('app-button') ?? [])].map((button) =>
        button.textContent?.trim(),
      );
    const click = async (index: number) => {
      dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')[index].click();
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, dialog, buttons, click };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should show title and text, with close on the left and retry on the right', async () => {
    const { dialog, buttons } = await setup();

    const title = dialog()!.querySelector('.table__error__popup__title')!;
    const text = dialog()!.querySelector('.table__error__popup__text')!;
    expect(title.textContent?.trim()).toBe('Errore');
    expect(text.textContent?.trim()).toBe(
      'In questo momento non riusciamo a fornire le informazioni richieste.',
    );
    expect(buttons()).toEqual(['Chiudi', 'Riprova']);
  });

  it('should only close with close', async () => {
    const { host, dialog, click } = await setup();

    await click(0);

    expect(host.open()).toBe(false);
    expect(dialog()).toBeNull();
    expect(host.retries).toBe(0);
  });

  it('should close and ask to load again with retry', async () => {
    const { host, dialog, click } = await setup();

    await click(1);

    expect(host.open()).toBe(false);
    expect(dialog()).toBeNull();
    expect(host.retries).toBe(1);
  });

  it('should be closed at once on retry, so a new error opens it again', async () => {
    const { fixture, host, dialog } = await setup();

    dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')[1].click();
    expect(host.open()).toBe(false); // not after the closing animation
    await fixture.whenStable();

    host.open.set(true); // the data failed again
    await fixture.whenStable();
    expect(dialog()).not.toBeNull();
  });

  it('should close with Esc as well', async () => {
    const { fixture, host, dialog } = await setup();

    document.body.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }),
    );
    await fixture.whenStable();

    expect(host.open()).toBe(false);
    expect(dialog()).toBeNull();
  });

  it('should hide each button on request, and the button row without buttons', async () => {
    let popup = await setup((host) => host.hideClose.set(true));
    expect(popup.buttons()).toEqual(['Riprova']);
    TestBed.resetTestingModule();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());

    popup = await setup((host) => host.hideRetry.set(true));
    expect(popup.buttons()).toEqual(['Chiudi']);
    TestBed.resetTestingModule();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());

    popup = await setup((host) => {
      host.hideClose.set(true);
      host.hideRetry.set(true);
    });
    expect(popup.dialog()!.querySelector('.table__error__popup__actions')).toBeNull();
  });
});
