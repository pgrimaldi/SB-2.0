import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PackageItem } from '../../../entities/settings/catalog/package-item';
import { Page, PageRequest } from '../../../entities/pagination/page';

@Injectable({ providedIn: 'root' })
export class PriceListService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/catalog`;

  readonly packageList = (request: PageRequest): Observable<Page<PackageItem>> =>
    this.httpClient.post<Page<PackageItem>>(`${this.endpoint}/packages-list`, request);
}
