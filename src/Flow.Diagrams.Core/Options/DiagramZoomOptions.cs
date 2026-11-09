using System;

namespace Flow.Diagrams.Core.Options;

/// <summary>Limits and wheel behavior of zooming.</summary>
public class DiagramZoomOptions
{
    private double _minimum = 0.1;
    private double _scaleFactor = 1.05;

    /// <summary>
    /// Whether the mouse wheel zooms; <c>true</c> by default. Programmatic <see cref="Diagram.SetZoom"/> still works.
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Whether the wheel direction is reversed; by default scrolling down (positive <see
    /// cref="Events.WheelEventArgs.DeltaY"/>) zooms in.
    /// </summary>
    public bool Inverse { get; set; }
    /// <summary>
    /// The smallest zoom, 0.1 by default; <see cref="Diagram.SetZoom"/> clamps to it. Setting zero or less throws
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public double Minimum
    {
        get => _minimum;
        set
        {
            if (value <= 0)
                throw new ArgumentException($"Minimum can't be less than zero");
            
            _minimum = value;
        }
    }
    /// <summary>The largest zoom, 2 by default; <see cref="Diagram.SetZoom"/> clamps to it.</summary>
    public double Maximum { get; set; } = 2;
    /// <summary>
    /// The factor one wheel step multiplies or divides the zoom by; 1.05 by default. Values outside 1.01 to 2 throw
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public double ScaleFactor
    {
        get => _scaleFactor;
        set
        {
            if (value is < 1.01 or > 2)
                throw new ArgumentException($"ScaleFactor can't be lower than 1.01 or greater than 2");
                    
            _scaleFactor = value;
        }
    }
}