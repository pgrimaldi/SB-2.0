export enum TableServiceMealDuration {
  ThirtyMinutes = 'ThirtyMinutes',
  FortyFiveMinutes = 'FortyFiveMinutes',
  OneHour = 'OneHour',
  OneHourFifteenMinutes = 'OneHourFifteenMinutes',
  OneHourThirtyMinutes = 'OneHourThirtyMinutes',
  OneHourFortyFiveMinutes = 'OneHourFortyFiveMinutes',
  TwoHours = 'TwoHours',
  TwoHoursThirtyMinutes = 'TwoHoursThirtyMinutes',
  ThreeHours = 'ThreeHours',
}

export const TABLE_SERVICE_MEAL_DURATION_MINUTES: Readonly<
  Record<TableServiceMealDuration, number>
> = {
  [TableServiceMealDuration.ThirtyMinutes]: 30,
  [TableServiceMealDuration.FortyFiveMinutes]: 45,
  [TableServiceMealDuration.OneHour]: 60,
  [TableServiceMealDuration.OneHourFifteenMinutes]: 75,
  [TableServiceMealDuration.OneHourThirtyMinutes]: 90,
  [TableServiceMealDuration.OneHourFortyFiveMinutes]: 105,
  [TableServiceMealDuration.TwoHours]: 120,
  [TableServiceMealDuration.TwoHoursThirtyMinutes]: 150,
  [TableServiceMealDuration.ThreeHours]: 180,
};
