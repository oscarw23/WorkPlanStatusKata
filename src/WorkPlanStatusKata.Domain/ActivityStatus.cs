namespace WorkPlanStatusKata.Domain;

public enum ActivityStatus
{
    Creada,
    EnProceso,
    Ejecutada,
    Finalizada,
    Fallida,
    Cancelada,
    PendienteEnvioMuestras,
    PendienteLaboratorio,
    PendienteSoporte,
    PendienteAprobacion
}
