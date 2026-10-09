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
  it('should reserve every other translation, including shorter texts, after a language change', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService({ fallbackLang: 'it' })],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', { hero: { title: 'Visione, visibilità, controllo.' } });
    translate.setTranslation('en', { hero: { title: 'Vision, visibility, control.' } });
    translate.setTranslation('fr', { hero: { title: 'Vision, visibilité, contrôle.' } });
    translate.setTranslation('es', { hero: { title: 'Visión, visibilidad, control.' } });
    translate.setTranslation('de', { hero: { title: 'Vision, Sichtbarkeit, Kontrolle.' } });
    translate.use('it');

    const fixture = TestBed.createComponent(I18nTextHost);
    await fixture.whenStable();
    const heading: HTMLElement = fixture.nativeElement.querySelector('h1');

    expect(heading.textContent?.trim()).toBe('Visione, visibilità, controllo.');
    const reserved = () => [...heading.querySelectorAll('.i18n__text__reserve')];
    expect(reserved().map((element) => element.getAttribute('data-i18n-reserve'))).toEqual([
      'Vision, visibility, control.',
      'Vision, visibilité, contrôle.',
      'Visión, visibilidad, control.',
      'Vision, Sichtbarkeit, Kontrolle.',
    ]);
    expect(reserved().every((element) => element.textContent === '')).toBe(true);

    translate.use('fr');
    await fixture.whenStable();

    expect(heading.textContent?.trim()).toBe('Vision, visibilité, contrôle.');
    expect(reserved().map((element) => element.getAttribute('data-i18n-reserve'))).toEqual([
      'Visione, visibilità, controllo.',
      'Vision, visibility, control.',
      'Visión, visibilidad, control.',
      'Vision, Sichtbarkeit, Kontrolle.',
    ]);
  });
});
