﻿using JsonApiDotNetCore.Models;

namespace OptimaJet.DWKit.StarterApplication.Models
{
    public class WorkflowDefinition : Identifiable
    {
        [Attr("name")]
        public string Name { get; set; }

        [Attr("enabled")]
        public bool Enabled { get; set; }

        [Attr("description")]
        public string Description { get; set; }

        [Attr("workflow-scheme")]
        public string WorkflowScheme { get; set; }

        [Attr("workflow-business-flow")]
        public string WorkflowBusinessFlow { get; set; }

        [HasOne("store-type")]
        public virtual StoreType StoreType { get; set; }
        public int? StoreTypeId { get; set; }
    }
}
