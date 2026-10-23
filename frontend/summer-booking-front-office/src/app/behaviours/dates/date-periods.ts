import { DateRange } from '../../entities/shared/date-range';
import { PageFilter } from '../../entities/pagination/page';

/** A calendar day as the API wants it: the same day in UTC, with no time zone shift. */
export function utcDayStart(day: Date): string {
  return new Date(Date.UTC(day.getFullYear(), day.getMonth(), day.getDate())).toISOString();
}

export function utcDayEnd(day: Date): string {
  return new Date(
    Date.UTC(day.getFullYear(), day.getMonth(), day.getDate(), 23, 59, 59, 999),
  ).toISOString();
}

/** The days of a range as table filters; none without a range. */
export function dateRangeFilters(range: DateRange | null): PageFilter[] {
  return range
    ? [
        { field: 'datetimeFrom', value: [utcDayStart(range.start)] },
        { field: 'datetimeTo', value: [utcDayEnd(range.end)] },
      ]
    : [];
}

export function todayPeriod(today: Date): DateRange {
  return { start: day(today, 0), end: day(today, 0) };
}

/** From Monday to Sunday. */
export function thisWeekPeriod(today: Date): DateRange {
  const monday = mondayOf(today);
  return { start: monday, end: day(monday, 6) };
}

export function nextWeekPeriod(today: Date): DateRange {
  const monday = day(mondayOf(today), 7);
  return { start: monday, end: day(monday, 6) };
}

/** Whole months, from the first day of the next one. */
export function nextMonthsPeriod(today: Date, months: number): DateRange {
  return {
    start: new Date(today.getFullYear(), today.getMonth() + 1, 1),
    end: new Date(today.getFullYear(), today.getMonth() + 1 + months, 0),
  };
}

/** Today and the six days before. */
export function lastSevenDaysPeriod(today: Date): DateRange {
  return { start: day(today, -6), end: day(today, 0) };
}

/** From the same day some months ago (or the last day of that month, if shorter) to today. */
export function lastMonthsPeriod(today: Date, months: number): DateRange {
  const monthEnd = new Date(today.getFullYear(), today.getMonth() - months + 1, 0);
  return {
    start: new Date(
      monthEnd.getFullYear(),
      monthEnd.getMonth(),
      Math.min(today.getDate(), monthEnd.getDate()),
    ),
    end: day(today, 0),
  };
}

function mondayOf(date: Date): Date {
  return day(date, -((date.getDay() + 6) % 7));
}

function day(date: Date, offset: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + offset);
}
