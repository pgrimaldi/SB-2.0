import {
  formatDate,
  formatNumber,
  pluralCategory,
  supportedLocale,
  toNumber,
} from './message-formatters';
import { Message, MessagePart, parseMessage } from './message-parser';

export type MessageArgs = Readonly<Record<string, unknown>>;

export type CompiledMessage = (args?: MessageArgs) => string;

export interface MessageFormatOptions {
  /** Used when the arguments have no `currency`. EUR by default. */
  readonly defaultCurrency?: string;
}

const DEFAULT_CURRENCY = 'EUR';

/** Typed arguments make a text a message of this library; any other text is left as it is. */
const TYPED_ARGUMENT = /\{\s*[A-Za-z_]\w*\s*,\s*(?:number|date|time|plural)\s*[,}]/;

export function isMessage(text: string): boolean {
  return TYPED_ARGUMENT.test(text);
}

/**
 * Compiles a message (ICU MessageFormat syntax, see `parseMessage`) for a locale, without `eval`:
 * the result is a function that fills it with `Intl`. A missing or unusable argument stays as it is
 * written (`{count}`), so the text still shows where the mistake is. Throws `MessageSyntaxError`
 * when the text is not valid.
 *
 * compileMessage('{count, plural, one {# posto} other {# posti}}', 'it')({ count: 3 }) → '3 posti'
 */
export function compileMessage(
  text: string,
  locale: string,
  options: MessageFormatOptions = {},
): CompiledMessage {
  const message = parseMessage(text);
  const context: Context = {
    locale: supportedLocale(locale),
    defaultCurrency: options.defaultCurrency ?? DEFAULT_CURRENCY,
  };
  return (args = {}) => render(message, args, context, null);
}

interface Context {
  readonly locale: string | undefined;
  readonly defaultCurrency: string;
}

/** `count` fills `#` inside a plural branch. */
function render(
  message: Message,
  args: MessageArgs,
  context: Context,
  count: number | null,
): string {
  return message.map((part) => renderPart(part, args, context, count)).join('');
}

function renderPart(
  part: MessagePart,
  args: MessageArgs,
  context: Context,
  count: number | null,
): string {
  if (typeof part === 'string') {
    return part;
  }
  const { locale, defaultCurrency } = context;
  switch (part.kind) {
    case 'count':
      return count === null ? '#' : formatNumber(count, 'number', locale, null, defaultCurrency);
    case 'value': {
      const value = args[part.name];
      return typeof value === 'string' || typeof value === 'number' ? String(value) : part.source;
    }
    case 'number': {
      const value = toNumber(args[part.name]);
      return value === null
        ? part.source
        : formatNumber(value, part.style, locale, args['currency'], defaultCurrency);
    }
    case 'date':
    case 'time':
      return formatDate(args[part.name], part.kind === 'time', part.style, locale) ?? part.source;
    case 'plural': {
      const value = toNumber(args[part.name]);
      if (value === null) {
        return part.source;
      }
      // An exact match (=0) wins over the category of the language (one, other…).
      const branch =
        part.branches[`=${value}`] ??
        part.branches[pluralCategory(value, locale)] ??
        part.branches['other'];
      return render(branch, args, context, value);
    }
  }
}
