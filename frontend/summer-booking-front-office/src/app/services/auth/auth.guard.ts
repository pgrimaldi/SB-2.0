import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LanguageService } from '../i18n/language.service';
import { SessionService } from './session.service';

// Sign-in happens in the popup opened from the header, so without a session the user goes back to the home.
export const authGuard: CanActivateFn = () => {
  if (inject(SessionService).isAuthenticated()) {
    return true;
  }

  return inject(Router).parseUrl(`/${inject(LanguageService).preferred()}/home`);
};
