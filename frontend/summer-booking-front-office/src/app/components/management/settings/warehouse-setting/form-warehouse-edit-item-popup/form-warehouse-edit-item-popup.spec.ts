import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { WarehouseItem } from '../../../../../entities/warehouse/warehouse-item';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { FormWarehouseEditItemPopup } from './form-warehouse-edit-item-popup';

const LETTINO: WarehouseItem = {
  idArticle: 'a1',
  name: 'Lettino',
  totalQuantity: 120,
  availableQuantity: 84,
  thresholdQuantity: 5,
  isThresholdWarningActive: true,
};

@Component({
  imports: [FormWarehouseEditItemPopup],
  template: `<app-form-warehouse-edit-item-popup
    [item]="item()"
    [(open)]="open"
    (saved)="saved = saved + 1"
  />`,
})
class FormWarehouseEditItemPopupHost {
  readonly item = signal<WarehouseItem | null>(LETTINO);
  readonly open = signal(false);
  saved = 0;
}

describe('FormWarehouseEditItemPopup', () => {
  const setup = async () => {
    const editWarehouseItem = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: WarehouseService, useValue: { editWarehouseItem } },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(FormWarehouseEditItemPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.form__warehouse__edit__item__popup');
    const inputs = () =>
      [...popup()!.querySelectorAll<HTMLInputElement>('app-filled-number-field input')] as const;
    const toggle = () => popup()!.querySelector<HTMLButtonElement>('button[role="switch"]')!;
    const save = () =>
      [...popup()!.querySelectorAll<HTMLButtonElement>('app-button button')].find((button) =>
        button.textContent?.includes('save'),
      )!;
    const type = async (input: HTMLInputElement, text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return {
      fixture,
      host: fixture.componentInstance,
      popup,
      inputs,
      toggle,
      save,
      type,
      editWarehouseItem,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should detect changes against the opening values and forget them when reopened', async () => {
    const { fixture, host, inputs, toggle, type } = await setup();
    const component: FormWarehouseEditItemPopup = fixture.debugElement.query(
      By.directive(FormWarehouseEditItemPopup),
    ).componentInstance;

    expect(component.hasUnsavedChanges()).toBe(false);
    await type(inputs()[0], '');
    expect(component.hasUnsavedChanges()).toBe(true);
    await type(inputs()[0], '120');
    expect(component.hasUnsavedChanges()).toBe(false);
    toggle().click();
    await fixture.whenStable();
    expect(component.hasUnsavedChanges()).toBe(true);

    host.open.set(false);
    await fixture.whenStable();
    expect(component.hasUnsavedChanges()).toBe(false);
    host.open.set(true);
    await fixture.whenStable();
    expect(component.hasUnsavedChanges()).toBe(false);
  });

  it('should show the values of the row, with the article locked', async () => {
    const { popup, inputs, toggle } = await setup();
    const select = popup()!.querySelector('mat-select')!;

    expect(popup()!.querySelector('h2')?.textContent?.trim()).toBe(
      'management.settings.warehouse.form.edit_title',
    );
    expect(select.textContent?.trim()).toBe('Lettino');
    expect(select.getAttribute('aria-disabled')).toBe('true');
    expect(inputs().map((input) => input.value)).toEqual(['120', '5']);
    expect(toggle().getAttribute('aria-checked')).toBe('true');
    expect(
      [...popup()!.querySelectorAll('.form__warehouse__edit__item__popup__buttons app-button')].map(
        (button) => button.textContent?.trim(),
      ),
    ).toEqual([
      'management.settings.warehouse.form.cancel',
      'management.settings.warehouse.form.save',
    ]);
  });

  it('should keep the alert off without a threshold above 0, even if the row has it on', async () => {
    const { fixture, host, toggle } = await setup();

    host.open.set(false);
    host.item.set({ ...LETTINO, thresholdQuantity: null });
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();

    expect(toggle().disabled).toBe(true);
    expect(toggle().getAttribute('aria-checked')).toBe('false');
  });

  it('should let Salva work only with a total above 0', async () => {
    const { inputs, save, type } = await setup();

    expect(save().disabled).toBe(false);
    await type(inputs()[0], '0');
    expect(save().disabled).toBe(true);
    await type(inputs()[0], '');
    expect(save().disabled).toBe(true);
    await type(inputs()[0], '3');
    expect(save().disabled).toBe(false);
  });

  it('should save the new values with a spinner, then close and tell the page', async () => {
    const { fixture, host, popup, inputs, toggle, save, type, editWarehouseItem } = await setup();
    const answer = new Subject<void>();
    editWarehouseItem.mockReturnValue(answer);
    await type(inputs()[0], '100');
    await type(inputs()[1], '8');
    toggle().click();
    await fixture.whenStable();

    save().click();
    await fixture.whenStable();
    expect(editWarehouseItem).toHaveBeenCalledWith('p1', {
      ...LETTINO,
      totalQuantity: 100,
      thresholdQuantity: 8,
      isThresholdWarningActive: false,
    });
    expect(popup()!.querySelector('mat-progress-spinner')).not.toBeNull();
    save().click();
    expect(editWarehouseItem).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.saved).toBe(1);
  });

  it('should show the error of the API and keep the form when the article is not saved', async () => {
    const { fixture, host, popup, inputs, save, type, editWarehouseItem } = await setup();
    editWarehouseItem.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { status: 400, code: 'validation.invalid_request', title: 'Invalid request' },
          }),
      ),
    );
    await type(inputs()[0], '90');

    save().click();
    await fixture.whenStable();
    expect(host.open()).toBe(true);
    expect(host.saved).toBe(0);
    // The message of the code (here the fallback key: the test has no translations).
    expect(
      popup()!.querySelector('.form__warehouse__edit__item__popup__error')?.textContent?.trim(),
    ).toBe('error.unknown');
    expect(inputs()[0].value).toBe('90');
  });

  it('should show the total in red with its message when it is emptied or 0', async () => {
    const { popup, inputs, save, type } = await setup();
    const message = () => popup()!.querySelector('.filled__number__field__error');

    expect(message()).toBeNull();
    await type(inputs()[0], '');
    // The generic key (the test has no translations).
    expect(message()?.textContent?.trim()).toBe('invalidate.greater_than');
    expect(save().disabled).toBe(true);

    await type(inputs()[0], '0');
    expect(message()).not.toBeNull();
    await type(inputs()[0], '4');
    expect(message()).toBeNull();
  });

  it('should show an error of the API on a field under that field, until the field changes', async () => {
    const { fixture, popup, inputs, save, type, editWarehouseItem } = await setup();
    editWarehouseItem.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.invalid_request',
              title: 'Invalid request',
              errors: [{ field: 'thresholdQuantity', code: 'validation.invalid_value' }],
            },
          }),
      ),
    );
    const thresholdError = () =>
      popup()!
        .querySelectorAll('app-filled-number-field')[1]
        .querySelector('.filled__number__field__error');
    await type(inputs()[1], '9');

    save().click();
    await new Promise((resolve) => setTimeout(resolve));
    await fixture.whenStable();
    expect(thresholdError()?.textContent?.trim()).toBe('error.unknown');
    expect(popup()!.querySelector('.form__warehouse__edit__item__popup__error')).toBeNull();

    await type(inputs()[1], '7');
    expect(thresholdError()).toBeNull();
  });
});
