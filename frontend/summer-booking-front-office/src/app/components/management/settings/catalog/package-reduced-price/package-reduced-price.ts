import { ChangeDetectionStrategy, Component, computed, inject, input, model } from '@angular/core';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../../../behaviours/i18n/date-language.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { PackageReducedSeasonalPrice } from '../../../../../entities/settings/catalog/package-reduced-seasonal-price';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { FilledDateField } from '../../../../shared/ui/inputs/filled-date-field/filled-date-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

@Component({
  selector: 'app-package-reduced-price',
  imports: [Button, FilledDateField, FilledTextField, FormField, IconButton, TranslatePipe],
  templateUrl: './package-reduced-price.html',
  styleUrl: './package-reduced-price.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PackageReducedPrice {
  /** The row of the package: null until Aggiungi. */
  readonly reducedSeasonalPrice = input.required<FieldTree<PackageReducedSeasonalPrice | null>>();
  /** The fields of the box, kept by the panel so that they outlive the tab. */
  readonly draftForm = input.required<FieldTree<PackageReducedSeasonalPrice>>();
  readonly isOpen = model.required<boolean>();

  private readonly dateLanguage = inject(DateLanguageBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly calendarIcon = ['/assets/images/GeneralIcon_Calendar.svg'] as const;
  protected readonly editIcon = ['/assets/images/Manager_Table_Edit.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly row = computed(() => this.reducedSeasonalPrice()().value());
  /** An error of the whole price (e.g. from the backend), under it. */
  protected readonly error = computed(() => this.validationText.text(this.reducedSeasonalPrice()));
  /** The package is only to look at (or being saved). */
  protected readonly isLocked = computed(
    () => this.reducedSeasonalPrice()().readonly() || this.reducedSeasonalPrice()().disabled(),
  );
  private readonly dayFormat = computed(
    () =>
      new Intl.DateTimeFormat(this.dateLanguage.locale(), {
        ...DATE_FORMATS.display.dateInput,
        timeZone: 'UTC',
      }),
  );

  protected day(date: string | null): string {
    return date ? this.dayFormat().format(new Date(date)) : '';
  }

  /** Only one price: the button stays off while the box or the row is there. */
  protected add(): void {
    this.edit(new PackageReducedSeasonalPrice());
  }

  protected edit(price: PackageReducedSeasonalPrice): void {
    this.draftForm()().value.set({ ...price });
    this.draftForm()().reset();
    this.isOpen.set(true);
  }

  protected close(): void {
    this.isOpen.set(false);
  }

  protected save(): void {
    this.reducedSeasonalPrice()().value.set({ ...this.draftForm()().value() });
    this.isOpen.set(false);
  }

  protected delete(): void {
    this.reducedSeasonalPrice()().value.set(null);
  }
}
