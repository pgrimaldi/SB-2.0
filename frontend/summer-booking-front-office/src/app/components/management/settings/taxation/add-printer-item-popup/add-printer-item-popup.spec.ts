import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FieldTree } from '@angular/forms/signals';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { PrinterMode } from '../../../../../entities/enums/printer-mode';
import { PrinterModel } from '../../../../../entities/enums/printer-model';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import {
  AddPrinterItemRequest,
  MAX_PRINTER_PORT,
} from '../../../../../entities/settings/taxation/add-printer-item-request';
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

const PRINTER: AddPrinterItemRequest = {
  ...new AddPrinterItemRequest(),
  printerName: 'EPSON - 3',
  printerIP: '192.0.2.12',
  printerPort: 9100,
  printerModel: PrinterModel.Fp81,
  printerMode: PrinterMode.Fiscal,
};

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
    const component = fixture.debugElement.query(By.directive(AddPrinterItemPopup))
      .componentInstance as unknown as {
      draftForm: FieldTree<AddPrinterItemRequest>;
      vatRateOptions: () => readonly (readonly { value: string }[])[];
      setRate: (index: number, key: string | null) => void;
    };
    const { draftForm } = component;
    const fill = async (changes: Partial<AddPrinterItemRequest> = {}) => {
      draftForm().value.set({ ...PRINTER, ...changes });
      await fixture.whenStable();
    };
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return {
      fixture,
      host: fixture.componentInstance,
      component,
      draftForm,
      popup,
      save,
      fill,
      settle,
      addPrinter,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should send the form, with a spinner, then close and tell the page', async () => {
    const { host, popup, save, fill, settle, addPrinter } = await setup();
    const answer = new Subject<void>();
    addPrinter.mockReturnValue(answer);
    await fill();

    save().click();
    await settle();
    expect(addPrinter).toHaveBeenCalledWith(PRINTER);
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
    const { host, popup, save, fill, settle, addPrinter } = await setup();
    addPrinter.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    await fill();

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
    const { popup, save, fill, settle, addPrinter } = await setup();
    await fill();
    addPrinter.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: ApiErrorCode.ValidationFailed,
              title: 'Invalid request',
              errors: [
                { field: 'printerName', code: ApiErrorCode.ValidationInvalid },
                { field: 'isSsl', code: ApiErrorCode.ValidationInvalid },
                { field: 'departmentVatRates', code: ApiErrorCode.ValidationInvalid },
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

  it('should save only with the limits of the backend kept', async () => {
    const { save, fill, draftForm } = await setup();
    expect(save().disabled).toBe(true);

    await fill();
    expect(save().disabled).toBe(false);

    for (const wrong of [
      { printerName: '  ' },
      { printerIP: null },
      { printerPort: null },
      { printerPort: 0 },
      { printerPort: MAX_PRINTER_PORT + 1 },
      { printerModel: null },
      { printerMode: null },
    ]) {
      await fill(wrong);
      expect(save().disabled, JSON.stringify(wrong)).toBe(true);
    }
    expect(draftForm.printerName().maxLength?.()).toBe(120);
    expect(draftForm.footer().maxLength?.()).toBe(160);
  });

  it('should keep the departments of a fiscal printer only, each with a rate of its own', async () => {
    const { fill, component, draftForm, popup, fixture } = await setup();
    await fill();
    expect(
      popup()!.querySelectorAll('.add__printer__item__popup__rates app-filled-select'),
    ).toHaveLength(5);

    component.setRate(0, '22');
    component.setRate(1, '10');
    expect(draftForm.departmentVatRates().value()).toEqual([22, 10, null, null, null]);
    expect(component.vatRateOptions()[2].map(({ value }) => value)).toEqual([
      'none',
      '0',
      '4',
      '5',
    ]);
    component.setRate(1, 'none');
    expect(draftForm.departmentVatRates().value()).toEqual([22, null, null, null, null]);

    draftForm.printerMode().value.set(PrinterMode.Text);
    await fixture.whenStable();
    expect(popup()!.querySelector('.add__printer__item__popup__rates')).toBeNull();
    expect(draftForm.departmentVatRates().value()).toEqual([null, null, null, null, null]);
  });
});
