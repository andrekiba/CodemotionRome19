using Alexa.NET.Response;

namespace CodemotionRome19.Functions.Alexa
{
    public class Repr : Reprompt
    {
        public Repr(string text) : base(text)
        {
            
        }

        public Repr(IOutputSpeech speech)
        {
            OutputSpeech = speech;
        }
    }
}
