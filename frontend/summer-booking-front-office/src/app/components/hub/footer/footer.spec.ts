import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { HubFooter } from './footer';

describe('HubFooter', () => {
  it('should render the links, the social buttons and the current year', async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideTranslateService({ fallbackLang: 'it' })],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      hub: { footer: { legal: { company: '©{{year}} Qbitsoft Srl' } } },
    });
    translate.use('it');

    const fixture = TestBed.createComponent(HubFooter);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelectorAll('.hub__footer__link').length).toBe(8);
    expect(element.querySelectorAll('.hub__footer__social__link').length).toBe(4);
    expect(element.querySelectorAll('.hub__footer__bottom p')[1].textContent?.trim()).toBe(
      `©${new Date().getFullYear()} Qbitsoft Srl`,
    );
  });
});
