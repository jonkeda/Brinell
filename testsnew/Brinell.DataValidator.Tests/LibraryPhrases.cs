using Brinell.Core.Testing;
using Brinell.Uat;

namespace Brinell.DataValidator.Tests.Library;

[UatPhraseClass]
public sealed class LibraryPhrases : UatPhraseClassBase
{
    [UatWhen("I lend {title} to {reader} until {due}")]
    public void Lend(string title, UatUserRole reader, DateOnly due)
    {
    }

    [UatWhen("I shelve the books")]
    public void Shelve(IReadOnlyList<ShelvedBook> books)
    {
    }

    [UatWhen]
    public void When_I_forget_the_day(DateOnly day)
    {
    }
}
