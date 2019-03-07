using System;
using System.Collections.Generic;
using System.Text;
using CodemotionRome19.Core.Azure;

namespace CodemotionRome19.Functions.Alexa
{
    public static class AzureResourceTypeResolution
    {
        static Dictionary<AzureResourceType, List<string>> dictionary = new Dictionary<AzureResourceType, List<string>>();
    }
}
