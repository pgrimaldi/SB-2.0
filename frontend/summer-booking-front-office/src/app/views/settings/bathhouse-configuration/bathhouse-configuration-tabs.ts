import { Type } from '@angular/core';
import { BathhouseGeneralSettings } from './bathhouse-general-settings/bathhouse-general-settings';
import { BathhouseServicesSettings } from './bathhouse-services-settings/bathhouse-services-settings';
import { BathhouseMediaSettings } from './bathhouse-media-settings/bathhouse-media-settings';

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
    component: BathhouseGeneralSettings,
  },
  {
    path: '2',
    label: 'management.settings.bathhouse_configuration.tabs.services',
    widthPercent: 33,
    component: BathhouseServicesSettings,
  },
  {
    path: '3',
    label: 'management.settings.bathhouse_configuration.tabs.media',
    widthPercent: 33,
    component: BathhouseMediaSettings,
  },
];
