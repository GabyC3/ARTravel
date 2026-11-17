import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CreateDestinationRequest, DestinationType } from '../../models/destination.model';
import { DestinationsService } from '../../services/destinations.service';
import { DestinationsComponent } from './destinations.component';

describe('DestinationsComponent', () => {
  it('shows an explicit empty state when no destinations are returned', async () => {
    await TestBed.configureTestingModule({
      imports: [DestinationsComponent],
      providers: [{
        provide: DestinationsService,
        useValue: {
          list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 })
        }
      }]
    }).compileComponents();

    const fixture = TestBed.createComponent(DestinationsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state h2') as HTMLElement | null;
    expect(emptyState?.textContent?.trim()).toBe('No hay destinos registrados');
  });

  it('submits the selected parent and images when creating a child destination', async () => {
    const submittedRequests: CreateDestinationRequest[] = [];
    await TestBed.configureTestingModule({
      imports: [DestinationsComponent],
      providers: [{
        provide: DestinationsService,
        useValue: {
          list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }),
          parents: () => of([]),
          create: (request: CreateDestinationRequest) => {
            submittedRequests.push(request);
            return of({ destination: {}, images: [] });
          }
        }
      }]
    }).compileComponents();

    const fixture = TestBed.createComponent(DestinationsComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.openCreateDialog();
    component.form.name = 'Villa Carlos Paz';
    component.form.destinationType = DestinationType.City;
    component.form.hierarchy = 'child';
    component.form.parentDestinationId = 42;
    component.selectedImageUrls = ['/images/cordoba.jpg', '/images/carlos_paz.webp'];
    component.primaryImageUrl = '/images/carlos_paz.webp';

    component.saveDestination(true);

    expect(submittedRequests[0]?.parentDestinationId).toBe(42);
    expect(submittedRequests[0]?.imageUrls).toEqual([
      '/images/carlos_paz.webp',
      '/images/cordoba.jpg'
    ]);
  });
});
