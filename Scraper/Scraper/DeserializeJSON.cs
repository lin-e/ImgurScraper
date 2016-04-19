using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Scraper
{
    // Code from http://stackoverflow.com/a/3806407
    public class DynamicJson
    {
        public static dynamic Deserialize(string inputString)
        {
            JavaScriptSerializer mainSerializer = new JavaScriptSerializer();
            mainSerializer.RegisterConverters(new[] { new DynamicJsonConverter() });
            return mainSerializer.Deserialize(inputString, typeof(object));
        }
    }
    public sealed class DynamicJsonConverter : JavaScriptConverter
    {
        public override object Deserialize(IDictionary<string, object> objectDictionary, Type itemType, JavaScriptSerializer jsSerializer)
        {
            if (objectDictionary == null)
            {
                throw new ArgumentNullException("objectDictionary");
            }
            return itemType == typeof(object) ? new DynamicJsonObject(objectDictionary) : null;
        }
        public override IDictionary<string, object> Serialize(object inputObject, JavaScriptSerializer jsSerializer)
        {
            throw new NotImplementedException();
        }
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new ReadOnlyCollection<Type>(new List<Type>(new[] { typeof(object) })); }
        }
        private sealed class DynamicJsonObject : DynamicObject
        {
            private readonly IDictionary<string, object> objectDictionary;

            public DynamicJsonObject(IDictionary<string, object> objectDictionary)
            {
                if (objectDictionary == null)
                {
                    throw new ArgumentNullException("objectDictionary");
                }
                this.objectDictionary = objectDictionary;
            }
            public override string ToString()
            {
                var stringBuilder = new StringBuilder("{");
                ToString(stringBuilder);
                return stringBuilder.ToString();
            }
            private void ToString(StringBuilder stringBuilder)
            {
                var firstInDictionary = true;
                foreach (var keyPair in objectDictionary)
                {
                    if (!firstInDictionary)
                    {
                        stringBuilder.Append(",");
                    }
                    firstInDictionary = false;
                    var keyValue = keyPair.Value;
                    var keyName = keyPair.Key;
                    if (keyValue is string)
                    {
                        stringBuilder.AppendFormat("{0}:\"{1}\"", keyName, keyValue);
                    }
                    else if (keyValue is IDictionary<string, object>)
                    {
                        new DynamicJsonObject((IDictionary<string, object>)keyValue).ToString(stringBuilder);
                    }
                    else if (keyValue is ArrayList)
                    {
                        stringBuilder.Append(keyName + ":[");
                        var firstInArray = true;
                        foreach (var arrayValue in (ArrayList)keyValue)
                        {
                            if (!firstInArray)
                            {
                                stringBuilder.Append(",");
                            }
                            firstInArray = false;
                            if (arrayValue is IDictionary<string, object>)
                            {
                                new DynamicJsonObject((IDictionary<string, object>)arrayValue).ToString(stringBuilder);
                            }
                            else if (arrayValue is string)
                            {
                                stringBuilder.AppendFormat("\"{0}\"", arrayValue);
                            }
                            else
                            {
                                stringBuilder.AppendFormat("{0}", arrayValue);
                            }

                        }
                        stringBuilder.Append("]");
                    }
                    else
                    {
                        stringBuilder.AppendFormat("{0}:{1}", keyName, keyValue);
                    }
                }
                stringBuilder.Append("}");
            }
            public override bool TryGetMember(GetMemberBinder memberBinder, out object objResult)
            {
                if (!objectDictionary.TryGetValue(memberBinder.Name, out objResult))
                {
                    objResult = null;
                    return true;
                }
                objResult = WrapResultObject(objResult);
                return true;
            }
            public override bool TryGetIndex(GetIndexBinder memberBinder, object[] indexArray, out object objResult)
            {
                if (indexArray.Length == 1 && indexArray[0] != null)
                {
                    if (!objectDictionary.TryGetValue(indexArray[0].ToString(), out objResult))
                    {
                        objResult = null;
                        return true;
                    }
                    objResult = WrapResultObject(objResult);
                    return true;
                }
                return base.TryGetIndex(memberBinder, indexArray, out objResult);
            }
            private static object WrapResultObject(object objResult)
            {
                var dictionary = objResult as IDictionary<string, object>;
                if (dictionary != null)
                    return new DynamicJsonObject(dictionary);

                var arrayList = objResult as ArrayList;
                if (arrayList != null && arrayList.Count > 0)
                {
                    return (arrayList[0] is IDictionary<string, object>) ? new List<object>(arrayList.Cast<IDictionary<string, object>>().Select(x => new DynamicJsonObject(x))) : new List<object>(arrayList.Cast<object>());
                }
                return objResult;
            }
        }
    }
}