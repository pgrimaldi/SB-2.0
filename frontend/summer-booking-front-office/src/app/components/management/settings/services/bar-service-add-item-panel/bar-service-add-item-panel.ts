import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  model,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { form } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, finalize, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { ServiceGeneralInfo } from '../../../../../entities/settings/services/service-general-info';
import { ServiceIconItem } from '../../../../../entities/settings/services/service-icon-item';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import {
  SERVICE_LANGUAGES,
  ServiceGeneralInfoTab,
} from '../service-general-info-tab/service-general-info-tab';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
    prices: string;
    bookings: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
  Prices,
  Bookings,
}

@Component({
  selector: 'app-bar-service-add-item-panel',
  imports: [ServiceGeneralInfoTab, SidePanel, TabGroup, TranslatePipe],
  templateUrl: './bar-service-add-item-panel.html',
  styleUrl: './bar-service-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarServiceAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly auth = inject(AuthBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly language = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly PanelTab = PanelTab;
  protected readonly mainLanguage = this.language.current;
  protected readonly selectedTab = signal<number>(PanelTab.GeneralInfo);
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.services.form',
    ) as Observable<ServiceFormTexts>,
    { initialValue: null },
  );
  protected readonly tabs = computed<readonly TabGroupTab[]>(() => [
    { label: this.texts()?.tabs.general_info ?? '', widthPercent: 34 },
    { label: this.texts()?.tabs.prices ?? '', widthPercent: 33 },
    { label: this.texts()?.tabs.bookings ?? '', widthPercent: 33 },
  ]);
  private readonly draft = signal(newGeneralInfo());
  protected readonly draftForm = form(this.draft);
  protected readonly categories = signal<readonly SelectOption[]>([]);
  protected readonly icons = signal<readonly ServiceIconItem[]>([]);
  protected readonly isFetching = signal(false);
  /** The image chosen from the computer, instead of an icon of the list. */
  private readonly image = signal<File | null>(null);
  protected readonly imagePreviewUrl = signal<string | null>(null);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private fetching?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() &&
      (this.image() !== null || JSON.stringify(this.draft()) !== JSON.stringify(newGeneralInfo()))
    );
  }

  protected chooseIcon(idIcon: number): void {
    this.draftForm.idIcon().value.set(idIcon);
    this.setImage(null);
  }

  protected chooseImage(image: File): void {
    this.draftForm.idIcon().value.set(null);
    this.setImage(image);
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  private reset(): void {
    this.draft.set(newGeneralInfo());
    this.draftForm().reset();
    this.setImage(null);
    this.selectedTab.set(PanelTab.GeneralInfo);
    this.fetchChoices();
  }

  /** The categories of the property and the icons a bar service can have. */
  private fetchChoices(): void {
    this.fetching?.unsubscribe();
    this.error.set(null);
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.isFetching.set(true);
    this.fetching = forkJoin({
      categories: this.catalog.barServiceCategoryCombobox(idProperty),
      icons: this.catalog.barServiceIconList(),
    })
      .pipe(finalize(() => this.isFetching.set(false)))
      .subscribe({
        next: ({ categories, icons }) => {
          this.categories.set(categories.map(({ id, value }) => ({ value: id, label: value })));
          this.icons.set(icons);
        },
        error: (error: unknown) => this.error.set(toApiProblem(error)),
      });
  }

  private setImage(image: File | null): void {
    const previewUrl = this.imagePreviewUrl();
    if (previewUrl) {
      URL.revokeObjectURL(previewUrl);
    }
    this.image.set(image);
    this.imagePreviewUrl.set(image ? URL.createObjectURL(image) : null);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.fetching?.unsubscribe();
    this.setImage(null);
  }
}

/** Empty, with a name and a description for every language: the user's one is the main. */
function newGeneralInfo(): ServiceGeneralInfo {
  return Object.assign(new ServiceGeneralInfo(), {
    translations: SERVICE_LANGUAGES.map((language) =>
      Object.assign(new ServiceTranslation(), { language }),
    ),
  });
}
