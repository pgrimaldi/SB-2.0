import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { FormField, form } from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { AddPrinterItemRequest } from '../../../../../entities/settings/taxation/add-printer-item-request';
import { Button, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { Checkbox } from '../../../../shared/ui/checkboxes/checkbox/checkbox';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledNumberField } from '../../../../shared/ui/inputs/filled-number-field/filled-number-field';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';
import { FilledSelect } from '../../../../shared/ui/selects/filled-select/filled-select';
import { SelectOption } from '../../../../shared/ui/selects/select/select';

const VAT_RATES = ['0', '4', '5', '10', '22'];

@Component({
  selector: 'app-add-printer-item-popup',
  imports: [
    Button,
    Checkbox,
    FilledNumberField,
    FilledSelect,
    FilledTextField,
    FormField,
    TranslatePipe,
  ],
  templateUrl: './add-printer-item-popup.html',
  styleUrl: './add-printer-item-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // The popup is rendered by Material in an overlay outside this component.
  encapsulation: ViewEncapsulation.None,
})
export class AddPrinterItemPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonSize = ButtonSize;
  protected readonly vatRateOptions: readonly SelectOption[] = VAT_RATES.map((rate) => ({
    value: rate,
    label: `${rate}%`,
  }));
  private readonly draft = signal(new AddPrinterItemRequest());
  protected readonly draftForm = form(this.draft);
  protected readonly closeIcon = '/assets/images/CloseAddPrinter_setting.svg';

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      if (this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return (
      this.open() && JSON.stringify(this.draft()) !== JSON.stringify(new AddPrinterItemRequest())
    );
  }

  protected async leave(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.close();
    }
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(32rem, 90vw)',
      maxWidth: 'none',
      maxHeight: '90vh',
      panelClass: 'add__printer__item__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private reset(): void {
    this.draft.set(new AddPrinterItemRequest());
    this.draftForm().reset();
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
