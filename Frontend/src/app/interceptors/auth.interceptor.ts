import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authService = inject(AuthService);
    const token = authService.getToken();

    // Si hay token, clonamos el request y le agregamos el header
    if (token) {
        const clonedReq = req.clone({
            setHeaders: {
            Authorization: `Bearer ${token}`
            }
        });
        return next(clonedReq);
    }
    // Si no hay token, dejamos pasar el request sin modificar
    return next(req);
};