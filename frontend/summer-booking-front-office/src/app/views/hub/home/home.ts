import { ChangeDetectionStrategy, Component, OnDestroy, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { Footer } from '../../../components/hub/footer/footer';
import { Header } from '../../../components/hub/header/header';
import { I18nText } from '../../../components/i18n/i18n-text/i18n-text';
import { EqualHeight } from '../../../components/shared/layout/equal-height/equal-height';
import { Button } from '../../../components/shared/ui/buttons/button/button';
import { Card } from '../../../components/shared/ui/cards/card/card';
import { CardLogo } from '../../../components/shared/ui/cards/card-logo/card-logo';
import { Carousel } from '../../../components/shared/ui/carousels/carousel/carousel';
import { CarouselSlide } from '../../../components/shared/ui/carousels/carousel/carousel-slide';

@Component({
  selector: 'app-home',
  imports: [
    Button,
    Card,
    CardLogo,
    Carousel,
    CarouselSlide,
    EqualHeight,
    Footer,
    Header,
    I18nText,
    TranslatePipe,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home implements OnDestroy {
  private readonly translateService = inject(TranslateService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  protected readonly managementCards = [
    { key: 'all', icon: 'all-in-one' },
    { key: 'agility', icon: 'agility' },
    { key: 'control', icon: 'precise-control' },
  ] as const;

  protected readonly shiftCards = [
    { key: 'reserve', icon: 'reserve-attraction' },
    { key: 'explode', icon: 'booking-boost-online' },
    { key: 'services', icon: 'extra-services' },
  ] as const;

  protected readonly aboutUsReviews = [
    { key: 'anna', icon: 'about-us-one' },
    { key: 'carla', icon: 'about-us-two' },
    { key: 'franco', icon: 'about-us-three' },
    { key: 'marco', icon: 'about-us-four' },
    { key: 'paolo', icon: 'about-us-five' },
  ] as const;

  protected readonly mediaOutlets = [
    { logo: 'tgr', name: 'TGR' },
    { logo: 'la-nazione', name: 'La Nazione' },
    { logo: 'la-repubblica', name: 'la Repubblica' },
    { logo: 'il-tirreno', name: 'Il Tirreno' },
    { logo: 'in-toscana', name: 'intoscana.it' },
    { logo: 'go-news.it', name: 'gonews.it' },
    { logo: 'mondo-balneare', name: 'Mondo Balneare' },
  ] as const;

  /** Title and description of the page, in the current language. */
  private readonly pageSubscription: Subscription;

  constructor() {
    this.pageSubscription = this.translateService
      .stream(['home.page.title', 'home.page.description'])
      .subscribe((page: Record<string, string>) => this.showPageTexts(page));
  }

  /** Subscription of the page texts: title and description follow the language. */
  private showPageTexts(page: Record<string, string>): void {
    this.title.setTitle(page['home.page.title']);
    this.meta.updateTag({ name: 'description', content: page['home.page.description'] });
  }

  ngOnDestroy(): void {
    this.pageSubscription.unsubscribe();
  }
}
