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
import { Observable, firstValueFrom, of, switchMap, tap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { AddPrinterItemPopup } from '../../../components/management/settings/taxation/add-printer-item-popup/add-printer-item-popup';
import { ElectronicReceiptAccountForm } from '../../../components/management/settings/taxation/electronic-receipt-account-form/electronic-receipt-account-form';
import { FileDownloadBehaviour } from '../../../behaviours/files/file-download.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
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
import { PrinterItemListResponse } from '../../../entities/settings/taxation/printer-item-list-response';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../services/api/taxation/taxation.service';

/** Used only when the server gives no name in `Content-Disposition`. */
const GUIDE_FALLBACK_NAME = 'electronic-receipt-guide';

/** Translation group `management.settings.taxation.printer.table`. */
interface PrinterTableHeaders {
  name: string;
  ip: string;
}

@Component({
  selector: 'app-taxation',
  imports: [
    AddPrinterItemPopup,
    AlertPopup,
    Button,
    Checkbox,
    ConfirmPopup,
    ElectronicReceiptAccountForm,
    ExpansionPanel,
    I18nText,
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
    '/assets/images/Taxation_Printer.svg',
    '/assets/images/GeneralIcon_ChevronRight.svg',
  ] as const;
  /** [before the title, arrow]. */
  protected readonly receiptPanelIcons = [
    '/assets/images/Taxation_ElectronicReceipt.svg',
    '/assets/images/GeneralIcon_ChevronRight.svg',
  ] as const;
  protected readonly addPrinterIcon = ['/assets/images/GeneralIcon_Add.svg'] as const;
  private readonly deletePrinterIcon = '/assets/images/GeneralIcon_Delete.svg';
  protected readonly downloadGuideIcon = ['/assets/images/Taxation_DownloadGuide.svg'] as const;

  protected readonly printersExpanded = signal(true);
  protected readonly receiptExpanded = signal(false);
  protected readonly hasPrinters = signal(false);
  /** A change of a printer is running: the whole page waits for it. */
  protected readonly isPrinterActionRunning = signal(false);
  protected readonly isDeletePrinterAsked = signal(false);
  protected readonly isAddPrinterOpen = signal(false);
  protected readonly printerToDelete = signal<PrinterItemListResponse | null>(null);
  private readonly printersTable = viewChild.required(Table);
  protected readonly printersPerPage = 5;
  protected readonly printerParams = computed<object | null>(() =>
    this.auth.user()?.propertyId ? {} : null,
  );
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
  protected readonly termsAndConditions = signal<string | null>(null);
  protected readonly printersLoadFailed = signal(false);
  protected readonly isGuideDownloading = signal(false);
  protected readonly isAcceptChosen = signal(false);
  protected readonly isReceiptActivated = signal(false);
  protected readonly isReceiptAccepted = computed(
    () => this.isReceiptActivated() || this.isAcceptChosen(),
  );
  protected readonly isReceiptAccountShown = signal(true);
  protected readonly toAccountIcon = '/assets/images/GeneralIcon_ChevronRight.svg';
  protected readonly isSendingReceiptChoice = signal(false);
  /** The error of an action (switch, delete, guide) goes in a popup. */
  private readonly actionError = signal<ApiProblem | null>(null);
  protected readonly isActionErrorShown = signal(false);
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly addPrinterCloseIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  private readonly receiptError = signal<ApiProblem | null>(null);
  protected readonly actionErrorMessage = this.messageOf(this.actionError);
  protected readonly receiptErrorMessage = this.messageOf(this.receiptError);

  constructor() {
    // Loaded again when the language changes: the server sends the terms translated.
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()?.propertyId) {
        return;
      }
      const subscription = untracked(() =>
        this.taxation.electronicReceipt().subscribe({
          next: (termsAndConditions) => {
            this.termsAndConditions.set(termsAndConditions);
            this.receiptError.set(null);
          },
          error: (error: unknown) => {
            this.termsAndConditions.set(null);
            this.receiptError.set(toApiProblem(error));
          },
        }),
      );
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected readonly loadPrinters: TableLoad<object, PrinterItemListResponse> = (request) =>
    this.taxation.printerList(request).pipe(
      tap({
        next: (page) => this.hasPrinters.set(page.total > 0),
        error: () => this.hasPrinters.set(false),
      }),
    );

  protected readonly setPrinterEnabled: TableRowToggle<PrinterItemListResponse> = (
    { idPrinter },
    isEnabled,
  ) =>
    void this.changePrinter(idPrinter, (id) =>
      this.taxation.setPrinterIsActive({ idPrinter: id, isEnabled }),
    );

  protected deletePrinter(): void {
    this.isDeletePrinterAsked.set(false);
    void this.changePrinter(this.printerToDelete()?.idPrinter ?? null, (idPrinter) =>
      this.taxation.deletePrinter(idPrinter),
    );
  }

  protected async downloadGuide(): Promise<void> {
    if (!this.auth.user()?.propertyId || this.isGuideDownloading()) {
      return;
    }
    this.isGuideDownloading.set(true);
    try {
      const guide = await firstValueFrom(this.taxation.downloadGuideElectronicReceipt());
      this.fileDownload.save(guide, GUIDE_FALLBACK_NAME);
    } catch (error: unknown) {
      this.showActionError(error);
    } finally {
      this.isGuideDownloading.set(false);
    }
  }

  protected chooseReceipt(isAccepted: boolean, isChecked: boolean): void {
    this.isAcceptChosen.set(isChecked && isAccepted);
  }

  protected async sendReceiptChoice(): Promise<void> {
    if (!this.auth.user()?.propertyId || this.isSendingReceiptChoice() || !this.isAcceptChosen()) {
      return;
    }
    this.isSendingReceiptChoice.set(true);
    try {
      await firstValueFrom(this.taxation.activateElectronicReceipt(this.isAcceptChosen()));
      this.isReceiptActivated.set(true);
      this.isReceiptAccountShown.set(true);
    } catch (error: unknown) {
      this.showActionError(error);
    } finally {
      this.isSendingReceiptChoice.set(false);
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
    call: (idPrinter: string) => Observable<void>,
  ): Promise<void> {
    if (this.auth.user()?.propertyId && idPrinter && !this.isPrinterActionRunning()) {
      this.isPrinterActionRunning.set(true);
      try {
        await firstValueFrom(call(idPrinter));
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
