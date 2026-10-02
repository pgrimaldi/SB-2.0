/**
 * Pages of the settings, in menu order: `path` under `/settings` and the translation key of the name.
 * The only list of them: the routes (app.routes.ts) and the side panel both come from here.
 */
export const SETTINGS_PAGES = [
  { path: 'property', label: 'management.settings.menu.property' },
  { path: 'users', label: 'management.settings.menu.users' },
  { path: 'seasons', label: 'management.settings.menu.seasons' },
  { path: 'sectors', label: 'management.settings.menu.sectors' },
  { path: 'map', label: 'management.settings.menu.map' },
  { path: 'warehouse', label: 'management.settings.menu.warehouse' },
  { path: 'price-list', label: 'management.settings.menu.price_list' },
  { path: 'services', label: 'management.settings.menu.services' },
  { path: 'discounts', label: 'management.settings.menu.discounts' },
  { path: 'sharing', label: 'management.settings.menu.sharing' },
  { path: 'online-bookings', label: 'management.settings.menu.online_bookings' },
  { path: 'fiscalization', label: 'management.settings.menu.fiscalization' },
  { path: 'email', label: 'management.settings.menu.email' },
  { path: 'database', label: 'management.settings.menu.database' },
  { path: 'help', label: 'management.settings.menu.help' },
] as const;
