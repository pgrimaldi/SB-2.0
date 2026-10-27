import { Routes } from '@angular/router';
import { unsavedChangesGuard } from '../../../behaviours/forms/unsaved-changes.guard';
import { SeasonPeriods } from './season-periods/season-periods';
import { Seasons } from './seasons';
import { SEASONS_TABS } from './seasons-tabs';

export const SEASONS_ROUTES: Routes = [
  {
    path: '',
    component: Seasons,
    // On the page, not on the tabs: the season dates stay on screen when the tab changes.
    canDeactivate: [unsavedChangesGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: '1' },
      ...SEASONS_TABS.map(({ path, isNextSeason }) => ({
        path,
        component: SeasonPeriods,
        data: { isNextSeason },
      })),
      { path: '**', redirectTo: '1' },
    ],
  },
];
