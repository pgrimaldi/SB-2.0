import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Card } from './card';

@Component({
  imports: [Card],
  template: `
    <app-card [pathIcon]="['/icon.png']">
      <span cardHeading>Tutto in uno</span>
      Testo della card
    </app-card>
  `,
})
class CardHost {}

describe('Card', () => {
  it('should render icon, heading and projected content', async () => {
    const fixture = TestBed.createComponent(CardHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('mat-card')).toBeTruthy();
    expect(element.querySelector('img')?.getAttribute('src')).toBe('/icon.png');
    expect(element.querySelector('h3')?.textContent?.trim()).toBe('Tutto in uno');
    expect(element.querySelector('.card__content')?.textContent?.trim()).toBe('Testo della card');
  });
});
