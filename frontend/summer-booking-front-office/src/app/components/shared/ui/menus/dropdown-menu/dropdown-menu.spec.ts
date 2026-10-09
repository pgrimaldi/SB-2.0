import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { DropdownMenu } from './dropdown-menu';

@Component({
  imports: [DropdownMenu],
  template: `
    <app-dropdown-menu [items]="items" [pathIcon]="['/it.svg', '/gb.svg']" [(selected)]="selected">
      Lingua
    </app-dropdown-menu>
  `,
})
class DropdownMenuHost {
  readonly items = [
    { value: 'it', label: 'Italiano' },
    { value: 'en', label: 'English' },
  ];
  selected = 'it';
}

describe('DropdownMenu', () => {
  it('should show one icon per item, in the order of the items, and select an item', async () => {
    TestBed.configureTestingModule({
      providers: [{ provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } }],
    });
    const fixture = TestBed.createComponent(DropdownMenuHost);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('.dropdown__menu__trigger').click();
    await fixture.whenStable();
    const options = [...document.querySelectorAll<HTMLButtonElement>('[mat-menu-item]')];
    expect(options.map((option) => option.querySelector('img')?.getAttribute('src'))).toEqual([
      '/it.svg',
      '/gb.svg',
    ]);
    expect(options.map((option) => option.textContent?.trim())).toEqual(['Italiano', 'English']);

    options[1].click();
    expect(fixture.componentInstance.selected).toBe('en');
  });
});
