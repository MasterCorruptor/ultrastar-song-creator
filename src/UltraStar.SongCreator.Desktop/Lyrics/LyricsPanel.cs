using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using UltraStar.SongCreator.Acquisition.Lyrics;
using UltraStar.SongCreator.Core;

namespace UltraStar.SongCreator.Desktop.Lyrics;

internal sealed record LyricsPanelReceipt(long Generation, LyricsSessionSnapshot Snapshot,
    LyricsSessionOutcome? Outcome = null, bool Failed = false);

/// <summary>Bounded synchronous admission, separate search observation and explicitly transferred ownership.</summary>
internal sealed class LyricsPanelLifetime
{
    private readonly object sync = new();
    private readonly IDisposable? ownedProvider;
    private readonly HashSet<Task> observations = [];
    private (long Generation, Func<Task<LyricsSessionOutcome>> Action)? pending;
    private Task pump = Task.CompletedTask;
    private Task<bool>? close;
    private bool running, executing, stopped;
    private long latest;
    internal LyricsAcquisitionSession Session { get; }
    internal event Action<LyricsPanelReceipt>? Completed;
    internal bool CleanupFailed { get; private set; }
    internal long LatestGeneration { get { lock (sync) return latest; } }

    // Non-null provider explicitly transfers both its and the matching session's ownership.
    internal LyricsPanelLifetime(LyricsAcquisitionSession session, IDisposable? ownedProvider = null)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        this.ownedProvider = ownedProvider;
    }

    internal bool IsSettled(long generation)
    {
        lock (sync) return !stopped && latest == generation && !executing && pending is null;
    }

    internal bool Enqueue(long generation, Func<Task<LyricsSessionOutcome>> action)
    {
        lock (sync)
        {
            if (stopped) return false;
            latest = generation;
            pending = (generation, action);
            if (!running)
            {
                running = true;
                pump = Task.Run(Pump);
            }
            return true;
        }
    }

    private void Pump()
    {
        while (true)
        {
            (long Generation, Func<Task<LyricsSessionOutcome>> Action) job;
            lock (sync)
            {
                if (stopped || pending is null) { running = false; executing = false; return; }
                job = pending.Value; pending = null; executing = true;
            }
            Task<LyricsSessionOutcome>? result = null;
            var failed = false;
            try { result = job.Action(); }
            catch (Exception) { failed = true; }
            lock (sync) executing = false;
            if (failed) Publish(new(job.Generation, Session.Current, Failed: true));
            else if (result is not null)
            {
                if (!result.IsCompleted) Publish(new(job.Generation, Session.Current));
                Observe(job.Generation, result);
            }
        }
    }

    private void Observe(long generation, Task<LyricsSessionOutcome> task)
    {
        // Register before starting observer so even synchronous completion cannot escape teardown.
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (sync) observations.Add(finished.Task);
        _ = ObserveCore();
        async Task ObserveCore()
        {
            try
            {
                var outcome = await task.ConfigureAwait(false);
                Publish(new(generation, Session.Current, outcome));
            }
            catch (Exception) { Publish(new(generation, Session.Current, Failed: true)); }
            finally
            {
                lock (sync) observations.Remove(finished.Task);
                finished.TrySetResult();
            }
        }
    }

    private void Publish(LyricsPanelReceipt receipt)
    {
        Action<LyricsPanelReceipt>? listeners;
        lock (sync)
        {
            if (stopped || latest != receipt.Generation) return;
            listeners = Completed;
        }
        if (listeners is null) return;
        foreach (Action<LyricsPanelReceipt> listener in listeners.GetInvocationList())
            try { listener(receipt); }
            catch (Exception) { /* Observers cannot fault admission or cleanup. */ }
    }

    internal void StopAdmission()
    {
        lock (sync) { stopped = true; pending = null; }
    }

    internal Task<bool> CloseAsync()
    {
        lock (sync)
        {
            if (close is not null) return close;
            stopped = true; pending = null;
            var admitted = pump;
            close = Task.Run(async () =>
            {
                try
                {
                    // DisposeAsync itself may run a blocking cancellation callback. Already off UI.
                    if (ownedProvider is not null) await Session.DisposeAsync().ConfigureAwait(false);
                    await admitted.ConfigureAwait(false);
                    Task[] remaining;
                    lock (sync) remaining = observations.ToArray();
                    await Task.WhenAll(remaining).ConfigureAwait(false);
                    ownedProvider?.Dispose();
                    return true;
                }
                catch (Exception) { CleanupFailed = true; return false; }
            });
            return close;
        }
    }
}

