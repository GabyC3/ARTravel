import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, switchMap, tap, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse, RegisterRequest, UserSummary } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly apiUrl = `${environment.apiUrl}/auth`;
    private readonly TOKEN_KEY = 'jwt_token';
    private readonly REFRESH_TOKEN_KEY = 'refresh_token';
    private currentUser: UserSummary | null = null;

    constructor(private readonly http: HttpClient) {}

    login(credentials: LoginRequest): Observable<LoginResponse> {
        return this.loginRequest(credentials).pipe(
            catchError(error => {
                if (!this.isExpiredAccessTokenError(error) || !this.getRefreshToken()) {
                    return throwError(() => error);
                }

                return this.refreshAccessToken().pipe(
                    switchMap(() => this.loginRequest(credentials)),
                    catchError(refreshError => {
                        this.logout();
                        return throwError(() => refreshError);
                    })
                );
            })
        );
    }

    register(request: RegisterRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.apiUrl}/register`, request).pipe(
            tap(() => {
                this.logout();
                this.currentUser = null;
            })
        );
    }

    getCurrentUser(): Observable<UserSummary> {
        return this.http.get<UserSummary>(`${this.apiUrl}/me`).pipe(
            tap(user => this.currentUser = user)
        );
    }

    getCachedUser(): UserSummary | null {
        return this.currentUser;
    }

    normalizeRoles(roles: string[] | null | undefined): string[] {
        return (roles ?? []).map(role => role
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .trim()
            .toUpperCase());
    }

    hasRole(role: string): boolean {
        const roles = this.normalizeRoles(this.currentUser?.roles);
        return roles.includes(role.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase());
    }

    hasAnyRole(roles: string[]): boolean {
        const normalizedRoles = this.normalizeRoles(this.currentUser?.roles);
        return roles.some(role => normalizedRoles.includes(
            role.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase()
        ));
    }

    logoutCurrentSession(): Observable<void> {
        return this.http.post<void>(`${this.apiUrl}/logout`, {}).pipe(
            finalize(() => this.logout())
        );
    }

    logout(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    getRefreshToken(): string | null {
        return localStorage.getItem(this.REFRESH_TOKEN_KEY);
    }

    isAuthenticated(): boolean {
        return Boolean(this.getToken());
    }

    private loginRequest(credentials: LoginRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
            tap(response => {
                this.saveSession(response);
                this.currentUser = response.user;
            })
        );
    }

    private refreshAccessToken(): Observable<LoginResponse> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            return throwError(() => new Error('No hay un refresh token disponible.'));
        }

        return this.http.post<LoginResponse>(`${this.apiUrl}/refresh-token`, { refreshToken }).pipe(
            tap(response => {
                this.saveSession(response);
                this.currentUser = response.user;
            })
        );
    }

    private saveSession(response: LoginResponse): void {
        localStorage.setItem(this.TOKEN_KEY, response.accessToken);
        localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    }

    private isExpiredAccessTokenError(error: unknown): boolean {
        if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
            return false;
        }

        const message = [
            JSON.stringify(error.error ?? ''),
            error.headers.get('WWW-Authenticate') ?? ''
        ].join(' ');
        return /(?:token|access token).{0,50}(?:expired|vencid|caducad)|(?:expired|vencid|caducad).{0,50}(?:token|access token)/i
            .test(message);
    }
}