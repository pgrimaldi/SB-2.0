import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of, throwError } from 'rxjs';
import { MapBackgroundImage } from '../../../../../entities/enums/map-background-image';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import {
  MapBackground,
  MapSettings,
} from '../../../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { MapBackgroundPanel } from './map-background-panel';

describe('MapBackgroundPanel', () => {
  const setup = async () => {
    const removeBackground = vi.fn(() => of(new MapSettings()));
    const updateBackground = vi.fn();
    const readSettings = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        {
          provide: MapService,
          useValue: { removeBackground, updateBackground, settings: readSettings },
        },
      ],
    });
    const settings = new MapSettings();
    settings.background = Object.assign(new MapBackground(), {
      isCustom: true,
      url: 'https://example.test/sfondo.png',
    });
    settings.backgroundVersion = 'b1';
    const fixture = TestBed.createComponent(MapBackgroundPanel);
    fixture.componentRef.setInput('settings', settings);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const click = async (button: HTMLButtonElement) => {
      button.click();
      await fixture.whenStable();
    };
    const action = (key: string) =>
      [
        ...element.querySelectorAll<HTMLButtonElement>('.map__background__panel__actions button'),
      ].find(
        (button) => button.textContent?.trim() === `management.settings.map_configuration.${key}`,
      );
    const edit = () => action('edit')!;
    const trash = () =>
      element.querySelector<HTMLButtonElement>('.map__background__panel__delete button')!;
    const popupButtons = () => [
      ...document.querySelectorAll<HTMLButtonElement>('.confirm__popup__actions button'),
    ];
    const slots = () => [
      ...element.querySelectorAll<HTMLButtonElement>('.map__background__panel__slot'),
    ];
    return {
      element,
      click,
      action,
      edit,
      trash,
      popupButtons,
      slots,
      removeBackground,
      updateBackground,
      readSettings,
    };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should delete the uploaded background only once the deletion is confirmed', async () => {
    const { element, click, edit, trash, popupButtons, removeBackground } = await setup();

    await click(edit());
    await click(trash());
    expect(document.querySelector('.confirm__popup')).not.toBeNull();
    expect(removeBackground).not.toHaveBeenCalled();
    await click(popupButtons()[0]);
    expect(removeBackground).not.toHaveBeenCalled();

    await click(trash());
    await click(popupButtons()[1]);
    expect(removeBackground).toHaveBeenCalledTimes(1);
    expect(element.querySelector('.map__background__panel__delete')).toBeNull();
  });

  it('should read the background again and say why when someone changed it after the read', async () => {
    const { element, click, action, edit, slots, updateBackground, readSettings } = await setup();
    const changed = new MapSettings();
    changed.background = Object.assign(new MapBackground(), {
      name: MapBackgroundImage.BackgroundBoardwalk,
    });
    changed.backgroundVersion = 'b2';
    readSettings.mockReturnValue(of(changed));
    updateBackground.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { status: 409, code: ApiErrorCode.ResourceVersionConflict },
          }),
      ),
    );

    await click(edit());
    await click(slots()[0]);
    await click(action('save')!);
    expect(updateBackground).toHaveBeenCalledWith(MapBackgroundImage.BackgroundBeach, 'b1');
    expect(readSettings).toHaveBeenCalledTimes(1);
    expect(slots()[1].classList).toContain('map__background__panel__slot__chosen');
    expect(action('edit')).toBeDefined();
    expect(document.querySelector('.table__error__popup')).not.toBeNull();
    expect(element.querySelector('.map__background__panel__error')).toBeNull();

    updateBackground.mockReturnValue(of('b3'));
    await click(edit());
    await click(slots()[0]);
    await click(action('save')!);
    expect(updateBackground).toHaveBeenLastCalledWith(MapBackgroundImage.BackgroundBeach, 'b2');
  });
});
