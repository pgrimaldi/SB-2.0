import { routes } from '../../app.routes';
import { SETTINGS_PAGES } from './settings-pages';

describe('Settings', () => {
  it('should have a route for every page of the menu and for its other screens', () => {
    const settings = routes
      .flatMap((route) => route.children ?? [])
      .find((route) => route.path === 'settings');

    expect(settings?.children?.map(({ path }) => path)).toEqual(
      SETTINGS_PAGES.flatMap(({ path, subPages = [] }) => [
        path,
        ...subPages.map((subPage) => subPage.path),
      ]),
    );
  });
});
