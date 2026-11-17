import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateCustomerRequest,
  Customer,
  PagedCustomers,
  UpdateCustomerRequest
} from '../models/customer.model';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly apiUrl = `${environment.apiUrl}/customers`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    isActive: boolean | null
  ): Observable<PagedCustomers> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (isActive !== null) {
      params = params.set('isActive', isActive);
    }
    return this.http.get<PagedCustomers>(this.apiUrl, { params });
  }

  get(id: number): Observable<Customer> {
    return this.http.get<Customer>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateCustomerRequest): Observable<Customer> {
    return this.http.post<Customer>(this.apiUrl, request);
  }

  update(id: number, request: UpdateCustomerRequest): Observable<Customer> {
    return this.http.put<Customer>(`${this.apiUrl}/${id}`, request);
  }

  setStatus(id: number, isActive: boolean): Observable<Customer> {
    return this.http.patch<Customer>(`${this.apiUrl}/${id}/status`, { isActive });
  }
}
