import { HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
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
      error: new Blob([JSON.stringify({ status: 403, code: 'operation.not_allowed' })]),
    });

    expect(await problemOf(error)).toEqual({ status: 403, code: 'operation.not_allowed' });
  });

  it('should treat a body that is not JSON as an answer in another format', async () => {
    const error = new HttpErrorResponse({ status: 500, error: new Blob(['<html>']) });

    expect((await problemOf(error)).code).not.toBe('operation.not_allowed');
    expect((await problemOf(error)).status).toBe(500);
  });
});
