import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { FieldTree } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import {
  DATE_FORMATS,
  DateLanguageBehaviour,
} from '../../../../../behaviours/i18n/date-language.behaviour';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { IconButton } from '../../../../shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { ExpansionPanel } from '../../../../shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { PublicHolidayAddPopup } from '../public-holiday-add-popup/public-holiday-add-popup';

@Component({
  selector: 'app-public-holidays',
  imports: [
    Button,
    Checkbox,
    ConfirmPopup,
    ExpansionPanel,
    IconButton,
    PublicHolidayAddPopup,
    TranslatePipe,
  ],
  templateUrl: './public-holidays.html',
  styleUrl: './public-holidays.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicHolidays {
  readonly isIncluded = input.required<FieldTree<boolean>>();
  /** The days, at midnight UTC as ISO strings, in order. */
  readonly dates = input.required<FieldTree<string[]>>();

  private readonly dateLanguage = inject(DateLanguageBehaviour);
  protected readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly addIcon = ['/assets/images/GeneralIcon_AddBlue.svg'] as const;
  protected readonly deleteIcon = ['/assets/images/GeneralIcon_Delete.svg'] as const;
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [before the title, arrow]. */
  protected readonly sectionIcons = ['', '/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly isAdding = signal(false);
  protected readonly isIncludedValue = computed(() => this.isIncluded()().value());
  /** What the checkbox shows: unticked while the popup asks, its value again after the answer. */
  protected readonly isChecked = linkedSignal(() => this.isIncludedValue());
  protected readonly isCheckboxLocked = computed(
    () => this.isIncluded()().readonly() || this.isIncluded()().disabled(),
  );
  protected readonly isAskingOff = signal(false);
  /** Closed at every change of the checkbox: the section opens only while it is ticked. */
  protected readonly isExpanded = linkedSignal({
    source: this.isIncludedValue,
    computation: () => false,
  });
  /** The package or item is only to look at (or being saved). */
  protected readonly isLocked = computed(
    () => this.dates()().readonly() || this.dates()().disabled(),
  );
  private readonly dayFormat = computed(
    () =>
      new Intl.DateTimeFormat(this.dateLanguage.locale(), {
        ...DATE_FORMATS.display.dateInput,
        timeZone: 'UTC',
      }),
  );

  protected day(date: string): string {
    return this.dayFormat().format(new Date(date));
  }

  /** Ticking turns the option on at once; unticking asks first, as it deletes the days. */
  protected toggle(checked: boolean): void {
    this.isChecked.set(checked);
    if (checked) {
      this.isIncluded()().value.set(true);
      this.isIncluded()().markAsTouched();
    } else {
      this.isAskingOff.set(true);
    }
  }

  protected confirmOff(): void {
    this.isIncluded()().value.set(false);
    this.isIncluded()().markAsTouched();
    this.dates()().value.set([]);
    this.dates()().markAsTouched();
    this.isAskingOff.set(false);
  }

  protected closeOff(isOpen: boolean): void {
    if (!isOpen) {
      this.isAskingOff.set(false);
      this.isChecked.set(this.isIncludedValue());
    }
  }

  protected add(added: readonly string[]): void {
    this.dates()().value.update((dates) => [...new Set([...dates, ...added])].sort());
    this.dates()().markAsTouched();
  }

  protected remove(removed: string): void {
    this.dates()().value.update((dates) => dates.filter((date) => date !== removed));
    this.dates()().markAsTouched();
  }
}
