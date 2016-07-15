using System;
using System.Collections.Generic;

namespace TexasTest
{
    public class PatientDemographicInfo
    {
        public IEnumerable<PatientIdentifier> PatientIdentifier { get; set; }
        public IEnumerable<AddressInfo> AddressInformation { get; set; }
        public IEnumerable<PatientName> PatientName { get; set; }
        public string Gender { get; set; }
        public bool IsDeceased { get; set; }
        public DateTime BirthDate { get; set; }
    }
}