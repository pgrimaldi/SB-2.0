import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { IconButton } from './icon-button';

@Component({
  imports: [IconButton],
  template: `
    <app-icon-button class="icon" label="Impostazioni" icon="/assets/images/settings.svg" />
    <app-icon-button class="text" label="Account">S</app-icon-button>
  `,
})
class IconButtonHost {}

describe('IconButton', () => {
  it('should show the icon or the text, named by its label', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(IconButtonHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const icon = element.querySelector('.icon button')!;
    const text = element.querySelector('.text button')!;

    expect(icon.getAttribute('aria-label')).toBe('Impostazioni');
    expect(icon.querySelector('img')?.getAttribute('src')).toBe('/assets/images/settings.svg');
    expect(text.getAttribute('aria-label')).toBe('Account');
    expect(text.textContent?.trim()).toBe('S');
    expect(text.querySelector('img')).toBeNull();
  });
});
