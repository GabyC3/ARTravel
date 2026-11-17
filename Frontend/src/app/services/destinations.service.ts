import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateDestinationRequest,
  Destination,
  DestinationDetail,
  DestinationImage,
  PagedDestinations,
  AddDestinationImageRequest,
  UpdateDestinationRequest
} from '../models/destination.model';

@Injectable({ providedIn: 'root' })
export class DestinationsService {
  private readonly apiUrl = `${environment.apiUrl}/destinations`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    isActive: boolean | null
  ): Observable<PagedDestinations> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (isActive !== null) {
      params = params.set('isActive', isActive);
    }

    return this.http.get<PagedDestinations>(this.apiUrl, { params });
  }

  get(id: number): Observable<DestinationDetail> {
    return this.http.get<DestinationDetail>(`${this.apiUrl}/${id}`);
  }

  parents(): Observable<Destination[]> {
    return this.http.get<Destination[]>(`${this.apiUrl}/parents`);
  }

  create(request: CreateDestinationRequest): Observable<DestinationDetail> {
    return this.http.post<DestinationDetail>(this.apiUrl, request);
  }

  update(id: number, request: UpdateDestinationRequest): Observable<DestinationDetail> {
    return this.http.put<DestinationDetail>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  addImage(id: number, request: AddDestinationImageRequest): Observable<DestinationImage> {
    return this.http.post<DestinationImage>(`${this.apiUrl}/${id}/images`, request);
  }

  removeImage(id: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}/images/${imageId}`);
  }

  setPrimaryImage(id: number, imageId: number): Observable<DestinationImage> {
    return this.http.patch<DestinationImage>(`${this.apiUrl}/${id}/images/${imageId}/primary`, {});
  }
}
