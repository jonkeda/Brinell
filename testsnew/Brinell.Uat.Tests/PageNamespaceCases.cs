using Brinell.Core.Composition;
using Brinell.Core.Testing;

namespace Brinell.Uat.Tests.PageNamespaceCases;

// Two pages named Ledger, one per namespace: the composition keeps both.
[TestPage("Ledger")]
[BddNamespace("Invoicing")]
[BddIgnore]
public sealed class InvoicingLedgerPage
{
}

[TestPage("Ledger")]
[BddNamespace("Accounts")]
[BddIgnore]
public sealed class AccountsLedgerPage
{
}
