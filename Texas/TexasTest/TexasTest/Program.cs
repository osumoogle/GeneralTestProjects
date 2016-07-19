using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using TexasTest.XCPD;
using TexasTest.XCA;
using TextAndMtom;

namespace TexasTest
{
    internal class Program
    {
        private const string HomeCommunityId = "urn:oid:2.16.840.1.113883.3.3126.20.3.7";
        private const string CertificateName = "CN=kno2fy-cq.com";

        static void Main(string[] args)
        {
            var name = new PatientName
            {
                Given = new [] { "Nwhintwo" } ,
                Family = "Nwhinzzztestpatient"
            };
            var address = new AddressInfo
            {
                City = "Helena",
                State = "AL",
                ZipCode = "35080",
            };
            var birthDate = new DateTime(1982, 1, 2);
            var ack = DoStuff(name, address, birthDate, "M");

            Console.WriteLine($"{ack.controlActProcess.subject != null}");
            Console.WriteLine($"{ack.controlActProcess.queryAck.queryResponseCode.code}");

            var patients = new List<PatientDemographicInfo>();
            if (ack.controlActProcess.subject != null && ack.controlActProcess.subject.Any())
            {
                patients.AddRange(
                    ack.controlActProcess.subject.Select(subject => subject.registrationEvent.subject1.patient)
                        .Select(patient => new PatientDemographicInfo
                        {
                            PatientIdentifier = patient.id.Select(i => new PatientIdentifier(i)),
                            PatientName = patient.patientPerson.name.Select(n => new PatientName(n)),
                            AddressInformation = patient.patientPerson.addr.Select(a => new AddressInfo(a)),
                            Gender = patient.patientPerson.administrativeGenderCode.code,
                            IsDeceased = patient.patientPerson.deceasedInd.value,
                            StatusCode = patient.statusCode.code,
                            BirthDate =
                                DateTime.ParseExact(patient.patientPerson.birthTime.value, "yyyyMMdd",
                                    CultureInfo.CurrentCulture),
                            MatchConfidencePercent = patient.subjectOf1.queryMatchObservation.value
                        }));
            }

            foreach (var patient in patients)
            {
                var serializer = new JavaScriptSerializer();
                Console.WriteLine(serializer.Serialize(patient));
                var documents = GetDocumentMetaData(patient);
                foreach (var doc in documents)
                {
                    //var id = doc.Slot.FirstOrDefault(x => x.name.ToLower().Equals("repositoryuniqueid"));
                    //if (id != null)
                    //{
                    //    Console.WriteLine(id.ValueList?[0].Value1);
                    //    //id);
                    //}
                    Console.WriteLine(doc.id);
                    GetDocument(doc);
                    //Console.WriteLine(serializer.Serialize(doc));
                }
            }
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
                Subject = new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, "test"),
                    new Claim("urn:oasis:names:tc:xspa:1.0:subject:subject-id", "test"),
                    new Claim("urn:oasis:names:tc:SAML:1.1:nameid-format:X509SubjectName", "test"),
                    //new Claim("urn:oasis:names:tc:xspa:1.0:subject:organization", $"urn:oid:{HomeCommunityId}"),
                    //new Claim("urn:oasis:names:tc:xspa:1.0:subject:organization-id", $"urn:oid:{HomeCommunityId}"),
                    new Claim("urn:nhin:names:saml:homeCommunityId", $"{HomeCommunityId}"),
                    //new Claim("urn:nhin:names:saml:homeCommunityId", $"urn:oid:{HomeCommunityId}")
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

        private static T GetClient<T>(string configuration)
        {
            var factory = new ChannelFactory<T>(configuration);
            factory.Credentials.SupportInteractive = false;
            factory.Credentials.UseIdentityConfiguration = true;

            if (typeof(T) == typeof(IQuery))
            {
                ((CustomBinding)factory.Endpoint.Binding).Elements.Insert(1, new CustomTextMessageBindingElement());
                ((CustomBinding) factory.Endpoint.Binding).Elements.Remove<TextMessageEncodingBindingElement>();
            }
            else if (typeof(T) == typeof(IRetrieve))
            {
                //((CustomBinding)factory.Endpoint.Binding).Elements.Insert(1, new CustomTextMessageBindingElement("Mtom", "application/xop+xml", MessageVersion.Soap12WSAddressing10));
                ((CustomBinding)factory.Endpoint.Binding).Elements.Remove<MtomMessageEncodingBindingElement>();
                ((CustomBinding)factory.Endpoint.Binding).Elements.Insert(1, new TextOrMtomEncodingBindingElement());
            }
            else
                ((CustomBinding)factory.Endpoint.Binding).Elements.Insert(1, new CustomTextMessageBindingElement());

            return factory.CreateChannelWithIssuedToken(GeneratedSaml2Token());
        }

