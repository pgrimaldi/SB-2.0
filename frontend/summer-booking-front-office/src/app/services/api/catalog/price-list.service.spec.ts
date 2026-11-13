import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { AdditionalItem } from '../../../entities/settings/catalog/additional-item';
import { AdditionalItemAddItemRequest } from '../../../entities/settings/catalog/additional-item-add-item-request';
import { PackageAddItemRequest } from '../../../entities/settings/catalog/package-add-item-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { Language } from '../../../entities/shared/language';
import { PriceListService } from './price-list.service';

describe('PriceListService', () => {
  it('should ask a page of packages with the request of the table', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const request: PageRequest = {
      idProperty: 'p1',
      page: 2,
      pageSize: 10,
      search: 'ombrellone',
      filters: [],
      sortField: 'packageName',
      sortDirection: 'Descending',
    };
    const page: Page<PackageItem> = {
      total: 11,
      rows: [{ idPackage: 'k11', packageName: 'Ombrellone e lettino' }],
    };
    let answer: Page<PackageItem> | undefined;

    priceLists.packageList(request).subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/packages-list`,
    );
    expect(call.request.body).toEqual(request);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });

  it('should delete the chosen packages in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);

    priceLists.deletePackageItems({ idProperty: 'p1', idPackages: ['k1', 'k2'] }).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/delete-package-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idPackages: ['k1', 'k2'] });
    controller.verify();
  });

  it('should duplicate the chosen packages in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);

    priceLists.duplicatePackageItems({ idProperty: 'p1', idPackages: ['k1', 'k2'] }).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/duplicate-package-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idPackages: ['k1', 'k2'] });
    controller.verify();
  });

  it('should ask the packages of the dropdowns with the property only', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    let answer: unknown;

    priceLists.packageCombobox('p1').subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/package-combobox`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1' });
    call.flush([{ id: 'k1', value: 'Ombrellone e 2 lettini' }]);
    expect(answer).toEqual([{ id: 'k1', value: 'Ombrellone e 2 lettini' }]);
    controller.verify();
  });

  it('should create the additional item sending the whole form and the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const intermediatePrice = { idPeriod: 'pe1', isFirstRow: false, dayQuantity: 3, price: 40 };
    const request: AdditionalItemAddItemRequest = Object.assign(
      new AdditionalItemAddItemRequest(),
      {
        idArticle: 'r1',
        idPackages: ['k1'],
        isMapAssignable: true,
        sectorPrices: [{ idSector: 's1', seasonalHalfDayPrice: 50, seasonalPrice: 90 }],
        isPublicHolidaysIncluded: true,
        publicHolidayDates: ['2026-06-02T00:00:00.000Z', '2026-08-15T00:00:00.000Z'],
        isIntermediatePricesIncluded: true,
        intermediatePrices: [intermediatePrice],
      },
    );

    const sent = Object.assign(new AdditionalItemAddItemRequest(), request, {
      intermediatePrices: [{ ...intermediatePrice, extra: 'not sent' }],
      extra: 'not sent',
    });

    priceLists.createAdditionalItem('p1', sent).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/create-additional-services-article-item`,
    );
    expect(call.request.body).toEqual({ ...request, idProperty: 'p1' });
    controller.verify();
  });

  it('should read the additional item to edit by its id', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    let answer: unknown;

    priceLists.selectedAdditionalItem('a1').subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'GET' &&
        url === `${environment.apiBaseUrl}/catalog/get-additional-services-article-item`,
    );
    expect(call.request.params.get('idAdditionalItem')).toBe('a1');
    call.flush({ idAdditionalItem: 'a1', idArticle: 'r1' });
    expect(answer).toEqual({ idAdditionalItem: 'a1', idArticle: 'r1' });
    controller.verify();
  });

  it('should update the additional item sending the whole form, its id and the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const request: AdditionalItemAddItemRequest = Object.assign(
      new AdditionalItemAddItemRequest(),
      {
        idArticle: 'r1',
        isOnlineOnly: true,
        publicHolidayDates: ['2026-08-15T00:00:00.000Z'],
      },
    );

    priceLists
      .updateAdditionalItem('p1', 'a1', {
        ...request,
        extra: 'not sent',
      } as AdditionalItemAddItemRequest)
      .subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/update-additional-services-article-item`,
    );
    expect(call.request.body).toEqual({ ...request, idAdditionalItem: 'a1', idProperty: 'p1' });
    controller.verify();
  });

  it('should ask a page of additional items with the request of the table', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const request: PageRequest = {
      idProperty: 'p1',
      page: 1,
      pageSize: 10,
      search: 'lettino',
      filters: [],
      sortField: 'additionalItemName',
      sortDirection: 'Ascending',
    };
    const page: Page<AdditionalItem> = {
      total: 1,
      rows: [{ idAdditionalItem: 'a1', additionalItemName: 'Lettino' }],
    };
    let answer: Page<AdditionalItem> | undefined;

    priceLists.additionalItemList(request).subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/additional-services-articles-list`,
    );
    expect(call.request.body).toEqual(request);
    call.flush(page);
    expect(answer).toEqual(page);
    controller.verify();
  });

  it('should delete the chosen additional items in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);

    priceLists
      .deleteAdditionalItems({ idProperty: 'p1', idAdditionalItems: ['a1', 'a2'] })
      .subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/delete-additional-services-article-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idAdditionalItems: ['a1', 'a2'] });
    controller.verify();
  });

  it('should duplicate the chosen additional items in one call', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);

    priceLists
      .duplicateAdditionalItems({ idProperty: 'p1', idAdditionalItems: ['a1', 'a2'] })
      .subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' &&
        url === `${environment.apiBaseUrl}/catalog/duplicate-additional-services-article-item`,
    );
    expect(call.request.body).toEqual({ idProperty: 'p1', idAdditionalItems: ['a1', 'a2'] });
    controller.verify();
  });

  it('should create the package sending the whole form without the property', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const intermediatePrice = { idPeriod: 'pe1', isFirstRow: true, dayQuantity: 2, price: 35 };
    const reducedSeasonalPrice = {
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-06-30T00:00:00.000Z',
      reducedSeasonalPriceName: 'Giugno',
      sectorPrices: [{ idSector: 's1', seasonalHalfDayPrice: 400, seasonalPrice: 700 }],
    };
    const request: PackageAddItemRequest = Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: Language.It, packageName: 'Ombrellone e 2 lettini' }],
      maxPeopleQuantity: 4,
      umbrellaQuantity: 1,
      idSectors: ['s1'],
      sectorPrices: [{ idSector: 's1', seasonalHalfDayPrice: 500, seasonalPrice: 900 }],
      isPublicHolidaysIncluded: true,
      publicHolidayDates: ['2026-06-02T00:00:00.000Z', '2026-08-15T00:00:00.000Z'],
      isIntermediatePricesIncluded: true,
      intermediatePrices: [{ ...intermediatePrice, extra: 'not sent' }],
      reducedSeasonalPrices: [
        {
          ...reducedSeasonalPrice,
          sectorPrices: [{ ...reducedSeasonalPrice.sectorPrices[0], extra: 'not sent' }],
          extra: 'not sent',
        },
      ],
    });

    priceLists.createPackage(request).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/packages/create-package`,
    );
    expect(call.request.body).toEqual({
      ...request,
      reducedSeasonalPrices: [reducedSeasonalPrice],
      intermediatePrices: [intermediatePrice],
    });
    expect(call.request.body).not.toHaveProperty('idProperty');
    controller.verify();
  });

  it('should read the package to edit by its id', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    let answer: unknown;

    priceLists.selectedPackage('k1').subscribe((read) => (answer = read));

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'GET' && url === `${environment.apiBaseUrl}/catalog/packages/k1`,
    );
    call.flush({ idPackage: 'k1', maxPeopleQuantity: 4 });
    expect(answer).toEqual({ idPackage: 'k1', maxPeopleQuantity: 4 });
    controller.verify();
  });

  it('should update the package sending the whole form with its id', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const priceLists = TestBed.inject(PriceListService);
    const controller = TestBed.inject(HttpTestingController);
    const reducedSeasonalPrice = {
      dateFrom: '2026-06-01T00:00:00.000Z',
      dateTo: '2026-06-30T00:00:00.000Z',
      reducedSeasonalPriceName: 'Giugno',
      sectorPrices: [{ idSector: 's1', seasonalHalfDayPrice: 400, seasonalPrice: 700 }],
    };
    const request: PackageAddItemRequest = Object.assign(new PackageAddItemRequest(), {
      translations: [{ language: Language.It, packageName: 'Ombrellone e 3 lettini' }],
      sunbedQuantity: 3,
      reducedSeasonalPrices: [reducedSeasonalPrice],
      publicHolidayDates: ['2026-08-15T00:00:00.000Z'],
    });

    const sent = Object.assign(new PackageAddItemRequest(), request, {
      reducedSeasonalPrices: [
        {
          ...reducedSeasonalPrice,
          sectorPrices: [{ ...reducedSeasonalPrice.sectorPrices[0], extra: 'not sent' }],
          extra: 'not sent',
        },
      ],
      extra: 'not sent',
    });

    priceLists.updatePackage('k1', sent).subscribe();

    const call = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/packages/update-package`,
    );
    expect(call.request.body).toEqual({ ...request, idPackage: 'k1' });
    controller.verify();
  });
});
