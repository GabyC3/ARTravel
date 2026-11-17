import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { UserSummary } from '../../models/auth.model';
import { ClientsComponent } from '../clients/clients.component';
import { DestinationsComponent } from '../destinations/destinations.component';
import { PackagesComponent } from '../packages/packages.component';
import { UserManagementComponent } from '../user-management/user-management.component';
import { ReservationsComponent } from '../reservations/reservations.component';
import { ItinerariesComponent } from '../itineraries/itineraries.component';

interface DashboardSection {
  label: string;
  slug: string;
  icon: string;
}

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    RouterLink,
    ClientsComponent,
    DestinationsComponent,
    PackagesComponent,
    UserManagementComponent,
    ReservationsComponent,
    ItinerariesComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  readonly sections: DashboardSection[] = [
    { label: 'Dashboard', slug: 'dashboard', icon: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z' },
    { label: 'Clientes', slug: 'clientes', icon: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M22 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75' },
    { label: 'Destinos', slug: 'destinos', icon: 'M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0Z M12 10a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z' },
    { label: 'Paquetes', slug: 'paquetes', icon: 'm12 3 9 5-9 5-9-5 9-5Z M3 8v9l9 5 9-5V8 M12 13v9' },
    { label: 'Excursiones', slug: 'excursiones', icon: 'M3 17h18l-2-8H5l-2 8Z M7 9l2-5h6l2 5 M8 17v3 M16 17v3 M7 13h.01 M17 13h.01' },
    { label: 'Reservas', slug: 'reservas', icon: 'M6 3v3 M18 3v3 M4 8h16 M5 5h14a1 1 0 0 1 1 1v14H4V6a1 1 0 0 1 1-1Z M8 12h3v3H8z' },
    { label: 'Itinerarios', slug: 'itinerarios', icon: 'M4 5h16M4 12h16M4 19h16M8 3v4m8 3v4m-8 3v4' },
    { label: 'Pagos', slug: 'pagos', icon: 'M3 6h18v13H3z M3 10h18 M7 15h4' },
    { label: 'Guías y transporte', slug: 'guias-transporte', icon: 'M4 16V6h12l4 5v5h-2 M4 16h2 M10 16h4 M8 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4 M18 19a2 2 0 1 0 0-4 2 2 0 0 0 0 4' },
    { label: 'Gestión de usuarios', slug: 'usuarios', icon: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M9 11a4 4 0 1 0 0-8 4 4 0 0 0 0 8 M22 21v-2a4 4 0 0 0-3-3.87 M16 3.13a4 4 0 0 1 0 7.75' },
    { label: 'Configuración', slug: 'configuracion', icon: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z M19.4 15a1.7 1.7 0 0 0 .34 1.88l.06.06-1.7 2.94-.08-.02a1.7 1.7 0 0 0-1.78.63l-.05.07h-3.4l-.05-.07a1.7 1.7 0 0 0-1.78-.63l-.08.02-1.7-2.94.06-.06A1.7 1.7 0 0 0 9.6 15l-.09-.03v-3.4l.09-.03a1.7 1.7 0 0 0-.34-1.88L9.2 9.6l1.7-2.94.08.02a1.7 1.7 0 0 0 1.78-.63l.05-.07h3.4l.05.07a1.7 1.7 0 0 0 1.78.63l.08-.02 1.7 2.94-.06.06a1.7 1.7 0 0 0-.34 1.88l.09.03v3.4l-.09.03Z' }
  ];

  readonly products = [
    { name: 'Salinas Grandes', share: '34%', style: 'salt-flats' },
    { name: 'Quebrada de Humahuaca', share: '28%', style: 'mountain-valley' },
    { name: 'Cataratas del Iguazú', share: '24%', style: 'waterfall' }
  ];

  readonly user = signal<UserSummary | null>(null);
  readonly visibleSections = computed(() => {
    const user = this.user();
    const roles = (user?.roles ?? []).map(role =>
      role.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toUpperCase());
    const has = (role: string) => roles.includes(role);
    const isAdmin = has('ADMIN');
    const isManager = has('MANAGER');
    const isAgent = has('AGENTE') || has('AGENT');
    const isOperator = has('OPERADOR') || has('OPERATOR');
    const isGuide = has('GUIA') || has('GUIDE');

    if (roles.length === 0) {
      return this.sections.filter(section => section.slug === 'dashboard');
    }

    return this.sections.filter(section => {
      if (section.slug === 'dashboard') return true;
      if (section.slug === 'usuarios') return isAdmin || isManager;
      if (section.slug === 'roles') return false;
      if (section.slug === 'clientes') return isAdmin || isManager || isAgent || isOperator;
      if (section.slug === 'pagos') return isAdmin || isManager || isAgent;
      if (section.slug === 'guias-transporte') return isAdmin || isManager || isOperator || isGuide;
      if (section.slug === 'configuracion') return isAdmin || isManager;
      if (['destinos', 'paquetes', 'excursiones', 'itinerarios', 'salidas', 'reservas'].includes(section.slug)) {
        return isAdmin || isManager || isAgent || isOperator || isGuide;
      }
      return isAdmin;
    });
  });
  selectedSection = 'Dashboard';
  isProfileView = false;
  isUserMenuOpen = false;
  readonly userLoadError = signal('');
  readonly fullName = computed(() => {
    const user = this.user();
    return user ? `${user.firstName} ${user.lastName}` : 'Cargando usuario…';
  });
  readonly roleName = computed(() => {
    const role = this.user()?.roles[0];
    if (!role) {
      return 'Sin rol asignado';
    }

    const knownRoles: Record<string, string> = {
      ADMIN: 'Administrador',
      USER: 'Usuario'
    };
    return knownRoles[role.toUpperCase()] ??
      role.toLowerCase().replace(/\b\w/g, character => character.toUpperCase());
  });
  readonly initials = computed(() => {
    const user = this.user();
    return user
      ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
      : 'AR';
  });

  constructor(
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const sectionSlug = params.get('section') ?? 'dashboard';
      this.isProfileView = sectionSlug === 'perfil';
      this.selectedSection = this.isProfileView
        ? 'Perfil'
        : sectionSlug === 'reportes'
          ? 'Gestión de usuarios'
          : this.sections.find(section => section.slug === sectionSlug)?.label ?? 'Dashboard';

    });

    this.authService.getCurrentUser().subscribe({
      next: user => {
        this.user.set(user);
        const sectionSlug = this.route.snapshot.paramMap.get('section') ?? 'dashboard';
        if (!this.isProfileView && sectionSlug !== 'dashboard' &&
            !this.visibleSections().some(section => section.slug === sectionSlug)) {
          this.router.navigate(['/dashboard.html', 'dashboard']);
        }
      },
      error: () => this.userLoadError.set('No fue posible cargar los datos de usuario.')
    });
  }

  selectSection(section: DashboardSection): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/dashboard.html', section.slug]);
  }

  openProfile(): void {
    this.isUserMenuOpen = false;
    this.router.navigate(['/dashboard.html', 'perfil']);
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen = !this.isUserMenuOpen;
  }

  logout(): void {
    this.isUserMenuOpen = false;
    this.authService.logoutCurrentSession().subscribe({
      next: () => this.router.navigate(['/login']),
      error: error => {
        console.error('No se pudo cerrar la sesión en el servidor.', error);
        this.router.navigate(['/login']);
      }
    });
  }

  get selectedIcon(): string {
    return this.sections.find(section => section.label === this.selectedSection)?.icon ?? this.sections[0].icon;
  }
}
