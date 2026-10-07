import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, RedirectFunction } from '@angular/router';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from './language.behaviour';

/** Matches the `:lang` segment only for supported languages; anything else falls through to 404. */
export const languageMatchGuard: CanMatchFn = (_route, segments) =>
  inject(LanguageBehaviour).isSupported(segments[0]?.path);

export const languageActivateGuard: CanActivateFn = (route) => {
  inject(LanguageBehaviour).use(route.paramMap.get('lang') as Language);
  return true;
};

export const redirectToPreferredLanguage =
  (path: string): RedirectFunction =>
  () =>
    `/${inject(LanguageBehaviour).preferred()}/${path}`;
