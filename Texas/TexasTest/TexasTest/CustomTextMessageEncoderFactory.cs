using System.ServiceModel.Channels;

namespace TexasTest
{
    public class CustomTextMessageEncoderFactory : MessageEncoderFactory
    {
        internal CustomTextMessageEncoderFactory(string mediaType, string charSet, MessageVersion version)
        {
            MessageVersion = version;
            MediaType = mediaType;
            CharSet = charSet;
            Encoder = new CustomTextMessageEncoder(this);
        }

        public override MessageEncoder Encoder { get; }

        public override MessageVersion MessageVersion { get; }

        internal string MediaType { get; }

        internal string CharSet { get; }
    }
}