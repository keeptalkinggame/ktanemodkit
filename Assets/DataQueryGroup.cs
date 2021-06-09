/// <summary>
/// Represents a grouping over a data field, when undefined, the query will not generate groups of data to aggregate on.
/// </summary>
public class DataQueryGroup
{
    /// <summary>
    /// Represents the column to perform grouping on.
    /// </summary>
    public DataRowNoneColumnEnum column = DataRowNoneColumnEnum.None;
}