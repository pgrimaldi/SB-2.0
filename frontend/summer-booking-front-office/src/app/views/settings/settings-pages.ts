import { Route } from '@angular/router';

export interface SettingsPage {
  path: string;
  /** Translation key of the page name. */
  label: string;
  /** Set once the page is built; without it the route shows nothing. */
  loadComponent?: Route['loadComponent'];
  /** Other screens of the page, not in the menu (e.g. its trash); their paths are under the page's. */
  subPages?: readonly Route[];
  /** A page with routes of its own (e.g. its tabs, `bathhouse-configuration/2`), loaded with it. */
  loadChildren?: Route['loadChildren'];
}

/** In menu order. The single source of both the routes (app.routes.ts) and the side panel. */
export const SETTINGS_PAGES: readonly SettingsPage[] = [
  {
    path: 'bathhouse-configuration',
    label: 'management.settings.menu.property',
    loadChildren: () =>
      import('./bathhouse-configuration/bathhouse-configuration.routes').then(
        (routes) => routes.BATHHOUSE_CONFIGURATION_ROUTES,
      ),
  },
  {
    path: 'user-management',
    label: 'management.settings.menu.users',
    loadComponent: () =>
      import('./user-management/user-management').then((component) => component.UserManagement),
    subPages: [
      {
        path: 'user-management/logs',
        loadComponent: () =>
          import('./user-management/user-management-logs/user-management-logs').then(
            (component) => component.UserManagementLogs,
          ),
      },
    ],
  },
  {
    path: 'seasons',
    label: 'management.settings.menu.seasons',
    loadChildren: () => import('./seasons/seasons.routes').then((routes) => routes.SEASONS_ROUTES),
  },
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
  {
    path: 'vouchers',
    label: 'management.settings.menu.discounts',
    loadComponent: () => import('./vouchers/vouchers').then((component) => component.Vouchers),
  },
  {
    path: 'sharing',
    label: 'management.settings.menu.sharing',
    loadComponent: () => import('./sharing/sharing').then((component) => component.Sharing),
  },
  {
    path: 'online-booking-configuration',
    label: 'management.settings.menu.online_bookings',
    loadComponent: () =>
      import('./online-booking-configuration/online-booking-configuration').then(
        (component) => component.OnlineBookingConfiguration,
      ),
  },
  {
    path: 'taxation',
    label: 'management.settings.menu.fiscalization',
    loadComponent: () => import('./taxation/taxation').then((component) => component.Taxation),
  },
  {
    path: 'email-configuration',
    label: 'management.settings.menu.email',
    loadComponent: () =>
      import('./email-configuration/email-configuration').then(
        (component) => component.EmailConfiguration,
      ),
  },
  {
    path: 'database',
    label: 'management.settings.menu.database',
    loadComponent: () => import('./database/database').then((component) => component.Database),
    subPages: [
      {
        path: 'database/trash',
        loadComponent: () => import('./database/database').then((component) => component.Database),
        data: { isTrash: true },
      },
    ],
  },
  {
    path: 'contact-support',
    label: 'management.settings.menu.help',
    loadComponent: () =>
      import('./contact-support/contact-support').then((component) => component.ContactSupport),
  },
];
