using System;
using System.Collections.Generic;
using TexasTest.XCPD;

namespace TexasTest
{
    public class PatientDemographicInfo
    {
        public IEnumerable<PatientIdentifier> PatientIdentifier { get; set; }
        public IEnumerable<PatientIdentifier> OtherIdentifiers { get; set; }
        public IEnumerable<AddressInfo> AddressInformation { get; set; }
        public IEnumerable<PatientName> PatientName { get; set; }
        public string StatusCode { get; set; }
        public string Gender { get; set; }
        public bool IsDeceased { get; set; }
        public DateTime BirthDate { get; set; }
        public GeneralValueSpecification MatchConfidencePercent { get; set; }
    }
}