using Microsoft.Extensions.Options;

namespace EngineeringFort;

public class UnitSystemService
{
    public List<string> ActiveUnits { get; } = [];

    public Dictionary<MemberInfo, string[]> QuantityMemberFormats { get; }


    public UnitSystemService(IOptions<QuantityOptions> options)
    {
        ActiveUnits = [.. options.Value.DefaultUnits];
        QuantityMemberFormats = [];
        foreach (var formats in options.Value.Formats)
        {
            var memberInfo = GetMemberInfo(formats.Key);
            if (memberInfo is null) continue;

            QuantityMemberFormats[memberInfo] = formats.Value;
        }
    }

    /// <param name="str">
    ///     <c>"Type.Member"</c>, with a nested type written the way the runtime names it,
    ///     <c>"Outer+Nested.Member"</c>, since further dots are kept for property paths.
    /// </param>
    static MemberInfo? GetMemberInfo(string str)
    {
        var parts = str.Split('.');
        switch (parts.Length)
        {
            case 1:
                // A bare member name, for that member on any type; not supported yet.
                return null;
            case 2:
                var names = parts[0].Split('+');
                // TODO: Optimize
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(type => type.Name == names[0] && !type.IsNested);
                foreach (var nested in names[1..])
                    type = type?.GetNestedType(nested);
                return type?.GetMember(parts[1]).FirstOrDefault();
            default:
                // A property path, such as a member of a member's type; not supported yet.
                return null;
        }
    }

    /// <summary>The format configured for <paramref name="memberInfo"/>, or the default.</summary>
    /// <remarks>
    ///     Only the first of a member's formats is read. The rest are spellings of the same value
    ///     kept for reference; <see cref="QuantityFormat"/> decides the abbreviation instead.
    /// </remarks>
    public QuantityFormat GetFormat(MemberInfo memberInfo) => new(GetFormats(memberInfo)?.FirstOrDefault());

    /// <param name="member">A property of <paramref name="owner"/>. Unknown names take the default.</param>
    public QuantityFormat GetFormat(Type owner, string member) =>
        owner.GetProperty(member) is { } property ? GetFormat(property) : default;

    /// <inheritdoc cref="GetFormat(Type, string)" />
    public QuantityFormat GetFormat<TOwner>(string member) => GetFormat(typeof(TOwner), member);

    public string[]? GetFormats(MemberInfo memberInfo)
    {
        if (QuantityMemberFormats.TryGetValue(memberInfo, out var formats)) return formats;

        var baseType = memberInfo.ReflectedType?.BaseType;
        var baseMemberInfo = baseType?.GetMember(memberInfo.Name).FirstOrDefault();
        return baseMemberInfo is not null ? GetFormats(baseMemberInfo) : null;
    }
}
