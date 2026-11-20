import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { SharingSaveInformationRequest } from '../../../entities/settings/sharing/sharing-save-information-request';
import { SharingService } from '../../../services/api/sharing/sharing.service';
import { Sharing } from './sharing';

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

  it('should show at once the error of the days when the minimum is turned off and on again', async () => {
    const information = new SharingGeneralInformation();
    information.isMinimumBookingDayEnabled = true;
    information.minimumBookingDayQuantity = 0;
    const { element, click } = await setup(information);
    const daysError = () => element.querySelector('.sharing__amount .filled__number__field__error');

    expect(daysError()).toBeNull();
    await click(1);
    await click(1);
    expect(daysError()).not.toBeNull();
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
    expect(saveInformation).toHaveBeenCalledWith(
      { ...new SharingSaveInformationRequest(), isSharingEnabled: true },
      'v1',
    );
    expect(generalInformation).toHaveBeenCalledTimes(2);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.sharing__error')).toBeNull();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });
});
