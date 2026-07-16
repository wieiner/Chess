namespace RubikState;

public enum RubikValidationLevel
{
    Failed,
    BasicCounts,
    CubieInventory,
    InventoryAndOrientation,
    FullSmallCube
}

public sealed record RubikSolvabilityResult(
    int Size,
    bool BasicCountsValid,
    bool CubieInventoryValid,
    bool OrientationValid,
    bool OrientationProven,
    bool PermutationValid,
    bool ParityValid,
    bool ParityProven,
    bool SolverReady,
    RubikValidationLevel ValidationLevel,
    IReadOnlyList<RubikValidationIssue> Issues);

public static class RubikSolvabilityValidator
{
    public static RubikSolvabilityResult Validate(RubikStateDocument document)
    {
        var basic = RubikStateValidator.Validate(document);
        if (!basic.IsValid)
            return new(document.Size, false, false, false, false, false, false, false, false,
                RubikValidationLevel.Failed, basic.Issues.Select(issue => new RubikValidationIssue(
                    RubikValidationSeverity.Error, issue.Code.ToString(), null, null, null, null,
                    issue.Message, "Fix basic state validation first.")).ToArray());

        var decomposition = RubikCubieDecomposer.Decompose(document);
        if (!decomposition.Complete)
            return new(document.Size, true, false, false, false, false, false, false, false,
                RubikValidationLevel.BasicCounts, decomposition.Issues);

        var issues = new List<RubikValidationIssue>();
        var centerFrameCanonical = document.Size == 2 || document.Size % 2 == 1 && HasCanonicalCenters(document);
        var orientationProven = document.Size <= 3 && centerFrameCanonical;
        var orientationValid = true;
        if (orientationProven)
        {
            var cornerOrientationValid = decomposition.Corners.Sum(corner => corner.Orientation) % 3 == 0;
            if (!cornerOrientationValid)
                issues.Add(Invariant("cornerOrientation", "Corner orientation sum is not divisible by three.",
                    "Correct the twisted corner; one corner cannot be twisted alone."));
            var edgeOrientationValid = decomposition.Wings.Count(wing => wing.Flipped) % 2 == 0;
            if (!edgeOrientationValid)
                issues.Add(Invariant("edgeOrientation", "Observed edge flip sum is odd.",
                    "Correct the single flipped edge observation."));
            orientationValid = cornerOrientationValid && edgeOrientationValid;
        }
        else
        {
            issues.Add(new(RubikValidationSeverity.Warning, "orientationFrameUnproven", null, null, null, null,
                "The fixed orientation frame is not canonical/proved for this NxN state.",
                "Normalize center orientation or use a future NxN reduction validator before solving."));
        }

        var permutationValid = true;
        var parityValid = true;
        var parityProven = document.Size == 2 || document.Size == 3 && centerFrameCanonical;
        if (document.Size == 3 && parityProven)
        {
            var solved = RubikCubieDecomposer.Decompose(SolvedDocument(3));
            var cornerPermutation = BuildPermutation(decomposition.Corners.Select(piece => piece.ColorSignature),
                solved.Corners.Select(piece => piece.ColorSignature));
            var edgePermutation = BuildPermutation(decomposition.Wings.Select(piece => piece.ColorSignature),
                solved.Wings.Select(piece => piece.ColorSignature));
            permutationValid = cornerPermutation is not null && edgePermutation is not null;
            parityValid = permutationValid && PermutationParity(cornerPermutation!) == PermutationParity(edgePermutation!);
            if (!parityValid)
                issues.Add(Invariant("permutationParity", "Corner and edge permutation parity do not match.",
                    "Check for a two-piece swap that cannot be produced by legal 3x3 turns."));
        }
        else if (document.Size > 3)
        {
            issues.Add(new(RubikValidationSeverity.Warning, "nxnParityUnproven", null, null, null, null,
                "Wing and center inventory is valid, but full NxN permutation/parity proof is not implemented.",
                "Treat this state as inventory-valid, not solver-ready."));
        }

        var solverReady = document.Size <= 3 && orientationProven && parityProven && orientationValid && permutationValid && parityValid;
        var level = solverReady ? RubikValidationLevel.FullSmallCube :
            orientationProven && orientationValid ? RubikValidationLevel.InventoryAndOrientation : RubikValidationLevel.CubieInventory;
        return new(document.Size, true, true, orientationValid, orientationProven, permutationValid, parityValid,
            parityProven, solverReady, level, issues);
    }

    private static int[]? BuildPermutation(IEnumerable<string> actual, IEnumerable<string> solved)
    {
        var solvedList = solved.ToArray();
        var index = solvedList.Select((signature, position) => (signature, position))
            .GroupBy(item => item.signature, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(item => item.position).ToQueue(), StringComparer.Ordinal);
        var result = new List<int>();
        foreach (var signature in actual)
        {
            if (!index.TryGetValue(signature, out var positions) || positions.Count == 0) return null;
            result.Add(positions.Dequeue());
        }
        return result.ToArray();
    }

    private static int PermutationParity(IReadOnlyList<int> permutation)
    {
        var parity = 0;
        for (var left = 0; left < permutation.Count; left++)
        for (var right = left + 1; right < permutation.Count; right++)
            if (permutation[left] > permutation[right]) parity ^= 1;
        return parity;
    }

    private static RubikStateDocument SolvedDocument(int size)
    {
        var area = size * size;
        return RubikStateDocument.Create(size,
            Enumerable.Range(1, 6).SelectMany(color => Enumerable.Repeat(color, area)).ToArray());
    }

    private static bool HasCanonicalCenters(RubikStateDocument document)
    {
        var middle = document.Size / 2;
        var index = middle * document.Size + middle;
        return document.Faces.InFaceOrder().Select((face, faceIndex) => face.Value[index] == faceIndex + 1).All(value => value);
    }

    private static RubikValidationIssue Invariant(string code, string message, string action) =>
        new(RubikValidationSeverity.Error, code, null, null, null, null, message, action);

    private static Queue<T> ToQueue<T>(this IEnumerable<T> values) => new(values);
}
