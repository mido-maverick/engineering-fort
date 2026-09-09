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

    private static MemberInfo? GetMemberInfo(string str)
    {
        var parts = str.Split('.');
        switch (parts.Length)
        {
            case 1:
                return null;
            case 2:
                // TODO: Optimize
                var type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(type => type.Name == parts[0]);
                return type?.GetMember(parts[1]).FirstOrDefault();
            case > 2:
                return null;
            default:
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
