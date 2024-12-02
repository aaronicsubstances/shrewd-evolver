using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class QueryParamSourceConfig
    {
        public QueryParamSourceConfig Fallback { get; set; }
        public IList<string> FallbackIdsToInclude { get; set; }
        public IList<string> FallbackIdsToExclude { get; set; }
        public IList<QueryParamConfig> ParamConfigs { get; set; }
        public QueryParamConfig CommonParamConfig { get; set; }

        /// <summary>
        /// NB: CommonParamConfig overrides Fallback in priority.
        /// </summary>
        /// <returns></returns>
        public QueryParamSourceConfig GetEffectiveConfig()
        {
            var merged = new QueryParamSourceConfig
            {
                ParamConfigs = new List<QueryParamConfig>()
            };
            if (Fallback != null)
            {
                var fallbackEquivalent = Fallback.GetEffectiveConfig();

                foreach (var paramConfig in fallbackEquivalent.ParamConfigs)
                {
                    if (this.FallbackIdsToExclude?.Any(x => x == paramConfig.Id) == true)
                    {
                        continue;
                    }

                    // treat non-null but empty included ids as null array, so that all
                    // non-excluded ids will be included.
                    if (this.FallbackIdsToInclude?.Any() == true &&
                        !this.FallbackIdsToInclude.Contains(paramConfig.Id))
                    {
                        continue;
                    }

                    // make a copy to be modified later.
                    merged.ParamConfigs.Add(paramConfig.MakeDuplicate());
                }
            }

            if (CommonParamConfig != null)
            {
                foreach (var paramConfig in merged.ParamConfigs)
                {
                    CommonParamConfig.TransferNonEmptyPropsAsideId(paramConfig);
                }
            }

            if (this.ParamConfigs != null)
            {
                foreach (var paramConfig in this.ParamConfigs)
                {
                    QueryParamConfig existingParamConfig = null;
                    if (!string.IsNullOrEmpty(paramConfig.Id))
                    {
                        existingParamConfig = merged.ParamConfigs.FirstOrDefault(
                            x => x.Id == paramConfig.Id);
                    }
                    if (existingParamConfig == null)
                    {
                        merged.ParamConfigs.Add(paramConfig);
                    }
                    else
                    {
                        paramConfig.TransferNonEmptyPropsAsideId(existingParamConfig);
                    }
                }
            }
            return merged;
        }
    }
}
