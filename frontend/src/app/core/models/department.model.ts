export interface Department {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  isActive: boolean;
  employeeCount: number;
}

export interface CreateDepartmentRequest {
  name: string;
  description?: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  description?: string;
  isActive: boolean;
}
