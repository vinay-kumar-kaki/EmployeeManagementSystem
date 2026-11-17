export interface AdminDashboardStats {
  totalEmployees: number;
  activeDepartments: number;
  pendingLeaves: number;
  presentTodayCount: number;
  todayAttendanceRate: string;
}

export interface EmployeeDashboardStats {
  totalLeavesApplied: number;
  approvedLeaves: number;
  pendingLeaves: number;
  rejectedLeaves: number;
  hasCheckedInToday: boolean;
  formattedTodayHours: string;
}
