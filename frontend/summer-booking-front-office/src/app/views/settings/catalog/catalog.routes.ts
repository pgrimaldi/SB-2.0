import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../../behaviours/forms/unsaved-changes.guard';
import { Catalog } from './catalog';
import { CATALOG_TABS } from './catalog-tabs';

export const CATALOG_ROUTES: Routes = [
  {
    path: '',
    component: Catalog,
    children: [
      { path: '', pathMatch: 'full', redirectTo: '1' },
      ...CATALOG_TABS.map(({ path, component }) => ({
        path,
        component,
        canDeactivate: [unsavedChangesGuard],
      })),
      { path: '**', redirectTo: '1' },
    ],
  },
];
