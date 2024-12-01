using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class QueryParamSourceConfig
    {
        public QueryParamSourceConfig FallbackConfig { get; set; }
        public List<string> FallbackParamsToInclude { get; set; }
        public List<string> FallbackParamsToExclude { get; set; }
        public List<QueryParamConfig> ParamConfigs { get; set; }
        public QueryParamConfig FallbackParamConfig { get; set; }

        /// <summary>
        /// NB: FallbackParamConfig overrides FallbackConfig in priority.
        /// </summary>
        /// <returns></returns>
        public QueryParamSourceConfig GetEffectiveConfig()
        {
            var merged = new QueryParamSourceConfig
            {
                ParamConfigs = new List<QueryParamConfig>()
            };
            if (this.ParamConfigs != null)
            {
                foreach (var paramConfig in this.ParamConfigs)
                {
                    var effectiveParamConfig = new QueryParamConfig
                    {
                        ParamId = paramConfig.ParamId
                    };
                    paramConfig.OverwriteEmptyPropsAsideId(effectiveParamConfig);
                    merged.ParamConfigs.Add(effectiveParamConfig);
                }
            }
            if (FallbackParamConfig != null)
            {
                foreach (var paramConfig in merged.ParamConfigs)
                {
                    FallbackParamConfig.OverwriteEmptyPropsAsideId(paramConfig);
                }
            }
            var fallbackEquivalent = FallbackConfig?.GetEffectiveConfig();
            if (fallbackEquivalent?.ParamConfigs != null)
            {
                foreach (var paramConfig in fallbackEquivalent.ParamConfigs)
                {
                    if (this.FallbackParamsToExclude?.Any(x => x == paramConfig.ParamId) == true)
                    {
                        continue;
                    }
                    // treat non-null but empty included ids as null array, so that all
                    // non-excluded ids will be included.
                    if (this.FallbackParamsToInclude?.Any() == true &&
                        !this.FallbackParamsToInclude.Contains(paramConfig.ParamId))
                    {
                        continue;
                    }
                    var effectiveParamConfig = merged.ParamConfigs.FirstOrDefault(
                        x => x.ParamId == paramConfig.ParamId);
                    if (effectiveParamConfig == null)
                    {
                        effectiveParamConfig = new QueryParamConfig
                        {
                            ParamId = paramConfig.ParamId
                        };
                        paramConfig.OverwriteEmptyPropsAsideId(effectiveParamConfig);
                        merged.ParamConfigs.Add(effectiveParamConfig);
                    }
                    else
                    {
                        paramConfig.OverwriteEmptyPropsAsideId(effectiveParamConfig);
                    }
                }
            }
            return merged;
        }
    }
}
