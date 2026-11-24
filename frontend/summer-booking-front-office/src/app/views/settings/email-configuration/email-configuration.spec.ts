import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { UnsavedChangesBehaviour } from '../../../behaviours/forms/unsaved-changes.behaviour';
import { SmtpSecurity } from '../../../entities/enums/smtp-security';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { EmailConfigurationData } from '../../../entities/settings/email-configuration/email-configuration-data';
import { NotificationService } from '../../../services/api/notification/notification.service';
import { EmailConfiguration } from './email-configuration';

describe('EmailConfiguration', () => {
  const FIELDS = {
    fromEmail: 'prenotazioni@lido-demo.example',
    fromName: 'Lido Demo',
    smtpHost: 'smtp.lido-demo.example',
    smtpPort: 587,
    smtpUsername: 'prenotazioni@lido-demo.example',
    security: SmtpSecurity.StartTls,
    verifyCertificate: true,
    testRecipients: ['prova@lido-demo.example', 'info@lido-demo.example'],
    isActive: true,
  };
  /** The server never sends the SMTP password: only that one is saved. */
  const CONFIGURATION: EmailConfigurationData = {
    ...FIELDS,
    hasSmtpPassword: true,
    lastVerificationAt: '2026-11-20T10:00:00Z',
    lastVerificationError: null,
    version: 'v1',
  };
  /** Without a new password typed, the saved one is kept (null). */
  const SENT = { ...FIELDS, smtpPassword: null };

  const setup = async (
    emailConfiguration = vi.fn((): Observable<EmailConfigurationData> => of(CONFIGURATION)),
    sendTestEmail = vi.fn(),
    saveEmailConfiguration = vi.fn(),
  ) => {
    const user = signal({ propertyId: 'p1', role: 'admin' });
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: NotificationService,
          useValue: { emailConfiguration, sendTestEmail, saveEmailConfiguration },
        },
        { provide: AuthBehaviour, useValue: { user } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          email_configuration: {
            security: { none: 'Nessuna', starttls: 'STARTTLS', ssl: 'SSL' },
          },
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
    const yes = () =>
      element.querySelector<HTMLButtonElement>('.email__configuration__override button')!;
    const allowChanges = async () => {
      yes().click();
      await fixture.whenStable();
    };
    const errorText = () =>
      element
        .querySelector('.email__configuration__error')
        ?.textContent?.replace(/\s+/g, ' ')
        .trim();
    return {
      fixture,
      element,
      user,
      inputs,
      buttons,
      yes,
      type,
      settle,
      allowChanges,
      errorText,
      emailConfiguration,
      sendTestEmail,
      saveEmailConfiguration,
    };
  };

  const apiError = (status: number, code: ApiErrorCode, rest: object = {}) =>
    throwError(
      () => new HttpErrorResponse({ status, error: { status, code, title: code, ...rest } }),
    );
  const closePopups = () =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());

  it('should fill the fields with the configuration of the property', async () => {
    const { element, emailConfiguration } = await setup();

    expect(emailConfiguration).toHaveBeenCalledWith();
    expect([...element.querySelectorAll('input')].map((input) => input.value)).toEqual([
      'prenotazioni@lido-demo.example',
      'Lido Demo',
      'smtp.lido-demo.example',
      '587',
      'prenotazioni@lido-demo.example',
      '',
      'prova@lido-demo.example, info@lido-demo.example',
    ]);
  });

  it('should show the error when the configuration does not arrive, send nothing and read it again with Riprova', async () => {
    const { fixture, element, buttons, yes, errorText, emailConfiguration } = await setup(
      vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown' }))),
    );

    expect(errorText()).toBe('error.unknown');
    expect(yes().disabled).toBe(true);
    const [sendTest, , save] = buttons();
    expect(sendTest.disabled).toBe(true);
    expect(save.disabled).toBe(true);

    emailConfiguration.mockReturnValue(of(CONFIGURATION));
    element.querySelector<HTMLButtonElement>('.email__configuration__failure button')!.click();
    await fixture.whenStable();
    expect(errorText()).toBeUndefined();
    expect(yes().disabled).toBe(false);
  });

  it('should send the values in the fields as the test email, then say it was sent', async () => {
    const { fixture, element, buttons, type, settle, allowChanges, sendTestEmail } = await setup();
    const answer = new Subject<void>();
    sendTestEmail.mockReturnValue(answer);
    await allowChanges();
    await type(1, 'Lido Prova');

    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).toHaveBeenCalledWith({ ...SENT, fromName: 'Lido Prova' });
    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();

    answer.next();
    answer.complete();
    await settle();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.test_sent.title',
    );
    closePopups();
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
              code: ApiErrorCode.ValidationFailed,
              title: 'Invalid request',
              errors: [{ field: 'fromName', code: ApiErrorCode.ValidationInvalid }],
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

  it('should save the values in the fields, blocking the other buttons meanwhile, then say it and lock the fields again', async () => {
    const {
      fixture,
      element,
      inputs,
      buttons,
      type,
      settle,
      allowChanges,
      saveEmailConfiguration,
    } = await setup();
    const answer = new Subject<void>();
    saveEmailConfiguration.mockReturnValue(answer);
    await allowChanges();
    await type(1, 'Lido Nuovo');

    buttons()[2].click();
    await fixture.whenStable();
    expect(saveEmailConfiguration).toHaveBeenCalledWith(
      {
        ...SENT,
        fromName: 'Lido Nuovo',
      },
      'v1',
    );
    expect(buttons()[0].disabled).toBe(true);
    expect(buttons()[1].disabled).toBe(true);

    answer.next();
    answer.complete();
    await settle();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.saved.title',
    );
    expect(buttons()[0].disabled).toBe(false);
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(
      element.querySelector<HTMLButtonElement>('.email__configuration__override button')!.disabled,
    ).toBe(false);
    closePopups();
  });

  it('should never show the saved SMTP password, and send a new one only when it is typed', async () => {
    const { inputs, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    saveEmailConfiguration.mockReturnValue(of(undefined));
    const password = () => inputs()[5];
    expect(password().value).toBe('');

    await allowChanges();
    await type(5, 'nuova-password');
    buttons()[2].click();
    await settle();

    expect(saveEmailConfiguration).toHaveBeenCalledWith(
      {
        ...SENT,
        smtpPassword: 'nuova-password',
      },
      'v1',
    );
    expect(password().value).toBe(''); // saved: the field never holds it again
    closePopups();
  });

  it('should lock the fields until the server answers Salva, and give them back when it fails', async () => {
    const { fixture, inputs, buttons, settle, allowChanges, saveEmailConfiguration } =
      await setup();
    const answer = new Subject<void>();
    saveEmailConfiguration.mockReturnValue(answer);
    await allowChanges();

    buttons()[2].click();
    await fixture.whenStable();
    expect(inputs().every((input) => input.readOnly)).toBe(true);

    answer.error(new HttpErrorResponse({ status: 500 }));
    await settle();
    expect(inputs().some((input) => input.readOnly)).toBe(false);
    closePopups();
  });

  it('should read the configuration again and say why when someone changed it after the read', async () => {
    const {
      element,
      inputs,
      buttons,
      type,
      settle,
      allowChanges,
      emailConfiguration,
      saveEmailConfiguration,
    } = await setup();
    saveEmailConfiguration.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    await allowChanges();
    await type(1, 'Lido Nuovo');

    buttons()[2].click();
    await settle();
    expect(emailConfiguration).toHaveBeenCalledTimes(2);
    expect(inputs()[1].value).toBe('Lido Demo');
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.email__configuration__error')).toBeNull();
    closePopups();
  });

  it('should not say that the data were read again after a conflict when the reading fails', async () => {
    const { buttons, type, settle, allowChanges, emailConfiguration, saveEmailConfiguration } =
      await setup();
    saveEmailConfiguration.mockReturnValue(apiError(409, ApiErrorCode.ResourceVersionConflict));
    emailConfiguration.mockReturnValue(apiError(500, ApiErrorCode.ServerUnexpected));
    await allowChanges();
    await type(1, 'Lido Nuovo');

    buttons()[2].click();
    await settle();
    expect(document.querySelector('.table__error__popup')).toBeNull();
    expect(buttons()[2].disabled).toBe(true);
  });

  it('should ask the password again before the saved one could go to another server', async () => {
    const {
      fixture,
      element,
      inputs,
      buttons,
      type,
      allowChanges,
      sendTestEmail,
      saveEmailConfiguration,
    } = await setup();
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
    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).not.toHaveBeenCalled();

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

  it('should keep every field read-only until Sì of the question is pressed, and Salva off', async () => {
    const { fixture, element, inputs, buttons, yes } = await setup();
    const select = () => element.querySelector('mat-select')!;

    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(select().getAttribute('aria-disabled')).toBe('true');
    expect(buttons()[2].disabled).toBe(true);

    yes().click();
    await fixture.whenStable();
    expect(inputs().some((input) => input.readOnly)).toBe(false);
    expect(select().getAttribute('aria-disabled')).toBe('false');
    expect(yes().disabled).toBe(true);
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should ask before Reset loses what was typed, then read again with every field read-only', async () => {
    const { fixture, inputs, buttons, yes, type, settle, allowChanges, emailConfiguration } =
      await setup();
    const unsaved = TestBed.inject(UnsavedChangesBehaviour);
    await allowChanges();
    await type(1, 'Nome modificato');

    buttons()[1].click();
    await fixture.whenStable();
    expect(unsaved.asking()).toBe(true);
    unsaved.answer(false);
    await settle();
    expect(inputs()[1].value).toBe('Nome modificato');
    expect(emailConfiguration).toHaveBeenCalledTimes(1);

    buttons()[1].click();
    unsaved.answer(true);
    await settle();
    expect(emailConfiguration).toHaveBeenCalledTimes(2);
    expect(inputs()[1].value).toBe('Lido Demo');
    expect(inputs().every((input) => input.readOnly)).toBe(true);
    expect(yes().disabled).toBe(false);
  });

  it('should show the saved values, nothing to lose and nothing to send while the reading of Reset fails', async () => {
    const { fixture, inputs, buttons, yes, type, settle, allowChanges, emailConfiguration } =
      await setup();
    await allowChanges();
    await type(5, 'password-scritta');
    emailConfiguration.mockReturnValue(apiError(500, ApiErrorCode.ServerUnexpected));

    buttons()[1].click();
    TestBed.inject(UnsavedChangesBehaviour).answer(true);
    await settle();
    expect(inputs()[5].value).toBe('');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect([yes(), buttons()[0], buttons()[2]].every((button) => button.disabled)).toBe(true);
  });

  it('should take away with Reset the errors of the API under the fields', async () => {
    const { buttons, settle, allowChanges, saveEmailConfiguration, inputs } = await setup();
    saveEmailConfiguration.mockReturnValue(
      apiError(400, ApiErrorCode.ValidationFailed, {
        errors: [{ field: 'smtpHost', code: ApiErrorCode.ValidationInvalid }],
      }),
    );
    await allowChanges();
    buttons()[2].click();
    await settle();
    expect(buttons()[2].disabled).toBe(true);

    buttons()[1].click();
    await settle();
    await allowChanges();
    expect(inputs()[2].value).toBe('smtp.lido-demo.example');
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should keep Sì off while a call runs', async () => {
    const { fixture, buttons, yes, settle, sendTestEmail } = await setup();
    const answer = new Subject<void>();
    sendTestEmail.mockReturnValue(answer);

    buttons()[0].click();
    await fixture.whenStable();
    expect(yes().disabled).toBe(true);
    answer.error(new HttpErrorResponse({ status: 500 }));
    await settle();
    expect(yes().disabled).toBe(false);
  });

  it('should say under the panel which field the API refused while read-only, and leave it free after Sì', async () => {
    const { buttons, settle, allowChanges, sendTestEmail, errorText } = await setup();
    sendTestEmail.mockReturnValue(
      apiError(400, ApiErrorCode.ValidationFailed, {
        errors: [{ field: 'smtpHost', code: ApiErrorCode.ValidationInvalid }],
      }),
    );
    buttons()[0].click();
    await settle();
    expect(errorText()).toBe(
      'management.settings.email_configuration.fields.smtp_server.title error.unknown',
    );

    await allowChanges();
    expect(errorText()).toBeUndefined();
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should show at once, after Sì, what is wrong in the configuration saved', async () => {
    const { element, buttons, allowChanges } = await setup(
      vi.fn(() => of({ ...CONFIGURATION, smtpPort: 465, security: SmtpSecurity.None })),
    );
    await allowChanges();

    expect(element.querySelector('.filled__select__error')?.textContent?.trim()).toBe(
      'invalidate.email_configuration.security.ssl_port',
    );
    expect(buttons()[2].disabled).toBe(true);
  });

  it('should send nothing before Sì when there is no configuration saved', async () => {
    const { buttons } = await setup(
      vi.fn(() => of({ ...CONFIGURATION, fromEmail: null, smtpHost: null, smtpUsername: null })),
    );
    expect(buttons()[0].disabled).toBe(true);
    expect(buttons()[2].disabled).toBe(true);
  });

  it('should keep what is typed when the user changes but not the property', async () => {
    const { fixture, user, inputs, type, allowChanges, emailConfiguration } = await setup();
    await allowChanges();
    await type(1, 'Lido Nuovo');

    user.set({ propertyId: 'p1', role: 'user1' });
    await fixture.whenStable();
    expect(emailConfiguration).toHaveBeenCalledTimes(1);
    expect(inputs()[1].value).toBe('Lido Nuovo');
  });

  it('should send with each save the version given back by the last one', async () => {
    const { fixture, inputs, buttons, type, settle, allowChanges, saveEmailConfiguration } =
      await setup();
    saveEmailConfiguration.mockReturnValue(of('v2'));
    await allowChanges();
    await type(1, 'Lido Due');
    buttons()[2].click();
    await settle();
    await allowChanges();
    await type(1, 'Lido Tre');
    buttons()[2].click();
    await settle();

    expect(saveEmailConfiguration.mock.calls.map(([, version]) => version)).toEqual(['v1', 'v2']);
    expect(inputs()[1].value).toBe('Lido Tre');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    closePopups();
  });

  it('should count as a change only a value different from the one read, spaces at the sides aside', async () => {
    const { fixture, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    saveEmailConfiguration.mockReturnValue(of('v2'));
    await allowChanges();
    await type(5, '   ');
    await type(6, 'prova@lido-demo.example;info@lido-demo.example,');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    buttons()[2].click();
    await settle();
    expect(saveEmailConfiguration).toHaveBeenCalledWith(SENT, 'v1');
    closePopups();
    await allowChanges();

    await type(1, 'Lido Nuovo');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await type(1, ' Lido Demo ');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should keep the saved password for the same server, user and security typed with spaces at the sides', async () => {
    const { element, buttons, type, allowChanges } = await setup();
    const passwordError = () =>
      element
        .querySelectorAll('app-filled-text-field')[4]
        .querySelector('.filled__text__field__error');
    await allowChanges();

    await type(2, ' smtp.lido-demo.example ');
    await type(4, ' prenotazioni@lido-demo.example ');
    expect(passwordError()).toBeNull();
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should keep Salva off while what the backend wants is missing or not valid', async () => {
    const { element, buttons, inputs, type, allowChanges } = await setup();
    await allowChanges();

    for (const [index, wrong] of [
      [0, ''],
      [0, 'senza chiocciola'],
      [2, '   '],
      [4, ''],
      [6, 'prova@lido-demo.example, sbagliato'],
      [6, Array.from({ length: 11 }, (_, number) => `p${number}@lido-demo.example`).join(',')],
    ] as const) {
      const right = inputs()[index].value;
      await type(index, wrong);
      expect(buttons()[2].disabled, `${index} ${wrong}`).toBe(true);
      await type(index, right);
      expect(buttons()[2].disabled).toBe(false);
    }
    await type(1, ''); // the name may be missing
    expect(buttons()[2].disabled).toBe(false);
    expect(element.querySelector('.email__configuration__error')).toBeNull();
    expect(inputs().map((input) => input.maxLength)).toEqual([320, 160, 255, -1, 320, -1, -1]);
  });

  it('should want SSL on 465', async () => {
    const { element, buttons, type, allowChanges } = await setup();
    const securityError = () =>
      element.querySelector('app-filled-select .filled__select__error')?.textContent?.trim();
    await allowChanges();

    await type(3, '465');
    await type(5, 'password-attuale');
    expect(buttons()[2].disabled).toBe(true);
    expect(securityError()).toBe('invalidate.email_configuration.security.ssl_port');
    await type(3, '2525');
    expect(buttons()[2].disabled).toBe(false);
  });

  it('should refuse a saved port 587 without STARTTLS', async () => {
    const { element, buttons, allowChanges } = await setup(
      vi.fn(() => of({ ...CONFIGURATION, security: SmtpSecurity.Ssl })),
    );
    await allowChanges();
    expect(element.querySelector('.filled__select__error')?.textContent?.trim()).toBe(
      'invalidate.email_configuration.security.start_tls_port',
    );
    expect(buttons()[2].disabled).toBe(true);
  });

  it('should keep the test email off without recipients, and send them as a list', async () => {
    const { fixture, buttons, type, allowChanges, sendTestEmail } = await setup();
    sendTestEmail.mockReturnValue(new Subject<void>());
    await allowChanges();

    await type(6, '');
    expect(buttons()[0].disabled).toBe(true);
    await type(6, ' uno@lido-demo.example；due@lido-demo.example، UNO@lido-demo.example ');
    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).toHaveBeenCalledWith({
      ...SENT,
      testRecipients: ['uno@lido-demo.example', 'due@lido-demo.example'],
    });
  });

  it('should say how the saved configuration stands, and read it again after a test of it', async () => {
    const standing = async (changes: Partial<EmailConfigurationData>) => {
      TestBed.resetTestingModule();
      const page = await setup(vi.fn(() => of({ ...CONFIGURATION, ...changes })));
      return {
        ...page,
        text: () =>
          page.element.querySelector('.email__configuration__verification')?.textContent?.trim(),
      };
    };
    const key = (name: string) => `management.settings.email_configuration.verification.${name}`;

    expect((await standing({ fromEmail: null })).text()).toBe(key('not_configured'));
    expect((await standing({ isActive: false })).text()).toBe(key('off'));
    expect((await standing({ lastVerificationAt: null })).text()).toBe(key('never'));
    expect((await standing({})).text()).toBe(key('done'));

    const { buttons, settle, text, errorText, emailConfiguration, sendTestEmail } = await standing(
      {},
    );
    sendTestEmail.mockReturnValue(
      apiError(422, ApiErrorCode.NotificationTestEmailFailed, {
        args: { detail: '535 Authentication failed' },
      }),
    );
    const failed = { ...CONFIGURATION, lastVerificationError: '535 Authentication failed' };
    emailConfiguration.mockReturnValue(of({ ...failed, version: 'v9' }));
    buttons()[0].click();
    await settle();
    expect(errorText()).toBe('error.unknown management.settings.email_configuration.server_answer');
    expect(text()).toBe(key('done')); // the outcome of a configuration this page does not show

    emailConfiguration.mockReturnValue(of(failed));
    buttons()[0].click();
    await settle();
    expect(emailConfiguration).toHaveBeenCalledTimes(3);
    expect(text()).toBe(key('failed'));
  });

  it('should not read the outcome again after a test of values not saved, nor once the page is closed', async () => {
    const { fixture, buttons, type, settle, allowChanges, emailConfiguration, sendTestEmail } =
      await setup();
    sendTestEmail.mockReturnValue(of(undefined));
    await allowChanges();
    await type(1, 'Lido Prova');
    buttons()[0].click();
    await settle();
    expect(emailConfiguration).toHaveBeenCalledTimes(1);
    closePopups();

    await type(1, 'Lido Demo');
    const answer = new Subject<void>();
    sendTestEmail.mockReturnValue(answer);
    buttons()[0].click();
    await fixture.whenStable();
    fixture.destroy();
    answer.next();
    answer.complete();
    await settle().catch(() => undefined);
    expect(emailConfiguration).toHaveBeenCalledTimes(1);
  });

  it('should forget the outcome of the last test once another sender or connection is saved', async () => {
    const { element, buttons, type, settle, allowChanges, saveEmailConfiguration } = await setup();
    const verification = () =>
      element.querySelector('.email__configuration__verification')?.textContent?.trim();
    const key = (name: string) => `management.settings.email_configuration.verification.${name}`;
    saveEmailConfiguration.mockReturnValue(of('v2'));
    await allowChanges();
    await type(1, 'Lido Nuovo');
    buttons()[2].click();
    await settle();
    expect(verification()).toBe(key('done'));

    await allowChanges();
    await type(0, 'info@lido-demo.example');
    buttons()[2].click();
    await settle();
    expect(verification()).toBe(key('never'));
    closePopups();
  });

  it('should save the switches too, changed only after Sì', async () => {
    const { fixture, element, buttons, type, settle, allowChanges, saveEmailConfiguration } =
      await setup();
    saveEmailConfiguration.mockReturnValue(of('v2'));
    const switches = () => [...element.querySelectorAll<HTMLButtonElement>('app-toggle button')];
    expect(switches().every((toggle) => toggle.disabled)).toBe(true);

    await allowChanges();
    switches().forEach((toggle) => toggle.click());
    await fixture.whenStable();
    expect(buttons()[2].disabled).toBe(true); // the password is asked again: no certificate check
    await type(5, 'password-attuale');
    buttons()[2].click();
    await settle();
    expect(saveEmailConfiguration).toHaveBeenCalledWith(
      { ...SENT, smtpPassword: 'password-attuale', isActive: false, verifyCertificate: false },
      'v1',
    );
    closePopups();
  });
});
