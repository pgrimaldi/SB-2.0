import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
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
export class ServicesLimits {
  /** The rows and sectors of the form of the panel, which validates and saves them. */
  readonly form = input.required<FieldTree<string[]>>();

  /** Example rows until an API gives the rows and sectors of the property. */
  protected readonly sectorOptions: readonly SelectOption[] = [
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e01', label: 'Fila 1' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e02', label: 'Fila 2' },
    { value: '7c0e2a51-3b9f-4d6e-8a21-5f4b9c1d0e03', label: 'Fila 3' },
  ];
}
