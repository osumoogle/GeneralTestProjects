using System;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.Net;

namespace TextAndMtom
{
    public class IncomingEncoderInspector : IEndpointBehavior, IDispatchMessageInspector
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        public object AfterReceiveRequest(ref Message request, System.ServiceModel.IClientChannel channel, System.ServiceModel.InstanceContext instanceContext)
        {
            object result;
            request.Properties.TryGetValue(TextOrMtomEncodingBindingElement.IsIncomingMessageMtomPropertyName, out result);
            return result;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var isMtom = (correlationState is bool) && (bool)correlationState;
            reply.Properties.Add(TextOrMtomEncodingBindingElement.IsIncomingMessageMtomPropertyName, isMtom);
            if (!isMtom) return;
            var boundary = $"uuid:{Guid.NewGuid()}";
            const string startUri = "http://tempuri.org/0";
            const string startInfo = "application/soap+xml";
            var contentType = "multipart/related; type=\"application/xop+xml\";start=\"<" +
                              startUri +
                              ">\";boundary=\"" +
                              boundary +
                              "\";start-info=\"" +
                              startInfo + "\"";

            HttpResponseMessageProperty respProp;
            if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
            {
                respProp = reply.Properties[HttpResponseMessageProperty.Name] as HttpResponseMessageProperty;
            }
            else
            {
                respProp = new HttpResponseMessageProperty();
                reply.Properties[HttpResponseMessageProperty.Name] = respProp;
            }

            respProp.Headers[HttpResponseHeader.ContentType] = contentType;
            respProp.Headers["MIME-Version"] = "1.0";

            reply.Properties[TextOrMtomEncodingBindingElement.MtomBoundaryPropertyName] = boundary;
            reply.Properties[TextOrMtomEncodingBindingElement.MtomStartInfoPropertyName] = startInfo;
            reply.Properties[TextOrMtomEncodingBindingElement.MtomStartUriPropertyName] = startUri;
        }
    }
}
