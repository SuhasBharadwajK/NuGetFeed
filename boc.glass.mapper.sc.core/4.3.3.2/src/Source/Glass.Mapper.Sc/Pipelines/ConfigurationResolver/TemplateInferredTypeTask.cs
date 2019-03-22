/*
   Copyright 2012 Michael Edwards
 
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 
*/
//-CRE-

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Glass.Mapper.Pipelines.ConfigurationResolver;
using Glass.Mapper.Sc.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Glass.Mapper.Sc.Pipelines.ConfigurationResolver
{
    /// <summary>
    /// TemplateInferredTypeTask
    /// </summary>
    public class TemplateInferredTypeTask : AbstractConfigurationResolverTask
    {
        static ConcurrentDictionary<Tuple<Context, Type, ID>, SitecoreTypeConfiguration>
            _inferredCache = new ConcurrentDictionary<Tuple<Context, Type, ID>, SitecoreTypeConfiguration>();


        public TemplateInferredTypeTask()
        {
            Name = "TemplateInferredTypeTask";
        }

        /// <summary>
        /// Executes the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public override void Execute(ConfigurationResolverArgs args)
        {
            if (args.Result == null && args.AbstractTypeCreationContext.InferType)
            {
                var scContext = args.AbstractTypeCreationContext as SitecoreTypeCreationContext;

                var requestedType = scContext.RequestedType;
                var item = scContext.Item;
                var templateId = item != null ? item.TemplateID : scContext.TemplateId;

                var key = new Tuple<Context, Type, ID>(args.Context, requestedType, templateId);
                if (_inferredCache.ContainsKey(key))
                {
                    args.Result = _inferredCache[key];
                }
                else
                {

                    var configs = args.Context.TypeConfigurations.Select(x => x.Value as SitecoreTypeConfiguration);
                    var bestType = FindBestType(scContext.SitecoreService.Database.Templates[templateId], configs,
                        requestedType);
                    if (bestType != null)
                    {
                        args.Result = bestType;
                        if (!_inferredCache.TryAdd(key, args.Result as SitecoreTypeConfiguration))
                        {
                            //TODO: some logging
                        }
                    }
                }
            }
            base.Execute(args);
        }

        private SitecoreTypeConfiguration FindBestType(TemplateItem templateItem, IEnumerable<SitecoreTypeConfiguration> configs, Type requestedType)
        {
            if (templateItem == null)
                return null;
            var types = configs.Where(x => x.TemplateId == templateItem.ID);
            if (types.Any())
            {
                var result = types.FirstOrDefault(x => requestedType.IsAssignableFrom(x.Type));
                if (result != null)
                    return result;
            }
            foreach (var btmpl in templateItem.BaseTemplates)
            {
                var result = FindBestType(btmpl, configs, requestedType);
                if (result != null)
                    return result;
            }
            return null;
        }

    }
}

