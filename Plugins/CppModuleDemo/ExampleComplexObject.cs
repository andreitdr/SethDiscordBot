using System.Runtime.InteropServices;

namespace CppModuleDemo;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct ExampleComplexObject
{
    public int IntegerValue;
    public double DoubleValue;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string StringValue;
    
    public ExampleComplexObject(int integerValue, double doubleValue, string stringValue)
    {
        IntegerValue = integerValue;
        DoubleValue = doubleValue;
        StringValue = stringValue;
    }
}