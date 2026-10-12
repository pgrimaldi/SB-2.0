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
});
