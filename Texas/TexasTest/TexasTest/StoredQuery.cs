using System;
using System.Collections.Generic;

namespace TexasTest
{
    public class StoredQuery
    {
        public StoredQueryIdentifier Identifier { get; set; }
        private const string Prefix = "urn:uuid:";
        private readonly Dictionary<StoredQueryIdentifier, string> _queries = new Dictionary<StoredQueryIdentifier, string>
        {
            { StoredQueryIdentifier.FindDocuments, "14d4debf-8f97-4251-9a74-a90016b0af0d" },
            { StoredQueryIdentifier.FindSubmissionSets, "f26abbcb-ac74-4422-8a30-edb644bbc1a9" },
            { StoredQueryIdentifier.FindFolders, "958f3006-baad-4929-a4deff1114824431" },
            { StoredQueryIdentifier.GetAll, "10b545ea-725c-446d-9b95-8aeb444eddf3" },
            { StoredQueryIdentifier.GetDocuments, "5c4f972b-d56b-40ac-a5fcc8ca9b40b9d4" },
            { StoredQueryIdentifier.GetFolders, "5737b14c-8a1a-4539-b659-e03a34a5e1e4" },
            { StoredQueryIdentifier.GetAssociations, "a7ae438b-4bc2-4642-93e9-be891f7bb155" },
            { StoredQueryIdentifier.GetDocumentsAndAssociations, "bab9529a-4a10-40b3-a01ff68a615d247a" },
            { StoredQueryIdentifier.GetSubmissionSets, "51224314-5390-4169-9b91-b1980040715a" },
            { StoredQueryIdentifier.GetSubmissionSetsAndContents, "e8e3cb2c-e39c-46b9-99e4-c12f57260b83" },
            { StoredQueryIdentifier.GetFolderAndContents, "b909a503-523d-4517-8acf-8e5834dfc4c7" },
            { StoredQueryIdentifier.GetFoldersForDocument, "10cae35a-c7f9-4cf5-b61efc3278ffb578" },
            { StoredQueryIdentifier.GetRelatedDocuments, "d90e5407-b356-4d91-a89f-873917b4b0e6" },
            { StoredQueryIdentifier.FindDocumentsByReferenceId, "12941a89-e02e-4be5-967cce4bfc8fe492" },
        };

        public StoredQuery(StoredQueryIdentifier identifier)
        {
            Identifier = identifier;
        }

        public override string ToString()
        {
            return $"{Prefix}{_queries[Identifier]}";
        }
    }
}
