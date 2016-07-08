using System;
using System.ServiceModel;
using System.Xml;
using TexasTest.XCPD;

namespace TexasTest
{
    class Program
    {
        private const string HomeCommunityId = "2.16.840.1.113883.3.3126.20.3.7";

        static void Main(string[] args)
        {
            var ack = DoStuff();

            //while (!task.IsFaulted && !task.IsCompleted)
            //{
            //    Thread.Sleep(10);
            //}
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        private static Acknowledgement DoStuff()
        {
            var client = new RespondingGatewaySyncClient(new WSHttpBinding(SecurityMode.Transport),
                new EndpointAddress(
                    "https://open-ic.epic.com/Interconnect-CE-2015/wcf/epic.community.hie/xcpdrespondinggatewaysync.svc"));


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
                        }
                    }
                }
            };
            XmlAttribute[] attributes = null;
            var controlActProcessResponse = new RegistryQueryResponseControlActOfSubject1DemographicsParameterList();
            var ack = client.CrossGatewayPatientDiscovery(ref id, ref creationTime, ref interactionId,
                ref processingCode,
                ref processingModeCode, ref acceptAckCode, ref receiver, respondTo, ref sender, controlActProcess,
                ref attributes, out controlActProcessResponse);
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
}