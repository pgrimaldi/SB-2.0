import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { WarehouseSelectedItem } from '../../../../../entities/warehouse/warehouse-selected-item';
import { WarehouseSettingItem } from '../../../../../entities/warehouse/warehouse-setting-item';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { FormWarehouseEditItemPopup } from './form-warehouse-edit-item-popup';

const LETTINO_ROW: WarehouseSettingItem = {
  idArticle: 'a1',
  name: 'Lettino',
  totalQuantity: 120,
  lowStockThreshold: 5,
};

const LETTINO: WarehouseSelectedItem = {
  id: 'a1',
  articleCode: 'sunbed',
  name: 'Lettino',
  totalQuantity: 120,
  lowStockThreshold: 5,
  lowStockAlertEnabled: true,
  mapPlaceable: true,
  version: 'v1',
};

@Component({
  imports: [FormWarehouseEditItemPopup],
  template: `<app-form-warehouse-edit-item-popup
    [item]="item()"
    [(open)]="open"
    (saved)="saved = saved + 1"
    (failed)="failures.push($event)"
  />`,
})
class FormWarehouseEditItemPopupHost {
  readonly item = signal<WarehouseSettingItem | null>(LETTINO_ROW);
  readonly open = signal(false);
  saved = 0;
  readonly failures: ApiProblem[] = [];
}

describe('FormWarehouseEditItemPopup', () => {
  const setup = async (
    selectedArticle = vi.fn((): Observable<WarehouseSelectedItem> => of(LETTINO)),
  ) => {
    const updateArticle = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: WarehouseService, useValue: { selectedArticle, updateArticle } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
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
      selectedArticle,
      updateArticle,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should read the article at every opening and keep Salva off until it arrives', async () => {
    const answer = new Subject<WarehouseSelectedItem>();
    const { fixture, host, inputs, save, selectedArticle } = await setup(vi.fn(() => answer));

    expect(selectedArticle).toHaveBeenCalledWith('a1');
    expect(save().disabled).toBe(true);
    expect(inputs().every((input) => input.disabled || input.readOnly)).toBe(true);

    answer.next(LETTINO);
    await fixture.whenStable();
    expect(save().disabled).toBe(false);

    host.open.set(false);
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();
    expect(selectedArticle).toHaveBeenCalledTimes(2);
  });

  it('should close, tell the page and offer to read again when the article cannot be read', async () => {
    const selectedArticle = vi
      .fn((): Observable<WarehouseSelectedItem> => of(LETTINO))
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 0 })));
    const { fixture, host, popup } = await setup(selectedArticle);
    const errorButtons = () => [
      ...document.querySelectorAll<HTMLButtonElement>('.table__error__popup app-button button'),
    ];

    expect(host.open()).toBe(false);
    expect(popup()).toBeNull();
    expect(host.failures).toHaveLength(1);
    expect(errorButtons()).toHaveLength(2);

    errorButtons()[1].click();
    await fixture.whenStable();
    expect(selectedArticle).toHaveBeenCalledTimes(2);
    expect(host.open()).toBe(true);
    expect(popup()).not.toBeNull();
  });

  it('should not offer to read again an article that is gone', async () => {
    const { host } = await setup(
      vi.fn(() =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 404,
              error: { status: 404, code: ApiErrorCode.InventoryItemNotFound },
            }),
        ),
      ),
    );

    expect(host.open()).toBe(false);
    expect(host.failures.map((problem) => problem.code)).toEqual([
      ApiErrorCode.InventoryItemNotFound,
    ]);
    expect(document.querySelectorAll('.table__error__popup app-button button')).toHaveLength(1);
  });

  it('should detect changes against the values read and forget them when reopened', async () => {
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

  it('should fill the form with the article read, with the article locked', async () => {
    const { popup, inputs, toggle } = await setup();
    const select = popup()!.querySelector('mat-select')!;

    expect(select.textContent?.trim()).toBe('Lettino');
    expect(select.getAttribute('aria-disabled')).toBe('true');
    expect(inputs().map((input) => input.value)).toEqual(['120', '5']);
    expect(toggle().getAttribute('aria-checked')).toBe('true');
  });

  it('should keep the alert off without a threshold above 0, even if the article has it on', async () => {
    const { toggle } = await setup(vi.fn(() => of({ ...LETTINO, lowStockThreshold: null })));

    expect(toggle().disabled).toBe(true);
    expect(toggle().getAttribute('aria-checked')).toBe('false');
  });

  it('should let Salva work with a total from the threshold up to the backend limit, also 0 without a threshold', async () => {
    const { inputs, save, type } = await setup();

    expect(save().disabled).toBe(false);
    await type(inputs()[0], '3');
    expect(save().disabled).toBe(true);
    await type(inputs()[0], '5');
    expect(save().disabled).toBe(false);
    await type(inputs()[0], '');
    expect(save().disabled).toBe(true);
    await type(inputs()[1], '');
    await type(inputs()[0], '0');
    expect(save().disabled).toBe(false);
    await type(inputs()[0], '2147483648');
    expect(save().disabled).toBe(true);
    await type(inputs()[0], '2147483647');
    expect(save().disabled).toBe(false);
  });

  it('should send back the whole article read, with the changes, then close and tell the page', async () => {
    const { fixture, host, popup, inputs, toggle, save, type, updateArticle } = await setup();
    const answer = new Subject<string | null>();
    updateArticle.mockReturnValue(answer);
    await type(inputs()[0], '100');
    await type(inputs()[1], '8');
    toggle().click();
    await fixture.whenStable();

    save().click();
    await fixture.whenStable();
    expect(updateArticle).toHaveBeenCalledWith(
      'a1',
      {
        articleCode: 'sunbed',
        name: 'Lettino',
        totalQuantity: 100,
        lowStockThreshold: 8,
        lowStockAlertEnabled: false,
      },
      'v1',
    );
    expect(popup()!.querySelector('mat-progress-spinner')).not.toBeNull();
    save().click();
    expect(updateArticle).toHaveBeenCalledTimes(1);

    answer.next('v2');
    answer.complete();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.saved).toBe(1);
  });

  it('should close, tell the page and say why when someone changed the article after the read', async () => {
    const { fixture, host, inputs, save, type, selectedArticle, updateArticle } = await setup();
    updateArticle.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    await type(inputs()[0], '90');

    save().click();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.failures.map(({ code }) => code)).toEqual([ApiErrorCode.ResourceVersionConflict]);
    expect(host.saved).toBe(0);
    expect(selectedArticle).toHaveBeenCalledTimes(1);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
  });

  it('should show the error of the API and keep the form when the article is not saved', async () => {
    const { fixture, host, popup, inputs, save, type, updateArticle } = await setup();
    updateArticle.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.InventoryNameTaken },
          }),
      ),
    );
    await type(inputs()[0], '90');

    save().click();
    await fixture.whenStable();
    expect(host.open()).toBe(true);
    expect(host.saved).toBe(0);
    expect(host.failures.map((problem) => problem.code)).toEqual([ApiErrorCode.InventoryNameTaken]);
    expect(
      popup()!.querySelector('.form__warehouse__edit__item__popup__error')?.textContent?.trim(),
    ).toBe('error.unknown');
    expect(inputs()[0].value).toBe('90');
  });

  it('should show an error of the API on a field under that field, until the field changes', async () => {
    const { fixture, popup, inputs, save, type, updateArticle } = await setup();
    updateArticle.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: ApiErrorCode.ValidationFailed,
              errors: [{ field: 'lowStockThreshold', code: ApiErrorCode.ValidationOutOfRange }],
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
