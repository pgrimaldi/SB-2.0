import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { MapBackgroundImage } from '../../../entities/enums/map-background-image';
import { MapMixedSpotIcon } from '../../../entities/enums/map-mixed-spot-icon';
import { MapUmbrellaIcon } from '../../../entities/enums/map-umbrella-icon';
import { DEFAULT_MAP_GRID, MapGrid } from '../../../entities/settings/map-configuration/map-grid';
import { MapSettings } from '../../../entities/settings/map-configuration/map-settings';
import { MapService } from './map.service';

describe('MapService', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    return {
      map: TestBed.inject(MapService),
      controller: TestBed.inject(HttpTestingController),
    };
  };

  it('should read the settings without parameters', () => {
    const { map, controller } = setup();
    const settings: MapSettings = { ...new MapSettings(), rows: 6, columns: 16, tiltDegrees: 8 };
    let answer: MapSettings | undefined;

    map.settings().subscribe((read) => (answer = read));

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'GET' && url === `${environment.apiBaseUrl}/catalog/map/settings`,
    );
    expect(request.request.params.keys()).toEqual([]);
    request.flush(settings);
    expect(answer).toEqual(settings);
    controller.verify();
  });

  it('should send the custom background as the file field of a multipart form', () => {
    const { map, controller } = setup();
    const image = new File(['png'], 'sfondo.png', { type: 'image/png' });

    map.uploadBackground(image).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/map/background`,
    ).request;
    expect(request.body).toBeInstanceOf(FormData);
    expect((request.body as FormData).get('file')).toBe(image);
    controller.verify();
  });

  it('should remove the custom background without a body', () => {
    const { map, controller } = setup();

    map.removeBackground().subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'DELETE' && url === `${environment.apiBaseUrl}/catalog/map/background`,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should send the name of the chosen background', () => {
    const { map, controller } = setup();

    map.updateBackground(MapBackgroundImage.BackgroundBoardwalk).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/catalog/map/update-background`,
    ).request;
    expect(request.body).toEqual({ backgroundName: MapBackgroundImage.BackgroundBoardwalk });
    controller.verify();
  });

  it('should send every value of the grid, icons included, and nothing else', () => {
    const { map, controller } = setup();
    const grid: MapGrid = {
      ...DEFAULT_MAP_GRID,
      rowQuantity: 6,
      maxRowItemQuantity: 16,
      tiltDegrees: 8,
      umbrellaIcons: {
        freeIcon: MapUmbrellaIcon.Umbrella3,
        occupiedIcon: MapMixedSpotIcon.MixedSpot4,
      },
    };

    map.updateConfiguration({ ...grid, extraField: 'not sent' } as MapGrid).subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'PUT' && url === `${environment.apiBaseUrl}/catalog/map/update-configuration`,
    ).request;
    expect(request.body).toEqual(grid);
    controller.verify();
  });

  it('should reset the map without a body', () => {
    const { map, controller } = setup();

    map.reset().subscribe();

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/map/reset`,
    ).request;
    expect(request.body).toBeNull();
    controller.verify();
  });

  it('should ask the areas of the dropdowns with the property only', () => {
    const { map, controller } = setup();
    let answer: unknown;

    map.sectorAreaCombobox('p1').subscribe((read) => (answer = read));

    const request = controller.expectOne(
      ({ method, url }) =>
        method === 'POST' && url === `${environment.apiBaseUrl}/catalog/map/sector-area-combobox`,
    );
    expect(request.request.body).toEqual({ idProperty: 'p1' });
    request.flush([{ id: 'a1', value: 'Area lettini' }]);
    expect(answer).toEqual([{ id: 'a1', value: 'Area lettini' }]);
    controller.verify();
  });
});
