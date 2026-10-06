using System.Globalization;
using System.Text;
using Brinell.Core.Recording;

namespace Brinell.Narrator;

/// <summary>Which touch marks a composed video gets.</summary>
public enum BddTouchMode
{
    /// <summary>Every mark: rings and ripples for taps, a travelling dot for swipes, outlines and chips.</summary>
    On,

    /// <summary>No marks: the video as recorded.</summary>
    Off,

    /// <summary>Only the target's outline and the key chips: no rings, ripples or swipe dots.</summary>
    OutlineOnly,
}

/// <summary>
/// How a composed video shows the test's taps and swipes: the script's <c>Touches</c> and
/// <c>TouchColor</c>, over <see cref="ColorVariable"/>. Composition only: a scene's hash and its
/// raw video do not change with them.
/// </summary>
/// <param name="Mode">Which marks are drawn.</param>
/// <param name="Color">The accent, <c>#RRGGBB</c>.</param>
public sealed record BddTouchMarks(BddTouchMode Mode, string Color)
{
    /// <summary>The accent when nothing names one: Exact red.</summary>
    public const string DefaultColor = "#E8111E";

    /// <summary>The accent, when the script does not name one.</summary>
    public const string ColorVariable = "BRINELL_TOUCH_COLOR";

    /// <summary>Every mark, in Exact red.</summary>
    public static BddTouchMarks Default { get; } = new(BddTouchMode.On, DefaultColor);

    /// <summary>
    /// The marks a script asks for. Its accent, else the host's setting, else
    /// <see cref="ColorVariable"/>, else Exact red; a value that is not <c>#RRGGBB</c> is passed over.
    /// </summary>
    /// <param name="script">The video script; null for a run without one.</param>
    /// <param name="read">Reads a process variable; the process's by default.</param>
    /// <param name="setting">The host's touch colour setting (the Presenter's); null for none.</param>
    public static BddTouchMarks For(BddVideoScript? script, Func<string, string?>? read = null, string? setting = null)
    {
        read ??= Environment.GetEnvironmentVariable;
        var color = new[] { script?.TouchColor, setting, read(ColorVariable) }.FirstOrDefault(IsColor)?.Trim().ToUpperInvariant() ?? DefaultColor;
        return new BddTouchMarks(script?.Touches ?? BddTouchMode.On, color);
    }

    /// <summary>Whether a value is a colour a script may give: <c>#RRGGBB</c>.</summary>
    /// <param name="value">The value.</param>
    public static bool IsColor(string? value)
        => value?.Trim() is { Length: 7 } text && text[0] == '#' && text[1..].All(char.IsAsciiHexDigit);

    /// <summary>A script's <c>Touches</c> value, or null when it is not one.</summary>
    /// <param name="value">The value: on, off or outline-only.</param>
    public static BddTouchMode? ParseMode(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "on" => BddTouchMode.On,
        "off" => BddTouchMode.Off,
        "outline-only" => BddTouchMode.OutlineOnly,
        _ => null,
    };
}

/// <summary>
/// Draws a video's touch marks from its timeline: an ASS subtitle track of vector shapes that
/// ffmpeg's <c>subtitles</c> filter (libass) burns into the composed video. A tap's ring closes in
/// before the tap, while the screen still shows what is pressed; then a dot and a ripple. A swipe is
/// a dot travelling with a trail and an arrow; typing is the field's outline and a keyboard chip; a
/// key is a chip in the corner. Every mark is the accent with a white stroke and a dark halo, so it
/// reads on light and dark screens, sized from the video's short side.
/// </summary>
public static class BddTouchOverlay
{
    /// <summary>How long a tap's ring takes to close, before the tap.</summary>
    public const double PreRoll = 0.25;

    private const double Ripple = 0.4;
    private const double SwipeTravel = 0.4;
    private const double DoubleTapGap = 0.12;
    private const double ChipTime = 0.6;
    private const double TypeTime = 0.4;

