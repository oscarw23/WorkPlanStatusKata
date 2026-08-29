namespace WorkPlanStatusKata.Domain;

public record WorkPlan(Guid Id, WorkPlanStatus Estado, Guid? TecnicoId, IReadOnlyList<Activity> Actividades);
