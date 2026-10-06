import { Routes } from '@angular/router';
import { authGuard } from './behaviours/auth/auth.guard';
import {
  languageActivateGuard,
  languageMatchGuard,
  redirectToPreferredLanguage,
} from './behaviours/i18n/language.guards';
import { unsavedChangesGuard } from './behaviours/forms/unsaved-changes.guard';
import { SETTINGS_PAGES } from './views/settings/settings-pages';

export const routes: Routes = [
  {
    path: ':lang',
    canMatch: [languageMatchGuard],
    canActivate: [languageActivateGuard],
    children: [
      {
        path: 'home',
        loadComponent: () => import('./views/hub/home/home').then((component) => component.Home),
      },
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'home',
      },
    ],
  },
  {
    path: 'home',
    pathMatch: 'full',
    redirectTo: redirectToPreferredLanguage('home'),
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: redirectToPreferredLanguage('home'),
  },
  // Private management area. After the redirects, so that "/" still goes to the home;
  // the session is checked on entering the layout and on every page change inside it.
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () => import('./views/layout/layout').then((component) => component.Layout),
    children: [
      {
        path: 'beachmap',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./views/beachmap/beachmap').then((component) => component.Beachmap),
      },
      {
        path: 'warehouse',
        canDeactivate: [unsavedChangesGuard],
        loadComponent: () =>
          import('./views/warehouse/warehouse').then((component) => component.Warehouse),
      },
      // Every menu page has its route already; a page not built yet (no `loadComponent` in
      // SETTINGS_PAGES) gets an empty one.
      {
        path: 'settings',
        loadComponent: () =>
          import('./views/settings/settings').then((component) => component.Settings),
        children: SETTINGS_PAGES.map(({ path, loadComponent }) =>
          loadComponent
            ? { path, loadComponent, canDeactivate: [unsavedChangesGuard] }
            : { path, children: [] },
        ),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./views/errors/not-found/not-found').then((component) => component.NotFound),
  },
];
