import { DOCUMENT } from '@angular/common';
import { HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

const MAX_FILE_NAME_LENGTH = 150;
/** Path separators and characters that no file system accepts in a name. */
const UNSAFE_CHARACTERS = /[\\/:*?"<>|\u0000-\u001f\u007f]/g;

/**
 * Saves a file sent by our API on the user's device, with the name the server gives in
 * `Content-Disposition` (`filename*` in UTF-8 first, then `filename`), or `fallbackName`.
 */
@Injectable({ providedIn: 'root' })
export class FileDownloadBehaviour {
  private readonly document = inject(DOCUMENT);

  save(response: HttpResponse<Blob>, fallbackName: string): void {
    if (!response.body) {
      return;
    }
    const url = URL.createObjectURL(response.body);
    const link = this.document.createElement('a');
    link.href = url;
    link.download = fileNameOf(response.headers.get('Content-Disposition')) ?? fallbackName;
    link.click();
    // Some browsers read the file only after the click has returned.
    setTimeout(() => URL.revokeObjectURL(url));
  }
}

/** The server's name is not trusted: only its last part, without unsafe characters, is kept. */
export function fileNameOf(contentDisposition: string | null): string | null {
  if (!contentDisposition) {
    return null;
  }
  const encoded = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(contentDisposition)?.[1];
  const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(contentDisposition)?.[1];
  let name = plain;
  if (encoded) {
    try {
      name = decodeURIComponent(encoded.trim());
    } catch {
      // A malformed encoded name: the plain one, if any.
    }
  }
  const safe = name
    ?.split(/[\\/]/)
    .pop()
    ?.replace(UNSAFE_CHARACTERS, '')
    .trim()
    .slice(0, MAX_FILE_NAME_LENGTH);
  return safe && !/^\.+$/.test(safe) ? safe : null;
}
