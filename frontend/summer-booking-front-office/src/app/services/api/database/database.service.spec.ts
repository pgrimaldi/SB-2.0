import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PageRequest } from '../../../entities/pagination/page';
import { DatabaseService } from './database.service';

describe('DatabaseService', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const controller = TestBed.inject(HttpTestingController);
    return {
      database: TestBed.inject(DatabaseService),
      controller,
      bodyOf: (route: string): unknown =>
        controller.expectOne({ method: 'POST', url: `${environment.apiBaseUrl}${route}` }).request
          .body,
    };
  };

  it('should read the tables and the agreement partners, and empty the trash, with the APIs of the backend', () => {
    const { database, controller, bodyOf } = setup();
    const request: PageRequest = {
      page: 1,
      pageSize: 10,
      search: '',
      filters: [],
      sortField: '',
      sortDirection: 'Ascending',
    };
    database.existingBookingList(request).subscribe();
    database.trashItemList(request).subscribe();
    database.affiliatedHotelCombobox().subscribe();
    database.emptyTrash().subscribe();

    expect(bodyOf('/booking/reservations/reservation-list')).toEqual(request);
    expect(bodyOf('/booking/trash/trash-list')).toEqual(request);
    expect(bodyOf('/catalog/agreement-partners/agreement-partner-combobox')).toBeNull();
    expect(bodyOf('/booking/trash/empty')).toBeNull();
    controller.verify();
  });

  it('should clean up, restore and purge the bookings with the APIs of the trash', () => {
    const { database, controller, bodyOf } = setup();

    database.moveToTrash({ ids: ['b1'], password: 'segreta' }).subscribe();
    database.restoreBookingItem(['b2']).subscribe();
    database.deleteSelectedItem(['b3']).subscribe();

    expect(bodyOf('/booking/cleanup/move-to-trash')).toEqual({ ids: ['b1'], password: 'segreta' });
    expect(bodyOf('/booking/trash/restore')).toEqual({ ids: ['b2'] });
    expect(bodyOf('/booking/trash/purge')).toEqual({ ids: ['b3'] });
    controller.verify();
  });
});
