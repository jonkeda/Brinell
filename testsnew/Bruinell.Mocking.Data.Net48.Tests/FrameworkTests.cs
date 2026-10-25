using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Xunit;

namespace Bruinell.Mocking.Data.Net48.Tests;

/// <summary>The ADO.NET wrapper in a .NET Framework process, over the framework's own SqlClient and the local SQL Server.</summary>
public sealed class FrameworkTests
{
    private static string Server => Environment.GetEnvironmentVariable("BRUINELL_SQL_SERVER") is { Length: > 0 } server ? server : ".";

    [Fact]
    public void Wrapper_LogsTheFrameworksSqlClient_CommandsReadersAndProcedures()
    {
        Assert.StartsWith(".NET Framework", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        var log = new SqlLog(null, keep: true);
        using (var connection = new LoggingDbConnection(new SqlConnection($"Server={Server};Database=master;Integrated Security=True"), log))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sys.databases WHERE name = @name";
                var name = command.CreateParameter();
                name.ParameterName = "@name";
                name.Value = "master";
                command.Parameters.Add(name);
                using (var reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.Equal("master", reader.GetString(0));
                }
            }

            using (var procedure = connection.CreateCommand())
            {
                procedure.CommandType = CommandType.StoredProcedure;
                procedure.CommandText = "sys.sp_server_info";
                using (var reader = procedure.ExecuteReader())
                {
                    while (reader.Read())
                    {
                    }
                }
            }
        }

        var entries = log.Entries;
        Assert.Equal("open", entries[0].Kind);
        var query = entries.Single(entry => entry.Text == "SELECT name FROM sys.databases WHERE name = @name");
        Assert.Equal(1, query.Rows);
        Assert.Equal(new[] { "@name" }, query.Parameters);
        Assert.Contains(entries, entry => entry.Text == "EXEC sys.sp_server_info" && entry.Rows > 0);
    }
}
