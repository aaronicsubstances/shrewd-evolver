using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public delegate Task<object[]> ResultSubsetReprGeneratorType(ResultSubsetReprGeneratorParams arg);

    public class ResultSubsetConfig
    {
        public ResultSubsetConfig Fallback { get; set; }
        public IList<string> FallbackIdsToInclude { get; set; }
        public IList<string> FallbackIdsToExclude { get; set; }
        public IList<ResultFieldConfig> FieldConfigs { get; set; }
        public ResultFieldConfig CommonFieldConfig { get; set; }
        public IList<ResultSubsetReprGeneratorType> ResultSubsetReprGeneratorChain { get; set; }
        public bool DiscardFallbackResultSubsetReprGeneratorChain { get; set; }

        /// <summary>
        /// NB: CommonFieldConfig overrides Fallback in priority.
        /// </summary>
        /// <returns></returns>
        public ResultSubsetConfig GetEffectiveConfig()
        {
            var merged = new ResultSubsetConfig
            {
                FieldConfigs = new List<ResultFieldConfig>()
            };

            if (Fallback != null)
            {
                var fallbackEquivalent = Fallback.GetEffectiveConfig();
                merged.ResultSubsetReprGeneratorChain = fallbackEquivalent.ResultSubsetReprGeneratorChain;
                foreach (var fieldConfig in fallbackEquivalent.FieldConfigs)
                {
                    if (this.FallbackIdsToExclude?.Any(x => x == fieldConfig.Id) == true)
                    {
                        continue;
                    }

                    // treat non-null but empty included ids as null array, so that all
                    // non-excluded ids will be included.
                    if (this.FallbackIdsToInclude?.Any() == true &&
                        !this.FallbackIdsToInclude.Contains(fieldConfig.Id))
                    {
                        continue;
                    }

                    // make a copy to be modified later.
                    merged.FieldConfigs.Add(fieldConfig.MakeDuplicate());
                }
            }

            if (CommonFieldConfig != null)
            {
                foreach (var fieldConfig in merged.FieldConfigs)
                {
                    CommonFieldConfig.TransferNonEmptyPropsAsideId(fieldConfig);
                }
            }

            if (this.FieldConfigs != null)
            {
                foreach (var fieldConfig in this.FieldConfigs)
                {
                    ResultFieldConfig existingFieldConfig = null;
                    if (!string.IsNullOrEmpty(fieldConfig.Id))
                    {
                        existingFieldConfig = merged.FieldConfigs.FirstOrDefault(
                            x => x.Id == fieldConfig.Id);
                    }
                    if (existingFieldConfig == null)
                    {
                        merged.FieldConfigs.Add(fieldConfig);
                    }
                    else
                    {
                        fieldConfig.TransferNonEmptyPropsAsideId(existingFieldConfig);
                    }
                }
            }

            merged.ResultSubsetReprGeneratorChain = MicroOrmHelpers.MergeChains(
                ResultSubsetReprGeneratorChain, merged.ResultSubsetReprGeneratorChain,
                DiscardFallbackResultSubsetReprGeneratorChain);

            return merged;
        }
    }
}
