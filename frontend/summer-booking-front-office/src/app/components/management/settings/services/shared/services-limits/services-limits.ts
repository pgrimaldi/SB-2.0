import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { EMPTY, Observable } from 'rxjs';
import { AuthBehaviour } from '../../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../../behaviours/errors/error-text.behaviour';
import { DropdownList } from '../../../../../../behaviours/forms/dropdown-list';
import { SectorService } from '../../../../../../services/api/catalog/sector.service';
import { FormRow } from '../../../../../shared/ui/forms/form-row/form-row';
import { FormRowLabel } from '../../../../../shared/ui/forms/form-row/form-row-label';
import { FilledMultiSelect } from '../../../../../shared/ui/selects/filled-multi-select/filled-multi-select';
import { SelectOption } from '../../../../../shared/ui/selects/select/select';
import { SectionTitle } from '../../../../../shared/ui/titles/section-title/section-title';

@Component({
  selector: 'app-services-limits',
  imports: [FilledMultiSelect, FormField, FormRow, FormRowLabel, SectionTitle, TranslatePipe],
  templateUrl: './services-limits.html',
  styleUrl: './services-limits.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServicesLimits implements OnDestroy {
  /** The rows and sectors of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<string[]>>();
  /** When it changes the sectors are read again, e.g. because one is no longer there. */
  readonly sectorsVersion = input(0);

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly sectors = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorCombobox() : EMPTY),
    this.errorText,
    () => this.areSectorsRead.set(true),
  );
  private readonly areSectorsRead = signal(false);
  private readonly missingSectorText = toSignal(
    inject(TranslateService).stream(
      'management.settings.services.form.prices.limits.missing_sector',
    ) as Observable<string>,
    { initialValue: '' },
  );
  /** Also the chosen ones that are no longer there, for the user to take away. */
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() => {
    const sectors = this.sectors.items();
    const missing =
      this.areSectorsRead() && !this.sectors.loadError()
        ? this.form()()
            .value()
            .filter((idSector) => !sectors.some(({ id }) => id === idSector))
        : [];
    return [
      ...sectors.map(({ id, value }) => ({ value: id, label: value })),
      ...missing.map((idSector) => ({ value: idSector, label: this.missingSectorText() })),
    ];
  });

  /** Read every time the section is shown, i.e. at every opening of the Prices tab. */
  constructor() {
    effect(() => {
      this.sectorsVersion();
      untracked(() => this.sectors.load());
    });
  }

  ngOnDestroy(): void {
    this.sectors.destroy();
  }
}
