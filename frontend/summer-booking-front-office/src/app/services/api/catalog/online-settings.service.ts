import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { BookingLimitRule } from '../../../entities/settings/online-booking-configuration/booking-limit-rule';
import { LegendItemRequest } from '../../../entities/settings/online-booking-configuration/legend-item-request';
import { OnlineBookingOptions } from '../../../entities/settings/online-booking-configuration/online-booking-options';
import { OnlineBookingOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-options-update-request';
import { OnlineBookingPeriod } from '../../../entities/settings/online-booking-configuration/online-booking-period';
import { OnlineBookingServicesOptions } from '../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { OnlineBookingServicesOptionsUpdateRequest } from '../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { onlyFieldsOf } from '../only-fields-of';
import { ifMatch, versionOf } from '../versions/version-headers';

@Injectable({ providedIn: 'root' })
export class OnlineSettingsService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog/online-settings`;

  readonly onlineBookingOptions = (): Observable<OnlineBookingOptions> =>
    this.httpClient.get<OnlineBookingOptions>(this.endpoint).pipe(map(optionsOf));

  /** Every option, changed or not, with the limits and the legend. Gives back the new version. */
  readonly updateOnlineBookingOptions = (
    options: OnlineBookingOptionsUpdateRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<OnlineBookingOptions>(this.endpoint, bodyOf(options), {
        headers: ifMatch(version),
        observe: 'response',
      })
      .pipe(map(versionOf));

  readonly onlineServicesOptions = (): Observable<OnlineBookingServicesOptions> =>
    this.httpClient.get<OnlineBookingServicesOptions>(`${this.endpoint}/services`);

  /** Every option, changed or not. Gives back the new version. */
  readonly updateOnlineServicesOptions = (
    options: OnlineBookingServicesOptionsUpdateRequest,
    version: string | null,
  ): Observable<string | null> =>
    this.httpClient
      .put<OnlineBookingServicesOptions>(
        `${this.endpoint}/services`,
        onlyFieldsOf(OnlineBookingServicesOptionsUpdateRequest, options),
        { headers: ifMatch(version), observe: 'response' },
      )
      .pipe(map(versionOf));
}

/** The backend sends and takes days (`2026-05-01`) and times with the seconds; the form wants midnight UTC and `HH:mm`. */
function optionsOf(answer: OnlineBookingOptions): OnlineBookingOptions {
  const options = onlyFieldsOf(OnlineBookingOptions, answer);
  return {
    ...options,
    allowedPeriods: options.allowedPeriods.map((period) => ({
      startsOn: midnightOf(period.startsOn),
      endsOn: midnightOf(period.endsOn),
    })),
    stayLimits: options.stayLimits.map((rule) => ({
      ...onlyFieldsOf(BookingLimitRule, rule),
      startsOn: midnightOf(rule.startsOn),
      endsOn: midnightOf(rule.endsOn),
    })),
    morningFrom: timeOf(options.morningFrom),
    morningTo: timeOf(options.morningTo),
    afternoonFrom: timeOf(options.afternoonFrom),
    afternoonTo: timeOf(options.afternoonTo),
    afternoonConversionTime: timeOf(options.afternoonConversionTime),
  };
}

function bodyOf(options: OnlineBookingOptionsUpdateRequest): object {
  return {
    ...onlyFieldsOf(OnlineBookingOptionsUpdateRequest, options),
    allowedPeriods: options.allowedPeriods.map((period) => ({
      ...onlyFieldsOf(OnlineBookingPeriod, period),
      startsOn: dayOf(period.startsOn),
      endsOn: dayOf(period.endsOn),
    })),
    stayLimits: options.stayLimits.map((rule) => ({
      ...onlyFieldsOf(BookingLimitRule, rule),
      startsOn: dayOf(rule.startsOn),
      endsOn: dayOf(rule.endsOn),
    })),
    legend: options.legend.map((item) => onlyFieldsOf(LegendItemRequest, item)),
  };
}

function midnightOf(day: string | null): string | null {
  return day ? `${day.slice(0, 10)}T00:00:00.000Z` : null;
}

function dayOf(date: string | null): string | null {
  return date ? date.slice(0, 10) : null;
}

function timeOf(time: string | null): string | null {
  return time ? time.slice(0, 5) : null;
}
