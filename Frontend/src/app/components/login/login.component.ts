import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginRequest } from '../../models/auth.model';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
    standalone: true,
    selector: 'app-login',
    imports: [FormsModule, CommonModule],
    template: `
        <div class="login-container">
            <h2>Iniciar Sesión</h2>
            <div *ngIf="error" class="error">{{ error }}</div>
            <input type="email" [(ngModel)]="credentials.email" placeholder="Email" />
            <input type="password" [(ngModel)]="credentials.password" placeholder="Contraseña" />
            <button (click)="onLogin()">Ingresar</button>
        </div>`
})

export class LoginComponent {
    credentials: LoginRequest = { email: '', password: '' };
    error: string = '';
    constructor(
        private authService: AuthService,
        private router: Router
    ) {}

    onLogin(): void {
        this.authService.login(this.credentials).subscribe({
        next: () => {
            this.router.navigate(['/productos']);
        },
        error: (err) => {
            this.error = 'Credenciales inválidas';
        }
        });
    }
}