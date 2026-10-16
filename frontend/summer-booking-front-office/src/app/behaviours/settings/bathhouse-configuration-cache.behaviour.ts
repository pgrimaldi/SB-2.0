import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { ComboboxItem } from '../../entities/combobox/combobox-item';
import { BathhouseGeneralInformation } from '../../entities/settings/bathhouse-configuration/bathhouse-general-information';
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
  private countriesAnswer: Observable<ComboboxItem[]> | null = null;

  generalInformation(idProperty: string): Observable<BathhouseGeneralInformation> {
    this.generalInformationAnswer ??= this.catalog
      .bathhouseConfigurationGeneral(idProperty)
      .pipe(shareReplay(1));
    return this.generalInformationAnswer;
  }

  countries(): Observable<ComboboxItem[]> {
    this.countriesAnswer ??= this.catalog.countriesCombobox().pipe(shareReplay(1));
    return this.countriesAnswer;
  }
}
