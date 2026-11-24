import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EnvironmentInjector,
  OnDestroy,
  Signal,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FieldTree,
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  ValidationError,
  applyEach,
  applyWhen,
  disabled,
  form,
  max,
  metadata,
  min,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, Observable, Subscription, firstValueFrom } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { fileLinkOf } from '../../../../../behaviours/files/file-link';
import {
  apiFieldErrorsOrGeneral,
  clearApiFieldErrors,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { MapIconCategory } from '../../../../../entities/enums/map-icon-category';
import { OnlineBookingAudience } from '../../../../../entities/enums/online-booking-audience';
import { OnlineBookingBlock } from '../../../../../entities/enums/online-booking-block';
import { OnlinePaymentMethod } from '../../../../../entities/enums/online-payment-method';
import { SupplementType } from '../../../../../entities/enums/supplement-type';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { MapCatalogIcon } from '../../../../../entities/settings/map-configuration/map-catalog-icon';
import {
  BookingLimitRule,
  MAX_BOOKING_LIMIT_DAYS,
} from '../../../../../entities/settings/online-booking-configuration/booking-limit-rule';
import {
  LegendItem,
  MAX_LEGEND_ITEM_QUANTITY,
  MAX_LEGEND_LABEL_LENGTH,
} from '../../../../../entities/settings/online-booking-configuration/legend-item';
import { OnlineBookingOptions } from '../../../../../entities/settings/online-booking-configuration/online-booking-options';
import { OnlineBookingOptionsDraft } from '../../../../../entities/settings/online-booking-configuration/online-booking-options-draft';
import {
  MAX_ONLINE_BOOKING_HORIZON_DAYS,
  MAX_ONLINE_BOOKING_PERIOD_QUANTITY,
  MAX_ONLINE_BOOKING_QUANTITY,
  OnlineBookingOptionsUpdateRequest,
} from '../../../../../entities/settings/online-booking-configuration/online-booking-options-update-request';
import {
  MAX_ONLINE_USEFUL_INFO_LENGTH,
  MAX_SURCHARGE_AMOUNT,
  MAX_SURCHARGE_PERCENTAGE,
} from '../../../../../entities/settings/online-booking-configuration/online-booking-services-options-update-request';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { OnlineSettingsService } from '../../../../../services/api/catalog/online-settings.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../../services/api/only-fields-of';
import { showFirstError, showMessage } from '../../../../shared/forms/first-error';
import {
  MaxLengthError,
  maxHtmlLength,
  rangesOverlap,
  requiredText,
  timeRange,
} from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { DATA_RELOAD } from '../../../../shared/ui/data/data-reload';
import { PeriodCalendar } from '../../../../shared/ui/datepickers/period-calendar/period-calendar';
import {
  IconPickerPopup,
  IconPickerPopupIcon,
} from '../../../../shared/ui/dialogs/icon-picker-popup/icon-picker-popup';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledTimeField } from '../../../../shared/ui/inputs/filled-time-field/filled-time-field';
import { RichTextField } from '../../../../shared/ui/inputs/rich-text-field/rich-text-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { Toggle } from '../../../../shared/ui/toggles/toggle/toggle';
import { comparableJson } from '../../../../shared/forms/text-comparison';
import { ReadErrorPopup } from '../../../read-error-popup/read-error-popup';
import { optionsWithMissing } from '../../catalog/options-with-missing';
import { VersionConflictPopup } from '../../../version-conflict-popup/version-conflict-popup';

interface BookingOptionsTexts {
  supplement_types: Record<'none' | 'percentage' | 'fixed', string>;
  online_booking_blocks: Record<OnlineBookingBlock, string>;
  online_booking_audiences: Record<'all' | 'contacts_only', string>;
  booking_limits: { missing_sector: string };
  yes: string;
  no: string;
}

interface OnlineBookingError extends ValidationError {
  readonly kind: OnlineBookingErrorKind;
  readonly max?: number;
}

enum OnlineBookingErrorKind {
  PeriodsRequired = 'online_booking_configuration.periods.required',
  PeriodsOverlap = 'online_booking_configuration.periods.overlap',
  TooManyPeriods = 'online_booking_configuration.periods.max',
  LimitWithoutDays = 'online_booking_configuration.booking_limits.days_required',
  LimitMaxDays = 'online_booking_configuration.booking_limits.max_days',
  LimitsOverlap = 'online_booking_configuration.booking_limits.overlap',
}

