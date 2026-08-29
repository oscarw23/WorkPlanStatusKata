namespace WorkPlanStatusKata.Domain;

public record DerivationResult(WorkPlanStatus NuevoEstado, bool Cambio, string ReglaAplicada, string Motivo);
