using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a query over a DataSet.
/// </summary>
public class DataQuery
{
    /// <summary>
    /// Represents the selection expressions the query will do on the data set.
    /// </summary>
    public readonly IList<DataQuerySelection> selections = new List<DataQuerySelection>();

    /// <summary>
    /// Represents the filters applied on the data set.
    /// </summary>
    public readonly IList<DataQueryFilter> filters = new List<DataQueryFilter>();

    /// <summary>
    /// Represents the group on the data set.
    /// </summary>
    public readonly DataQueryGroup group = new DataQueryGroup();

    /// <summary>
    /// Represents the limitations on the result set.
    /// </summary>
    public readonly DataQueryLimitation limits = new DataQueryLimitation();

}