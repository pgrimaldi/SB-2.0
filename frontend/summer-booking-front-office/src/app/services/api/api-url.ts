import { environment } from '../../../environments/environment';

/** True for requests to our API (never to other hosts): only they get tokens and user data. */
export function isApiUrl(url: string): boolean {
  const base = environment.apiBaseUrl;
  return url === base || url.startsWith(`${base}/`);
}
