using System.Collections.Concurrent;
using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

// Simula la "tabla SQL" como fuente de verdad transaccional. Una escritura
// de diccionario ya es atómica para el alcance de este kata; no hay
// transacción real que demostrar sin una base de datos SQL real.
public class InMemorySqlWorkPlanRepository : ISqlWorkPlanRepository
{
    private readonly ConcurrentDictionary<Guid, WorkPlanStatus> _estados = new();

    public Task ActualizarEstadoAsync(Guid workPlanId, WorkPlanStatus nuevoEstado)
    {
        _estados[workPlanId] = nuevoEstado;
        return Task.CompletedTask;
    }

    public WorkPlanStatus? ObtenerEstado(Guid workPlanId) =>
        _estados.TryGetValue(workPlanId, out var estado) ? estado : null;
}
