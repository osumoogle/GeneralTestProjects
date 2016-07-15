using System;
using System.ServiceModel.Channels;

namespace TexasTest
{
    public class CustomTextMessageBindingElement : MessageEncodingBindingElement
    {
        public string Encoding { get; set; }
        public string MediaType { get; set; }

        public CustomTextMessageBindingElement(string encoding, string mediaType, MessageVersion version)
        {
            MediaType = mediaType;
            Encoding = encoding;
            MessageVersion = version;
        }

        public CustomTextMessageBindingElement(): this("UTF-8", "application/soap+xml", MessageVersion.Soap12WSAddressing10)
        {
            
        }

        public override MessageVersion MessageVersion { get; set; }

        public override BindingElement Clone()
        {
            return new CustomTextMessageBindingElement();
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new CustomTextMessageEncoderFactory(MediaType, Encoding, MessageVersion);
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }
    }
}