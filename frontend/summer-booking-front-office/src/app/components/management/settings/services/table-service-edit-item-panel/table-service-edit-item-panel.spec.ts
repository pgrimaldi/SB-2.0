import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { Observable, of, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { LanguageBehaviour } from '../../../../../behaviours/i18n/language.behaviour';
import { TableServiceBookingPayment } from '../../../../../entities/enums/table-service-booking-payment';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { TableServiceAvailabilityDay } from '../../../../../entities/settings/services/table-service-availability-day';
import { TableServiceSelectedAvailabilityBookings } from '../../../../../entities/settings/services/table-service-selected-availability-bookings';
import { TableServiceSelectedGeneralInfo } from '../../../../../entities/settings/services/table-service-selected-general-info';
import { TableServiceUpdateAvailabilityBookingsRequest } from '../../../../../entities/settings/services/table-service-update-availability-bookings-request';
import { Language } from '../../../../../entities/shared/language';
import { CatalogService } from '../../../../../services/api/catalog/catalog.service';
import { TableServiceEditItemPanel } from './table-service-edit-item-panel';

/** The index of the Disponibilità e Prenotazioni tab. */
const AVAILABILITY_BOOKINGS = 1;

const day = (dayOfWeek: number, startTime: string, endTime: string) =>
  Object.assign(new TableServiceAvailabilityDay(), {
    dayOfWeek,
    isEnabled: true,
    startTime,
    endTime,
  });

describe('TableServiceEditItemPanel', () => {
  const setup = async (values: Partial<TableServiceSelectedAvailabilityBookings>) => {
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
    const updateTableServiceAvailabilityBookings = vi.fn(
      (request: TableServiceUpdateAvailabilityBookingsRequest): Observable<string | null> =>
        of(request ? 'v2' : null),
    );
    TestBed.configureTestingModule({
      providers: [
        provideNativeDateAdapter(),
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
        {
          provide: CatalogService,
          useValue: {
            updateTableServiceAvailabilityBookings,
            tableServiceGeneralInfo: () => of(new TableServiceSelectedGeneralInfo()),
            tableServiceAvailabilityBookings: () =>
              of(
                Object.assign(new TableServiceSelectedAvailabilityBookings(), {
                  tableGroups: [{ tableQuantity: 10, seatPerTableQuantity: 4 }],
                  ...values,
                }),
              ),
          },
        },
        { provide: LanguageBehaviour, useValue: { current: signal(Language.It) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('it', {
      management: {
        settings: {
          services: {
            form: {
              tabs: { general_info: 'Info Generali' },
              tables: { days: {}, modes: {}, meal_durations: {}, payments: {}, deposit_modes: {} },
            },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(TableServiceEditItemPanel);
    const panel = fixture.componentInstance;
    fixture.componentRef.setInput('idTableService', 't1');
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    await panel['requestTab'](AVAILABILITY_BOOKINGS);
    await fixture.whenStable();
    const save = async () => {
      panel['save']();
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return { panel, save, updateTableServiceAvailabilityBookings };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should send back what it does not show', async () => {
    const otherTimeSlots = [{ startTime: '19:00', endTime: '22:00' }];
    const hidden = {
      averageMealMinutes: 100,
      minSeatPerBookingQuantity: 2,
      maxSeatPerBookingQuantity: 8,
      depositAmount: 15,
    };
    const { panel, save, updateTableServiceAvailabilityBookings } = await setup({
      ...hidden,
      bookingPayment: TableServiceBookingPayment.Deposit,
      availabilities: [
        {
          dateFrom: '2026-07-01T00:00:00.000Z',
          dateTo: '2026-07-31T00:00:00.000Z',
          days: [{ ...day(6, '12:00', '15:00'), otherTimeSlots }],
        },
      ],
    });

    expect(panel.hasUnsavedChanges()).toBe(false);
    panel['startEdit']();
    panel['draftForm'].maxBookingPerTimeSlotQuantity().value.set(5);
    await save();

    const [[request]] = updateTableServiceAvailabilityBookings.mock.calls;
    expect(request).toMatchObject({
      ...hidden,
      depositPercentage: null,
      maxBookingPerTimeSlotQuantity: 5,
    });
    expect(request.availabilities[0].days[5]).toMatchObject({ dayOfWeek: 6, otherTimeSlots });
  });

  it('should say above the buttons an error on a field with no place for it, with Salva on', async () => {
    const { panel, save, updateTableServiceAvailabilityBookings } = await setup({
      minSeatPerBookingQuantity: 2,
    });
    const refuse = (field: string) =>
      updateTableServiceAvailabilityBookings.mockReturnValue(
        throwError(
          () =>
            new HttpErrorResponse({
              status: 400,
              error: {
                status: 400,
                code: ApiErrorCode.ValidationFailed,
                errors: [{ field, code: ApiErrorCode.ValidationInvalid }],
              },
            }),
        ),
      );

    panel['startEdit']();
    for (const [field, code] of [
      ['minSeatPerBookingQuantity', ApiErrorCode.ValidationInvalid],
      ['bookingPayment', ApiErrorCode.ValidationInvalid],
      ['totalSeatQuantity', ApiErrorCode.ValidationInvalid],
      ['availabilities[0].days', ApiErrorCode.ValidationInvalid],
      ['idTableService', ApiErrorCode.ValidationFailed],
    ]) {
      refuse(field);
      await save();
      expect(panel['error']()?.code).toBe(code);
      expect(panel['isSaveDisabled']()).toBe(false);
    }
    refuse('tableGroups');
    await save();
    expect(panel['error']()).toBeNull();
    expect(panel['draftForm'].tableGroups().errors()).toHaveLength(1);
  });

  it('should show the same hours for all the days when two or more have the same', async () => {
    const period = { dateFrom: '2026-07-01T00:00:00.000Z', dateTo: '2026-07-31T00:00:00.000Z' };
    const { panel } = await setup({
      availabilities: [
        { ...period, days: [day(5, '12:00', '15:00'), day(6, '12:00', '15:00')] },
        { ...period, days: [day(5, '12:00', '15:00'), day(6, '13:00', '15:00')] },
        { ...period, days: [day(5, '12:00', '15:00')] },
      ],
    });

    const [same, different, alone] = panel['draft']().availabilities;
    expect(same).toMatchObject({
      isSameTimeForAllDays: true,
      startTime: '12:00',
      endTime: '15:00',
    });
    expect(same.days[0]).toMatchObject({ isEnabled: false, startTime: '12:00', endTime: '15:00' });
    expect(different.isSameTimeForAllDays).toBe(false);
    expect(alone.isSameTimeForAllDays).toBe(false);
    expect(panel.hasUnsavedChanges()).toBe(false);
  });
});
