import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { SettingsLink, SettingsMenu } from './settings-menu';

@Component({ template: '' })
class EmptyPage {}

@Component({
  imports: [SettingsMenu],
  template: `<app-settings-menu [links]="links" />`,
})
class SettingsMenuHost {
  readonly links: SettingsLink[] = [
    { route: '/settings/users', label: 'Gestione utenti' },
    { route: '/settings/sharing', label: 'Sharing' },
  ];
}

describe('SettingsMenu', () => {
  it('should list the pages it is given and highlight the current one', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'settings/:page', component: EmptyPage }]),
        provideTranslateService(),
      ],
    });
    const fixture = TestBed.createComponent(SettingsMenuHost);
    await TestBed.inject(Router).navigateByUrl('/settings/sharing');
    await fixture.whenStable();
    const links = [
      ...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>(
        '.settings__menu__item',
      ),
    ];

    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/settings/users',
      '/settings/sharing',
    ]);
    expect(links.map((link) => link.classList.contains('settings__menu__item__active'))).toEqual([
      false,
      true,
    ]);
    expect(links[1].getAttribute('aria-current')).toBe('page');
  });
});
