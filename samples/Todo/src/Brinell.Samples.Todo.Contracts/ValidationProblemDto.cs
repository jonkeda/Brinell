namespace Brinell.Samples.Todo.Contracts;

/// <summary>
/// The part of the API's validation problem document (RFC 9457) a client reads: the status and
/// the messages per field. The real API sends more members (<c>title</c>, <c>type</c>); clients
/// ignore them.
/// </summary>
/// <param name="Status">400 for a value that breaks a rule, 409 for a name that is taken.</param>
/// <param name="Errors">The messages, by JSON property name.</param>
public sealed record ValidationProblemDto(int Status, IReadOnlyDictionary<string, string[]> Errors);
