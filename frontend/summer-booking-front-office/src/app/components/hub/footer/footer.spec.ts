import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Footer } from './footer';

describe('Footer', () => {
  it('should render the links, the social buttons and the current year', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService({ fallbackLang: 'it' })],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      hub: { footer: { legal: { company: '©{{year}} Qbitsoft Srl' } } },
    });
    translate.use('it');

    const fixture = TestBed.createComponent(Footer);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('.footer__link').length).toBe(8);
    expect(element.querySelectorAll('.footer__social__link').length).toBe(4);
    expect(element.querySelectorAll('.footer__bottom p')[1].textContent?.trim()).toBe(
      `©${new Date().getFullYear()} Qbitsoft Srl`,
    );
  });
});
