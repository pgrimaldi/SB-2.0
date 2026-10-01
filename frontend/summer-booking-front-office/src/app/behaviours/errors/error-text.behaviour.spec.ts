import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { ApiProblem } from '../../entities/errors/api-problem';
import { ErrorTextBehaviour } from './error-text.behaviour';

describe('ErrorTextBehaviour', () => {
  afterEach(() => vi.restoreAllMocks());

  const setup = () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      error: {
        unknown: 'Si è verificato un errore. Riprova.',
        booking: { umbrella_overlap: "L'ombrellone {{umbrellaId}} è già prenotato il {{date}}." },
        validation: {
          end_before_start: 'La fine viene prima dell’inizio.',
          required: 'Obbligatorio.',
        },
      },
    });
    translate.setTranslation('en', {
      error: {
        unknown: 'Something went wrong. Please try again.',
        booking: { umbrella_overlap: 'Umbrella {{umbrellaId}} is already booked on {{date}}.' },
      },
    });
    translate.use('it');
    return { errors: TestBed.inject(ErrorTextBehaviour), translate };
  };

  const overlap: ApiProblem = {
    status: 409,
    code: 'booking.umbrella_overlap',
    args: { umbrellaId: '42', date: '01/08/2026' },
  };

  it('should use the backend code as the key under error., with its args', async () => {
    const { errors } = setup();

    expect(await firstValueFrom(errors.text(overlap))).toBe(
      "L'ombrellone 42 è già prenotato il 01/08/2026.",
    );
  });

  it('should show error.unknown for a code with no translation, and warn the developers', async () => {
    const { errors } = setup();
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    const text = await firstValueFrom(errors.text({ status: 409, code: 'booking.brand_new_code' }));

    expect(text).toBe('Si è verificato un errore. Riprova.');
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('booking.brand_new_code'));
  });

  it('should give the translation key of the error, error.unknown while it has none', async () => {
    const { errors } = setup();
    vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    expect(await firstValueFrom(errors.key(overlap))).toBe('error.booking.umbrella_overlap');
    expect(await firstValueFrom(errors.key({ status: 500, code: 'server.brand_new' }))).toBe(
      'error.unknown',
    );
  });

  it('should follow the language', () => {
    const { errors, translate } = setup();
    const texts: string[] = [];
    const subscription = errors.text(overlap).subscribe((text) => texts.push(text));

    translate.use('en');
    subscription.unsubscribe();

    expect(texts).toEqual([
      "L'ombrellone 42 è già prenotato il 01/08/2026.",
      'Umbrella 42 is already booked on 01/08/2026.',
    ]);
  });

  it('should give the messages of the field errors by field, the first one for each field', async () => {
    const { errors } = setup();

    const texts = await firstValueFrom(
      errors.fieldTexts({
        status: 400,
        code: 'validation.invalid_request',
        errors: [
          { field: 'datetimeTo', code: 'validation.end_before_start' },
          { field: 'name', code: 'validation.required' },
          { field: 'name', code: 'validation.end_before_start' }, // second error of the same field
        ],
      }),
    );

    expect(texts).toEqual({
      datetimeTo: 'La fine viene prima dell’inizio.',
      name: 'Obbligatorio.',
    });
    expect(await firstValueFrom(errors.fieldTexts(overlap))).toEqual({}); // no field errors
  });
});
