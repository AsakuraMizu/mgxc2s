using System.Text;
using Margrete2S.Nodes;

namespace Margrete2S.Parser;

public class ChartMeta
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public string? Designer { get; set; }

    public int Difficulty { get; set; }

    public string? Level { get; set; }

    public string? AudioFile { get; set; }

    public float PreviewStart { get; set; }

    public float PreviewEnd { get; set; }

    public string? JacketFile { get; set; }

    public float Bpm { get; set; }
}

internal class NoteContext
{
    public required Note Note { get; set; }

    public List<NoteContext> ChildContexts { get; set; } = [];

}

public class MgxcParser
{
    private bool _useNewExhold;

    private NoteContext? _currentTopContext;

    private readonly List<NoteContext> _contexts = [];

    private readonly Action<string[], int>? _currentParser;

    private Soflan? _lastSoflan;

    private Drop? _lastDrop;

    private readonly float _groundHeight;

    private int _currentLine;

    private readonly List<Node> _notes = [];

    private readonly List<Node> _allNotes = [];

    private readonly List<Node> _headers = [];

    private readonly ChartMeta _meta = new();

    public string ErrorType = "";

    public int ErrorLine;

    public int NoteCount => _allNotes.Count;

    public int TapCount => _allNotes.Count((Node v) => v is Tap);

    public int ExTapCount => _allNotes.Count((Node v) => v is ExTap);

    public int FlickCount => _allNotes.Count((Node v) => v is Flick);

    public int DamageCount => _allNotes.Count((Node v) => v is Damage);

    public int AirCount => _allNotes.Count((Node v) => v is Air);

    public int HoldCount => _allNotes.Count((Node v) => v is Tap);

    public int SlideCount => _allNotes.Count((Node v) => v is Slide);

    public int AirHoldCount => _allNotes.Count((Node v) => v is AirHold);

    public int AirSlideCount => _allNotes.Count((Node v) => v is AirSlide);

    public int CrushCount => _allNotes.Count((Node v) => v is Crush);

    public Exception? Error { get; private set; }

