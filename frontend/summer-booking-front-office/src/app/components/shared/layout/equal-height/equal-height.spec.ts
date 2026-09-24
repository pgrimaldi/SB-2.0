import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { EqualHeight } from './equal-height';

@Component({
  imports: [EqualHeight],
  template: `
    <main appEqualHeight=".card">
      <div class="card" data-height="380"></div>
      <div class="card" data-height="466"></div>
      <div class="card" data-height="412"></div>
    </main>
  `,
})
class EqualHeightHost {}

describe('EqualHeight', () => {
  it('should expose the tallest element height to every element', async () => {
    // jsdom has no layout: each element reports the height written in data-height.
    const original = HTMLElement.prototype.getBoundingClientRect;
    HTMLElement.prototype.getBoundingClientRect = function (this: HTMLElement) {
      return { height: Number(this.dataset['height'] ?? 0) } as DOMRect;
    };

    try {
      const fixture = TestBed.createComponent(EqualHeightHost);
      await fixture.whenStable();
      const main: HTMLElement = fixture.nativeElement.querySelector('main');

      expect(main.style.getPropertyValue('--app-equal-height')).toBe('466px');
    } finally {
      HTMLElement.prototype.getBoundingClientRect = original;
    }
  });
});
