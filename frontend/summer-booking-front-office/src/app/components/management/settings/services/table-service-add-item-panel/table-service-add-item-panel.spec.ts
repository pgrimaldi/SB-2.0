import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { TableServiceAvailabilityMode } from '../../../../../entities/enums/table-service-availability-mode';
import { TableServiceBookingPayment } from '../../../../../entities/enums/table-service-booking-payment';
import { TableServiceAvailabilityDay } from '../../../../../entities/settings/services/table-service-availability-day';
import { TableServiceAvailabilityDraft } from '../../../../../entities/settings/services/table-service-availability-draft';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { TableServiceAddItemPanel } from './table-service-add-item-panel';

describe('TableServiceAddItemPanel', () => {
  const setup = async () => {
    // jsdom has no matchMedia.
    vi.stubGlobal(
      'matchMedia',
      vi.fn(() => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
      })),
    );
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        { provide: CatalogService, useValue: {} },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: { settings: { services: { form: { tabs: { general_info: 'Info Generali' } } } } },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(TableServiceAddItemPanel);
    const panel = fixture.componentInstance;
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const form = panel['draftForm'];
    form.translations[0].serviceName().value.set('Pranzo');
    form.translations[0].description().value.set('Ai tavoli');
    form.tableGroups().value.set([{ tableQuantity: 10, seatPerTableQuantity: 4 }]);
    return { panel, form, isSaveDisabled: () => panel['isSaveDisabled']() };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should want the tables, each with other seats, or the total of the seats', async () => {
    const { form, isSaveDisabled } = await setup();
    const accepts = (tableQuantity: number | null, seatPerTableQuantity: number | null) => {
      form.tableGroups().value.set([
        { tableQuantity: 10, seatPerTableQuantity: 4 },
        { tableQuantity, seatPerTableQuantity },
      ]);
      return !isSaveDisabled();
    };

    expect(accepts(500, 100)).toBe(true);
    expect(accepts(null, 2)).toBe(false);
    expect(accepts(501, 2)).toBe(false);
    expect(accepts(0, 2)).toBe(false);
    expect(accepts(5, null)).toBe(false);
    expect(accepts(5, 101)).toBe(false);
    expect(accepts(5, 4)).toBe(false);
    form.tableGroups().value.set([]);
    expect(isSaveDisabled()).toBe(true);

    form.availabilityMode().value.set(TableServiceAvailabilityMode.Seats);
    for (const [seats, isDisabled] of [
      [null, true],
      [0, true],
      [10_001, true],
      [10_000, false],
    ] as const) {
      form.totalSeatQuantity().value.set(seats);
      expect(isSaveDisabled()).toBe(isDisabled);
    }
  });

  it('should want the dates of an availability, a day and its hours', async () => {
    const { form, isSaveDisabled } = await setup();

    form.availabilities().value.set([
      Object.assign(new TableServiceAvailabilityDraft(), {
        days: [1, 2].map((dayOfWeek) =>
          Object.assign(new TableServiceAvailabilityDay(), { dayOfWeek }),
        ),
      }),
    ]);
    const availability = form.availabilities[0];
    availability.dateFrom().value.set('2026-07-01T00:00:00.000Z');
    availability.dateTo().value.set('2026-07-31T00:00:00.000Z');
    expect(isSaveDisabled()).toBe(true);
    availability.days[0].isEnabled().value.set(true);
    expect(isSaveDisabled()).toBe(true);
    availability.days[0].startTime().value.set('12:00');
    availability.days[0].endTime().value.set('15:00');
    expect(isSaveDisabled()).toBe(false);

    availability.isSameTimeForAllDays().value.set(true);
    expect(isSaveDisabled()).toBe(true);
    availability.startTime().value.set('12:00');
    availability.endTime().value.set('15:00');
    expect(isSaveDisabled()).toBe(false);
    availability.dateTo().value.set(null);
    expect(isSaveDisabled()).toBe(true);
  });

  it('should want the percentage of a deposit and the bookings per time slot in their limits', async () => {
    const { form, isSaveDisabled } = await setup();

    form.bookingPayment().value.set(TableServiceBookingPayment.Deposit);
    for (const [percentage, isDisabled] of [
      [null, true],
      [0, true],
      [100.5, true],
      [100, false],
    ] as const) {
      form.depositPercentage().value.set(percentage);
      expect(isSaveDisabled()).toBe(isDisabled);
    }
    form.maxBookingPerTimeSlotQuantity().value.set(0);
    expect(isSaveDisabled()).toBe(true);
    form.maxBookingPerTimeSlotQuantity().value.set(null);
    expect(isSaveDisabled()).toBe(false);
  });
});
