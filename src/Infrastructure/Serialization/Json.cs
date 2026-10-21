using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace RoomPilot
{
    public static class Json
    {
        public static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength = 2097152
            };
        }

        public static IDictionary<string, object> Object(object value)
        {
            var result = value as IDictionary<string, object>;
            if (result == null)
                throw new UserError(L10n.Text("error.serviceResponse"));
            return result;
        }

        public static object Get(IDictionary<string, object> obj, string name)
        {
            object v;
            return obj.TryGetValue(name, out v) ? v : null;
        }

        public static string String(IDictionary<string, object> obj, string name)
        {
            return Convert.ToString(Get(obj, name));
        }

        public static IEnumerable<IDictionary<string, object>> Items(object value)
        {
            var seq = value as IEnumerable;
            if (seq == null || value is string)
                yield break;
            foreach (var v in seq)
                yield return Object(v);
        }
    }
}
