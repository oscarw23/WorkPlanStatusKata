namespace WorkPlanStatusKata.Domain;

public record Activity(Guid Id, ActivityStatus Estado, bool EsFisicoQuimico);
