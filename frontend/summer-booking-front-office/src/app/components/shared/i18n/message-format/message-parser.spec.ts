import {
  DateStyle,
  MessagePartKind,
  MessageSyntaxError,
  NumberStyle,
  parseMessage,
} from './message-parser';

describe('parseMessage', () => {
  it('should read plain text, apostrophes and # outside plurals as text', () => {
    expect(parseMessage("L'ombrellone n. #4")).toEqual(["L'ombrellone n. #4"]);
    expect(parseMessage('')).toEqual([]);
  });

  it('should read values, numbers, dates and times with their styles', () => {
    expect(
      parseMessage('{name}: {total, number, currency} il { day , date , long } {at, time}'),
    ).toEqual([
      { kind: MessagePartKind.Value, name: 'name', source: '{name}' },
      ': ',
      {
        kind: MessagePartKind.Number,
        name: 'total',
        style: NumberStyle.Currency,
        source: '{total, number, currency}',
      },
      ' il ',
      {
        kind: MessagePartKind.Date,
        name: 'day',
        style: DateStyle.Long,
        source: '{ day , date , long }',
      },
      ' ',
      { kind: MessagePartKind.Time, name: 'at', style: DateStyle.Medium, source: '{at, time}' },
    ]);
    expect(parseMessage('{n, number}')).toEqual([
      { kind: MessagePartKind.Number, name: 'n', style: NumberStyle.Number, source: '{n, number}' },
    ]);
  });

  it('should read plurals with exact matches, categories, # and nested arguments', () => {
    const [plural] = parseMessage(
      '{count, plural, =0 {nessun posto} one {# posto il {day, date}} other {# posti}}',
    );

    expect(plural).toEqual({
      kind: MessagePartKind.Plural,
      name: 'count',
      source: '{count, plural, =0 {nessun posto} one {# posto il {day, date}} other {# posti}}',
      branches: {
        '=0': ['nessun posto'],
        one: [
          { kind: MessagePartKind.Count },
          ' posto il ',
          {
            kind: MessagePartKind.Date,
            name: 'day',
            style: DateStyle.Medium,
            source: '{day, date}',
          },
        ],
        other: [{ kind: MessagePartKind.Count }, ' posti'],
      },
    });
  });

  it.each([
    ['{count, plural, one {# posto}}', 'Plural without "other"'],
    ['{count, plural, other {a} other {b}}', 'Repeated plural selector "other"'],
    ['{count, plural, some {a} other {b}}', 'Plural selector expected'],
    ['{n, choice}', 'Unknown argument type "choice"'],
    ['{n, number, money}', 'Unknown number style "money"'],
    ['{d, date, tiny}', 'Unknown date style "tiny"'],
    ['{n, number', '"}" expected'],
    ['{{name}} has {n, number}', 'Argument name expected'],
    ['a } b', 'Unexpected "}"'],
  ])('should refuse %s', (text, reason) => {
    expect(() => parseMessage(text)).toThrow(MessageSyntaxError);
    expect(() => parseMessage(text)).toThrow(reason);
  });
});
