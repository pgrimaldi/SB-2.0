import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  TreeValidationResult,
  applyEach,
  applyWhen,
  disabled,
  form,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { TableServiceAvailabilityMode } from '../../../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../../../entities/enums/table-service-booking-payment';
import { TableServiceDepositMode } from '../../../../../entities/enums/table-service-deposit-mode';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { TableServiceAvailability } from '../../../../../entities/settings/services/table-service-availability';
import { TableServiceAvailabilityDay } from '../../../../../entities/settings/services/table-service-availability-day';
import { TableServiceSelectedAvailabilityBookings } from '../../../../../entities/settings/services/table-service-selected-availability-bookings';
import { TableServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/table-service-selected-general-info';
import { TableServiceTableGroup } from '../../../../../entities/settings/services/table-service-table-group';
import { TableServiceUpdateAvailabilityBookingsRequest } from '../../../../../entities/settings/services/table-service-update-availability-bookings-request';
import { TableServiceUpdateGeneralRequest } from '../../../../../entities/settings/services/table-service-update-general-request';
import { DAYS_OF_WEEK } from '../../../../../entities/shared/days-of-week';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SidePanelButtons } from '../../../../shared/ui/dialogs/side-panel/side-panel-buttons';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { TableServiceAvailabilityBookingsTab } from '../table-service-availability-bookings-tab/table-service-availability-bookings-tab';
import { TableServiceGeneralInfoTab } from '../table-service-general-info-tab/table-service-general-info-tab';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
    availability_bookings: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  AvailabilityBookings,
}

/** The fields of the form each tab shows, reads and saves. */
const TAB_FIELDS: Readonly<Record<PanelTab, readonly (keyof TableServiceAddItemRequest)[]>> = {
  [PanelTab.GeneralInfo]: ['translations', 'isHighlighted'],
  [PanelTab.AvailabilityBookings]: [
    'availabilities',
    'availabilityMode',
    'tableGroups',
    'totalSeatQuantity',
    'averageMealDuration',
    'maxBookingPerTimeSlotQuantity',
    'bookingPayment',
    'depositMode',
    'depositPercentage',
    'isOnlineBookingEnabled',
  ],
};

