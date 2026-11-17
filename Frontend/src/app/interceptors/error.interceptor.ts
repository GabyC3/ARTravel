import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const authService = inject(AuthService);
    const isPublicAuthRequest = /\/auth\/(login|refresh-token|register)(?:\?|$)/.test(req.url);
    
    return next(req).pipe(
        catchError(error => {
            if (error.status === 401 && !isPublicAuthRequest) {
                authService.logout();
                router.navigate(['/login']);
            }
        
            if (error.status === 403) {
                router.navigate(['/sin-permiso']);
            }

            if (error.status === 500) {
                console.error('Error interno del servidor', error);
            }

            return throwError(() => error);
        })
    );
};