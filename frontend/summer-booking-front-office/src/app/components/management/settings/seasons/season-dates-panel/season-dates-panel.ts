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
  ValidationError,
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
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { SeasonDates } from '../../../../../entities/settings/seasons/season-dates';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { onlyFieldsOf } from '../../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { DATA_RELOAD } from '../../../../shared/ui/data/data-reload';
import { AlertPopup } from '../../../../shared/ui/dialogs/alert-popup/alert-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

@Component({
  selector: 'app-season-dates-panel',
  imports: [
    AlertPopup,
    Button,
    FilledDateField,
    FormField,
    FormRow,
    SectionTitle,
    TranslatePipe,
    VersionConflictPopup,
  ],
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
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  /** [X]. */
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly isPeriodsOutsideShown = signal(false);
  protected readonly isConflictShown = signal(false);
  private loaded = new SeasonDates();
  private readonly dates = signal(new SeasonDates());
  protected readonly datesForm = form(this.dates, (path) => {
    required(path.startsOn);
    required(path.endsOn);
    required(path.nextStartsOn);
    required(path.nextEndsOn);
    readonly(path, () => !this.isEditing() || this.isSaving() || this.isLoading());
  });
  protected readonly fieldErrors = {
    startsOn: this.validationText.message(this.datesForm.startsOn),
    endsOn: this.validationText.message(this.datesForm.endsOn),
    nextStartsOn: this.validationText.message(this.datesForm.nextStartsOn),
    nextEndsOn: this.validationText.message(this.datesForm.nextEndsOn),
  };
  protected readonly canSave = computed(() => this.datesForm().valid());
  protected readonly nextSeasonMinDate = computed(() => dayAfter(this.datesForm.endsOn().value()));
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
      this.seasonDates.reloads();
      // Dates typed and not saved are never lost: they are read again only without changes.
      if (!this.auth.user()?.propertyId || untracked(() => this.hasUnsavedChanges())) {
        return;
      }
      const subscription = untracked(() => {
        this.error.set(null);
        this.isLoaded.set(false);
        this.seasonDates.isLoaded.set(false);
        this.isLoading.set(true);
        return this.seasons.seasonDates().subscribe({
          next: (dates) => {
            this.loaded = this.withDefaultStarts(onlyFieldsOf(SeasonDates, dates));
            this.dates.set(this.loaded);
            this.seasonDates.dates.set(this.loaded);
            this.datesForm().reset();
            this.isEditing.set(false);
            this.isLoaded.set(true);
            this.seasonDates.isLoaded.set(true);
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
    return comparableJson(this.dates()) !== comparableJson(this.loaded);
  }

  protected edit(): void {
    if (this.isLoaded()) {
      this.isEditing.set(true);
    }
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.dates.set(this.loaded);
    this.datesForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected save(): void {
    if (this.isSaving() || !this.isLoaded() || !this.auth.user()?.propertyId || !this.canSave()) {
      return;
    }
    void submit(this.datesForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = this.dates();
        if (await this.hasPeriodsOutside(sent)) {
          this.isPeriodsOutsideShown.set(true);
          return;
        }
        const saved = await firstValueFrom(this.seasons.saveSeasonDates(sent, this.loaded.version));
        this.loaded = saved;
        this.dates.set(saved);
        this.seasonDates.dates.set(saved);
        this.datesForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.reload();
          return undefined;
        }
        return apiFieldErrorsOrGeneral(problem, this.datesForm, (general) =>
          this.error.set(general),
        ).map((fieldError): ValidationError.WithFieldTree =>
          fieldError.fieldTree === this.datesForm.nextStartsOn &&
          fieldError.code === ApiErrorCode.ValidationOutOfRange
            ? { kind: 'seasons.next_season_too_early', fieldTree: fieldError.fieldTree }
            : fieldError,
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  private reload(): void {
    this.cancel();
    this.seasonDates.reload();
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
      startsOn: dates.startsOn ?? firstOfJanuary(year),
      nextStartsOn: dates.nextStartsOn ?? firstOfJanuary(year + 1),
    };
  }

  /** The periods of both seasons as saved now, against the new first and last days. */
  private async hasPeriodsOutside(dates: SeasonDates): Promise<boolean> {
    const [current, next] = await Promise.all([
      firstValueFrom(this.seasons.periodList(false)),
      firstValueFrom(this.seasons.periodList(true)),
    ]);
    return (
      isAnyOutside(current, dates.startsOn, dates.endsOn) ||
      isAnyOutside(next, dates.nextStartsOn, dates.nextEndsOn)
    );
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

/** A date range of the periods before `startsOn` or after `endsOn`; no limit while one is missing. */
function isAnyOutside(
  periods: readonly SeasonPeriodItem[],
  startsOn: string | null,
  endsOn: string | null,
): boolean {
  if (!startsOn || !endsOn) {
    return false;
  }
  const first = startsOn.slice(0, 10);
  const last = endsOn.slice(0, 10);
  return periods.some(({ ranges }) =>
    ranges.some(
      (range) =>
        (range.startsOn ?? '').slice(0, 10) < first || (range.endsOn ?? '').slice(0, 10) > last,
    ),
  );
}

function dayAfter(day: string | null): string | null {
  return day ? new Date(Date.parse(day) + 24 * 60 * 60 * 1000).toISOString() : null;
}
