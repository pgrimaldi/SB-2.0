import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ElectronicReceiptAccount } from '../../../../../entities/settings/taxation/electronic-receipt-account';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { ElectronicReceiptAccountForm } from './electronic-receipt-account-form';

describe('ElectronicReceiptAccountForm', () => {
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
    return { fixture, element, inputs, save, type, settle, saveElectronicReceiptAccount };
  };

  it('should save the form with the property, with a spinner, then say it is saved', async () => {
    const { element, inputs, save, type, settle, saveElectronicReceiptAccount } = await setup();
    const answer = new Subject<void>();
    saveElectronicReceiptAccount.mockReturnValue(answer);
    await type(inputs()[0], 'Bagno Esempio');

    expect(element.querySelector('.electronic__receipt__account__form__saved')).toBeNull();
    save().click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledWith('p1', {
      ...new ElectronicReceiptAccount(),
      companyName: 'Bagno Esempio',
    });
    expect(save().querySelector('mat-progress-spinner')).not.toBeNull();
    expect(
      element.querySelector('.electronic__receipt__account__form__fields')!.hasAttribute('inert'),
    ).toBe(true);
    save().click();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await settle();
    expect(
      element.querySelector('.electronic__receipt__account__form__edit button'),
    ).not.toBeNull();
    expect(element.querySelector('.electronic__receipt__account__form__error')).toBeNull();
    expect(
      element.querySelector('.electronic__receipt__account__form__saved')?.textContent?.trim(),
    ).toBe('management.settings.taxation.receipt.account.saved');
  });

  it('should show an error of the API under its field, not above Salva', async () => {
    const { element, save, settle, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.failed',
              title: 'Invalid request',
              errors: [{ field: 'vatNumber', code: 'validation.invalid' }],
            },
          }),
      ),
    );

    save().click();
    await settle();
    const vatNumber = element.querySelectorAll('app-filled-text-field')[7];
    // The message of the code (here the fallback key: the test has no translations).
    expect(vatNumber.querySelector('.filled__text__field__error')?.textContent?.trim()).toBe(
      'error.unknown',
    );
    expect(element.querySelector('.electronic__receipt__account__form__error')).toBeNull();
  });

  it('should show an error that is not about a field above Salva', async () => {
    const { element, save, settle, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 0 })),
    );
    save().click();
    await settle();
    expect(
      element.querySelector('.electronic__receipt__account__form__error')?.textContent?.trim(),
    ).toBeTruthy();
  });

  it('should empty PIN and password once saved and lock the account behind Modifica, which opens all but the password, and Annulla', async () => {
    const { fixture, element, inputs, type, settle, saveElectronicReceiptAccount } = await setup();
    saveElectronicReceiptAccount.mockReturnValue(of(undefined));
    const button = (name: string) =>
      element.querySelector<HTMLButtonElement>(
        `.electronic__receipt__account__form__${name} button`,
      );
    const editable = () =>
      inputs()
        .filter((input) => !input.readOnly)
        .map((input) =>
          input.closest('app-filled-text-field')!.querySelector('label')!.textContent!.trim(),
        );
    await type(inputs()[0], 'Bagno Esempio');
    await type(inputs()[9], 'segreta-1');

    button('save')!.click();
    await settle();
    expect(saveElectronicReceiptAccount).toHaveBeenCalledWith(
      'p1',
      expect.objectContaining({ password: 'segreta-1' }),
    );
    expect(inputs()[9].value).toBe('');
    expect(inputs()[9].placeholder).toBe(
      'management.settings.taxation.receipt.account.password_saved',
    );
    expect(inputs()[8].placeholder).toBe('');
    expect(editable()).toEqual([]);
    expect(button('save')).toBeNull();
    expect(button('cancel')).toBeNull();

    button('edit')!.click();
    await fixture.whenStable();
    expect(editable().length).toBe(9);
    expect(editable()).not.toContain('management.settings.taxation.receipt.account.password');
    expect(button('edit')).toBeNull();
    expect(button('cancel')).not.toBeNull();
    expect(button('save')).not.toBeNull();

    await type(inputs()[0], 'Bagno Nuovo');
    button('cancel')!.click();
    await fixture.whenStable();
    expect(inputs()[0].value).toBe('Bagno Esempio');
    expect(inputs()[9].value).toBe('');
    expect(editable()).toEqual([]);
    expect(button('edit')).not.toBeNull();
  });
});