        private static PatientRegistryFindCandidatesResponse DoStuff(PatientName name, AddressInfo address, DateTime birthDate, string gender)
        {

            var client = GetClient<IRespondingGatewaySyncChannel>("IRespondingGatewaySync");
            client.Open();
            var id = new II();
            var creationTime = new TS();
            var interactionId = new II();
            var processingCode = new CS();
            var processingModeCode = new CS();
            var acceptAckCode = new CS();
            var receiver = new Receiver();
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
                                    family = new EntityNamePart { Value = name.Family },
                                    given = name.Given.Select(g => new EntityNamePart { Value = g }).ToArray()
                                },
                            semanticsText = "LivingSubject.name"
                        },
                        livingSubjectAdministrativeGender = new ParameterItemOfCodedWithEquivalents
                        {
                            value = new CE { code = gender.ToUpper() },
                            semanticsText = "LivingSubject.Administrative.gender"
                        },
                        livingSubjectBirthTime = new ParameterItemOfPointInTime
                        {
                            value = new TS
                            {
                                value = birthDate.ToString("yyyyMMdd"),
                            },
                            semanticsText = "LivingSubject.BirthTime"
                        },
                        patientAddress = new ParameterItemOfPostalAddress
                        {
                            value = new AD
                            {
                                city = address.City,
                                state = address.State,
                                streetAddressLine = new[]
                                {
                                    address.Street
                                },
                                postalCode = address.ZipCode,
                                country = address.Country,
                                county = address.County
                            },
                        },
                    }
                }
            };
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
                null);
            var ack = client.CrossGatewayPatientDiscovery(request);
            client.Close();
            return ack;
        }

        private static List<ExtrinsicObject> GetDocumentMetaData(PatientDemographicInfo info)
        {
            var client = GetClient<IQuery>("IQuery");
            var storedQuery = new StoredQuery(StoredQueryIdentifier.FindDocuments);
            var query = new AdhocQuery
            {
                id = storedQuery.ToString(),
                home = HomeCommunityId,
            };
            var slots = info.PatientIdentifier.Select(pid => new Slot
            {
                name = "$XDSDocumentEntryPatientId", ValueList = new[]
                {
                    new Value
                    {
                        Value1 = $"'{pid.Extension}^^^&{pid.Identifier}&ISO'"
                    }
                },
                slotType = "XDSDocumentEntry.patientId"
            }).ToList();

            slots.Add(new Slot
            {
                name = "$XDSDocumentEntryStatus",
                ValueList =
                            new[] { new Value { Value1 = "('urn:oasis:names:tc:ebxml-regrep:StatusType:Approved')" } },
                slotType = "XDSDocumentEntry.status"
            });
            query.Slot = slots.ToArray();
            var response = client.CrossGatewayQuerySyncRequest(new AdhocQueryRequest
            {
                AdhocQuery = query,
                ResponseOption = new ResponseOption
                {
                    returnComposedObjects = "true",
                    returnType = ReturnType.LeafClass
                }
            });
            if (response.RegistryObjectList?.ExtrinsicObject != null && response.RegistryObjectList.ExtrinsicObject.Any())
            {
                return response.RegistryObjectList.ExtrinsicObject.ToList();
            }
            return null;
        }

        private static void GetDocument(ExtrinsicObject obj)
        {
            var client = GetClient<IRetrieve>("IRetrieve");
            var storedQuery = new StoredQuery(StoredQueryIdentifier.GetDocuments);
            var uniqueId = obj.Slot.FirstOrDefault(s => s.name.Equals("repositoryUniqueId")).ValueList.First().Value1;
            var query = client.CrossGatewayRetrieveSyncRequest(new RetrieveDocumentSetRequest
            {
                DocumentRequest = new []
                {
                    new DocumentRequest
                    {
                        HomeCommunityId = obj.home,
                        DocumentUniqueId = obj.id,
                        RepositoryUniqueId = uniqueId
                    }
                }
            });
            Console.WriteLine("DocumentRetrieve didn't crash...");
        }
    }
}