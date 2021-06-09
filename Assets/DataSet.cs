using System.Collections.Generic;

/// <summary>
/// Represents a dataset that the module will use either as a datasource or an expected result.
/// 
/// The DataSet is functional with Linq which is how we'll create the expected DataSet.
/// </summary>
public class DataSet
{

    /// <summary>
    /// Contains all rows in the dataset
    /// </summary>
    public IList<DataRow> rows = new List<DataRow>();

    /// <summary>
    /// Adds a row to the dataset
    /// </summary>
    /// <param name="row">Row of data to add to the dataset</param>
    public void AddRow(DataRow row)
    {
        rows.Add(row);
    }
}