    public MgxcParser(string mgxc, float groundHeight)
    {
        _groundHeight = groundHeight;
        Note.SetGroundHeight(groundHeight);
        string[] array = mgxc.Split('\n');
        _currentLine = 0;
        string[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            string[] array3 = array2[i].Split('\t');
            _currentLine++;
            if (array3[0] == "BEGIN")
            {
                Console.WriteLine("Parsing " + array3[1]);
                switch (array3[1])
                {
                    case "META":
                        _currentParser = ParseMeta;
                        break;
                    case "HEADER":
                        _currentParser = ParseHeader;
                        break;
                    case "NOTES":
                        _currentParser = ParseNotes;
                        break;
                    default:
                        Console.WriteLine("Can not parse " + array3[1]);
                        _currentParser = null;
                        break;
                }
            }
            else
            {
                try
                {
                    _currentParser?.Invoke(array3, _currentLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== Parse Error ===");
                    Console.WriteLine($"Line: {_currentLine}");
                    Error = ex;
                    ErrorType = "Parse Error";
                    ErrorLine = _currentLine;
                    throw;
                }
            }
        }
        try
        {
            DoPostProcess();
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== Post Process Error ===");
            Console.WriteLine($"Line: {_currentLine}");
            Error = ex;
            ErrorType = "Post Process Error";
            ErrorLine = _currentLine;
            throw;
        }
    }

    private void DoPostProcess()
    {
        if (_currentTopContext != null)
        {
            _contexts.Add(_currentTopContext);
        }
        _contexts.Sort(delegate (NoteContext a, NoteContext b)
        {
            if (a.Note is Slide && b.Note is not Slide)
            {
                return 1;
            }
            return (a.Note is not Slide && b.Note is Slide) ? (-1) : (a.Note.Tick - b.Note.Tick);
        });
        if (!_useNewExhold)
        {
            Console.WriteLine("Warning: Ex Hold should always be enabled; auto-fixed");
        }
        foreach (NoteContext extap in _contexts.Where((NoteContext v) => v.Note is ExTap && v.ChildContexts.Count == 0).ToList())
        {
            _currentLine = extap.Note.LineNumber;
            List<IExTapable> list = _contexts.Where((NoteContext v) => v.Note.Tick == extap.Note.Tick && v.Note.Lane == extap.Note.Lane && v.Note.Width == extap.Note.Width && v.Note is IExTapable n && !n.IsEx).Select((NoteContext v) => (IExTapable)v.Note).ToList();
            foreach (IExTapable item in list)
            {
                item.IsEx = true;
                item.ExEffectType = ((ExTap)extap.Note).ExEffectType;
            }
            if (list.Count != 0)
            {
                _contexts.Remove(extap);
            }
        }
        foreach (NoteContext context in _contexts)
        {
            ProcessContext(context);
            _notes.Sort((Node a, Node b) => a.Tick - b.Tick);
            _allNotes.AddRange(_notes.Where((Node v) => v is not IHoldable holdable || holdable.Length > 0));
            _notes.Clear();
        }
        if (_lastSoflan != null || _lastDrop != null)
        {
            Node node = _allNotes.OrderByDescending((Node v) => v.Tick).FirstOrDefault() ?? throw new IndexOutOfRangeException("No notes?");
            if (_lastSoflan != null)
            {
                _currentLine = _lastSoflan.LineNumber;
                _lastSoflan.Length = node.Tick - _lastSoflan.Tick;
            }
            if (_lastDrop != null)
            {
                _currentLine = _lastDrop.LineNumber;
                _lastDrop.Length = node.Tick - _lastDrop.Tick;
            }
        }
        _headers.RemoveAll((Node node) => node is Soflan soflan && soflan.Speed == 1 || node is Drop drop && drop.Speed == 1);
    }

    private void ParseMeta(string[] tokens, int lineNumber)
    {
        if (tokens.Length < 2)
        {
            return;
        }
        string text = tokens[0];
        string text2 = tokens[1];
        switch (text)
        {
            case "TITLE":
                _meta.Title = text2;
                break;
            case "ARTIST":
                _meta.Artist = text2;
                break;
            case "DESIGNER":
                _meta.Designer = text2;
                break;
            case "DIFFICULTY":
                _meta.Difficulty = int.Parse(text2);
                break;
            case "PLAYLEVEL":
                _meta.Level = text2;
                break;
            case "SONGID":
                _meta.Id = text2;
                break;
            case "BGM":
                _meta.AudioFile = text2;
                break;
            case "BGMOFFSET":
                if (float.Parse(text2) != 0f)
                {
                    throw new InvalidDataException("Non-Zero BGM offset is not supported!");
                }
                break;
            case "JACKET":
                _meta.JacketFile = text2;
                break;
            case "MAINBPM":
                _meta.Bpm = float.Parse(text2);
                break;
            case "EXLONG":
                _useNewExhold = int.Parse(text2) != 0;
                break;
        }
    }

    private void ParseHeader(string[] tokens, int lineNumber)
    {
        string text = tokens[0];
        string? text2 = tokens.ElementAtOrDefault(1);
        string? text3 = tokens.ElementAtOrDefault(2);
        string? text4 = tokens.ElementAtOrDefault(3);
        Node node = text switch
        {
            "BPM" => new Bpm
            {
                Tick = int.Parse(text2 ?? throw new ArgumentNullException("arg1 is reqired")),
                Value = float.Parse(text3 ?? throw new ArgumentNullException("arg2 is reqired")),
                LineNumber = lineNumber
            },
            "BEAT" => new Met
            {
                Tick = int.Parse(text2 ?? throw new ArgumentNullException("arg1 is reqired")) * 1920,
                Denominator = int.Parse(text3 ?? throw new ArgumentNullException("arg2 is reqired")),
                Numerator = int.Parse(text4 ?? throw new ArgumentNullException("arg3 is reqired")),
                LineNumber = lineNumber
            },
            "TIL" => new Soflan
            {
                Tick = int.Parse(text3 ?? throw new ArgumentNullException("arg2 is reqired")),
                Speed = float.Parse(text4 ?? throw new ArgumentNullException("arg3 is reqired")),
                LineNumber = lineNumber
            },
            "SPDMOD" => new Drop
            {
                Tick = int.Parse(text2 ?? throw new ArgumentNullException("arg2 is reqired")),
                Speed = float.Parse(text3 ?? throw new ArgumentNullException("arg3 is reqired")),
                LineNumber = lineNumber
            },
            _ => throw new NotImplementedException("Unknown node type " + text),
        };
        if (node is Soflan soflan)
        {
            if (_lastSoflan != null)
            {
                _lastSoflan.Length = soflan.Tick - _lastSoflan.Tick;
            }
            _lastSoflan = soflan;
        }
        if (node is Drop drop)
        {
            if (_lastDrop != null)
            {
                _lastDrop.Length = drop.Tick - _lastDrop.Tick;
            }
            _lastDrop = drop;
        }
        _headers.Add(node);
    }

    private static Air.Dir ParseDir(string dir)
    {
        return dir switch
        {
            "U" => Air.Dir.IR,
            "UL" => Air.Dir.UL,
            "UR" => Air.Dir.UR,
            "D" => Air.Dir.DW,
            "DL" => Air.Dir.DL,
            "DR" => Air.Dir.DR,
            _ => Air.Dir.IR,
        };
    }

    private static Color ParseColor(string color)
    {
        return color switch
        {
            "0" => Color.DEF,
            "1" => Color.RED,
            "2" => Color.ORN,
            "3" => Color.YEL,
            "4" => Color.GRN,
            "5" => Color.CYN,
            "6" => Color.BLU,
            "7" => Color.PPL,
            "8" => Color.PNK,
            "9" => Color.VLT,
            "10" => Color.GRY,
            "11" => Color.BLK,
            "35" => Color.NON,
            _ => throw new ArgumentOutOfRangeException(nameof(color), "Color out of range"),
        };
    }

    private static ExEffectType ParseExEffectType(string type)
    {
        return type switch
        {
            "U" => ExEffectType.UP,
            "D" => ExEffectType.DW,
            "C" => ExEffectType.CE,
            "L" => ExEffectType.LS,
            "R" => ExEffectType.RS,
            "RL" => ExEffectType.LC,
            "RR" => ExEffectType.RC,
            "IO" => ExEffectType.BS,
            "OI" => ExEffectType.BS,
            _ => ExEffectType.UP,
        };
    }

    private void ParseNotes(string[] tokens, int lineNumber)
    {
        if (tokens.Length != 10)
        {
            return;
        }
        string type = tokens[0];
        int num = type.Count((char v) => v == '.');
        NoteContext? noteContext = _currentTopContext;
        NoteContext? noteContext2 = null;
        for (int i = 0; i < num; i++)
        {
            if (noteContext == null)
            {
                throw new InvalidDataException($"Note requires context depth {i} but context is not exists");
            }
            noteContext2 = noteContext;
            noteContext = noteContext.ChildContexts.LastOrDefault();
        }
        if (num == 0 && noteContext != null)
        {
            _contexts.Add(noteContext);
            noteContext = null;
            _currentTopContext = null;
        }
        string seqType = tokens[1];
        string dirOrExEffectType = tokens[2];
        string crushType = tokens[3];
        int tick = int.Parse(tokens[4]) / 5 * 5;
        int lane = int.Parse(tokens[5]);
        int width = int.Parse(tokens[6]);
        int height = int.Parse(tokens[7]);
        Color color = ParseColor(tokens[9]);
        type = type.Replace(".", "");
        Note note = type switch
        {
            "t" => new Tap
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                LineNumber = lineNumber
            },
            "e" => new ExTap
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                ExEffectType = ParseExEffectType(dirOrExEffectType),
                LineNumber = lineNumber
            },
            "f" => new Flick
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                LineNumber = lineNumber
            },
            "d" => new Damage
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                LineNumber = lineNumber
            },
            "h" => new Hold
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                LineNumber = lineNumber
            },
            "s" => new Slide
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                IsVisible = seqType != "LC" && seqType != "CC",
                IsCurved = seqType == "CC",
                LineNumber = lineNumber
            },
            "a" => new Air
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                Direction = ParseDir(dirOrExEffectType),
                LineNumber = lineNumber
            },
            "H" => new AirHold
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                EndVisible = true,
                LineNumber = lineNumber
            },
            "S" => new AirSlide
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                Height = height,
                IsVisible = seqType != "LC" && seqType != "EX",
                Color = color,
                LineNumber = lineNumber
            },
            "C" => new Crush
            {
                Lane = lane,
                Tick = tick,
                Width = width,
                Height = height,
                Type = seqType,
                Type2 = crushType,
                Color = color,
                LineNumber = lineNumber
            },
            _ => throw new NotImplementedException("Unknown note type " + type),
        };
        NoteContext noteContext3 = new()
        {
            Note = note
        };
        if (noteContext2 == null)
        {
            _currentTopContext = noteContext3;
        }
        else
        {
            noteContext2.ChildContexts.Add(noteContext3);
        }
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("VERSION\t1.11.00\t1.11.00");
        stringBuilder.AppendLine("MUSIC\t0");
        stringBuilder.AppendLine("SEQUENCEID\t0");
        stringBuilder.AppendLine($"DIFFICULT\t{_meta.Difficulty:00}");
        stringBuilder.AppendLine("LEVEL\t0.0");
        stringBuilder.AppendLine($"CREATOR\t{_meta.Designer}");
        stringBuilder.AppendLine($"BPM_DEF\t{_meta.Bpm:F3}\t{_meta.Bpm:F3}\t{_meta.Bpm:F3}\t{_meta.Bpm:F3}");
        stringBuilder.AppendLine("MET_DEF\t4\t4");
        stringBuilder.AppendLine("RESOLUTION\t384");
        stringBuilder.AppendLine("CLK_DEF\t384");
        stringBuilder.AppendLine("PROGJUDGE_BPM\t240.000");
        stringBuilder.AppendLine("PROGJUDGE_AER\t  0.999");
        stringBuilder.AppendLine("TUTORIAL\t0");
        stringBuilder.AppendLine("");
        foreach (Node header in _headers)
        {
            _currentLine = header.LineNumber;
            try
            {
                stringBuilder.AppendLine(header.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== Write Header Error ===");
                Console.WriteLine($"Line: {_currentLine}");
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Stack: \n" + ex.StackTrace);
            }
        }
        stringBuilder.AppendLine("");
        foreach (Node allNote in _allNotes)
        {
            _currentLine = allNote.LineNumber;
            try
            {
                stringBuilder.AppendLine(allNote.Text);
            }
            catch (Exception ex2)
            {
                Console.WriteLine("=== Write Note Error ===");
                Console.WriteLine($"Line: {_currentLine}");
                Console.WriteLine("Error: " + ex2.Message);
                Console.WriteLine("Stack: \n" + ex2.StackTrace);
            }
        }
        return stringBuilder.ToString();
    }
    private void ProcessCrush(NoteContext ctx)
    {
        Crush crush = (Crush)ctx.Note;
        int num = 0;
        foreach (NoteContext childContext in ctx.ChildContexts)
        {
            if (childContext.Note is not Crush crush2)
            {
                throw new Exception("Non crush note in child of crush?");
            }
            crush2.Color = crush.Color;
            if (crush2.Type == "ST" && crush.Type2 == "N")
            {
                crush.EndLane = crush2.Lane;
                crush.EndWidth = crush2.Width;
                crush.EndHeight = crush2.Height;
                crush.Length = crush2.Tick - crush.Tick;
                crush.Dense = 0;
                _notes.Add(crush);
                crush = crush2;
                num = 0;
                crush.Type = "BG";
                crush.Type2 = "AT";
                continue;
            }
            if (crush2.Type == "ST")
            {
                num++;
                continue;
            }
            crush.EndLane = crush2.Lane;
            crush.EndWidth = crush2.Width;
            crush.EndHeight = crush2.Height;
            crush.Length = crush2.Tick - crush.Tick;
            if (crush.Type2 == "N")
            {
                crush.Dense = 0;
            }
            else
            {
                crush.Dense = crush.Length / (num + 1);
            }
            _notes.Add(crush);
            if (crush2.Type2 == "AT" && crush.Dense == 0)
            {
                _notes.Add(new Crush
                {
                    Tick = crush.Tick + crush.Length,
                    Color = crush.Color,
                    Lane = crush.EndLane,
                    Width = crush.EndWidth,
                    Height = crush.EndHeight,
                    EndHeight = crush.EndHeight,
                    EndLane = crush.EndLane,
                    EndWidth = crush.EndWidth,
                    Dense = 1000,
                    Length = 5
                });
            }
            else if (crush.Dense != 0 && crush2.Type2 == "N")
            {
                crush.Length -= 5;
                _notes.Add(new Crush
                {
                    Tick = crush.Tick + crush.Length,
                    Color = crush.Color,
                    Dense = 0,
                    Lane = crush.EndLane,
                    Width = crush.EndWidth,
                    Height = crush.EndHeight,
                    Length = 5,
                    EndHeight = crush.EndHeight,
                    EndLane = crush.EndLane,
                    EndWidth = crush.EndWidth
                });
            }
            crush = crush2;
            num = 0;
        }
    }

    private void ProcessContext(NoteContext context, NoteContext? parent = null, Note? previous = null)
    {
        _currentLine = context.Note.LineNumber;
        if (context.Note is Crush)
        {
            ProcessCrush(context);
            return;
        }
        Note note = context.Note;
        _notes.Add(note);
        if (parent != null)
        {
            if (previous is Hold hold && note is Hold hold2)
            {
                hold.Length = hold2.Tick - hold.Tick;
            }
            else if (previous is Air air && note is AirHold airHold)
            {
                _notes.Remove(previous);
                airHold.Parent = air.Parent;
            }
            else if (parent.Note is not AirHold && note is AirHold airHold2)
            {
                airHold2.Parent = parent.Note.Id;
            }
            else if (previous is AirHold airHold3 && note is AirHold airHold4)
            {
                airHold3.Length = airHold4.Tick - airHold3.Tick;
                airHold4.Parent = previous.Id;
            }
            else
            {
                if (previous is Slide parentSlide && note is Slide slide)
                {
                    if (parentSlide.IsCurved && !slide.IsCurved)
                    {
                        throw new Exception("no curved slide");
                        // ProcessCurvedSlide(ref parentSlide, slide, parent);
                    }
                    parentSlide.Length = slide.Tick - parentSlide.Tick;
                    parentSlide.EndLane = slide.Lane;
                    parentSlide.EndWidth = slide.Width;
                    parentSlide.IsVisible = slide.IsVisible;
                    slide.Parent = parent.Note;
                    slide.Previous = previous;
                }
                else if (parent.Note is Air air2 && note is AirSlide airSlide)
                {
                    if (previous != null)
                    {
                        _notes.Remove(previous);
                    }
                    airSlide.Parent = air2.Parent;
                }
                else if (previous is AirSlide airSlide2 && note is AirSlide airSlide3)
                {
                    airSlide2.Length = airSlide3.Tick - airSlide2.Tick;
                    airSlide2.EndLane = airSlide3.Lane;
                    airSlide2.EndWidth = airSlide3.Width;
                    airSlide2.EndHeight = airSlide3.Height;
                    if (context.ChildContexts.Count == 0)
                    {
                        airSlide2.IsVisible = airSlide3.IsVisible;
                    }
                    airSlide3.Parent = previous.Id;
                }
                else if (note is Air air3)
                {
                    air3.Parent = parent.Note.Id;
                }
            }
        }
        Note previous2 = note;
        foreach (NoteContext childContext in context.ChildContexts)
        {
            ProcessContext(childContext, context, previous2);
            previous2 = childContext.Note;
        }
    }
}
