import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { FormWarehouseAddItemPopup } from './form-warehouse-add-item-popup';

@Component({
  imports: [FormWarehouseAddItemPopup],
  template: `<app-form-warehouse-add-item-popup [(open)]="open" (added)="added = added + 1" />`,
})
class FormWarehouseAddItemPopupHost {
  readonly open = signal(false);
  added = 0;
}

describe('FormWarehouseAddItemPopup', () => {
  const setup = async () => {
    const comboboxList = vi.fn(() =>
      of([
        { id: 'a1', value: 'Lettino' },
        { id: 'a2', value: 'Ombrellone' },
      ]),
    );
    const addWarehouseItem = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: WarehouseService, useValue: { comboboxList, addWarehouseItem } },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(FormWarehouseAddItemPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.form__warehouse__add__item__popup');
    const trigger = () => popup()!.querySelector<HTMLElement>('.mat-mdc-select-trigger')!;
    const openSelect = async () => {
      trigger().click();
      await fixture.whenStable();
      return [...document.querySelectorAll<HTMLElement>('mat-option')];
    };
    return {
      fixture,
      host: fixture.componentInstance,
      popup,
      trigger,
      openSelect,
      comboboxList,
      addWarehouseItem,
    };
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
      [...popup()!.querySelectorAll('.form__warehouse__add__item__popup__buttons app-button')].map(
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
      .querySelector<HTMLButtonElement>('.form__warehouse__add__item__popup__buttons button')!
      .click();
    await fixture.whenStable();
    expect(host.open()).toBe(false);

    host.open.set(true);
    await fixture.whenStable();
    popup()!.querySelector<HTMLButtonElement>('.form__warehouse__add__item__popup__close')!.click();
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

  it('should let the threshold alert be turned on only with a threshold above 0', async () => {
    const { fixture, popup } = await setup();
    const threshold = () =>
      popup()!.querySelectorAll('app-filled-number-field input')[1] as HTMLInputElement;
    const toggle = () => popup()!.querySelector('button[role="switch"]') as HTMLButtonElement;
    const type = async (text: string) => {
      threshold().value = text;
      threshold().dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };

    expect(toggle().disabled).toBe(true);
    await type('0');
    expect(toggle().disabled).toBe(true);

    await type('5');
    expect(toggle().disabled).toBe(false);
    toggle().click();
    await fixture.whenStable();
    expect(toggle().getAttribute('aria-checked')).toBe('true');

    // Without a threshold above 0 the alert goes off and stays off.
    await type('');
    expect(toggle().disabled).toBe(true);
    expect(toggle().getAttribute('aria-checked')).toBe('false');
    await type('3');
    expect(toggle().getAttribute('aria-checked')).toBe('false');
  });

  /** Fills the whole form with the alert on; returns a getter of the Aggiungi button. */
  const fill = async ({ fixture, popup, openSelect }: Awaited<ReturnType<typeof setup>>) => {
    const add = () =>
      [...popup()!.querySelectorAll<HTMLButtonElement>('app-button button')].find((button) =>
        button.textContent?.includes('add'),
      )!;
    expect(add().disabled).toBe(true);
    (await openSelect())[1].click();
    await fixture.whenStable();
    const [quantity, threshold] = popup()!.querySelectorAll<HTMLInputElement>(
      'app-filled-number-field input',
    );
    for (const [input, text] of [
      [quantity, '7'],
      [threshold, '2'],
    ] as const) {
      input.value = text;
      input.dispatchEvent(new Event('input'));
    }
    await fixture.whenStable();
    popup()!.querySelector<HTMLButtonElement>('button[role="switch"]')!.click();
    await fixture.whenStable();
    expect(add().disabled).toBe(false);
    return add;
  };

  it('should add the article with a spinner, then close and tell the page', async () => {
    const context = await setup();
    const { fixture, host, popup, addWarehouseItem } = context;
    const answer = new Subject<void>();
    addWarehouseItem.mockReturnValue(answer);
    const add = await fill(context);

    add().click();
    await fixture.whenStable();
    expect(addWarehouseItem).toHaveBeenCalledWith({
      idProperty: 'p1',
      idArticle: 'a2',
      articleQuantity: 7,
      thresholdQuantity: 2,
      isThresholdWarningActive: true,
    });
    expect(popup()!.querySelector('mat-progress-spinner')).not.toBeNull();
    expect(
      popup()!.querySelector('.form__warehouse__add__item__popup__fields')!.hasAttribute('inert'),
    ).toBe(true);
    add().click();
    expect(addWarehouseItem).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.added).toBe(1);
  });

  it('should show the error of the API and keep the form when the article is not added', async () => {
    const context = await setup();
    const { fixture, host, popup, trigger, addWarehouseItem } = context;
    addWarehouseItem.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { status: 400, code: 'validation.invalid_request', title: 'Invalid request' },
          }),
      ),
    );
    const add = await fill(context);

    add().click();
    await fixture.whenStable();
    expect(host.open()).toBe(true);
    expect(host.added).toBe(0);
    // The message of the code (here the fallback key: the test has no translations).
    expect(popup()!.querySelector('[role="alert"]')?.textContent?.trim()).toBe('error.unknown');
    expect(popup()!.querySelector('mat-progress-spinner')).toBeNull();
    expect(trigger().textContent?.trim()).toBe('Ombrellone');
  });

  it('should keep Aggiungi off with a quantity of 0', async () => {
    const context = await setup();
    const { fixture, popup } = context;
    const add = await fill(context);
    const quantity = popup()!.querySelector<HTMLInputElement>('app-filled-number-field input')!;

    quantity.value = '0';
    quantity.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(add().disabled).toBe(true);
  });
});
