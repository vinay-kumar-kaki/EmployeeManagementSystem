export interface Employee {
  id: number;
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  departmentId: number;
  departmentName: string;
  designation: string;
  joiningDate: string;
  salary: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string;
  departmentId: number;
  designation: string;
  joiningDate: string;
  salary: number;
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  departmentId: number;
  designation: string;
  salary: number;
  isActive: boolean;
}

export interface EmployeeProfile {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber?: string;
  departmentName: string;
  designation: string;
  joiningDate: string;
  salary: number;
  isActive: boolean;
}
