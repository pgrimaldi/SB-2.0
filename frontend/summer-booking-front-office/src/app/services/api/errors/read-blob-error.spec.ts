import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { FRONTEND_ERROR_CODES } from '../../../entities/errors/api-problem';
import { readBlobError } from './read-blob-error';
import { toApiProblem } from './to-api-problem';

describe('readBlobError', () => {
  const problemOf = async (error: unknown) => {
    try {
      await firstValueFrom(readBlobError(error));
    } catch (thrown: unknown) {
      return toApiProblem(thrown);
    }
    throw new Error('no error');
  };

  it('should give the code of a Problem Details sent as a file', async () => {
    const error = new HttpErrorResponse({
      status: 403,
      error: new Blob([JSON.stringify({ status: 403, code: ApiErrorCode.AuthForbidden })]),
    });

    expect(await problemOf(error)).toEqual({ status: 403, code: ApiErrorCode.AuthForbidden });
  });

  it('should treat a body that is not JSON as an answer in another format', async () => {
    const error = new HttpErrorResponse({ status: 500, error: new Blob(['<html>']) });

    expect(await problemOf(error)).toEqual({
      status: 500,
      code: FRONTEND_ERROR_CODES.unexpected,
    });
  });
});
