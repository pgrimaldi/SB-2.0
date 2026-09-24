import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CardHub } from './card-hub';

@Component({
  imports: [CardHub],
  template: `
    <app-card-hub iconSrc="/icon.png">
      <span cardHubHeading>Tutto in uno</span>
      Testo della card
    </app-card-hub>
  `,
})
class CardHubHost {}

describe('CardHub', () => {
  it('should render icon, heading and projected content', async () => {
    const fixture = TestBed.createComponent(CardHubHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('mat-card')).toBeTruthy();
    expect(element.querySelector('img')?.getAttribute('src')).toBe('/icon.png');
    expect(element.querySelector('h3')?.textContent?.trim()).toBe('Tutto in uno');
    expect(element.querySelector('.card__hub__content')?.textContent?.trim()).toBe(
      'Testo della card',
    );
  });
});
