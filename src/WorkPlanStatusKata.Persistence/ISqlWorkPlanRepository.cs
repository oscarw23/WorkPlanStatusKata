using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

public interface ISqlWorkPlanRepository
{
    Task ActualizarEstadoAsync(Guid workPlanId, WorkPlanStatus nuevoEstado);
}