    /// <summary>The ASS file for a composed video; null when there is nothing to draw.</summary>
    /// <param name="timeline">The video's timeline, every scene on one clock.</param>
    /// <param name="width">The composed video's width.</param>
    /// <param name="height">The composed video's height.</param>
    /// <param name="marks">Which marks, in which colour.</param>
    public static string? Ass(BddVideoTimeline timeline, int width, int height, BddTouchMarks marks)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(marks);
        if (marks.Mode == BddTouchMode.Off || width <= 0 || height <= 0)
        {
            return null;
        }

        var draw = new Drawing(width, height, AssColor(marks.Color), marks.Mode == BddTouchMode.OutlineOnly);
        var sceneStart = 0d;
        foreach (var scene in timeline.Scenes)
        {
            var sceneEnd = sceneStart + scene.Duration;
            if (scene.Interactions is { Count: > 0 } interactions && Frame.Of(scene, width, height) is { } frame)
            {
                foreach (var interaction in interactions)
                {
                    draw.Mark(interaction, frame, sceneStart, sceneEnd);
                }
            }

            sceneStart = sceneEnd;
        }

        return draw.Lines == 0 ? null : draw.ToString();
    }

    /// <summary>An <c>#RRGGBB</c> colour as ASS writes it: <c>&amp;HBBGGRR&amp;</c>.</summary>
    /// <param name="color">The colour.</param>
    public static string AssColor(string color)
    {
        if (!BddTouchMarks.IsColor(color))
        {
            throw new ArgumentException($"'{color}' is not a colour: #RRGGBB.", nameof(color));
        }

        var text = color.Trim();
        return $"&H{text[5..7]}{text[3..5]}{text[1..3]}&".ToUpperInvariant();
    }

    // Where a scene's driver coordinates land in the composed frame: through the area its video
    // shows, then scaled and padded to the composed size as the composer does.
    private sealed record Frame(double OffsetX, double OffsetY, double ScaleX, double ScaleY, double SourceX, double SourceY, int Width, int Height)
    {
        public static Frame? Of(BddSceneTimeline scene, int width, int height)
        {
            if (scene.Width <= 0 || scene.Height <= 0)
            {
                return null;
            }

            var source = scene.Source ?? new BddBox(0, 0, scene.Width, scene.Height);
            if (source.IsEmpty)
            {
                return null;
            }

            var fit = Math.Min((double)width / scene.Width, (double)height / scene.Height);
            return new Frame(
                (width - (scene.Width * fit)) / 2,
                (height - (scene.Height * fit)) / 2,
                (double)scene.Width / source.Width * fit,
                (double)scene.Height / source.Height * fit,
                source.X,
                source.Y,
                width,
                height);
        }

        public (double X, double Y) Point(double x, double y)
            => (OffsetX + ((x - SourceX) * ScaleX), OffsetY + ((y - SourceY) * ScaleY));

        public (double X, double Y, double Width, double Height) Box(BddBox box)
        {
            var (left, top) = Point(box.X, box.Y);
            return (left, top, box.Width * ScaleX, box.Height * ScaleY);
        }

        public bool Shows(double x, double y) => x >= 0 && y >= 0 && x <= Width && y <= Height;
    }

    private sealed class Drawing
    {
        private readonly StringBuilder _events = new();
        private readonly int _width;
        private readonly int _height;
        private readonly string _accent;
        private readonly bool _outlineOnly;
        private readonly double _r;
        private readonly double _stroke;

        public Drawing(int width, int height, string accent, bool outlineOnly)
        {
            _width = width;
            _height = height;
            _accent = accent;
            _outlineOnly = outlineOnly;
            _r = Math.Max(8, Math.Min(width, height) * 0.03);
            _stroke = Math.Max(2, _r / 8);
        }

        public int Lines { get; private set; }

        public void Mark(BddTimedInteraction interaction, Frame frame, double sceneStart, double sceneEnd)
        {
            var at = interaction.At;
            if (interaction.Kind == InteractionKind.Key)
            {
                Chip(Clamp(at, sceneStart, sceneEnd), Clamp(at + ChipTime, sceneStart, sceneEnd), KeyText(interaction.Key));
                return;
            }

            if (interaction.Bounds.IsEmpty)
            {
                return;
            }

            var box = frame.Box(interaction.Bounds);
            var (cx, cy) = (box.X + (box.Width / 2), box.Y + (box.Height / 2));
            if (!frame.Shows(cx, cy))
            {
                return;
            }

            double Start(double time) => Clamp(time, sceneStart, sceneEnd);
            switch (interaction.Kind)
            {
                case InteractionKind.Tap:
                case InteractionKind.RightClick:
                    Outline(box, Start(at - PreRoll), Start(at + Ripple));
                    Tap(cx, cy, at, sceneStart, sceneEnd, ripples: 1);
                    if (interaction.Kind == InteractionKind.RightClick)
                    {
                        Label(Start(at), Start(at + ChipTime), box, "Right click");
                    }

                    break;

                case InteractionKind.DoubleTap:
                    Outline(box, Start(at - PreRoll), Start(at + DoubleTapGap + Ripple));
                    Tap(cx, cy, at, sceneStart, sceneEnd, ripples: 2);
                    break;

                case InteractionKind.LongPress:
                    var hold = Math.Max(0.5, interaction.Duration);
                    Outline(box, Start(at - PreRoll), Start(at + hold + Ripple));
                    LongPress(cx, cy, Start(at), Start(at + hold), Start(at + hold + Ripple));
                    break;

                case InteractionKind.Hover:
                    Outline(box, Start(at), Start(at + TypeTime));
                    break;

                case InteractionKind.Swipe:
                    Swipe(interaction, frame, box, at, sceneStart, sceneEnd);
                    break;

                case InteractionKind.Type:
                case InteractionKind.Clear:
                    var end = Start(at + Math.Max(TypeTime, interaction.Duration));
                    Outline(box, Start(at), end);
                    Label(Start(at), end, box, interaction.Kind == InteractionKind.Type ? "⌨" : "⌨ ⌫");
                    break;
            }
        }

        public override string ToString()
        {
            var size = Math.Max(16, Math.Round(Math.Min(_width, _height) * 0.035));
            var text = new StringBuilder()
                .Append("[Script Info]\n")
                .Append("; Touch marks drawn by Brinell's narrator from the timeline's interactions.\n")
                .Append("ScriptType: v4.00+\n")
                .Append(CultureInfo.InvariantCulture, $"PlayResX: {_width}\nPlayResY: {_height}\n")
                .Append("ScaledBorderAndShadow: yes\nWrapStyle: 2\n\n")
                .Append("[V4+ Styles]\n")
                .Append("Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n")
                .Append("Style: Mark,Arial,20,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,0,0,5,0,0,0,1\n")
                .Append(CultureInfo.InvariantCulture, $"Style: Chip,Segoe UI Symbol,{size},&H00FFFFFF,&H00FFFFFF,{Opaque(_accent)},&H64000000,1,0,0,0,100,100,0,0,3,{Math.Round(size / 3)},0,3,24,24,24,1\n\n")
                .Append("[Events]\n")
                .Append("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\n")
                .Append(_events);
            return text.ToString();
        }

        // A ring closes onto the point before the tap; then a dot and a ripple, twice for a double tap.
        private void Tap(double x, double y, double at, double sceneStart, double sceneEnd, int ripples)
        {
            if (_outlineOnly)
            {
                return;
            }

            var closing = Clamp(at - PreRoll, sceneStart, sceneEnd);
            var closeMs = Ms(at - closing);
            Ring(closing, Clamp(at, sceneStart, sceneEnd), x, y, _r, $"\\fscx220\\fscy220\\t(0,{closeMs},\\fscx100\\fscy100)");
            for (var index = 0; index < ripples; index++)
            {
                var time = at + (index * DoubleTapGap);
                var start = Clamp(time, sceneStart, sceneEnd);
                var end = Clamp(time + Ripple, sceneStart, sceneEnd);
                Dot(start, end, x, y, _r * 0.45, $"\\fad(0,{Ms(Ripple / 2)})");
                Ring(start, end, x, y, _r, $"\\t(0,{Ms(Ripple)},\\fscx250\\fscy250\\alpha&HFF&)");
            }
        }

        // The dot holds while a ring grows round it; then the ripple.
        private void LongPress(double x, double y, double start, double release, double end)
        {
            if (_outlineOnly)
            {
                return;
            }

            Dot(start, end, x, y, _r * 0.45, $"\\fad(0,{Ms(Ripple / 2)})");
            Ring(start, release, x, y, _r, $"\\fscx60\\fscy60\\t(0,{Ms(release - start)},\\fscx160\\fscy160)");
            Ring(release, end, x, y, _r * 1.6, $"\\t(0,{Ms(end - release)},\\fscx180\\fscy180\\alpha&HFF&)");
        }

        // A dot travels from where the swipe starts to where it ends, with a fading trail, and an
        // arrow shows at the end. A bridge's swipe is instant, so the travel ends when it runs.
        private void Swipe(BddTimedInteraction interaction, Frame frame, (double X, double Y, double Width, double Height) box, double at, double sceneStart, double sceneEnd)
        {
            double Start(double time) => Clamp(time, sceneStart, sceneEnd);
            if (_outlineOnly || interaction.From is not { } from || interaction.To is not { } to)
            {
                Outline(box, Start(at - PreRoll), Start(at + Ripple));
                return;
            }

            var (x1, y1) = frame.Point(from.X, from.Y);
            var (x2, y2) = frame.Point(to.X, to.Y);
            var (begin, finish) = interaction.Duration > 0
                ? (at, at + interaction.Duration)
                : (at - SwipeTravel, at);
            for (var trail = 3; trail >= 0; trail--)
            {
                var delay = trail * 0.04;
                var start = Start(begin + delay);
                var end = Start(finish + delay + 0.15);
                var travel = Ms(finish - begin);
                var alpha = trail == 0 ? "\\alpha&H00&" : $"\\alpha&H{0x40 + (trail * 0x30):X2}&";
                var radius = _r * (trail == 0 ? 0.5 : 0.4 - (trail * 0.05));
                Shape(start, end, $"\\move({F(x1)},{F(y1)},{F(x2)},{F(y2)},0,{travel}){alpha}\\fad(0,150)", Circle(radius), fill: true);
            }

            var angle = Math.Atan2(-(y2 - y1), x2 - x1) * 180 / Math.PI;
            Shape(Start(finish), Start(finish + 0.35), $"\\pos({F(x2)},{F(y2)})\\org({F(x2)},{F(y2)})\\frz{F(angle)}\\fad(0,150)", Arrow(_r * 1.4), fill: true);
        }

        private void Outline((double X, double Y, double Width, double Height) box, double start, double end)
        {
            var (w, h) = (box.Width + (_stroke * 4), box.Height + (_stroke * 4));
            var drawing = string.Create(CultureInfo.InvariantCulture, $"m 0 0 l {F(w)} 0 l {F(w)} {F(h)} l 0 {F(h)}");
            Shape(start, end, $"\\pos({F(box.X + (box.Width / 2))},{F(box.Y + (box.Height / 2))})\\fad(80,150)", drawing, fill: false, thin: true);
        }

        private void Ring(double start, double end, double x, double y, double radius, string effect)
            => Shape(start, end, $"\\pos({F(x)},{F(y)}){effect}", Circle(radius), fill: false);

        private void Dot(double start, double end, double x, double y, double radius, string effect)
            => Shape(start, end, $"\\pos({F(x)},{F(y)}){effect}", Circle(radius), fill: true);

        // One shape, three layers: a dark halo, a white stroke, the accent on top.
        private void Shape(double start, double end, string placement, string drawing, bool fill, bool thin = false)
        {
            if (end <= start)
            {
                return;
            }

            var stroke = thin ? _stroke * 0.75 : _stroke;
            var fillAlpha = fill ? "&H00&" : "&HFF&";
            // The placement last: its alpha and its animations act on the colours set before it.
            Event(0, start, end, $"{{\\an5\\p1\\1c&H000000&\\1a{(fill ? "&H70&" : "&HFF&")}\\3c&H000000&\\3a&H70&\\bord{F(stroke * 3)}\\shad0{placement}}}{drawing}");
            Event(1, start, end, $"{{\\an5\\p1\\1c&HFFFFFF&\\1a{fillAlpha}\\3c&HFFFFFF&\\bord{F(stroke * 2)}\\shad0{placement}}}{drawing}");
            Event(2, start, end, $"{{\\an5\\p1\\1c{_accent}\\1a{fillAlpha}\\3c{_accent}\\bord{F(stroke)}\\shad0{placement}}}{drawing}");
        }

        // A chip in the bottom-right corner: Enter, Back, Menu.
        private void Chip(double start, double end, string text)
        {
            if (end > start)
            {
                Event(3, start, end, $"{{\\fad(80,150)}}{Escape(text)}", style: "Chip");
            }
        }

        // A chip at the target's top-right corner: the keyboard while typing, "Right click".
        private void Label(double start, double end, (double X, double Y, double Width, double Height) box, string text)
        {
            if (end <= start)
            {
                return;
            }

            var x = Math.Min(_width - 4, box.X + box.Width);
            var y = Math.Max(4, box.Y);
            Event(3, start, end, $"{{\\an3\\pos({F(x)},{F(y)})\\fad(80,150)}}{Escape(text)}", style: "Chip");
        }

        private void Event(int layer, double start, double end, string text, string style = "Mark")
        {
            _events.Append(CultureInfo.InvariantCulture, $"Dialogue: {layer},{Time(start)},{Time(end)},{style},,0,0,0,,{text}\n");
            Lines++;
        }

        private static string KeyText(string? key) => key switch
        {
            "Enter" => "⏎ Enter",
            "Back" => "← Back",
            "Menu" => "☰ Menu",
            "Tab" => "⇥ Tab",
            "Submit" => "⏎ Submit",
            null => "Key",
            _ => key,
        };

        // The style's colour: &H00BBGGRR, opaque.
        private static string Opaque(string accent) => "&H00" + accent[2..^1];

        private static string Escape(string text) => text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("{", "\\{", StringComparison.Ordinal);

        // libass boxes a drawing from (0, 0) to its furthest point, and \an5 puts that box's centre on
        // \pos: so every shape is drawn in positive coordinates, symmetric in its box, to be centred
        // on its point and to scale round it.

        // A circle in a 2r box, from four Bezier arcs.
        private static string Circle(double r)
        {
            var k = r * 0.5523;
            var (a, b, c, d) = (r - k, r + k, 2 * r, r);
            return string.Create(CultureInfo.InvariantCulture,
                $"m {F(d)} 0 b {F(b)} 0 {F(c)} {F(a)} {F(c)} {F(d)} b {F(c)} {F(b)} {F(b)} {F(c)} {F(d)} {F(c)} b {F(a)} {F(c)} 0 {F(b)} 0 {F(d)} b 0 {F(a)} {F(a)} 0 {F(d)} 0");
        }

        // An arrowhead pointing right in a square box; turned with \frz.
        private static string Arrow(double size)
            => string.Create(CultureInfo.InvariantCulture, $"m 0 0 l {F(size)} {F(size / 2)} l 0 {F(size)} l {F(size / 4)} {F(size / 2)}");

        private static int Ms(double seconds) => (int)Math.Round(Math.Max(0, seconds) * 1000);
    }

    private static double Clamp(double time, double start, double end) => Math.Min(Math.Max(time, start), end);

    private static string F(double value) => Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);

    // ASS times: h:mm:ss.cc.
    private static string Time(double seconds)
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return string.Create(CultureInfo.InvariantCulture, $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}");
    }
}
