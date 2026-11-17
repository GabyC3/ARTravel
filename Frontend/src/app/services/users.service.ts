import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserSummary } from '../models/auth.model';
import {
  CreateUserRequest,
  PagedUsers,
  UpdateUserRequest
} from '../models/identity-management.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly apiUrl = `${environment.apiUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  list(page: number, pageSize: number, search: string): Observable<PagedUsers> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    return this.http.get<PagedUsers>(this.apiUrl, { params });
  }

  get(id: number): Observable<UserSummary> {
    return this.http.get<UserSummary>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateUserRequest): Observable<UserSummary> {
    return this.http.post<UserSummary>(this.apiUrl, request);
  }

  update(id: number, request: UpdateUserRequest): Observable<UserSummary> {
    return this.http.put<UserSummary>(`${this.apiUrl}/${id}`, request);
  }

  setStatus(id: number, isActive: boolean): Observable<UserSummary> {
    return this.http.patch<UserSummary>(`${this.apiUrl}/${id}/status`, { isActive });
  }
}
