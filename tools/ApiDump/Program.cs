using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

// Aufruf: ApiDump <Ordner mit Spiel-DLLs> "<Typ> <Typ> <Suche*>" [weitere DLL-Ordner …]
// Typ = Name oder voller Name → alle eigenen Member; mit * oder ? = Typnamen suchen; Typ.Name* = Member suchen.
if (args.Length < 2)
{
    Console.Error.WriteLine("ApiDump <Spiel-DLL-Ordner> \"<Typen>\" [weitere Ordner …]");
    return 1;
}

var gameDir = args[0];
var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
// Laufzeit zuerst: mscorlib/System.* der Spiel-Pakete sind teils alte Fassaden, mit denen Enums nicht auflösbar sind
foreach (var file in Directory.EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
    paths.TryAdd(Path.GetFileName(file), file);
foreach (var file in Directory.EnumerateFiles(gameDir, "*.dll"))
    paths.TryAdd(Path.GetFileName(file), file);
foreach (var dir in args.Skip(2).Where(Directory.Exists))
foreach (var file in Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories))
    paths.TryAdd(Path.GetFileName(file), file);

using var context = new MetadataLoadContext(new PathAssemblyResolver(paths.Values), "System.Private.CoreLib");
var game = new List<Assembly>();
foreach (var file in Directory.EnumerateFiles(gameDir, "*.dll").OrderBy(f => f))
{
    try { game.Add(context.LoadFromAssemblyPath(paths[Path.GetFileName(file)])); }
    catch (Exception exc) { Console.WriteLine($"# {Path.GetFileName(file)} nicht geladen: {exc.Message}"); }
}

var types = game.SelectMany(TypesOf).Select(t => (Type: t, Full: FullNameOf(t))).Where(t => t.Full != null).ToList();
Console.WriteLine($"# {game.Count} Assemblies, {types.Count} Typen");

