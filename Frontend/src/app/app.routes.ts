import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';

export const routes: Routes = [

    // Rutas públicas (sin guard = AllowAnonymous)
    { path: 'login', component: LoginComponent },

    // Rutas protegidas (con guard = [Authorize])

    // Redirección por defecto
];
