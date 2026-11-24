import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { AddPrinterItemRequest } from '../../../entities/settings/taxation/add-printer-item-request';
import { ElectronicReceiptAccount } from '../../../entities/settings/taxation/electronic-receipt-account';
import { TaxationService } from './taxation.service';

describe('TaxationService', () => {
  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      taxation: TestBed.inject(TaxationService),
      controller: TestBed.inject(HttpTestingController),
    };
  }

  it('should call the printers under the domain of the printers', () => {
    const { taxation, controller } = setup();
    const page = {
      page: 1,
      pageSize: 5,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    } as unknown as Parameters<typeof taxation.printerList>[0];

    taxation.printerList(page).subscribe();
    taxation
      .addPrinter({ ...new AddPrinterItemRequest(), extra: true } as AddPrinterItemRequest)
      .subscribe();
    taxation.setPrinterIsActive({ idPrinter: 'p1', isEnabled: true }).subscribe();
    taxation.deletePrinter('p1').subscribe();

    const bodyOf = (name: string): unknown =>
      controller.expectOne({
        method: 'POST',
        url: `${environment.apiBaseUrl}/fiscalprint/printers/${name}`,
      }).request.body;
    expect(bodyOf('printer-list')).toEqual(page);
    expect(bodyOf('add-printer')).toEqual({ ...new AddPrinterItemRequest() });
    expect(bodyOf('set-is-active')).toEqual({ idPrinter: 'p1', isEnabled: true });
    expect(bodyOf('delete-printer')).toEqual({ idPrinter: 'p1' });
    controller.verify();
  });

  it('should call the electronic receipt under its domain', () => {
    const { taxation, controller } = setup();

    taxation.electronicReceipt().subscribe();
    taxation.activateElectronicReceipt(true).subscribe();
    taxation
      .saveElectronicReceiptAccount({
        ...new ElectronicReceiptAccount(),
        extra: true,
      } as ElectronicReceiptAccount)
      .subscribe();
    taxation.downloadGuideElectronicReceipt().subscribe();

    const bodyOf = (method: string, name: string): unknown =>
      controller.expectOne({ method, url: `${environment.apiBaseUrl}/fiskaly/${name}` }).request
        .body;
    expect(bodyOf('GET', 'electronic-receipt')).toBeNull();
    expect(bodyOf('POST', 'electronic-receipt-activation')).toEqual({
      isElectronicReceiptAccepted: true,
    });
    expect(bodyOf('POST', 'save-electronic-receipt-account')).toEqual({
      ...new ElectronicReceiptAccount(),
    });
    expect(bodyOf('POST', 'download-guide-electronic-receipt')).toBeNull();
    controller.verify();
  });
});
