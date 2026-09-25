import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LanguageBehaviour } from '../i18n/language.behaviour';
import { AuthBehaviour } from './auth.behaviour';

// Sign-in happens in the popup opened from the header, so without a session the user goes back to the home.
export const authGuard: CanActivateFn = () => {
  if (inject(AuthBehaviour).isAuthenticated()) {
    return true;
  }

  return inject(Router).parseUrl(`/${inject(LanguageBehaviour).preferred()}/home`);
};
