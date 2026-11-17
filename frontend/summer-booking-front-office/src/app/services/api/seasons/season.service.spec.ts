import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SeasonDates } from '../../../entities/settings/seasons/season-dates';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
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

  it('should ask the periods of the chosen season of the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.periodList(true).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/period-list?isNextSeason=true`,
    ).request;
    expect(request.method).toBe('GET');
    controller.verify();
  });

  it('should send the name, the date intervals and the season of the new period, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const period = {
      ...new SeasonPeriodAddRequest(),
      periodName: 'Bassa stagione',
      intervals: [
        { dateFrom: '2026-05-01T00:00:00.000Z', dateTo: '2026-06-15T00:00:00.000Z', extra: 1 },
      ],
      extra: true,
    };

    seasons.addPeriod(false, period).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/add-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      periodName: 'Bassa stagione',
      intervals: [{ dateFrom: '2026-05-01T00:00:00.000Z', dateTo: '2026-06-15T00:00:00.000Z' }],
      isNextSeason: false,
    });
    controller.verify();
  });

  it('should send the period to delete', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.deletePeriod('d1').subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/delete-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idPeriod: 'd1' });
    controller.verify();
  });

  it('should send the date intervals to delete', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.deleteIntervals(['i1', 'i2']).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/delete-intervals`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idIntervals: ['i1', 'i2'] });
    controller.verify();
  });

  it('should send the new days of the date interval, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const interval = {
      idInterval: 'i1',
      dateFrom: '2026-05-02T00:00:00.000Z',
      dateTo: '2026-06-14T00:00:00.000Z',
      extra: true,
    };

    seasons.updateInterval(interval).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/update-interval`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      idInterval: 'i1',
      dateFrom: '2026-05-02T00:00:00.000Z',
      dateTo: '2026-06-14T00:00:00.000Z',
    });
    controller.verify();
  });

  it('should send the new name of the period, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    const period = { idPeriod: 'd1', periodName: 'Pre stagione', extra: 1 };

    seasons.updatePeriod(period).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/update-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idPeriod: 'd1', periodName: 'Pre stagione' });
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

  it('should send the new date interval and its period, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const interval = {
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-06-10T00:00:00.000Z',
      extra: true,
    };

    seasons.addInterval('d1', interval).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/add-interval`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-06-10T00:00:00.000Z',
      idPeriod: 'd1',
    });
    controller.verify();
  });
});
