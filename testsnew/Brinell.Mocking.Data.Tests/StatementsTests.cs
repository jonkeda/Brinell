namespace Brinell.Mocking.Data.Tests;

public sealed class StatementsTests
{
    [Theory]
    [InlineData("SELECT * FROM [Places] AS [p]", "dbo.places")]
    [InlineData("SELECT * FROM sales.Orders o JOIN [sales].[Lines] l ON l.OrderId = o.Id", "sales.orders,sales.lines")]
    [InlineData("INSERT INTO [dbo].[Places] ([Id]) VALUES (@p0)", "dbo.places")]
    [InlineData("UPDATE Places SET Name = @n", "dbo.places")]
    [InlineData("MERGE INTO [Stock] AS t USING src ON 1 = 1", "dbo.stock,dbo.src")]
    [InlineData("EXEC [dbo].[VisitCount] @PlaceId = @p0", "dbo.visitcount")]
    [InlineData("EXECUTE @result = billing.Close @id", "billing.close")]
    [InlineData("SELECT [Value] FROM OPENJSON(@p0)", "")]
    [InlineData("EXEC sp_executesql N'SELECT 1'", "")]
    public void Objects_AreNamedTablesViewsAndProcedures(string text, string expected)
        => Assert.Equal(expected.Split(',', StringSplitOptions.RemoveEmptyEntries), Statements.Objects(text).ToArray());

    [Fact]
    public void WithoutLiterals_ReplacesStringsAndNumbers_NotNamesOrParameters()
        => Assert.Equal(
            "SELECT TOP(?) [p1].[Name] FROM [Places] WHERE [Name] = ? AND [Kind] IN (?, ?) AND [Id] = @p0 AND [Rank] > ?",
            Statements.WithoutLiterals("SELECT TOP(2) [p1].[Name] FROM [Places] WHERE [Name] = N'O''Brien' AND [Kind] IN ('city', 'town') AND [Id] = @p0 AND [Rank] > -1.5"));

    [Theory]
    [InlineData("INSERT INTO Places VALUES (1)", true)]
    [InlineData("SET NOCOUNT ON; UPDATE Places SET Name = @n", true)]
    [InlineData("SELECT * FROM Places", false)]
    public void IsWrite_SeesInsertUpdateDeleteMerge(string text, bool expected) => Assert.Equal(expected, Statements.IsWrite(text));
}
