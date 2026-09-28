import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { WarehouseItem } from '../../../entities/warehouse/warehouse-item';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/warehouse`;

  getAll(): Observable<WarehouseItem[]> {
    return this.httpClient.get<WarehouseItem[]>(`${this.endpoint}/list`);
  }
}
