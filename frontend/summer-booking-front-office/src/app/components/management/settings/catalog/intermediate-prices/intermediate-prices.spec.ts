import { Component, signal } from '@angular/core';
import { form } from '@angular/forms/signals';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { PackageIntermediatePrice } from '../../../../../entities/settings/catalog/package-intermediate-price';
import { PackagePeriodPrice } from '../../../../../entities/settings/catalog/package-period-price';
import { SeasonPeriodItem } from '../../../../../entities/settings/seasons/season-period-item';
import { IntermediatePrices } from './intermediate-prices';

@Component({
  imports: [IntermediatePrices],
  template: `
    <app-intermediate-prices
      [isIncluded]="fields.isIncluded"
      [prices]="fields.prices"
      [periodPrices]="fields.periodPrices"
      [periods]="periods"
      [hasOtherSectors]="hasOtherSectors()"
      idFirstSector="s1"
    />
  `,
})
class Host {
  readonly model = signal({
    isIncluded: true,
    prices: [] as PackageIntermediatePrice[],
    periodPrices: ['s1', 's2', 's3'].map((idSector) =>
      Object.assign(new PackagePeriodPrice(), { idSector, idPeriod: 0 }),
    ),
  });
  readonly fields = form(this.model);
  readonly hasOtherSectors = signal(true);
  readonly periods = [Object.assign(new SeasonPeriodItem(), { key: 0, name: 'Bassa' })];
}

describe('IntermediatePrices', () => {
  const setup = async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
      ],
    });
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();
    const prices = fixture.debugElement.children[0].componentInstance as IntermediatePrices;
    const model = fixture.componentInstance.model;
    const choose = (index: number) => prices['selectedRow'].set(String(index));
    return {
      prices,
      model,
      choose,
      hasOtherSectors: fixture.componentInstance.hasOtherSectors,
      fixture,
    };
  };

  it('should write the price of 7 days in the column of the first sector too', async () => {
    const { prices, model, choose } = await setup();

    choose(0);
    prices['setPrice'](7, 120);
    expect(model().prices).toMatchObject([
      { idPeriod: 0, isFirstRow: true, dayQuantity: 7, price: 120 },
    ]);
    expect(model().periodPrices.map(({ sevenDaysPrice }) => sevenDaysPrice)).toEqual([
      120,
      null,
      null,
    ]);
  });

  it('should offer the other rows only with more than one sector', async () => {
    const { prices, hasOtherSectors, fixture } = await setup();

    expect(prices['options']()).toHaveLength(2);
    hasOtherSectors.set(false);
    await fixture.whenStable();
    expect(prices['choices']().map(({ isFirstRow }) => isFirstRow)).toEqual([true]);
  });

  it('should write the price of 30 days in the column of all the other sectors', async () => {
    const { prices, model, choose } = await setup();

    choose(1);
    prices['setPrice'](30, 400);
    prices['setPrice'](5, 80);
    expect(model().periodPrices.map(({ monthlyPrice }) => monthlyPrice)).toEqual([null, 400, 400]);
    prices['setPrice'](30, null);
    expect(model().prices).toMatchObject([{ isFirstRow: false, dayQuantity: 5, price: 80 }]);
    expect(model().periodPrices.map(({ monthlyPrice }) => monthlyPrice)).toEqual([
      null,
      null,
      null,
    ]);
  });
});
