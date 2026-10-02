import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PageTitle } from './page-title';

@Component({
  imports: [PageTitle],
  template: `
    <app-page-title
      class="path"
      text="Magazzino"
      [pathIcon]="['/assets/images/warehouse-dark.svg']"
    />
    <app-page-title class="none" text="Impostazioni" />
  `,
})
class PageTitleHost {}

describe('PageTitle', () => {
  it('should be the h1 of the page, with its icon on the left', async () => {
    const fixture = TestBed.createComponent(PageTitleHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const title = element.querySelector('.path h1')!;

    expect(title.textContent?.trim()).toBe('Magazzino');
    expect(title.querySelector('img')?.getAttribute('src')).toBe(
      '/assets/images/warehouse-dark.svg',
    );
    expect(title.querySelector('img')?.getAttribute('alt')).toBe(''); // the text names it
  });

  it('should show no icon when none is given', async () => {
    const fixture = TestBed.createComponent(PageTitleHost);
    await fixture.whenStable();
    const title: HTMLElement = fixture.nativeElement.querySelector('.none h1');

    expect(title.textContent?.trim()).toBe('Impostazioni');
    expect(title.querySelector('img, mat-icon')).toBeNull();
  });
});
