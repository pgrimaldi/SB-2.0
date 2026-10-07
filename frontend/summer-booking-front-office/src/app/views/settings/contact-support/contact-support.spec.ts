import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { SystemService } from '../../../services/api/system/system.service';
import { ContactSupport } from './contact-support';

describe('ContactSupport', () => {
  const SUPPORT_INFO = {
    phoneNumber: '050 7916620',
    mailAddress: 'info@summerbooking.it',
    supportHour: ['1 maggio - 30 settembre', '1 ottobre - 30 aprile'],
  };

  const setup = async (supportInfo = vi.fn(() => of(SUPPORT_INFO))) => {
    const contactSupport = vi.fn();
    const language = signal('it');
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: SystemService, useValue: { contactSupport, supportInfo } },
        { provide: DATA_RELOAD, useValue: language },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(ContactSupport);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const fields = () => [
      ...element.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
        'form input, form textarea',
      ),
    ];
    const send = () => element.querySelector<HTMLButtonElement>('.contact__support__send button')!;
    const type = async (index: number, text: string) => {
      fields()[index].value = text;
      fields()[index].dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    // The answer of the API is awaited by Signal Forms submit(): one more tick before checking.
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const fill = async () => {
      await type(0, ' Anna ');
      await type(1, 'Bianchi');
      await type(2, 'anna.bianchi@example.com');
      await type(4, 'Vorrei informazioni.');
    };
    return {
      fixture,
      element,
      fields,
      send,
      type,
      settle,
      fill,
      contactSupport,
      supportInfo,
      language,
    };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should show the support contacts of the API and the five fields with their examples', async () => {
    const { element, fields, supportInfo } = await setup();

    expect(supportInfo).toHaveBeenCalledWith({ idProperty: 'p1' });
    expect(element.querySelector('a[href="tel:0507916620"]')?.textContent?.trim()).toBe(
      '050 7916620',
    );
    expect(element.querySelector('a[href="mailto:info@summerbooking.it"]')).not.toBeNull();
    expect(
      [...element.querySelectorAll('.contact__support__box__hours p')].map((line) =>
        line.textContent?.trim(),
      ),
    ).toEqual(['1 maggio - 30 settembre', '1 ottobre - 30 aprile']);
    expect(fields().map((field) => field.placeholder)).toEqual([
      'management.settings.contact_support.form.examples.first_name',
      'management.settings.contact_support.form.examples.last_name',
      'management.settings.contact_support.form.examples.email',
      'management.settings.contact_support.form.examples.mobile_phone',
      '',
    ]);
  });

  it('should send only with name, surname, email and message, the mobile phone optional', async () => {
    const { send, type, fill } = await setup();

    expect(send().disabled).toBe(true);
    await type(0, 'Anna');
    // The generic key (the test has no translations).
    await type(0, '');
    expect(document.body.textContent).toContain('invalidate.required');
    await fill();
    expect(send().disabled).toBe(false);
  });

  it('should send the trimmed fields, null without a mobile phone, then thank and empty the form', async () => {
    const { fixture, element, fields, send, settle, fill, contactSupport } = await setup();
    const answer = new Subject<void>();
    contactSupport.mockReturnValue(answer);
    await fill();

    send().click();
    await fixture.whenStable();
    expect(contactSupport).toHaveBeenCalledWith({
      idProperty: 'p1',
      firstName: 'Anna',
      lastName: 'Bianchi',
      email: 'anna.bianchi@example.com',
      mobilePhone: null,
      message: 'Vorrei informazioni.',
    });
    expect(element.querySelector('mat-progress-spinner')).not.toBeNull();
    expect(fields().every((field) => field.readOnly)).toBe(true); // nothing changes until the answer

    answer.next();
    answer.complete();
    await settle();
    expect(document.querySelector('.message__popup h2')?.textContent?.trim()).toBe(
      'management.settings.contact_support.sent.title',
    );
    expect(fields().map((field) => field.value)).toEqual(['', '', '', '', '']);
    expect(element.querySelector('[class$="__error"]')).toBeNull();
  });

  it('should keep what was typed and show the error when the message is not sent', async () => {
    const { fixture, element, fields, send, fill, contactSupport } = await setup();
    contactSupport.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' })),
    );
    await fill();

    send().click();
    await fixture.whenStable();
    expect(element.querySelector('.contact__support__error')?.textContent?.trim()).not.toBe('');
    expect(fields()[1].value).toBe('Bianchi');
    expect(document.querySelector('.message__popup')).toBeNull();
  });

  it('should ask the support contacts again when the language changes', async () => {
    const { fixture, supportInfo, language } = await setup();

    language.set('en');
    await fixture.whenStable();
    expect(supportInfo).toHaveBeenCalledTimes(2);
  });

  it('should keep the boxes and show the error when the support contacts do not arrive', async () => {
    const { element } = await setup(
      vi.fn(() => throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown' }))),
    );

    expect(element.querySelectorAll('.contact__support__box').length).toBe(3);
    expect(element.querySelector('.contact__support__box a')).toBeNull();
    expect(element.querySelector('.contact__support__info__error')?.textContent?.trim()).not.toBe(
      '',
    );
  });

  it('should show an error of the API on a field under that field, until the field changes', async () => {
    const { element, send, type, settle, fill, contactSupport } = await setup();
    contactSupport.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.invalid_request',
              title: 'Invalid request',
              errors: [{ field: 'email', code: 'validation.invalid_value' }],
            },
          }),
      ),
    );
    await fill();
    const emailError = () =>
      element
        .querySelectorAll('app-filled-text-field')[2]
        .querySelector('.filled__text__field__error');

    send().click();
    await settle();
    // The message of the code (here the fallback key: the test has no translations).
    expect(emailError()?.textContent?.trim()).toBe('error.unknown');
    expect(element.querySelector('.contact__support__error')).toBeNull();

    await type(2, 'anna@example.com');
    expect(emailError()).toBeNull();
  });
});
