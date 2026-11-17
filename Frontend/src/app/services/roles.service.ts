import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateRoleRequest,
  PagedRoles,
  Role
} from '../models/identity-management.model';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly apiUrl = `${environment.apiUrl}/roles`;
  private readonly usersUrl = `${environment.apiUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  list(page = 1, pageSize = 100): Observable<PagedRoles> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedRoles>(this.apiUrl, { params });
  }

  create(request: CreateRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.apiUrl, request);
  }

  assign(userId: number, roleId: number): Observable<void> {
    return this.http.post<void>(`${this.usersUrl}/${userId}/roles/${roleId}`, {});
  }

  remove(userId: number, roleId: number): Observable<void> {
    return this.http.delete<void>(`${this.usersUrl}/${userId}/roles/${roleId}`);
  }
}
