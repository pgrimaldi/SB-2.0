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
import { FormField, TreeValidationResult, form, required, submit } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, finalize, firstValueFrom, forkJoin, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ComboboxItem } from '../../../../../entities/combobox/combobox-item';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { AdditionalItemAddItemRequest } from '../../../../../entities/settings/catalog/additional-item-add-item-request';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { PackageSectorPrice } from '../../../../../entities/settings/catalog/package-sector-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { PriceListService } from '../../../../../services/api/catalog/price-list.service';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { SeasonService } from '../../../../../services/api/seasons/season.service';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { SidePanel } from '../../../../shared/ui/dialogs/side-panel/side-panel';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';
import { PriceListTab } from '../price-list-tab/price-list-tab';

@Component({
  selector: 'app-additional-item-add-item-panel',
  imports: [
    Checkbox,
    FilledMultiSelect,
    FilledSelect,
    FormField,
    PriceListTab,
    SectionTitle,
    SidePanel,
    TranslatePipe,
  ],
  templateUrl: './additional-item-add-item-panel.html',
  styleUrl: './additional-item-add-item-panel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdditionalItemAddItemPanel implements UnsavedChanges, OnDestroy {
  readonly open = model(false);
  /** Icons as image paths: [X]. */
  readonly pathIcon = input<readonly string[] | null>();
  readonly created = output<void>();

  private readonly auth = inject(AuthBehaviour);
  private readonly priceLists = inject(PriceListService);
  private readonly warehouse = inject(WarehouseService);
  private readonly sectorService = inject(SectorService);
  private readonly seasons = inject(SeasonService);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  private readonly draft = signal(new AdditionalItemAddItemRequest());
  protected readonly draftForm = form(this.draft, (path) => {
    required(path.idArticle);
  });
  /** The form with nothing chosen: with an empty price for every row once the rows are read. */
  private readonly pristine = signal(JSON.stringify(new AdditionalItemAddItemRequest()));
  private readonly articles = signal<readonly ComboboxItem[]>([]);
  protected readonly articlesLoadError = signal<string | null>(null);
  /** The name of the chosen article, in the rows of the intermediate prices. */
  protected readonly articleName = computed(
    () => this.articles().find(({ id }) => id === this.draft().idArticle)?.value ?? '',
  );
  protected readonly articleOptions = computed<readonly SelectOption[]>(() =>
    this.articles().map(({ id, value }) => ({ value: id, label: value })),
  );
  private readonly packages = signal<readonly ComboboxItem[]>([]);
  protected readonly packagesLoadError = signal<string | null>(null);
  protected readonly packageOptions = computed<readonly SelectOption[]>(() =>
    this.packages().map(({ id, value }) => ({ value: id, label: value })),
  );
  protected readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly periods = signal<readonly SeasonPeriodItem[]>([]);
  protected readonly isPriceListLoaded = signal(false);
  protected readonly isFetching = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly isSaveDisabled = computed(
    () => this.isFetching() || this.draftForm().invalid(),
  );
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  private loadings = new Subscription();

  constructor() {
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && JSON.stringify(this.draft()) !== this.pristine();
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isSaving() || !idProperty) {
      return;
    }

    void submit(this.draftForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.priceLists.createAdditionalItem(idProperty, this.draft()));
        this.isSaving.set(false);
        this.open.set(false);
        this.created.emit();
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

  private reset(): void {
    this.loadings.unsubscribe();
    this.loadings = new Subscription();
    this.isFetching.set(false);
    this.isSaving.set(false);
    this.error.set(null);
    this.draft.set(new AdditionalItemAddItemRequest());
    this.draftForm().reset();
    this.pristine.set(JSON.stringify(new AdditionalItemAddItemRequest()));
    this.articles.set([]);
    this.articlesLoadError.set(null);
    this.packages.set([]);
    this.packagesLoadError.set(null);
    this.sectors.set([]);
    this.periods.set([]);
    this.isPriceListLoaded.set(false);
    this.load();
  }

  /** The articles and the packages of the dropdowns, the sectors and the periods of the price rows. */
  private load(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (!idProperty) {
      return;
    }
    this.loadings.add(
      this.warehouse.articleCombobox(idProperty).subscribe({
        next: (articles) => this.articles.set(articles),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.articles.set([]);
          this.articlesLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      }),
    );
    this.loadings.add(
      this.priceLists.packageCombobox(idProperty).subscribe({
        next: (packages) => this.packages.set(packages),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.packages.set([]);
          this.packagesLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      }),
    );
    this.isFetching.set(true);
    this.loadings.add(
      forkJoin([
        this.sectorService.sectorCombobox(idProperty),
        this.seasons.periodList(idProperty, false),
      ])
        .pipe(finalize(() => this.isFetching.set(false)))
        .subscribe({
          next: ([sectors, periods]) => {
            this.setPriceRows(sectors, periods);
            this.isPriceListLoaded.set(true);
          },
          error: (error: unknown) => this.error.set(toApiProblem(error)),
        }),
    );
  }

  /** An empty price for every sector, and for every sector in every period. */
  private setPriceRows(
    sectors: readonly ComboboxItem[],
    periods: readonly SeasonPeriodItem[],
  ): void {
    const sectorPrices = sectors.map(({ id }) =>
      Object.assign(new PackageSectorPrice(), { idSector: id }),
    );
    const periodPrices = sectors.flatMap(({ id }) =>
      periods.map(({ idPeriod }) =>
        Object.assign(new PackagePeriodPrice(), { idSector: id, idPeriod }),
      ),
    );
    this.sectors.set(sectors);
    this.periods.set(periods);
    this.draftForm.sectorPrices().value.set(sectorPrices);
    this.draftForm.periodPrices().value.set(periodPrices);
    this.pristine.set(
      JSON.stringify(
        Object.assign(new AdditionalItemAddItemRequest(), { sectorPrices, periodPrices }),
      ),
    );
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    this.loadings.unsubscribe();
  }
}
