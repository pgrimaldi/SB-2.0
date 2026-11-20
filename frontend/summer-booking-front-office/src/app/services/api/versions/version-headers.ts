import { HttpHeaders, HttpResponse } from '@angular/common/http';

const IF_MATCH_HEADER = 'If-Match';
const ETAG_HEADER = 'ETag';

/**
 * The version read with the data goes back with the save: the backend answers 409
 * `resource.version_conflict` when someone changed the data in the meantime.
 */
export function ifMatch(version: string | null): HttpHeaders {
  return version ? new HttpHeaders({ [IF_MATCH_HEADER]: `"${version}"` }) : new HttpHeaders();
}

/** The version of the data after a save, from its `ETag` (also on a 204 without a body). */
export function versionOf(response: HttpResponse<unknown>): string | null {
  return unquoted(response.headers.get(ETAG_HEADER));
}

/** The version a request sends in `If-Match`. */
export function ifMatchOf(headers: HttpHeaders): string | null {
  return unquoted(headers.get(IF_MATCH_HEADER));
}

export function etag(version: string): HttpHeaders {
  return new HttpHeaders({ [ETAG_HEADER]: `"${version}"` });
}

function unquoted(tag: string | null): string | null {
  return tag ? tag.replace(/^W\//, '').replace(/^"(.*)"$/, '$1') : null;
}