/// <summary>Explicit transient lyrics view. Borrows editing/acquisition; Detach never disposes either.</summary>
public sealed class LyricsPanel : UserControl
{
    private readonly EditingSession editing;
    private readonly LyricsPanelLifetime lifetime;
    private readonly Guid targetId;
    private long generation;
    private bool detached, populating;
    private LyricsPage? displayedPage;
    private LyricsSessionSnapshot? accepted;
    public TextBox ArtistInput { get; } = new() { PlaceholderText = "Artist" };
    public TextBox TitleInput { get; } = new() { PlaceholderText = "Sangtittel" };
    public Button SearchButton { get; } = new() { Content = "Søk etter sangtekst" };
    public Button CancelButton { get; } = new() { Content = "Avbryt søk" };
    public ComboBox Candidates { get; } = new();
    public ComboBox Variants { get; } = new();
    public TextBox ManualInput { get; } = new() { AcceptsReturn = true, PlaceholderText = "Lim inn egen sangtekst" };
    public Button PrepareManualButton { get; } = new() { Content = "Forbered innlimt tekst" };
    public Button ApplyButton { get; } = new() { Content = "Bruk valgt tekst i sangen" };
    public TextBox Preview { get; } = new() { IsReadOnly = true, AcceptsReturn = true };
    public TextBox StoredLyrics { get; } = new() { IsReadOnly = true, AcceptsReturn = true };
    public TextBlock TargetLabel { get; } = new();
    public TextBlock StatusLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public TextBlock DiagnosticsLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public TextBlock ProvenanceLabel { get; } = new() { TextWrapping = TextWrapping.Wrap };
    public TextBox TimingPreview { get; } = new() { IsReadOnly = true, AcceptsReturn = true };

    private sealed record CandidateChoice(LyricsCandidate Value)
    {
        public override string ToString() =>
            $"{Value.Artist} — {Value.Title} ({Value.DurationSeconds?.ToString("G", CultureInfo.InvariantCulture) ?? "?"} s) · {Value.TextStatus}";
    }
    private sealed record VariantChoice(string? Id, string Label)
    {
        public override string ToString() => Label;
    }

    public LyricsPanel(EditingSession editing, LyricsAcquisitionSession acquisition)
        : this(editing, new LyricsPanelLifetime(acquisition)) { }

    internal LyricsPanel(EditingSession editing, LyricsPanelLifetime lifetime)
    {
        Dispatcher.UIThread.VerifyAccess();
        this.editing = editing ?? throw new ArgumentNullException(nameof(editing));
        this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        targetId = editing.Current.Id;
        var body = new StackPanel { Spacing = 8 };
        foreach (var control in new Control[] { TargetLabel, ArtistInput, TitleInput, SearchButton, CancelButton,
            StatusLabel, Candidates, Variants, DiagnosticsLabel, ManualInput, PrepareManualButton,
            Preview, ProvenanceLabel, TimingPreview, ApplyButton,
            new TextBlock { Text = "Tekst som er lagret i gjeldende sang:" }, StoredLyrics })
            body.Children.Add(control);
        Content = body;
        ArtistInput.PropertyChanged += InputChanged;
        TitleInput.PropertyChanged += InputChanged;
        ManualInput.PropertyChanged += InputChanged;
        SearchButton.Click += (_, _) => Search();
        CancelButton.Click += (_, _) => Cancel();
        PrepareManualButton.Click += (_, _) => PrepareManual();
        ApplyButton.Click += (_, _) => Apply();
        Candidates.SelectionChanged += CandidateChanged;
        Variants.SelectionChanged += VariantChanged;
        lifetime.Completed += Receipt;
        lifetime.Session.Changed += AcquisitionChanged;
        editing.Changed += EditingChanged;
        ClearEligibility();
        SearchButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        StatusLabel.Text = "Oppgi artist og tittel, eller lim inn tekst.";
        RefreshStored();
    }

