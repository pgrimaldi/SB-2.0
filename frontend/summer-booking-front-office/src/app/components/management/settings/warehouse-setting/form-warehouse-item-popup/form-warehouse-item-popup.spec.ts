import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
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
    const comboboxList = vi.fn(() =>
      of([
        { id: 'a1', value: 'Lettino' },
        { id: 'a2', value: 'Ombrellone' },
      ]),
    );
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: WarehouseService, useValue: { comboboxList } },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(FormWarehouseItemPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.form__warehouse__item__popup');
    const trigger = () => popup()!.querySelector<HTMLElement>('.mat-mdc-select-trigger')!;
    const openSelect = async () => {
      trigger().click();
      await fixture.whenStable();
      return [...document.querySelectorAll<HTMLElement>('mat-option')];
    };
    return { fixture, host: fixture.componentInstance, popup, trigger, openSelect, comboboxList };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should show the four fields and the two buttons', async () => {
    const { popup } = await setup();

    expect(popup()?.querySelector('h2')?.textContent?.trim()).toBe(
      'management.settings.warehouse.form.title',
    );
    expect(popup()?.querySelectorAll('app-filled-select').length).toBe(1);
    expect(popup()?.querySelectorAll('app-filled-number-field').length).toBe(2);
    expect(popup()?.querySelectorAll('app-toggle').length).toBe(1);
    expect(
      [...popup()!.querySelectorAll('.form__warehouse__item__popup__buttons app-button')].map(
        (button) => button.textContent?.trim(),
      ),
    ).toEqual([
      'management.settings.warehouse.form.cancel',
      'management.settings.warehouse.form.add',
    ]);
  });

  it('should offer the articles of the property in the select', async () => {
    const { openSelect, comboboxList } = await setup();

    expect(comboboxList).toHaveBeenCalledWith({ idProperty: 'p1' });
    expect((await openSelect()).map((option) => option.textContent?.trim())).toEqual([
      'Lettino',
      'Ombrellone',
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

  it('should start from an empty form, with the articles loaded again, every time it opens', async () => {
    const { fixture, host, trigger, openSelect, comboboxList } = await setup();

    (await openSelect())[1].click();
    await fixture.whenStable();
    expect(trigger().textContent?.trim()).toBe('Ombrellone');

    host.open.set(false);
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();

    expect(trigger().textContent?.trim()).toBe('management.settings.warehouse.form.choose');
    expect(comboboxList).toHaveBeenCalledTimes(2);
  });
});
