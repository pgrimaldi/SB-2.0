import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { SystemService } from '../../../services/api/system/system.service';
import { EmailConfiguration } from './email-configuration';

describe('EmailConfiguration', () => {
  const FIELDS = {
    senderMailAddress: 'prenotazioni@lido-demo.example',
    senderName: 'Lido Demo',
    smtpServerAddress: 'smtp.lido-demo.example',
    smtpPort: 587,
    smtpUsername: 'prenotazioni@lido-demo.example',
    smtpSecurity: 'Tls' as const,
  };
  /** The server never sends the SMTP password: only that one is saved. */
  const CONFIGURATION = { ...FIELDS, hasSmtpPassword: true };
  /** Without a new password typed, the saved one is kept (null). */
  const SENT = { idProperty: 'p1', ...FIELDS, smtpPassword: null };

  const setup = async (
    emailConfiguration = vi.fn(() => of(CONFIGURATION)),
    sendTestEmail = vi.fn(),
    saveEmailConfiguration = vi.fn(),
  ) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: SystemService,
          useValue: { emailConfiguration, sendTestEmail, saveEmailConfiguration },
        },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          email_configuration: { security: { none: 'Nessuna', ssl: 'SSL', tls: 'TLS' } },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(EmailConfiguration);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const inputs = () => [...element.querySelectorAll('input')];
    const buttons = () => [
      ...element.querySelectorAll<HTMLButtonElement>('.email__configuration__actions button'),
    ];
    // Only what a user can do: a read-only field cannot be typed in.
    const type = async (index: number, text: string) => {
      if (inputs()[index].readOnly) {
        throw new Error(`Field ${index} is read-only: press Sì first.`);
      }
      inputs()[index].value = text;
      inputs()[index].dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    // submit() of Signal Forms awaits the answer in a promise that Angular does not wait for.
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const allowChanges = async () => {
      element.querySelector<HTMLButtonElement>('.email__configuration__override button')!.click();
      await fixture.whenStable();
    };
    return {
      fixture,
      element,
      inputs,
      buttons,
      type,
      settle,
      allowChanges,
      emailConfiguration,
      sendTestEmail,
      saveEmailConfiguration,
    };
  };

  it('should show six masked fields and the security select', async () => {
    const { element } = await setup();
    const inputs = [...element.querySelectorAll('input')];

    expect(inputs.length).toBe(6);
    for (const input of inputs) {
      expect(input.type).toBe('password');
      expect(input.autocomplete).toBe('new-password');
    }
    expect(element.querySelector('mat-select')).not.toBeNull();
  });

  it('should show the three buttons, the first two with their icon', async () => {
    const { element } = await setup();
    const buttons = [...element.querySelectorAll('.email__configuration__actions app-button')];

    expect(buttons.map((button) => button.textContent?.trim())).toEqual([
      'management.settings.email_configuration.send_test',
      'management.settings.email_configuration.reset',
      'management.settings.email_configuration.save',
    ]);
    expect(
      buttons.map((button) => button.querySelector('img')?.getAttribute('src') ?? null),
    ).toEqual(['/assets/images/mail-send-white.svg', '/assets/images/trash-white.svg', null]);
  });

  it('should fill the fields with the configuration of the property, all hidden but the security', async () => {
    const { element, emailConfiguration } = await setup();

    expect(emailConfiguration).toHaveBeenCalledWith({ idProperty: 'p1' });
    expect([...element.querySelectorAll('input')].map((input) => input.value)).toEqual([
      'prenotazioni@lido-demo.example',
      'Lido Demo',
      'smtp.lido-demo.example',
      '587',
      'prenotazioni@lido-demo.example',
      '',
    ]);
    expect(element.querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('TLS');
  });

  it('should show the error when the configuration does not arrive', async () => {
    const { element } = await setup(
      vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown' }))),
    );

    expect(element.querySelector('.email__configuration__error')?.textContent?.trim()).not.toBe('');
    // Without the values of the server nothing can be changed: Reset reads them again.
    expect(
      element.querySelector<HTMLButtonElement>('.email__configuration__override button')!.disabled,
    ).toBe(true);
  });

  it('should send the values in the fields as the test email, then say it was sent', async () => {
    const { fixture, element, buttons, type, settle, allowChanges, sendTestEmail } = await setup();
    const answer = new Subject<void>();
    sendTestEmail.mockReturnValue(answer);
    await allowChanges();
    await type(1, 'Lido Prova');

    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).toHaveBeenCalledWith({ ...SENT, senderName: 'Lido Prova' });
    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();

    answer.next();
    answer.complete();
    await settle();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.test_sent.title',
    );
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should show under the port what is wrong, keep Salva off and send nothing until it is right', async () => {
    const { fixture, element, buttons, type, allowChanges, sendTestEmail } = await setup();
    const portError = () =>
      element.querySelector('app-filled-number-field .filled__number__field__error');
    await allowChanges();

    await type(3, '');
    expect(portError()?.textContent?.trim()).toBe('invalidate.required');
    expect(buttons()[2].disabled).toBe(true);

    await type(3, '70000');
    expect(portError()?.textContent?.trim()).toBe('invalidate.max');
    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).not.toHaveBeenCalled();

    await type(3, '0');
    expect(portError()?.textContent?.trim()).toBe('invalidate.min');

    await type(3, '587'); // the saved port: the password need not be typed again
    expect(portError()).toBeNull();
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should show an error of the API on a field under that field, until the field changes', async () => {
    const { element, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    await allowChanges();
    saveEmailConfiguration.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.invalid_request',
              title: 'Invalid request',
              errors: [{ field: 'senderName', code: 'validation.invalid_value' }],
            },
          }),
      ),
    );
    const nameError = () =>
      element
        .querySelectorAll('app-filled-text-field')[1]
        .querySelector('.filled__text__field__error');

    buttons()[2].click();
    await settle();
    expect(nameError()?.textContent?.trim()).toBe('error.unknown');
    expect(element.querySelector('.email__configuration__error')).toBeNull();
    expect(buttons()[2].disabled).toBe(true);

    await type(1, 'Lido Demo 2');
    expect(nameError()).toBeNull();
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should save the values in the fields, blocking the other buttons meanwhile, then say it', async () => {
    const { fixture, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    const answer = new Subject<void>();
    saveEmailConfiguration.mockReturnValue(answer);
    await allowChanges();
    await type(1, 'Lido Nuovo');

    buttons()[2].click();
    await fixture.whenStable();
    expect(saveEmailConfiguration).toHaveBeenCalledWith({
      ...SENT,
      senderName: 'Lido Nuovo',
    });
    expect(buttons()[0].disabled).toBe(true);
    expect(buttons()[1].disabled).toBe(true);

    answer.next();
    answer.complete();
    await settle();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.saved.title',
    );
    expect(buttons()[0].disabled).toBe(false);
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should never show the saved SMTP password, and send a new one only when it is typed', async () => {
    const { inputs, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    saveEmailConfiguration.mockReturnValue(of(undefined));
    const password = () => inputs()[5];
    expect(password().value).toBe('');
    expect(password().placeholder).toBe(
      'management.settings.email_configuration.fields.smtp_password.saved',
    );

    await allowChanges();
    await type(5, 'nuova-password');
    buttons()[2].click();
    await settle();

    expect(saveEmailConfiguration).toHaveBeenCalledWith({
      ...SENT,
      smtpPassword: 'nuova-password',
    });
    expect(password().value).toBe(''); // saved: the field never holds it again
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should lock the fields until the server answers Salva', async () => {
    const { fixture, inputs, buttons, settle, allowChanges, saveEmailConfiguration } =
      await setup();
    const answer = new Subject<void>();
    saveEmailConfiguration.mockReturnValue(answer);
    await allowChanges();

    buttons()[2].click();
    await fixture.whenStable();
    expect(inputs().every((input) => input.readOnly)).toBe(true);

    answer.next();
    answer.complete();
    await settle();
    expect(inputs().some((input) => input.readOnly)).toBe(false);
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should ask the password again before the saved one could go to another server', async () => {
    const { fixture, element, inputs, buttons, type, allowChanges, saveEmailConfiguration } =
      await setup();
    const passwordError = () =>
      element
        .querySelectorAll('app-filled-text-field')[4]
        .querySelector('.filled__text__field__error');
    await allowChanges();

    await type(2, 'smtp.altro-server.example');
    expect(passwordError()?.textContent?.trim()).toBe(
      'invalidate.email_configuration.smtp_password.retype',
    );
    expect(buttons()[2].disabled).toBe(true);
    buttons()[0].click(); // the test email sends nothing either
    await fixture.whenStable();

    await type(5, 'password-attuale');
    expect(passwordError()).toBeNull();
    expect(buttons()[2].disabled).toBe(false);
    expect(saveEmailConfiguration).not.toHaveBeenCalled();
    expect(inputs()[5].value).toBe('password-attuale');
  });

  it('should let the settings be changed only once they have arrived from the server', async () => {
    const answer = new Subject<typeof CONFIGURATION>();
    const { fixture, element, inputs } = await setup(vi.fn(() => answer));
    const yes = element.querySelector<HTMLButtonElement>('.email__configuration__override button')!;

    expect(yes.disabled).toBe(true);
    expect(inputs().every((input) => input.readOnly)).toBe(true);

    answer.next(CONFIGURATION);
    answer.complete();
    await fixture.whenStable();
    expect(yes.disabled).toBe(false);
  });

  it('should keep every field read-only until Sì of the question is pressed', async () => {
    const { fixture, element, inputs } = await setup();
    const yes = element.querySelector<HTMLButtonElement>('.email__configuration__override button')!;
    const select = () => element.querySelector('mat-select')!;

    expect(element.querySelector('.email__configuration__question')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.override.question',
    );
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(select().getAttribute('aria-disabled')).toBe('true');

    yes.click();
    await fixture.whenStable();
    expect(inputs().some((input) => input.readOnly)).toBe(false);
    expect(select().getAttribute('aria-disabled')).toBe('false');
    expect(yes.disabled).toBe(true);
  });

  it('should make every field read-only again and Sì usable with Reset', async () => {
    const { fixture, element, inputs, buttons, type } = await setup();
    const yes = element.querySelector<HTMLButtonElement>('.email__configuration__override button')!;
    yes.click();
    await fixture.whenStable();
    await type(1, 'Nome modificato');

    buttons()[1].click();
    await fixture.whenStable();

    expect(inputs()[1].value).toBe('Lido Demo');
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(element.querySelector('mat-select')?.getAttribute('aria-disabled')).toBe('true');
    expect(yes.disabled).toBe(false);
  });
});
