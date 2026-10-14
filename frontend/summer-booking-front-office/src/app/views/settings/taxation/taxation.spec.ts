import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { HttpErrorResponse, HttpResponse } from '@angular/common/http';
import { Observable, Subject, map, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
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
    { idPrinter: 'pr1', printerName: 'EPSON - 1', printerIP: '192.0.2.10', isEnabled: false },
    { idPrinter: 'pr2', printerName: 'EPSON - 2', printerIP: '192.0.2.11', isEnabled: true },
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
    const electronicReceipt = vi.fn(() => of({ termsAndConditions }));
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
      idProperty: 'p1',
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    });
    expect(electronicReceipt).toHaveBeenCalledWith('p1');
    expect(
      [...element.querySelectorAll('.taxation__printers tbody tr.table__row')].map((row) =>
        [...row.querySelectorAll('td')].slice(0, 2).map((cell) => cell.textContent?.trim()),
      ),
    ).toEqual([
      ['EPSON - 1', '192.0.2.10'],
      ['EPSON - 2', '192.0.2.11'],
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

  it('should read the printers 5 at a time', async () => {
    const printers = Array.from({ length: 7 }, (_, index) => ({
      idPrinter: `pr${index + 1}`,
      printerName: `EPSON - ${index + 1}`,
      printerIP: `192.0.2.${index + 1}`,
      isEnabled: false,
    }));
    const { fixture, element, printerList } = await setup('<p>Termini</p>', of(printers));
    const names = () =>
      [...element.querySelectorAll('.taxation__printers tbody tr.table__row td:first-child')].map(
        (cell) => cell.textContent?.trim(),
      );

    expect(names()).toEqual(['EPSON - 1', 'EPSON - 2', 'EPSON - 3', 'EPSON - 4', 'EPSON - 5']);

    element.querySelector<HTMLButtonElement>('.mat-mdc-paginator-navigation-next')!.click();
    await fixture.whenStable();
    expect(printerList).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2, pageSize: 5 }));
    expect(names()).toEqual(['EPSON - 6', 'EPSON - 7']);
  });

  it('should switch a printer with the page spinner, then read the printers again', async () => {
    const { fixture, element, printerList, setPrinterIsActive, answer, spinner } =
      await setup('<p>Termini</p>');
    printerList.mockClear();

    element.querySelector<HTMLButtonElement>('tbody tr.table__row app-toggle button')!.click();
    await fixture.whenStable();
    expect(setPrinterIsActive).toHaveBeenCalledWith({
      idProperty: 'p1',
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
      idProperty: 'p1',
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
        error: { status: 404, code: 'resource.not_found', title: 'Resource not found' },
      }),
    );
    await settle(fixture);

    expect(printerList).toHaveBeenCalledWith({
      idProperty: 'p1',
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

    expect(deletePrinter).toHaveBeenCalledWith({ idProperty: 'p1', idPrinter: 'pr1' });
    expect(document.querySelector('mat-dialog-container')).toBeNull();
    expect(spinner()).not.toBeNull();
    answer.next();
    answer.complete();
    await settle(fixture);
    expect(printerList).toHaveBeenCalledWith({
      idProperty: 'p1',
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
    expect(downloadGuideElectronicReceipt).toHaveBeenCalledWith('p1');
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

    expect(dialog()?.querySelector('.table__error__popup__title')?.textContent?.trim()).toBe(
      'table.load_error.title',
    );
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

    send().click();
    await settle(fixture);
    expect(activateElectronicReceipt).toHaveBeenCalledWith({
      idProperty: 'p1',
      isElectronicReceiptAccepted: true,
    });
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

    element.querySelector<HTMLButtonElement>('.electronic__receipt__account__form__back')!.click();
    await fixture.whenStable();
    expect(formBox().hidden).toBe(true);
    expect(termsBox().hidden).toBe(false);
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.checked).toBe(true);
    expect(element.querySelector<HTMLInputElement>('.taxation__accept input')!.disabled).toBe(true);
    expect(element.querySelector<HTMLInputElement>('.taxation__refuse input')!.disabled).toBe(true);
    expect(element.querySelector<HTMLButtonElement>('.taxation__send button')!.disabled).toBe(true);
    expect(element.querySelector('.taxation__refuse__warning')).toBeNull();

    element.querySelector<HTMLButtonElement>('.taxation__to__account')!.click();
    await fixture.whenStable();
    expect(formBox().hidden).toBe(false);
  });
});
