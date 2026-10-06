import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthBehaviour } from '../auth/auth.behaviour';
import { UnsavedChangesBehaviour } from './unsaved-changes.behaviour';
import { unsavedChangesGuard } from './unsaved-changes.guard';

describe('UnsavedChangesBehaviour', () => {
  const setup = (signedIn = true) => {
    TestBed.configureTestingModule({
      providers: [{ provide: AuthBehaviour, useValue: { isAuthenticated: () => signedIn } }],
    });
    const unsaved = TestBed.inject(UnsavedChangesBehaviour);
    const changed = signal(false);
    unsaved.watch({ hasUnsavedChanges: changed });
    const leavePage = () =>
      TestBed.runInInjectionContext(() =>
        unsavedChangesGuard(
          null,
          {} as ActivatedRouteSnapshot,
          {} as RouterStateSnapshot,
          {} as RouterStateSnapshot,
        ),
      ) as boolean | Promise<boolean>;
    return { unsaved, changed, leavePage };
  };

  it('should let the user leave a page at once without changes, and ask with unsaved ones', async () => {
    const { unsaved, changed, leavePage } = setup();
    expect(await leavePage()).toBe(true);
    expect(unsaved.asking()).toBe(false);

    changed.set(true);
    const stay = leavePage();
    expect(unsaved.asking()).toBe(true);
    unsaved.answer(false);
    expect(await stay).toBe(false);

    const leave = leavePage();
    unsaved.answer(true);
    expect(await leave).toBe(true);
  });

  it('should let the page go without asking once the session has ended', async () => {
    const { unsaved, changed, leavePage } = setup(false);
    changed.set(true);

    expect(await leavePage()).toBe(true);
    expect(unsaved.asking()).toBe(false);
  });

  it('should have the browser ask before closing or reloading the tab with unsaved changes', () => {
    const { changed } = setup();
    const unload = () => {
      const event = new Event('beforeunload', { cancelable: true });
      window.dispatchEvent(event);
      return event.defaultPrevented;
    };

    expect(unload()).toBe(false);
    changed.set(true);
    expect(unload()).toBe(true);
  });
});
