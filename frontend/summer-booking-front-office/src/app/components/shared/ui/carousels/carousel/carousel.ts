import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  contentChildren,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { resolveIcons } from '../../icons/icons';
import { FormatTextPipe } from '../../texts/format-text';
import { CarouselSlide } from './carousel-slide';

const SWIPE_THRESHOLD = 0.2;
/** Pointer movement below this distance is still treated as a click. */
const DRAG_START_DISTANCE = 5;

export interface CarouselTexts {
  previous?: string;
  next?: string;
  /** With `{{index}}` and `{{total}}`. */
  slide?: string;
  /** With `{{position}}`. */
  position?: string;
}

/** Slides per view and the peek of the next slide are set in CSS (see carousel.scss). */
@Component({
  selector: 'app-carousel',
  imports: [FormatTextPipe, MatIconModule, NgTemplateOutlet],
  templateUrl: './carousel.html',
  styleUrl: './carousel.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(keydown.arrowleft)': 'previous()',
    '(keydown.arrowright)': 'next()',
  },
})
export class Carousel implements OnDestroy {
  readonly accessibleLabel = input<string>();
  readonly texts = input<CarouselTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [previous arrow, next arrow]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [previous arrow, next arrow]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  private readonly slideDirectives = contentChildren(CarouselSlide);
  private readonly viewport = viewChild.required<ElementRef<HTMLElement>>('viewport');
  private readonly viewportWidth = signal(0);
  private readonly slideWidth = signal(0);
  private dragStartX: number | null = null;
  private observer?: ResizeObserver;

  protected readonly slides = computed(() => this.slideDirectives().map((slide) => slide.template));
  protected readonly index = signal(0);
  protected readonly dragOffset = signal(0);
  protected readonly dragging = signal(false);

  protected readonly perView = computed(() => {
    const slideWidth = this.slideWidth();
    return slideWidth > 0 ? Math.max(1, Math.floor(this.viewportWidth() / slideWidth + 0.001)) : 1;
  });
  protected readonly maxIndex = computed(() => Math.max(0, this.slides().length - this.perView()));
  protected readonly positions = computed(() =>
    Array.from({ length: this.maxIndex() + 1 }, (_, position) => position),
  );

  /** The last position aligns the last slide with the right edge instead of leaving a gap. */
  private readonly offset = computed(() => {
    const maxOffset = Math.max(0, this.slides().length * this.slideWidth() - this.viewportWidth());
    return Math.min(this.index() * this.slideWidth(), maxOffset);
  });
  protected readonly transform = computed(
    () => `translate3d(${this.dragOffset() - this.offset()}px, 0, 0)`,
  );

  constructor() {
    afterNextRender(() => {
      this.measure();
      if (typeof ResizeObserver === 'undefined') {
        return;
      }
      this.observer = new ResizeObserver(() => this.measure());
      this.observer.observe(this.viewport().nativeElement);
    });
  }

  previous(): void {
    this.goTo(this.index() - 1);
  }

  next(): void {
    this.goTo(this.index() + 1);
  }

  protected goTo(position: number): void {
    this.index.set(Math.min(Math.max(position, 0), this.maxIndex()));
  }

  /** Only slides fully inside the viewport are exposed to assistive technologies and focus. */
  protected isVisible(slide: number): boolean {
    const slideWidth = this.slideWidth();
    if (!slideWidth) {
      return slide < this.perView();
    }
    const start = slide * slideWidth;
    return (
      start >= this.offset() - 1 && start + slideWidth <= this.offset() + this.viewportWidth() + 1
    );
  }

  protected onPointerDown(event: PointerEvent): void {
    if (event.button !== 0 || (event.target as Element).closest('button')) {
      return;
    }
    this.dragStartX = event.clientX;
  }

  protected onPointerMove(event: PointerEvent): void {
    if (this.dragStartX === null) {
      return;
    }
    const distance = event.clientX - this.dragStartX;
    if (!this.dragging() && Math.abs(distance) < DRAG_START_DISTANCE) {
      return;
    }
    if (!this.dragging()) {
      this.dragging.set(true);
      this.viewport().nativeElement.setPointerCapture(event.pointerId);
    }
    const atEdge =
      (distance > 0 && this.index() === 0) || (distance < 0 && this.index() === this.maxIndex());
    this.dragOffset.set(atEdge ? distance / 3 : distance);
  }

  protected onPointerUp(): void {
    if (this.dragging()) {
      const distance = this.dragOffset();
      const threshold = this.slideWidth() * SWIPE_THRESHOLD;
      if (distance <= -threshold) {
        this.next();
      } else if (distance >= threshold) {
        this.previous();
      }
    }
    this.dragStartX = null;
    this.dragging.set(false);
    this.dragOffset.set(0);
  }

  private measure(): void {
    const viewport = this.viewport().nativeElement;
    const firstSlide = viewport.querySelector('.carousel__slide');
    this.viewportWidth.set(viewport.clientWidth);
    this.slideWidth.set(firstSlide?.getBoundingClientRect().width ?? 0);
    this.goTo(this.index());
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
