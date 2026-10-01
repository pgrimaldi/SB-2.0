import { HttpErrorResponse } from '@angular/common/http';
import { toApiProblem } from './to-api-problem';

/** An error answer of the server with this status and body. */
const answer = (status: number, error: unknown) =>
  new HttpErrorResponse({ status, error, url: '/api/booking/create' });

describe('toApiProblem', () => {
  it("should keep the backend's Problem Details, with the real status of the answer", () => {
    const problem = toApiProblem(
      answer(409, {
        type: 'https://errors.summerbooking/booking/umbrella-overlap',
        status: 400, // a wrong status in the body: the answer's one counts
        code: 'booking.umbrella_overlap',
        args: { umbrellaId: '42', date: '2026-08-01', row: 3 },
        traceId: '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
        title: 'Umbrella already booked',
      }),
    );

    expect(problem).toEqual({
      status: 409,
      code: 'booking.umbrella_overlap',
      args: { umbrellaId: '42', date: '2026-08-01', row: 3 },
      traceId: '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
      type: 'https://errors.summerbooking/booking/umbrella-overlap',
      title: 'Umbrella already booked',
    });
  });

  it('should keep the well-formed validation errors of the fields', () => {
    const problem = toApiProblem(
      answer(400, {
        code: 'validation.invalid_request',
        errors: [
          { field: 'datetimeTo', code: 'validation.end_before_start', args: {} },
          { field: 'name', code: 'validation.too_long', args: { max: 50 } },
          { field: '', code: 'validation.required' }, // no field
          { field: 'email', code: 'Not a code' }, // malformed code
          'datetimeFrom', // not an object
        ],
      }),
    );

    expect(problem.errors).toEqual([
      { field: 'datetimeTo', code: 'validation.end_before_start' },
      { field: 'name', code: 'validation.too_long', args: { max: 50 } },
    ]);
  });

  it('should give its own codes to errors that do not come from the backend', () => {
    expect(toApiProblem(answer(0, new ProgressEvent('error')))).toEqual({
      status: 0,
      code: 'network.unavailable',
    });
    for (const status of [502, 503, 504]) {
      expect(toApiProblem(answer(status, '<html>Bad Gateway</html>'))).toEqual({
        status,
        code: 'server.unavailable',
      });
    }
    expect(toApiProblem(answer(500, '<html>Error</html>'))).toEqual({
      status: 500,
      code: 'server.unexpected',
    });
    expect(toApiProblem(answer(404, null))).toEqual({ status: 404, code: 'server.unexpected' });
    expect(toApiProblem(new TypeError('x is undefined'))).toEqual({
      status: 0,
      code: 'client.unexpected',
    });
  });

  it('should treat a missing or malformed code as an answer in another format', () => {
    for (const code of [
      undefined,
      42,
      'auth',
      'Auth.Invalid',
      'auth.invalid credentials',
      '<img src=x onerror=alert(1)>',
      `auth.${'x'.repeat(100)}`,
    ]) {
      expect(toApiProblem(answer(401, { code, title: 'Unauthorized' }))).toEqual({
        status: 401,
        code: 'server.unexpected',
      });
    }
  });

  it('should drop args, trace id and texts that are not well formed', () => {
    const problem = toApiProblem(
      answer(422, {
        code: 'booking.out_of_season',
        args: {
          date: '2026-12-25',
          nested: { evil: true },
          huge: 'x'.repeat(201),
          notANumber: Number.NaN,
          'bad name': 'x',
        },
        traceId: 'not a <trace> id',
        title: 'x'.repeat(301),
        type: 42,
      }),
    );

    expect(problem).toEqual({
      status: 422,
      code: 'booking.out_of_season',
      args: { date: '2026-12-25' },
    });
    expect(toApiProblem(answer(422, { code: 'booking.out_of_season', args: { a: {} } }))).toEqual(
      { status: 422, code: 'booking.out_of_season' }, // no args left: none at all
    );
  });

  it('should keep at most 50 field errors', () => {
    const errors = Array.from({ length: 60 }, (_, index) => ({
      field: `field${index}`,
      code: 'validation.required',
    }));

    expect(
      toApiProblem(answer(400, { code: 'validation.invalid_request', errors })).errors,
    ).toHaveLength(50);
  });
});
