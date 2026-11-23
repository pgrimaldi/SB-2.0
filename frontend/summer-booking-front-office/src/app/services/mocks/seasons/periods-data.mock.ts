import { SeasonPeriodItem } from '../../../entities/settings/seasons/season-period-item';

/** The days of its date ranges as the backend keeps them (`2026-05-01`). */
export type PeriodRow = Omit<SeasonPeriodItem, 'version'> & {
  propertyId: number;
  isNextSeason: boolean;
};

export const PERIODS: PeriodRow[] = [
  {
    propertyId: 1,
    isNextSeason: false,
    key: 0,
    name: 'Bassa stagione',
    ranges: [
      { startsOn: '2026-05-01', endsOn: '2026-06-15' },
      { startsOn: '2026-09-01', endsOn: '2026-09-30' },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    key: 1,
    name: 'Media stagione',
    ranges: [{ startsOn: '2026-06-16', endsOn: '2026-07-31' }],
  },
  {
    propertyId: 1,
    isNextSeason: false,
    key: 2,
    name: 'Alta stagione',
    ranges: [{ startsOn: '2026-08-01', endsOn: '2026-08-31' }],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    key: 0,
    name: 'Bassa stagione',
    ranges: [
      { startsOn: '2027-05-01', endsOn: '2027-06-30' },
      { startsOn: '2027-09-01', endsOn: '2027-09-30' },
    ],
  },
  {
    propertyId: 1,
    isNextSeason: true,
    key: 1,
    name: 'Alta stagione',
    ranges: [{ startsOn: '2027-07-01', endsOn: '2027-08-31' }],
  },
];
