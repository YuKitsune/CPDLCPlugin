namespace CPDLCPlugin;

// Converts between the indicated airspeed and the Mach number at a pressure altitude, in the ISA.
// The conversion does not use the temperature, because the pressure altitude gives the static pressure.
public static class SpeedConversions
{
    // The speed of sound at sea level in the ISA, in knots
    const double SeaLevelSpeedOfSound = 661.4786;

    const double TropopauseFeet = 36089;

    public static double IndicatedAirspeedToMach(double knots, double pressureAltitudeFeet)
    {
        // The impact pressure, divided by the sea level pressure
        var impactPressure = Math.Pow(1 + 0.2 * Math.Pow(knots / SeaLevelSpeedOfSound, 2), 3.5) - 1;
        var pressureRatio = PressureRatio(pressureAltitudeFeet);

        return Math.Sqrt(5 * (Math.Pow(impactPressure / pressureRatio + 1, 2.0 / 7.0) - 1));
    }

    public static double MachToIndicatedAirspeed(double mach, double pressureAltitudeFeet)
    {
        // The impact pressure, divided by the sea level pressure
        var impactPressure = PressureRatio(pressureAltitudeFeet) * (Math.Pow(1 + 0.2 * mach * mach, 3.5) - 1);

        return SeaLevelSpeedOfSound * Math.Sqrt(5 * (Math.Pow(impactPressure + 1, 2.0 / 7.0) - 1));
    }

    // The static pressure at the pressure altitude, divided by the sea level pressure
    static double PressureRatio(double pressureAltitudeFeet)
    {
        return pressureAltitudeFeet <= TropopauseFeet
            ? Math.Pow(1 - 6.8755856E-06 * pressureAltitudeFeet, 5.2558797)
            : 0.2233609 * Math.Exp(-4.806346E-05 * (pressureAltitudeFeet - TropopauseFeet));
    }
}
