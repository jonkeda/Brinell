using System.Data.Common;

namespace Brinell.Samples.Todo.Infrastructure.Data;

/// <summary>A parameter by name and value on any provider's command: the repositories use plain ADO.NET, so a test can wrap the connection.</summary>
internal static class DbCommandExtensions
{
    public static DbParameter AddParameter(this DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}
