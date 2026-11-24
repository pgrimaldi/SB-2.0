import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { ApiErrorCode } from '../../entities/errors/api-error-codes';
import { ErrorTextBehaviour } from '../errors/error-text.behaviour';
import { DropdownList } from './dropdown-list';

describe('DropdownList', () => {
  const forbidden = () =>
    throwError(
      () =>
        new HttpErrorResponse({
          status: 403,
          error: { status: 403, code: ApiErrorCode.AuthForbidden, title: 'Forbidden' },
        }),
    );

  const setup = () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', { error: { auth: { forbidden: 'Operazione non permessa.' } } });
    translate.use('it');
    const read = vi.fn<() => Observable<readonly string[]>>(() => of(['Lettino', 'Ombrellone']));
    const list = new DropdownList(read, TestBed.inject(ErrorTextBehaviour));
    return { read, list };
  };

  it('should give the options read, without an error', () => {
    const { read, list } = setup();

    list.load();

    expect(read).toHaveBeenCalledTimes(1);
    expect(list.items()).toEqual(['Lettino', 'Ombrellone']);
    expect(list.loadError()).toBeNull();
  });

  it('should give no options and the text of the code when the read fails', () => {
    const { read, list } = setup();
    list.load();
    read.mockReturnValueOnce(forbidden());

    list.load();

    expect(list.items()).toEqual([]);
    expect(list.loadError()).toBe('Operazione non permessa.');
  });

  it('should read again on retry after a failed read, and drop the error once read', () => {
    const { read, list } = setup();
    read.mockReturnValueOnce(forbidden());
    list.load();

    list.retry();

    expect(read).toHaveBeenCalledTimes(2);
    expect(list.items()).toEqual(['Lettino', 'Ombrellone']);
    expect(list.loadError()).toBeNull();
  });

  it('should not read again on retry when the options are already read', () => {
    const { read, list } = setup();
    list.load();

    list.retry();

    expect(read).toHaveBeenCalledTimes(1);
  });
});
