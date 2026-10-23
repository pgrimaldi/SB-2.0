import { Injectable, inject } from '@angular/core';
import { Observable, of, shareReplay } from 'rxjs';
import { ComboboxItem } from '../../entities/combobox/combobox-item';
import { BathhouseGeneralInformation } from '../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseServiceItem } from '../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { CatalogService } from '../../services/api/catalog/catalog.service';

/**
 * Provided by the page Informazioni stabilimento: its tabs share one instance, which goes with the
 * page. Each API is called the first time a tab asks for it; going back to a tab reuses the answer.
 * A failed call is not kept: the next request tries again.
 */
@Injectable()
export class BathhouseConfigurationCacheBehaviour {
  private readonly catalog = inject(CatalogService);

  private generalInformationAnswer: Observable<BathhouseGeneralInformation> | null = null;
  private servicesAnswer: Observable<BathhouseServiceItem[]> | null = null;
  private mediaAnswer: Observable<BathhouseMedia> | null = null;
  private countriesAnswer: Observable<ComboboxItem[]> | null = null;

  generalInformation(idProperty: string): Observable<BathhouseGeneralInformation> {
    this.generalInformationAnswer ??= this.catalog
      .bathhouseConfigurationGeneral(idProperty)
      .pipe(shareReplay(1));
    return this.generalInformationAnswer;
  }

  /** After a save: going back to the tab shows what was saved, without asking again. */
  keepGeneralInformation(information: BathhouseGeneralInformation): void {
    this.generalInformationAnswer = of(information);
  }

  services(idProperty: string): Observable<BathhouseServiceItem[]> {
    this.servicesAnswer ??= this.catalog
      .bathhouseConfigurationServices(idProperty)
      .pipe(shareReplay(1));
    return this.servicesAnswer;
  }

  keepServices(services: BathhouseServiceItem[]): void {
    this.servicesAnswer = of(services);
  }

  media(idProperty: string): Observable<BathhouseMedia> {
    this.mediaAnswer ??= this.catalog.bathhouseConfigurationMedia(idProperty).pipe(shareReplay(1));
    return this.mediaAnswer;
  }

  countries(): Observable<ComboboxItem[]> {
    this.countriesAnswer ??= this.catalog.countriesCombobox().pipe(shareReplay(1));
    return this.countriesAnswer;
  }
}
