using TexasTest.XCPD;

namespace TexasTest
{
    public class AddressInfo
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public string HouseNumber { get; set; }
        public string County { get; set; }

        public AddressInfo() { }

        public AddressInfo(AD address)
        {
            Street = address.streetAddressLine[0];
            City = address.city;
            State = address.state;
            ZipCode = address.postalCode;
            County = address.county;
            Country = address.country;
            HouseNumber = address.houseNumber;
        }
    }
}