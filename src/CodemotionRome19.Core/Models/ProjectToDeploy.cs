using System.Collections.Generic;

namespace CodemotionRome19.Core.Models
{
    public class ProjectToDeploy
    {
        public string Id { get; set; }
        public string ProjectName { get; set; }
        public string PipelineName { get; set; }
        public string Branch { get; set; }
        public string RequestedByUser { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
    }
}
