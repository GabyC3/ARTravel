import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Departure,
  DepartureAvailability,
  DeparturePrice,
  DepartureStatus,
  PagedDepartures
} from '../models/departure.model';

@Injectable({ providedIn: 'root' })
export class DeparturesService {
  private readonly apiUrl = `${environment.apiUrl}/departures`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    status: DepartureStatus | null = null,
    productId: number | null = null
  ): Observable<PagedDepartures> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (status !== null) {
      params = params.set('status', status);
    }
    if (productId !== null) {
      params = params.set('productId', productId);
    }
    return this.http.get<PagedDepartures>(this.apiUrl, { params });
  }

  get(id: number): Observable<Departure> {
    return this.http.get<Departure>(`${this.apiUrl}/${id}`);
  }

  availability(id: number): Observable<DepartureAvailability> {
    return this.http.get<DepartureAvailability>(`${this.apiUrl}/${id}/availability`);
  }

  prices(id: number): Observable<DeparturePrice[]> {
    return this.http.get<DeparturePrice[]>(`${this.apiUrl}/${id}/prices`);
  }
}
