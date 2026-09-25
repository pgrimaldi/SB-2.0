import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { MatDialog } from '@angular/material/dialog';
import { provideTranslateService } from '@ngx-translate/core';
import { LoginDialog } from './login-dialog';

@Component({
  imports: [LoginDialog],
  template: `<app-login-dialog [(open)]="open" />`,
})
class LoginDialogHost {
  readonly open = signal(false);
}

describe('LoginDialog', () => {
  const setup = async (mobile: boolean) => {
    // jsdom has no matchMedia: provide one answering the phone breakpoint.
    window.matchMedia = vi.fn().mockReturnValue({
      matches: mobile,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    } as unknown as MediaQueryList);
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        // Closing waits for the exit animation, which jsdom never plays.
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const open = vi.spyOn(TestBed.inject(MatDialog), 'open');
    const fixture = TestBed.createComponent(LoginDialogHost);
    await fixture.whenStable();
    return { fixture, open };
  };

  afterEach(() => vi.restoreAllMocks());

  it('should open only when open becomes true, at 30% width on regular screens', async () => {
    const { fixture, open } = await setup(false);
    expect(open).not.toHaveBeenCalled();

    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    expect(open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ width: '30vw', height: '', maxWidth: 'none' }),
    );
  });

  it('should open at 90% width on phones', async () => {
    const { fixture, open } = await setup(true);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    expect(open).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ width: '90vw', height: '' }),
    );
  });

  it('should set open back to false when the popup is closed', async () => {
    const { fixture } = await setup(false);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();

    TestBed.inject(MatDialog).closeAll();
    await fixture.whenStable();

    expect(fixture.componentInstance.open()).toBe(false);
  });
});
