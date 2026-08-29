using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

public class WorkPlanStatusHistoryDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("workPlanId")]
    [BsonRepresentation(BsonType.String)]
    public Guid WorkPlanId { get; set; }

    [BsonElement("estadoAnterior")]
    public string EstadoAnterior { get; set; } = "";

    [BsonElement("estadoNuevo")]
    public string EstadoNuevo { get; set; } = "";

    [BsonElement("reglaAplicada")]
    public string ReglaAplicada { get; set; } = "";

    [BsonElement("usuario")]
    public string Usuario { get; set; } = "";

    [BsonElement("fecha")]
    public DateTime Fecha { get; set; }

    [BsonElement("motivo")]
    public string Motivo { get; set; } = "";

    [BsonElement("actividadesSnapshot")]
    public List<ActivitySnapshotDocument> ActividadesSnapshot { get; set; } = [];

    public static WorkPlanStatusHistoryDocument Desde(StatusChangeRecord registro) => new()
    {
        WorkPlanId = registro.WorkPlanId,
        EstadoAnterior = registro.EstadoAnterior.ToString(),
        EstadoNuevo = registro.EstadoNuevo.ToString(),
        ReglaAplicada = registro.ReglaAplicada,
        Usuario = registro.Usuario,
        Fecha = registro.FechaHora.UtcDateTime,
        Motivo = registro.Motivo,
        ActividadesSnapshot = registro.ActividadesSnapshot
            .Select(a => new ActivitySnapshotDocument
            {
                Id = a.Id,
                Estado = a.Estado.ToString(),
                EsFisicoQuimico = a.EsFisicoQuimico
            })
            .ToList()
    };
}

public class ActivitySnapshotDocument
{
    [BsonElement("id")]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("estado")]
    public string Estado { get; set; } = "";

    [BsonElement("esFisicoQuimico")]
    public bool EsFisicoQuimico { get; set; }
}
