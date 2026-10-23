import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../../behaviours/forms/unsaved-changes.guard';
import { BathhouseConfiguration } from './bathhouse-configuration';
import { BATHHOUSE_CONFIGURATION_TABS } from './bathhouse-configuration-tabs';

export const BATHHOUSE_CONFIGURATION_ROUTES: Routes = [
  {
    path: '',
    component: BathhouseConfiguration,
    children: [
      { path: '', pathMatch: 'full', redirectTo: '1' },
      // The guard goes on each tab, where the forms are: on the page too, leaving would ask twice.
      ...BATHHOUSE_CONFIGURATION_TABS.map(({ path, component }) => ({
        path,
        component,
        canDeactivate: [unsavedChangesGuard],
      })),
      { path: '**', redirectTo: '1' },
    ],
  },
];
