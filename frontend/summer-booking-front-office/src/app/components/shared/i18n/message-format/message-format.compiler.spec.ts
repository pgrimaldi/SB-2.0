import { TestBed } from '@angular/core/testing';
import {
  TranslateService,
  provideTranslateCompiler,
  provideTranslateService,
} from '@ngx-translate/core';
import { MESSAGE_FORMAT_OPTIONS, MessageFormatCompiler } from './message-format.compiler';

describe('MessageFormatCompiler', () => {
  const setup = (options?: { defaultCurrency: string }) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService({ compiler: provideTranslateCompiler(MessageFormatCompiler) }),
        ...(options ? [{ provide: MESSAGE_FORMAT_OPTIONS, useValue: options }] : []),
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      seats: 'Restano {count, plural, one {# posto} other {# posti}}',
      price: '{amount, number, currency}',
      greeting: 'Ciao {{name}}',
      table: { range: '{{start}} – {{end}} di {{total}}' },
      broken: '{count, plural, one {# posto}}',
    });
    translate.use('it');
    return translate;
  };

  it('should fill the messages with the translation params', () => {
    const translate = setup();

    expect(translate.instant('seats', { count: 1 })).toBe('Restano 1 posto');
    expect(translate.instant('seats', { count: 4 })).toBe('Restano 4 posti');
  });

  it('should leave the other texts to ngx-translate, also inside groups', () => {
    const translate = setup();

    expect(translate.instant('greeting', { name: 'Marta' })).toBe('Ciao Marta');
    expect(translate.instant('table')).toEqual({ range: '{{start}} – {{end}} di {{total}}' });
  });

  it('should keep a message that is not valid as plain text', () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const translate = setup();

    expect(translate.instant('broken', { count: 1 })).toBe('{count, plural, one {# posto}}');
    expect(console.error).toHaveBeenCalled();
  });

  it('should use the default currency of the options', () => {
    const translate = setup({ defaultCurrency: 'CHF' });

    expect(translate.instant('price', { amount: 5 }).replace(/ /g, ' ')).toBe('5,00 CHF');
  });
});
