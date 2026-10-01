import { compileMessage, isMessage } from './message-format';

/** `Intl` separates number and currency with a no-break space: plain spaces read better here. */
const text = (value: string) => value.replace(/ | /g, ' ');

describe('compileMessage', () => {
  const posti = '{count, plural, =0 {nessun posto} one {# posto} other {# posti}}';

  it('should choose the plural branch: exact match, then the category of the language', () => {
    const it = compileMessage(posti, 'it');

    expect([0, 1, 3, 12345].map((count) => it({ count }))).toEqual([
      'nessun posto',
      '1 posto',
      '3 posti',
      '12.345 posti',
    ]);
  });

  it('should follow the plural rules of every language', () => {
    const pl = compileMessage(
      '{n, plural, one {# miejsce} few {# miejsca} many {# miejsc} other {# miejsca}}',
      'pl',
    );

    expect([1, 2, 5].map((n) => pl({ n }))).toEqual(['1 miejsce', '2 miejsca', '5 miejsc']);
  });

  it('should format numbers, percentages and currencies in the language', () => {
    const message =
      '{n, number} | {n, number, integer} | {rate, number, percent} | {amount, number, currency}';

    expect(text(compileMessage(message, 'it')({ n: 1234.5, rate: 0.25, amount: 280 }))).toBe(
      '1234,5 | 1235 | 25% | 280,00 €',
    );
    expect(text(compileMessage(message, 'en')({ n: 1234.5, rate: 0.25, amount: 280 }))).toBe(
      '1,234.5 | 1,235 | 25% | €280.00',
    );
  });

  it('should take the currency from the currency argument, else the default one', () => {
    const price = '{amount, number, currency}';

    expect(text(compileMessage(price, 'it')({ amount: 280, currency: 'CHF' }))).toBe('280,00 CHF');
    expect(text(compileMessage(price, 'it')({ amount: 280, currency: 'euro' }))).toBe('280,00 €');
    expect(text(compileMessage(price, 'en', { defaultCurrency: 'USD' })({ amount: 280 }))).toBe(
      '$280.00',
    );
  });

  it('should show a day as it is and a moment in the time zone of the user', () => {
    const day = '{day, date, long}';
    const moment = '2026-08-01T23:30:00Z';

    expect(compileMessage(day, 'it')({ day: '2026-08-01' })).toBe('1 agosto 2026');
    expect(compileMessage(day, 'en')({ day: '2026-08-01' })).toBe('August 1, 2026');
    expect(compileMessage(day, 'it')({ day: moment })).toBe(
      new Intl.DateTimeFormat('it', { dateStyle: 'long' }).format(new Date(moment)),
    );
    expect(compileMessage('{at, time, short}', 'en')({ at: moment })).toBe(
      new Intl.DateTimeFormat('en', { timeStyle: 'short' }).format(new Date(moment)),
    );
  });

  it('should leave missing or unusable arguments as they are written', () => {
    const message = compileMessage('{name}: {count, plural, other {# posti}} il {day, date}', 'it');

    expect(message()).toBe('{name}: {count, plural, other {# posti}} il {day, date}');
    expect(message({ name: 'Lido', count: 'tre', day: 'domani' })).toBe(
      'Lido: {count, plural, other {# posti}} il {day, date}',
    );
  });

  it('should keep apostrophes and the text around the arguments', () => {
    expect(
      compileMessage(
        "L'ombrellone {id} ha {n, plural, one {# posto} other {# posti}}",
        'it',
      )({
        id: '42',
        n: 2,
      }),
    ).toBe("L'ombrellone 42 ha 2 posti");
  });

  it('should fall back to the browser language for a locale Intl does not accept', () => {
    expect(compileMessage('{n, number}', 'not a locale!')({ n: 3 })).toBe('3');
  });
});

describe('isMessage', () => {
  it('should recognise only texts with typed arguments', () => {
    expect(isMessage('{n, number}')).toBe(true);
    expect(isMessage('Restano {count, plural, other {# posti}}')).toBe(true);
    expect(isMessage('Il {day,date,long}')).toBe(true);
    expect(isMessage('Ciao {{name}}')).toBe(false);
    expect(isMessage('{name}')).toBe(false);
    expect(isMessage('Ordina per {{column}}')).toBe(false);
  });
});
