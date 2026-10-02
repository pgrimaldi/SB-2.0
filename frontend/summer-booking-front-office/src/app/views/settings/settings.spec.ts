import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { Settings } from './settings';
import { SETTINGS_PAGES } from './settings-pages';

describe('Settings', () => {
  it('should show the side panel with every settings page and, next to it, the page chosen', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService()],
    });
    const fixture = TestBed.createComponent(Settings);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    const panel = element.querySelector('app-settings-menu');
    const content = element.querySelector('.settings__content');
    const links = [...element.querySelectorAll<HTMLAnchorElement>('.settings__menu__item')];
    expect(links.map((link) => link.getAttribute('href'))).toEqual(
      SETTINGS_PAGES.map(({ path }) => `/settings/${path}`),
    );
    expect(content?.querySelector('router-outlet')).not.toBeNull();
    expect(panel?.nextElementSibling).toBe(content);
  });

  it('should have a route for every page of the menu', () => {
    const settings = routes
      .flatMap((route) => route.children ?? [])
      .find((route) => route.path === 'settings');

    expect(settings?.children?.map(({ path }) => path)).toEqual(
      SETTINGS_PAGES.map(({ path }) => path),
    );
  });
});
