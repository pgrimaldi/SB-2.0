import {
  ChangeDetectionStrategy,
  Component,
  Signal,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { FileDownloadBehaviour } from '../../../behaviours/files/file-download.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { Checkbox } from '../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { AlertPopup } from '../../../components/shared/ui/dialogs/alert-popup/alert-popup';
import { ConfirmPopup } from '../../../components/shared/ui/dialogs/confirm-popup/confirm-popup';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { ExpansionPanel } from '../../../components/shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { PageSpinner } from '../../../components/shared/ui/spinners/page-spinner/page-spinner';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowAction,
  TableRowToggle,
} from '../../../components/shared/ui/tables/table/table';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { ElectronicReceiptResponse } from '../../../entities/settings/taxation/electronic-receipt-response';
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../services/api/taxation/taxation.service';

/** Used only when the server gives no name in `Content-Disposition`. */
const GUIDE_FALLBACK_NAME = 'electronic-receipt-guide';

interface PrinterListParams {
  idProperty: string;
}

/** Translation group `management.settings.taxation.printer.table`. */
interface PrinterTableHeaders {
  name: string;
  ip: string;
}

@Component({
  selector: 'app-taxation',
  imports: [
    AlertPopup,
    Button,
    Checkbox,
    ConfirmPopup,
    ExpansionPanel,
    PageSpinner,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './taxation.html',
  styleUrl: './taxation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Taxation {
  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly translateService = inject(TranslateService);
  private readonly fileDownload = inject(FileDownloadBehaviour);
  protected readonly ButtonSize = ButtonSize;

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
  private readonly deletePrinterIcon = '/assets/images/DeletePrinter_setting.svg';
  protected readonly downloadGuideIcon = ['/assets/images/DownloadGuide_setting.svg'] as const;

  protected readonly printersExpanded = signal(true);
  protected readonly receiptExpanded = signal(false);
  protected readonly hasPrinters = signal(false);
  /** A change of a printer is running: the whole page waits for it. */
  protected readonly isPrinterActionRunning = signal(false);
  protected readonly isDeletePrinterAsked = signal(false);
  protected readonly printerToDelete = signal<PrinterItemListResponse | null>(null);
  private readonly printersTable = viewChild.required(Table);
  protected readonly printersPerPage = 5;
  protected readonly printerParams = computed<PrinterListParams | null>(() => {
    const idProperty = this.auth.user()?.idProperty;
    return idProperty ? { idProperty } : null;
  });
  private readonly printerHeaders = toSignal(
    this.translateService.stream(
      'management.settings.taxation.printer.table',
    ) as Observable<PrinterTableHeaders>,
    { initialValue: { name: '', ip: '' } },
  );
  protected readonly printerColumns = computed<readonly TableColumn<PrinterItemListResponse>[]>(
    () => [
      { field: 'printerName', header: this.printerHeaders().name },
      { field: 'printerIP', header: this.printerHeaders().ip },
    ],
  );
  private readonly deleteLabel = toSignal(
    this.translateService.stream('table.actions.delete') as Observable<string>,
    { initialValue: '' },
  );
  protected readonly printerActions = computed<readonly TableRowAction<PrinterItemListResponse>[]>(
    () => [
      {
        label: this.deleteLabel(),
        icon: this.deletePrinterIcon,
        action: (printer) => this.askDeletePrinter(printer),
      },
    ],
  );
  protected readonly receipt = signal(new ElectronicReceiptResponse());
  protected readonly printersLoadFailed = signal(false);
  protected readonly isGuideDownloading = signal(false);
  /** The error of an action (switch, delete, guide) goes in a popup. */
  private readonly actionError = signal<ApiProblem | null>(null);
  protected readonly isActionErrorShown = signal(false);
  protected readonly actionErrorIcon = ['/assets/images/Warning_setting.svg'] as const;
  private readonly receiptError = signal<ApiProblem | null>(null);
  protected readonly actionErrorMessage = this.messageOf(this.actionError);
  protected readonly receiptErrorMessage = this.messageOf(this.receiptError);

  constructor() {
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

  /** The API sends every printer at once: the table gets the page it asks for. */
  protected readonly loadPrinters: TableLoad<PrinterListParams, PrinterItemListResponse> = ({
    idProperty,
    page,
    pageSize,
  }) =>
    this.taxation.printerList(idProperty).pipe(
      tap({
        next: (printers) => this.hasPrinters.set(printers.length > 0),
        error: () => this.hasPrinters.set(false),
      }),
      map((printers) => ({
        total: printers.length,
        rows: printers.slice((page - 1) * pageSize, page * pageSize),
      })),
    );

  protected readonly setPrinterEnabled: TableRowToggle<PrinterItemListResponse> = (
    { idPrinter },
    isEnabled,
  ) =>
    void this.changePrinter(idPrinter, (idProperty, id) =>
      this.taxation.setPrinterIsActive({ idProperty, idPrinter: id, isEnabled }),
    );

  protected deletePrinter(): void {
    this.isDeletePrinterAsked.set(false);
    void this.changePrinter(this.printerToDelete()?.idPrinter ?? null, (idProperty, idPrinter) =>
      this.taxation.deletePrinter({ idProperty, idPrinter }),
    );
  }

  protected async downloadGuide(): Promise<void> {
    const idProperty = this.auth.user()?.idProperty;
    if (!idProperty || this.isGuideDownloading()) {
      return;
    }
    this.isGuideDownloading.set(true);
    try {
      const guide = await firstValueFrom(
        this.taxation.downloadGuideElectronicReceipt({ idProperty }),
      );
      this.fileDownload.save(guide, GUIDE_FALLBACK_NAME);
    } catch (error: unknown) {
      this.showActionError(error);
    } finally {
      this.isGuideDownloading.set(false);
    }
  }

  private askDeletePrinter(printer: PrinterItemListResponse): void {
    this.printerToDelete.set(printer);
    this.isDeletePrinterAsked.set(true);
  }

  /**
   * Then the printers are read again in any case: after an error the switch must go back to the
   * state the server has.
   */
  private async changePrinter(
    idPrinter: string | null,
    call: (idProperty: string, idPrinter: string) => Observable<void>,
  ): Promise<void> {
    const idProperty = this.auth.user()?.idProperty;
    if (idProperty && idPrinter && !this.isPrinterActionRunning()) {
      this.isPrinterActionRunning.set(true);
      try {
        await firstValueFrom(call(idProperty, idPrinter));
      } catch (error: unknown) {
        this.showActionError(error);
      } finally {
        this.isPrinterActionRunning.set(false);
      }
    }
    this.printersTable().reload();
  }

  private showActionError(error: unknown): void {
    this.actionError.set(toApiProblem(error));
    this.isActionErrorShown.set(true);
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
