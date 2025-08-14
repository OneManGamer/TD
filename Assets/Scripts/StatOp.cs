// File: StatOp.cs
public enum StatOp
{
    Add = 0,       // result += value
    Mul = 1,       // result *= value
    Override = 2   // result  = value (usually highest order wins)
}