foreach (var query in args[1].Split(new[] { ' ', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries))
{
    try { Query(query); }
    catch (Exception exc) { Console.WriteLine($"=== {query}: Fehler {exc.GetType().Name}: {exc.Message}"); }
}
return 0;

void Query(string query)
{
    Console.WriteLine();
    var dot = query.LastIndexOf('.');
    if (query.IndexOfAny(new[] { '*', '?' }) >= 0 && dot > 0 && Find(query[..dot]).Any())
    {
        // Typ.Muster* → passende Member eines Typs (auch geerbte)
        var pattern = Wildcard(query[(dot + 1)..]);
        foreach (var type in Find(query[..dot]))
        {
            Console.WriteLine($"=== {Name(type)} – Member \"{query[(dot + 1)..]}\" (inkl. geerbte)");
            foreach (var member in SafeMembers(type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                         .Where(m => pattern.IsMatch(m.Name)))
                Console.WriteLine("  " + Describe(member));
        }
        return;
    }
    if (query.IndexOfAny(new[] { '*', '?' }) >= 0)
    {
        var pattern = Wildcard(query);
        Console.WriteLine($"=== Suche {query}");
        foreach (var (type, full) in types.Where(t => pattern.IsMatch(t.Type.Name) || pattern.IsMatch(t.Full!)).Take(200))
            Console.WriteLine($"  {type.Assembly.GetName().Name}: {full}");
        return;
    }
    var found = Find(query).ToList();
    if (found.Count == 0) Console.WriteLine($"=== {query}: nicht gefunden");
    foreach (var type in found) Dump(type);
}

IEnumerable<Type> Find(string name) =>
    types.Where(t => t.Type.Name == name || t.Full == name || t.Full!.Replace('+', '.') == name).Select(t => t.Type);

void Dump(Type type)
{
    var kind = type.IsEnum ? "enum" : type.IsValueType ? "struct" : type.IsInterface ? "interface" : "class";
    var bases = new List<string>();
    for (var b = SafeBase(type); b != null && b.FullName != "System.Object"; b = SafeBase(b)) bases.Add(Name(b));
    Console.WriteLine($"=== {kind} {type.FullName} [{type.Assembly.GetName().Name}]" + (bases.Count > 0 ? " : " + string.Join(" : ", bases) : ""));
    if (type.IsEnum)
    {
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            Console.WriteLine($"  {field.Name} = {field.GetRawConstantValue()}");
        return;
    }
    foreach (var member in SafeMembers(type, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                 .OrderBy(m => m.MemberType).ThenBy(m => m.Name))
        Console.WriteLine("  " + Describe(member));
}

IEnumerable<MemberInfo> SafeMembers(Type type, BindingFlags flags)
{
    MemberInfo[] members;
    try { members = type.GetMembers(flags); }
    catch (Exception exc) { Console.WriteLine($"  # Member nicht lesbar: {exc.Message}"); yield break; }
    foreach (var member in members)
    {
        if (member is MethodInfo { IsSpecialName: true }) continue; // get_/set_/add_ – stehen schon als Property
        if (member is ConstructorInfo { IsStatic: true }) continue;
        yield return member;
    }
}

string Describe(MemberInfo member)
{
    try
    {
        return member switch
        {
            FieldInfo f => $"{(f.IsStatic ? "static " : "")}field {Name(f.FieldType)} {f.Name}",
            PropertyInfo p => $"{(p.GetMethod?.IsStatic == true || p.SetMethod?.IsStatic == true ? "static " : "")}prop {Name(p.PropertyType)} {p.Name} {{{(p.GetMethod != null ? " get;" : "")}{(p.SetMethod != null ? " set;" : "")} }}",
            ConstructorInfo c => $"ctor({Params(c)})",
            MethodInfo m => $"{(m.IsStatic ? "static " : "")}{(m.IsVirtual && !m.IsFinal ? "virtual " : "")}{Name(m.ReturnType)} {m.Name}{Generic(m)}({Params(m)})",
            Type t => $"nested {(t.IsEnum ? "enum" : "type")} {t.Name}",
            EventInfo e => $"event {Name(e.EventHandlerType!)} {e.Name}",
            _ => member.ToString() ?? member.Name,
        };
    }
    catch (Exception exc)
    {
        return $"{member.MemberType} {member.Name} # {exc.GetType().Name}: {exc.Message}";
    }
}

string Generic(MethodInfo m) => m.IsGenericMethodDefinition ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";

string Params(MethodBase method) => string.Join(", ", method.GetParameters().Select(p =>
{
    var type = p.ParameterType;
    var prefix = type.IsByRef ? (p.IsOut ? "out " : "ref ") : "";
    if (type.IsByRef) type = type.GetElementType()!;
    var optional = p.HasDefaultValue ? " = " + (p.RawDefaultValue ?? "null") : "";
    return $"{prefix}{Name(type)} {p.Name}{optional}";
}));

static string Name(Type type)
{
    if (type.IsByRef) return Name(type.GetElementType()!) + "&";
    if (type.IsArray) return Name(type.GetElementType()!) + "[]";
    if (type.IsGenericParameter) return type.Name;
    var name = type.IsNested ? Name(type.DeclaringType!) + "." + type.Name : (type.Namespace is { Length: > 0 } ns && !ns.StartsWith("System") ? ns + "." : "") + type.Name;
    if (!type.IsGenericType) return name;
    var tick = name.IndexOf('`');
    if (tick >= 0) name = name[..tick];
    return name + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">";
}

static string? FullNameOf(Type type)
{
    try { return type.FullName; }
    catch { return null; } // kaputte Metadaten in manchen DLLs
}

static Type? SafeBase(Type type)
{
    try { return type.BaseType; }
    catch { return null; }
}

static IEnumerable<Type> TypesOf(Assembly assembly)
{
    try { return assembly.GetTypes(); }
    catch (ReflectionTypeLoadException exc) { return exc.Types.Where(t => t != null)!; }
    catch { return Array.Empty<Type>(); }
}

static Regex Wildcard(string pattern) =>
    new("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
