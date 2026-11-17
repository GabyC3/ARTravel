import { UserSummary } from './auth.model';

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber: string | null;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
}

export interface PagedUsers {
  items: UserSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Role {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface PagedRoles {
  items: Role[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateRoleRequest {
  name: string;
  description: string | null;
}
