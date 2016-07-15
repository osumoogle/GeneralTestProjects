using TexasTest.XCPD;

namespace TexasTest
{
    public class PatientIdentifier
    {
        public string Identifier { get; set; }
        public string Extension { get; set; }

        public PatientIdentifier()
        {
            
        }

        public PatientIdentifier(II node)
        {
            Identifier = node.root;
            Extension = node.extension;
        }
    }
}