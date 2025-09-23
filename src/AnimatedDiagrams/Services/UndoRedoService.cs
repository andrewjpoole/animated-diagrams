namespace AnimatedDiagrams.Services;

public class UndoRedoService
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void PushState(string serialized)
    {
        _undo.Push(serialized);
        _redo.Clear();
    }

    public string? Undo(string current)
    {
        if (!CanUndo) return null;
        _redo.Push(current);
        return _undo.Pop();
    }

    public string? Redo(string current)
    {
        if (!CanRedo) return null;
        _undo.Push(current);
        return _redo.Pop();
    }
}