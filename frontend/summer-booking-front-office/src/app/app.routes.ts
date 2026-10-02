import { Routes } from '@angular/router';
import { authGuard } from './behaviours/auth/auth.guard';
import {
  languageActivateGuard,
  languageMatchGuard,
  redirectToPreferredLanguage,
} from './behaviours/i18n/language.guards';
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
        loadComponent: () =>
          import('./views/beachmap/beachmap').then((component) => component.Beachmap),
      },
      {
        path: 'warehouse',
        loadComponent: () =>
          import('./views/warehouse/warehouse').then((component) => component.Warehouse),
      },
      // Settings: side panel and, next to it, the settings pages (views/settings/<page>). Every page
      // of the menu has its route already; a built page has its `loadComponent` in SETTINGS_PAGES.
      {
        path: 'settings',
        loadComponent: () =>
          import('./views/settings/settings').then((component) => component.Settings),
        children: SETTINGS_PAGES.map(({ path, loadComponent }) =>
          loadComponent ? { path, loadComponent } : { path, children: [] },
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
