import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DeletePackageItemsRequest } from '../../../entities/settings/catalog/delete-package-items-request';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { Page, PageRequest } from '../../../entities/pagination/page';

@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly packageList = (request: PageRequest): Observable<Page<PackageItem>> =>
    this.httpClient.post<Page<PackageItem>>(`${this.endpoint}/packages-list`, request);

  /** All or nothing: one id that cannot be deleted and none is. Answers 204 with no body. */
  readonly deletePackageItems = (request: DeletePackageItemsRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-package-item`, request);
}
