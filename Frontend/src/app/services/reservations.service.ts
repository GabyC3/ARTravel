import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CancelReservationRequest,
  CreateReservationPassengerRequest,
  CreateReservationRequest,
  PagedReservations,
  ReservationDetail,
  ReservationPassenger,
  ReservationPayment,
  UpdateReservationRequest
} from '../models/reservation.model';
import { PagedDepartureManifest } from '../models/departure.model';

@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly apiUrl = `${environment.apiUrl}/reservations`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    status: number | null
  ): Observable<PagedReservations> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (status !== null) {
      params = params.set('status', status);
    }
    return this.http.get<PagedReservations>(this.apiUrl, { params });
  }

  get(id: number): Observable<ReservationDetail> {
    return this.http.get<ReservationDetail>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateReservationRequest): Observable<ReservationDetail> {
    return this.http.post<ReservationDetail>(this.apiUrl, request);
  }

  update(id: number, request: UpdateReservationRequest): Observable<ReservationDetail> {
    return this.http.put<ReservationDetail>(`${this.apiUrl}/${id}`, request);
  }

  confirm(id: number): Observable<ReservationDetail> {
    return this.http.patch<ReservationDetail>(`${this.apiUrl}/${id}/confirm`, {});
  }

  cancel(id: number, request: CancelReservationRequest): Observable<ReservationDetail> {
    return this.http.patch<ReservationDetail>(`${this.apiUrl}/${id}/cancel`, request);
  }

  payments(id: number): Observable<ReservationPayment[]> {
    return this.http.get<ReservationPayment[]>(`${this.apiUrl}/${id}/payments`);
  }

  addPassenger(
    reservationId: number,
    request: CreateReservationPassengerRequest
  ): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${reservationId}/passengers`, request);
  }

  removePassenger(reservationId: number, passengerId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${reservationId}/passengers/${passengerId}`);
  }

  departureReservations(
    departureId: number,
    page = 1,
    pageSize = 100
  ): Observable<PagedReservations> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedReservations>(
      `${environment.apiUrl}/departures/${departureId}/reservations`,
      { params }
    );
  }

  departurePassengers(
    departureId: number,
    page = 1,
    pageSize = 100
  ): Observable<PagedDepartureManifest> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<PagedDepartureManifest>(
      `${environment.apiUrl}/departures/${departureId}/passengers`,
      { params }
    );
  }

  reservationPassengers(
    reservationId: number,
    page = 1,
    pageSize = 100
  ): Observable<{ items: ReservationPassenger[]; page: number; pageSize: number; totalCount: number }> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    return this.http.get<{ items: ReservationPassenger[]; page: number; pageSize: number; totalCount: number }>(
      `${this.apiUrl}/${reservationId}/passengers`,
      { params }
    );
  }
}