@Component({
  selector: 'app-table-service-edit-item-panel',
  imports: [
    Button,
    SidePanel,
    SidePanelButtons,
    TabGroup,
    TableServiceAvailabilityBookingsTab,
    TableServiceGeneralInfoTab,
    TranslatePipe,
  ],
  templateUrl: './table-service-edit-item-panel.html',
  styleUrl: './table-service-edit-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceEditItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  readonly idTableService = input<string | null>(null);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly updated = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly language = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly PanelTab = PanelTab;
  protected readonly mainLanguage = this.language.current;
  protected readonly selectedTab = signal<number>(PanelTab.GeneralInfo);
  /** The tab marked in the bar: back to `selectedTab` when the user stays on the tab being edited. */
  protected readonly tabIndex = signal<number>(PanelTab.GeneralInfo);
  /** Modifica pressed: the fields of the tab can be changed, until Annulla or Salva. */
  protected readonly isEditing = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isLocked = computed(() => !this.isEditing() || this.isSaving());
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form',
    ) as Observable<ServiceFormTexts>,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabGroupTab[]>(() => [
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 50 },
    { label: this.texts()?.tabs.availability_bookings ?? '', widthPercent: 50 },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
    disabled(path, () => this.isLocked());
    applyEach(path.translations, (translation) => {
      applyWhen(
        translation,
        ({ valueOf }) =>
          this.selectedTab() === PanelTab.GeneralInfo &&
          valueOf(translation.language) === this.mainLanguage(),
        (main) => {
          requiredText(main.serviceName);
          requiredText(main.description);
        },
      );
    });
    applyEach(path.availabilities, (availability) => {
      disabled(
        availability.startTime,
        ({ valueOf }) => !valueOf(availability.isSameTimeForAllDays),
      );
      disabled(availability.endTime, ({ valueOf }) => !valueOf(availability.isSameTimeForAllDays));
      applyEach(availability.days, (day) => {
        disabled(day.startTime, ({ valueOf }) => valueOf(availability.isSameTimeForAllDays));
        disabled(day.endTime, ({ valueOf }) => valueOf(availability.isSameTimeForAllDays));
      });
    });
  });
  /** The values as the APIs gave them, tab by tab: the form is compared with them. */
  private readonly pristine = signal(newDraft());
  /** The tabs read (or being read) since the panel opened: changing tab keeps their data. */
  private readonly loadedTabs = signal<ReadonlySet<PanelTab>>(new Set());
  /** The tabs whose data are being read: their fields cannot be used meanwhile. */
  private readonly loadingTabs = signal<ReadonlySet<PanelTab>>(new Set());
  protected readonly isEditDisabled = computed(() => {
    const tab = this.selectedTab() as PanelTab;
    return this.loadingTabs().has(tab) || !this.loadedTabs().has(tab);
  });
  protected readonly isSaveDisabled = computed(
    () => this.loadingTabs().has(this.selectedTab()) || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private readonly loadings = new Map<PanelTab, Subscription>();

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== JSON.stringify(this.pristine());
  }

  protected isLoading(tab: PanelTab): boolean {
    return this.loadingTabs().has(tab);
  }

  protected startEdit(): void {
    this.error.set(null);
    this.isEditing.set(true);
  }

  /** Annulla: the tab goes back to the values read, only to look at. */
  protected cancelEdit(): void {
    const tab = this.selectedTab() as PanelTab;
    const pristine = structuredClone(this.pristine());
    this.draft.update((draft) => ({ ...draft, ...pick(pristine, tab) }));
    this.draftForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  /**
   * Another tab while editing: with changes, first the question about losing them; staying, the bar
   * marks the tab being edited again.
   */
  protected async requestTab(index: number): Promise<void> {
    const current = this.selectedTab();
    if (index === current) {
      return;
    }
    if (this.isEditing()) {
      if (!(await this.unsaved.confirmDiscard(this))) {
        this.tabIndex.set(current);
        return;
      }
      this.cancelEdit();
    }
    this.selectedTab.set(index);
    this.tabIndex.set(index);
    this.loadTab(index);
  }

  /** Salva: only the tab being edited, with its API. */
  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    const idTableService = this.idTableService();
    if (this.isSaving() || !idProperty || !idTableService) {
      return;
    }
    const tab = this.selectedTab() as PanelTab;
    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.update(tab, idProperty, idTableService));
        this.isSaving.set(false);
        this.open.set(false);
        this.updated.emit();
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
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  /** The data of a tab, read from its API the first time the tab is opened. */
  private loadTab(tab: PanelTab): void {
    const idTableService = this.idTableService();
    if (!idTableService || this.loadedTabs().has(tab)) {
      return;
    }
    this.loadedTabs.update((tabs) => new Set(tabs).add(tab));
    this.error.set(null);
    this.setLoading(tab, true);
    if (tab === PanelTab.GeneralInfo) {
      this.loadings.set(
        tab,
        this.catalog
          .tableServiceGeneralInfo(idTableService)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (info) => this.setGeneralInfo(info),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    } else {
      this.loadings.set(
        tab,
        this.catalog
          .tableServiceAvailabilityBookings(idTableService)
          .pipe(finalize(() => this.setLoading(tab, false)))
          .subscribe({
            next: (values) => this.setAvailabilityBookings(values),
            error: (error: unknown) => this.loadFailed(tab, error),
          }),
      );
    }
  }

  /** The tab is read again the next time it is opened. */
  private loadFailed(tab: PanelTab, error: unknown): void {
    this.loadedTabs.update((tabs) => {
      const next = new Set(tabs);
      next.delete(tab);
      return next;
    });
    this.error.set(toApiProblem(error));
  }

  /** The update API of the tab, with the fields of that tab only. */
  private update(tab: PanelTab, idProperty: string, idTableService: string): Observable<void> {
    const draft = this.draft();
    if (tab === PanelTab.GeneralInfo) {
      return this.catalog.updateTableServiceGeneral(
        idProperty,
        Object.assign(new TableServiceUpdateGeneralRequest(), {
          idTableService,
          ...pick(draft, tab),
        }),
      );
    }
    return this.catalog.updateTableServiceAvailabilityBookings(
      idProperty,
      Object.assign(new TableServiceUpdateAvailabilityBookingsRequest(), {
        idTableService,
        ...pick(draft, tab),
      }),
    );
  }

  private reset(): void {
    this.loadings.forEach((loading) => loading.unsubscribe());
    this.loadings.clear();
    this.loadedTabs.set(new Set());
    this.loadingTabs.set(new Set());
    this.error.set(null);
    this.isEditing.set(false);
    this.isSaving.set(false);
    this.draft.set(newDraft());
    this.pristine.set(newDraft());
    this.draftForm().reset();
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.tabIndex.set(PanelTab.GeneralInfo);
    this.loadTab(PanelTab.GeneralInfo);
  }

  /** Name and description in every language, also those the API leaves out (empty). */
  private setGeneralInfo(info: TableServiceSelectedGeneralInfo): void {
    this.applyLoaded({
      translations: SERVICE_LANGUAGES.map((language) => {
        const translation = info.translations.find((item) => item.language === language);
        return Object.assign(new ServiceTranslation(), {
          language,
          serviceName: translation?.serviceName ?? null,
          description: translation?.description ?? null,
        });
      }),
      isHighlighted: info.isHighlighted ?? false,
    });
  }

  /** Every availability with its seven days, also those the API leaves out (unticked). */
  private setAvailabilityBookings(values: TableServiceSelectedAvailabilityBookings): void {
    this.applyLoaded({
      availabilities: values.availabilities.map((availability) =>
        Object.assign(new TableServiceAvailability(), availability, {
          days: DAYS_OF_WEEK.map(({ number }) =>
            Object.assign(
              new TableServiceAvailabilityDay(),
              availability.days.find((day) => day.dayOfWeek === number),
              { dayOfWeek: number },
            ),
          ),
        }),
      ),
      availabilityMode: values.availabilityMode ?? TableServiceAvailabilityMode.Tables,
      tableGroups: values.tableGroups.map((tableGroup) =>
        Object.assign(new TableServiceTableGroup(), tableGroup),
      ),
      totalSeatQuantity: values.totalSeatQuantity,
      averageMealDuration: values.averageMealDuration,
      maxBookingPerTimeSlotQuantity: values.maxBookingPerTimeSlotQuantity,
      bookingPayment: values.bookingPayment ?? TableServiceBookingPayment.None,
      depositMode: values.depositMode ?? TableServiceDepositMode.Total,
      depositPercentage: values.depositPercentage,
      isOnlineBookingEnabled: values.isOnlineBookingEnabled ?? false,
    });
  }

  /** Values read from an API: into the form, and into what the form is compared with. */
  private applyLoaded(values: Partial<TableServiceAddItemRequest>): void {
    this.draft.update((draft) => ({ ...draft, ...values }));
    this.pristine.update((pristine) => ({ ...pristine, ...structuredClone(values) }));
  }

  private setLoading(tab: PanelTab, isLoading: boolean): void {
    this.loadingTabs.update((tabs) => {
      const next = new Set(tabs);
      if (isLoading) {
        next.add(tab);
      } else {
        next.delete(tab);
      }
      return next;
    });
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loadings.forEach((loading) => loading.unsubscribe());
  }
}

/** Empty, with a name and a description for every language: the user's one is the main. */
function newDraft(): TableServiceAddItemRequest {
  return Object.assign(new TableServiceAddItemRequest(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}

/** The values of the fields of one tab. */
function pick(
  values: TableServiceAddItemRequest,
  tab: PanelTab,
): Partial<TableServiceAddItemRequest> {
  return Object.fromEntries(TAB_FIELDS[tab].map((field) => [field, values[field]]));
}
