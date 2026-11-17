export interface Passenger {
  id: number;
  firstName: string;
  lastName: string;
  documentType: string;
  documentNumber: string;
  birthDate: string | null;
  nationality: string | null;
  email: string | null;
  phoneNumber: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  passportExpirationDate: string | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PagedPassengers {
  items: Passenger[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PagedPassengerTrips {
  items: {
    id: number;
    reservationId: number;
    passengerId: number | null;
    departureId: number | null;
    reservationPassengerId: number | null;
    origin: string;
    destination: string;
    departureAt: string;
    arrivalAt: string | null;
    status: string;
  }[];
  page: number;
  pageSize: number;
  totalCount: number;
}
