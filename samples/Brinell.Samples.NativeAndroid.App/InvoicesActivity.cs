using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Google.Android.Material.Snackbar;

namespace Brinell.Samples.NativeAndroid.App;

/// <summary>
/// 200 invoices in a <c>RecyclerView</c>, so rows are recycled as the list scrolls. Delete removes a
/// row with an Undo snackbar; pulling to refresh is busy for a moment.
/// </summary>
[Activity(Name = "com.brinell.samples.nativeandroid.InvoicesActivity", Label = "Invoices", Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = Scheme, DataHost = "invoices")]
public sealed class InvoicesActivity : GalleryActivity
{
    private readonly List<(string Name, string Amount)> _invoices =
        [.. Enumerable.Range(1, 200).Select(n => ($"Invoice {n}", $"€ {n * 10},00"))];

    private InvoiceAdapter _adapter = null!;
    private TextView _count = null!;

    protected override int Layout => Resource.Layout.invoices;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _count = FindViewById<TextView>(Resource.Id.count)!;
        _adapter = new InvoiceAdapter(_invoices, Delete);
        var list = FindViewById<RecyclerView>(Resource.Id.invoices)!;
        list.SetLayoutManager(new LinearLayoutManager(this));
        list.SetAdapter(_adapter);

        var refresh = FindViewById<SwipeRefreshLayout>(Resource.Id.refresh)!;
        refresh.Refresh += (_, _) => Busy(1000, () =>
        {
            refresh.Refreshing = false;
            _count.Text = $"{_invoices.Count} invoices, refreshed";
        });

        ShowCount();
    }

    private void Delete(int position)
    {
        var removed = _invoices[position];
        _invoices.RemoveAt(position);
        _adapter.NotifyItemRemoved(position);
        ShowCount();
        var deleted = Snackbar.Make(FindViewById(Resource.Id.coordinator)!, $"Deleted {removed.Name}", Snackbar.LengthLong)
            .SetAction("Undo", _ =>
            {
                _invoices.Insert(position, removed);
                _adapter.NotifyItemInserted(position);
                ShowCount();
            });
        deleted.SetDuration(SnackbarMs);
        deleted.Show();
    }

    private void ShowCount() => _count.Text = $"{_invoices.Count} invoices";

    private sealed class InvoiceAdapter(List<(string Name, string Amount)> invoices, Action<int> delete) : RecyclerView.Adapter
    {
        public override int ItemCount => invoices.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var row = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.invoice_row, parent, false)!;
            var holder = new Row(row);
            row.FindViewById(Resource.Id.delete)!.Click += (_, _) =>
            {
                if (holder.BindingAdapterPosition != RecyclerView.NoPosition)
                {
                    delete(holder.BindingAdapterPosition);
                }
            };
            return holder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var (name, amount) = invoices[position];
            holder.ItemView.FindViewById<TextView>(Resource.Id.name)!.Text = name;
            holder.ItemView.FindViewById<TextView>(Resource.Id.amount)!.Text = amount;
        }

        private sealed class Row(View view) : RecyclerView.ViewHolder(view);
    }
}
