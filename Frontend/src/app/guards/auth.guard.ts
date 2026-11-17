import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { firstValueFrom } from 'rxjs';

export const authGuard: CanActivateFn = async (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
        return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    const getCachedUser = (authService as unknown as { getCachedUser?: () => { roles: string[] } | null }).getCachedUser;
    const getCurrentUser = (authService as unknown as {
        getCurrentUser?: () => import('rxjs').Observable<{ roles: string[] }>
    }).getCurrentUser;
    let user = getCachedUser ? getCachedUser.call(authService) : null;
    if (!user && getCurrentUser) {
        try {
            user = await firstValueFrom(getCurrentUser.call(authService));
        } catch {
            if (typeof authService.logout === 'function') {
                authService.logout();
            }
            return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
        }
    }

    const allowedRoles = route.data?.['allowedRoles'] as string[] | undefined;
    const section = route.paramMap?.get?.('section') ?? 'dashboard';
    const sectionRoles: Record<string, string[]> = {
        dashboard: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide', 'User'],
        perfil: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide', 'User'],
        usuarios: ['Admin', 'Manager'],
        reportes: ['Admin', 'Manager'],
        roles: ['Admin'],
        clientes: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator'],
        destinos: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        paquetes: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        excursiones: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        itinerarios: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        salidas: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        reservas: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide'],
        pagos: ['Admin', 'Manager', 'Agente', 'Agent'],
        'guias-transporte': ['Admin', 'Manager', 'Operador', 'Operator', 'Guia', 'Guide'],
        configuracion: ['Admin', 'Manager']
    };
    const effectiveAllowedRoles = sectionRoles[section] ?? allowedRoles ?? [];
    if (effectiveAllowedRoles.length === 0) {
        return true;
    }

    if (!user) {
        return getCurrentUser ? router.createUrlTree(['/dashboard.html', 'dashboard']) : true;
    }

    const normalizedAllowed = effectiveAllowedRoles.map(role =>
        role.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase());
    const normalizedUserRoles = user.roles.map(role =>
        role.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toUpperCase());
    if (normalizedUserRoles.some(role => normalizedAllowed.includes(role))) {
        return true;
    }

    return router.createUrlTree(['/dashboard.html', 'dashboard']);
};