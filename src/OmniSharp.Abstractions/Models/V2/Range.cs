namespace OmniSharp.Models.V2;

public record Range(Point Start, Point End)
{
    public bool Contains(int line, int column)
    {
        if (Start.Line > line || End.Line < line)
        {
            return false;
        }

        if (Start.Line == line && Start.Column > column)
        {
            return false;
        }

        if (End.Line == line && End.Column < column)
        {
            return false;
        }

        return true;
    }

    public bool IsValid() => Start is not null && Start.Line > -1 && Start.Column > -1 && End is not null && End.Line > -1 && End.Column > -1;
}
