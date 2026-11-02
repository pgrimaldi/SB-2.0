import { Routes } from '@angular/router';
import { Services } from './services';
import { SERVICES_TABS } from './services-tabs';

export const SERVICES_ROUTES: Routes = [
  {
    path: '',
    component: Services,
    children: [
      { path: '', pathMatch: 'full', redirectTo: '1' },
      ...SERVICES_TABS.map(({ path, component }) => ({ path, component })),
      { path: '**', redirectTo: '1' },
    ],
  },
];
