import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { FormWarehouseItemPopup } from './form-warehouse-item-popup';

@Component({
  imports: [FormWarehouseItemPopup],
  template: `<app-form-warehouse-item-popup [(open)]="open" />`,
})
class FormWarehouseItemPopupHost {
  readonly open = signal(false);
}

describe('FormWarehouseItemPopup', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const fixture = TestBed.createComponent(FormWarehouseItemPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.form__warehouse__item__popup');
    return { fixture, host: fixture.componentInstance, popup };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should show the four fields and the two buttons', async () => {
    const { popup } = await setup();

    expect(popup()?.querySelector('h2')?.textContent?.trim()).toBe(
      'management.settings.warehouse.form.title',
    );
    expect(popup()?.querySelectorAll('app-filled-text-field').length).toBe(1);
    expect(popup()?.querySelectorAll('app-filled-number-field').length).toBe(2);
    expect(popup()?.querySelectorAll('app-toggle').length).toBe(1);
    expect(popup()?.querySelector('app-filled-text-field input')?.getAttribute('maxlength')).toBe(
      '500',
    );
    expect(
      [...popup()!.querySelectorAll('.form__warehouse__item__popup__buttons app-button')].map(
        (button) => button.textContent?.trim(),
      ),
    ).toEqual([
      'management.settings.warehouse.form.cancel',
      'management.settings.warehouse.form.add',
    ]);
  });

  it('should close with Annulla and with the X', async () => {
    const { fixture, host, popup } = await setup();

    popup()!
      .querySelector<HTMLButtonElement>('.form__warehouse__item__popup__buttons button')!
      .click();
    await fixture.whenStable();
    expect(host.open()).toBe(false);

    host.open.set(true);
    await fixture.whenStable();
    popup()!.querySelector<HTMLButtonElement>('.form__warehouse__item__popup__close')!.click();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
  });

  it('should start from an empty form every time it opens', async () => {
    const { fixture, host, popup } = await setup();
    const name = () => popup()!.querySelector<HTMLInputElement>('app-filled-text-field input')!;

    name().value = 'Ombrellone';
    name().dispatchEvent(new Event('input'));
    host.open.set(false);
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();

    expect(name().value).toBe('');
  });
});
