using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MData.Models
{
    public enum MyCustomEnum  {  Foo =1, Bar =2 }
    public class SampleDataModel :DataModel
    {
        [PrimaryKey]
        public long Id { get; set; }

        public string TextData1 { get; set; }

        public int IntData2 { get; set; }

        public MyCustomEnum CustomEnum { get; set; } 

     
        [ExcludeAsSqlParam]
        public bool IsAuthenticated { get; set; } = false;  // example of field not stored in database
      
        public SampleDataModel()
        {
            // any casting or changes when setting parameters
            RuleSet = new Dictionary<object, object>()
            {
                {  typeof(MyCustomEnum), new RulesCaster() { CastTypeParameter = typeof(Int32), CastTypeFill = typeof(MyCustomEnum) } },
            };
        }


        
        public SampleDataModel Copy()
        {
            return (SampleDataModel)this.MemberwiseClone();
        }

    
        public static List<SampleDataModel> GetList(Dictionary<string, object> parmValues = null)
        {
            SampleDataModel sampleDataModel = new SampleDataModel();
            return sampleDataModel.GetList<SampleDataModel>(parmValues);
        }

     
        public static SampleDataModel Get(Dictionary<string, object> parmValues = null)
        {
            SampleDataModel sampleDataModel = new SampleDataModel();
            return sampleDataModel.Get<SampleDataModel>(parmValues);
        }

        private List<SampleDataModel> getList(Dictionary<string, object> parmValues = null)
        {
            return base.GetList<SampleDataModel>(parmValues);
        }
        private SampleDataModel get(Dictionary<string, object> parmValues = null)
        {
            return base.Get<SampleDataModel>(parmValues);
        }
        

        // override for unit test
        public override string ToString()
        {
            return string.Format("Id : {0}  TextData1 : {1} : IntData2 : {2} : CustomEnum : {3} ", this.Id, this.TextData1, this.IntData2, this.CustomEnum.ToString());
        }

    }
}

