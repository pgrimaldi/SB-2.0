import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { routes } from '../../app.routes';
import { AuthBehaviour } from './auth.behaviour';

describe('authGuard', () => {
  const signIn = () =>
    TestBed.inject(AuthBehaviour).start(
      { token: 'token', user: { email: 'u@e.it', idProperty: 'property-1' } },
      false,
    );

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter(routes), provideTranslateService({ fallbackLang: 'it' })],
    });
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('should send visitors without a session back to the home', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/beachmap');

    expect(TestBed.inject(Router).url).toBe('/it/home');
  });

  it('should open the management area with a session, while "/" still goes to the home', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/beachmap');
    expect(TestBed.inject(Router).url).toBe('/beachmap');

    await harness.navigateByUrl('/');
    expect(TestBed.inject(Router).url).toBe('/it/home');
  });

  it('should close the management area after logout, also going back to a private page', async () => {
    signIn();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/beachmap');

    TestBed.inject(AuthBehaviour).logout();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/it/home');

    await harness.navigateByUrl('/beachmap');
    expect(TestBed.inject(Router).url).toBe('/it/home');
  });
});
