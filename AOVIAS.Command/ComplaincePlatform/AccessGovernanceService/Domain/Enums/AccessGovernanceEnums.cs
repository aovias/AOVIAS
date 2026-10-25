namespace AccessGovernanceService.Domain.Enums;

public enum PolicyEffect { Permit, Deny }
public enum PolicyStatus { Draft, Published, Archived }
public enum DelegationStatus { PendingApproval, Active, Rejected, Revoked, Expired }
public enum ScopeType { Platform, Organization, Hospital, Department }
