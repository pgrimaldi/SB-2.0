import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  TreeValidationResult,
  form,
  readonly,
  required,
  submit,
} from '@angular/forms/signals';
import { DateAdapter } from '@angular/material/core';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { SeasonDatesBehaviour } from '../../../../../behaviours/settings/season-dates.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { SeasonDates } from '../../../../../entities/settings/seasons/season-dates';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { onlyFieldsOf } from '../../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { DATA_RELOAD } from '../../../../shared/ui/data/data-reload';
import { AlertPopup } from '../../../../shared/ui/dialogs/alert-popup/alert-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

@Component({
  selector: 'app-season-dates-panel',
  imports: [AlertPopup, Button, FilledDateField, FormField, FormRow, SectionTitle, TranslatePipe],
  templateUrl: './season-dates-panel.html',
  styleUrl: './season-dates-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeasonDatesPanel implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly seasons = inject(SeasonService);
  private readonly seasonDates = inject(SeasonDatesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly dateAdapter = inject<DateAdapter<Date>>(DateAdapter);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [calendar, before the date]. */
  protected readonly dateIcon = ['/assets/images/GeneralIcon_CalendarFilled.svg'] as const;
  protected readonly isLoading = signal(false);
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  protected readonly isPeriodsOutsideShown = signal(false);
  private loaded = new SeasonDates();
  private readonly dates = signal(new SeasonDates());
  protected readonly datesForm = form(this.dates, (path) => {
    required(path.currentSeasonDateFrom);
    required(path.currentSeasonDateTo);
    readonly(path, () => !this.isEditing() || this.isSaving() || this.isLoading());
  });
  protected readonly fieldErrors = {
    currentSeasonDateFrom: this.validationText.message(this.datesForm.currentSeasonDateFrom),
    currentSeasonDateTo: this.validationText.message(this.datesForm.currentSeasonDateTo),
    nextSeasonDateFrom: this.validationText.message(this.datesForm.nextSeasonDateFrom),
    nextSeasonDateTo: this.validationText.message(this.datesForm.nextSeasonDateTo),
  };
  protected readonly canSave = computed(() => this.datesForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.error.set(null);
        this.isLoading.set(true);
        return this.seasons.seasonDates(idProperty).subscribe({
          next: (dates) => {
            this.loaded = this.withDefaultStarts(onlyFieldsOf(SeasonDates, dates));
            this.dates.set(this.loaded);
            this.seasonDates.dates.set(this.loaded);
            this.datesForm().reset();
            this.isEditing.set(false);
            this.isLoading.set(false);
          },
          error: (error: unknown) => {
            this.error.set(toApiProblem(error));
            this.isLoading.set(false);
          },
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.datesForm().dirty();
  }

  protected edit(): void {
    this.isEditing.set(true);
  }

  protected cancel(): void {
    this.dates.set(this.loaded);
    this.datesForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isSaving() || !idProperty || !this.canSave()) {
      return;
    }
    void submit(this.datesForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = this.dates();
        if (await this.hasPeriodsOutside(idProperty, sent)) {
          this.cancel();
          this.isPeriodsOutsideShown.set(true);
          return undefined;
        }
        await firstValueFrom(this.seasons.saveSeasonDates(idProperty, sent));
        this.loaded = sent;
        this.seasonDates.dates.set(sent);
        this.datesForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.datesForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  /**
   * A start the server does not send: 1 January of this year for the current season, of next year
   * for the next one.
   */
  private withDefaultStarts(dates: SeasonDates): SeasonDates {
    const year = this.dateAdapter.getYear(this.dateAdapter.today());
    const firstOfJanuary = (inYear: number): string =>
      new Date(Date.UTC(inYear, 0, 1)).toISOString();
    return {
      ...dates,
      currentSeasonDateFrom: dates.currentSeasonDateFrom ?? firstOfJanuary(year),
      nextSeasonDateFrom: dates.nextSeasonDateFrom ?? firstOfJanuary(year + 1),
    };
  }

  /** The periods of both seasons as saved now, against the new first and last days. */
  private async hasPeriodsOutside(idProperty: string, dates: SeasonDates): Promise<boolean> {
    const [current, next] = await Promise.all([
      firstValueFrom(this.seasons.periodList(idProperty, false)),
      firstValueFrom(this.seasons.periodList(idProperty, true)),
    ]);
    return (
      isAnyOutside(current, dates.currentSeasonDateFrom, dates.currentSeasonDateTo) ||
      isAnyOutside(next, dates.nextSeasonDateFrom, dates.nextSeasonDateTo)
    );
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** A date interval of the periods before `dateFrom` or after `dateTo`; no limit while one is missing. */
function isAnyOutside(
  periods: readonly SeasonPeriodItem[],
  dateFrom: string | null,
  dateTo: string | null,
): boolean {
  if (!dateFrom || !dateTo) {
    return false;
  }
  const first = dateFrom.slice(0, 10);
  const last = dateTo.slice(0, 10);
  return periods.some(({ intervals }) =>
    intervals.some(
      (interval) =>
        (interval.dateFrom ?? '').slice(0, 10) < first ||
        (interval.dateTo ?? '').slice(0, 10) > last,
    ),
  );
}
