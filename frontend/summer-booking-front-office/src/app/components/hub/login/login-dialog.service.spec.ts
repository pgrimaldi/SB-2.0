import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideTranslateService } from '@ngx-translate/core';
import { LoginDialog } from './login-dialog';
import { LoginDialogService } from './login-dialog.service';

describe('LoginDialogService', () => {
  const openWith = (mobile: boolean) => {
    // jsdom has no matchMedia: provide one answering the phone breakpoint.
    window.matchMedia = vi.fn().mockReturnValue({
      matches: mobile,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    } as unknown as MediaQueryList);
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const open = vi.spyOn(TestBed.inject(MatDialog), 'open');
    TestBed.inject(LoginDialogService).open();
    return open;
  };

  afterEach(() => vi.restoreAllMocks());

  it('should open at 30% width and 40% height on regular screens', () => {
    expect(openWith(false)).toHaveBeenCalledWith(
      LoginDialog,
      expect.objectContaining({ width: '30vw', height: '40vh', maxWidth: 'none' }),
    );
  });

  it('should open at 90% width and 55% height on phones', () => {
    expect(openWith(true)).toHaveBeenCalledWith(
      LoginDialog,
      expect.objectContaining({ width: '90vw', height: '55vh' }),
    );
  });
});
