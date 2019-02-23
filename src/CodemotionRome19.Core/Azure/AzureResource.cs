﻿namespace CodemotionRome19.Core.Azure
{
    public class AzureResource
    {
        public string Name { get; set; }

        public AzureResourceType Type { get; set; }

        public string Description { get; set; }

        public bool IsAvailable { get; set; }
    }
}
