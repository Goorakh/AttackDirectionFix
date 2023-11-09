using HG.Reflection;
using R2API.Utils;

[assembly: SearchableAttribute.OptIn]
[assembly: NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]