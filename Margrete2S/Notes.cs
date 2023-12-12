namespace Margrete2S.Nodes;

public abstract class Node
{
    public int Tick { get; set; }

    public abstract string Id { get; }

    public virtual string Text => Id + "\t" + ConvertTick(Tick);

    public int LineNumber { get; set; }

    public static string ConvertTick(int tick)
    {
        return $"{tick / 1920}\t{(int)Math.Round(tick % 1920 / 5f)}";
    }

    public static string ConvertLength(int tick)
    {
        return $"{(int)Math.Round(tick / 5f)}";
    }
}

public class Bpm : Node
{
    public float Value { get; set; }

    public override string Id => "BPM";

    public override string Text => $"{base.Text}\t{Value}";
}

public class Met : Node
{
    public int Denominator { get; set; }

    public int Numerator { get; set; }

    public override string Id => "MET";

    public override string Text => $"{base.Text}\t{Numerator}\t{Denominator}";
}

public class Soflan : Node
{
    public int Length { get; set; }

    public float Speed { get; set; }

    public override string Id => "SFL";

    public override string Text => $"{base.Text}\t{ConvertLength(Length)}\t{Speed:F6}";
}

public abstract class Note : Node
{
    private static float _groundHeight;

    public int Lane { get; set; }

    public int Width { get; set; }

    public override string Text => $"{base.Text}\t{Lane}\t{Width}";

    public static void SetGroundHeight(float height)
    {
        _groundHeight = height;
    }

    public static float TransformHeight(float input)
    {
        return (float)Math.Max((double)input * 0.625, _groundHeight);
    }
}

public interface IHoldable
{
    int Length { get; set; }
}

public interface IExTapable
{
    bool IsEx { get; set; }

    string ExEffectType { get; set; }
}

public enum Color
{
    DEF = 0,
    RED = 1,
    ORN = 2,
    YEL = 3,
    GRN = 4,
    CYN = 5,
    BLU = 6,
    PPL = 7,
    PNK = 8,
    VLT = 9,
    GRY = 10,
    BLK = 11,
    NON = 35
}

public class Air : Note
{
    public enum Dir
    {
        IR,
        UL,
        UR,
        DW,
        DL,
        DR
    }

    public override string Id => $"A{Direction}";

    public Dir Direction { get; set; }

    public string? Parent { get; set; }

    public Color Color { get; set; }

    public override string Text => $"{base.Text}\t{Parent!}\t{Color}";
}
public class AirHold : Note, IHoldable
{
    public bool EndVisible { get; set; }

    public int Length { get; set; }

    public Color Color { get; set; }

    public string? Parent { get; set; }

    public override string Id => $"AH{(EndVisible ? 'D' : 'X')}";

    public override string Text => $"{base.Text}\t{Parent!}\t{ConvertLength(Length)}\t{Color}";
}

public class AirSlide : Note, IHoldable
{
    public bool IsVisible { get; set; }

    public int Length { get; set; }

    public float Height { get; set; }

    public float EndHeight { get; set; }

    public int EndLane { get; set; }

    public int EndWidth { get; set; }

    public Color Color { get; set; }

    public string? Parent { get; set; }

    public override string Id => $"AS{(IsVisible ? 'D' : 'C')}";

    public override string Text => $"{base.Text}\t{Parent!}\t{TransformHeight(Height)}\t{ConvertLength(Length)}\t{EndLane}\t{EndWidth}\t{TransformHeight(EndHeight)}\t{Color}";
}

public class Crush : Note, IHoldable
{
    public string? Type { get; set; }

    public string? Type2 { get; set; }

    public int Length { get; set; }

    public float Height { get; set; }

    public float EndHeight { get; set; }

    public int EndLane { get; set; }

    public int EndWidth { get; set; }

    public int Dense { get; set; }

    public Color Color { get; set; }

    public override string Id => "ALD";

    public override string Text => $"{base.Text}\t{ConvertLength(Dense)}\t{TransformHeight(Height)}\t{ConvertLength(Length)}\t{EndLane}\t{EndWidth}\t{TransformHeight(EndHeight)}\t{Color}";
}

public class Damage : Note
{
    public override string Id => "MNE";
}

public class ExTap : Note
{
    public string ExEffectType { get; set; } = "UP";


    public override string Id => "CHR";

    public override string Text => base.Text + "\t" + ExEffectType;
}

public class Flick : Note
{
    public override string Id => "FLK";
}

public class Hold : Note, IHoldable, IExTapable
{
    public bool IsEx { get; set; }

    public string ExEffectType { get; set; } = "UP";

    public int Length { get; set; }

    public override string Id => $"H{(IsEx ? 'X' : 'L')}D";

    public override string Text => $"{base.Text}\t{ConvertLength(Length)}{(IsEx ? $"\t{ExEffectType}" : "")}";
}

public class Slide : Note, IHoldable, IExTapable
{
    public int Length { get; set; }

    public bool IsEx { get; set; }

    public string ExEffectType { get; set; } = "UP";


    public bool IsVisible { get; set; }

    public bool IsCurved { get; set; }

    public int EndLane { get; set; }

    public int EndWidth { get; set; }

    public Note? Parent { get; set; }

    public Note? Previous { get; set; }

    public virtual char Prefix => 'S';

    public override string Id => $"S{(IsEx ? 'X' : 'L')}{(IsVisible ? 'D' : 'C')}";

    public override string Text => $"{base.Text}\t{ConvertLength(Length)}\t{EndLane}\t{EndWidth}\tSLD{(IsEx ? $"\t{ExEffectType}" : "")}";
}

public class Tap : Note
{
    public override string Id => "TAP";
}
