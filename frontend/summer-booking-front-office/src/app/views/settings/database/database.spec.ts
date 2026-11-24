import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS, provideNativeDateAdapter } from '@angular/material/core';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { MAX_BOOKING_IDS } from '../../../entities/database/move-to-trash-request';
import { ApiErrorCode } from '../../../entities/errors/api-error-codes';
import { BulkResult } from '../../../entities/shared/bulk-result';
import { DatabaseService } from '../../../services/api/database/database.service';
import { Database } from './database';

describe('Database', () => {
  const setup = async (result: BulkResult) => {
    const trashItemList = vi.fn(() => of({ total: 0, rows: [] }));
    const restoreBookingItem = vi.fn(() => of(result));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { isTrash: true } } } },
        { provide: DatabaseService, useValue: { trashItemList, restoreBookingItem } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const translate = TestBed.inject(TranslateService);
    const options = { options: {} };
    translate.setTranslation('it', {
      management: {
        settings: {
          database: {
            filters: {
              booking_typology: options,
              agreement_booking: options,
              payment_status: options,
              chips: {},
            },
          },
        },
      },
    });
    translate.use('it');
    const fixture = TestBed.createComponent(Database);
    await fixture.whenStable();
    const select = async (ids: string[]) => {
      fixture.componentInstance['idSelectedBookings'].set(ids);
      await fixture.whenStable();
    };
    const restore = () =>
      fixture.nativeElement.querySelector('.database__restore button') as HTMLButtonElement;
    return { fixture, select, restore, trashItemList, restoreBookingItem };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should restore the chosen bookings and read the trash again', async () => {
    const { fixture, select, restore, trashItemList, restoreBookingItem } = await setup({
      succeeded: ['b1', 'b2'],
      failed: [],
    });
    await select(['b1', 'b2']);
    trashItemList.mockClear();

    restore().click();
    await fixture.whenStable();
    expect(restoreBookingItem).toHaveBeenCalledWith(['b1', 'b2']);
    expect(trashItemList).toHaveBeenCalled();
    expect(restore().disabled).toBe(true); // nothing chosen any more
    expect(document.querySelector('.alert__popup')).toBeNull();
  });

  it('should say why some of the chosen bookings could not be restored', async () => {
    const { fixture, select, restore } = await setup({
      succeeded: ['b1'],
      failed: [{ id: 'b2', code: ApiErrorCode.BookingRestoreWindowExpired }],
    });
    await select(['b1', 'b2']);

    restore().click();
    await fixture.whenStable();
    expect(document.querySelector('.alert__popup')?.textContent).toContain('error.unknown');
    expect(restore().disabled).toBe(true);
  });

  it('should not act on more bookings than the backend takes at a time', async () => {
    const { select, restore } = await setup({ succeeded: [], failed: [] });
    await select(Array.from({ length: MAX_BOOKING_IDS + 1 }, (_, index) => `b${index}`));
    expect(restore().disabled).toBe(true);
    await select(['b1']);
    expect(restore().disabled).toBe(false);
  });
});
