import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonPeriodItem } from '../../../entities/settings/seasons/season-period-item';
import { SeasonService } from './season.service';

describe('SeasonService', () => {
  it('should read the season days of the token as days at midnight UTC', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    let dates: SeasonDates | undefined;

    seasons.seasonDates().subscribe((answer) => (dates = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/catalog/season`);
    expect(call.request.method).toBe('GET');
    call.flush({
      startsOn: '2026-05-01',
      endsOn: '2026-09-30',
      nextStartsOn: null,
      nextEndsOn: null,
    });
    expect(dates).toEqual({
      startsOn: '2026-05-01T00:00:00.000Z',
      endsOn: '2026-09-30T00:00:00.000Z',
      nextStartsOn: null,
      nextEndsOn: null,
    });
    controller.verify();
  });

  it('should read a season not set yet (204) as four empty days', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    let dates: SeasonDates | undefined;

    seasons.seasonDates().subscribe((answer) => (dates = answer));

    controller
      .expectOne(`${environment.apiBaseUrl}/catalog/season`)
      .flush(null, { status: 204, statusText: 'No Content' });
    expect(dates).toEqual(new SeasonDates());
    controller.verify();
  });

  it('should send all the season days, nothing else, and read the days as saved', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const dates = {
      startsOn: '2026-05-01T00:00:00.000Z',
      endsOn: '2026-09-30T00:00:00.000Z',
      nextStartsOn: null,
      nextEndsOn: null,
      extra: true,
    };

    let saved: SeasonDates | undefined;

    seasons.saveSeasonDates(dates).subscribe((answer) => (saved = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/catalog/season`);
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({
      startsOn: '2026-05-01',
      endsOn: '2026-09-30',
      nextStartsOn: null,
      nextEndsOn: null,
    });
    call.flush({
      startsOn: '2026-05-01',
      endsOn: '2026-09-30',
      nextStartsOn: null,
      nextEndsOn: null,
    });
    expect(saved).toEqual({
      startsOn: '2026-05-01T00:00:00.000Z',
      endsOn: '2026-09-30T00:00:00.000Z',
      nextStartsOn: null,
      nextEndsOn: null,
    });
    controller.verify();
  });

  it('should read the periods of the current season as days at midnight UTC, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    let periods: SeasonPeriodItem[] | undefined;

    seasons.periodList(false).subscribe((answer) => (periods = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/catalog/tariff-periods`);
    expect(call.request.method).toBe('GET');
    call.flush([
      {
        key: 0,
        name: 'Bassa stagione',
        ranges: [{ startsOn: '2026-05-01', endsOn: '2026-06-15', extra: 1 }],
        extra: true,
      },
    ]);
    expect(periods).toEqual([
      {
        key: 0,
        name: 'Bassa stagione',
        ranges: [{ startsOn: '2026-05-01T00:00:00.000Z', endsOn: '2026-06-15T00:00:00.000Z' }],
      },
    ]);
    controller.verify();
  });

  it('should ask the periods of the next season on its own route', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.periodList(true).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/next-season/tariff-periods`,
    ).request;
    expect(request.method).toBe('GET');
    controller.verify();
  });

  it('should create the period of the next season with its name and date ranges as days, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const period = {
      ...new SeasonPeriodAddRequest(),
      name: 'Bassa stagione',
      ranges: [
        { startsOn: '2027-05-01T00:00:00.000Z', endsOn: '2027-06-15T00:00:00.000Z', extra: 1 },
      ],
      extra: true,
    };
    let created: SeasonPeriodItem | undefined;

    seasons.addPeriod(true, period).subscribe((answer) => (created = answer));

    const call = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/next-season/tariff-periods`,
    );
    expect(call.request.method).toBe('POST');
    expect(call.request.body).toEqual({
      name: 'Bassa stagione',
      ranges: [{ startsOn: '2027-05-01', endsOn: '2027-06-15' }],
    });
    call.flush(
      {
        key: 2,
        name: 'Bassa stagione',
        ranges: [{ startsOn: '2027-05-01', endsOn: '2027-06-15' }],
      },
      { status: 201, statusText: 'Created' },
    );
    expect(created).toEqual({
      key: 2,
      name: 'Bassa stagione',
      ranges: [{ startsOn: '2027-05-01T00:00:00.000Z', endsOn: '2027-06-15T00:00:00.000Z' }],
    });
    controller.verify();
  });

  it('should delete the period of the next season by its key in the route, without a body', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.deletePeriod(true, 0).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/next-season/tariff-periods/0`,
    ).request;
    expect(request.method).toBe('DELETE');
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should save the name and all the date ranges of the period as days, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const period = {
      key: 0,
      name: 'Pre stagione',
      ranges: [
        { startsOn: '2026-05-01T00:00:00.000Z', endsOn: '2026-06-15T00:00:00.000Z', extra: 1 },
        { startsOn: '2026-09-01T00:00:00.000Z', endsOn: '2026-09-30T00:00:00.000Z' },
      ],
      extra: true,
    };
    let saved: SeasonPeriodItem | undefined;

    seasons.updatePeriod(false, 0, period).subscribe((answer) => (saved = answer));

    const call = controller.expectOne(`${environment.apiBaseUrl}/catalog/tariff-periods/0`);
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({
      name: 'Pre stagione',
      ranges: [
        { startsOn: '2026-05-01', endsOn: '2026-06-15' },
        { startsOn: '2026-09-01', endsOn: '2026-09-30' },
      ],
    });
    call.flush({
      key: 0,
      name: 'Pre stagione',
      ranges: [
        { startsOn: '2026-05-01', endsOn: '2026-06-15' },
        { startsOn: '2026-09-01', endsOn: '2026-09-30' },
      ],
    });
    expect(saved).toEqual({
      key: 0,
      name: 'Pre stagione',
      ranges: [
        { startsOn: '2026-05-01T00:00:00.000Z', endsOn: '2026-06-15T00:00:00.000Z' },
        { startsOn: '2026-09-01T00:00:00.000Z', endsOn: '2026-09-30T00:00:00.000Z' },
      ],
    });
    controller.verify();
  });

  it('should send the date ranges to delete with the key of their period, as days, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const ranges = [
      {
        key: 0,
        startsOn: '2027-05-01T00:00:00.000Z',
        endsOn: '2027-06-30T00:00:00.000Z',
        extra: 1,
      },
      { key: 1, startsOn: '2027-07-01T00:00:00.000Z', endsOn: '2027-07-15T00:00:00.000Z' },
    ];

    seasons.deleteRanges(true, ranges).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/catalog/next-season/tariff-periods/bulk-delete-ranges`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      ranges: [
        { key: 0, startsOn: '2027-05-01', endsOn: '2027-06-30' },
        { key: 1, startsOn: '2027-07-01', endsOn: '2027-07-15' },
      ],
    });
    controller.verify();
  });
});
