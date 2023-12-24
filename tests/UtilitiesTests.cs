using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Scraper.Utilities;

namespace ScraperTests
{
    [TestClass]
    public class UtilitiesTests
    {

        [TestMethod]
        public void DeriveUnitPriceString_2L()
        {
            string? unitPriceString = DeriveUnitPriceString("Bottle 2L", 6.5f);
            Assert.AreEqual<string>(unitPriceString, "3.25/L/2", unitPriceString);
        }

        [TestMethod]
        public void DeriveUnitPriceNoodles()
        {
            string? unitPriceString = DeriveUnitPriceString("72g each 5pack", 4.5f);
            Assert.AreEqual<string>(unitPriceString, "12.5/g/360", unitPriceString);
        }

        [TestMethod]
        public void DeriveUnitPriceString_Multiplier()
        {
            string? unitPriceString = DeriveUnitPriceString("Pouch 4 x 107mL", 6.5f);
            Assert.AreEqual<string>(unitPriceString, "15.19/L/428", unitPriceString);
        }

        [TestMethod]
        public void DeriveUnitPriceString_Decimal()
        {
            string? unitPriceString = DeriveUnitPriceString("Bottle 1.5L", 3f);
            Assert.AreEqual<string>(unitPriceString, "2/L/1.5", unitPriceString);
        }

        [TestMethod]
        public void DeriveUnitPriceString_SimpleKg()
        {
            string? unitPriceString = DeriveUnitPriceString("kg", 3f);
            Assert.AreEqual<string>(unitPriceString, "3/kg/1", unitPriceString);
        }

    }
}