import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { SharingGeneralInformation } from '../../../entities/settings/sharing/sharing-general-information';
import { SharingService } from '../../../services/api/sharing/sharing.service';
import { Sharing } from './sharing';

describe('Sharing', () => {
  const setup = async (information = new SharingGeneralInformation()) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: SharingService,
          useValue: { generalInformation: () => of(information), saveInformation: vi.fn() },
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
    return { fixture, element, click };
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
});
