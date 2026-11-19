/** Two texts compared without the spaces at the start and at the end; no text counts as `''`. */
export function sameText(
  first: string | null | undefined,
  second: string | null | undefined,
): boolean {
  return (first ?? '').trim() === (second ?? '').trim();
}

/** As `sameText`, without telling capitals from small letters (e.g. a name already used). */
export function sameTextIgnoringCase(
  first: string | null | undefined,
  second: string | null | undefined,
): boolean {
  return (first ?? '').trim().toLowerCase() === (second ?? '').trim().toLowerCase();
}

/**
 * `JSON.stringify` of a model with every text without the spaces at the sides, and a text left
 * empty as `null`: two models compared this way differ only by real changes ("ci sono modifiche?").
 */
export function comparableJson(value: unknown): string {
  return JSON.stringify(value, (_key, item: unknown) =>
    typeof item === 'string' ? item.trim() || null : item,
  );
}
