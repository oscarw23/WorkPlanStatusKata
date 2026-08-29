using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

public interface IWorkPlanHistoryLogger
{
    Task RegistrarAsync(StatusChangeRecord registro);
}
