import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ComboboxItem } from '../../../entities/combobox/combobox-item';
import { Page, PageRequest } from '../../../entities/pagination/page';
import { CreateUserRequest } from '../../../entities/settings/user-management/create-user-request';
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

  /** The users to choose from, as the backend decides them (e.g. those of the log filter). */
  readonly userCombobox = (): Observable<ComboboxItem[]> =>
    this.httpClient.post<ComboboxItem[]>(`${this.endpoint}/user-combobox`, null);

  /** For the property of the token. Answers 201 with the new user. */
  readonly createUser = (request: CreateUserRequest): Observable<UserSelectedItem> =>
    this.httpClient.post<UserSelectedItem>(
      `${this.endpoint}/create-user`,
      onlyFieldsOf(CreateUserRequest, request),
    );

  /** GET: the user travels in the query string (`?idUser=…`). Never the password. */
  readonly selectedUser = (idUser: string): Observable<UserSelectedItem> =>
    this.httpClient.get<UserSelectedItem>(`${this.endpoint}/get-selected-user`, {
      params: { idUser },
    });

  /** For the property of the token; `password` null keeps the saved one. Answers 200 with the user. */
  readonly updateUser = (request: UpdateUserRequest): Observable<UserSelectedItem> =>
    this.httpClient.post<UserSelectedItem>(
      `${this.endpoint}/update-user`,
      onlyFieldsOf(UpdateUserRequest, request),
    );

  /** For the property of the token. Answers 204 with no body. */
  readonly deleteUser = (idUser: string): Observable<void> =>
    this.httpClient.delete<void>(`${this.endpoint}/property-users/${encodeURIComponent(idUser)}`);
}
