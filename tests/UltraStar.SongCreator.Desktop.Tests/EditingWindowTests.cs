using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using UltraStar.SongCreator.Core;
using UltraStar.SongCreator.Desktop;

[assembly: AvaloniaTestApplication(typeof(UltraStar.SongCreator.Desktop.Tests.TestAppBuilder))]

namespace UltraStar.SongCreator.Desktop.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<EditorApplication>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class EditingWindowTests
{
    private static EditorWindow Open()
    {
        var window = new EditorWindow(new EditingSession(DemoSong.Create()));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
    private static Button FirstNote(EditorWindow w) => w.Timeline.Children.OfType<Button>()
        .Single(b => (Guid)b.Tag! == w.Session.Current.Phrases[0].Notes[0].Id);
    private static void Click(EditorWindow w, Control control)
    {
        Dispatcher.UIThread.RunJobs();
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), w)!.Value;
        w.MouseDown(point, MouseButton.Left, RawInputModifiers.None);
        w.MouseUp(point, MouseButton.Left, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }
    private static void Enter(EditorWindow w, string text)
    {
        w.StartInput.Focus();
        w.StartInput.SelectAll();
        w.KeyTextInput(text);
    }

    [AvaloniaFact]
    public void InputMovesAndUndoRedoRestoreExactSnapshotsAndGeometry()
    {
        var w = Open();
        try
        {
            var before = w.Session.Current;
            Assert.False(w.MoveButton.IsEnabled);
            Click(w, FirstNote(w));
            Assert.Equal(before.Phrases[0].Notes[0].Id, w.Session.SelectedNoteId);
            Enter(w, "2,5");
            Click(w, w.MoveButton);
            var after = w.Session.Current;
            Assert.NotSame(before, after);
            Assert.Equal(2.5, after.Phrases[0].Notes[0].StartSeconds);
            Assert.Equal(EditorWindow.TimeOrigin + 2.5 * EditorWindow.SecondsScale, Canvas.GetLeft(FirstNote(w)));
            Assert.True(w.UndoButton.IsEnabled); Assert.False(w.RedoButton.IsEnabled);
            Click(w, w.UndoButton);
            Assert.Same(before, w.Session.Current);
            Assert.Equal(EditorWindow.TimeOrigin + EditorWindow.SecondsScale, Canvas.GetLeft(FirstNote(w)));
            Assert.False(w.UndoButton.IsEnabled); Assert.True(w.RedoButton.IsEnabled);
            Click(w, w.RedoButton);
            Assert.Same(after, w.Session.Current);
            Assert.Contains("2.5", w.StartInput.Text);
            Assert.Equal(EditorWindow.TimeOrigin + 2.5 * EditorWindow.SecondsScale, Canvas.GetLeft(FirstNote(w)));
            Assert.False(w.RedoButton.IsEnabled);
        }
        finally { w.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-1")]
    [InlineData("7.5")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1e999")]
    public void RejectedInputPreservesSongHistoryAndVisibleGeometry(string input)
    {
        var w = Open();
        try
        {
            Click(w, FirstNote(w));
            var before = w.Session.Current;
            var left = Canvas.GetLeft(FirstNote(w));
            Enter(w, input);
            Click(w, w.MoveButton);
            Assert.Same(before, w.Session.Current);
            Assert.Equal(left, Canvas.GetLeft(FirstNote(w)));
            Assert.False(w.UndoButton.IsEnabled); Assert.False(w.RedoButton.IsEnabled);
            Assert.False(string.IsNullOrWhiteSpace(w.MessageLabel.Text));
            Assert.DoesNotContain("Noten er flyttet", w.MessageLabel.Text);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void RejectedAndNoOpEditsPreserveRedoButNewEditClearsIt()
    {
        var w = Open();
        try
        {
            Click(w, FirstNote(w)); Enter(w, "2"); Click(w, w.MoveButton);
            Click(w, w.UndoButton);
            var before = w.Session.Current;
            Enter(w, "NaN"); Click(w, w.MoveButton);
            Assert.Same(before, w.Session.Current); Assert.True(w.RedoButton.IsEnabled);
            Enter(w, "1"); Click(w, w.MoveButton);
            Assert.Same(before, w.Session.Current); Assert.True(w.RedoButton.IsEnabled);
            Enter(w, "4"); Click(w, w.MoveButton);
            Assert.False(w.RedoButton.IsEnabled);
        }
        finally { w.Close(); }
    }

    [AvaloniaFact]
    public void TimelineUsesDomainSecondsDurationAndMidiAndValidationFollowsSnapshot()
    {
        var w = Open();
        try
        {
            foreach (var n in w.Session.Current.Phrases[0].Notes)
            {
                var b = w.Timeline.Children.OfType<Button>().Single(b => (Guid)b.Tag! == n.Id);
                Assert.Equal(EditorWindow.TimeOrigin + n.StartSeconds * EditorWindow.SecondsScale, Canvas.GetLeft(b));
                Assert.Equal(n.DurationSeconds * EditorWindow.SecondsScale, b.Width);
                Assert.Equal(EditorWindow.PitchTop(n.MidiPitch), Canvas.GetTop(b));
                Assert.Equal(n.Text.Trim(), b.Content);
            }
            Assert.Contains("ingen lydfil", w.ValidationLabel.Text);
            Click(w, FirstNote(w)); Enter(w, "3"); Click(w, w.MoveButton);
            Assert.Contains("overlapper", w.ValidationLabel.Text);
            Click(w, w.UndoButton);
            Assert.DoesNotContain("overlapper", w.ValidationLabel.Text);
        }
        finally { w.Close(); }
    }
}
