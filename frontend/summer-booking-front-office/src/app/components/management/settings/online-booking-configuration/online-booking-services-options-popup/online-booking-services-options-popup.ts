import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  Signal,
  ViewEncapsulation,
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
  applyWhen,
  form,
  max,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { OnlineBookingServicesOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-services-options';
import { BookingService } from '../../../../../services/api/booking/booking.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../../services/api/only-fields-of';
import { greaterThan } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { RichTextField } from '../../../../shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';

interface ServicesOptionsTexts {
  supplement_types: Record<'none' | 'percentage' | 'fixed', string>;
}

@Component({
  selector: 'app-online-booking-services-options-popup',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FormField,
    FormRow,
    RichTextField,
    SectionTitle,
    Toggle,
    TranslatePipe,
  ],
  templateUrl: './online-booking-services-options-popup.html',
  styleUrl: './online-booking-services-options-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class OnlineBookingServicesOptionsPopup
  extends BasePopup
  implements UnsavedChanges, OnDestroy
{
  private readonly auth = inject(AuthBehaviour);
  private readonly booking = inject(BookingService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [bold, italic, underline, bulleted list, numbered list, link]. */
  protected readonly richTextIcons = [
    '/assets/images/GeneralIcon_Bold.svg',
    '/assets/images/GeneralIcon_Italic.svg',
    '/assets/images/GeneralIcon_Underline.svg',
    '/assets/images/GeneralIcon_BulletList.svg',
    '/assets/images/GeneralIcon_OrderedList.svg',
    '/assets/images/GeneralIcon_TextLink.svg',
  ] as const;

  private readonly draft = signal(new OnlineBookingServicesOptions());
  private loaded: OnlineBookingServicesOptions | null = null;
  protected readonly draftForm = form(this.draft, (path) => {
    applyWhen(
      path.supplementValue,
      ({ valueOf }) => valueOf(path.supplementType) !== SupplementType.None,
      (supplementValue) => greaterThan(supplementValue, 0),
    );
    max(path.supplementValue, ({ valueOf }) =>
      valueOf(path.supplementType) === SupplementType.Percentage ? 100 : undefined,
    );
  });
  protected readonly hasSupplement = computed(
    () => this.draftForm.supplementType().value() !== SupplementType.None,
  );
  protected readonly isPercentage = computed(
    () => this.draftForm.supplementType().value() === SupplementType.Percentage,
  );
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.draft()) as (keyof OnlineBookingServicesOptions)[]).map((name) => [
      name,
      this.validationText.message(
        this.draftForm[name],
        name === 'supplementValue'
          ? 'management.settings.online_booking_configuration.services_options.field_names.supplement_value'
          : undefined,
      ),
    ]),
  ) as Readonly<Record<keyof OnlineBookingServicesOptions, Signal<string | null>>>;
  protected readonly isLoading = signal(false);
  protected readonly isSaving = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.online_booking_configuration.services_options',
    ) as Observable<ServicesOptionsTexts>,
    { initialValue: null },
  );
  protected readonly supplementTypeOptions = computed<readonly SelectOption<SupplementType>[]>(
    () => [
      { value: SupplementType.None, label: this.texts()?.supplement_types.none ?? '' },
      { value: SupplementType.Percentage, label: this.texts()?.supplement_types.percentage ?? '' },
      { value: SupplementType.Fixed, label: this.texts()?.supplement_types.fixed ?? '' },
    ],
  );

  constructor() {
    super();
    this.unsaved.watch(this);
    effect((onCleanup) => {
      if (!this.open()) {
        return;
      }
      const subscription = untracked(() => this.load());
      onCleanup(() => subscription?.unsubscribe());
    });
    effect(() => {
      if (!this.hasSupplement()) {
        untracked(() => this.draftForm.supplementValue().value.set(null));
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      !this.isLoading() &&
      JSON.stringify(this.draft()) !==
        JSON.stringify(this.loaded ?? new OnlineBookingServicesOptions())
    );
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || this.isSaving() || !idProperty) {
      return;
    }
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = this.draft();
        await firstValueFrom(this.booking.updateOnlineServicesOptions(idProperty, sent));
        this.loaded = sent;
        this.isSaving.set(false);
        this.close();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.draftForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (!this.isSaving() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isSaving();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(44rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'online__booking__services__options__popup__panel',
      disableClose: true,
    };
  }

  /** The saved options are asked only the first time: then the popup shows the last ones kept. */
  private load(): Subscription | undefined {
    this.error.set(null);
    if (this.loaded) {
      this.fill(this.loaded);
      return;
    }
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.fill(new OnlineBookingServicesOptions());
    this.isLoading.set(true);
    return this.booking.onlineServicesOptions(idProperty).subscribe({
      next: (options) => {
        const loaded = onlyFieldsOf(OnlineBookingServicesOptions, options);
        if (loaded.supplementType === SupplementType.None) {
          loaded.supplementValue = null;
        }
        this.loaded = loaded;
        this.fill(loaded);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiProblem(error));
        this.isLoading.set(false);
      },
    });
  }

  private fill(options: OnlineBookingServicesOptions): void {
    this.draft.set(options);
    this.draftForm().reset();
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
