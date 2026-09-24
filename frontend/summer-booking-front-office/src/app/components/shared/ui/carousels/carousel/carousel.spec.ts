import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { Carousel, CarouselSlide } from './carousel';

@Component({
  imports: [Carousel, CarouselSlide],
  template: `
    <app-carousel accessibleLabel="Recensioni">
      @for (slide of slides; track slide) {
        <p *appCarouselSlide>{{ slide }}</p>
      }
    </app-carousel>
  `,
})
class CarouselHost {
  readonly slides = ['A', 'B', 'C', 'D', 'E'];
}

describe('Carousel', () => {
  let fixture: ComponentFixture<CarouselHost>;
  let element: HTMLElement;

  const activeDot = () =>
    [...element.querySelectorAll('.app__carousel__dot')].findIndex((dot) =>
      dot.classList.contains('app__carousel__dot__active'),
    );
  const click = async (selector: string) => {
    element.querySelector<HTMLButtonElement>(selector)!.click();
    await fixture.whenStable();
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    fixture = TestBed.createComponent(CarouselHost);
    element = fixture.nativeElement;
    await fixture.whenStable();
  });

  it('should render every slide and one dot per position', () => {
    // Without layout (jsdom) one slide is visible per view: five positions.
    expect(element.querySelectorAll('.app__carousel__slide').length).toBe(5);
    expect(element.querySelectorAll('.app__carousel__dot').length).toBe(5);
    expect(activeDot()).toBe(0);
  });

  it('should show only the arrows that lead somewhere', async () => {
    expect(element.querySelector('.app__carousel__arrow__previous')).toBeNull();
    expect(element.querySelector('.app__carousel__arrow__next')).toBeTruthy();

    await click('.app__carousel__arrow__next');
    expect(activeDot()).toBe(1);
    expect(element.querySelector('.app__carousel__arrow__previous')).toBeTruthy();

    await click('.app__carousel__dot:last-child');
    expect(activeDot()).toBe(4);
    expect(element.querySelector('.app__carousel__arrow__next')).toBeNull();
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
