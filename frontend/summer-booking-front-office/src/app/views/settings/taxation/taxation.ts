import {
  ChangeDetectionStrategy,
  Component,
  Signal,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { IconButton } from '../../../components/shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { ExpansionPanel } from '../../../components/shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { Toggle } from '../../../components/shared/ui/toggles/toggle/toggle';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../services/api/taxation/taxation.service';

@Component({
  selector: 'app-taxation',
  imports: [Button, Checkbox, ExpansionPanel, IconButton, Toggle, TranslatePipe],
  templateUrl: './taxation.html',
  styleUrl: './taxation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Taxation {
  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });

  /** [before the title, arrow]. */
  protected readonly printerPanelIcons = [
    '/assets/images/Printer_setting.svg',
    '/assets/images/PanelChevron_setting.svg',
  ] as const;
  /** [before the title, arrow]. */
  protected readonly receiptPanelIcons = [
    '/assets/images/ElectronicReceipt_setting.svg',
    '/assets/images/PanelChevron_setting.svg',
  ] as const;
  protected readonly addPrinterIcon = ['/assets/images/AddPrinter_setting.svg'] as const;
  protected readonly deletePrinterIcon = ['/assets/images/DeletePrinter_setting.svg'] as const;
  protected readonly downloadGuideIcon = ['/assets/images/DownloadGuide_setting.svg'] as const;

  protected readonly printersExpanded = signal(true);
  protected readonly receiptExpanded = signal(false);
  protected readonly printers = signal<readonly PrinterItemListResponse[]>([]);
  protected readonly receipt = signal(new ElectronicReceiptResponse());
  private readonly printersError = signal<ApiProblem | null>(null);
  private readonly receiptError = signal<ApiProblem | null>(null);
  protected readonly printersErrorMessage = this.messageOf(this.printersError);
  protected readonly receiptErrorMessage = this.messageOf(this.receiptError);

  constructor() {
    effect((onCleanup) => {
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() =>
        this.taxation.printerList(idProperty).subscribe({
          next: (printers) => {
            this.printers.set(printers);
            this.printersError.set(null);
          },
          error: (error: unknown) => {
            this.printers.set([]);
            this.printersError.set(toApiProblem(error));
          },
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
    // Loaded again when the language changes: the server sends the terms translated.
    effect((onCleanup) => {
      this.dataReload?.();
      const idProperty = this.auth.user()?.idProperty;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() =>
        this.taxation.electronicReceipt(idProperty).subscribe({
          next: (receipt) => {
            this.receipt.set(receipt);
            this.receiptError.set(null);
          },
          error: (error: unknown) => {
            this.receipt.set(new ElectronicReceiptResponse());
            this.receiptError.set(toApiProblem(error));
          },
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
  }

  private messageOf(problem: Signal<ApiProblem | null>): Signal<string | null> {
    return toSignal(
      toObservable(problem).pipe(
        switchMap((current) => (current ? this.errorText.text(current) : of(null))),
      ),
      { initialValue: null },
    );
  }
}
