/**
 * A link of a file (photo, PDF, icon) that goes into `src` or `href`: only https ones or ours are
 * kept, a relative one becomes complete on our origin, anything else is null.
 */
export function fileLinkOf(link: string | null, origin: string): string | null {
  if (!link) {
    return null;
  }
  try {
    const url = new URL(link, origin);
    return url.protocol === 'https:' || url.origin === origin ? url.href : null;
  } catch {
    return null;
  }
}
