import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { AuthBehaviour } from '../../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../../behaviours/errors/error-text.behaviour';
import { ComboboxItem } from '../../../../../../entities/combobox/combobox-item';
import { SectorService } from '../../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../../services/api/errors/to-api-problem';
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

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);

  private readonly sectors = signal<readonly ComboboxItem[]>([]);
  protected readonly sectorsLoadError = signal<string | null>(null);
  private readonly sectorsSubscription?: Subscription;
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    this.sectors().map(({ id, value }) => ({ value: id, label: value })),
  );

  /** Read every time the section is shown, i.e. at every opening of the Prices tab. */
  constructor() {
    const idProperty = this.auth.user()?.propertyId;
    if (idProperty) {
      this.sectorsSubscription = this.sectorService.sectorCombobox(idProperty).subscribe({
        next: (sectors) => this.sectors.set(sectors),
        error: (error: unknown) => {
          const problem = toApiProblem(error);
          this.sectors.set([]);
          this.sectorsLoadError.set(this.errorText.instant(problem.code, problem.args));
        },
      });
    }
  }

  ngOnDestroy(): void {
    this.sectorsSubscription?.unsubscribe();
  }
}
