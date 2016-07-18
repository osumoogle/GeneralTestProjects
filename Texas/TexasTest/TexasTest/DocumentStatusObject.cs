using System;

namespace TexasTest
{
    public class DocumentStatusObject
    {
        private const string Prefix = "urn:oasis:names:tc:ebxml-regrep:StatusType:";
        private readonly DocumentStatus _documentStatus;

        public DocumentStatusObject(DocumentStatus status)
        {
            _documentStatus = status;
        }

        public override string ToString()
        {
            return $"{Prefix}{Enum.GetName(typeof(DocumentStatus), _documentStatus)}";
        }
    }
}