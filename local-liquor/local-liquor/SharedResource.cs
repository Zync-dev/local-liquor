using Microsoft.Extensions.Localization;

// The assembly is called "local-liquor" but the code lives in "local_liquor",
// and the localizer derives its resource prefix from the assembly name unless
// told otherwise. Without this the lookup would miss every string.
[assembly: RootNamespace("local_liquor")]

namespace local_liquor;

/// <summary>
/// Marker type that gives <c>IStringLocalizer&lt;SharedResource&gt;</c> somewhere to look.
///
/// It lives at the project root on purpose: the localizer builds the resource name from
/// the root namespace, the configured ResourcesPath and the type's namespace *below* the
/// root. Putting this class in local_liquor.Resources would make it hunt for
/// Resources/Resources/SharedResource.resx.
///
/// All site copy lives in Resources/SharedResource.resx (Danish, the default) and
/// Resources/SharedResource.en.resx.
/// </summary>
public sealed class SharedResource;
