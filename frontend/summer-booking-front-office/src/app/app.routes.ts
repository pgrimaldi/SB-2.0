import { Routes } from '@angular/router';
import { authGuard } from './services/auth/auth.guard';
import {
  languageActivateGuard,
  languageMatchGuard,
  redirectToPreferredLanguage,
} from './services/i18n/language.guards';

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
    path: 'login',
    loadComponent: () =>
      import('./views/login/login-view.component').then(
        (component) => component.LoginViewComponent,
      ),
  },
  {
    path: 'bookings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./views/bookings/bookings-view.component').then(
        (component) => component.BookingsViewComponent,
      ),
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
  {
    path: '**',
    loadComponent: () =>
      import('./views/errors/not-found/not-found').then((component) => component.NotFound),
  },
];
