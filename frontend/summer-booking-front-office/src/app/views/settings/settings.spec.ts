import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { Settings } from './settings';

describe('Settings', () => {
  it('should show the side panel and, next to it, the place of the settings page', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService()],
    });
    const fixture = TestBed.createComponent(Settings);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    const panel = element.querySelector('app-settings-menu');
    const content = element.querySelector('.settings__content');
    expect(panel?.querySelector('aside nav')).not.toBeNull();
    expect(content?.querySelector('router-outlet')).not.toBeNull();
    expect(panel?.nextElementSibling).toBe(content);
  });
});
