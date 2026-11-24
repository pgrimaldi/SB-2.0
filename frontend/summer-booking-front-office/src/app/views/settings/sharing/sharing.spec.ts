import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import {
  MAX_SHARING_BOOKING_DAYS,
  MAX_SHARING_CREDIT_AMOUNT,
  MAX_SHARING_CREDIT_UNITS,
  SharingSaveInformationRequest,
} from '../../../entities/settings/sharing/sharing-save-information-request';
import { SharingService } from '../../../services/api/sharing/sharing.service';
import { Sharing } from './sharing';

/** As the backend keeps the settings when nothing is on. */
const EMPTY = {
  ...new SharingSaveInformationRequest(),
  manualCreditUnitsPerDay: 0,
  manualCreditAmount: 0,
  voucherCreditAmount: 0,
  cardTopUpAmount: 0,
};

const refusal = (status: number, code: string, errors?: object[]) =>
  throwError(() => new HttpErrorResponse({ status, error: { status, code, errors } }));

describe('Sharing', () => {
  const setup = async (information = new SharingGeneralInformation()) => {
    const generalInformation = vi.fn((): Observable<SharingGeneralInformation> => of(information));
    const user = signal({ propertyId: 'p1', role: 'admin' });
    const saveInformation = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: SharingService,
          useValue: { generalInformation, saveInformation },
        },
        { provide: AuthBehaviour, useValue: { user } },
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
    const refuse = (status: number, code: string, errors?: object[]) =>
      saveInformation.mockReturnValueOnce(refusal(status, code, errors));
    return {
      fixture,
      element,
      click,
      save,
      refuse,
      user,
      generalInformation,
      saveInformation,
    };
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

    form.minBookingDays().value.set(MAX_SHARING_BOOKING_DAYS);
    form.manualCreditUnitsPerDay().value.set(MAX_SHARING_CREDIT_UNITS + 1);
    form.manualCreditAmount().value.set(MAX_SHARING_CREDIT_AMOUNT);
    form.voucherCreditAmount().value.set(0.5);
    form.cardTopUpAmount().value.set(MAX_SHARING_CREDIT_AMOUNT + 1);
    expect(form().valid()).toBe(false);
    form.cardTopUpAmount().value.set(10);
    expect(form().valid()).toBe(false);
    form.manualCreditUnitsPerDay().value.set(MAX_SHARING_CREDIT_UNITS);
    expect(form().valid()).toBe(true);
    form.minBookingDays().value.set(MAX_SHARING_BOOKING_DAYS + 1);
    expect(form().valid()).toBe(false);
  });

  it('should say at once why settings read without the value of their option cannot be saved', async () => {
    const { element } = await setup(
      Object.assign(new SharingGeneralInformation(), {
        ...EMPTY,
        voucherCreditEnabled: true,
      }),
    );

    expect(element.querySelectorAll('.filled__number__field__error')).toHaveLength(1);
  });

  it('should save no days with the minimum off and 0 for an empty number, which are not changes', async () => {
    const { fixture, click, save, saveInformation } = await setup(
      Object.assign(new SharingGeneralInformation(), {
        ...EMPTY,
        minBookingDays: 3,
      }),
    );
    const { componentInstance: sharing } = fixture;
    saveInformation.mockReturnValue(of('v2'));

    sharing['informationForm'].voucherCreditAmount().value.set(null);
    expect(sharing.hasUnsavedChanges()).toBe(false);

    await click(1);
    await save();
    expect(saveInformation).toHaveBeenCalledWith(EMPTY, null);
    expect(sharing['informationForm']().value()).toEqual({
      ...EMPTY,
      isMinBookingDaysEnabled: false,
    });
    expect(sharing.hasUnsavedChanges()).toBe(false);
  });

  it('should put the values read back, read the settings again and only then say why when someone changed them after the read', async () => {
    const information = Object.assign(new SharingGeneralInformation(), { version: 'v1' });
    const { fixture, element, click, save, refuse, generalInformation, saveInformation } =
      await setup(information);
    const { componentInstance: sharing } = fixture;
    const answer = new Subject<SharingGeneralInformation>();
    refuse(409, ApiErrorCode.ResourceVersionConflict);
    generalInformation.mockReturnValueOnce(answer);
    await click(0);

    await save();
    expect(saveInformation).toHaveBeenCalledWith({ ...EMPTY, isEnabled: true }, 'v1');
    expect(generalInformation).toHaveBeenCalledTimes(2);
    expect(sharing['informationForm'].isEnabled().value()).toBe(false);
    expect(sharing.hasUnsavedChanges()).toBe(false);
    expect(sharing['isConflictShown']()).toBe(false);

    answer.next(Object.assign(new SharingGeneralInformation(), { version: 'v2' }));
    await fixture.whenStable();
    expect(sharing['isConflictShown']()).toBe(true);
    expect(element.querySelector('.sharing__error')).toBeNull();

    sharing['isConflictShown'].set(false);
    refuse(409, ApiErrorCode.ResourceVersionConflict);
    generalInformation.mockReturnValueOnce(refusal(500, 'server.unexpected'));
    await click(0);
    await save();
    expect(sharing['isConflictShown']()).toBe(false);
    expect(sharing.hasUnsavedChanges()).toBe(false);
    expect(element.querySelector('.sharing__error')).not.toBeNull();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should save nothing and offer to read again when the settings cannot be read', async () => {
    const { fixture, element, save, generalInformation, saveInformation } = await setup();
    const { componentInstance: sharing } = fixture;
    generalInformation.mockReturnValueOnce(refusal(403, ApiErrorCode.AuthForbidden));
    sharing['reload']();
    await fixture.whenStable();

    expect(element.querySelector('.sharing__error')).not.toBeNull();
    await save();
    expect(saveInformation).not.toHaveBeenCalled();

    element.querySelector<HTMLButtonElement>('.sharing__failure button')!.click();
    await fixture.whenStable();
    expect(generalInformation).toHaveBeenCalledTimes(3);
    expect(element.querySelector('.sharing__error')).toBeNull();
    expect(sharing['loaded']()).toBe(true);
  });

  it('should send the version of the last save and forget a reading started before it', async () => {
    const { fixture, click, save, generalInformation, saveInformation } = await setup(
      Object.assign(new SharingGeneralInformation(), { ...EMPTY, version: 'v1' }),
    );
    const { componentInstance: sharing } = fixture;
    saveInformation.mockReturnValueOnce(of('v2')).mockReturnValueOnce(of('v3'));
    await click(0);
    await save();
    await click(2);
    await save();
    expect(saveInformation.mock.calls.map(([, version]) => version)).toEqual(['v1', 'v2']);

    const reading = new Subject<SharingGeneralInformation>();
    const saving = new Subject<string>();
    saveInformation.mockReturnValueOnce(saving);
    generalInformation.mockReturnValueOnce(reading);
    await click(3);
    await save();
    sharing['reload']();
    await fixture.whenStable();
    saving.next('v4');
    await new Promise((resolve) => setTimeout(resolve));
    reading.next(Object.assign(new SharingGeneralInformation(), { ...EMPTY, version: 'v3' }));
    await fixture.whenStable();
    expect(sharing['version']).toBe('v4');
    expect(sharing['informationForm'].allArticles().value()).toBe(true);
    expect(sharing['loaded']()).toBe(true);
  });

  it('should not read the settings again when only the role of the user changes', async () => {
    const { fixture, click, user, generalInformation } = await setup();

    await click(0);
    user.set({ propertyId: 'p1', role: 'manager' });
    await fixture.whenStable();
    expect(generalInformation).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
  });

  it('should show an error of the backend under its field, or under the panels when the field is off', async () => {
    const { fixture, element, click, save, refuse } = await setup();
    const { componentInstance: sharing } = fixture;
    const form = sharing['informationForm'];
    await click(0);
    refuse(400, ApiErrorCode.ValidationFailed, [
      { field: 'manualCreditAmount', code: ApiErrorCode.ValidationOutOfRange },
    ]);
    await save();
    expect(form.manualCreditAmount().errors()).toHaveLength(1);
    expect(element.querySelector('.sharing__error')).toBeNull();

    form.manualCreditAmount().value.set(1);
    await fixture.whenStable();
    expect(form.manualCreditAmount().errors()).toHaveLength(0);
    refuse(400, ApiErrorCode.ValidationFailed, [
      { field: 'minBookingDays', code: ApiErrorCode.ValidationOutOfRange },
    ]);
    await save();
    expect(sharing['error']()?.code).toBe(ApiErrorCode.ValidationOutOfRange);
  });
});
