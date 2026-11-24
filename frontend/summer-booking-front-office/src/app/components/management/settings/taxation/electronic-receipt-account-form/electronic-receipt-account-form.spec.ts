import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { UnsavedChangesBehaviour } from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ElectronicReceiptAccount } from '../../../../../entities/settings/taxation/electronic-receipt-account';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { ElectronicReceiptAccountForm } from './electronic-receipt-account-form';

function answer(status: number, code: ApiErrorCode, field?: string): HttpErrorResponse {
  const errors = field ? { errors: [{ field, code: ApiErrorCode.ValidationInvalid }] } : {};
  return new HttpErrorResponse({ status, error: { status, code, ...errors } });
}

describe('ElectronicReceiptAccountForm', () => {
  const filled: ElectronicReceiptAccount = {
    companyName: 'Bagno Esempio',
    companyAddress: 'Via del Mare',
    streetNumber: '1',
    postalCode: '00100',
    city: 'Roma',
    country: 'Italia',
    taxCode: 'XXXXXXXXXXXXXXXX',
    revenueAgencyCode: 'AE00000000',
    vatNumber: '00000000000',
    pin: '00000',
    password: 'segreta-1',
  };

  const setup = async () => {
    const saveElectronicReceiptAccount = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: TaxationService, useValue: { saveElectronicReceiptAccount } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(ElectronicReceiptAccountForm);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const inputs = () => [...element.querySelectorAll<HTMLInputElement>('input')];
    const save = () =>
      element.querySelector<HTMLButtonElement>('.electronic__receipt__account__form__save button')!;
    const type = async (input: HTMLInputElement, text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const fillAll = async () => {
      for (const [index, text] of Object.values(filled).entries()) {
        await type(inputs()[index], text);
      }
    };
    return { fixture, element, inputs, save, type, settle, fillAll, saveElectronicReceiptAccount };
  };

  it('should keep Salva off until every field is filled, then save the form with a spinner', async () => {
    const { element, inputs, save, type, settle, fillAll, saveElectronicReceiptAccount } =
      await setup();
    const answer = new Subject<void>();
    saveElectronicReceiptAccount.mockReturnValue(answer);
    await fillAll();
    await type(inputs()[7], ' ');
    expect(save().disabled).toBe(true);
    await type(inputs()[7], filled.revenueAgencyCode!);

    save().click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledWith(filled);
    expect(save().querySelector('mat-progress-spinner')).not.toBeNull();
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    save().click();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await settle();
    expect(
      element.querySelector('.electronic__receipt__account__form__edit button'),
    ).not.toBeNull();
    expect(element.querySelector('.electronic__receipt__account__form__error')).toBeNull();
  });

  it('should show an error of the API under its field, not above Salva', async () => {
    const { element, save, settle, fillAll, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: ApiErrorCode.ValidationFailed,
              title: 'Invalid request',
              errors: [{ field: 'vatNumber', code: ApiErrorCode.ValidationInvalid }],
            },
          }),
      ),
    );

    await fillAll();
    save().click();
    await settle();
    const vatNumber = element.querySelectorAll('app-filled-text-field')[8];
    // The message of the code (here the fallback key: the test has no translations).
    expect(vatNumber.querySelector('.filled__text__field__error')?.textContent?.trim()).toBe(
      'error.unknown',
    );
    expect(element.querySelector('.electronic__receipt__account__form__error')).toBeNull();
  });

  it('should show an error that is not about a field above Salva', async () => {
    const { element, save, settle, fillAll, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 0 })),
    );
    await fillAll();
    save().click();
    await settle();
    expect(
      element.querySelector('.electronic__receipt__account__form__error')?.textContent?.trim(),
    ).toBeTruthy();
  });

  it('should empty PIN and password once saved and lock the account behind Modifica, which opens all but the password, and Annulla', async () => {
    const { fixture, element, inputs, type, settle, fillAll, saveElectronicReceiptAccount } =
      await setup();
    saveElectronicReceiptAccount.mockReturnValue(of(undefined));
    const button = (name: string) =>
      element.querySelector<HTMLButtonElement>(
        `.electronic__receipt__account__form__${name} button`,
      );
    const editable = () => inputs().flatMap((input, index) => (input.readOnly ? [] : [index]));
    await fillAll();

    button('save')!.click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledWith(
      expect.objectContaining({ password: 'segreta-1' }),
    );
    expect(inputs()[10].value).toBe('');
    expect(inputs()[9].value).toBe('');
    expect(editable()).toEqual([]);
    expect(button('save')).toBeNull();
    expect(button('cancel')).toBeNull();

    button('edit')!.click();
    await fixture.whenStable();
    expect(editable()).toEqual([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
    expect(button('edit')).toBeNull();
    expect(button('cancel')).not.toBeNull();
    expect(button('save')!.disabled).toBe(false);

    await type(inputs()[0], 'Bagno Nuovo');
    button('cancel')!.click();
    await fixture.whenStable();
    const unsaved = TestBed.inject(UnsavedChangesBehaviour);
    expect(unsaved.asking()).toBe(true);
    unsaved.answer(true); // Esci senza salvare
    await settle();
    expect(inputs()[0].value).toBe('Bagno Esempio');
    expect(inputs()[10].value).toBe('');
    expect(editable()).toEqual([]);
    expect(button('edit')).not.toBeNull();
  });

  it('should count as a change only a value different from the saved one, spaces at the sides aside', async () => {
    const { fixture, inputs, type } = await setup();

    await type(inputs()[0], 'Bagno Esempio');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await type(inputs()[0], '   ');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should send the texts without the spaces at the sides, and a PIN of spaces as no PIN', async () => {
    const { element, fixture, inputs, type, settle, fillAll, save, saveElectronicReceiptAccount } =
      await setup();
    saveElectronicReceiptAccount.mockReturnValue(of(undefined));
    await fillAll();
    await type(inputs()[0], '  Bagno Esempio ');
    save().click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenLastCalledWith(filled);

    element
      .querySelector<HTMLButtonElement>('.electronic__receipt__account__form__edit button')!
      .click();
    await fixture.whenStable();
    await type(inputs()[9], '   ');
    save().click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenLastCalledWith({
      ...filled,
      pin: null,
      password: null,
    });
  });

  it('should take the errors of the API away with Annulla', async () => {
    const { element, fixture, settle, fillAll, save, saveElectronicReceiptAccount } = await setup();
    const button = (name: string) =>
      element.querySelector<HTMLButtonElement>(
        `.electronic__receipt__account__form__${name} button`,
      )!;
    saveElectronicReceiptAccount.mockReturnValue(of(undefined));
    await fillAll();
    save().click();
    await settle();

    button('edit').click();
    await fixture.whenStable();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(() => answer(400, ApiErrorCode.ValidationFailed, 'vatNumber')),
    );
    save().click();
    await settle();
    expect(save().disabled).toBe(true);

    button('cancel').click();
    await settle();
    button('edit').click();
    await fixture.whenStable();
    expect(save().disabled).toBe(false);
  });

  it('should show above Salva an error on the password, which cannot be changed once saved', async () => {
    const { element, fixture, settle, fillAll, save, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(of(undefined));
    await fillAll();
    save().click();
    await settle();

    element
      .querySelector<HTMLButtonElement>('.electronic__receipt__account__form__edit button')!
      .click();
    await fixture.whenStable();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(() => answer(400, ApiErrorCode.ValidationFailed, 'password')),
    );
    save().click();
    await settle();
    expect(element.querySelector('.electronic__receipt__account__form__error')).not.toBeNull();
    expect(save().disabled).toBe(false);
  });

  it('should tell the page when the receipt is off or the terms are to be accepted again', async () => {
    const { element, fixture, settle, fillAll, save, saveElectronicReceiptAccount } = await setup();
    const refused: string[] = [];
    fixture.componentInstance.refused.subscribe(({ code }) => refused.push(code));
    await fillAll();

    for (const [status, code] of [
      [403, ApiErrorCode.FiskalyNotEnabled],
      [409, ApiErrorCode.FiskalyTermsNotAccepted],
    ] as const) {
      saveElectronicReceiptAccount.mockReturnValue(throwError(() => answer(status, code)));
      save().click();
      await settle();
      expect(element.querySelector('.electronic__receipt__account__form__error')).toBeNull();
    }
    expect(refused).toEqual([ApiErrorCode.FiskalyNotEnabled, ApiErrorCode.FiskalyTermsNotAccepted]);
  });

  it('should stop at the lengths the backend takes', async () => {
    const { inputs } = await setup();

    expect(inputs().map((input) => input.maxLength)).toEqual([
      128, 128, 16, 5, 128, -1, 16, -1, 13, -1, -1,
    ]);
  });
});
