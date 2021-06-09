/// <summary>
/// Represents a filter over a data row, encapsulates up to two column references or 1 column reference and a scalar value.
/// </summary>
public class DataQueryFilter
{
    /// <summary>
    /// Represents the left operand to perform filter on.
    /// </summary>
    public readonly DataRowColumnEnum leftOperandColumn;

    /// <summary>
    /// Represents the right operand to perform filter on.
    /// </summary>
    public readonly DataRowColumnEnum rightOperandColumn;

    /// <summary>
    /// Represents the right operand to perform filter on when we have a value.
    /// </summary>
    public readonly int rightOperandValue = 0;

    /// <summary>
    /// Defines if we use a column reference or a value for the right operand.
    /// </summary>
    public readonly bool hasRightOperandValue = true;

    /// <summary>
    /// Represents the operator to apply on left and right operands.
    /// </summary>
    public readonly DataRowFilterOperatorEnum op = DataRowFilterOperatorEnum.OperatorEqual;

    /// <summary>
    /// Simple column to scalar value filter
    /// </summary>
    /// <param name="leftOperand">Column to use in operation</param>
    /// <param name="op">Operator to apply</param>
    /// <param name="rightOperand">Value to compare against</param>
    public DataQueryFilter(DataRowColumnEnum leftOperand, DataRowFilterOperatorEnum op, int rightOperand)
    {
        leftOperandColumn = leftOperand;
        this.op = op;
        rightOperandValue = rightOperand;
    }

    /// <summary>
    /// Simple column to scalar value filter
    /// </summary>
    /// <param name="leftOperand">Column to use in operation</param>
    /// <param name="op">Operator to apply</param>
    /// <param name="rightOperand">Value to compare against</param>
    public DataQueryFilter(DataRowColumnEnum leftOperand, DataRowFilterOperatorEnum op, DataRowColumnEnum rightOperand)
    {
        leftOperandColumn = leftOperand;
        this.op = op;
        rightOperandColumn = rightOperand;
        hasRightOperandValue = false;
    }

}