using Brinell.Core.Composition;
using Brinell.Core.Testing;

namespace Brinell.Uat.Tests.PageNamespaceCases;

// Two pages named Ledger, one per namespace: the composition keeps both.
[TestPage("Ledger")]
[UatNamespace("Invoicing")]
[UatIgnore]
public sealed class InvoicingLedgerPage
{
}

[TestPage("Ledger")]
[UatNamespace("Accounts")]
[UatIgnore]
public sealed class AccountsLedgerPage
{
}
