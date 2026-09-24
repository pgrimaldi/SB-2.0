import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { HubFooter } from '../../../components/hub/footer/footer';
import { HubHeader } from '../../../components/hub/header/header';
import { I18nText } from '../../../components/shared/i18n/i18n-text/i18n-text';
import { EqualHeight } from '../../../components/shared/layout/equal-height/equal-height';
import { ButtonComponent } from '../../../components/shared/ui/buttons/button/button.component';
import { CardHub } from '../../../components/shared/ui/cards/card-hub/card-hub';
import { CardLogo } from '../../../components/shared/ui/cards/card-logo/card-logo';
import { Carousel, CarouselSlide } from '../../../components/shared/ui/carousels/carousel/carousel';

@Component({
  selector: 'app-home',
  imports: [
    ButtonComponent,
    CardHub,
    CardLogo,
    Carousel,
    CarouselSlide,
    EqualHeight,
    HubFooter,
    HubHeader,
    I18nText,
    TranslatePipe,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home {
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

  constructor() {
    const title = inject(Title);

    inject(TranslateService)
      .stream('home.page.title')
      .pipe(takeUntilDestroyed())
      .subscribe((pageTitle: string) => title.setTitle(pageTitle));
  }
}
