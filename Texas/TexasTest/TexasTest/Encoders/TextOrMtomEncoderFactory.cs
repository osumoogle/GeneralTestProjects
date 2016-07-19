using System.ServiceModel.Channels;

namespace TextAndMtom
{
    public class TextOrMtomEncoderFactory : MessageEncoderFactory
    {
        MessageVersion _messageVersion;
        readonly TextOrMtomEncoder _encoder;

        public TextOrMtomEncoderFactory(MessageVersion messageVersion)
        {
            _messageVersion = messageVersion;
            _encoder = new TextOrMtomEncoder(messageVersion);
        }
        public override MessageEncoder Encoder => _encoder;

        public override MessageVersion MessageVersion => _encoder.MessageVersion;
    }
}