using WorkPlanStatusKata.Domain;
using WorkPlanStatusKata.Persistence;

namespace WorkPlanStatusKata.Persistence.Tests;

public class FakeHistoryLogger : IWorkPlanHistoryLogger
{
    public bool DebeFallar { get; set; }

    public List<StatusChangeRecord> Registros { get; } = [];

    public Task RegistrarAsync(StatusChangeRecord registro)
    {
        if (DebeFallar)
        {
            throw new InvalidOperationException("Fallo simulado al escribir en Mongo");
        }

        Registros.Add(registro);
        return Task.CompletedTask;
    }
}