/** The select wants text keys: the answer it shows for a yes/no field. */
enum Answer {
  Yes = 'Yes',
  No = 'No',
}

@Component({
  selector: 'app-online-booking-options-panel',
  imports: [
    Button,
    Checkbox,
    FilledDateField,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FilledTimeField,
    FormField,
    FormRow,
    FormRowLabel,
    IconButton,
    IconPickerPopup,
    PeriodCalendar,
    ReadErrorPopup,
    RichTextField,
    SectionTitle,
    SidePanel,
    Toggle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './online-booking-options-panel.html',
  styleUrl: './online-booking-options-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnlineBookingOptionsPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly translateService = inject(TranslateService);
  private readonly auth = inject(AuthBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly injector = inject(EnvironmentInjector);
  private readonly mapService = inject(MapService);
  private readonly onlineSettings = inject(OnlineSettingsService);
  private readonly origin = inject(DOCUMENT).location.origin;
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  /** [previous month, next month, remove a period]. */
  protected readonly calendarIcons = [
    '/assets/images/GeneralIcon_Previous.svg',
    '/assets/images/GeneralIcon_Next.svg',
    '/assets/images/GeneralIcon_Close.svg',
  ] as const;
  protected readonly clockIcon = ['/assets/images/GeneralIcon_Clock.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly addIcon = ['/assets/images/OnlineBookingConfiguration_AddRule.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly emptyIconIcon = ['/assets/images/GeneralIcon_CloseRed.svg'] as const;
  /** [bold, italic, underline, bulleted list, numbered list, link]. */
  protected readonly richTextIcons = [
    '/assets/images/GeneralIcon_Bold.svg',
    '/assets/images/GeneralIcon_Italic.svg',
    '/assets/images/GeneralIcon_Underline.svg',
    '/assets/images/GeneralIcon_BulletList.svg',
    '/assets/images/GeneralIcon_OrderedList.svg',
    '/assets/images/GeneralIcon_TextLink.svg',
  ] as const;
  protected readonly paymentMethods = [
    {
      method: OnlinePaymentMethod.PayNow,
      label:
        'management.settings.online_booking_configuration.booking_options.payment_methods.pay_now',
    },
    {
      method: OnlinePaymentMethod.PayAtProperty,
      label:
        'management.settings.online_booking_configuration.booking_options.payment_methods.pay_at_property',
    },
    {
      method: OnlinePaymentMethod.PayAtPropertyWithoutCard,
      label:
        'management.settings.online_booking_configuration.booking_options.payment_methods.pay_at_property_without_card',
    },
    {
      method: OnlinePaymentMethod.Deposit,
      label:
        'management.settings.online_booking_configuration.booking_options.payment_methods.deposit',
    },
    {
      method: OnlinePaymentMethod.PrepaidCard,
      label:
        'management.settings.online_booking_configuration.booking_options.payment_methods.prepaid_card',
    },
  ] as const;
  private readonly content = viewChild<ElementRef<HTMLElement>>('content');
  protected readonly sectors = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorCombobox() : EMPTY),
    this.errorText,
  );
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    optionsWithMissing(
      this.sectors,
      [...new Set(this.draft().stayLimits.map(({ sectorId }) => sectorId))].filter(
        (sectorId) => sectorId !== null,
      ),
      this.texts()?.booking_limits.missing_sector ?? '',
    ),
  );
  /** The days of the other rules of the same sector, for each rule. */
  protected readonly takenPeriods = computed<readonly (readonly BookingLimitRule[])[]>(() => {
    const rules = this.draft().stayLimits;
    return rules.map(({ sectorId }, index) =>
      rules.filter(
        (other, otherIndex) =>
          otherIndex !== index && sectorId !== null && other.sectorId === sectorId,
      ),
    );
  });
  protected readonly icons = new DropdownList<MapCatalogIcon>(
    () => this.mapService.iconCatalog(),
    this.errorText,
  );
  protected readonly iconChoices = computed<readonly IconPickerPopupIcon[]>(() =>
    this.icons.items().flatMap(({ id, category, url }) => {
      const link = fileLinkOf(url, this.origin);
      return id && link && category !== MapIconCategory.Template ? [{ value: id, url: link }] : [];
    }),
  );
  protected readonly pickingItem = signal<number | null>(null);
  protected readonly isPickerOpen = computed(
    () => this.open() && this.pickingItem() !== null && this.icons.isRead(),
  );
  protected readonly isFetchingIcons = computed(
    () => this.pickingItem() !== null && !this.icons.isRead() && !this.icons.loadError(),
  );
  protected readonly pickedIcon = computed(() => {
    const index = this.pickingItem();
    return index === null ? null : (this.draft().legend[index]?.iconId ?? null);
  });

  private readonly draft = signal(new OnlineBookingOptionsDraft());
  /** The values as the API gave them: the form is compared with them. */
  private loaded = new OnlineBookingOptionsDraft();
  private version: string | null = null;
  /** Only a change of the type by the user empties the value, never a reading. */
  private shownType = SupplementType.None;
  protected readonly draftForm = form(this.draft, (path) => {
    disabled(
      path.isLocationRestricted,
      ({ value, valueOf }) => !value() && !valueOf(path.allowedSpotIds).length,
    );
    validate(path.allowedPeriods, ({ value, valueOf }): OnlineBookingError | undefined => {
      const periods = value();
      if (!valueOf(path.isPeriodRestricted)) {
        return undefined;
      }
      if (!periods.length) {
        return { kind: OnlineBookingErrorKind.PeriodsRequired };
      }
      if (periods.length > MAX_ONLINE_BOOKING_PERIOD_QUANTITY) {
        return {
          kind: OnlineBookingErrorKind.TooManyPeriods,
          max: MAX_ONLINE_BOOKING_PERIOD_QUANTITY,
        };
      }
      return periods.some((period, index) =>
        periods.slice(index + 1).some((other) => rangesOverlap(period, other)),
      )
        ? { kind: OnlineBookingErrorKind.PeriodsOverlap }
        : undefined;
    });
    applyWhen(
      path.surchargeValue,
      ({ valueOf }) => valueOf(path.surchargeType) !== SupplementType.None,
      (surchargeValue) => {
        required(surchargeValue);
        min(surchargeValue, 0);
        max(surchargeValue, ({ valueOf }) =>
          valueOf(path.surchargeType) === SupplementType.Percentage
            ? MAX_SURCHARGE_PERCENTAGE
            : MAX_SURCHARGE_AMOUNT,
        );
      },
    );
    applyWhen(
      path,
      ({ value }) => value().allowsHalfDay,
      (chosen) => {
        timeRange(chosen.morningFrom, chosen.morningTo);
        timeRange(chosen.afternoonFrom, chosen.afternoonTo);
      },
    );
    applyWhen(
      path.afternoonConversionTime,
      ({ valueOf }) => valueOf(path.isAfternoonConversionEnabled),
      (time) => required(time),
    );
    required(path.onlineBookingBlock);
    min(path.maxSpotsPerBooking, 1);
    max(path.maxSpotsPerBooking, MAX_ONLINE_BOOKING_QUANTITY);
    min(path.bookingHorizonDays, 0);
    max(path.bookingHorizonDays, MAX_ONLINE_BOOKING_HORIZON_DAYS);
    min(path.maxBookingsPerDay, 1);
    max(path.maxBookingsPerDay, MAX_ONLINE_BOOKING_QUANTITY);
    applyEach(path.stayLimits, (rule) => {
      required(rule.sectorId);
      required(rule.startsOn);
      required(rule.endsOn);
      min(rule.minDays, 1);
      max(rule.minDays, MAX_BOOKING_LIMIT_DAYS);
      min(rule.maxDays, 1);
      max(rule.maxDays, MAX_BOOKING_LIMIT_DAYS);
      validate(rule, ({ value, valueOf, index }): OnlineBookingError | undefined => {
        const { sectorId, minDays, maxDays } = value();
        if (minDays === null && maxDays === null) {
          return { kind: OnlineBookingErrorKind.LimitWithoutDays };
        }
        if (minDays !== null && maxDays !== null && maxDays < minDays) {
          return { kind: OnlineBookingErrorKind.LimitMaxDays };
        }
        return sectorId !== null &&
          valueOf(path.stayLimits).some(
            (other, otherIndex) =>
              otherIndex !== index() &&
              other.sectorId === sectorId &&
              rangesOverlap(value(), other),
          )
          ? { kind: OnlineBookingErrorKind.LimitsOverlap }
          : undefined;
      });
    });
    applyEach(path.legend, (item) => {
      requiredText(item.label);
      metadata(item.label, MAX_LENGTH, () => MAX_LEGEND_LABEL_LENGTH);
      validate(item.label, ({ value }): MaxLengthError | undefined =>
        (value()?.length ?? 0) > MAX_LEGEND_LABEL_LENGTH
          ? { kind: 'max_length', max: MAX_LEGEND_LABEL_LENGTH }
          : undefined,
      );
    });
    maxHtmlLength(path.goodToKnow, MAX_ONLINE_USEFUL_INFO_LENGTH);
    maxHtmlLength(path.usefulInfo, MAX_ONLINE_USEFUL_INFO_LENGTH);
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.draft()) as (keyof OnlineBookingOptionsDraft)[]).map((name) => [
      name,
      this.validationText.message(this.draftForm[name] as FieldTree<unknown>),
    ]),
  ) as Readonly<Record<keyof OnlineBookingOptionsDraft, Signal<string | null>>>;
  protected readonly halfDays = [
    {
      label: 'management.settings.online_booking_configuration.booking_options.morning',
      start: this.draftForm.morningFrom,
      end: this.draftForm.morningTo,
    },
    {
      label: 'management.settings.online_booking_configuration.booking_options.afternoon',
      start: this.draftForm.afternoonFrom,
      end: this.draftForm.afternoonTo,
    },
  ];
  protected readonly hasSupplement = computed(
    () => this.draftForm.surchargeType().value() !== SupplementType.None,
  );
  protected readonly isPercentage = computed(
    () => this.draftForm.surchargeType().value() === SupplementType.Percentage,
  );
  protected readonly canAddLegendItem = computed(
    () => this.draftForm.legend().value().length < MAX_LEGEND_ITEM_QUANTITY,
  );
  protected readonly isSuspended = signal(false);
  protected readonly isReading = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(
    () => this.isReading() || !this.isLoaded() || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly readError = signal<ApiProblem | null>(null);
  protected readonly isConflictShown = signal(false);
  protected readonly errorMessage = this.errorText.message(this.error);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.online_booking_configuration.booking_options',
    ) as Observable<BookingOptionsTexts>,
    { initialValue: null },
  );
  protected readonly supplementTypeOptions = computed<readonly SelectOption<SupplementType>[]>(
    () => [
      { value: SupplementType.None, label: this.texts()?.supplement_types.none ?? '' },
      { value: SupplementType.Percentage, label: this.texts()?.supplement_types.percentage ?? '' },
      { value: SupplementType.Fixed, label: this.texts()?.supplement_types.fixed ?? '' },
    ],
  );
  protected readonly answerOptions = computed<readonly SelectOption<Answer>[]>(() => [
    { value: Answer.Yes, label: this.texts()?.yes ?? '' },
    { value: Answer.No, label: this.texts()?.no ?? '' },
  ]);
  protected readonly bookingConfirmation = computed(() =>
    this.draftForm.requiresManualConfirmation().value() ? Answer.Yes : Answer.No,
  );
  protected readonly onlineBookingAudienceOptions = computed<
    readonly SelectOption<OnlineBookingAudience>[]
  >(() => [
    {
      value: OnlineBookingAudience.All,
      label: this.texts()?.online_booking_audiences.all ?? '',
    },
    {
      value: OnlineBookingAudience.ContactsOnly,
      label: this.texts()?.online_booking_audiences.contacts_only ?? '',
    },
  ]);
  protected readonly onlineBookingBlockOptions = computed<
    readonly SelectOption<OnlineBookingBlock>[]
  >(() =>
    Object.values(OnlineBookingBlock).map((value) => ({
      value,
      label: this.texts()?.online_booking_blocks[value] ?? '',
    })),
  );

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.open()) {
        untracked(() => this.stop());
        return;
      }
      const subscription = untracked(() => this.load());
      onCleanup(() => subscription?.unsubscribe());
    });
    effect(() => {
      const field = this.draftForm.isPeriodRestricted();
      if (field.dirty()) {
        const isOn = field.value();
        untracked(() => {
          const periods = this.draftForm.allowedPeriods();
          if (isOn) {
            periods.markAsTouched();
          } else {
            periods.value.set(this.loaded.isPeriodRestricted ? [] : this.loaded.allowedPeriods);
          }
        });
      }
    });
    effect(() => {
      const field = this.draftForm.allowsHalfDay();
      if (field.dirty() && !field.value()) {
        untracked(() =>
          (['morningFrom', 'morningTo', 'afternoonFrom', 'afternoonTo'] as const).forEach((name) =>
            this.draftForm[name]().value.set(this.loaded.allowsHalfDay ? null : this.loaded[name]),
          ),
        );
      }
    });
    effect(() => {
      const field = this.draftForm.isAfternoonConversionEnabled();
      if (field.dirty()) {
        const isOn = field.value();
        untracked(() => {
          const time = this.draftForm.afternoonConversionTime();
          if (isOn) {
            time.markAsTouched();
          } else {
            time.value.set(null);
          }
        });
      }
    });
    effect(() => {
      const type = this.draftForm.surchargeType().value();
      untracked(() => {
        if (this.shownType === type) {
          return;
        }
        this.shownType = type;
        const value = this.draftForm.surchargeValue();
        value.value.set(type === SupplementType.None ? this.valueWithoutSupplement() : null);
        value.markAsTouched();
      });
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && this.isLoaded() && comparable(this.draft()) !== comparable(this.loaded);
  }

  protected isAccepted(method: OnlinePaymentMethod): boolean {
    return this.draftForm.paymentMethods().value().includes(method);
  }

  protected accept(method: OnlinePaymentMethod, isAccepted: boolean): void {
    const methods = this.draftForm.paymentMethods();
    methods.value.update((accepted) =>
      isAccepted ? [...accepted, method] : accepted.filter((other) => other !== method),
    );
    methods.markAsDirty();
  }

  protected addBookingLimitRule(): void {
    this.draftForm.stayLimits().value.update((rules) => [...rules, new BookingLimitRule()]);
    // Says at once why Salva is off.
    this.draftForm.stayLimits[this.draft().stayLimits.length - 1]().markAsTouched();
  }

  protected removeBookingLimitRule(index: number): void {
    this.draftForm
      .stayLimits()
      .value.update((rules) => rules.filter((_, ruleIndex) => ruleIndex !== index));
  }

  protected addLegendItem(): void {
    this.draftForm.legend().value.update((items) => [...items, new LegendItem()]);
    this.draftForm.legend[this.draft().legend.length - 1]().markAsTouched();
  }

  protected removeLegendItem(index: number): void {
    this.pickingItem.update((picking) =>
      picking === null || picking < index ? picking : picking === index ? null : picking - 1,
    );
    this.draftForm
      .legend()
      .value.update((items) => items.filter((_, itemIndex) => itemIndex !== index));
  }

  protected iconUrlOf(item: FieldTree<LegendItem>): string | null {
    return fileLinkOf(item.iconUrl().value(), this.origin);
  }

  protected pickIcon(index: number): void {
    this.pickingItem.set(index);
    if (!this.icons.isRead()) {
      this.icons.load();
    }
  }

  protected pickerChanged(isOpen: boolean): void {
    if (!isOpen) {
      this.pickingItem.set(null);
    }
  }

  protected chooseIcon(iconId: string): void {
    const index = this.pickingItem();
    if (index === null) {
      return;
    }
    const item = this.draftForm.legend[index];
    item.iconId().value.set(iconId);
    item.iconUrl().value.set(this.icons.items().find(({ id }) => id === iconId)?.url ?? null);
  }

  protected emptyIcon(item: FieldTree<LegendItem>): void {
    item.iconId().value.set(null);
    item.iconUrl().value.set(null);
  }

  protected setBookingConfirmation(answer: Answer | null): void {
    this.draftForm.requiresManualConfirmation().value.set(answer === Answer.Yes);
  }

  protected save(): void {
    if (this.isReading() || this.isSaving() || !this.isLoaded()) {
      return;
    }
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.onlineSettings.updateOnlineBookingOptions(savedOf(this.draft()), this.version),
        );
        this.isSaving.set(false);
        this.open.set(false);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.isSaving.set(false);
          this.open.set(false);
          return undefined;
        }
        if (problem.code === ApiErrorCode.CatalogSectorNotFound) {
          this.sectors.load();
        }
        if (problem.code === ApiErrorCode.MapIconNotFound) {
          this.icons.clear();
        }
        return apiFieldErrorsOrGeneral(
          problem,
          this.draftForm,
          (general) => this.error.set(general),
          hiddenFieldsOf(this.draft()),
        );
      } finally {
        this.isSaving.set(false);
      }
    }).then((isSaved) => {
      if (!isSaved) {
        showFirstError(this.content(), '.online__booking__options__panel__error', this.injector);
      } else if (this.error()) {
        showMessage(this.content(), '.online__booking__options__panel__general', this.injector);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  protected retryRead(): void {
    this.open.set(true);
  }

  /** The saved options are read at every opening: the panel always starts from the server. */
  private load(): Subscription | undefined {
    this.error.set(null);
    this.readError.set(null);
    this.isLoaded.set(false);
    this.isSaving.set(false);
    this.isSuspended.set(false);
    this.sectors.clear();
    if (this.icons.loadError()) {
      this.icons.clear();
    }
    this.fill(new OnlineBookingOptionsDraft());
    if (!this.auth.user()?.propertyId) {
      return undefined;
    }
    this.sectors.load();
    this.isReading.set(true);
    return this.onlineSettings.onlineBookingOptions().subscribe({
      next: (options) => {
        this.version = options.version;
        this.isSuspended.set(options.isSuspendedByPlatform);
        this.fill(draftOf(options));
        this.draftForm().markAsTouched();
        this.isLoaded.set(true);
        this.isReading.set(false);
      },
      error: (error: unknown) => {
        this.isReading.set(false);
        this.open.set(false);
        this.readError.set(toApiProblem(error));
      },
    });
  }

  /** Closed while something is being read: its answer is not wanted any more. */
  private stop(): void {
    this.isReading.set(false);
    this.pickingItem.set(null);
  }

  private fill(options: OnlineBookingOptionsDraft): void {
    this.shownType = options.surchargeType;
    this.loaded = options;
    this.draft.set(options);
    clearApiFieldErrors(this.draftForm);
    this.draftForm().reset();
  }

  /** The value read together with no supplement goes back as it was. */
  private valueWithoutSupplement(): number | null {
    return this.loaded.surchargeType === SupplementType.None ? this.loaded.surchargeValue : null;
  }

  ngOnDestroy(): void {
    this.icons.destroy();
    this.sectors.destroy();
    this.unsaved.unwatch(this);
  }
}

function draftOf(options: OnlineBookingOptions): OnlineBookingOptionsDraft {
  return {
    ...onlyFieldsOf(OnlineBookingOptionsUpdateRequest, options),
    isAfternoonConversionEnabled: options.afternoonConversionTime !== null,
  };
}

/** As the backend keeps them: 0 for an empty value of the supplement. */
function savedOf(draft: OnlineBookingOptionsDraft): OnlineBookingOptionsUpdateRequest {
  return {
    ...onlyFieldsOf(OnlineBookingOptionsUpdateRequest, draft),
    surchargeValue: draft.surchargeValue ?? 0,
  };
}

/** The order of the periods and of the payment methods means nothing, nor the address of a chosen icon. */
function comparable(draft: OnlineBookingOptionsDraft): string {
  return comparableJson([
    {
      ...savedOf(draft),
      allowedPeriods: draft.allowedPeriods
        .map(({ startsOn, endsOn }) => `${startsOn} ${endsOn}`)
        .sort(),
      paymentMethods: [...draft.paymentMethods].sort(),
      legend: draft.legend.map((item) => ({ ...item, iconUrl: item.iconId ? null : item.iconUrl })),
    },
    draft.isAfternoonConversionEnabled,
  ]);
}

/** The fields with no place for their error in the form as it is. */
function hiddenFieldsOf(
  draft: OnlineBookingOptionsDraft,
): readonly (keyof OnlineBookingOptionsDraft)[] {
  return [
    'allowedSpotIds',
    'isBlocked',
    ...(draft.isPeriodRestricted ? [] : (['allowedPeriods'] as const)),
    ...(draft.allowsHalfDay
      ? []
      : (['morningFrom', 'morningTo', 'afternoonFrom', 'afternoonTo'] as const)),
    ...(draft.isAfternoonConversionEnabled ? [] : (['afternoonConversionTime'] as const)),
    ...(draft.surchargeType === SupplementType.None ? (['surchargeValue'] as const) : []),
  ];
}
