using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MData.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace MData_UnitTest
{
    [TestClass]
    public class AllTests
    {


        [TestInitialize]
        public void Setup()
        {
            // change the connection string to point at your local server (in DBLayer.cs); run the scripts in sampledbscripts on your server 
            // to use these tests
        }
        [TestMethod]
        public void OneBigTest()
        {
            SampleDataModel sampleDataModel2;
            SampleDataModel sampleDataModel3;
            SampleDataModel sampleDataModel = new SampleDataModel()
            {
                TextData1 = "my new TextData",
                IntData2 = 3,
                CustomEnum = MyCustomEnum.Foo,
            };


            // I haven't yet added fetching the key back, but that is a simple modification
            // so let's get it by TextData1 then by IntData2
            sampleDataModel.Upsert();


            // pass in name value pairs of the columns and search values (using new instance for testing convenience)
            // use TextData1
            sampleDataModel2 = SampleDataModel.Get(new Dictionary<string, object>()
            {
                    { "TextData1",  "my new TextData" },
            });

            Debug.WriteLine(sampleDataModel2.ToString());
            Assert.IsNotNull(sampleDataModel2);



            // use int
            sampleDataModel3 = SampleDataModel.Get(new Dictionary<string, object>()
            {
                    { "IntData2",3},
            });
            Debug.WriteLine(sampleDataModel3.ToString());
            Assert.IsNotNull(sampleDataModel3);

            // change a value
            sampleDataModel2.IntData2 = 10;
            sampleDataModel2.Upsert();

            // fetch back into #3 by id to see if it changed
            sampleDataModel3 = SampleDataModel.Get(new Dictionary<string, object>()
            {
                { "Id", sampleDataModel2.Id }
            });
            Debug.WriteLine(sampleDataModel3.ToString());

            Assert.AreEqual(sampleDataModel3.IntData2, sampleDataModel2.IntData2);

            // add a few (haven't handled bulk add yet but that's easy too. So for now, one at a time
            sampleDataModel = new SampleDataModel()
            {
                TextData1 = "my new TextData2",
                IntData2 = 55,
                CustomEnum = MyCustomEnum.Foo,
            };
            sampleDataModel.Upsert();

            sampleDataModel = new SampleDataModel()
            {
                TextData1 = "my new TextData3",
                IntData2 = 55,
                CustomEnum = MyCustomEnum.Foo,
            };
            sampleDataModel.Upsert();

            // now let's get them all
            // ISSUE: I have a byref issue in my List<T>
            List<SampleDataModel> result = SampleDataModel.GetList();

            foreach (var sample in result)
                Debug.WriteLine(sample.ToString());

            // now lets just get the ones with intData = 3
            result = SampleDataModel.GetList(new Dictionary<string, object>()
            {
                {"IntData2",55}
            });


            foreach (var sample in result)
                Debug.WriteLine(sample.ToString());
        }
    }
}
