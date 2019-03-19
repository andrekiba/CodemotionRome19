using System;
using System.Collections.Generic;
using System.Text;

namespace CodemotionRome19.Core.Models
{
    public class ProjectToBuild
    {
        public string Id { get; set; }
        public string ProjectName { get; set; }
        public string PipelineName { get; set; }
        public string Branch { get; set; }
        public string RequestedByUser { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }
}
