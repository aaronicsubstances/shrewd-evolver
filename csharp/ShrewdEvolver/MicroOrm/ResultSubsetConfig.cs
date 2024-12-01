using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AaronicSubstances.ShrewdEvolver.MicroOrm
{
    public class ResultSubsetConfig
    {
        public ResultSubsetConfig FallbackConfig { get; set; }
        public List<string> FallbackFieldsToInclude { get; set; }
        public List<string> FallbackFieldsToExclude { get; set; }
        public List<ResultFieldConfig> FieldConfigs { get; set; }
        public ResultFieldConfig FallbackFieldConfig { get; set; }
        public Func<object> BareResultSubsetGenerator { get; set; }

        /// <summary>
        /// NB: FallbackFieldConfig overrides FallbackConfig in priority.
        /// </summary>
        /// <returns></returns>
        public ResultSubsetConfig GetEffectiveConfig()
        {
            var merged = new ResultSubsetConfig
            {
                FieldConfigs = new List<ResultFieldConfig>()
            };
            if (this.FieldConfigs != null)
            {
                foreach (var fieldConfig in this.FieldConfigs)
                {
                    var effectiveFieldConfig = new ResultFieldConfig
                    {
                        FieldId = fieldConfig.FieldId
                    };
                    fieldConfig.OverwriteEmptyPropsAsideId(effectiveFieldConfig);
                    merged.FieldConfigs.Add(effectiveFieldConfig);
                }
            }
            if (FallbackFieldConfig != null)
            {
                foreach (var fieldConfig in merged.FieldConfigs)
                {
                    FallbackFieldConfig.OverwriteEmptyPropsAsideId(fieldConfig);
                }
            }
            var fallbackEquivalent = FallbackConfig?.GetEffectiveConfig();
            if (fallbackEquivalent?.FieldConfigs != null)
            {
                foreach (var fieldConfig in fallbackEquivalent.FieldConfigs)
                {
                    if (this.FallbackFieldsToExclude?.Any(x => x == fieldConfig.FieldId) == true)
                    {
                        continue;
                    }
                    // treat non-null but empty included ids as null array, so that all
                    // non-excluded ids will be included.
                    if (this.FallbackFieldsToInclude?.Any() == true &&
                        !this.FallbackFieldsToInclude.Contains(fieldConfig.FieldId))
                    {
                        continue;
                    }
                    var effectiveFieldConfig = merged.FieldConfigs.FirstOrDefault(
                        x => x.FieldId == fieldConfig.FieldId);
                    if (effectiveFieldConfig == null)
                    {
                        effectiveFieldConfig = new ResultFieldConfig
                        {
                            FieldId = fieldConfig.FieldId
                        };
                        fieldConfig.OverwriteEmptyPropsAsideId(effectiveFieldConfig);
                        merged.FieldConfigs.Add(effectiveFieldConfig);
                    }
                    else
                    {
                        fieldConfig.OverwriteEmptyPropsAsideId(effectiveFieldConfig);
                    }
                }
            }
            merged.BareResultSubsetGenerator = this.BareResultSubsetGenerator ??
                fallbackEquivalent?.BareResultSubsetGenerator;
            return merged;
        }
    }
}
