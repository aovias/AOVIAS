export interface DepartmentComplianceDto {
  department: string;
  complianceScore: number;
  totalTasks: number;
  completedTasks: number;
}

export interface ChapterComplianceDto {
  chapterId: string;
  chapterCode: string;
  chapterName: string;
  complianceScore: number;
  totalTasks: number;
  completedTasks: number;
}

export interface HospitalDashboardDto {
  hospitalId: string;
  complianceScore: number;
  riskIndex: number;
  departmentBreakdown: DepartmentComplianceDto[];
  chapterBreakdown?: ChapterComplianceDto[];
}
