/**
 * Message format: plurals, numbers, currencies and dates in translations, with the ICU
 * MessageFormat syntax and the browser's `Intl` (no eval, no dependencies). Self-contained, to be
 * reused as a library: only the ngx-translate compiler depends on ngx-translate.
 */
export type { CompiledMessage, MessageArgs, MessageFormatOptions } from './message-format';
export { compileMessage, isMessage } from './message-format';
export { MESSAGE_FORMAT_OPTIONS, MessageFormatCompiler } from './message-format.compiler';
export type { Message, MessagePart } from './message-parser';
export { MessagePartKind, MessageSyntaxError, parseMessage } from './message-parser';
