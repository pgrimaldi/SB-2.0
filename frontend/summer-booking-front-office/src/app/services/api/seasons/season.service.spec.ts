import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { SeasonPeriodAddRequest } from '../../../entities/settings/seasons/season-period-add-request';
import { SeasonService } from './season.service';

describe('SeasonService', () => {
  it('should ask the season dates of the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.seasonDates('p1').subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/season-dates?idProperty=p1`,
    ).request;
    expect(request.method).toBe('GET');
    controller.verify();
  });

  it('should ask the periods of the chosen season of the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.periodList('p1', true).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/period-list?idProperty=p1&isNextSeason=true`,
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

    seasons.addPeriod('p1', false, period).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/add-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      periodName: 'Bassa stagione',
      intervals: [{ dateFrom: '2026-05-01T00:00:00.000Z', dateTo: '2026-06-15T00:00:00.000Z' }],
      idProperty: 'p1',
      isNextSeason: false,
    });
    controller.verify();
  });

  it('should send the property and the period to delete', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.deletePeriod({ idProperty: 'p1', idPeriod: 'd1' }).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/delete-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idProperty: 'p1', idPeriod: 'd1' });
    controller.verify();
  });

  it('should send the property and the date intervals to delete', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);

    seasons.deleteIntervals({ idProperty: 'p1', idIntervals: ['i1', 'i2'] }).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/delete-intervals`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idProperty: 'p1', idIntervals: ['i1', 'i2'] });
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

    seasons.updateInterval('p1', interval).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/update-interval`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      idInterval: 'i1',
      dateFrom: '2026-05-02T00:00:00.000Z',
      dateTo: '2026-06-14T00:00:00.000Z',
      idProperty: 'p1',
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

    seasons.updatePeriod('p1', period).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/update-period`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({ idPeriod: 'd1', periodName: 'Pre stagione', idProperty: 'p1' });
    controller.verify();
  });

  it('should send all the season dates, also the empty ones, nothing else', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const seasons = TestBed.inject(SeasonService);
    const controller = TestBed.inject(HttpTestingController);
    const dates = {
      currentSeasonDateFrom: '2026-05-01T00:00:00.000Z',
      currentSeasonDateTo: '2026-09-30T00:00:00.000Z',
      nextSeasonDateFrom: null,
      nextSeasonDateTo: null,
      extra: true,
    };

    seasons.saveSeasonDates('p1', dates).subscribe();

    const request = controller.expectOne(
      `${environment.apiBaseUrl}/seasons/save-season-dates`,
    ).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      currentSeasonDateFrom: '2026-05-01T00:00:00.000Z',
      currentSeasonDateTo: '2026-09-30T00:00:00.000Z',
      nextSeasonDateFrom: null,
      nextSeasonDateTo: null,
      idProperty: 'p1',
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

    seasons.addInterval('p1', 'd1', interval).subscribe();

    const request = controller.expectOne(`${environment.apiBaseUrl}/seasons/add-interval`).request;
    expect(request.method).toBe('POST');
    expect(request.body).toEqual({
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-06-10T00:00:00.000Z',
      idPeriod: 'd1',
      idProperty: 'p1',
    });
    controller.verify();
  });
});
