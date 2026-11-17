export interface LeaveType {
  id: number;
  name: string;
  defaultDaysPerYear: number;
  isActive: boolean;
}

export interface SaveLeaveTypeRequest {
  name: string;
  defaultDaysPerYear: number;
  isActive: boolean;
}

export interface ApplyLeaveRequest {
  leaveTypeId: number;
  startDate: string;
  endDate: string;
  reason: string;
}

export interface ReviewLeaveRequest {
  status: number; // 1 = Approved, 2 = Rejected
  adminRemarks?: string;
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  departmentName: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  reason: string;
  status: number; // 0 = Pending, 1 = Approved, 2 = Rejected
  statusName: string;
  adminRemarks?: string;
  appliedAt: string;
  reviewedAt?: string;
}
