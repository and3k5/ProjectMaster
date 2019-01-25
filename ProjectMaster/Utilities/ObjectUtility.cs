using System.Linq;

namespace ProjectMaster.Utilities
{
    public static class ObjectUtility
    {
        /// <summary>
        /// Copies common properties from A to B
        /// </summary>
        /// <typeparam name="TA">Class type of A</typeparam>
        /// <typeparam name="TB">Class type of B</typeparam>
        /// <param name="a">Object to copy properties from</param>
        /// <param name="b">Object to copy properties to</param>
        public static void CopyCommonValues<TA, TB>(TA a, TB b) where TA : class where TB : class
        {
            var aProperties = typeof(TA).GetProperties();
            var bProperties = typeof(TB).GetProperties();

            foreach (var aPropertyInfo in aProperties)
            {
                var bPropertyInfo = bProperties.SingleOrDefault(p => p.Module == aPropertyInfo.Module && p.MetadataToken == aPropertyInfo.MetadataToken);

                if (bPropertyInfo == null)
                    continue;

                var value = aPropertyInfo.GetValue(a);

                bPropertyInfo.SetValue(b, value);
            }
        }
    }
}