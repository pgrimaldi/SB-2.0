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
  const CONFIGURATION = {
    senderMailAddress: 'prenotazioni@lido-demo.example',
    senderName: 'Lido Demo',
    smtpServerAddress: 'smtp.lido-demo.example',
    smtpPort: 587,
    smtpUsername: 'prenotazioni@lido-demo.example',
    smtpPassword: 'password-di-esempio',
    smtpSecurity: 'Tls' as const,
  };

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
    const type = async (index: number, text: string) => {
      inputs()[index].value = text;
      inputs()[index].dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return {
      fixture,
      element,
      inputs,
      buttons,
      type,
      emailConfiguration,
      sendTestEmail,
      saveEmailConfiguration,
    };
  };

  it('should show six masked fields and the security select, each named by its text', async () => {
    const { element } = await setup();
    const inputs = [...element.querySelectorAll('input')];

    expect(inputs.length).toBe(6);
    for (const input of inputs) {
      expect(input.type).toBe('password');
      expect(input.autocomplete).toBe('new-password');
      const [title, description] = input.getAttribute('aria-labelledby')!.split(' ');
      expect(element.querySelector(`#${title}`)?.textContent).toContain('.title');
      expect(element.querySelector(`#${description}`)?.textContent).toContain('.description');
    }
    expect(element.querySelector('mat-select')?.getAttribute('aria-labelledby')).toContain(
      'email-configuration-smtp_security',
    );
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
      'password-di-esempio',
    ]);
    expect(element.querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('TLS');
  });

  it('should show the error when the configuration does not arrive', async () => {
    const { element } = await setup(
      vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown' }))),
    );

    expect(element.querySelector('.email__configuration__error')?.textContent?.trim()).not.toBe('');
  });

  it('should ask the configuration again with Reset, losing what was typed', async () => {
    const { fixture, inputs, buttons, type, emailConfiguration } = await setup();
    await type(1, 'Nome modificato');

    buttons()[1].click();
    await fixture.whenStable();

    expect(emailConfiguration).toHaveBeenCalledTimes(2);
    expect(inputs()[1].value).toBe('Lido Demo');
  });

  it('should send the values in the fields as the test email, then say it was sent', async () => {
    const { fixture, element, buttons, type, sendTestEmail } = await setup();
    const answer = new Subject<void>();
    sendTestEmail.mockReturnValue(answer);
    await type(1, 'Lido Prova');

    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).toHaveBeenCalledWith({
      idProperty: 'p1',
      ...CONFIGURATION,
      senderName: 'Lido Prova',
    });
    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.test_sent.title',
    );
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
  });

  it('should send a port that is not a whole number as null and show the error of the API', async () => {
    const { fixture, element, buttons, type, sendTestEmail } = await setup();
    sendTestEmail.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { status: 400, code: 'validation.invalid_request', title: 'Invalid request' },
          }),
      ),
    );
    await type(3, '58a');

    buttons()[0].click();
    await fixture.whenStable();
    expect(sendTestEmail).toHaveBeenCalledWith(expect.objectContaining({ smtpPort: null }));
    expect(element.querySelector('.email__configuration__error')).not.toBeNull();
  });

  it('should save the values in the fields, blocking the other buttons meanwhile, then say it', async () => {
    const { fixture, buttons, type, saveEmailConfiguration } = await setup();
    const answer = new Subject<void>();
    saveEmailConfiguration.mockReturnValue(answer);
    await type(4, 'nuovo-utente@lido-demo.example');

    buttons()[2].click();
    await fixture.whenStable();
    expect(saveEmailConfiguration).toHaveBeenCalledWith({
      idProperty: 'p1',
      ...CONFIGURATION,
      smtpUsername: 'nuovo-utente@lido-demo.example',
    });
    expect(buttons()[0].disabled).toBe(true);
    expect(buttons()[1].disabled).toBe(true);

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.email_configuration.saved.title',
    );
    expect(buttons()[0].disabled).toBe(false);
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());
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
});
