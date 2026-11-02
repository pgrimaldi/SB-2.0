import { Type } from '@angular/core';
import { ServicesBar } from './services-bar/services-bar';
import { ServicesGeneral } from './services-general/services-general';
import { ServicesHourly } from './services-hourly/services-hourly';
import { AutomaticPrices } from './automatic-prices/automatic-prices';
import { TableBooking } from './table-booking/table-booking';

export interface ServicesTab {
  path: string;
  /** Translation key of the tab name. */
  label: string;
  widthPercent: number;
  component: Type<unknown>;
}

/** In tab order. The single source of both the routes and the tabs of the page. */
export const SERVICES_TABS: readonly ServicesTab[] = [
  {
    path: '1',
    label: 'management.settings.services.tabs.bar_services',
    widthPercent: 20,
    component: ServicesBar,
  },
  {
    path: '2',
    label: 'management.settings.services.tabs.services',
    widthPercent: 20,
    component: ServicesGeneral,
  },
  {
    path: '3',
    label: 'management.settings.services.tabs.hourly_services',
    widthPercent: 20,
    component: ServicesHourly,
  },
  {
    path: '4',
    label: 'management.settings.services.tabs.automatic_prices',
    widthPercent: 20,
    component: AutomaticPrices,
  },
  {
    path: '5',
    label: 'management.settings.services.tabs.table_booking',
    widthPercent: 20,
    component: TableBooking,
  },
];
