using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace TextAndMtom
{
    public class TextOrMtomEncoder : MessageEncoder
    {
        readonly MessageEncoder _textEncoder;
        readonly MessageEncoder _mtomEncoder;

        public TextOrMtomEncoder(MessageVersion messageVersion)
        {
            _textEncoder = new TextMessageEncodingBindingElement(messageVersion, Encoding.UTF8).CreateMessageEncoderFactory().Encoder;
            _mtomEncoder = new MtomMessageEncodingBindingElement(messageVersion, Encoding.UTF8).CreateMessageEncoderFactory().Encoder;
        }

        public override string ContentType => _textEncoder.ContentType;

        public override string MediaType => _textEncoder.MediaType;

        public override MessageVersion MessageVersion => _textEncoder.MessageVersion;

        public override bool IsContentTypeSupported(string contentType)
        {
            return _mtomEncoder.IsContentTypeSupported(contentType);
        }

        public override T GetProperty<T>()
        {
            var result = (_textEncoder.GetProperty<T>() ?? _mtomEncoder.GetProperty<T>()) ?? base.GetProperty<T>();
            return result;
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            var result = _mtomEncoder.ReadMessage(buffer, bufferManager, contentType);
            result.Properties.Add(TextOrMtomEncodingBindingElement.IsIncomingMessageMtomPropertyName, IsMtomMessage(contentType));
            var body = result.ToString();
            var stream = GenerateStreamFromString(body);
            stream = ProcessMemoryStream(stream, false);
            var reader = XmlReader.Create(stream);
            var message = Message.CreateMessage(reader, int.MaxValue, MessageVersion);
            return message;
        }

        public Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            if (!ShouldWriteMtom(message))
                return _textEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
            using (var ms = new MemoryStream())
            {
                var mtomWriter = CreateMtomWriter(ms, message);
                message.WriteMessage(mtomWriter);
                mtomWriter.Flush();
                var buffer = bufferManager.TakeBuffer((int)ms.Position + messageOffset);
                Array.Copy(ms.GetBuffer(), 0, buffer, messageOffset, (int)ms.Position);
                return new ArraySegment<byte>(buffer, messageOffset, (int)ms.Position);
            }
        }

        private static bool IsMtomMessage(string contentType)
        {
            return contentType.IndexOf("type=\"application/xop+xml\"", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool ShouldWriteMtom(Message message)
        {
            object temp;
            return message.Properties.TryGetValue(TextOrMtomEncodingBindingElement.IsIncomingMessageMtomPropertyName, out temp) && (bool)temp;
        }

        private XmlDictionaryWriter CreateMtomWriter(Stream stream, Message message)
        {
            var boundary = message.Properties[TextOrMtomEncodingBindingElement.MtomBoundaryPropertyName] as string;
            var startUri = message.Properties[TextOrMtomEncodingBindingElement.MtomStartUriPropertyName] as string;
            var startInfo = message.Properties[TextOrMtomEncodingBindingElement.MtomStartInfoPropertyName] as string;
            return XmlDictionaryWriter.CreateMtomWriter(stream, Encoding.UTF8, int.MaxValue, startInfo, boundary, startUri, false, false);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            stream = ProcessMemoryStream(stream, false);
            var result = _mtomEncoder.ReadMessage(stream, maxSizeOfHeaders, contentType);
            result.Properties.Add(TextOrMtomEncodingBindingElement.IsIncomingMessageMtomPropertyName, IsMtomMessage(contentType));
            return result;
        }

        private static MemoryStream ProcessMemoryStream(XmlReader reader)
        {
            StreamWriter xmlStream = null;
            var outputStream = new MemoryStream();
            var continueFilter = false;
            try
            {
                xmlStream = new StreamWriter(outputStream);
                using (
                    var writer = XmlWriter.Create(xmlStream,
                        new XmlWriterSettings {ConformanceLevel = ConformanceLevel.Auto}))
                {
                    while (reader.Read())
                    {
                        if (reader.LocalName.Equals("SignatureConfirmation") &&
                            reader.NamespaceURI.Equals(
                                "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd"))
                        {
                            if (!reader.IsEmptyElement) continueFilter = reader.IsStartElement();
                        }
                        else if (reader.LocalName.Equals("Signature") &&
                                 reader.NamespaceURI.Equals("http://www.w3.org/2000/09/xmldsig#"))
                        {
                            if (!reader.IsEmptyElement) continueFilter = reader.IsStartElement();
                        }
                        else if (continueFilter)
                        {
                            // continue to next node
                        }
                        else
                            XmlHelper.WriteShallowNode(reader, writer);
                    }
                    writer.Flush();
                    reader.Close();
                }
                outputStream.Position = 0;
                return outputStream;
            }
            finally
            {
                xmlStream?.Dispose();
            }
        }

        private static MemoryStream ProcessMemoryStream(Stream inputStream, bool dispose)
        {
            StreamWriter xmlStream = null;
            var outputStream = new MemoryStream();
            var continueFilter = false;
            try
            {
                xmlStream = new StreamWriter(outputStream);
                using (var reader = XmlReader.Create(inputStream))
                {
                    using (
                        var writer = XmlWriter.Create(xmlStream,
                            new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Auto }))
                    {
                        while (reader.Read())
                        {
                            if (reader.LocalName.Equals("SignatureConfirmation") &&
                                reader.NamespaceURI.Equals(
                                    "http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd"))
                            {
                                if (!reader.IsEmptyElement) continueFilter = reader.IsStartElement();
                            }
                            else if (reader.LocalName.Equals("Signature") &&
                                     reader.NamespaceURI.Equals("http://www.w3.org/2000/09/xmldsig#"))
                            {
                                if (!reader.IsEmptyElement) continueFilter = reader.IsStartElement();
                            }
                            else if (continueFilter)
                            {
                                // continue to next node
                            }
                            else
                                XmlHelper.WriteShallowNode(reader, writer);
                        }
                        writer.Flush();
                    }
                    reader.Close();
                }
                outputStream.Position = 0;
                return outputStream;
            }
            finally
            {
                if (xmlStream != null && dispose) xmlStream.Dispose();
            }
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            stream = ProcessMemoryStream(stream, false);
            if (ShouldWriteMtom(message))
            {
                XmlDictionaryWriter mtomWriter = CreateMtomWriter(stream, message);
                message.WriteMessage(mtomWriter);
            }
            else
            {
                _textEncoder.WriteMessage(message, stream);
            }
        }
    }

    public static class XmlHelper
    {
        internal static void WriteShallowNode(XmlReader reader, XmlWriter writer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (reader.IsEmptyElement)
                    {
                        writer.WriteEndElement();
                    }
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                case XmlNodeType.CDATA:
                    writer.WriteCData(reader.Value);
                    break;
                case XmlNodeType.EntityReference:
                    writer.WriteEntityRef(reader.Name);
                    break;
                case XmlNodeType.XmlDeclaration:
                case XmlNodeType.ProcessingInstruction:
                    writer.WriteProcessingInstruction(reader.Name, reader.Value);
                    break;
                case XmlNodeType.DocumentType:
                    writer.WriteDocType(reader.Name, reader.GetAttribute("PUBLIC"), reader.GetAttribute("SYSTEM"),
                        reader.Value);
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
            }
        }
    }
}