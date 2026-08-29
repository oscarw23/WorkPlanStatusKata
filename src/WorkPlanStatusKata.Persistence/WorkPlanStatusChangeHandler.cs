using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

public class WorkPlanStatusChangeHandler
{
    private readonly WorkPlanStatusDerivator _derivator;
    private readonly ISqlWorkPlanRepository _sqlRepository;
    private readonly IWorkPlanHistoryLogger _historyLogger;

    public WorkPlanStatusChangeHandler(
        WorkPlanStatusDerivator derivator,
        ISqlWorkPlanRepository sqlRepository,
        IWorkPlanHistoryLogger historyLogger)
    {
        _derivator = derivator;
        _sqlRepository = sqlRepository;
        _historyLogger = historyLogger;
    }

    public async Task<DerivationResult> ProcesarAsync(WorkPlan workPlan, StatusChangeTrigger trigger, string usuario, DateTimeOffset fechaHora)
    {
        var resultado = _derivator.Derive(workPlan, trigger);

        if (!resultado.Cambio)
        {
            return resultado;
        }

        await _sqlRepository.ActualizarEstadoAsync(workPlan.Id, resultado.NuevoEstado);

        var registro = StatusChangeRecord.Crear(workPlan, resultado, usuario, fechaHora);

        try
        {
            await _historyLogger.RegistrarAsync(registro);
        }
        catch (Exception ex)
        {
            // Consistencia eventual intencional: un fallo al escribir en Mongo
            // no debe revertir ni bloquear el cambio ya confirmado en SQL.
            Console.Error.WriteLine(
                $"[WorkPlanStatusChangeHandler] Fallo al registrar histórico en Mongo para WorkPlan {workPlan.Id}: {ex}");
        }

        return resultado;
    }
}
