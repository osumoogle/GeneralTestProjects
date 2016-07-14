using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using TexasTest.XCPD;

namespace TexasTest
{
    class Program
    {
        private const string HomeCommunityId = "2.16.840.1.113883.3.3126.20.3.7";
        //private const string CertificateName = "CN=Kno2, O=Kno2, L=Boise, S=Idaho, C=US";
        private const string CertificateName = "CN=kno2fy-cq.com";

        static void Main(string[] args)
        {
            var ack = DoStuff();

            Console.WriteLine($"{ack.controlActProcess.subject != null}");
            Console.WriteLine($"{ack.controlActProcess.queryAck.queryResponseCode.code}");

            //while (!task.IsFaulted && !task.IsCompleted)
            //{
            //    Thread.Sleep(10);
            //}
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        private static Saml2SecurityToken GeneratedSaml2Token()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, CertificateName, false);
            var cert = certs.Cast<X509Certificate2>().FirstOrDefault();

            if (cert == null)
                throw new Exception("Unable to find certificate");

            var rsa = cert.PrivateKey as RSACryptoServiceProvider;
            var rsaClause = new RsaKeyIdentifierClause(rsa);
            var ski = new SecurityKeyIdentifier(rsaClause);
            var issueInstant = DateTime.UtcNow;
            var descriptor = new SecurityTokenDescriptor
            {
                TokenIssuerName = CertificateName,
                TokenType = SecurityTokenTypes.Saml,
                Lifetime = new Lifetime(issueInstant, issueInstant + new TimeSpan(8, 0, 0)),
                //AppliesToAddress = "http://localhost/RelyingPartyApplication",
                Subject = new ClaimsIdentity(new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, "test"),
                    new Claim("urn:oasis:names:tc:xspa:1.0:subject:subject-id", "test"),
                    new Claim("urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName", "test"),
                    new Claim("urn:oasis:names:tc:xspa:1.0:subject:organization", $"urn:oid:{HomeCommunityId}"),
                    new Claim("urn:oasis:names:tc:xspa:1.0:subject:organization-id", $"urn:oid:{HomeCommunityId}"),
                    new Claim("urn:nhin:names:saml:homeCommunityId", $"urn:oid:{HomeCommunityId}"),
                    new Claim("urn:nhin:names:saml:homeCommunityId", $"urn:oid:{HomeCommunityId}")
                }),
                SigningCredentials = new X509SigningCredentials(cert, SecurityAlgorithms.RsaSha1Signature, SecurityAlgorithms.Sha1Digest),
                Proof = new AsymmetricProofDescriptor(ski)
            };

            var tokenHandler = new Saml2SecurityTokenHandler(new SamlSecurityTokenRequirement());
            var token = tokenHandler.CreateToken(descriptor) as Saml2SecurityToken;
            var rsaKey = new RsaSecurityKey(rsa);
            var keys = new ReadOnlyCollection<SecurityKey>(new SecurityKey[] { rsaKey });

            return new Saml2SecurityToken(token.Assertion, keys, new X509SecurityToken(cert));
        }

        private static PatientRegistryFindCandidatesResponse DoStuff()
        {
            //var binding = new WS2007FederationHttpBinding(WSFederationHttpSecurityMode.TransportWithMessageCredential);
            //binding.Security.Message.IssuedKeyType = SecurityKeyType.SymmetricKey;
            //binding.Security.Message.EstablishSecurityContext = false;
            //var endpoint = new EndpointAddress(
            //        "https://open-ic.epic.com/Interconnect-CE-2015/wcf/epic.community.hie/xcpdrespondinggatewaysync.svc");
            
            //var factory = new ChannelFactory<IRespondingGatewaySyncChannel>(binding, endpoint);
            var factory = new ChannelFactory<IRespondingGatewaySyncChannel>("IRespondingGatewaySync");
            factory.Credentials.SupportInteractive = false;
            factory.Credentials.UseIdentityConfiguration = true;
            //factory.Credentials.ClientCertificate.SetCertificate(CertificateName, StoreLocation.LocalMachine, StoreName.My);

            ((CustomBinding)factory.Endpoint.Binding).Elements.Insert(1, new CustomTextMessageBindingElement());

            var client = factory.CreateChannelWithIssuedToken(GeneratedSaml2Token());

            client.Open();
            var id = new II();
            var creationTime = new TS();
            var interactionId = new II();
            var processingCode = new CS();
            var processingModeCode = new CS();
            var acceptAckCode = new CS();
            var receiver = new Receiver();
            var respondTo = new RespondTo();
            var sender = new Sender
            {
                typeCode = "SND",
                device = new Device
                {
                    classCode = "DEV",
                    determinerCode = "INSTANCE",
                    id = new[] { new II { root = "1.2.840.114350.1.13.999.567" } },
                    asAgent = new Agent
                    {
                        classCode = "AGNT",
                        representedOrganization = new Organization()
                        {
                            classCode = "ORG",
                            determinerCode = "INSTANCE",
                            id = new II { root = HomeCommunityId }
                        }
                    }
                }
            };
            var controlActProcess = new QueryControlActRequestOfQueryByParameterOfDemographicsParameterList
            {
                classCode = "CACT",
                moodCode = "EVN",
                code = new CD
                {
                    code = "PRPA_TE201305UV02",
                    codeSystem = "2.16.840.1.113883.1.6"
                },
                queryByParameter = new QueryByParameterOfDemographicsParameterList
                {
                    queryId = new II { root = "1.2.840.114350.1.13.28.1.18.5.999", extension = "18204" },
                    statusCode = new CS { code = "new" },
                    parameterList = new DemographicsParameterList
                    {
                        livingSubjectName = new ParameterItemOfEntityName
                        {
                            value =
                                new EN
                                {
                                    family = new EntityNamePart { Value = "Nwhinzzztestpatient" },
                                    given = new[] { new EntityNamePart { Value = "Nwhintwo" } }
                                },
                            semanticsText = "LivingSubject.name"
                        },
                        livingSubjectAdministrativeGender = new ParameterItemOfCodedWithEquivalents()
                        {
                            value = new CE { code = "M" }
                        },
                        livingSubjectBirthTime = new ParameterItemOfPointInTime
                        {
                            value = new TS
                            {
                                value = "19820102"
                            }
                        },
                        patientAddress = new ParameterItemOfPostalAddress
                        {
                            value = new AD
                            {
                                city = "Helena",
                                state = "AL",
                                streetAddressLine = new[]
                                {
                                    "1200 Test Street"
                                },
                                postalCode = "35080",
                            },
                        },
                    }
                }
            };
            XmlAttribute[] attributes = null;
            var request = new PatientRegistryQueryByDemographics(
                id,
                creationTime,
                interactionId,
                processingCode,
                processingModeCode,
                acceptAckCode,
                receiver,
                null,
                sender,
                controlActProcess,
                attributes);
            var ack = client.CrossGatewayPatientDiscovery(request);
            client.Close();
            return ack;
        }
    }

    //private static Task<AdhocQueryResponse> DoStuff()
    //{
    //    var client =
    //        new QueryClient(
    //            new WSHttpBinding
    //            {
    //                TextEncoding = Encoding.UTF8,
    //                Security = new WSHttpSecurity {Mode = SecurityMode.Transport}
    //            },
    //            new EndpointAddress(
    //                "https://open-ic.epic.com/interconnect-ce-2015/wcf/epic.community.hie/respondinggateway.svc"));
    //    client.Open();
    //    return client.CrossGatewayQuerySyncRequestAsync(new AdhocQueryRequest(
    //        new ResponseOption {returnComposedObjects = "true", returnType = ReturnType.LeafClass}, new AdhocQuery
    //        {
    //            Slot = new[]
    //            {
    //                new Slot
    //                {
    //                    name = "LivingSubjectAdministrativeGender",
    //                    ValueList = new[]
    //                    {
    //                        new Value {Value1 = "M"}
    //                    }
    //                },
    //                new Slot
    //                {
    //                    name = "LivingSubjectName",
    //                    slotType = "given",
    //                    ValueList = new[]
    //                    {
    //                        new Value {Value1 = "John"}
    //                    }
    //                },
    //                new Slot
    //                {
    //                    name = "LivingSubjectName",
    //                    slotType = "family",
    //                    ValueList = new[]
    //                    {
    //                        new Value {Value1 = "Smith"} 
    //                    }
    //                }
    //            },
    //            id = Guid.NewGuid().ToString(),
    //            home = Guid.NewGuid().ToString()
    //        }));
    //}

    //private static Acknowledgement DoStuff()
    //{
    //    var client = new CareRespondingGatewaySyncClient(new WSHttpBinding { TextEncoding = Encoding.UTF8, Security = new WSHttpSecurity { Mode = SecurityMode.Transport } }, new EndpointAddress(
    //        "https://open-ic.epic.com/interconnect-ce-2015/wcf/epic.community.hie/respondinggateway.svc?wsdl"));

    //    var id = new II {};
    //    var created = new TS {};
    //    var interaction_id = new II();
    //    var processingMode = new CS();
    //    var processingCode = new CS();
    //    var processingAckCode = new CS();
    //    var rec = new Receiver
    //    {
    //        device = new Device
    //        {

    //        }
    //    };
    //    var respondTo = new RespondTo();
    //    var responseList = new RegistryQueryResponseControlActOfSubject1DemographicsParameterList();
    //    var sender = new Sender();
    //    XmlAttribute[] myRefs = null;
    //    var retVal = client.CrossGatewayPatientDiscovery(ref id, ref created, ref interaction_id, ref processingCode,
    //        ref processingMode, ref processingAckCode, ref rec, respondTo, ref sender,
    //        new QueryControlActRequestOfQueryByParameterOfDemographicsParameterList
    //        {
    //            authorOrPerformer = new AuthorOrPerformer
    //            {
    //                assignedDevice = new AssignedDevice
    //                {
    //                    id = new II()
    //                    {

    //                    }
    //                }
    //            }
    //        }, ref myRefs, out responseList);
    //    client.Close();
    //    return retVal;
    //}

    //private static PRPA_IN201306UV02 Test()
    //{
    //    var client = new CareQualityService.RespondingGateway_PortTypeClient(new BasicHttpBinding(),
    //        new EndpointAddress("http://gazelle.ihe.net/XCPDRESPSimulator-XCPDRESPSimulator/RespondingGatewayPortTypeImpl"));
    //    client.Open();
    //    return client.RespondingGateway_PRPA_IN201305UV02(new PRPA_IN201305UV02
    //    {
    //        controlActProcess = new PRPA_IN201305UV02QUQI_MT021001UV01ControlActProcess
    //        {
    //            queryByParameter = new PRPA_MT201306UV02QueryByParameter
    //            {
    //                parameterList = new PRPA_MT201306UV02ParameterList
    //                {
    //                    livingSubjectName = new[]
    //                    {
    //                        new PRPA_MT201306UV02LivingSubjectName
    //                        {
    //                            value = new[]
    //                            {
    //                                new EN
    //                                {
    //                                    //Items = new object[] { "given" },
    //                                    Text = new[] {"John"}
    //                                },
    //                                new EN
    //                                {
    //                                    //Items = new object[] { "family" },
    //                                    Text = new [] { "Smith" }
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    });
    //}

    public class CustomTextMessageEncoder : MessageEncoder
    {
        private CustomTextMessageEncoderFactory factory;
        private XmlWriterSettings writerSettings;
        private string contentType;

        public CustomTextMessageEncoder(CustomTextMessageEncoderFactory factory)
        {
            this.factory = factory;

            this.writerSettings = new XmlWriterSettings();
            //this.writerSettings.Encoding = Encoding.GetEncoding(factory.CharSet);
            this.contentType = string.Format("{0}; charset={1}",
                this.factory.MediaType, this.writerSettings.Encoding.HeaderName);
        }

        public override string ContentType
        {
            get
            {
                return this.contentType;
            }
        }

        public override string MediaType
        {
            get
            {
                return factory.MediaType;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return this.factory.MessageVersion;
            }
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            byte[] msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            MemoryStream stream = new MemoryStream(msgContents);
            stream = ProcessMemoryStream(stream, false);
            return ReadMessage(stream, int.MaxValue);
        }

        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            stream = ProcessMemoryStream(stream, false);
            XmlReader reader = XmlReader.Create(stream);
            return Message.CreateMessage(reader, maxSizeOfHeaders, this.MessageVersion);
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            MemoryStream stream = new MemoryStream();
            XmlWriter writer = XmlWriter.Create(stream);
            message.WriteMessage(writer);
            writer.Close();

            byte[] messageBytes = stream.GetBuffer();
            int messageLength = (int)stream.Position;
            stream.Close();

            int totalLength = messageLength + messageOffset;
            byte[] totalBytes = bufferManager.TakeBuffer(totalLength);
            Array.Copy(messageBytes, 0, totalBytes, messageOffset, messageLength);

            ArraySegment<byte> byteArray = new ArraySegment<byte>(totalBytes, messageOffset, messageLength);
            return byteArray;
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            stream = ProcessMemoryStream(stream, false);
            XmlWriter writer = XmlWriter.Create(stream);
            message.WriteMessage(writer);
            writer.Close();
        }

        private MemoryStream ProcessMemoryStream(Stream inputStream, bool dispose)
        {
            StreamWriter xmlStream = null;
            var outputStream = new MemoryStream();
            bool continueFilter = false;
            try
            {
                xmlStream = new StreamWriter(outputStream);
                using (var reader = XmlReader.Create(inputStream))
                {
                    using (
                        var writer = XmlWriter.Create(xmlStream,
                            new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Auto }))
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
            catch (Exception ex)
            {
                // handle error
                throw;
            }
            finally
            {
                if (xmlStream != null && dispose) xmlStream.Dispose();
            }
        }

        internal static class XmlHelper
        {
            internal static void WriteShallowNode(XmlReader reader, XmlWriter writer)
            {
                if (reader == null)
                {
                    throw new ArgumentNullException("reader");
                }
                if (writer == null)
                {
                    throw new ArgumentNullException("writer");
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


    public class CustomTextMessageEncoderFactory : MessageEncoderFactory
    {
        private MessageEncoder encoder;
        private MessageVersion version;
        private string mediaType;
        private string charSet;

        internal CustomTextMessageEncoderFactory(string mediaType, string charSet,
            MessageVersion version)
        {
            this.version = version;
            this.mediaType = mediaType;
            this.charSet = charSet;
            this.encoder = new CustomTextMessageEncoder(this);
        }

        public override MessageEncoder Encoder
        {
            get
            {
                return this.encoder;
            }
        }

        public override MessageVersion MessageVersion
        {
            get
            {
                return this.version;
            }
        }

        internal string MediaType
        {
            get
            {
                return this.mediaType;
            }
        }

        internal string CharSet
        {
            get
            {
                return this.charSet;
            }
        }
    }

    public class CustomTextMessageBindingElement : MessageEncodingBindingElement
    {
        public string Encoding { get; set; }
        public string MediaType { get; set; }

        public CustomTextMessageBindingElement(string encoding, string mediaType, MessageVersion version)
        {
            this.MediaType = mediaType;
            this.Encoding = encoding;
            this.MessageVersion = version;
        }

        CustomTextMessageBindingElement(CustomTextMessageBindingElement binding)
            : this(binding.Encoding, binding.MediaType, binding.MessageVersion)
        {
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
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return context.CanBuildInnerChannelFactory<TChannel>();
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.BuildInnerChannelListener<TChannel>();
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            context.BindingParameters.Add(this);
            return context.CanBuildInnerChannelListener<TChannel>();
        }
    }
}