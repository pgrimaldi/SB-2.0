import { Route } from '@angular/router';

export interface SettingsPage {
  path: string;
  /** Translation key of the page name. */
  label: string;
  /** Set once the page is built; without it the route shows nothing. */
  loadComponent?: Route['loadComponent'];
}

/** In menu order. The single source of both the routes (app.routes.ts) and the side panel. */
export const SETTINGS_PAGES: readonly SettingsPage[] = [
  { path: 'property', label: 'management.settings.menu.property' },
  { path: 'users', label: 'management.settings.menu.users' },
  { path: 'seasons', label: 'management.settings.menu.seasons' },
  { path: 'sectors', label: 'management.settings.menu.sectors' },
  { path: 'map', label: 'management.settings.menu.map' },
  {
    path: 'warehouse-setting',
    label: 'management.settings.menu.warehouse',
    loadComponent: () =>
      import('./warehouse-setting/warehouse-setting').then(
        (component) => component.WarehouseSetting,
      ),
  },
  { path: 'price-list', label: 'management.settings.menu.price_list' },
  { path: 'services', label: 'management.settings.menu.services' },
  { path: 'discounts', label: 'management.settings.menu.discounts' },
  { path: 'sharing', label: 'management.settings.menu.sharing' },
  { path: 'online-bookings', label: 'management.settings.menu.online_bookings' },
  { path: 'fiscalization', label: 'management.settings.menu.fiscalization' },
  {
    path: 'email-configuration',
    label: 'management.settings.menu.email',
    loadComponent: () =>
      import('./email-configuration/email-configuration').then(
        (component) => component.EmailConfiguration,
      ),
  },
  { path: 'database', label: 'management.settings.menu.database' },
  {
    path: 'contact-support',
    label: 'management.settings.menu.help',
    loadComponent: () =>
      import('./contact-support/contact-support').then((component) => component.ContactSupport),
  },
];
