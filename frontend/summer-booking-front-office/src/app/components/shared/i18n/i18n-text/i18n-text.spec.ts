import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { I18nText } from './i18n-text';

@Component({
  imports: [I18nText],
  template: `<h1 appI18nText="hero.title"></h1>`,
})
class I18nTextHost {}

describe('I18nText', () => {
  it('should show the current translation and reserve the other one', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService({ fallbackLang: 'it' })],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', { hero: { title: 'Visione, visibilità, controllo.' } });
    translate.setTranslation('en', { hero: { title: 'Vision, visibility, control.' } });
    translate.use('it');

    const fixture = TestBed.createComponent(I18nTextHost);
    await fixture.whenStable();
    const heading: HTMLElement = fixture.nativeElement.querySelector('h1');

    expect(heading.textContent?.trim()).toBe('Visione, visibilità, controllo.');
    expect(heading.getAttribute('data-i18n-reserve-1')).toBe('Vision, visibility, control.');
    expect(heading.hasAttribute('data-i18n-reserve-2')).toBe(false);
  });
});
