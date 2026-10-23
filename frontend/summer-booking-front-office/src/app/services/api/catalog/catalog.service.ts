import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { BathhouseGeneralInformation } from '../../../entities/settings/bathhouse-configuration/bathhouse-general-information';
import { BathhouseMedia } from '../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseServiceItem } from '../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  /** Names in the language of the request (Accept-Language); `id` is the ISO 3166-1 alpha-2 code. */
  readonly countriesCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.get<ComboboxItem[]>(`${this.endpoint}/countries-combobox`);

  readonly bathhouseConfigurationGeneral = (
    idProperty: string,
  ): Observable<BathhouseGeneralInformation> =>
    this.httpClient.get<BathhouseGeneralInformation>(
      `${this.endpoint}/bathhouse-configuration-general`,
      { params: { idProperty } },
    );

  /**
   * Every service of the catalogue, with the name in the language of the request (Accept-Language)
   * and whether the property offers it.
   */
  readonly bathhouseConfigurationServices = (
    idProperty: string,
  ): Observable<BathhouseServiceItem[]> =>
    this.httpClient.get<BathhouseServiceItem[]>(
      `${this.endpoint}/bathhouse-configuration-services`,
      { params: { idProperty } },
    );

  readonly bathhouseConfigurationMedia = (idProperty: string): Observable<BathhouseMedia> =>
    this.httpClient.get<BathhouseMedia>(`${this.endpoint}/bathhouse-configuration-media`, {
      params: { idProperty },
    });

  /** Every field of the tab, changed or not. Answers 204 with no body. */
  readonly saveBathhouseConfigurationGeneral = (
    idProperty: string,
    information: BathhouseGeneralInformation,
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-bathhouse-configuration-general`, {
      ...onlyFieldsOf(BathhouseGeneralInformation, information),
      idProperty,
    });

  /** Only the services the property offers. Answers 204 with no body. */
  readonly saveBathhouseConfigurationServices = (
    idProperty: string,
    services: readonly BathhouseServiceItem[],
  ): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/save-bathhouse-configuration-services`, {
      services: services.map((service) => onlyFieldsOf(BathhouseServiceItem, service)),
      idProperty,
    });
}
