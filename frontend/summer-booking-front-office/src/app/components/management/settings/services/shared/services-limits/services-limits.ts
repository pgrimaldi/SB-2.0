import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  input,
} from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY } from 'rxjs';
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

  private readonly auth = inject(AuthBehaviour);
  private readonly sectorService = inject(SectorService);
  private readonly errorText = inject(ErrorTextBehaviour);

  protected readonly sectors = new DropdownList(
    () => (this.auth.user()?.propertyId ? this.sectorService.sectorCombobox() : EMPTY),
    this.errorText,
  );
  protected readonly sectorOptions = computed<readonly SelectOption[]>(() =>
    this.sectors.items().map(({ id, value }) => ({ value: id, label: value })),
  );

  /** Read every time the section is shown, i.e. at every opening of the Prices tab. */
  constructor() {
    this.sectors.load();
  }

  ngOnDestroy(): void {
    this.sectors.destroy();
  }
}
