import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { UnsavedChangesBehaviour } from '../../../behaviours/forms/unsaved-changes.behaviour';
import { ManagementMenu } from './management-menu';

describe('ManagementMenu', () => {
  const setup = async (revoked: boolean) => {
    const logout = vi.fn().mockResolvedValue(revoked);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideTranslateService(),
        { provide: AuthBehaviour, useValue: { logout } },
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const fixture = TestBed.createComponent(ManagementMenu);
    await fixture.whenStable();
    const button = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
      'button.management__menu__item',
    )!;
    return { fixture, logout, button };
  };

  it('should tell the user that the logout failed, as the session is still valid', async () => {
    const { fixture, logout, button } = await setup(false);

    button.click();
    await fixture.whenStable();

    expect(logout).toHaveBeenCalledTimes(1);
    const dialog = document.querySelector('[role="alertdialog"]');
    expect(dialog?.querySelector('.alert__popup__title')?.textContent?.trim()).toBe(
      'management.logout_failed.title',
    );
    expect(button.disabled).toBe(false); // it can be tried again
  });

  it('should ask before logging out with unsaved changes, and stay signed in on Resta', async () => {
    const { fixture, logout, button } = await setup(true);
    const unsaved = TestBed.inject(UnsavedChangesBehaviour);
    unsaved.watch({ hasUnsavedChanges: () => true });
    // The answer reaches the menu after the current task.
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };

    button.click();
    await fixture.whenStable();
    expect(unsaved.asking()).toBe(true);
    unsaved.answer(false);
    await settle();
    expect(logout).not.toHaveBeenCalled();

    button.click();
    await fixture.whenStable();
    unsaved.answer(true);
    await settle();
    expect(logout).toHaveBeenCalledTimes(1);
  });

  it('should show nothing when the logout succeeds', async () => {
    const { fixture, button } = await setup(true);

    button.click();
    await fixture.whenStable();

    expect(document.querySelector('[role="alertdialog"]')).toBeNull();
  });
});
