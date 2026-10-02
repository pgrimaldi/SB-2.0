import { Route } from '@angular/router';

/** A settings page: its path under `/settings`, the translation key of its name and, once built, its component. */
export interface SettingsPage {
  path: string;
  label: string;
  /** The page (`views/settings/<page>/`), loaded when opened; without it the route shows nothing. */
  loadComponent?: Route['loadComponent'];
}

/**
 * Pages of the settings, in menu order. The only list of them: the routes (app.routes.ts) and the
 * side panel both come from here.
 */
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
  { path: 'email', label: 'management.settings.menu.email' },
  { path: 'database', label: 'management.settings.menu.database' },
  { path: 'help', label: 'management.settings.menu.help' },
];
