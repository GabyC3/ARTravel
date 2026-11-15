import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);
    const authService = inject(AuthService);
    
    return next(req).pipe(
        catchError(error => {
            if (error.status === 401) {
                // Token inválido o expirado -> logout y redirigir
                authService.logout();
                router.navigate(['/login']);
            }
        
            if (error.status === 403) {
                // No tiene permisos
                router.navigate(['/sin-permiso']);
            }

            if (error.status === 500) {
                console.error('Error interno del servidor', error);
            }

            return throwError(() => error);
        })
    );
};