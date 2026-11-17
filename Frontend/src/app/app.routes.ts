import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'dashboard.html', pathMatch: 'full', redirectTo: 'dashboard.html/dashboard' },
    {
        path: 'dashboard.html/:section',
        component: DashboardComponent,
        canActivate: [authGuard],
        data: {
            allowedRoles: ['Admin', 'Manager', 'Agente', 'Agent', 'Operador', 'Operator', 'Guia', 'Guide', 'User']
        }
    },
    { path: '', pathMatch: 'full', redirectTo: 'login' },
    { path: '**', redirectTo: 'dashboard.html/dashboard' }
];
