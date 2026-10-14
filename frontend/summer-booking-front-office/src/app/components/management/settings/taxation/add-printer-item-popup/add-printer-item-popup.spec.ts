import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { AddPrinterItemRequest } from '../../../../../entities/settings/taxation/add-printer-item-request';
import { TaxationService } from '../../../../../services/api/taxation/taxation.service';
import { AddPrinterItemPopup } from './add-printer-item-popup';

@Component({
  imports: [AddPrinterItemPopup],
  template: `<app-add-printer-item-popup [(open)]="open" (added)="added = added + 1" />`,
})
class AddPrinterItemPopupHost {
  readonly open = signal(false);
  added = 0;
}

describe('AddPrinterItemPopup', () => {
  const setup = async () => {
    const addPrinter = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: TaxationService, useValue: { addPrinter } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(AddPrinterItemPopupHost);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.add__printer__item__popup');
    const save = () =>
      popup()!.querySelector<HTMLButtonElement>('.add__printer__item__popup__save button')!;
    const typeName = async (name: string) => {
      const input = popup()!.querySelector<HTMLInputElement>(
        '.add__printer__item__popup__name input',
      )!;
      input.value = name;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, popup, save, typeName, settle, addPrinter };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should send the form with the property, with a spinner, then close and tell the page', async () => {
    const { host, popup, save, typeName, settle, addPrinter } = await setup();
    const answer = new Subject<void>();
    addPrinter.mockReturnValue(answer);
    await typeName('EPSON - 3');

    save().click();
    await settle();
    expect(addPrinter).toHaveBeenCalledWith('p1', {
      ...new AddPrinterItemRequest(),
      printerName: 'EPSON - 3',
    });
    expect(
      popup()!.querySelector('.add__printer__item__popup__save mat-progress-spinner'),
    ).not.toBeNull();
    expect(popup()!.querySelector('.add__printer__item__popup__form')!.hasAttribute('inert')).toBe(
      true,
    );
    save().click();
    expect(addPrinter).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await settle();
    expect(host.open()).toBe(false);
    expect(host.added).toBe(1);
  });

  it('should keep the form open with the general error when the printer is not added', async () => {
    const { host, popup, save, typeName, settle, addPrinter } = await setup();
    addPrinter.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    await typeName('EPSON - 3');

    save().click();
    await settle();
    expect(host.open()).toBe(true);
    expect(host.added).toBe(0);
    expect(
      popup()!.querySelector('.add__printer__item__popup__general')?.textContent?.trim(),
    ).toBeTruthy();
    expect(
      popup()!.querySelector<HTMLInputElement>('.add__printer__item__popup__name input')!.value,
    ).toBe('EPSON - 3');
  });

  it('should show the errors of the API on fields under those fields', async () => {
    const { popup, save, settle, addPrinter } = await setup();
    addPrinter.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.invalid_request',
              title: 'Invalid request',
              errors: [
                { field: 'printerName', code: 'validation.invalid_value' },
                { field: 'isSsl', code: 'validation.invalid_value' },
              ],
            },
          }),
      ),
    );

    save().click();
    await settle();
    // The message of the code (here the fallback key: the test has no translations).
    expect(
      popup()!
        .querySelector('.add__printer__item__popup__name .filled__text__field__error')
        ?.textContent?.trim(),
    ).toBe('error.unknown');
    expect(
      popup()!
        .querySelector('.add__printer__item__popup__ssl .checkbox__error')
        ?.textContent?.trim(),
    ).toBe('error.unknown');
    expect(popup()!.querySelector('.add__printer__item__popup__general')).toBeNull();
  });
});
