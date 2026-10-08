export enum MessagePartKind {
  Value,
  Number,
  Date,
  Time,
  Plural,
  Count,
}

export enum NumberStyle {
  Number,
  Integer,
  Percent,
  Currency,
}

/** The values are those of `dateStyle` and `timeStyle` of `Intl.DateTimeFormat`. */
export enum DateStyle {
  Short = 'short',
  Medium = 'medium',
  Long = 'long',
  Full = 'full',
}

export type MessagePart =
  | string
  | { readonly kind: MessagePartKind.Value; readonly name: string; readonly source: string }
  | {
      readonly kind: MessagePartKind.Number;
      readonly name: string;
      readonly style: NumberStyle;
      readonly source: string;
    }
  | {
      readonly kind: MessagePartKind.Date | MessagePartKind.Time;
      readonly name: string;
      readonly style: DateStyle;
      readonly source: string;
    }
  | {
      readonly kind: MessagePartKind.Plural;
      readonly name: string;
      /** `other` is always present. */
      readonly branches: Readonly<Record<string, Message>>;
      readonly source: string;
    }
  /** `#` inside a plural branch: the number of that plural. */
  | { readonly kind: MessagePartKind.Count };

export type Message = readonly MessagePart[];

export class MessageSyntaxError extends Error {
  constructor(
    reason: string,
    readonly text: string,
    readonly position: number,
  ) {
    super(`${reason} at position ${position} of "${text}"`);
    this.name = 'MessageSyntaxError';
  }
}

const NAME = /[A-Za-z_]\w*/y;
const PLURAL_SELECTOR = /=\d+|zero|one|two|few|many|other/y;
// The words of the syntax, as they are written in the translations.
const ARGUMENT_TYPES: Readonly<
  Record<
    string,
    MessagePartKind.Number | MessagePartKind.Date | MessagePartKind.Time | MessagePartKind.Plural
  >
> = {
  number: MessagePartKind.Number,
  date: MessagePartKind.Date,
  time: MessagePartKind.Time,
  plural: MessagePartKind.Plural,
};
const NUMBER_STYLES: Readonly<Record<string, NumberStyle>> = {
  integer: NumberStyle.Integer,
  percent: NumberStyle.Percent,
  currency: NumberStyle.Currency,
};
const DATE_STYLES: Readonly<Record<string, DateStyle>> = {
  short: DateStyle.Short,
  medium: DateStyle.Medium,
  long: DateStyle.Long,
  full: DateStyle.Full,
};

/** Own keys only: a word such as `constructor` is not in the table. */
function wordIn<T>(table: Readonly<Record<string, T>>, word: string): T | undefined {
  return Object.hasOwn(table, word) ? table[word] : undefined;
}

/**
 * Parses a message in the ICU MessageFormat syntax (the subset this library supports):
 * `{name}`, `{name, number[, integer|percent|currency]}`, `{name, date|time[, short|medium|long|full]}`
 * and `{name, plural, =0 {…} one {# …} other {# …}}`. Unlike ICU, apostrophes are always plain
 * text ("L'ombrellone"), so braces cannot be written as text.
 */
export function parseMessage(text: string): Message {
  const parser = new MessageParser(text);
  const message = parser.message(false);
  if (parser.position < text.length) {
    parser.fail('Unexpected "}"');
  }
  return message;
}

class MessageParser {
  position = 0;

  constructor(private readonly text: string) {}

  /** Text and arguments up to the end, or up to the `}` that closes a plural branch. */
  message(inPlural: boolean): Message {
    const parts: MessagePart[] = [];
    let plain = '';
    while (this.position < this.text.length) {
      const char = this.text[this.position];
      if (char === '}') {
        break;
      }
      if (char === '{' || (char === '#' && inPlural)) {
        if (plain) {
          parts.push(plain);
          plain = '';
        }
        parts.push(char === '{' ? this.argument() : this.count());
      } else {
        plain += char;
        this.position++;
      }
    }
    if (plain) {
      parts.push(plain);
    }
    return parts;
  }

  fail(reason: string): never {
    throw new MessageSyntaxError(reason, this.text, this.position);
  }

  private count(): MessagePart {
    this.position++;
    return { kind: MessagePartKind.Count };
  }

  private argument(): MessagePart {
    const start = this.position;
    this.expect('{');
    const name = this.token(NAME, 'Argument name expected');
    const source = () => this.text.slice(start, this.position);

    if (this.skip('}')) {
      return { kind: MessagePartKind.Value, name, source: source() };
    }
    this.expect(',');
    const type = this.token(NAME, 'Argument type expected');
    const kind = wordIn(ARGUMENT_TYPES, type);
    if (kind === MessagePartKind.Plural) {
      this.expect(',');
      const branches = this.branches();
      this.expect('}');
      return { kind, name, branches, source: source() };
    }
    if (kind === undefined) {
      this.fail(`Unknown argument type "${type}"`);
    }
    const word = this.skip(',') ? this.token(NAME, 'Style expected') : undefined;
    this.expect('}');

    if (kind === MessagePartKind.Number) {
      const style = word === undefined ? NumberStyle.Number : wordIn(NUMBER_STYLES, word);
      if (style === undefined) {
        this.fail(`Unknown number style "${word}"`);
      }
      return { kind, name, style, source: source() };
    }
    const style = word === undefined ? DateStyle.Medium : wordIn(DATE_STYLES, word);
    if (style === undefined) {
      this.fail(`Unknown ${type} style "${word}"`);
    }
    return { kind, name, style, source: source() };
  }

  /** `=0 {…} one {…} other {…}`: at least `other`, each selector once. */
  private branches(): Record<string, Message> {
    const branches: Record<string, Message> = {};
    while (!this.peek('}')) {
      const selector = this.token(PLURAL_SELECTOR, 'Plural selector expected (=N, one, other…)');
      if (selector in branches) {
        this.fail(`Repeated plural selector "${selector}"`);
      }
      this.expect('{');
      branches[selector] = this.message(true);
      this.expect('}');
    }
    if (!('other' in branches)) {
      this.fail('Plural without "other"');
    }
    return branches;
  }

  /** Skips spaces first; `pattern` must be sticky (`y`). */
  private token(pattern: RegExp, reason: string): string {
    this.spaces();
    pattern.lastIndex = this.position;
    const match = pattern.exec(this.text);
    if (!match) {
      this.fail(reason);
    }
    this.position += match[0].length;
    return match[0];
  }

  private expect(char: string): void {
    if (!this.skip(char)) {
      this.fail(`"${char}" expected`);
    }
  }

  /** Skips spaces first. */
  private skip(char: string): boolean {
    if (!this.peek(char)) {
      return false;
    }
    this.position++;
    return true;
  }

  private peek(char: string): boolean {
    this.spaces();
    // Inside an argument the text cannot end: a `}` is always still to come.
    if (this.position >= this.text.length && char === '}') {
      this.fail('"}" expected');
    }
    return this.text[this.position] === char;
  }

  private spaces(): void {
    while (/\s/.test(this.text[this.position] ?? '')) {
      this.position++;
    }
  }
}
