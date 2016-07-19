using System.ServiceModel.Channels;

namespace TextAndMtom
{
    public class TextOrMtomEncodingBindingElement : MessageEncodingBindingElement
    {
        public const string IsIncomingMessageMtomPropertyName = "IncomingMessageIsMtom";
        public const string MtomBoundaryPropertyName = "__MtomBoundary";
        public const string MtomStartInfoPropertyName = "__MtomStartInfo";
        public const string MtomStartUriPropertyName = "__MtomStartUri";

        private MessageVersion _messageVersion = MessageVersion.Default;

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new TextOrMtomEncoderFactory(_messageVersion);
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return _messageVersion;
            }
            set
            {
                _messageVersion = value;
            }
        }

        public override BindingElement Clone()
        {
            return this;
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            return context.CanBuildInnerChannelListener<TChannel>();
        }
    }
}
