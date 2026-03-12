var asm = System.Reflection.Assembly.LoadFrom(@"C:\Users\bruno\.nuget\packages\elbruno.text2image\0.6.0\lib\net10.0\ElBruno.Text2Image.dll");
foreach (var t in asm.GetExportedTypes())
{
    Console.WriteLine(t.FullName + " : " + t.BaseType?.FullName);
    foreach (var m in t.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
        Console.WriteLine("  M: " + m.Name + "(" + string.Join(",", m.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name)) + ") -> " + m.ReturnType.Name);
    foreach (var p in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        Console.WriteLine("  P: " + p.PropertyType.Name + " " + p.Name);
    foreach (var c in t.GetConstructors())
        Console.WriteLine("  C: (" + string.Join(",", c.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name)) + ")");
}
