using System;

namespace Warlock.Mvp
{
    public static class MvpScopeGuard
    {
        public static void EnsureFfaOnly(string mode)
        {
            if (!string.Equals(mode, MvpConstants.ModeFfa, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Only FFA is allowed in MVP core");
            }
        }

        public static bool IsExplicitlyExcluded(string feature)
        {
            foreach (var excluded in MvpConstants.ExplicitMvpExclusions)
            {
                if (string.Equals(excluded, feature, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
