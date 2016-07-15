using System.Collections.Generic;
using System.Linq;
using TexasTest.XCPD;

namespace TexasTest
{
    public class PatientName
    {
        public IEnumerable<string> Given { get; set; }
        public string Family { get; set; }

        public PatientName() { }

        public PatientName(PN name)
        {
            Given = name.given.Select(g => g.Value);
            Family = name.family.Value;
        }
    }
}