import { Pipe, PipeTransform } from '@angular/core';

export type TextParams = Readonly<Record<string, string | number>>;

/**
 * Fills the `{{name}}` placeholders of an already translated text (same syntax as the translation
 * files): `formatText('{{index}} di {{total}}', { index: 2, total: 5 })` gives `'2 di 5'`.
 * Without a text it gives `null`: the component leaves that text out.
 */
export function formatText(text: string | null | undefined, params: TextParams): string | null {
  return text
    ? text.replace(/\{\{\s*(\w+)\s*\}\}/g, (placeholder, name: string) =>
        name in params ? String(params[name]) : placeholder,
      )
    : null;
}

@Pipe({ name: 'formatText' })
export class FormatTextPipe implements PipeTransform {
  transform(text: string | null | undefined, params: TextParams): string | null {
    return formatText(text, params);
  }
}
