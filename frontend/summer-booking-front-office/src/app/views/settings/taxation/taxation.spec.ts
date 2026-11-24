import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, Subject, map, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { PrinterMode } from '../../../entities/enums/printer-mode';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { PageRequest } from '../../../entities/pagination/page';
import { FileDownloadBehaviour } from '../../../behaviours/files/file-download.behaviour';
import { TaxationService } from '../../../services/api/taxation/taxation.service';
import { Taxation } from './taxation';

/** The page awaits the answer in an async method: its end runs after the current microtasks. */
const settle = async (fixture: ComponentFixture<unknown>) => {
  await Promise.resolve();
  await fixture.whenStable();
};

describe('Taxation', () => {
  const PRINTERS = [
    {
      idPrinter: 'pr1',
      printerName: 'EPSON - 1',
      printerIP: '192.0.2.10',
      printerMode: PrinterMode.Fiscal,
      isEnabled: false,
    },
    {
      idPrinter: 'pr2',
      printerName: 'EPSON - 2',
      printerIP: '192.0.2.11',
      printerMode: PrinterMode.Orders,
      isEnabled: true,
    },
  ];

  const setup = async (
    termsAndConditions: string,
    printers: Observable<typeof PRINTERS> = of(PRINTERS),
  ) => {
    const user = signal({ propertyId: 'p1' });
    const printerList = vi.fn(({ page, pageSize }: PageRequest) =>
      printers.pipe(
        map((list) => ({
          total: list.length,
          rows: list.slice((page - 1) * pageSize, page * pageSize),
        })),
      ),
    );
    const electronicReceipt = vi.fn(() => of(termsAndConditions));
    const answer = new Subject<void>();
    const setPrinterIsActive = vi.fn(() => answer);
    const deletePrinter = vi.fn(() => answer);
    const guide = new Subject<HttpResponse<Blob>>();
    const downloadGuideElectronicReceipt = vi.fn(() => guide);
    const save = vi.fn();
    const activateElectronicReceipt = vi.fn(() => of(undefined));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: TaxationService,
          useValue: {
            printerList,
            electronicReceipt,
            setPrinterIsActive,
            deletePrinter,
            downloadGuideElectronicReceipt,
            activateElectronicReceipt,
          },
        },
        { provide: FileDownloadBehaviour, useValue: { save } },
        { provide: AuthBehaviour, useValue: { user } },
      ],
    });
    const fixture = TestBed.createComponent(Taxation);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const spinner = () => document.querySelector('.page__spinner');
    return {
      fixture,
      element,
      printerList,
      electronicReceipt,
      setPrinterIsActive,
      deletePrinter,
      answer,
      spinner,
      downloadGuideElectronicReceipt,
      guide,
      save,
      activateElectronicReceipt,
    };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should load the printers and the terms of the property', async () => {
    const { element, printerList, electronicReceipt } = await setup('<p>Termini</p>');

    expect(printerList).toHaveBeenCalledWith({
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
    expect(electronicReceipt).toHaveBeenCalledWith();
    expect(
      [...element.querySelectorAll('.taxation__printers tbody tr.table__row')].map((row) =>
        [...row.querySelectorAll('td')].slice(0, 3).map((cell) => cell.textContent?.trim()),
      ),
    ).toEqual([
      ['EPSON - 1', '192.0.2.10', 'fiscal'],
      ['EPSON - 2', '192.0.2.11', 'orders'],
    ]);
    expect(element.querySelector('.taxation__terms p')?.textContent).toBe('Termini');
  });

  it('should show the terms without scripts or event handlers', async () => {
    const { element } = await setup(
      '<p onclick="alert(1)">Termini</p><script>alert(2)</script><img src="x" onerror="alert(3)">',
    );
    const terms = element.querySelector('.taxation__terms')!;

    expect(terms.querySelector('script')).toBeNull();
    expect(terms.querySelector('[onclick], [onerror]')).toBeNull();
    expect(terms.querySelector('p')?.textContent).toBe('Termini');
  });

  it('should switch a printer with the page spinner, then read the printers again', async () => {
    const { fixture, element, printerList, setPrinterIsActive, answer, spinner } =
      await setup('<p>Termini</p>');
    printerList.mockClear();

    element.querySelector<HTMLButtonElement>('tbody tr.table__row app-toggle button')!.click();
    await fixture.whenStable();
    expect(setPrinterIsActive).toHaveBeenCalledWith({
      idPrinter: 'pr1',
      isEnabled: true,
    });
    expect(spinner()).not.toBeNull();
    expect(printerList).not.toHaveBeenCalled();

    answer.next();
    answer.complete();
    await settle(fixture);
    expect(spinner()).toBeNull();
    expect(printerList).toHaveBeenCalledWith({
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
    expect(document.querySelector('.alert__popup__text')).toBeNull();
  });

  it('should show the error of a failed switch in a popup and read the printers again', async () => {
    const { fixture, element, printerList, answer } = await setup('<p>Termini</p>');
    printerList.mockClear();

    element.querySelector<HTMLButtonElement>('tbody tr.table__row app-toggle button')!.click();
    await fixture.whenStable();
    answer.error(
      new HttpErrorResponse({
        status: 404,
        error: { status: 404, code: ApiErrorCode.FiscalPrintPrinterNotFound },
      }),
    );
    await settle(fixture);

    expect(printerList).toHaveBeenCalledWith({
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
    // In a popup, the message of the code (here the fallback key: the test has no translations).
    expect(document.querySelector('.alert__popup__text')?.textContent?.trim()).toBe(
      'error.unknown',
    );
  });

  it('should delete a printer only after the confirmation', async () => {
    const { fixture, element, printerList, deletePrinter, answer, spinner } =
      await setup('<p>Termini</p>');
    printerList.mockClear();

    element.querySelector<HTMLButtonElement>('tbody tr.table__row .table__action')!.click();
    await fixture.whenStable();
    expect(deletePrinter).not.toHaveBeenCalled();
    const buttons = [
      ...document.querySelectorAll<HTMLButtonElement>('mat-dialog-container app-button button'),
    ];
    buttons.at(-1)!.click();
    await fixture.whenStable();

    expect(deletePrinter).toHaveBeenCalledWith('pr1');
    expect(document.querySelector('mat-dialog-container')).toBeNull();
    expect(spinner()).not.toBeNull();
    answer.next();
    answer.complete();
    await settle(fixture);
    expect(printerList).toHaveBeenCalledWith({
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
  });

  it('should download the guide with the spinner on its button', async () => {
    const { fixture, element, downloadGuideElectronicReceipt, guide, save } =
      await setup('<p>Termini</p>');
    const button = () => element.querySelector<HTMLButtonElement>('.taxation__guide button')!;

    button().click();
    await fixture.whenStable();
    expect(downloadGuideElectronicReceipt).toHaveBeenCalledWith();
    expect(element.querySelector('.taxation__guide mat-progress-spinner')).not.toBeNull();
    button().click(); // ignored while it is running
    expect(downloadGuideElectronicReceipt).toHaveBeenCalledTimes(1);

    const response = new HttpResponse({ body: new Blob(['guida']) });
    guide.next(response);
    guide.complete();
    await settle(fixture);
    expect(save).toHaveBeenCalledWith(response, 'electronic-receipt-guide');
    expect(element.querySelector('.taxation__guide mat-progress-spinner')).toBeNull();
  });

  it('should show the error of a failed download in a popup', async () => {
    const { fixture, element, guide, save } = await setup('<p>Termini</p>');

    element.querySelector<HTMLButtonElement>('.taxation__guide button')!.click();
    await fixture.whenStable();
    guide.error(new HttpErrorResponse({ status: 0 }));
    await settle(fixture);

    expect(save).not.toHaveBeenCalled();
    expect(document.querySelector('.alert__popup__text')?.textContent?.trim()).toBeTruthy();
  });

  it('should open the load error popup when the printers do not arrive, and retry from it', async () => {
    const { fixture, printerList } = await setup(
      '<p>Termini</p>',
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    const dialog = () => document.querySelector('mat-dialog-container');

    expect(dialog()).not.toBeNull();
    printerList.mockReturnValue(of({ total: PRINTERS.length, rows: PRINTERS }));
    dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')[1].click();
    await fixture.whenStable();

    expect(printerList).toHaveBeenCalledTimes(2);
    expect(dialog()).toBeNull();
  });

  it('should start on Non accetto with Invia off, and send true once Accetto is chosen', async () => {
    const { fixture, element, activateElectronicReceipt } = await setup('<p>Termini</p>');
    const accept = () => element.querySelector<HTMLInputElement>('.taxation__accept input')!;
    const refuse = () => element.querySelector<HTMLInputElement>('.taxation__refuse input')!;
    const send = () => element.querySelector<HTMLButtonElement>('.taxation__send button')!;

    expect(refuse().checked).toBe(true);
    expect(accept().checked).toBe(false);
    expect(send().disabled).toBe(true);

    accept().click();
    await fixture.whenStable();
    expect(refuse().checked).toBe(false);
    expect(send().disabled).toBe(false);

    accept().click();
    await fixture.whenStable();
    expect(accept().checked).toBe(true);
    expect(send().disabled).toBe(false);

    send().click();
    await settle(fixture);
    expect(activateElectronicReceipt).toHaveBeenCalledWith(true);
  });

  it('should show the account form once the server accepted the receipt', async () => {
    const { fixture, element } = await setup('<p>Termini</p>');
    const form = () => element.querySelector('app-electronic-receipt-account-form');

    expect(form()).toBeNull();
    element.querySelector<HTMLInputElement>('.taxation__accept input')!.click();
    await fixture.whenStable();
    element.querySelector<HTMLButtonElement>('.taxation__send button')!.click();
    await settle(fixture);

    expect(form()?.parentElement?.hidden).toBe(false);
    expect(
      element.querySelector<HTMLElement>('.taxation__terms__heading')!.parentElement!.hidden,
    ).toBe(true);
  });

  it('should keep the terms read-only behind ‹ once accepted', async () => {
    const { fixture, element } = await setup('<p>Termini</p>');
    element.querySelector<HTMLInputElement>('.taxation__accept input')!.click();
    await fixture.whenStable();
    element.querySelector<HTMLButtonElement>('.taxation__send button')!.click();
    await settle(fixture);
    const formBox = () =>
      element.querySelector('app-electronic-receipt-account-form')!.parentElement!;
    const termsBox = () =>
      element.querySelector<HTMLElement>('.taxation__terms__heading')!.parentElement!;

    expect(formBox().hidden).toBe(false);
    expect(termsBox().hidden).toBe(true);

    element
      .querySelector<HTMLButtonElement>('.electronic__receipt__account__form__back button')!
      .click();
    await fixture.whenStable();
    expect(formBox().hidden).toBe(true);
    expect(termsBox().hidden).toBe(false);
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.checked).toBe(true);
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.disabled).toBe(true);
    expect(element.querySelector<HTMLInputElement>('.taxation__refuse input')!.disabled).toBe(true);
    expect(element.querySelector<HTMLButtonElement>('.taxation__send button')!.disabled).toBe(true);
    expect(element.querySelector('.taxation__refuse__warning')).toBeNull();

    element.querySelector<HTMLButtonElement>('.taxation__to__account button')!.click();
    await fixture.whenStable();
    expect(formBox().hidden).toBe(false);
  });

  it('should show only the message when the electronic receipt is off for the property', async () => {
    const { fixture, element, electronicReceipt, activateElectronicReceipt } =
      await setup('<p>Termini</p>');
    const off = () =>
      throwError(
        () =>
          new HttpErrorResponse({
            status: 403,
            error: { status: 403, code: ApiErrorCode.FiskalyNotEnabled },
          }),
      );
    expect(element.querySelector('.taxation__choices')).not.toBeNull();

    activateElectronicReceipt.mockImplementation(off as never);
    const page = fixture.componentInstance as unknown as {
      chooseReceipt: (isAccepted: boolean) => void;
      sendReceiptChoice: () => Promise<void>;
    };
    page.chooseReceipt(true);
    await page.sendReceiptChoice();
    await settle(fixture);

    expect(electronicReceipt).toHaveBeenCalledTimes(1);
    expect(element.querySelector('.taxation__choices')).toBeNull();
    expect(element.querySelector('.taxation__terms')).toBeNull();
    expect(element.querySelector('.taxation__error')).not.toBeNull();
    expect(element.querySelector('.taxation__guide')).not.toBeNull();
    expect(document.querySelector('.alert__popup__text')).toBeNull();
  });

  it('should have nothing to accept until the terms are read, with a Riprova', async () => {
    const { fixture, element, electronicReceipt, activateElectronicReceipt } = await setup('');
    const page = fixture.componentInstance as unknown as {
      chooseReceipt: (isAccepted: boolean) => void;
      sendReceiptChoice: () => Promise<void>;
      readTermsAgain: () => void;
    };
    expect(element.querySelector('.taxation__choices')).toBeNull();
    page.chooseReceipt(true);
    await page.sendReceiptChoice();
    expect(activateElectronicReceipt).not.toHaveBeenCalled();

    electronicReceipt.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })) as never,
    );
    page.readTermsAgain();
    await fixture.whenStable();
    expect(element.querySelector('.taxation__choices')).toBeNull();

    electronicReceipt.mockReturnValue(of('<p>Termini</p>'));
    element.querySelector<HTMLButtonElement>('.taxation__failure button')!.click();
    await fixture.whenStable();
    expect(element.querySelector('.taxation__failure')).toBeNull();
    expect(element.querySelector('.taxation__choices')).not.toBeNull();
  });

  it('should show in a popup the error of a failed Invia and stay on the terms', async () => {
    const { fixture, element, activateElectronicReceipt } = await setup('<p>Termini</p>');
    activateElectronicReceipt.mockImplementation((() =>
      throwError(() => new HttpErrorResponse({ status: 500 }))) as never);

    element.querySelector<HTMLInputElement>('.taxation__accept input')!.click();
    await fixture.whenStable();
    element.querySelector<HTMLButtonElement>('.taxation__send button')!.click();
    await settle(fixture);

    expect(document.querySelector('.alert__popup__text')).not.toBeNull();
    expect(element.querySelector('app-electronic-receipt-account-form')).toBeNull();
    expect(element.querySelector('.taxation__choices')).not.toBeNull();
  });

  it('should open the terms again, keeping the form, when the server says they are not accepted', async () => {
    const { fixture, element } = await setup('<p>Termini</p>');
    element.querySelector<HTMLInputElement>('.taxation__accept input')!.click();
    await fixture.whenStable();
    element.querySelector<HTMLButtonElement>('.taxation__send button')!.click();
    await settle(fixture);

    const page = fixture.componentInstance as unknown as {
      showAccountRefusal: (problem: { status: number; code: string }) => void;
    };
    page.showAccountRefusal({ status: 409, code: ApiErrorCode.FiskalyTermsNotAccepted });
    await fixture.whenStable();
    expect(
      element.querySelector<HTMLElement>('app-electronic-receipt-account-form')!.parentElement!
        .hidden,
    ).toBe(true);
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.disabled).toBe(
      false,
    );
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.checked).toBe(false);
    expect(document.querySelector('.alert__popup__text')).not.toBeNull();
  });

  it('should open the links of the terms in a new tab', async () => {
    const { element } = await setup('<p><a href="https://example.com/privacy">privacy</a></p>');
    const open = vi.spyOn(window, 'open').mockReturnValue(null);
    const click = new MouseEvent('click', { bubbles: true, cancelable: true });

    element.querySelector('.taxation__terms a')!.dispatchEvent(click);
    expect(open).toHaveBeenCalledWith(
      'https://example.com/privacy',
      '_blank',
      'noopener,noreferrer',
    );
    expect(click.defaultPrevented).toBe(true);
  });

  it('should stop the download of the guide when the page is left', async () => {
    const { fixture, element, guide, save } = await setup('<p>Termini</p>');
    element.querySelector<HTMLButtonElement>('.taxation__guide button')!.click();
    await fixture.whenStable();

    fixture.destroy();
    guide.next(new HttpResponse({ body: new Blob(['guida']) }));
    expect(save).not.toHaveBeenCalled();
  });
});
