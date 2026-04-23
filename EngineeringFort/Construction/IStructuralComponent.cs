namespace EngineeringFort.Construction;

readonly record struct GridAxis(string Name);
readonly record struct GridRange(GridAxis Start, GridAxis End);
readonly record struct LevelReference(string Name);
readonly record struct LevelRange(LevelReference Start, LevelReference End);

interface IStructuralComponent
{
    GridRange X { get; }
    GridRange Y { get; }
    LevelRange Z { get; }
}
