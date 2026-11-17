import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AddItineraryActivityRequest,
  CreateItineraryRequest,
  Itinerary,
  ItineraryActivity,
  ItineraryActivityRequest,
  ItineraryDay,
  ItineraryDayRequest,
  ItineraryDetail,
  PagedItineraries,
  UpdateItineraryRequest
} from '../models/itinerary.model';

@Injectable({ providedIn: 'root' })
export class ItinerariesService {
  private readonly apiUrl = `${environment.apiUrl}/itineraries`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    productId: number | null,
    isActive: boolean | null
  ): Observable<PagedItineraries> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (productId !== null) {
      params = params.set('productId', productId);
    }
    if (isActive !== null) {
      params = params.set('isActive', isActive);
    }
    return this.http.get<PagedItineraries>(this.apiUrl, { params });
  }

  get(id: number): Observable<ItineraryDetail> {
    return this.http.get<ItineraryDetail>(`${this.apiUrl}/${id}`);
  }

  getDays(id: number): Observable<ItineraryDay[]> {
    return this.http.get<ItineraryDay[]>(`${this.apiUrl}/${id}/days`);
  }

  create(request: CreateItineraryRequest): Observable<ItineraryDetail> {
    return this.http.post<ItineraryDetail>(this.apiUrl, request);
  }

  update(id: number, request: UpdateItineraryRequest): Observable<ItineraryDetail> {
    return this.http.put<ItineraryDetail>(`${this.apiUrl}/${id}`, request);
  }

  setStatus(id: number, isActive: boolean): Observable<Itinerary> {
    return this.http.patch<Itinerary>(`${this.apiUrl}/${id}/status`, { isActive });
  }

  duplicate(id: number): Observable<ItineraryDetail> {
    return this.http.post<ItineraryDetail>(`${this.apiUrl}/${id}/duplicate`, {});
  }

  addDay(id: number, request: ItineraryDayRequest): Observable<ItineraryDay> {
    return this.http.post<ItineraryDay>(`${this.apiUrl}/${id}/days`, request);
  }

  updateDay(id: number, dayId: number, request: ItineraryDayRequest): Observable<ItineraryDay> {
    return this.http.put<ItineraryDay>(`${this.apiUrl}/${id}/days/${dayId}`, request);
  }

  removeDay(id: number, dayId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}/days/${dayId}`);
  }

  reorderDays(id: number, dayIds: number[]): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/days/reorder`, { dayIds });
  }

  activities(dayId: number): Observable<ItineraryActivity[]> {
    return this.http.get<ItineraryActivity[]>(`${environment.apiUrl}/itinerary-days/${dayId}/activities`);
  }

  addActivity(dayId: number, request: AddItineraryActivityRequest): Observable<ItineraryActivity> {
    return this.http.post<ItineraryActivity>(
      `${environment.apiUrl}/itinerary-days/${dayId}/activities`,
      request
    );
  }

  updateActivity(
    dayId: number,
    activityId: number,
    request: ItineraryActivityRequest
  ): Observable<ItineraryActivity> {
    return this.http.put<ItineraryActivity>(
      `${environment.apiUrl}/itinerary-days/${dayId}/activities/${activityId}`,
      request
    );
  }

  removeActivity(dayId: number, activityId: number): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/itinerary-days/${dayId}/activities/${activityId}`
    );
  }

  reorderActivities(dayId: number, activityIds: number[]): Observable<void> {
    return this.http.patch<void>(
      `${environment.apiUrl}/itinerary-days/${dayId}/activities/reorder`,
      { activityIds }
    );
  }
}
