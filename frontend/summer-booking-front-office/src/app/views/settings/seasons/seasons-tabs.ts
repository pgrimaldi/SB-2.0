export interface SeasonsTab {
  path: string;
  /** Translation key of the tab name. */
  label: string;
  widthPercent: number;
  isNextSeason: boolean;
}

/** In tab order. The single source of both the routes and the tabs of the page. */
export const SEASONS_TABS: readonly SeasonsTab[] = [
  {
    path: '1',
    label: 'management.settings.seasons.tabs.current_season',
    widthPercent: 50,
    isNextSeason: false,
  },
  {
    path: '2',
    label: 'management.settings.seasons.tabs.next_season',
    widthPercent: 50,
    isNextSeason: true,
  },
];
