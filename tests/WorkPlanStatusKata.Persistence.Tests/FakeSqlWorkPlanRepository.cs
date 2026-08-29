using WorkPlanStatusKata.Domain;
using WorkPlanStatusKata.Persistence;

namespace WorkPlanStatusKata.Persistence.Tests;

public class FakeSqlWorkPlanRepository : ISqlWorkPlanRepository
{
    public List<(Guid WorkPlanId, WorkPlanStatus NuevoEstado)> Actualizaciones { get; } = [];

    public Task ActualizarEstadoAsync(Guid workPlanId, WorkPlanStatus nuevoEstado)
    {
        Actualizaciones.Add((workPlanId, nuevoEstado));
        return Task.CompletedTask;
    }
}
