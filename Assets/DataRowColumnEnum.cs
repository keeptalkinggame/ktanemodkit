/// <summary>
/// Represents the selectable columns from a DataRow.
/// </summary>
public enum DataRowColumnEnum
{
    Min = ColumnA,
    ColumnA = 0,
    ColumnB = 1,
    ColumnC = 2,
    ColumnD = 3,
    ColumnE = 4,
    ColumnF = 5,
    ColumnG = 6,
    Max = ColumnG,
    MaxSimple = ColumnG,
    MaxComplex = ColumnE,
    MaxCruel = ColumnG
}

/// <summary>
/// Represents the selectable columns from a DataRow including a NONE column
/// </summary>
public enum DataRowNoneColumnEnum
{
    None = -1,
    ColumnA = 0,
    ColumnB = 1,
    ColumnC = 2,
    ColumnD = 3,
    ColumnE = 4,
    ColumnF = 5,
    ColumnG = 6
}