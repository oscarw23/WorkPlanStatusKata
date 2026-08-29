namespace WorkPlanStatusKata.Domain;

public class WorkPlanStatusDerivator
{
    private sealed record Rule(
        string Codigo,
        Func<WorkPlan, StatusChangeTrigger, bool> Aplica,
        Func<WorkPlan, StatusChangeTrigger, DerivationResult> Derivar);

    // El orden importa: reglas más específicas antes que las genéricas
    // (ej. CR04 -que exige TODAS las actividades finales- antes que CR03
    // -que solo exige que AL MENOS UNA haya salido de Creada-).
    private static readonly IReadOnlyList<Rule> Reglas = new[]
    {
        // CR05 va primero: la cancelación manual aplica inmediato desde
        // cualquier estado, incluso Finalizada, sin validar actividades.
        new Rule(
            "CR05",
            (wp, trigger) => trigger == StatusChangeTrigger.CancelacionManual,
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Cancelada, Cambio: true, "CR05", "Cancelación manual")),

        new Rule(
            "CR01",
            (wp, trigger) => trigger == StatusChangeTrigger.Creacion && wp.TecnicoId is null,
            (wp, trigger) => new DerivationResult(WorkPlanStatus.SinAsignar, Cambio: false, "CR01", "Orden creada sin técnico asignado")),

        new Rule(
            "CR02",
            (wp, trigger) => trigger == StatusChangeTrigger.AsignacionTecnico && wp.TecnicoId is not null,
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Asignada, Cambio: true, "CR02", "Se asignó técnico a la orden")),

        new Rule(
            "CR06",
            (wp, trigger) => trigger == StatusChangeTrigger.RechazoSoporte && wp.Estado == WorkPlanStatus.Finalizada,
            (wp, trigger) => new DerivationResult(WorkPlanStatus.EnProceso, Cambio: true, "CR06", "Se rechazó un soporte ya aprobado")),

        new Rule(
            "CR04",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Count > 0
                && wp.Actividades.All(a => a.Estado == ActivityStatus.Finalizada),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Finalizada, Cambio: true, "CR04", "Se cerró la última actividad pendiente")),

        // CR07: una Fallida cuenta como "reportada" para el mismo conteo de
        // CR04 (todas cerradas → Finalizada). Se distingue de CR04 en la
        // trazabilidad porque al menos una actividad terminó Fallida.
        new Rule(
            "CR07",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Count > 0
                && wp.Actividades.All(EsEstadoTerminalExitosoOFallido)
                && wp.Actividades.Any(a => a.Estado == ActivityStatus.Fallida),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Finalizada, Cambio: true, "CR07", "Se cerró la última actividad pendiente (incluye Fallida)")),

        // CR08: una Cancelada individual (mezclada con al menos una Finalizada
        // o Fallida) cuenta igual que CR07 para el cierre. El requisito de que
        // haya al menos una Finalizada/Fallida la separa a propósito del caso
        // CR09 (100% Canceladas), que todavía no está implementado.
        new Rule(
            "CR08",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Count > 0
                && wp.Actividades.All(EsEstadoTerminal)
                && wp.Actividades.Any(a => a.Estado == ActivityStatus.Cancelada)
                && wp.Actividades.Any(EsEstadoTerminalExitosoOFallido),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Finalizada, Cambio: true, "CR08", "Se cerró la última actividad pendiente (incluye Cancelada individual)")),

        // CR09: si el 100% de las actividades terminan en Cancelada (ninguna
        // Finalizada/Fallida) la orden se cancela automáticamente. Se
        // distingue de CR08 porque aquí NO hay ninguna Finalizada/Fallida.
        new Rule(
            "CR09",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Count > 0
                && wp.Actividades.All(a => a.Estado == ActivityStatus.Cancelada),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.Cancelada, Cambio: true, "CR09", "cancelación automática por cierre de todas las actividades")),

        // CR10: una actividad Físico/Química en un estado intermedio del flujo
        // de muestras mantiene la orden en proceso. Va antes que CR03 para que
        // la trazabilidad la atribuya a CR10 y no al guard genérico.
        new Rule(
            "CR10",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Any(a => a.EsFisicoQuimico && EsEstadoIntermedioMuestras(a.Estado)),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.EnProceso, Cambio: true, "CR10", "Actividad físico/química en flujo de muestras")),

        new Rule(
            "CR03",
            (wp, trigger) => trigger == StatusChangeTrigger.CambioActividad
                && wp.Actividades.Any(a => a.Estado != ActivityStatus.Creada),
            (wp, trigger) => new DerivationResult(WorkPlanStatus.EnProceso, Cambio: true, "CR03", "La primera actividad salió de Creada")),
    };

    public DerivationResult Derive(WorkPlan workPlan, StatusChangeTrigger trigger)
    {
        var regla = Reglas.FirstOrDefault(r => r.Aplica(workPlan, trigger));

        if (regla is null)
        {
            throw new NotImplementedException(
                $"Caso no contemplado todavía: trigger={trigger}, TecnicoId={(workPlan.TecnicoId.HasValue ? "asignado" : "sin asignar")}, actividades={workPlan.Actividades.Count}");
        }

        return regla.Derivar(workPlan, trigger);
    }

    private static bool EsEstadoTerminalExitosoOFallido(Activity actividad) =>
        actividad.Estado is ActivityStatus.Finalizada or ActivityStatus.Fallida;

    private static bool EsEstadoTerminal(Activity actividad) =>
        actividad.Estado is ActivityStatus.Finalizada or ActivityStatus.Fallida or ActivityStatus.Cancelada;

    private static bool EsEstadoIntermedioMuestras(ActivityStatus estado) =>
        estado is ActivityStatus.PendienteEnvioMuestras
            or ActivityStatus.PendienteLaboratorio
            or ActivityStatus.PendienteSoporte
            or ActivityStatus.PendienteAprobacion;
}
