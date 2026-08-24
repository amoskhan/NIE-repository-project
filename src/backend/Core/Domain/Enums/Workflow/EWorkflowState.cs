namespace Domain.Enums;

/// <summary>
/// Standard workflow states used across the platform.
/// Add new states here as needed for your domain.
/// </summary>
public enum EWorkflowState
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Completed = 5,
    Cancelled = 6,
    ReturnedForRevision = 7
}
