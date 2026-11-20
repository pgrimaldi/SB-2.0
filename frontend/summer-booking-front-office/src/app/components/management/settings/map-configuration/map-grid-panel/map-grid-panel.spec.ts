import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { UnsavedChangesBehaviour } from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { MapMixedSpotIcon } from '../../../../../entities/enums/map-mixed-spot-icon';
import { MapUmbrellaIcon } from '../../../../../entities/enums/map-umbrella-icon';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { MapSettings } from '../../../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { MapGridPanel } from './map-grid-panel';

describe('MapGridPanel', () => {
  const settings = (): MapSettings => {
    const read = new MapSettings();
    read.rows = 6;
    read.columns = 16;
    read.tiltDegrees = 8;
    read.icons.umbrella.free.name = MapUmbrellaIcon.Umbrella3;
    read.icons.umbrella.occupied.name = MapMixedSpotIcon.MixedSpot4;
    read.configurationVersion = 'c1';
    return read;
  };

  const setup = async () => {
    const updateConfiguration = vi.fn();
    const readSettings = vi.fn(() => of(settings()));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: MapService, useValue: { updateConfiguration, settings: readSettings } },
      ],
    });
    const fixture = TestBed.createComponent(MapGridPanel);
    fixture.componentRef.setInput('settings', settings());
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const button = (key: string) =>
      [...element.querySelectorAll<HTMLButtonElement>('.map__grid__panel__actions button')].find(
        (candidate) =>
          candidate.textContent?.trim() === `management.settings.map_configuration.${key}`,
      );
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    return { fixture, element, button, settle, updateConfiguration, readSettings };
  };

  it('should drop an error of the backend on a field the user did not change once Annulla restores the saved grid', async () => {
    const { element, button, settle, updateConfiguration } = await setup();
    updateConfiguration.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: ApiErrorCode.ValidationFailed,
              errors: [{ field: 'umbrellaIcons', code: ApiErrorCode.ValidationInvalid }],
            },
          }),
      ),
    );
    const rows = () => element.querySelector<HTMLInputElement>('input')!;
    const iconsError = () => element.querySelector('.map__grid__panel__icons__error');

    button('edit')!.click();
    await settle();
    rows().value = '7';
    rows().dispatchEvent(new Event('input'));
    await settle();
    button('save')!.click();
    await settle();
    expect(updateConfiguration).toHaveBeenCalledTimes(1);
    expect(iconsError()).not.toBeNull();

    button('cancel')!.click();
    await settle();
    const unsaved = TestBed.inject(UnsavedChangesBehaviour);
    expect(unsaved.asking()).toBe(true);
    unsaved.answer(true);
    await settle();
    expect(rows().value).toBe('6');

    button('edit')!.click();
    await settle();
    expect(iconsError()).toBeNull();
    expect(button('save')!.disabled).toBe(false);
  });

  it('should read the grid again and say why when someone changed it after the read', async () => {
    const { fixture, element, button, settle, updateConfiguration, readSettings } = await setup();
    readSettings.mockReturnValue(of({ ...settings(), rows: 9, configurationVersion: 'c2' }));
    updateConfiguration.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );
    const rows = () => element.querySelector<HTMLInputElement>('input')!;
    const type = async (text: string) => {
      rows().value = text;
      rows().dispatchEvent(new Event('input'));
      await settle();
    };

    button('edit')!.click();
    await settle();
    await type('7');
    button('save')!.click();
    await settle();
    expect(updateConfiguration).toHaveBeenLastCalledWith(
      expect.objectContaining({ rowQuantity: 7 }),
      'c1',
    );
    expect(readSettings).toHaveBeenCalledTimes(1);
    expect(rows().value).toBe('9');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.map__grid__panel__error')).toBeNull();

    updateConfiguration.mockReturnValue(of('c3'));
    button('edit')!.click();
    await settle();
    await type('8');
    button('save')!.click();
    await settle();
    expect(updateConfiguration).toHaveBeenLastCalledWith(
      expect.objectContaining({ rowQuantity: 8 }),
      'c2',
    );
  });

  it('should neither ask nor call when every value is back to the saved one', async () => {
    const { fixture, element, button, settle, updateConfiguration } = await setup();
    const rows = () => element.querySelector<HTMLInputElement>('input')!;
    const type = async (text: string) => {
      rows().value = text;
      rows().dispatchEvent(new Event('input'));
      await settle();
    };

    button('edit')!.click();
    await settle();
    element.querySelector<HTMLButtonElement>('.map__grid__panel__icon__button button')!.click();
    await settle();
    document.querySelector<HTMLButtonElement>('.icon__picker__popup__icon__chosen')!.click();
    await settle();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    await type('7');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    await type('6');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);

    button('save')!.click();
    await settle();
    expect(updateConfiguration).not.toHaveBeenCalled();
    expect(button('edit')).toBeDefined();
  });
});
