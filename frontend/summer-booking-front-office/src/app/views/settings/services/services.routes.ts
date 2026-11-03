import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../../behaviours/forms/unsaved-changes.guard';
import { Services } from './services';
import { SERVICES_TABS } from './services-tabs';

export const SERVICES_ROUTES: Routes = [
  {
    path: '',
    component: Services,
    children: [
      { path: '', pathMatch: 'full', redirectTo: '1' },
      // The guard goes on each tab, where the panels are: on the page too, leaving would ask twice.
      ...SERVICES_TABS.map(({ path, component }) => ({
        path,
        component,
        canDeactivate: [unsavedChangesGuard],
      })),
      { path: '**', redirectTo: '1' },
    ],
  },
];
