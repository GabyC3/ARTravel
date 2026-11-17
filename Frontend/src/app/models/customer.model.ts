export enum CustomerType {
  Individual = 1,
  Organization = 2
}

export interface Customer {
  id: number;
  customerType: CustomerType;
  firstName: string | null;
  lastName: string | null;
  businessName: string | null;
  documentType: string;
  documentNumber: string;
  email: string | null;
  phoneNumber: string | null;
  address: string | null;
  city: string | null;
  country: string | null;
  notes: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CustomerFields {
  customerType: CustomerType;
  firstName: string | null;
  lastName: string | null;
  businessName: string | null;
  documentType: string;
  documentNumber: string;
  email: string | null;
  phoneNumber: string | null;
  address: string | null;
  city: string | null;
  country: string | null;
  notes: string | null;
}

export interface CreateCustomerRequest extends CustomerFields {
  isActive: boolean;
}

export type UpdateCustomerRequest = CustomerFields;

export interface PagedCustomers {
  items: Customer[];
  page: number;
  pageSize: number;
  totalCount: number;
}
