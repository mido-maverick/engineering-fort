namespace EngineeringFort.Construction;

public readonly record struct GridAxis(string Name);
public readonly record struct GridRange(GridAxis Start, GridAxis End);
public readonly record struct LevelReference(string Name);
public readonly record struct LevelRange(LevelReference Start, LevelReference End);

interface IStructuralComponent
{
    GridRange X { get; }
    GridRange Y { get; }
    LevelRange Z { get; }
}
