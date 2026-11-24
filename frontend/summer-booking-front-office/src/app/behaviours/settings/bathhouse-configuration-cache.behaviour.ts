import { Injectable, inject } from '@angular/core';
import { Observable, of, shareReplay } from 'rxjs';
import { DATA_RELOAD } from '../../components/shared/ui/data/data-reload';
import { BathhouseSelectedGeneralInformation } from '../../entities/settings/bathhouse-configuration/bathhouse-selected-general-information';
import { BathhouseSelectedMedia } from '../../entities/settings/bathhouse-configuration/bathhouse-selected-media';
import { BathhouseSelectedServices } from '../../entities/settings/bathhouse-configuration/bathhouse-selected-services';
import { Country } from '../../entities/shared/country';
import { CatalogService } from '../../services/api/catalog/catalog.service';

/**
 * Provided by the page Informazioni stabilimento: its tabs share one instance, which goes with the
 * page. Each API is called the first time a tab asks for it; going back to a tab reuses the answer.
 * A failed call is not kept: the next request tries again. The answers belong to the data of one
 * `DATA_RELOAD` value (the language): with another one they are asked again.
 */
@Injectable()
export class BathhouseConfigurationCacheBehaviour {
  private readonly catalog = inject(CatalogService);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private answersReload: unknown;

  private generalInformationAnswer: Observable<BathhouseSelectedGeneralInformation> | null = null;
  private servicesAnswer: Observable<BathhouseSelectedServices> | null = null;
  private mediaAnswer: Observable<BathhouseSelectedMedia> | null = null;
  private countriesAnswer: Observable<Country[]> | null = null;

  generalInformation(): Observable<BathhouseSelectedGeneralInformation> {
    this.dropOldAnswers();
    this.generalInformationAnswer ??= this.catalog
      .bathhouseConfigurationGeneral()
      .pipe(shareReplay(1));
    return this.generalInformationAnswer;
  }

  /**
   * After a save: going back to the tab shows what was saved, with the new version, without asking
   * again.
   */
  keepGeneralInformation(information: BathhouseSelectedGeneralInformation): void {
    this.dropOldAnswers();
    this.generalInformationAnswer = of(information);
  }

  forgetGeneralInformation(): void {
    this.generalInformationAnswer = null;
  }

  services(): Observable<BathhouseSelectedServices> {
    this.dropOldAnswers();
    this.servicesAnswer ??= this.catalog.bathhouseConfigurationServices().pipe(shareReplay(1));
    return this.servicesAnswer;
  }

  keepServices(services: BathhouseSelectedServices): void {
    this.dropOldAnswers();
    this.servicesAnswer = of(services);
  }

  forgetServices(): void {
    this.servicesAnswer = null;
  }

  keepMedia(media: BathhouseSelectedMedia): void {
    this.dropOldAnswers();
    this.mediaAnswer = of(media);
  }

  forgetMedia(): void {
    this.mediaAnswer = null;
  }

  media(): Observable<BathhouseSelectedMedia> {
    this.dropOldAnswers();
    this.mediaAnswer ??= this.catalog.bathhouseConfigurationMedia().pipe(shareReplay(1));
    return this.mediaAnswer;
  }

  countries(): Observable<Country[]> {
    this.dropOldAnswers();
    this.countriesAnswer ??= this.catalog.countries().pipe(shareReplay(1));
    return this.countriesAnswer;
  }

  private dropOldAnswers(): void {
    const reload = this.dataReload?.();
    if (reload === this.answersReload) {
      return;
    }
    this.answersReload = reload;
    this.generalInformationAnswer = null;
    this.servicesAnswer = null;
    this.mediaAnswer = null;
    this.countriesAnswer = null;
  }
}
