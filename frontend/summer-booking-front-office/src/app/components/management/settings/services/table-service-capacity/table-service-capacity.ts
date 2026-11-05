import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { TableServiceAvailabilityMode } from '../../../../../entities/enums/table-service-availability-mode';
import { TableServiceTableGroup } from '../../../../../entities/settings/services/table-service-table-group';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { RadioGroup, RadioOption } from '../../../../shared/ui/radios/radio-group/radio-group';
import { SectionTitle } from '../../../../shared/ui/titles/section-title/section-title';

interface ModeTexts {
  tables: string;
  seats: string;
}

/** "Disponibilità tavoli e posti": tables with their seats, or only the seats, as chosen. */
@Component({
  selector: 'app-table-service-capacity',
  imports: [
    Button,
    ConfirmPopup,
    FilledNumberField,
    FormField,
    IconButton,
    RadioGroup,
    SectionTitle,
    TranslatePipe,
  ],
  templateUrl: './table-service-capacity.html',
  styleUrl: './table-service-capacity.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TableServiceCapacity {
  /** The fields of the form of the panel, which validates and saves them. */
  readonly availabilityMode = input.required<FieldTree<TableServiceAvailabilityMode>>();
  readonly tableGroups = input.required<FieldTree<TableServiceTableGroup[]>>();
  readonly totalSeatQuantity = input.required<FieldTree<number | null>>();

  private readonly translateService = inject(TranslateService);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly TableServiceAvailabilityMode = TableServiceAvailabilityMode;
  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly isAskingModeChange = signal(false);
  private readonly modeTexts = toSignal(
    this.translateService.stream(
      'management.settings.services.form.tables.modes',
    ) as Observable<ModeTexts>,
    { initialValue: null },
  );
  protected readonly modes = computed<readonly RadioOption<TableServiceAvailabilityMode>[]>(() => [
    { value: TableServiceAvailabilityMode.Tables, label: this.modeTexts()?.tables ?? '' },
    { value: TableServiceAvailabilityMode.Seats, label: this.modeTexts()?.seats ?? '' },
  ]);

  /** With tables or seats typed, the other choice asks first: it starts empty. */
  protected changeMode(): void {
    if (this.hasQuantities()) {
      this.isAskingModeChange.set(true);
    }
  }

  protected confirmModeChange(): void {
    this.clearQuantities();
    this.isAskingModeChange.set(false);
  }

  /** Closed without changing: back to the choice that has the values, which stay. */
  protected closeModeChange(isOpen: boolean): void {
    this.isAskingModeChange.set(isOpen);
    if (!isOpen && this.hasQuantities()) {
      this.availabilityMode()().value.set(
        this.tableGroups()().value().length > 0
          ? TableServiceAvailabilityMode.Tables
          : TableServiceAvailabilityMode.Seats,
      );
    }
  }

  protected addTableGroup(): void {
    this.tableGroups()().value.update((tableGroups) => [
      ...tableGroups,
      new TableServiceTableGroup(),
    ]);
  }

  protected removeTableGroup(index: number): void {
    this.tableGroups()().value.update((tableGroups) =>
      tableGroups.filter((_, groupIndex) => groupIndex !== index),
    );
  }

  /** Only the chosen way has values: the other one was emptied when it was left. */
  private hasQuantities(): boolean {
    return this.tableGroups()().value().length > 0 || this.totalSeatQuantity()().value() !== null;
  }

  private clearQuantities(): void {
    this.tableGroups()().value.set([]);
    this.totalSeatQuantity()().value.set(null);
  }
}
