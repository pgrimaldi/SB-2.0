import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Carousel } from './carousel';
import { CarouselSlide } from './carousel-slide';

@Component({
  imports: [Carousel, CarouselSlide],
  template: `
    <app-carousel
      accessibleLabel="Recensioni"
      [pathIcon]="['/previous.svg', '/next.svg']"
      [texts]="texts"
    >
      @for (slide of slides; track slide) {
        <p *appCarouselSlide>{{ slide }}</p>
      }
    </app-carousel>
  `,
})
class CarouselHost {
  readonly slides = ['A', 'B', 'C', 'D', 'E'];
  readonly texts = {
    previous: 'Precedente',
    next: 'Successivo',
    slide: '{{index}} di {{total}}',
    position: 'Vai alla posizione {{position}}',
  };
}

describe('Carousel', () => {
  let fixture: ComponentFixture<CarouselHost>;
  let element: HTMLElement;

  const activeDot = () =>
    [...element.querySelectorAll('.carousel__dot')].findIndex((dot) =>
      dot.classList.contains('carousel__dot__active'),
    );
  const click = async (selector: string) => {
    element.querySelector<HTMLButtonElement>(selector)!.click();
    await fixture.whenStable();
  };

  beforeEach(async () => {
    fixture = TestBed.createComponent(CarouselHost);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  it('should render every slide and one dot per position', () => {
    // Without layout (jsdom) one slide is visible per view: five positions.
    expect(element.querySelectorAll('.carousel__slide').length).toBe(5);
    expect(element.querySelectorAll('.carousel__dot').length).toBe(5);
    expect(activeDot()).toBe(0);
  });

  it('should show only the arrows that lead somewhere', async () => {
    expect(element.querySelector('.carousel__arrow__previous')).toBeNull();
    expect(element.querySelector('.carousel__arrow__next')).toBeTruthy();

    await click('.carousel__arrow__next');
    expect(activeDot()).toBe(1);
    expect(element.querySelector('.carousel__arrow__previous')).toBeTruthy();
    // Icons: [previous arrow, next arrow].
    expect(element.querySelector('.carousel__arrow__previous img')?.getAttribute('src')).toBe(
      '/previous.svg',
    );
    expect(element.querySelector('.carousel__arrow__next img')?.getAttribute('src')).toBe(
      '/next.svg',
    );

    await click('.carousel__dot:last-child');
    expect(activeDot()).toBe(4);
    expect(element.querySelector('.carousel__arrow__next')).toBeNull();
  });

  it('should name region, slides, arrows and dots with the given texts', async () => {
    const label = (selector: string) => element.querySelector(selector)?.getAttribute('aria-label');
    expect(label('[role="region"]')).toBe('Recensioni');
    expect(label('.carousel__slide__content')).toBe('1 di 5');
    expect(label('.carousel__arrow__next')).toBe('Successivo');
    expect(label('.carousel__dot')).toBe('Vai alla posizione 1');

    await click('.carousel__arrow__next');
    expect(label('.carousel__arrow__previous')).toBe('Precedente');
  });

  it('should move with the keyboard arrows', async () => {
    const carousel = element.querySelector('app-carousel')!;

    carousel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    await fixture.whenStable();
    expect(activeDot()).toBe(1);

    carousel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    carousel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    await fixture.whenStable();
    expect(activeDot()).toBe(0);
  });
});
