namespace GovernanceService.Domain.Enums;

public enum GovernanceAssignmentStatus { Draft, Active, Suspended, Revoked, Expired }
public enum CommitteeStatus { Active, Suspended, Dissolved }
public enum ChainStatus { Draft, Published, Archived }
public enum CommitteeMembershipStatus { Nominated, Active, TermEnded, Replaced }
public enum StepMode { Sequential, Parallel, Conditional }
public enum MeetingStatus { Scheduled, Held, Completed }
public enum CommitteeActionItemStatus { Open, InProgress, Completed, Cancelled }
