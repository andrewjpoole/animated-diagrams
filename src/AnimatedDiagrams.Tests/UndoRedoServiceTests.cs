using System;
using System.Collections.Generic;
using System.Linq;
using AnimatedDiagrams.Models;
using AnimatedDiagrams.Services;
using Xunit;

namespace AnimatedDiagrams.Tests;

public class UndoRedoServiceTests
{
    [Fact]
    public void PushState_And_Undo_Works_For_Single_Path()
    {
        var service = new UndoRedoService();
        // Push initial empty state
        var initial = service.SerializeSnapshot(new List<PathItem>(), "initial");
        service.PushState(initial);
        var items = new List<PathItem> { new SvgPathItem { D = "M 0 0 L 10 10" } };
        var snap = service.SerializeSnapshot(items, "draw path");
        service.PushState(snap);

        Assert.True(service.CanUndo);
        var undone = service.Undo(snap);
        Assert.False(string.IsNullOrEmpty(undone), "Undo returned null or empty string. Undo stack may be empty.");
        if (undone == null)
        {
            Assert.Fail($"Undo returned null. Undo stack count: {service.UndoCount}, Redo stack count: {service.RedoCount}");
        }
        var result = service.DeserializeSnapshot(undone!);
        Assert.NotNull(result);
        Assert.Empty(result.Items); // Should be the initial empty state
    }

    [Fact]
    public void Redo_Works_After_Undo()
    {
        var service = new UndoRedoService();
        // Push initial empty state
        var initial = service.SerializeSnapshot(new List<PathItem>(), "initial");
        service.PushState(initial);
    // ...
        var items = new List<PathItem> { new SvgPathItem { D = "M 1 1 L 2 2" } };
        var snap = service.SerializeSnapshot(items, "draw path");
        service.PushState(snap);
    // ...
        // Undo to initial
        var undone = service.Undo(snap);
    // ...
        Assert.False(string.IsNullOrEmpty(undone), "Undo returned null or empty string. Undo stack may be empty.");
        if (undone == null)
        {
            Assert.Fail($"Undo returned null. Undo stack count: {service.UndoCount}, Redo stack count: {service.RedoCount}");
        }
        // Redo: pass the state you just restored from undo as the current state
    var redone = service.Redo(undone);

        Assert.False(string.IsNullOrEmpty(redone), "Redo returned null or empty string. Redo stack may be empty.");
        if (redone == null)
        {
            Assert.Fail($"Redo returned null. Undo stack count: {service.UndoCount}, Redo stack count: {service.RedoCount}");
        }
        var result = service.DeserializeSnapshot(redone!);
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("M 1 1 L 2 2", ((SvgPathItem)result.Items[0]).D);
    }

    private void PrintStackCounts(UndoRedoService service, string label)
    {
    // ...
    }

    [Fact]
    public void PushState_And_Undo_Works_For_Multiple_Types()
    {
        var service = new UndoRedoService();
        // Push initial empty state
        var initial = service.SerializeSnapshot(new List<PathItem>(), "initial");
        service.PushState(initial);
        var items = new List<PathItem>
                {
                    new SvgPathItem { D = "M 0 0 L 10 10" },
                    new SvgCircleItem { Cx = 5, Cy = 5, R = 2 },
                    new PauseHintItem { Milliseconds = 1000 },
                    new SpeedHintItem { Multiplier = 2.0 }
                };
        var snap = service.SerializeSnapshot(items, "complex");
        service.PushState(snap);
        // Undo to initial
        var undone = service.Undo(snap);
        Assert.False(string.IsNullOrEmpty(undone), "Undo returned null or empty string. Undo stack may be empty.");
        if (undone == null)
        {
            Assert.Fail($"Undo returned null. Undo stack count: {service.UndoCount}, Redo stack count: {service.RedoCount}");
        }
        var result = service.DeserializeSnapshot(undone!);
        Assert.NotNull(result);
        Assert.Empty(result.Items); // Should be the initial empty state
    }
}