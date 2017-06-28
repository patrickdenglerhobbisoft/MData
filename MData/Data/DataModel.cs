using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MData.Models
{
    public class DataModel
    {

        private string ModelName
        {
            get  { return this.GetType().Name; }
        }
        internal Type ModelType
        {
            get { return this.GetType(); }
        }

        public virtual DataModel Copy()
        {
            return this.MemberwiseClone() as DataModel;
        }

        internal virtual List<T> GetList<T>(Dictionary<string, object> parameters)
        {
            var objectTypeInstance = Activator.CreateInstance<T>();
            DBLayer dbLayer= new DBLayer();

            DataSet ds = dbLayer.GetDataSet("spGet" + ModelName, DataUtilities.GetParameterListForFetch(objectTypeInstance as DataModel, parameters));
            dbLayer.Dispose();

            return DataUtilities.CreateObjectListFromDataSet<T> (ds, objectTypeInstance );
        }


        internal virtual T Get<T>(Dictionary<string,object> parameters)
        {
            var objectTypeInstance = Activator.CreateInstance<T>();
            DBLayer dbLayer= new DBLayer();

            DataSet ds = dbLayer.GetDataSet("spGet" + ModelName, DataUtilities.GetParameterListForFetch(objectTypeInstance as DataModel,parameters));
            dbLayer.Dispose();

            return DataUtilities.CreateObjectFromDataRow<T>(DBLayer.TopRow(ds) as DataRow, objectTypeInstance );
            
        }
        public virtual bool Upsert()
        {
            DBLayer dbLayer= new DBLayer();
            long returnValue = 0;
            dbLayer.ExecuteNonQuery("spUpsert" + ModelName, DataUtilities.GetParameterListForUpsert(this), ref returnValue);
            dbLayer.Dispose();
            return (returnValue > 0);
        }


        [ExcludeAsSqlParam]
        public Dictionary<object, object> RuleSet { get; set; } = new Dictionary<object, object>();


    }
}
