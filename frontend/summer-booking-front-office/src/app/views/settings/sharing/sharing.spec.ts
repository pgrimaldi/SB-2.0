import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import {
  MAX_SHARING_CREDIT_VALUE,
  MAX_SHARING_QUANTITY,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { SharingService } from '../../../services/api/sharing/sharing.service';
import { Sharing } from './sharing';

/** As the backend keeps the settings when nothing is on. */
const EMPTY = {
  ...new SharingSaveInformationRequest(),
  resoldDayCreditQuantity: 0,
  manualCreditValue: 0,
  voucherCreditValue: 0,
  prepaidCardCreditValue: 0,
};

describe('Sharing', () => {
  const setup = async (information = new SharingGeneralInformation()) => {
    const generalInformation = vi.fn(() => of(information));
    const saveInformation = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: SharingService,
          useValue: { generalInformation, saveInformation },
        },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(Sharing);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const toggle = (index: number) =>
      element.querySelectorAll<HTMLButtonElement>('app-toggle button')[index];
    const click = async (index: number) => {
      toggle(index).click();
      await fixture.whenStable();
    };
    // submit() of Signal Forms awaits the answer in a promise that Angular does not wait for.
    const save = async () => {
      element.querySelector<HTMLButtonElement>('.sharing__actions button')!.click();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return { fixture, element, click, save, generalInformation, saveInformation };
  };

  it('should not count as a change a toggle turned on and off again', async () => {
    const { fixture, click } = await setup();

    await click(0);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await click(0);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should say at once which value an option turned on still wants, and save nothing without it', async () => {
    const { fixture, element, click, saveInformation } = await setup();
    const form = fixture.componentInstance['informationForm'];
    const errors = () => element.querySelectorAll('.filled__number__field__error').length;
    const check = async (index: number) => {
      element.querySelectorAll<HTMLInputElement>('app-checkbox input')[index].click();
      await fixture.whenStable();
    };

    expect(errors()).toBe(0);
    await click(1);
    expect(errors()).toBe(1);
    await check(0);
    expect(errors()).toBe(3);
    await check(1);
    await check(2);
    expect(errors()).toBe(5);

    fixture.componentInstance['save']();
    await fixture.whenStable();
    expect(saveInformation).not.toHaveBeenCalled();

    form.minimumBookingDayQuantity().value.set(MAX_SHARING_QUANTITY);
    form.resoldDayCreditQuantity().value.set(1);
    form.manualCreditValue().value.set(MAX_SHARING_CREDIT_VALUE);
    form.voucherCreditValue().value.set(0.5);
    form.prepaidCardCreditValue().value.set(MAX_SHARING_CREDIT_VALUE + 1);
    expect(form().valid()).toBe(false);
    form.prepaidCardCreditValue().value.set(10);
    expect(form().valid()).toBe(true);
  });

  it('should say at once why settings read without the value of their option cannot be saved', async () => {
    const { element } = await setup(
      Object.assign(new SharingGeneralInformation(), {
        ...EMPTY,
        isAutomaticVoucherEnabled: true,
      }),
    );

    expect(element.querySelectorAll('.filled__number__field__error')).toHaveLength(1);
  });

  it('should save no days with the minimum off and 0 for an empty number, which are not changes', async () => {
    const { fixture, click, save, saveInformation } = await setup(
      Object.assign(new SharingGeneralInformation(), {
        ...EMPTY,
        isMinimumBookingDayEnabled: true,
        minimumBookingDayQuantity: 3,
      }),
    );
    const { componentInstance: sharing } = fixture;
    saveInformation.mockReturnValue(of('v2'));

    sharing['informationForm'].voucherCreditValue().value.set(null);
    expect(sharing.hasUnsavedChanges()).toBe(false);

    await click(1);
    await save();
    expect(saveInformation).toHaveBeenCalledWith(EMPTY, null);
    expect(sharing['informationForm']().value()).toEqual(EMPTY);
    expect(sharing.hasUnsavedChanges()).toBe(false);
  });

  it('should read the settings again and say why when someone changed them after the read', async () => {
    const information = Object.assign(new SharingGeneralInformation(), { version: 'v1' });
    const { fixture, element, click, save, generalInformation, saveInformation } =
      await setup(information);
    saveInformation.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    await click(0);

    await save();
    expect(saveInformation).toHaveBeenCalledWith({ ...EMPTY, isSharingEnabled: true }, 'v1');
    expect(generalInformation).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.sharing__error')).toBeNull();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });
});
