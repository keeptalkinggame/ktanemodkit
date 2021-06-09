using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a row of data in a DataSet.
/// </summary>
public class DataRow
{
    /// <summary>
    /// Values for the datarow
    /// </summary>
    protected readonly IList<int> values = new List<int>();

    /// <summary>
    /// Constructor for the DataRow, accepts values that the SQL Module will play with.
    /// </summary>
    /// <param name="values">Values for the row, values must be provided in the same order as they must match as exposed columns.</param>
    public DataRow(IList<int> values)
    {
        if (values.Count() < (int)DataRowColumnEnum.Max)
        {
            throw new System.FormatException("You must provide all columns to be contained in the DataRow up to a max of " + (int)DataRowColumnEnum.Max);
        }
        this.values = values.ToList();
    }

    /// <summary>
    /// Returns the value matching the requested column.
    /// </summary>
    /// <param name="column">The column to retrieve.</param>
    /// <returns>The value associated to that column in the row.</returns>
    public int GetValueByColumn(DataRowColumnEnum column)
    {
        return values[(int)column];
    }
}