using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Xml;

namespace TexasTest
{
    public class CustomTextMessageEncoder : MessageEncoder
    {
        private readonly CustomTextMessageEncoderFactory _factory;

        public CustomTextMessageEncoder(CustomTextMessageEncoderFactory factory)
        {
            _factory = factory;

            var writerSettings = new XmlWriterSettings();
            ContentType = $"{_factory.MediaType}; charset={writerSettings.Encoding.HeaderName}";
        }

        public override string ContentType { get; }

        public override string MediaType => _factory.MediaType;

        public override MessageVersion MessageVersion => _factory.MessageVersion;

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            var msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            var stream = new MemoryStream(msgContents);
            stream = ProcessMemoryStream(stream, false);
            return ReadMessage(stream, int.MaxValue);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            stream = ProcessMemoryStream(stream, false);
            var reader = XmlReader.Create(stream);
            return Message.CreateMessage(reader, maxSizeOfHeaders, MessageVersion);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            var stream = new MemoryStream();
            var writer = XmlWriter.Create(stream);
            message.WriteMessage(writer);
            writer.Close();

            var messageBytes = stream.GetBuffer();
            var messageLength = (int)stream.Position;
            stream.Close();

            var totalLength = messageLength + messageOffset;
            var totalBytes = bufferManager.TakeBuffer(totalLength);
            Array.Copy(messageBytes, 0, totalBytes, messageOffset, messageLength);

            var byteArray = new ArraySegment<byte>(totalBytes, messageOffset, messageLength);
            return byteArray;
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            stream = ProcessMemoryStream(stream, false);
            var writer = XmlWriter.Create(stream);
            message.WriteMessage(writer);
            writer.Close();
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