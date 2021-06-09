/// <summary>
/// Represents a selection over a data query, encapsulates a column reference and a potential aggregate function.
/// </summary>
public class DataQuerySelection
{
    /// <summary>
    /// Represents the column to perform selection on.
    /// </summary>
    public DataRowColumnEnum column;

    /// <summary>
    /// Represents the filters applied on the data set. None by default.
    /// </summary>
    public DataQueryAggregatorEnum aggregator = DataQueryAggregatorEnum.None;

    /// <summary>
    /// Simple column selector
    /// </summary>
    /// <param name="column">Column to select</param>
    public DataQuerySelection(DataRowColumnEnum column)
    {
        this.column = column;
    }

    /// <summary>
    /// Aggregated column selector
    /// </summary>
    /// <param name="column">Column to select</param>
    /// <param name="aggregator">Aggregator to apply</param>
    public DataQuerySelection(DataRowColumnEnum column, DataQueryAggregatorEnum aggregator)
    {
        this.column = column;
        this.aggregator = aggregator;
    }

}