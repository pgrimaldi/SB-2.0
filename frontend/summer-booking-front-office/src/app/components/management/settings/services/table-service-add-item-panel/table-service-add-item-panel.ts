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
import { toSignal } from '@angular/core/rxjs-interop';
import { applyEach, applyWhen, form } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { ServiceTranslation } from '../../../../../entities/settings/services/service-translation';
import { TableServiceAddItemRequest } from '../../../../../entities/settings/services/table-service-add-item-request';
import { requiredText } from '../../../../shared/forms/validators';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { TabGroup, TabGroupTab } from '../../../../shared/ui/tabs/tab-group/tab-group';
import { SERVICE_LANGUAGES } from '../shared/services-translations/services-translations';
import { TableServiceGeneralInfoTab } from '../table-service-general-info-tab/table-service-general-info-tab';

interface ServiceFormTexts {
  tabs: {
    general_info: string;
  };
}

/** The tabs of the panel, in their order. */
enum PanelTab {
  GeneralInfo,
}

@Component({
  selector: 'app-table-service-add-item-panel',
  imports: [SidePanel, TabGroup, TableServiceGeneralInfoTab, TranslatePipe],
  templateUrl: './table-service-add-item-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();

  private readonly language = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);

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
    { label: this.texts()?.tabs.general_info ?? '' },
  ]);
  private readonly draft = signal(newDraft());
  protected readonly draftForm = form(this.draft, (path) => {
    applyEach(path.translations, (translation) => {
      applyWhen(
        translation,
        ({ valueOf }) => valueOf(translation.language) === this.mainLanguage(),
        (main) => {
          requiredText(main.serviceName);
          requiredText(main.description);
        },
      );
    });
  });
  /** The form as it was when the panel opened. */
  private readonly pristine = JSON.stringify(newDraft());
  protected readonly isSaveDisabled = computed(() => this.draftForm().invalid());

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== this.pristine;
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.open.set(false);
    }
  }

  private reset(): void {
    this.draft.set(newDraft());
    this.draftForm().reset();
    this.selectedTab.set(PanelTab.GeneralInfo);
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
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
