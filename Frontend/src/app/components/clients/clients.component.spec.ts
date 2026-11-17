import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Customer, CustomerType, CreateCustomerRequest } from '../../models/customer.model';
import { CustomersService } from '../../services/customers.service';
import { ClientsComponent } from './clients.component';

describe('ClientsComponent', () => {
  const customer: Customer = {
    id: 17,
    customerType: CustomerType.Individual,
    firstName: 'Juan',
    lastName: 'Pérez',
    businessName: null,
    documentType: 'DNI',
    documentNumber: '30111222',
    email: 'juan@example.com',
    phoneNumber: null,
    address: null,
    city: null,
    country: 'Argentina',
    notes: null,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null
  };

  it('loads clients and displays their principal information', async () => {
    await TestBed.configureTestingModule({
      imports: [ClientsComponent],
      providers: [{
        provide: CustomersService,
        useValue: {
          list: () => of({ items: [customer], page: 1, pageSize: 12, totalCount: 1 })
        }
      }]
    }).compileComponents();

    const fixture = TestBed.createComponent(ClientsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const row = fixture.nativeElement.querySelector('tbody tr') as HTMLElement | null;
    expect(row?.textContent).toContain('Juan Pérez');
    expect(row?.textContent).toContain('DNI 30111222');
    expect(row?.textContent).toContain('juan@example.com');
  });

  it('creates an individual client with the endpoint contract fields', async () => {
    let request: CreateCustomerRequest | null = null;
    await TestBed.configureTestingModule({
      imports: [ClientsComponent],
      providers: [{
        provide: CustomersService,
        useValue: {
          list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }),
          create: (value: CreateCustomerRequest) => {
            request = value;
            return of(customer);
          }
        }
      }]
    }).compileComponents();

    const fixture = TestBed.createComponent(ClientsComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.openCreateDialog();
    component.form.firstName = 'Juan';
    component.form.lastName = 'Pérez';
    component.form.documentNumber = '30111222';
    component.submit(true);

    expect(request).toMatchObject({
      customerType: CustomerType.Individual,
      firstName: 'Juan',
      lastName: 'Pérez',
      documentType: 'DNI',
      documentNumber: '30111222',
      isActive: true
    });
  });
});
