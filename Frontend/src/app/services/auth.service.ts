import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth.model';

@Injectable({ providedIn: 'root' })

export class AuthService {

    private apiUrl = environment.apiUrl + '/auth';
    private readonly TOKEN_KEY = 'jwt_token';

    constructor(private http: HttpClient) {}

    // 1. LOGIN: envía credenciales y guarda el token
    login(credentials: LoginRequest): Observable<LoginResponse> {
        return this.http.post<LoginResponse>(
          `${this.apiUrl}/login`, credentials).pipe(
           tap(response => {
              localStorage.setItem(this.TOKEN_KEY, response.token);
           })
           );
    }

    // 2. LOGOUT: elimina el token
    logout(): void { localStorage.removeItem(this.TOKEN_KEY); }

    // 3. OBTENER TOKEN: lo usa el interceptor
    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    // 4. VERIFICAR AUTENTICACIÓN: lo usa el guard
    isAuthenticated(): boolean {
        const token = this.getToken();
        return token !== null;
    }

}