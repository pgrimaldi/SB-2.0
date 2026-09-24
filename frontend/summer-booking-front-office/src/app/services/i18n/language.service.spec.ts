import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { LanguageService } from './language.service';

const STORAGE_KEY = 'sb.language';

describe('LanguageService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideTranslateService({ fallbackLang: 'it' })],
    });
  });

  afterEach(() => localStorage.clear());

  it('should prefer Italian when nothing valid is stored', () => {
    const service = TestBed.inject(LanguageService);

    expect(service.preferred()).toBe('it');
    localStorage.setItem(STORAGE_KEY, 'fr');
    expect(service.preferred()).toBe('it');
    localStorage.setItem(STORAGE_KEY, 'en');
    expect(service.preferred()).toBe('en');
  });

  it('should redirect the root to the preferred language', async () => {
    localStorage.setItem(STORAGE_KEY, 'en');
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/');

    expect(TestBed.inject(Router).url).toBe('/en/home');
    expect(TestBed.inject(LanguageService).current()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('should remember the choice and move the URL to the new language', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/it/home');

    TestBed.inject(LanguageService).switchTo('en');
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/en/home');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en');
  });

  it('should not match unsupported languages', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/fr/home');

    expect(harness.routeNativeElement?.querySelector('.not__found')).toBeTruthy();
  });
});
