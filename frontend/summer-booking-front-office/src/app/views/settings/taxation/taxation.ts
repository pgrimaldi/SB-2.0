import {
  ChangeDetectionStrategy,
  Component,
  EnvironmentInjector,
  OnDestroy,
  Signal,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { DeleteItemsPopup } from '../../../components/management/delete-items-popup/delete-items-popup';
import { AddPrinterItemPopup } from '../../../components/management/settings/taxation/add-printer-item-popup/add-printer-item-popup';
import { ElectronicReceiptAccountForm } from '../../../components/management/settings/taxation/electronic-receipt-account-form/electronic-receipt-account-form';
import { FileDownloadBehaviour } from '../../../behaviours/files/file-download.behaviour';
import { ErrorTextBehaviour } from '../../../behaviours/errors/error-text.behaviour';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { Button, ButtonSize } from '../../../components/shared/ui/buttons/button/button';
import { IconButton } from '../../../components/shared/ui/buttons/icon-button/icon-button';
import { Checkbox } from '../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { AlertPopup } from '../../../components/shared/ui/dialogs/alert-popup/alert-popup';
import { TableErrorPopup } from '../../../components/shared/ui/dialogs/table-error-popup/table-error-popup';
import { DATA_RELOAD } from '../../../components/shared/ui/data/data-reload';
import { ExpansionPanel } from '../../../components/shared/ui/expansion-panels/expansion-panel/expansion-panel';
import { PageSpinner } from '../../../components/shared/ui/spinners/page-spinner/page-spinner';
import { Spinner } from '../../../components/shared/ui/spinners/spinner/spinner';
import { standardRowActions } from '../../../components/shared/tables/standard-row-actions';
import {
  Table,
  TableColumn,
  TableLoad,
  TableRowToggle,
} from '../../../components/shared/ui/tables/table/table';
import { PrinterMode } from '../../../entities/enums/printer-mode';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../entities/errors/api-problem';
import { toApiProblem } from '../../../services/api/errors/to-api-problem';
import { TaxationService } from '../../../services/api/taxation/taxation.service';

const GUIDE_FALLBACK_NAME = 'electronic-receipt-guide';

interface PrinterTableHeaders {
  name: string;
  ip: string;
  mode: string;
}

interface PrinterRow {
  idPrinter: string | null;
  printerName: string | null;
  printerIP: string | null;
  printerMode: string | null;
  isEnabled: boolean;
}

@Component({
  selector: 'app-taxation',
  imports: [
    AddPrinterItemPopup,
    AlertPopup,
    Button,
    Checkbox,
    DeleteItemsPopup,
    ElectronicReceiptAccountForm,
    ExpansionPanel,
    I18nText,
    IconButton,
    PageSpinner,
    Spinner,
    Table,
    TableErrorPopup,
    TranslatePipe,
  ],
  templateUrl: './taxation.html',
  styleUrl: './taxation.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Taxation implements OnDestroy {
  private readonly taxation = inject(TaxationService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly translateService = inject(TranslateService);
  private readonly fileDownload = inject(FileDownloadBehaviour);
  private readonly injector = inject(EnvironmentInjector);
  private readonly window = inject(DOCUMENT).defaultView;
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
  protected readonly downloadGuideIcon = ['/assets/images/Taxation_DownloadGuide.svg'] as const;

  protected readonly printersExpanded = signal(true);
  protected readonly receiptExpanded = signal(false);
  protected readonly hasPrinters = signal(false);
  protected readonly isReady = computed(() => !!this.auth.user()?.propertyId);
  protected readonly isPrinterActionRunning = signal(false);
  protected readonly isDeletePrinterAsked = signal(false);
  protected readonly isAddPrinterOpen = signal(false);
  protected readonly printerToDelete = signal<PrinterRow | null>(null);
  protected readonly idPrintersToDelete = computed(() => {
    const idPrinter = this.printerToDelete()?.idPrinter;
    return idPrinter ? [idPrinter] : [];
  });
  private readonly printersTable = viewChild.required(Table);
  protected readonly printersPerPage = 5;
  protected readonly printerParams = computed<object | null>(() => (this.isReady() ? {} : null));
  private readonly printerHeaders = toSignal(
    this.translateService.stream(
      'management.settings.taxation.printer.table',
    ) as Observable<PrinterTableHeaders>,
    { initialValue: { name: '', ip: '', mode: '' } },
  );
  protected readonly printerColumns = computed<readonly TableColumn<PrinterRow>[]>(() => [
    { field: 'printerName', header: this.printerHeaders().name },
    { field: 'printerIP', header: this.printerHeaders().ip },
    { field: 'printerMode', header: this.printerHeaders().mode },
  ]);
  protected readonly printerActions = standardRowActions<PrinterRow>({
    delete: (printer) => this.askDeletePrinter(printer),
  });
  private readonly termsReads = signal(0);
  protected readonly isReadingTerms = signal(false);
  protected readonly termsAndConditions = signal<string | null>(null);
  protected readonly areTermsRead = computed(() => !!this.termsAndConditions()?.trim());
  protected readonly printersLoadFailed = signal(false);
  protected readonly isGuideDownloading = signal(false);
  private guideDownload: Subscription | null = null;
  protected readonly isAcceptChosen = signal(false);
  protected readonly isReceiptActivated = signal(false);
  /** It stays in the page, with what was written, while the terms are shown. */
  protected readonly isAccountOpened = signal(false);
  protected readonly isReceiptAccepted = computed(
    () => this.isReceiptActivated() || this.isAcceptChosen(),
  );
  protected readonly isReceiptAccountShown = signal(true);
  protected readonly toAccountIcon = ['/assets/images/GeneralIcon_ChevronRight.svg'] as const;
  protected readonly isSendingReceiptChoice = signal(false);
  private readonly actionError = signal<ApiProblem | null>(null);
  protected readonly isActionErrorShown = signal(false);
  protected readonly closeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X, warning]. */
  protected readonly alertIcons = [
    '/assets/images/GeneralIcon_Close.svg',
    '/assets/images/GeneralIcon_Warning.svg',
  ] as const;
  private readonly receiptError = signal<ApiProblem | null>(null);
  protected readonly isReceiptOff = computed(
    () => this.receiptError()?.code === ApiErrorCode.FiskalyNotEnabled,
  );
  protected readonly actionErrorMessage = this.messageOf(this.actionError);
  protected readonly receiptErrorMessage = this.messageOf(this.receiptError);

  constructor() {
    effect((onCleanup) => {
      this.termsReads();
      this.dataReload?.();
      if (!this.isReady()) {
        return;
      }
      const subscription = untracked(() => {
        this.isReadingTerms.set(true);
        this.receiptError.set(null);
        return this.taxation.electronicReceipt().subscribe({
          next: (termsAndConditions) => {
            this.termsAndConditions.set(termsAndConditions);
            this.isReadingTerms.set(false);
          },
          error: (error: unknown) => this.showReceiptError(toApiProblem(error)),
        });
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected readonly loadPrinters: TableLoad<object, PrinterRow> = (request) =>
    this.taxation.printerList(request).pipe(
      switchMap((page) =>
        (
          this.translateService.get(
            'management.settings.taxation.printer.form.modes',
          ) as Observable<Record<PrinterMode, string>>
        ).pipe(
          map((modes) => ({
            total: page.total,
            rows: page.rows.map((printer): PrinterRow => ({
              ...printer,
              printerMode: printer.printerMode
                ? (modes[printer.printerMode] ?? printer.printerMode)
                : null,
            })),
          })),
        ),
      ),
      tap({
        next: (page) => this.hasPrinters.set(page.total > 0),
        error: () => this.hasPrinters.set(false),
      }),
    );

  protected readonly setPrinterEnabled: TableRowToggle<PrinterRow> = ({ idPrinter }, isEnabled) =>
    void this.changePrinter(idPrinter, (id) =>
      this.taxation.setPrinterIsActive({ idPrinter: id, isEnabled }),
    );

  protected readonly deletePrinter = ([idPrinter]: string[]): Observable<void> =>
    this.taxation.deletePrinter(idPrinter);

  protected readTermsAgain(): void {
    this.termsReads.update((reads) => reads + 1);
  }

  /** The links of the terms never take the place of the page. */
  protected openTermsLink(event: Event): void {
    const link = event.target instanceof Element ? event.target.closest('a') : null;
    if (link && /^https?:$/.test(link.protocol)) {
      event.preventDefault();
      this.window?.open(link.href, '_blank', 'noopener,noreferrer');
    }
  }

  protected downloadGuide(): void {
    if (!this.isReady() || this.isGuideDownloading()) {
      return;
    }
    this.isGuideDownloading.set(true);
    this.guideDownload = this.taxation.downloadGuideElectronicReceipt().subscribe({
      next: (guide) => {
        this.isGuideDownloading.set(false);
        this.fileDownload.save(guide, GUIDE_FALLBACK_NAME);
      },
      error: (error: unknown) => {
        this.isGuideDownloading.set(false);
        this.showActionError(toApiProblem(error));
      },
    });
  }

  protected chooseReceipt(isAccepted: boolean): void {
    this.isAcceptChosen.set(isAccepted);
  }

  /** The box chosen is changed only by choosing the other one. */
  protected keepChecked(box: Checkbox): void {
    afterNextRender(() => box.checked.set(true), { injector: this.injector });
  }

  protected async sendReceiptChoice(): Promise<void> {
    if (
      !this.isReady() ||
      !this.areTermsRead() ||
      this.isSendingReceiptChoice() ||
      !this.isAcceptChosen()
    ) {
      return;
    }
    this.isSendingReceiptChoice.set(true);
    try {
      await firstValueFrom(this.taxation.activateElectronicReceipt(this.isAcceptChosen()));
      this.isReceiptActivated.set(true);
      this.isAccountOpened.set(true);
      this.isReceiptAccountShown.set(true);
    } catch (error: unknown) {
      this.showReceiptRefusal(toApiProblem(error));
    } finally {
      this.isSendingReceiptChoice.set(false);
    }
  }

  /** The account cannot be saved: off for the property, or the terms are to be accepted again. */
  protected showAccountRefusal(problem: ApiProblem): void {
    this.isReceiptAccountShown.set(false);
    if (problem.code === ApiErrorCode.FiskalyTermsNotAccepted) {
      this.isReceiptActivated.set(false);
      this.isAcceptChosen.set(false);
    }
    this.showReceiptRefusal(problem);
  }

  private askDeletePrinter(printer: PrinterRow): void {
    this.printerToDelete.set(printer);
    this.isDeletePrinterAsked.set(true);
  }

  private async changePrinter(
    idPrinter: string | null,
    call: (idPrinter: string) => Observable<void>,
  ): Promise<void> {
    if (this.isReady() && idPrinter && !this.isPrinterActionRunning()) {
      this.isPrinterActionRunning.set(true);
      try {
        await firstValueFrom(call(idPrinter));
      } catch (error: unknown) {
        this.showActionError(toApiProblem(error));
      } finally {
        this.isPrinterActionRunning.set(false);
      }
    }
    this.printersTable().reload();
  }

  private showReceiptRefusal(problem: ApiProblem): void {
    if (problem.code === ApiErrorCode.FiskalyNotEnabled) {
      this.showReceiptError(problem);
    } else {
      this.showActionError(problem);
    }
  }

  private showReceiptError(problem: ApiProblem): void {
    this.isReadingTerms.set(false);
    this.termsAndConditions.set(null);
    this.receiptError.set(problem);
  }

  private showActionError(problem: ApiProblem): void {
    this.actionError.set(problem);
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

  ngOnDestroy(): void {
    this.guideDownload?.unsubscribe();
  }
}
