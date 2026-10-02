using Brinell.Core.Testing;
using Brinell.Uat;

namespace Brinell.DataValidator.Tests.Library;

[BddStepDefinitions]
public sealed class LibraryPhrases : BddStepDefinitionsBase
{
    [BddWhen("I lend {title} to {reader} until {due}")]
    public void Lend(string title, BddUserRole reader, DateOnly due)
    {
    }

    [BddWhen("I shelve the books")]
    public void Shelve(IReadOnlyList<ShelvedBook> books)
    {
    }

    [BddWhen]
    public void When_I_forget_the_day(DateOnly day)
    {
    }
}
