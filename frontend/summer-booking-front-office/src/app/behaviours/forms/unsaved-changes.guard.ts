import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { AuthBehaviour } from '../auth/auth.behaviour';
import { UnsavedChangesBehaviour } from './unsaved-changes.behaviour';

/**
 * On every page of the management area (app.routes.ts): leaving it with unsaved changes asks first.
 * Not when the session has ended: then the page must go, with its data.
 */
export const unsavedChangesGuard: CanDeactivateFn<unknown> = () =>
  !inject(AuthBehaviour).isAuthenticated() || inject(UnsavedChangesBehaviour).confirmLeave();
