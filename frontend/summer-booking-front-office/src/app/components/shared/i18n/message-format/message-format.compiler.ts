import { Injectable, InjectionToken, inject, isDevMode } from '@angular/core';
import {
  InterpolatableTranslation,
  InterpolatableTranslationObject,
  TranslateCompiler,
  TranslationObject,
} from '@ngx-translate/core';
import { MessageFormatOptions, compileMessage, isMessage } from './message-format';

/** Options of the compiler, e.g. `{ defaultCurrency: 'CHF' }`. */
export const MESSAGE_FORMAT_OPTIONS = new InjectionToken<MessageFormatOptions>(
  'MESSAGE_FORMAT_OPTIONS',
);

/**
 * Compiler of ngx-translate (`provideTranslateCompiler(MessageFormatCompiler)`): the texts with typed
 * arguments become compiled messages, filled with the translation params; every other text stays
 * as it is, with the `{{name}}` placeholders of ngx-translate. A message that is not valid stays as
 * plain text (with an error in the console during development), so the page never breaks.
 */
@Injectable()
export class MessageFormatCompiler extends TranslateCompiler {
  private readonly options = inject(MESSAGE_FORMAT_OPTIONS, { optional: true }) ?? {};

  compile(value: string, lang: string): InterpolatableTranslation {
    if (!isMessage(value)) {
      return value;
    }
    try {
      return compileMessage(value, lang, this.options);
    } catch (error) {
      if (isDevMode()) {
        console.error(`Translation "${value}" (${lang}) is not a valid message:`, error);
      }
      return value;
    }
  }

  compileTranslations(
    translations: TranslationObject,
    lang: string,
  ): InterpolatableTranslationObject {
    return this.compileNode(translations, lang) as InterpolatableTranslationObject;
  }

  /** Compiles the texts of a group of translations, at any depth. */
  private compileNode(node: unknown, lang: string): InterpolatableTranslation {
    if (typeof node === 'string') {
      return this.compile(node, lang);
    }
    if (Array.isArray(node)) {
      return node.map((item) => this.compileNode(item, lang));
    }
    if (node !== null && typeof node === 'object') {
      return Object.fromEntries(
        Object.entries(node).map(([key, value]) => [key, this.compileNode(value, lang)]),
      );
    }
    return node as InterpolatableTranslation;
  }
}
