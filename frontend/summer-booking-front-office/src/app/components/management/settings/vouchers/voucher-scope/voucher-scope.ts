import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../behaviours/forms/dropdown-list';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { DiscountScope } from '../../../../../entities/enums/discount-scope';
import { DiscountServiceCategory } from '../../../../../entities/enums/discount-service-category';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { FormRow } from '../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../shared/ui/forms/form-row/form-row-label';
import { FilledMultiSelect } from '../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';
import { optionsWithMissing } from '../../catalog/options-with-missing';

interface ScopeTexts {
  scopes: Record<'all' | 'custom' | 'without_custom', string>;
  missing_sector: string;
}

const CATEGORY_KEYS: Readonly<Record<DiscountServiceCategory, string>> = {
  [DiscountServiceCategory.Bar]: 'management.settings.services.tabs.bar_services',
  [DiscountServiceCategory.Normal]: 'management.settings.services.tabs.services',
  [DiscountServiceCategory.Hourly]: 'management.settings.services.tabs.hourly_services',
  [DiscountServiceCategory.Additional]: 'management.settings.catalog.tabs.additional_items',
};

@Component({
  selector: 'app-voucher-scope',
  imports: [FilledMultiSelect, FilledSelect, FormField, FormRow, FormRowLabel, TranslatePipe],
  templateUrl: './voucher-scope.html',
  styleUrl: './voucher-scope.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoucherScope implements OnDestroy {
  readonly scope = input.required<FieldTree<DiscountScope>>();
  readonly categories = input.required<FieldTree<DiscountServiceCategory[]>>();
  readonly sectors = input.required<FieldTree<string[]>>();
  /** When it changes the sectors are read again, e.g. because one is no longer there. */
  readonly sectorsVersion = input(0);

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly isLimited = computed(() => this.scope()().value() !== DiscountScope.All);
  protected readonly sectorList = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorCombobox() : EMPTY),
    this.errorText,
  );
  private readVersion: number | null = null;
  private readonly texts = toSignal(
    this.translateService.stream(
      'management.settings.vouchers.form.options',
    ) as Observable<ScopeTexts>,
    { initialValue: null },
  );
  private readonly categoryNames = toSignal(
    this.translateService.stream(Object.values(CATEGORY_KEYS)) as Observable<
      Record<string, string>
    >,
    { initialValue: null },
  );
  protected readonly scopeOptions = computed<readonly SelectOption<DiscountScope>[]>(() => [
    { value: DiscountScope.All, label: this.texts()?.scopes.all ?? '' },
    { value: DiscountScope.Custom, label: this.texts()?.scopes.custom ?? '' },
    { value: DiscountScope.WithoutCustom, label: this.texts()?.scopes.without_custom ?? '' },
  ]);
  protected readonly categoryOptions = computed<readonly SelectOption<DiscountServiceCategory>[]>(
    () =>
      Object.values(DiscountServiceCategory).map((category) => ({
        value: category,
        label: this.categoryNames()?.[CATEGORY_KEYS[category]] ?? '',
      })),
  );
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    optionsWithMissing(
      this.sectorList,
      this.sectors()().value(),
      this.texts()?.missing_sector ?? '',
    ),
  );

  constructor() {
    effect(() => {
      const version = this.sectorsVersion();
      if (this.isLimited() && version !== this.readVersion) {
        this.readVersion = version;
        untracked(() => this.sectorList.load());
      }
    });
  }

  ngOnDestroy(): void {
    this.sectorList.destroy();
  }
}
