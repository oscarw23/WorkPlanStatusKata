namespace WorkPlanStatusKata.Domain;

public record StatusChangeRecord(
    Guid WorkPlanId,
    WorkPlanStatus EstadoAnterior,
    WorkPlanStatus EstadoNuevo,
    string ReglaAplicada,
    string Usuario,
    DateTimeOffset FechaHora,
    string Motivo,
    IReadOnlyList<Activity> ActividadesSnapshot)
{
    public static StatusChangeRecord Crear(WorkPlan workPlan, DerivationResult resultado, string usuario, DateTimeOffset fechaHora)
    {
        return new StatusChangeRecord(
            workPlan.Id,
            EstadoAnterior: workPlan.Estado,
            EstadoNuevo: resultado.NuevoEstado,
            resultado.ReglaAplicada,
            usuario,
            fechaHora,
            Motivo: resultado.Motivo,
            ActividadesSnapshot: workPlan.Actividades);
    }
}
