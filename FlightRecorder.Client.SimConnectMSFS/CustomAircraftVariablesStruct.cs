using System;
using System.Runtime.InteropServices;

namespace FlightRecorder.Client.SimConnectMSFS;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct CustomAircraftVariablesStruct
{
    public const int MaxVariables = 32;

    public double V00; public double V01; public double V02; public double V03;
    public double V04; public double V05; public double V06; public double V07;
    public double V08; public double V09; public double V10; public double V11;
    public double V12; public double V13; public double V14; public double V15;
    public double V16; public double V17; public double V18; public double V19;
    public double V20; public double V21; public double V22; public double V23;
    public double V24; public double V25; public double V26; public double V27;
    public double V28; public double V29; public double V30; public double V31;

    public double this[int index]
    {
        get => index switch
        {
            0 => V00, 1 => V01, 2 => V02, 3 => V03,
            4 => V04, 5 => V05, 6 => V06, 7 => V07,
            8 => V08, 9 => V09, 10 => V10, 11 => V11,
            12 => V12, 13 => V13, 14 => V14, 15 => V15,
            16 => V16, 17 => V17, 18 => V18, 19 => V19,
            20 => V20, 21 => V21, 22 => V22, 23 => V23,
            24 => V24, 25 => V25, 26 => V26, 27 => V27,
            28 => V28, 29 => V29, 30 => V30, 31 => V31,
            _ => throw new IndexOutOfRangeException($"Index {index} is out of range for CustomAircraftVariablesStruct (0-{MaxVariables - 1})")
        };
        set
        {
            switch (index)
            {
                case 0: V00 = value; break; case 1: V01 = value; break; case 2: V02 = value; break; case 3: V03 = value; break;
                case 4: V04 = value; break; case 5: V05 = value; break; case 6: V06 = value; break; case 7: V07 = value; break;
                case 8: V08 = value; break; case 9: V09 = value; break; case 10: V10 = value; break; case 11: V11 = value; break;
                case 12: V12 = value; break; case 13: V13 = value; break; case 14: V14 = value; break; case 15: V15 = value; break;
                case 16: V16 = value; break; case 17: V17 = value; break; case 18: V18 = value; break; case 19: V19 = value; break;
                case 20: V20 = value; break; case 21: V21 = value; break; case 22: V22 = value; break; case 23: V23 = value; break;
                case 24: V24 = value; break; case 25: V25 = value; break; case 26: V26 = value; break; case 27: V27 = value; break;
                case 28: V28 = value; break; case 29: V29 = value; break; case 30: V30 = value; break; case 31: V31 = value; break;
                default: throw new IndexOutOfRangeException($"Index {index} is out of range for CustomAircraftVariablesStruct (0-{MaxVariables - 1})");
            }
        }
    }

    public static CustomAircraftVariablesStruct FromArray(double[]? values)
    {
        var s = new CustomAircraftVariablesStruct();
        if (values == null) return s;
        var len = Math.Min(values.Length, MaxVariables);
        for (int i = 0; i < len; i++)
        {
            s[i] = values[i];
        }
        return s;
    }

    public double[] ToArray(int count)
    {
        var len = Math.Clamp(count, 0, MaxVariables);
        var arr = new double[len];
        for (int i = 0; i < len; i++)
        {
            arr[i] = this[i];
        }
        return arr;
    }
}
