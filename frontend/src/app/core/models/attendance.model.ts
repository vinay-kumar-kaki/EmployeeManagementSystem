export interface AttendanceStatus {
  hasCheckedInToday: boolean;
  hasCheckedOutToday: boolean;
  checkInTime?: string;
  checkOutTime?: string;
  workDurationMinutes?: number;
  status: string;
  formattedDuration: string;
}

export interface AttendanceRecord {
  id: number;
  employeeId: number;
  employeeName: string;
  departmentName: string;
  date: string;
  checkInTime: string;
  checkOutTime?: string;
  workDurationMinutes?: number;
  status: string;
  formattedDuration: string;
}
