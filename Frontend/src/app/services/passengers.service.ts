import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  PagedPassengerTrips,
  PagedPassengers,
  Passenger
} from '../models/passenger.model';

@Injectable({ providedIn: 'root' })
export class PassengersService {
  private readonly apiUrl = `${environment.apiUrl}/passengers`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    isActive: boolean | null = true
  ): Observable<PagedPassengers> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (isActive !== null) {
      params = params.set('isActive', isActive);
    }
    return this.http.get<PagedPassengers>(this.apiUrl, { params });
  }

  get(id: number): Observable<Passenger> {
    return this.http.get<Passenger>(`${this.apiUrl}/${id}`);
  }

  trips(id: number, page = 1, pageSize = 20): Observable<PagedPassengerTrips> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedPassengerTrips>(`${this.apiUrl}/${id}/trips`, { params });
  }
}
