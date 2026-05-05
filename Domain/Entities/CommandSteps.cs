namespace Domain.Entities;

// Base type for all steps
public abstract record SessionStep;

public abstract record ClaimSteps : SessionStep
{
    public record WaitingForComponent : ClaimSteps;
    public record WaitingForSupplier : ClaimSteps;
    public record WaitingForDescription : ClaimSteps;
    public record WaitingForPhoto : ClaimSteps;
    public record WaitingForConfirmation: ClaimSteps;
}

public abstract record StartSteps : SessionStep
{
    public record WaitingForAuthCode : StartSteps;
}

