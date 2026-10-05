import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DateAdapter } from '@angular/material/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ManagementFiltersBehaviour } from '../../../behaviours/management/management-filters.behaviour';
import { BookingDayType } from '../../../entities/enums/booking-day-type';
import { I18nText } from '../../i18n/i18n-text/i18n-text';
import { IconButton } from '../../shared/ui/buttons/icon-button/icon-button';
import { Datepicker } from '../../shared/ui/datepickers/datepicker/datepicker';
import { SearchField } from '../../shared/ui/inputs/search-field/search-field';
import { Select, SelectOption } from '../../shared/ui/selects/select/select';

/** Translation group `management.header.period`. */
interface PeriodTexts {
  label: string;
  full_day: string;
  morning: string;
  afternoon: string;
}

@Component({
  selector: 'app-management-header',
  imports: [Datepicker, I18nText, IconButton, SearchField, Select, TranslatePipe],
  templateUrl: './management-header.html',
  styleUrl: './management-header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagementHeader {
  /** Dates and period live in the filters behaviour, read by the management pages. */
  protected readonly filters = inject(ManagementFiltersBehaviour);
  private readonly auth = inject(AuthBehaviour);
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);
  private readonly translateService = inject(TranslateService);
  private readonly router = inject(Router);

  private readonly periodTexts = toSignal(
    this.translateService.stream('management.header.period') as Observable<PeriodTexts>,
    { initialValue: { label: '', full_day: '', morning: '', afternoon: '' } },
  );
  protected readonly periods = computed<readonly SelectOption<BookingDayType>[]>(() => [
    { value: BookingDayType.FullDay, label: this.periodTexts().full_day },
    { value: BookingDayType.Morning, label: this.periodTexts().morning },
    { value: BookingDayType.Afternoon, label: this.periodTexts().afternoon },
  ]);

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

  protected openSettings(): void {
    void this.router.navigateByUrl('/settings');
  }
}
