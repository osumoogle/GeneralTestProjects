using NUnit.Framework;
using TexasTest;

namespace Tests
{
    [TestFixture]
    public class UnitTest1
    {
        [TestCase(DocumentStatus.Deprecated, "urn:oasis:names:tc:ebxml-regrep:StatusType:Deprecated")]
        [TestCase(DocumentStatus.Approved, "urn:oasis:names:tc:ebxml-regrep:StatusType:Approved")]
        [TestCase(DocumentStatus.Submitted, "urn:oasis:names:tc:ebxml-regrep:StatusType:Submitted")]
        public void TestDocumentStatus(DocumentStatus status, string expectedString)
        {
            Assert.AreEqual(expectedString, new DocumentStatusObject(status).ToString());
        }
    }
}
