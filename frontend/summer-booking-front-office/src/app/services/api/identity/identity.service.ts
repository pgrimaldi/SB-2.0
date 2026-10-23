import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
import { DeleteUserRequest } from '../../../entities/settings/user-management/delete-user-request';
import { UpdateUserRequest } from '../../../entities/settings/user-management/update-user-request';
import { UserItem } from '../../../entities/settings/user-management/user-item';
import { UserSelectedItem } from '../../../entities/settings/user-management/user-selected-item';
import { onlyFieldsOf } from '../only-fields-of';

@Injectable({ providedIn: 'root' })
export class IdentityService {
  private readonly httpClient = inject(HttpClient);
  private readonly endpoint = `${environment.apiBaseUrl}/identity`;

  readonly usersList = (request: PageRequest): Observable<Page<UserItem>> =>
    this.httpClient.post<Page<UserItem>>(`${this.endpoint}/users-list`, request);

  /** Answers 204 with no body. */
  readonly createUser = (idProperty: string, request: CreateUserRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/create-user`, {
      ...onlyFieldsOf(CreateUserRequest, request),
      idProperty,
    });

  /** GET: the user travels in the query string (`?idUser=…`). Never the password. */
  readonly selectedUser = (idUser: string): Observable<UserSelectedItem> =>
    this.httpClient.get<UserSelectedItem>(`${this.endpoint}/get-selected-user`, {
      params: { idUser },
    });

  /** `password` null keeps the saved one. Answers 204 with no body. */
  readonly updateUser = (idProperty: string, request: UpdateUserRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/update-user`, {
      ...onlyFieldsOf(UpdateUserRequest, request),
      idProperty,
    });

  /** Answers 204 with no body. */
  readonly deleteUser = (request: DeleteUserRequest): Observable<void> =>
    this.httpClient.post<void>(`${this.endpoint}/delete-user`, request);
}
