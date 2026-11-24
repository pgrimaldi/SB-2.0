import { Type } from '@angular/core';
import { AdditionalItems } from './additional-items/additional-items';
import { Packages } from './packages/packages';

export interface CatalogTab {
  path: string;
  /** Translation key of the tab name. */
  label: string;
  widthPercent: number;
  component: Type<unknown>;
}

/** In tab order. The single source of both the routes and the tabs of the page. */
export const CATALOG_TABS: readonly CatalogTab[] = [
  {
    path: '1',
    label: 'management.settings.catalog.tabs.packages',
    widthPercent: 50,
    component: Packages,
  },
  {
    path: '2',
    label: 'management.settings.catalog.tabs.additional_items',
    widthPercent: 50,
    component: AdditionalItems,
  },
];
