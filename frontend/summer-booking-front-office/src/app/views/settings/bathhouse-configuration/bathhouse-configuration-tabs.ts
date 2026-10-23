import { Type } from '@angular/core';
import { BathhouseConfiguration1 } from './bathhouse-configuration-1/bathhouse-configuration-1';
import { BathhouseConfiguration2 } from './bathhouse-configuration-2/bathhouse-configuration-2';
import { BathhouseConfiguration3 } from './bathhouse-configuration-3/bathhouse-configuration-3';

export interface BathhouseConfigurationTab {
  path: string;
  /** Translation key of the tab name. */
  label: string;
  widthPercent: number;
  component: Type<unknown>;
}

/** In tab order. The single source of both the routes and the tabs of the page. */
export const BATHHOUSE_CONFIGURATION_TABS: readonly BathhouseConfigurationTab[] = [
  {
    path: '1',
    label: 'management.settings.bathhouse_configuration.tabs.general_information',
    widthPercent: 34,
    component: BathhouseConfiguration1,
  },
  {
    path: '2',
    label: 'management.settings.bathhouse_configuration.tabs.services',
    widthPercent: 33,
    component: BathhouseConfiguration2,
  },
  {
    path: '3',
    label: 'management.settings.bathhouse_configuration.tabs.media',
    widthPercent: 33,
    component: BathhouseConfiguration3,
  },
];
