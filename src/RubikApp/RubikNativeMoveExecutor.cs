using RubikState;

namespace RubikApp;

internal sealed class RubikNativeMoveExecutorFactory : IRubikMoveExecutorFactory
{
    public IRubikMoveExecutor Create(RubikStateDocument state) => new RubikNativeMoveExecutor(state);
}

internal sealed class RubikNativeMoveExecutor : IRubikMoveExecutor
{
    private readonly NativeRubikEngine _engine = new();

    public RubikNativeMoveExecutor(RubikStateDocument state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!_engine.SetSize(state.Size) || !_engine.SetFacelets(state.Faces.Flatten()))
        {
            var error = _engine.GetLastInfo();
            _engine.Dispose();
            throw new InvalidOperationException($"Native verification state was rejected: {error}");
        }
        Size = state.Size;
    }

    public int Size { get; }

    public int[] GetFacelets() => _engine.GetFacelets();

    public bool TryApply(RubikMove move, out string error)
    {
        if (!move.IsValidFor(Size))
        {
            error = "Move is outside the cube bounds.";
            return false;
        }
        if (!_engine.RotateLayer(move.Axis, move.Layer, move.QuarterTurns))
        {
            error = _engine.GetLastInfo();
            return false;
        }
        error = string.Empty;
        return true;
    }

    public void Dispose() => _engine.Dispose();
}
