using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to create DataSets from existing data or random data at your convenience.
/// </summary>
public class DataSetFactory
{

    /// <summary>
    /// First matrix set of values to use in the game
    /// </summary>
    public static List<int>[] dataSetMatrix1 = new List<int>[] {
        new List<int>() { 1, 2, 3, 4, 5, 6, 7 },
        new List<int>() { 2, 3, 4, 5, 6, 7, 1 },
        new List<int>() { 3, 4, 5, 6, 7, 1, 2 },
        new List<int>() { 4, 5, 6, 7, 1, 2, 3 },
        new List<int>() { 5, 6, 7, 1, 2, 3, 4 },
        new List<int>() { 6, 7, 1, 2, 3, 4, 5 }
    };

    /// <summary>
    /// Generates a DataSet from a int matrix, used in testing but also in gameplay to load from static datasets.
    /// </summary>
    /// <param name="matrix">The 2D int matrix to load into a dataset</param>
    /// <returns>A dataset with data in it</returns>
    public static DataSet FromIntMatrix(List<int>[] matrix)
    {
        DataSet result = new DataSet();
        foreach (List<int> matrixRow in matrix)
        {
            // Copy each list before sending it in so we don't get reference modification errors later by error
            result.AddRow(new DataRow(new List<int>(matrixRow.ToArray())));
        }
        return result;
    }

    /// <summary>
    /// Generates a completely random dataset.
    /// </summary>
    /// <param name="rows">The number of rows to generate, defaults to 7.</param>
    /// <returns>A dataset with data in it</returns>
    public static DataSet FromRandomData(int rows = 7)
    {
        DataSet result = new DataSet();
        for (int iDataRow = 0; iDataRow < rows; iDataRow++)
        {
            result.AddRow(new DataRow(new List<int>()
            {
                Mathf.RoundToInt(Random.Range(0, 9)), // Column A
                Mathf.RoundToInt(Random.Range(0, 9)), // Column B
                Mathf.RoundToInt(Random.Range(0, 9)), // Column C
                Mathf.RoundToInt(Random.Range(0, 9)), // Column D
                Mathf.RoundToInt(Random.Range(0, 9)), // Column E
                Mathf.RoundToInt(Random.Range(0, 9)), // Column F
                Mathf.RoundToInt(Random.Range(0, 9)) // Column G
            }));
        }
        return result;
    }
}