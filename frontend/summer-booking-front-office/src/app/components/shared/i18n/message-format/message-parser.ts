/** Style of a `number` argument: plain, integer, percent or currency. */
export type NumberStyle = 'number' | 'integer' | 'percent' | 'currency';

/** Style of a `date` or `time` argument, as in `Intl.DateTimeFormat`. */
export type DateStyle = 'short' | 'medium' | 'long' | 'full';

/** A piece of a parsed message: plain text or an argument to fill. */
export type MessagePart =
  | string
  | { readonly kind: 'value'; readonly name: string; readonly source: string }
  | {
      readonly kind: 'number';
      readonly name: string;
      readonly style: NumberStyle;
      readonly source: string;
    }
  | {
      readonly kind: 'date' | 'time';
      readonly name: string;
      readonly style: DateStyle;
      readonly source: string;
    }
  | {
      readonly kind: 'plural';
      readonly name: string;
      /** Branches by selector: `=0`, `one`, `other`... (`other` is always there). */
      readonly branches: Readonly<Record<string, Message>>;
      readonly source: string;
    }
  /** `#` inside a plural branch: the number of that plural. */
  | { readonly kind: 'count' };

export type Message = readonly MessagePart[];

/** A text that is not a valid message; the position helps to find the mistake. */
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
const NUMBER_STYLES: readonly string[] = ['integer', 'percent', 'currency'];
const DATE_STYLES: readonly string[] = ['short', 'medium', 'long', 'full'];

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

  /** Fails with the reason and the current position. */
  fail(reason: string): never {
    throw new MessageSyntaxError(reason, this.text, this.position);
  }

  private count(): MessagePart {
    this.position++;
    return { kind: 'count' };
  }

  private argument(): MessagePart {
    const start = this.position;
    this.expect('{');
    const name = this.token(NAME, 'Argument name expected');
    const source = () => this.text.slice(start, this.position);

    if (this.skip('}')) {
      return { kind: 'value', name, source: source() };
    }
    this.expect(',');
    const kind = this.token(NAME, 'Argument type expected');
    if (kind === 'plural') {
      this.expect(',');
      const branches = this.branches();
      this.expect('}');
      return { kind, name, branches, source: source() };
    }
    if (kind !== 'number' && kind !== 'date' && kind !== 'time') {
      this.fail(`Unknown argument type "${kind}"`);
    }
    const style = this.skip(',') ? this.token(NAME, 'Style expected') : undefined;
    this.expect('}');

    if (kind === 'number') {
      if (style !== undefined && !NUMBER_STYLES.includes(style)) {
        this.fail(`Unknown number style "${style}"`);
      }
      return { kind, name, style: (style ?? 'number') as NumberStyle, source: source() };
    }
    if (style !== undefined && !DATE_STYLES.includes(style)) {
      this.fail(`Unknown ${kind} style "${style}"`);
    }
    return { kind, name, style: (style ?? 'medium') as DateStyle, source: source() };
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

  /** Reads a token (after optional spaces) with a sticky regular expression. */
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

  /** Moves past `char` (after optional spaces) when it is the next one. */
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
