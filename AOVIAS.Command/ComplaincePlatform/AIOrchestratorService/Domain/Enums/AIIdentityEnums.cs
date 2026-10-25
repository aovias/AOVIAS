namespace AIOrchestratorService.Domain.Enums;

public enum AIProfileStatus { Draft = 0, Active = 1, Archived = 2 }
public enum AIIdentityLifecycleStatus { Provisioning = 0, Active = 1, Suspended = 2, Retired = 3 }
public enum AutomationMode { ManualOnly = 0, HumanApprovalRequired = 1, AutoExecute = 2 }
public enum AIActionType { Read = 0, Suggest = 1, Generate = 2, Analyze = 3, Summarize = 4, Recommend = 5, Draft = 6, Escalate = 7, Notify = 8 }
public enum ApprovalRequirementType { NoApproval = 0, ManagerApproval = 1, CommitteeApproval = 2, QualityApproval = 3, ExecutiveApproval = 4, RiskBased = 5, ConfidenceBased = 6 }
public enum PromptLayerType { System = 0, Role = 1, Organization = 2, Department = 3, Framework = 4, Template = 5 }
public enum PromptProfileStatus { Draft = 0, Published = 1, Archived = 2 }
public enum ExecutionOutcome { Success = 0, Partial = 1, Failed = 2, Rejected = 3, PendingApproval = 4 }
public enum ApprovalStatus { NotRequired = 0, Pending = 1, Granted = 2, Denied = 3 }
