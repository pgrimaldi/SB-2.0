import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import {
  MapBackground,
  MapSettings,
} from '../../../../../entities/settings/map-configuration/map-settings';
import { MapService } from '../../../../../services/api/catalog/map.service';
import { MapBackgroundPanel } from './map-background-panel';

describe('MapBackgroundPanel', () => {
  const setup = async () => {
    const removeBackground = vi.fn(() => of(new MapSettings()));
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: MapService, useValue: { removeBackground } },
      ],
    });
    const settings = new MapSettings();
    settings.background = Object.assign(new MapBackground(), {
      isCustom: true,
      url: 'https://example.test/sfondo.png',
    });
    const fixture = TestBed.createComponent(MapBackgroundPanel);
    fixture.componentRef.setInput('settings', settings);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const click = async (button: HTMLButtonElement) => {
      button.click();
      await fixture.whenStable();
    };
    const edit = () =>
      [
        ...element.querySelectorAll<HTMLButtonElement>('.map__background__panel__actions button'),
      ].find(
        (button) => button.textContent?.trim() === 'management.settings.map_configuration.edit',
      )!;
    const trash = () =>
      element.querySelector<HTMLButtonElement>('.map__background__panel__delete button')!;
    const popupButtons = () => [
      ...document.querySelectorAll<HTMLButtonElement>('.confirm__popup__actions button'),
    ];
    return { element, click, edit, trash, popupButtons, removeBackground };
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
});
