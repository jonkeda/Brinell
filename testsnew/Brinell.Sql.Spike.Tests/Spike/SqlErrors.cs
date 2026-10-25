using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>
/// A recorded SQL Server error as a real <see cref="SqlException"/>, so the app's <c>catch (SqlException e) when
/// (e.Number == 2627)</c> runs in replay. <see cref="SqlException"/> has no public constructor: this builds one by
/// reflection over SqlClient's internals, which a SqlClient upgrade may move.
/// </summary>
public static class SqlErrors
{
    private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    public static SqlException Create(RecordedError error)
    {
        var sqlError = NewError(error);
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection).GetMethod("Add", Any, [typeof(SqlError)])!.Invoke(errors, [sqlError]);

        var create = typeof(SqlException).GetMethods(Any)
            .Where(method => method.Name == "CreateException")
            .OrderBy(method => method.GetParameters().Length)
            .First(method => method.GetParameters() is [{ ParameterType: var first }, { ParameterType: var second }, ..]
                && first == typeof(SqlErrorCollection) && second == typeof(string));
        var arguments = create.GetParameters().Select((parameter, i) => i switch
        {
            0 => errors,
            1 => (object?)"16.0",
            _ => parameter.HasDefaultValue ? parameter.DefaultValue : Default(parameter.ParameterType),
        }).ToArray();
        return (SqlException)create.Invoke(null, arguments)!;
    }

    // The constructor with the most parameters, each filled by its name: number, state, class, message, server.
    private static SqlError NewError(RecordedError error)
    {
        var constructor = typeof(SqlError).GetConstructors(Any).OrderByDescending(c => c.GetParameters().Length).First();
        var arguments = constructor.GetParameters().Select(parameter => parameter.Name switch
        {
            "infoNumber" or "number" => error.Number,
            "errorState" or "state" => error.State,
            "errorClass" or "class" => error.Class,
            "errorMessage" or "message" => error.Message,
            "server" => error.Server ?? string.Empty,
            "procedure" => string.Empty,
            _ => Default(parameter.ParameterType),
        }).ToArray();
        return (SqlError)constructor.Invoke(arguments);
    }

    private static object? Default(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
}
