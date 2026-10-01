import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialogConfig } from '@angular/material/dialog';
import { BasePopup } from './base-popup';

/** The smallest popup: a text and a button that closes it. */
@Component({
  selector: 'app-test-popup',
  template: `<ng-template #content>
    <p class="test__popup__text">Testo</p>
    <button type="button" (click)="close()">Chiudi</button>
  </ng-template>`,
})
class TestPopup extends BasePopup {
  protected dialogConfig(): MatDialogConfig {
    return { panelClass: 'test__popup__panel', ariaLabel: 'Popup di prova' };
  }
}

@Component({
  imports: [TestPopup],
  template: `@if (shown()) {
    <app-test-popup [(open)]="open" />
  }`,
})
class BasePopupHost {
  readonly shown = signal(true);
  readonly open = signal(false);
}

describe('BasePopup', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(BasePopupHost);
    await fixture.whenStable();
    const panel = () => document.querySelector('.test__popup__panel');
    return { fixture, host: fixture.componentInstance, panel };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should open with the options of the popup while open is true', async () => {
    const { fixture, host, panel } = await setup();
    expect(panel()).toBeNull();

    host.open.set(true);
    await fixture.whenStable();

    expect(panel()?.querySelector('.test__popup__text')?.textContent).toBe('Testo');
    expect(document.querySelector('mat-dialog-container')?.getAttribute('aria-label')).toBe(
      'Popup di prova',
    );
  });

  it('should be closed at once by close(), and open again right away', async () => {
    const { fixture, host, panel } = await setup();
    host.open.set(true);
    await fixture.whenStable();

    panel()!.querySelector('button')!.click();
    expect(host.open()).toBe(false); // not after the closing animation
    await fixture.whenStable();
    expect(panel()).toBeNull();

    host.open.set(true);
    await fixture.whenStable();
    expect(panel()).not.toBeNull();
  });

  it('should close when the page that holds it goes away', async () => {
    const { fixture, host, panel } = await setup();
    host.open.set(true);
    await fixture.whenStable();

    host.shown.set(false);
    await fixture.whenStable();

    expect(panel()).toBeNull();
  });
});
