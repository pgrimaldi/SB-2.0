/**
 * Icons of a wrapper, all of one kind, in the order the wrapper documents (e.g. search field:
 * magnifier, X). Who uses the wrapper passes them as one list: `[matIcon]` (Material icon names,
 * Material Symbols font) or `[pathIcon]` (image paths), never mixed. A missing position means no icon.
 */
export interface Icons {
  readonly material: boolean;
  readonly list: readonly string[];
}

/** `matIcon` wins over `pathIcon` as a whole list; with neither, no icons. */
export function resolveIcons(
  matIcon?: readonly string[] | null,
  pathIcon?: readonly string[] | null,
): Icons {
  return matIcon?.length
    ? { material: true, list: matIcon }
    : { material: false, list: pathIcon ?? [] };
}
