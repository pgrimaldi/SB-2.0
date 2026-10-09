import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IconButton } from './icon-button';

@Component({
  imports: [IconButton],
  template: `
    <app-icon-button
      class="path"
      label="Impostazioni"
      [pathIcon]="['/assets/images/settings.svg']"
    />
    <app-icon-button class="material" label="Notifiche" [matIcon]="['notifications']" />
    <app-icon-button
      class="both"
      label="Entrambe"
      [matIcon]="['settings']"
      [pathIcon]="['/assets/images/x.svg']"
    />
    <app-icon-button class="text" label="Account">S</app-icon-button>
  `,
})
class IconButtonHost {}

describe('IconButton', () => {
  it('should show the Material icon, else the image, else the text', async () => {
    const fixture = TestBed.createComponent(IconButtonHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const button = (name: string) => element.querySelector(`.${name} button`)!;

    expect(button('path').querySelector('img')?.getAttribute('src')).toBe(
      '/assets/images/settings.svg',
    );
    expect(button('material').querySelector('mat-icon')?.textContent?.trim()).toBe('notifications');
    expect(button('both').querySelector('mat-icon')?.textContent?.trim()).toBe('settings'); // matIcon wins
    expect(button('both').querySelector('img')).toBeNull();
    expect(button('text').textContent?.trim()).toBe('S');
    expect(button('text').querySelector('img, mat-icon')).toBeNull();
  });
});
