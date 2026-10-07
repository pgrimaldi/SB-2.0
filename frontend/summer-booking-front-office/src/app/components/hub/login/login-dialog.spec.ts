import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { LoginDialog } from './login-dialog';

@Component({
  imports: [LoginDialog],
  template: `<app-login-dialog [(open)]="open" />`,
})
class LoginDialogHost {
  readonly open = signal(false);
}

describe('LoginDialog', () => {
  const setup = async (mobile: boolean) => {
    // jsdom has no matchMedia: one answering the phone breakpoint, removed after each test by
    // src/test-setup.ts. Complete, old listener methods included: Material uses them too.
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({
        matches: mobile,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      } as unknown as MediaQueryList),
    );
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        // Closing waits for the exit animation, which jsdom never plays.
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const open = vi.spyOn(TestBed.inject(MatDialog), 'open');
    const fixture = TestBed.createComponent(LoginDialogHost);
    await fixture.whenStable();
    return { fixture, open };
  };

  const signInError = async (answer: () => Observable<never>) => {
    TestBed.overrideProvider(AuthBehaviour, { useValue: { signIn: answer } });
    const { fixture } = await setup(false);
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      error: {
        unknown: 'Si è verificato un errore. Riprova.',
        network: { unavailable: 'Connessione assente. Controlla la rete e riprova.' },
        auth: { invalid_credentials: 'Credenziali non valide' },
      },
    });
    translate.use('it');
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    document.querySelector<HTMLFormElement>('mat-dialog-container form')!.requestSubmit();
    await fixture.whenStable();
    return document.querySelector('.login__dialog__error')?.textContent?.trim();
  };

  it('should show the message of the error code the API answers', async () => {
    const wrongCredentials = () =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 401,
            error: { status: 401, code: 'auth.invalid_credentials', title: 'Invalid credentials' },
          }),
      );

    expect(await signInError(wrongCredentials)).toBe('Credenziali non valide');
  });

  it('should show its own message when there is no connection, and a generic one for unknown codes', async () => {
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const offline = () => throwError(() => new HttpErrorResponse({ status: 0 }));

    expect(await signInError(offline)).toBe('Connessione assente. Controlla la rete e riprova.');
    TestBed.resetTestingModule();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());

    const brandNew = () =>
      throwError(
        () => new HttpErrorResponse({ status: 423, error: { code: 'auth.account_locked' } }),
      );
    expect(await signInError(brandNew)).toBe('Si è verificato un errore. Riprova.');
  });

  it('should keep the spinner when closed and opened again during a sign-in, without the error of that attempt', async () => {
    const answer = new Subject<void>();
    TestBed.overrideProvider(AuthBehaviour, { useValue: { signIn: () => answer } });
    const { fixture } = await setup(false);
    const host = fixture.componentInstance;
    const submit = () =>
      document.querySelector<HTMLButtonElement>('mat-dialog-container button[type="submit"]')!;
    host.open.set(true);
    await fixture.whenStable();
    document.querySelector<HTMLFormElement>('mat-dialog-container form')!.requestSubmit();
    await fixture.whenStable();

    host.open.set(false); // closed while the server has not answered yet
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();
    expect(submit().disabled).toBe(true); // no second sign-in meanwhile
    expect(submit().querySelector('mat-progress-spinner')).not.toBeNull();
    expect(
      document.querySelector('mat-dialog-container app-text-field')?.hasAttribute('inert'),
    ).toBe(true);

    answer.error(new HttpErrorResponse({ status: 401 }));
    await fixture.whenStable();
    expect(document.querySelector('.login__dialog__error')).toBeNull();
    expect(submit().disabled).toBe(false);
  });

  it('should open only when open becomes true, at 30% width on regular screens', async () => {
    const { fixture, open } = await setup(false);
    expect(open).not.toHaveBeenCalled();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    expect(open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ width: '30vw', height: '', maxWidth: 'none' }),
    );
  });

  it('should open at 90% width on phones', async () => {
    const { fixture, open } = await setup(true);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    expect(open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ width: '90vw', height: '' }),
    );
  });

  it('should stop following the screen size and close the popup when destroyed', async () => {
    const { fixture } = await setup(false);
    const query = window.matchMedia('');
    const add = vi.spyOn(query, 'addEventListener');
    const remove = vi.spyOn(query, 'removeEventListener');
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const [, listener] = add.mock.calls[0];

    fixture.destroy();
    await new Promise((resolve) => setTimeout(resolve));

    expect(remove).toHaveBeenCalledWith('change', listener);
    expect(document.querySelector('mat-dialog-container')).toBeNull();
  });

  it('should set open back to false when the popup is closed', async () => {
    const { fixture } = await setup(false);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    TestBed.inject(MatDialog).closeAll();
    await fixture.whenStable();

    expect(fixture.componentInstance.open()).toBe(false);
  });
});
