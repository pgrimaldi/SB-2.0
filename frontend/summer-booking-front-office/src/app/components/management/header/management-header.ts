import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DateAdapter } from '@angular/material/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';
import { IconButton } from '../../shared/ui/buttons/icon-button/icon-button';
import { SearchField } from '../../shared/ui/inputs/search-field/search-field';
import { Select, SelectOption } from '../../shared/ui/selects/select/select';
import { Datepicker } from './datepicker/datepicker';

@Component({
  selector: 'app-management-header',
  imports: [Datepicker, I18nText, IconButton, SearchField, Select, TranslatePipe],
  templateUrl: './management-header.html',
  styleUrl: './management-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementHeader {
  /** Dates, period and search live in the filters behaviour, read by the management pages. */
  protected readonly filters = inject(ManagementFiltersBehaviour);
  private readonly auth = inject(AuthBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  protected readonly periods: readonly SelectOption<BookingDayType>[] = [
    { key: BookingDayType.FullDay, value: 'management.header.period.full-day' },
    { key: BookingDayType.Morning, value: 'management.header.period.morning' },
    { key: BookingDayType.Afternoon, value: 'management.header.period.afternoon' },
  ];

  /** Initial of the signed-in user, shown in the account button. */
  protected readonly initial = computed(
    () => this.auth.user()?.email.charAt(0).toUpperCase() ?? '',
  );

  protected readonly showsToday = computed(() => {
    const today = this.dateAdapter.today();
    return (
      this.dateAdapter.sameDate(this.filters.startDate(), today) &&
      this.dateAdapter.sameDate(this.filters.endDate(), today)
    );
  });

  protected resetToToday(): void {
    this.filters.startDate.set(this.dateAdapter.today());
    this.filters.endDate.set(this.dateAdapter.today());
  }
}
