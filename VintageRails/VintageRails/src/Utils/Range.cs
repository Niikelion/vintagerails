namespace VintageRails.Utils;

public class Ranged {

    public readonly double min;
    public readonly double max;

    public static Ranged LowerBound(double min) {
        return new Ranged(min, float.PositiveInfinity);
    }

    public static Ranged UpperBound(double max) {
        return new Ranged(float.NegativeInfinity, max);
    }
    
    public Ranged(double a, double b) {
        if (a < b) {
            min = a;
            max = b;
        }
        else {
            max = a;
            min = b;
        }
    }
    
    public bool IsInRange(double value) {
        return value >= min && value <= max;
    }
}