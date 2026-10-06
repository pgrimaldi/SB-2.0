import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
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

  const setup = async (emailConfiguration = vi.fn(() => of(CONFIGURATION))) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: SystemService, useValue: { emailConfiguration } },
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
    return { element: fixture.nativeElement as HTMLElement, emailConfiguration };
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
});
