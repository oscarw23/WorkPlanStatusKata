using MongoDB.Driver;
using WorkPlanStatusKata.Domain;

namespace WorkPlanStatusKata.Persistence;

public class MongoWorkPlanHistoryLogger : IWorkPlanHistoryLogger
{
    public const string NombreColeccion = "workplan_status_history";

    private readonly IMongoCollection<WorkPlanStatusHistoryDocument> _coleccion;

    public MongoWorkPlanHistoryLogger(string connectionString, string nombreBaseDatos)
    {
        var cliente = new MongoClient(connectionString);
        var baseDatos = cliente.GetDatabase(nombreBaseDatos);
        _coleccion = baseDatos.GetCollection<WorkPlanStatusHistoryDocument>(NombreColeccion);
    }

    public Task RegistrarAsync(StatusChangeRecord registro)
    {
        var documento = WorkPlanStatusHistoryDocument.Desde(registro);
        return _coleccion.InsertOneAsync(documento);
    }
}