    private LyricsQuery Query() => new(ArtistInput.Text ?? "", TitleInput.Text ?? "");
    private long Invalidate()
    {
        generation++;
        accepted = null;
        ClearEligibility();
        CancelButton.IsEnabled = false;
        return generation;
    }
    private void ClearEligibility()
    {
        ApplyButton.IsEnabled = false;
        Candidates.IsEnabled = false;
        Variants.IsEnabled = false;
        Preview.Text = "";
        ProvenanceLabel.Text = "";
        TimingPreview.Text = "";
    }
    private void InputChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (detached || e.Property != TextBox.TextProperty) return;
        var intent = Invalidate();
        // Old query results are immediately hidden, before any Reset callback can block.
        populating = true;
        Candidates.ItemsSource = null; Variants.ItemsSource = null; displayedPage = null;
        populating = false;
        DiagnosticsLabel.Text = "";
        PrepareManualButton.IsEnabled = true;
        SearchButton.IsEnabled = !string.IsNullOrWhiteSpace(ArtistInput.Text) && !string.IsNullOrWhiteSpace(TitleInput.Text);
        StatusLabel.Text = "Inndata er endret. Søk eller forbered teksten på nytt.";
        lifetime.Enqueue(intent, () => Task.FromResult(lifetime.Session.Reset()));
    }

    internal void Search()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (detached || !SearchButton.IsEnabled) return;
        var query = Query();
        var intent = Invalidate();
        SearchButton.IsEnabled = false;
        StatusLabel.Text = "Starter søk …";
        lifetime.Enqueue(intent, () => lifetime.Session.SearchAsync(query));
    }
    internal void Cancel()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (detached || !CancelButton.IsEnabled) return;
        var intent = Invalidate();
        StatusLabel.Text = "Avbryter søk …";
        lifetime.Enqueue(intent, () => Task.FromResult(lifetime.Session.Cancel()));
    }
    internal void PrepareManual()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (detached || !PrepareManualButton.IsEnabled) return;
        var text = ManualInput.Text ?? "";
        var intent = Invalidate();
        PrepareManualButton.IsEnabled = false;
        StatusLabel.Text = "Forbereder innlimt tekst …";
        lifetime.Enqueue(intent, () => Task.FromResult(lifetime.Session.SetManualText(text)));
    }

    private void CandidateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (populating || detached || !Candidates.IsEnabled) return;
        var candidate = (Candidates.SelectedItem as CandidateChoice)?.Value;
        var snapshot = accepted;
        if (candidate is null || snapshot?.Page is null) return;
        var intent = Invalidate();
        populating = true;
        Variants.ItemsSource = candidate.TextStatus == LyricsTextStatus.Instrumental
            ? new[] { new VariantChoice(null, "Instrumental (uten sangtekst)") }
            : candidate.Documents.Select(d => new VariantChoice(d.Id, $"{d.Id} · {d.SyncLevel}")).ToArray();
        Variants.SelectedIndex = -1;
        populating = false;
        // Never prepare an instrumental or available document merely by choosing its candidate.
        var revision = snapshot.Revision;
        lifetime.Enqueue(intent, () => Task.FromResult(candidate.TextStatus == LyricsTextStatus.Available
            ? lifetime.Session.Select(revision, candidate.ProviderId, candidate.Id, null)
            : new LyricsSessionOutcome(LyricsSessionOutcomeKind.Unchanged)));
    }

    private void VariantChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (populating || detached || !Variants.IsEnabled) return;
        var candidate = (Candidates.SelectedItem as CandidateChoice)?.Value;
        var variant = Variants.SelectedItem as VariantChoice;
        var snapshot = accepted;
        if (candidate is null || variant is null || snapshot?.Page is null) return;
        var intent = Invalidate();
        var revision = snapshot.Revision;
        lifetime.Enqueue(intent, () => Task.FromResult(lifetime.Session.Select(revision, candidate.ProviderId, candidate.Id, variant.Id)));
    }

    private void Receipt(LyricsPanelReceipt receipt)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (detached || generation != receipt.Generation || !lifetime.IsSettled(generation)) return;
            if (lifetime.Session.Current.Revision != receipt.Snapshot.Revision) return;
            if (receipt.Failed)
            {
                accepted = null; ClearEligibility();
                StatusLabel.Text = "Sangteksthandlingen kunne ikke fullføres.";
                EnableInputs(); return;
            }
            if (receipt.Outcome?.Kind is LyricsSessionOutcomeKind.Superseded or LyricsSessionOutcomeKind.Closed) return;
            Render(receipt.Snapshot, receipt.Outcome?.Kind == LyricsSessionOutcomeKind.Rejected);
        });
    }

    private void AcquisitionChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (detached) return;
            // Other users of a borrowed session cannot silently replace our explicit preview.
            if (accepted is not null && accepted.Revision != lifetime.Session.Current.Revision)
            {
                accepted = null; ClearEligibility();
            }
        });
    }
    private void EnableInputs()
    {
        SearchButton.IsEnabled = !string.IsNullOrWhiteSpace(ArtistInput.Text) && !string.IsNullOrWhiteSpace(TitleInput.Text);
        PrepareManualButton.IsEnabled = true;
    }
    private void Render(LyricsSessionSnapshot snapshot, bool rejected)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (snapshot.Query is not null && snapshot.Query != Query()) return;
        accepted = snapshot;
        EnableInputs();
        CancelButton.IsEnabled = snapshot.Status == LyricsSessionStatus.Searching;
        StatusLabel.Text = snapshot.Error is { } error ? ErrorText(error) : rejected
            ? "Valget er ikke lenger gyldig. Velg på nytt."
            : snapshot.Status switch
            {
                LyricsSessionStatus.Searching => "Søker etter sangtekst …",
                LyricsSessionStatus.Results => "Velg innspilling og tekstvariant eksplisitt.",
                LyricsSessionStatus.NoResults => "Ingen sangtekst funnet. Du kan lime inn tekst.",
                LyricsSessionStatus.Cancelled => "Søket er avbrutt.",
                LyricsSessionStatus.ManualReady => "Innlimt tekst er klar til kontroll.",
                LyricsSessionStatus.Disposed => "Sangtekstøkten er lukket.",
                _ => "Oppgi artist og tittel, eller lim inn tekst."
            };
        if (!ReferenceEquals(displayedPage, snapshot.Page))
        {
            populating = true;
            displayedPage = snapshot.Page;
            Candidates.ItemsSource = snapshot.Page?.Candidates.Select(c => new CandidateChoice(c)).ToArray();
            Candidates.SelectedIndex = -1;
            Variants.ItemsSource = null;
            populating = false;
        }
        Candidates.IsEnabled = snapshot.Status == LyricsSessionStatus.Results;
        Variants.IsEnabled = Candidates.IsEnabled && Candidates.SelectedItem is CandidateChoice;
        DiagnosticsLabel.Text = (Candidates.SelectedItem as CandidateChoice)?.Value is { } candidate
            ? Diagnostics(candidate) : "";
        var selected = snapshot.PreparedSelection;
        var explicitChoice = snapshot.Status == LyricsSessionStatus.ManualReady ||
            (Candidates.SelectedItem is CandidateChoice c && Variants.SelectedItem is VariantChoice v &&
             c.Value.Id == selected?.Provenance.CandidateId &&
             (selected.Kind == SelectedLyricsKind.Instrumental || v.Id == selected.Document?.Id));
        ApplyButton.IsEnabled = !rejected && selected is not null && explicitChoice && editing.Current.Id == targetId;
        Preview.Text = ApplyButton.IsEnabled ? selected?.Document?.Text ?? "Instrumental (uten sangtekst)" : "";
        ProvenanceLabel.Text = ApplyButton.IsEnabled ? Provenance(selected!) : "";
        TimingPreview.Text = ApplyButton.IsEnabled ? Timings(selected!) : "";
    }

    private static string ErrorText(LyricsSessionError error)
    {
        var message = error.Kind switch
        {
            LyricsSessionErrorKind.Validation => $"Sangtekstvalget er ugyldig ({error.ValidationError}).",
            LyricsSessionErrorKind.Provider => $"Sangtekstkilden kunne ikke svare ({error.ProviderError}).",
            _ => "Sangteksthandlingen kunne ikke fullføres."
        };
        if (error.HttpStatus is { } status) message += $" HTTP {status}.";
        if (error.RetryAfter is { } retry) message += $" Prøv igjen om minst {retry.TotalSeconds.ToString("G", CultureInfo.InvariantCulture)} sekunder.";
        return message;
    }
    private static string Diagnostics(LyricsCandidate candidate) => string.Join("\n",
        candidate.VariantDiagnostics.Select(d => $"{d.FormatId}/{d.VariantId}: {d.Severity} · {d.Code} · linje {d.PhysicalLineNumber?.ToString(CultureInfo.InvariantCulture) ?? "?"}")
        .Concat(candidate.Capabilities.UnsupportedFormats.Select(f => $"Ustøttet format: {f}")));
    private static string Provenance(SelectedLyrics value)
    {
        var p = value.Provenance;
        return $"{p.Origin} · {p.SourceId} · {p.CandidateId}\n{p.Artist} — {p.Title} · {p.Version}\n{p.SourceUrl}\nOpprettet: {p.CreatedAtUtc:O}\nHentet: {p.RetrievedAtUtc:O}\n{value.Document?.SyncLevel} · LyricsSourceSeconds (ikke justert til lyd)";
    }
    private static string Timings(SelectedLyrics value)
    {
        var text = new StringBuilder();
        if (value.Document is { } doc)
            foreach (var line in doc.Lines)
            {
                text.AppendLine($"{line.StartSeconds.ToString("G", CultureInfo.InvariantCulture)}–{line.EndSeconds?.ToString("G", CultureInfo.InvariantCulture) ?? "?"}: {line.Text}");
                foreach (var word in line.Words)
                    text.AppendLine($"  {word.StartSeconds.ToString("G", CultureInfo.InvariantCulture)}–{word.EndSeconds?.ToString("G", CultureInfo.InvariantCulture) ?? "?"}: {word.Text}");
            }
        return text.ToString();
    }

    internal bool Apply()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (detached || !ApplyButton.IsEnabled || accepted?.PreparedSelection is not { } selected) return false;
        if (!lifetime.IsSettled(generation) || lifetime.Session.Current.Revision != accepted.Revision || editing.Current.Id != targetId)
        {
            accepted = null; ClearEligibility(); return false;
        }
        var changed = editing.ApplySelectedLyrics(selected);
        StatusLabel.Text = editing.Message;
        return changed;
    }
    private void EditingChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.VerifyAccess();
        if (!detached) RefreshStored();
    }
    private void RefreshStored()
    {
        TargetLabel.Text = $"Gjeldende sang/demo: {editing.Current.Metadata.Artist} — {editing.Current.Metadata.Title}\nSangtekstvalget bekrefter ikke samsvar med demolyden.";
        StoredLyrics.Text = editing.Current.SelectedLyrics is { } value
            ? value.Document?.Text ?? "Instrumental (uten sangtekst)" : "";
        if (editing.Current.Id != targetId) ApplyButton.IsEnabled = false;
    }

    public void Detach()
    {
        Dispatcher.UIThread.VerifyAccess();
        if (detached) return;
        detached = true;
        Invalidate();
        lifetime.Completed -= Receipt;
        lifetime.Session.Changed -= AcquisitionChanged;
        editing.Changed -= EditingChanged;
        lifetime.StopAdmission();
        foreach (var control in new Control[] { ArtistInput, TitleInput, SearchButton, CancelButton, Candidates, Variants,
            ManualInput, PrepareManualButton, ApplyButton }) control.IsEnabled = false;
    }
    internal Task<bool> CloseAsync()
    {
        Detach();
        return lifetime.CloseAsync();
    }
}
