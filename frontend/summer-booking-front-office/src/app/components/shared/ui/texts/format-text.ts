import { Pipe, PipeTransform } from '@angular/core';

/** Values of the `{{name}}` placeholders of a text, e.g. `{ index: 2, total: 5 }`. */
export type TextParams = Readonly<Record<string, string | number>>;

/**
 * Fills the `{{name}}` placeholders of an already translated text (same syntax as the translation
 * files): `formatText('{{index}} di {{total}}', { index: 2, total: 5 })` gives `'2 di 5'`.
 * Without a text it gives `null`: the component leaves that text out and keeps working.
 */
export function formatText(text: string | null | undefined, params: TextParams): string | null {
  return text
    ? text.replace(/\{\{\s*(\w+)\s*\}\}/g, (placeholder, name: string) =>
        name in params ? String(params[name]) : placeholder,
      )
    : null;
}

/** `formatText` in templates: `{{ texts()?.slide | formatText: { index: 2, total: 5 } }}`. */
@Pipe({ name: 'formatText' })
export class FormatTextPipe implements PipeTransform {
  transform(text: string | null | undefined, params: TextParams): string | null {
    return formatText(text, params);
  }
}
